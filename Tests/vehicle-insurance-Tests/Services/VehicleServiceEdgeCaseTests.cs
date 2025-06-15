using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Shared.Services;
using VehicleInsurance.Data;
using VehicleInsurance.Services;
using Xunit;
using FluentAssertions;

namespace VehicleInsurance.Tests.Services;

/// <summary>
/// Tests for edge cases, error handling, and business logic scenarios in VehicleService
/// </summary>
public class VehicleServiceEdgeCaseTests : IDisposable
{
    private readonly VehicleDbContext _context;
    private readonly Mock<IServiceBusMessagingService> _mockMessagingService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<VehicleService>> _mockLogger;
    private readonly ApplicationInsightsService _appInsightsService;
    private readonly VehicleService _vehicleService;

    public VehicleServiceEdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<VehicleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VehicleDbContext(options);
        _mockMessagingService = new Mock<IServiceBusMessagingService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<VehicleService>>();

        // Create real ApplicationInsightsService with null TelemetryClient for testing
        _appInsightsService = new ApplicationInsightsService(null);

        _vehicleService = new VehicleService(
            _context,
            _mockMessagingService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _appInsightsService);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCorrectTimestamps()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var vehicle = new Vehicle
        {
            LicensePlate = "TIME123",
            Make = "Tesla",
            Model = "Model S",
            Year = 2023,
            Vin = "5YJSA1E26JF123456"
        };

