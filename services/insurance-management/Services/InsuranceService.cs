using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Services;
using InsuranceManagement.Data;
using InsuranceManagement.Models;
using InsuranceManagement.Mappers;
using Azure.Messaging.ServiceBus;

namespace InsuranceManagement.Services;

public class InsuranceService
{
    private readonly InsuranceManagementDbContext _db;
    private readonly IServiceBusMessagingService _messagingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InsuranceService> _logger;
    private readonly ApplicationInsightsService _appInsights; public InsuranceService(InsuranceManagementDbContext db, IServiceBusMessagingService messagingService,
        IConfiguration configuration, ILogger<InsuranceService> logger, ApplicationInsightsService appInsights)
    {
        _db = db;
        _messagingService = messagingService;
        _configuration = configuration;
        _logger = logger;
        _appInsights = appInsights;
    }
    public async Task<VehicleInsuranceDto?> GetByIdAsync(Guid id)
    {
        return await _appInsights.TrackOperationAsync(
            "InsuranceService.GetById",
            async () =>
            {
                var insurance = await GetInsuranceWithIncludesAsync()
                    .FirstOrDefaultAsync(x => x.Id == id);

                return insurance?.ToDto();
            },
            new Dictionary<string, string> { ["InsuranceId"] = id.ToString() });
    }
    public async Task<UserInsuranceDetailsDto?> GetByPersonalIdAsync(string personalId)
    {
        return await _appInsights.TrackOperationAsync(
            "InsuranceService.GetByPersonalId",
            async () =>
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.PersonalId == personalId);
                if (user == null) return null;

                var insurances = await GetInsuranceWithIncludesAsync()
                    .Where(vi => vi.UserId == user.Id)
                    .ToListAsync();

                return user.ToDetailsDto(insurances);
            },
            new Dictionary<string, string> { ["PersonalId"] = personalId });
    }
    public async Task<Result<VehicleInsuranceDto>> CreateAsync(CreateInsuranceRequest request)
    {
        try
        {
            var createdInsurance = await _appInsights.TrackOperationAsync(
                "InsuranceService.Create",
                async () =>
                {                    // Track business metrics
                    _appInsights.TrackBusinessEvent("Insurance.CreateRequested", new Dictionary<string, string>
                    {
                        ["UserId"] = request.UserId.ToString(),
                        ["PolicyId"] = request.PolicyId.ToString(),
                        ["VehicleId"] = request.VehicleId.ToString()
                    });

                    // Validate User exists
                    var userExists = await _db.Users.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                    {
                        _appInsights.TrackBusinessEvent("Insurance.CreateValidationError", new Dictionary<string, string>
                        {
                            ["Reason"] = "UserNotFound",
                            ["UserId"] = request.UserId.ToString()
                        });
                        throw new ArgumentException("USER_NOT_FOUND");
                    }

                    // Validate Policy exists
                    var policyExists = await _db.Policies.AnyAsync(p => p.Id == request.PolicyId);
                    if (!policyExists)
                    {
                        _appInsights.TrackBusinessEvent("Insurance.CreateValidationError", new Dictionary<string, string>
                        {
                            ["Reason"] = "PolicyNotFound",
                            ["PolicyId"] = request.PolicyId.ToString()
                        });
                        throw new ArgumentException("POLICY_NOT_FOUND");
                    }

                    // Validate Vehicle exists
                    var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == request.VehicleId);
                    if (!vehicleExists)
                    {
                        _appInsights.TrackBusinessEvent("Insurance.CreateValidationError", new Dictionary<string, string>
                        {
                            ["Reason"] = "VehicleNotFound",
                            ["VehicleId"] = request.VehicleId.ToString()
                        });
                        throw new ArgumentException("VEHICLE_NOT_FOUND");
                    }

                    // Validate no existing active insurance
                    var hasActiveInsurance = await _db.VehicleInsurances.AnyAsync(ui =>
                        ui.UserId == request.UserId &&
                        ui.VehicleId == request.VehicleId &&
                        ui.Status == InsuranceStatus.Active);

                    if (hasActiveInsurance)
                    {
                        _appInsights.TrackBusinessEvent("Insurance.CreateConflict", new Dictionary<string, string>
                        {
                            ["UserId"] = request.UserId.ToString(),
                            ["VehicleId"] = request.VehicleId.ToString(),
                            ["Reason"] = "ActiveInsuranceExists"
                        });

                        throw new InvalidOperationException("CONFLICT");
                    }

                    // Get and validate coverages
                    var coverages = await _db.Coverages
                        .Where(c => request.CoverageIds.Contains(c.Id))
                        .ToListAsync();

                    if (coverages.Count != request.CoverageIds.Count)
                    {
                        var missingCoverageIds = request.CoverageIds.Except(coverages.Select(c => c.Id)).ToList();

                        _appInsights.TrackBusinessEvent("Insurance.CreateValidationError", new Dictionary<string, string>
                        {
                            ["UserId"] = request.UserId.ToString(),
                            ["Reason"] = "InvalidCoverageIds",
                            ["MissingCoverageIds"] = string.Join(",", missingCoverageIds)
                        });

                        throw new ArgumentException($"VALIDATION_ERROR:{string.Join(", ", missingCoverageIds)}");
                    }

                    var totalPremium = coverages.Sum(c => c.Price);
                    var now = DateTime.UtcNow;

                    var insurance = new VehicleInsurance
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        PolicyId = request.PolicyId,
                        VehicleId = request.VehicleId,
                        Status = InsuranceStatus.Active,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        RenewalDate = request.RenewalDate,
                        CreatedAt = now,
                        UpdatedAt = now,
                        TotalPremium = totalPremium,
                        Deductible = request.Deductible,
                        Notes = request.Notes
                    };

                    _db.VehicleInsurances.Add(insurance);

                    // Add coverage relationships
                    foreach (var coverage in coverages)
                    {
                        _db.VehicleInsuranceCoverages.Add(new VehicleInsuranceCoverage
                        {
                            Id = Guid.NewGuid(),
                            VehicleInsuranceId = insurance.Id,
                            CoverageId = coverage.Id,
                            CreatedAt = now,
                            UpdatedAt = now,
                            StartDate = insurance.StartDate,
                            EndDate = insurance.EndDate,
                            Status = VehicleInsuranceCoverageStatus.Active
                        });
                    }

                    await _appInsights.TrackExternalServiceAsync(
                        "Database",
                        "SaveChanges",
                        async () => await _db.SaveChangesAsync());

                    _logger.LogInformation("Insurance created with ID: {InsuranceId}", insurance.Id);

                    // Track business metrics
                    _appInsights.TrackBusinessMetric("Insurance.TotalPremium", (double)totalPremium);
                    _appInsights.TrackBusinessMetric("Insurance.CoverageCount", coverages.Count);
                    _appInsights.TrackBusinessEvent("Insurance.Created", new Dictionary<string, string>
                    {
                        ["InsuranceId"] = insurance.Id.ToString(),
                        ["TotalPremium"] = totalPremium.ToString()
                    });

                    // Send invoice generation message
                    await SendInvoiceMessageAsync(insurance.Id);

                    // Return the created insurance
                    var created = await GetByIdAsync(insurance.Id);
                    return created ?? throw new InvalidOperationException("Failed to retrieve created insurance");
                },
                new Dictionary<string, string>
                {
                    ["UserId"] = request.UserId.ToString(),
                    ["PolicyId"] = request.PolicyId.ToString(),
                    ["VehicleId"] = request.VehicleId.ToString(),
                    ["CoverageCount"] = request.CoverageIds.Count.ToString()
                });

            return Result<VehicleInsuranceDto>.Success(createdInsurance);
        }
        catch (InvalidOperationException ex) when (ex.Message == "CONFLICT")
        {
            return Result<VehicleInsuranceDto>.Conflict(
                "An active insurance policy already exists for this user and vehicle combination. " +
                "Please cancel the existing policy before creating a new one.");
        }
        catch (ArgumentException ex) when (ex.Message == "USER_NOT_FOUND")
        {
            return Result<VehicleInsuranceDto>.Failure("User not found.");
        }
        catch (ArgumentException ex) when (ex.Message == "POLICY_NOT_FOUND")
        {
            return Result<VehicleInsuranceDto>.ValidationError("Policy not found.");
        }
        catch (ArgumentException ex) when (ex.Message == "VEHICLE_NOT_FOUND")
        {
            return Result<VehicleInsuranceDto>.ValidationError("Vehicle not found.");
        }
        catch (ArgumentException ex) when (ex.Message.StartsWith("VALIDATION_ERROR:"))
        {
            var missingIds = ex.Message.Replace("VALIDATION_ERROR:", "");
            return Result<VehicleInsuranceDto>.ValidationError(
                $"The following coverage IDs do not exist: {missingIds}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating insurance for user: {UserId}", request.UserId);
            return Result<VehicleInsuranceDto>.Failure("An unexpected error occurred while creating the insurance policy.");
        }
    }
    private async Task SendInvoiceMessageAsync(Guid insuranceId)
    {
        await _appInsights.TrackOperationAsync(
            "InsuranceService.SendInvoiceMessage",
            async () =>
            {
                try
                {
                    var queueName = _configuration["SvbusInvoiceGenQueueName"];
                    if (string.IsNullOrEmpty(queueName))
                    {
                        _logger.LogWarning("ServiceBusQueueName not configured");
                        return;
                    }

                    var now = DateTime.UtcNow;
                    var endOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddMonths(1).AddTicks(-1);

                    var message = new InvoiceGenerationMessage
                    {
                        VehicleInsuranceId = insuranceId,
                        BillingPeriodStart = now,
                        BillingPeriodEnd = endOfMonth
                    };
                    await _appInsights.TrackExternalServiceAsync(
                        "ServiceBus",
                        "SendInvoiceMessage",
                        async () =>
                        {
                            await _messagingService.SendMessageAsync(message, queueName, "InvoiceGeneration");
                            return true;
                        });

                    _logger.LogInformation("Invoice generation message sent for insurance: {InsuranceId}", insuranceId);

                    _appInsights.TrackBusinessEvent("Invoice.MessageSent", new Dictionary<string, string>
                    {
                        ["InsuranceId"] = insuranceId.ToString(),
                        ["QueueName"] = queueName
                    });
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceCommunicationProblem)
                {
                    // Handle Service Bus connectivity issues gracefully (e.g., in test environments)
                    _logger.LogWarning(ex, "Failed to send invoice message for insurance: {InsuranceId} due to Service Bus connectivity issues. This is expected in test environments.", insuranceId);
                    // Don't throw - continue with insurance creation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invoice message for insurance: {InsuranceId}", insuranceId);
                    // Don't throw - continue with insurance creation as the invoice message is not critical for the core operation
                    _logger.LogWarning("Invoice message sending failed, but insurance creation will continue");
                }
            },
            new Dictionary<string, string> { ["InsuranceId"] = insuranceId.ToString() });
    }

    private IQueryable<VehicleInsurance> GetInsuranceWithIncludesAsync() =>
        _db.VehicleInsurances
            .Include(x => x.Vehicle)
            .Include(x => x.Policy)
                .ThenInclude(p => p.PolicyCoverages)
                    .ThenInclude(pc => pc.Coverage)
            .Include(x => x.VehicleInsuranceCoverages)
                .ThenInclude(vic => vic.Coverage)
            .Include(x => x.Invoices);
}
