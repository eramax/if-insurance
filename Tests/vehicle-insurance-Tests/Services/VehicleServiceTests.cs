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
/// Unit tests for VehicleService ensuring proper CRUD operations and business logic
/// </summary>
public class VehicleServiceTests : IDisposable
{
    private readonly VehicleDbContext _context;
    private readonly Mock<IServiceBusMessagingService> _mockMessagingService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<VehicleService>> _mockLogger;
    private readonly ApplicationInsightsService _appInsightsService;
    private readonly VehicleService _vehicleService;

    public VehicleServiceTests()
    {
        // Setup in-memory database for testing
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
    public async Task GetByIdAsync_WithValidId_ShouldReturnVehicle()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateTestVehicle(vehicleId);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _vehicleService.GetByIdAsync(vehicleId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(vehicleId);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Camry");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _vehicleService.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleVehicles_ShouldReturnAllVehicles()
    {
        // Arrange
        var vehicle1 = CreateTestVehicle(Guid.NewGuid(), "Honda", "Civic");
        var vehicle2 = CreateTestVehicle(Guid.NewGuid(), "Ford", "Focus");
        await _context.Vehicles.AddRangeAsync(vehicle1, vehicle2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _vehicleService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Count().Should().Be(2);
        result.Should().Contain(v => v.Make == "Honda" && v.Model == "Civic");
        result.Should().Contain(v => v.Make == "Ford" && v.Model == "Focus");
    }

    [Fact]
    public async Task GetAllAsync_WithNoVehicles_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _vehicleService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidVehicle_ShouldCreateAndReturnVehicle()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            LicensePlate = "ABC123",
            Make = "Tesla",
            Model = "Model 3",
            Year = 2023,
            Vin = "1HGBH41JXMN109186"
        };

        // Act
        var result = await _vehicleService.CreateAsync(vehicle);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Make.Should().Be("Tesla");
        result.Model.Should().Be("Model 3");

        // Verify it was saved to database
        var savedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == result.Id);
        savedVehicle.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidIdAndData_ShouldUpdateAndReturnVehicle()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var originalVehicle = CreateTestVehicle(vehicleId);
        await _context.Vehicles.AddAsync(originalVehicle);
        await _context.SaveChangesAsync();

        var updatedVehicle = new Vehicle
        {
            LicensePlate = "XYZ789",
            Make = "BMW",
            Model = "X5",
            Year = 2024,
            Vin = "WBXPC9C50WP042386"
        };

        // Act
        var result = await _vehicleService.UpdateAsync(vehicleId, updatedVehicle);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(vehicleId);
        result.Make.Should().Be("BMW");
        result.Model.Should().Be("X5");
        result.Year.Should().Be(2024);
        result.LicensePlate.Should().Be("XYZ789");
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updatedVehicle = new Vehicle
        {
            LicensePlate = "XYZ789",
            Make = "BMW",
            Model = "X5",
            Year = 2024,
            Vin = "WBXPC9C50WP042386"
        };

        // Act
        var result = await _vehicleService.UpdateAsync(nonExistentId, updatedVehicle);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = CreateTestVehicle(vehicleId);
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        // Act
        var result = await _vehicleService.DeleteAsync(vehicleId);

        // Assert
        result.Should().BeTrue();

        // Verify vehicle was deleted from database
        var deletedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        deletedVehicle.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _vehicleService.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Helper method to create a test vehicle with default values
    /// </summary>
    private static Vehicle CreateTestVehicle(Guid id, string make = "Toyota", string model = "Camry")
    {
        return new Vehicle
        {
            Id = id,
            LicensePlate = "TEST123",
            Make = make,
            Model = model,
            Year = 2022,
            Vin = "1HGBH41JXMN109186",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
