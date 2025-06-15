using Microsoft.EntityFrameworkCore;
using Shared.Models;
using VehicleInsurance.Data;
using Xunit;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace VehicleInsurance.Tests.Data;

/// <summary>
/// Tests for VehicleDbContext to ensure proper data access and constraints
/// </summary>
public class VehicleDbContextTests : IDisposable
{
    private readonly VehicleDbContext _context;
    private readonly SqliteConnection _connection;

    public VehicleDbContextTests()
    {
        // Use SQLite in-memory database to properly test constraints
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<VehicleDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new VehicleDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveVehicle_WithValidData_ShouldSucceed()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = "TEST123",
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            Vin = "1HGBH41JXMN109186",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        // Assert
        var savedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        savedVehicle.Should().NotBeNull();
        savedVehicle!.Make.Should().Be("Toyota");
        savedVehicle.Model.Should().Be("Camry");
    }
    [Fact]
    public async Task SaveVehicle_WithDuplicateLicensePlate_ShouldThrowException()
    {
        // Arrange
        var vehicle1 = CreateTestVehicle("DUPLICATE");
        var vehicle2 = CreateTestVehicle("DUPLICATE"); // Same license plate

        _context.Vehicles.Add(vehicle1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Vehicles.Add(vehicle2);
        var action = async () => await _context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }
    [Fact]
    public async Task SaveVehicle_WithDuplicateVin_ShouldThrowException()
    {
        // Arrange
        var vin = "1HGBH41JXMN109186";
        var vehicle1 = CreateTestVehicle("PLATE1", vin);
        var vehicle2 = CreateTestVehicle("PLATE2", vin); // Same VIN

        _context.Vehicles.Add(vehicle1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Vehicles.Add(vehicle2);
        var action = async () => await _context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task QueryVehicles_ByMakeAndModel_ShouldReturnCorrectResults()
    {
        // Arrange
        var toyota1 = CreateTestVehicle("TOY1", "1HGBH41JXMN109186", "Toyota", "Camry");
        var toyota2 = CreateTestVehicle("TOY2", "1HGBH41JXMN109187", "Toyota", "Corolla");
        var honda = CreateTestVehicle("HON1", "1HGBH41JXMN109188", "Honda", "Civic");

        _context.Vehicles.AddRange(toyota1, toyota2, honda);
        await _context.SaveChangesAsync();

        // Act
        var toyotas = await _context.Vehicles
            .Where(v => v.Make == "Toyota")
            .ToListAsync();

        var camrys = await _context.Vehicles
            .Where(v => v.Make == "Toyota" && v.Model == "Camry")
            .ToListAsync();

        // Assert
        toyotas.Should().HaveCount(2);
        camrys.Should().HaveCount(1);
        camrys.First().Model.Should().Be("Camry");
    }

    [Fact]
    public async Task UpdateVehicle_ShouldUpdateTimestamp()
    {
        // Arrange
        var vehicle = CreateTestVehicle("UPD123");
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = vehicle.UpdatedAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(10);

        // Act
        vehicle.Make = "UpdatedMake";
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updatedVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        updatedVehicle.Should().NotBeNull();
        updatedVehicle!.Make.Should().Be("UpdatedMake");
        updatedVehicle.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    private static Vehicle CreateTestVehicle(string licensePlate, string vin = "1HGBH41JXMN109186", string make = "Toyota", string model = "Camry")
    {
        return new Vehicle
        {
            Id = Guid.NewGuid(),
            LicensePlate = licensePlate,
            Make = make,
            Model = model,
            Year = 2022,
            Vin = vin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
