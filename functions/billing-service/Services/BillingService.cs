using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;
using InsuranceManagementSystem.Functions.BillingService.Data;
using InsuranceManagementSystem.Functions.BillingService.Config;
using Shared.Services;
using Azure.Storage.Blobs;

namespace InsuranceManagementSystem.Functions.BillingService.Services;

/// <summary>
/// Interface for billing service operations
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Process all active insurances for billing
    /// </summary>
    Task<List<EmailInvoiceNotificationMessage>> ProcessAllInsurancesAsync();

    /// <summary>
    /// Process a single insurance for billing
    /// </summary>
    Task<EmailInvoiceNotificationMessage> ProcessSingleInsuranceAsync(InvoiceGenerationMessage message);
}

/// <summary>
/// Implementation of billing service
/// </summary>
public class BillingService : IBillingService
{
    private readonly ILogger<BillingService> _logger;
    private readonly InsuranceDbContext _dbContext;
    private readonly TelemetryClient _telemetryClient;
    private readonly ApplicationInsightsService _appInsightsService;
    private readonly IInvoiceGenerator _invoiceGenerator;
    private readonly AppConfig _appConfig;
    private readonly BlobServiceClient _blobServiceClient;

    public BillingService(
        ILogger<BillingService> logger,
        InsuranceDbContext dbContext,
        TelemetryClient telemetryClient,
        ApplicationInsightsService appInsightsService,
        IInvoiceGenerator invoiceGenerator,
        AppConfig appConfig,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _dbContext = dbContext;
        _telemetryClient = telemetryClient;
        _appInsightsService = appInsightsService;
        _invoiceGenerator = invoiceGenerator;
        _appConfig = appConfig;
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Process all active insurances for billing
    /// </summary>
    public async Task<List<EmailInvoiceNotificationMessage>> ProcessAllInsurancesAsync()
    {
        using var operation = _telemetryClient.StartOperation<Microsoft.ApplicationInsights.DataContracts.RequestTelemetry>("ProcessAllInsurances");

        try
        {
            _logger.LogInformation("Starting to process all active insurances for billing at {Time}", DateTime.UtcNow);
            _telemetryClient.TrackEvent("BillingServiceStarted");

            var dateRange = CalculateInvoiceDateRange();
            _logger.LogInformation("Calculated invoice date range: {StartDate} to {EndDate}",
                dateRange.StartDate, dateRange.EndDate);

            // Get all active insurances requiring invoices
            var activeInsurances = await GetActiveInsurancesWithCoveragesAsync(dateRange);

            if (!activeInsurances.Any())
            {
                _logger.LogInformation("No active insurances found requiring an invoice.");
                _telemetryClient.TrackEvent("NoInvoicesRequired");
                return new List<EmailInvoiceNotificationMessage>();
            }

            _logger.LogInformation("Found {Count} active insurances to process.", activeInsurances.Count);
            _telemetryClient.TrackMetric("ActiveInsurancesFound", activeInsurances.Count);

            // Generate invoices for each insurance
            var notificationMessages = new List<EmailInvoiceNotificationMessage>();

            foreach (var insuranceData in activeInsurances)
            {
                try
                {
                    // Generate invoice and notification
                    var notification = await _invoiceGenerator.GenerateInvoiceAsync(
                        insuranceData.VehicleInsuranceId,
                        insuranceData.StartDate,
                        insuranceData.EndDate,
                        insuranceData.TotalAmount);

                    if (notification != null)
                    {
                        notificationMessages.Add(notification);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating invoice for insurance {InsuranceId}",
                        insuranceData.VehicleInsuranceId);
                    _telemetryClient.TrackException(ex);
                }
            }

            _logger.LogInformation("Completed processing {Count} invoices, generated {NotificationCount} notifications",
                activeInsurances.Count, notificationMessages.Count);
            _telemetryClient.TrackEvent("BillingServiceCompleted", new Dictionary<string, string>
            {
                { "InvoicesProcessed", activeInsurances.Count.ToString() },
                { "NotificationsGenerated", notificationMessages.Count.ToString() }
            });

            return notificationMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing all insurances for billing");
            _telemetryClient.TrackException(ex);
            throw;
        }
    }

    /// <summary>
    /// Process a single insurance for billing
    /// </summary>
    public async Task<EmailInvoiceNotificationMessage> ProcessSingleInsuranceAsync(InvoiceGenerationMessage message)
    {
        using var operation = _telemetryClient.StartOperation<Microsoft.ApplicationInsights.DataContracts.RequestTelemetry>("ProcessSingleInsurance");

        try
        {
            _logger.LogInformation("Processing single insurance {InsuranceId} for billing at {Time}",
                message.VehicleInsuranceId, DateTime.UtcNow);

            // Set billing period
            var startDate = message.BillingPeriodStart ?? DateTime.UtcNow.Date;
            var endDate = message.BillingPeriodEnd ?? GetMonthEndDate();

            _logger.LogInformation("Using billing period: {StartDate} to {EndDate}", startDate, endDate);

            // Calculate prorated amount based on days remaining in the month
            var amount = await CalculateProRatedAmountAsync(message.VehicleInsuranceId, startDate, endDate);

            if (amount <= 0)
            {
                _logger.LogWarning("Calculated amount for insurance {InsuranceId} is zero or negative. Skipping.",
                    message.VehicleInsuranceId);
                return null;
            }

            // Generate invoice and notification
            var notification = await _invoiceGenerator.GenerateInvoiceAsync(
                message.VehicleInsuranceId,
                startDate,
                endDate,
                amount);

            if (notification != null)
            {
                _logger.LogInformation("Successfully generated invoice for insurance {InsuranceId}",
                    message.VehicleInsuranceId);
            }
            else
            {
                _logger.LogWarning("Failed to generate invoice for insurance {InsuranceId}",
                    message.VehicleInsuranceId);
            }

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing insurance {InsuranceId} for billing",
                message.VehicleInsuranceId);
            _telemetryClient.TrackException(ex);
            throw;
        }
    }

