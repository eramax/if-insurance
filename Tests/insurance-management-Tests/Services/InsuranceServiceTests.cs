using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using InsuranceManagement.Data;
using InsuranceManagement.Services;
using InsuranceManagement.Models;
using Shared.Models;
using Shared.Services;
using Microsoft.Data.Sqlite;

namespace insurance_management_Tests.Services;

public class InsuranceServiceTestsFixed : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Mock<IServiceBusMessagingService> _mockMessagingService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<InsuranceService>> _mockLogger;
    private readonly ApplicationInsightsService _appInsights;

    // Test data IDs - unique for each test run
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testUser2Id = Guid.NewGuid();
    private readonly Guid _testPolicyId = Guid.NewGuid();
    private readonly Guid _testVehicleId = Guid.NewGuid();
    private readonly Guid _testVehicle2Id = Guid.NewGuid();
    private readonly Guid _testCoverage1Id = Guid.NewGuid();
    private readonly Guid _testCoverage2Id = Guid.NewGuid();
    private readonly Guid _testVehicleInsuranceId = Guid.NewGuid();

    public InsuranceServiceTestsFixed()
    {
        // Create an in-memory SQLite database with a unique connection string
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _mockMessagingService = new Mock<IServiceBusMessagingService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<InsuranceService>>();
        _appInsights = new ApplicationInsightsService(null);
    }

    private InsuranceManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsuranceManagementDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new InsuranceManagementDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }    private async Task SeedTestDataAsync(InsuranceManagementDbContext context)
    {
        // First, add Users, Policies, Coverages, and Vehicles (independent entities)
        var user1 = new User
        {
            Id = _testUserId,
            PersonalId = Guid.NewGuid().ToString("N")[..8],
            Name = "Test User One",
            Email = $"testuser1{_testUserId:N}@example.com",
            PhoneNumber = "555-0001",
            Address = "123 Test St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = _testUser2Id,
            PersonalId = Guid.NewGuid().ToString("N")[..8],
            Name = "Test User Two",
            Email = $"testuser2{_testUser2Id:N}@example.com",
            PhoneNumber = "555-0002",
            Address = "456 Test Ave",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1985, 5, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var policy = new Policy
        {
            Id = _testPolicyId,
            Name = "Test Auto Policy",
            PolicyType = PolicyType.Vehicle,
            Description = "Test policy for vehicle insurance",
            InsuranceCompany = "Test Insurance Co",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage1 = new Coverage
        {
            Id = _testCoverage1Id,
            Name = "Test Liability Coverage",
            Tier = CoverageTier.Basic,
            Description = "Basic liability coverage for testing",
            Price = 100.00m,
            DurationInMonths = 12,
            Status = CoverageStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage2 = new Coverage
        {
            Id = _testCoverage2Id,
            Name = "Test Collision Coverage",
            Tier = CoverageTier.Complementary,
            Description = "Collision coverage for testing",
            Price = 200.00m,
            DurationInMonths = 12,
            Status = CoverageStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var vehicle1 = new Vehicle
        {
            Id = _testVehicleId,
            LicensePlate = $"TEST{_testVehicleId.ToString("N")[..6].ToUpper()}",
            Make = "Toyota",
            Model = "Camry",
            Year = 2020,
            Vin = $"TEST{_testVehicleId.ToString("N")[..13].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var vehicle2 = new Vehicle
        {
            Id = _testVehicle2Id,
            LicensePlate = $"TST2{_testVehicle2Id.ToString("N")[..6].ToUpper()}",
            Make = "Honda",
            Model = "Civic",
            Year = 2019,
            Vin = $"TST2{_testVehicle2Id.ToString("N")[..13].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add independent entities first
        await context.Users.AddRangeAsync(user1, user2);
        await context.Policies.AddAsync(policy);
        await context.Coverages.AddRangeAsync(coverage1, coverage2);
        await context.Vehicles.AddRangeAsync(vehicle1, vehicle2);
        await context.SaveChangesAsync();

        // Now add VehicleInsurance that depends on the entities above
        var insurance = new VehicleInsurance
        {
            Id = _testVehicleInsuranceId,
            UserId = _testUserId,
            PolicyId = _testPolicyId,
            VehicleId = _testVehicleId,
            Status = InsuranceStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(12),
            RenewalDate = DateTime.UtcNow.AddMonths(11),
            TotalPremium = 300.00m,
            Deductible = 500.00m,
            Notes = "Test insurance policy",
            PolicyNumber = "TEST001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.VehicleInsurances.AddAsync(insurance);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsInsurance()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        // Act
        var result = await service.GetByIdAsync(_testVehicleInsuranceId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_testVehicleInsuranceId);
        result.Status.Should().Be("Active");
        result.TotalPremium.Should().Be(300.00m);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);
        var invalidId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPersonalIdAsync_WithValidPersonalId_ReturnsUserDetails()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var user = await context.Users.FindAsync(_testUserId);
        var personalId = user!.PersonalId;
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        // Act
        var result = await service.GetByPersonalIdAsync(personalId);

        // Assert
        result.Should().NotBeNull();
        result!.User.Name.Should().Be("Test User One");
        result.User.PersonalId.Should().Be(personalId);
        result.Insurances.Should().HaveCount(1);
        result.Insurances.First().Id.Should().Be(_testVehicleInsuranceId);
    }

    [Fact]
    public async Task GetByPersonalIdAsync_WithInvalidPersonalId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);
        var invalidPersonalId = "99999999";

        // Act
        var result = await service.GetByPersonalIdAsync(invalidPersonalId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);        var request = new CreateInsuranceRequest(
            UserId: _testUser2Id,
            VehicleId: _testVehicle2Id,
            PolicyId: _testPolicyId,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { _testCoverage1Id }
        );

        // Act
        var result = await service.CreateAsync(request);        // Assert
        result.Should().NotBeNull();
        if (!result!.IsSuccess)
        {
            // Debug: Output the error message to understand what went wrong
            throw new Exception($"Expected success but got error: {result.ErrorMessage}");
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalPremium.Should().Be(100.00m);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidUserId_ReturnsFailure()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        var request = new CreateInsuranceRequest(
            UserId: Guid.NewGuid(), // Invalid user ID
            VehicleId: Guid.NewGuid(),
            PolicyId: _testPolicyId,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { _testCoverage1Id }
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    [Fact]
    public async Task CreateAsync_WithMultipleCoverages_CalculatesTotalPremiumCorrectly()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);        var request = new CreateInsuranceRequest(
            UserId: _testUser2Id,
            VehicleId: _testVehicle2Id,
            PolicyId: _testPolicyId,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { _testCoverage1Id, _testCoverage2Id }
        );

        // Act
        var result = await service.CreateAsync(request);        // Assert
        result.Should().NotBeNull();
        if (!result!.IsSuccess)
        {
            // Debug: Output the error message to understand what went wrong
            throw new Exception($"Expected success but got error: {result.ErrorMessage}");
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalPremium.Should().Be(300.00m); // 100 + 200
    }

    [Fact]
    public async Task CreateAsync_WithConflictingInsurance_ReturnsConflict()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        var request = new CreateInsuranceRequest(
            UserId: _testUserId, // User already has insurance for this vehicle
            VehicleId: _testVehicleId,
            PolicyId: _testPolicyId,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { _testCoverage1Id }
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCoverageIds_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        var request = new CreateInsuranceRequest(
            UserId: _testUser2Id,
            VehicleId: Guid.NewGuid(),
            PolicyId: _testPolicyId,
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { Guid.NewGuid() } // Invalid coverage ID
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidPolicyId_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateContext();
        await SeedTestDataAsync(context);
        var service = new InsuranceService(context, _mockMessagingService.Object, _mockConfiguration.Object, _mockLogger.Object, _appInsights);

        var request = new CreateInsuranceRequest(
            UserId: _testUser2Id,
            VehicleId: Guid.NewGuid(),
            PolicyId: Guid.NewGuid(), // Invalid policy ID
            StartDate: DateTime.Today,
            EndDate: DateTime.Today.AddMonths(12),
            RenewalDate: DateTime.Today.AddMonths(11),
            CoverageIds: new List<Guid> { _testCoverage1Id }
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Policy not found");
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