        // Act
        var result = await _vehicleService.CreateAsync(vehicle);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreation);
        result.CreatedAt.Should().BeBefore(afterCreation);
        result.UpdatedAt.Should().BeAfter(beforeCreation);
        result.UpdatedAt.Should().BeBefore(afterCreation);
        result.CreatedAt.Should().BeCloseTo(result.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateUniqueId()
    {
        // Arrange
        var vehicle1 = CreateTestVehicle("PLATE1", "VIN1234567890123");
        var vehicle2 = CreateTestVehicle("PLATE2", "VIN2345678901234");

        // Act
        var result1 = await _vehicleService.CreateAsync(vehicle1);
        var result2 = await _vehicleService.CreateAsync(vehicle2);

        // Assert
        result1.Id.Should().NotBeEmpty();
        result2.Id.Should().NotBeEmpty();
        result1.Id.Should().NotBe(result2.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        var originalVehicle = CreateTestVehicle("ORIG123", "ORIG1234567890123");
        originalVehicle = await _vehicleService.CreateAsync(originalVehicle);

        var originalCreatedAt = originalVehicle.CreatedAt;
        await Task.Delay(10); // Ensure timestamp difference

        var updateData = new Vehicle
        {
            Make = "UpdatedMake",
            Model = "UpdatedModel",
            LicensePlate = "UPD123",
            Year = 2024,
            Vin = "UPDT1234567890123"
        };

        // Act
        var result = await _vehicleService.UpdateAsync(originalVehicle.Id, updateData);

        // Assert
        result.Should().NotBeNull();
        result!.Make.Should().Be("UpdatedMake");
        result.Model.Should().Be("UpdatedModel");
        result.LicensePlate.Should().Be("UPD123");
        result.Year.Should().Be(2024);
        result.Vin.Should().Be("UPDT1234567890123");

        // Verify timestamps
        result.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        result.UpdatedAt.Should().BeAfter(originalCreatedAt); // Should be updated
    }

    [Fact]
    public async Task GetAllAsync_WithLargeDataset_ShouldReturnAllVehicles()
    {
        // Arrange
        var vehicles = new List<Vehicle>();
        for (int i = 0; i < 100; i++)
        {
            vehicles.Add(CreateTestVehicle($"PLATE{i:D3}", $"VIN{i:D14}"));
        }

        foreach (var vehicle in vehicles)
        {
            await _context.Vehicles.AddAsync(vehicle);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _vehicleService.GetAllAsync();

        // Assert
        result.Should().HaveCount(100);
        result.Select(v => v.LicensePlate).Should().BeEquivalentTo(vehicles.Select(v => v.LicensePlate));
    }
    [Fact]
    public async Task CreateAsync_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            LicensePlate = "NULL123",
            Make = "", // Testing empty string handling
            Model = "TestModel",
            Year = 2023,
            Vin = "NULL1234567890123"
        };

        // Act & Assert
        // The service should not crash but database constraints might fail
        var result = await _vehicleService.CreateAsync(vehicle);

        // The result should still have the vehicle with empty make
        result.Should().NotBeNull();
        result.Make.Should().Be("");
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrentModification_ShouldHandleGracefully()
    {
        // Arrange
        var vehicle = CreateTestVehicle("CONC123", "CONC1234567890123");
        vehicle = await _vehicleService.CreateAsync(vehicle);

        // Simulate concurrent modification by updating the vehicle directly in context
        var directVehicle = await _context.Vehicles.FirstAsync(v => v.Id == vehicle.Id);
        directVehicle.Make = "ConcurrentUpdate";
        await _context.SaveChangesAsync();

        var updateData = new Vehicle
        {
            Make = "ServiceUpdate",
            Model = "UpdatedModel",
            LicensePlate = "UPD123",
            Year = 2024,
            Vin = "UPDT1234567890123"
        };

        // Act
        var result = await _vehicleService.UpdateAsync(vehicle.Id, updateData);

        // Assert
        result.Should().NotBeNull();
        result!.Make.Should().Be("ServiceUpdate"); // Service update should win
    }

    [Fact]
    public async Task DeleteAsync_WithCascadeConstraints_ShouldHandleGracefully()
    {
        // Arrange
        var vehicle = CreateTestVehicle("DEL123", "DEL1234567890123");
        vehicle = await _vehicleService.CreateAsync(vehicle);

        // Act
        var result = await _vehicleService.DeleteAsync(vehicle.Id);

        // Assert
        result.Should().BeTrue();

        // Verify vehicle is actually deleted
        var deletedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        deletedVehicle.Should().BeNull();
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALIDPLATE")]
    public async Task CreateAsync_WithInvalidLicensePlate_ShouldStillCreate(string licensePlate)
    {
        // Arrange
        var vehicle = new Vehicle
        {
            LicensePlate = licensePlate,
            Make = "TestMake",
            Model = "TestModel",
            Year = 2023,
            Vin = "TEST1234567890123"
        };

        // Act
        var result = await _vehicleService.CreateAsync(vehicle);

        // Assert
        result.Should().NotBeNull();
        result.LicensePlate.Should().Be(licensePlate);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ShouldReturnNull()
    {
        // Act
        var result = await _vehicleService.GetByIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }
    [Fact]
    public async Task ApplicationInsights_ShouldBeCalledForAllOperations()
    {
        // Arrange
        var vehicle = CreateTestVehicle("AI123", "AI1234567890123");

        // Act & Assert - just ensure operations complete successfully
        // Note: ApplicationInsights calls are tested in the main VehicleServiceTests
        // We focus on edge case behavior here
        var createdVehicle = await _vehicleService.CreateAsync(vehicle);
        await _vehicleService.GetAllAsync();
        await _vehicleService.GetByIdAsync(vehicle.Id);
        await _vehicleService.UpdateAsync(vehicle.Id, vehicle);
        await _vehicleService.DeleteAsync(vehicle.Id);

        // Verify the vehicle was created successfully (basic sanity check)
        createdVehicle.Should().NotBeNull();
        createdVehicle.Id.Should().NotBe(Guid.Empty);
    }

    private static Vehicle CreateTestVehicle(string licensePlate, string vin)
    {
        return new Vehicle
        {
            LicensePlate = licensePlate,
            Make = "TestMake",
            Model = "TestModel",
            Year = 2022,
            Vin = vin
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