    /// <summary>
    /// Calculate the invoice date range for the next month
    /// </summary>
    private (DateTime StartDate, DateTime EndDate) CalculateInvoiceDateRange()
    {
        var currentDate = DateTime.UtcNow.Date;
        var firstDayOfUpcomingMonth = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(1);
        var lastDayOfUpcomingMonth = firstDayOfUpcomingMonth.AddMonths(1).AddDays(-1);

        return (firstDayOfUpcomingMonth, lastDayOfUpcomingMonth);
    }

    /// <summary>
    /// Get the end date of the current month
    /// </summary>
    private DateTime GetMonthEndDate()
    {
        var currentDate = DateTime.UtcNow.Date;
        return new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
    }

    /// <summary>
    /// Calculate a pro-rated amount based on days remaining in the month
    /// </summary>
    private async Task<decimal> CalculateProRatedAmountAsync(Guid vehicleInsuranceId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Get the insurance and its coverages
            var insurance = await _dbContext.VehicleInsurances
                .FirstOrDefaultAsync(i => i.Id == vehicleInsuranceId);

            if (insurance == null)
            {
                _logger.LogWarning("Insurance {InsuranceId} not found", vehicleInsuranceId);
                return 0;
            }

            // Get all active coverage links for this insurance
            var activeCoverageLinks = await _dbContext.VehicleInsuranceCoverages
                .Where(ic => ic.VehicleInsuranceId == vehicleInsuranceId &&
                          ic.Status == VehicleInsuranceCoverageStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            if (!activeCoverageLinks.Any())
            {
                _logger.LogWarning("No active coverages found for insurance {InsuranceId}", vehicleInsuranceId);
                return 0;
            }

            // Get coverage details
            var coverageIds = activeCoverageLinks.Select(ic => ic.CoverageId).ToList();
            var coverages = await _dbContext.Coverages
                .Where(c => coverageIds.Contains(c.Id) && c.Status == CoverageStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            if (!coverages.Any())
            {
                _logger.LogWarning("No active coverage details found for insurance {InsuranceId}", vehicleInsuranceId);
                return 0;
            }

            // Calculate total monthly cost
            var totalMonthlyCost = coverages.Sum(c => c.Price);

            // Calculate days in current month and days remaining
            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            var daysRemaining = (endDate - startDate).Days + 1; // +1 to include both start and end date

            // Calculate pro-rated amount
            var proRatedAmount = totalMonthlyCost * daysRemaining / daysInMonth;

            _logger.LogInformation("Calculated pro-rated amount for insurance {InsuranceId}: {Amount} " +
                                "({DaysRemaining}/{DaysInMonth} days)",
                                vehicleInsuranceId, proRatedAmount, daysRemaining, daysInMonth);

            return Math.Round(proRatedAmount, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pro-rated amount for insurance {InsuranceId}", vehicleInsuranceId);
            throw;
        }
    }

    /// <summary>
    /// Get active insurances with coverage amounts for the specified date range
    /// </summary>
    private async Task<List<(Guid VehicleInsuranceId, DateTime StartDate, DateTime EndDate, decimal TotalAmount)>>
        GetActiveInsurancesWithCoveragesAsync((DateTime StartDate, DateTime EndDate) dateRange)
    {
        try
        {
            _logger.LogInformation("Retrieving active insurances requiring invoices for date range: {StartDate} to {EndDate}",
                dateRange.StartDate, dateRange.EndDate);

            // Get active vehicle insurances
            var activeInsurances = await _dbContext.VehicleInsurances
                .Where(vi => vi.Status == InsuranceStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active insurances for evaluation", activeInsurances.Count);

            var result = new List<(Guid VehicleInsuranceId, DateTime StartDate, DateTime EndDate, decimal TotalAmount)>();

            // Process each insurance
            foreach (var insurance in activeInsurances)
            {
                // Calculate effective date range
                var calculatedStartDate = insurance.StartDate > dateRange.StartDate ? insurance.StartDate : dateRange.StartDate;
                var calculatedEndDate = insurance.EndDate < dateRange.EndDate ? insurance.EndDate : dateRange.EndDate;

                // Skip if dates don't make sense
                if (calculatedStartDate > calculatedEndDate)
                {
                    continue;
                }

                // Get coverages and calculate total amount
                var coverageAmount = await GetCoverageAmountForInsuranceAsync(insurance.Id);

                if (coverageAmount > 0)
                {
                    result.Add((
                        insurance.Id,
                        calculatedStartDate,
                        calculatedEndDate,
                        coverageAmount
                    ));
                }
            }

            _logger.LogInformation("Identified {Count} active insurances requiring invoicing", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active insurances with coverage amounts");
            throw;
        }
    }

    /// <summary>
    /// Get total coverage amount for an insurance
    /// </summary>
    private async Task<decimal> GetCoverageAmountForInsuranceAsync(Guid insuranceId)
    {
        try
        {
            // Get active coverage links
            var activeCoverageLinks = await _dbContext.VehicleInsuranceCoverages
                .Where(ic => ic.VehicleInsuranceId == insuranceId &&
                          ic.Status == VehicleInsuranceCoverageStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            if (!activeCoverageLinks.Any())
            {
                return 0;
            }

            // Get coverages
            var coverageIds = activeCoverageLinks.Select(ic => ic.CoverageId).ToList();
            var coverages = await _dbContext.Coverages
                .Where(c => coverageIds.Contains(c.Id) && c.Status == CoverageStatus.Active)
                .AsNoTracking()
                .ToListAsync();

            // Calculate total amount
            return coverages.Sum(c => c.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coverage amount for insurance {InsuranceId}", insuranceId);
            return 0;
        }
    }
}
