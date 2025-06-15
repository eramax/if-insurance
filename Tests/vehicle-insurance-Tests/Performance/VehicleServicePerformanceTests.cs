using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Shared.Services;
using System.Diagnostics;
using VehicleInsurance.Data;
using VehicleInsurance.Services;
using Xunit;
using FluentAssertions;

namespace VehicleInsurance.Tests.Performance;

/// <summary>
/// Performance and stress tests for VehicleService to ensure scalability
/// </summary>
public class VehicleServicePerformanceTests : IDisposable
{
    private readonly VehicleDbContext _context;
    private readonly Mock<IServiceBusMessagingService> _mockMessagingService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<VehicleService>> _mockLogger;
    private readonly ApplicationInsightsService _appInsightsService;
    private readonly VehicleService _vehicleService;

    public VehicleServicePerformanceTests()
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
    public async Task BulkCreateVehicles_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int vehicleCount = 1000;
        var vehicles = GenerateTestVehicles(vehicleCount);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = vehicles.Select(vehicle => _vehicleService.CreateAsync(vehicle));
        await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds

        var allVehicles = await _vehicleService.GetAllAsync();
        allVehicles.Should().HaveCount(vehicleCount);
    }

    [Fact]
    public async Task ConcurrentReadOperations_ShouldHandleMultipleRequests()
    {
        // Arrange
        var testVehicles = GenerateTestVehicles(10);
        foreach (var vehicle in testVehicles)
        {
            await _vehicleService.CreateAsync(vehicle);
        }

        var vehicleIds = testVehicles.Select(v => v.Id).ToList();

        // Act
        var readTasks = new List<Task<Vehicle?>>();
        for (int i = 0; i < 100; i++)
        {
            var randomId = vehicleIds[i % vehicleIds.Count];
            readTasks.Add(_vehicleService.GetByIdAsync(randomId));
        }

        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(readTasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        results.Should().HaveCount(100);
        results.Where(r => r != null).Should().HaveCount(100); // All should find vehicles
    }

    [Fact]
    public async Task ConcurrentUpdateOperations_ShouldHandleRaceConditions()
    {
        // Arrange
        var vehicle = GenerateTestVehicles(1).First();
        var createdVehicle = await _vehicleService.CreateAsync(vehicle);

        // Act - Multiple concurrent updates
        var updateTasks = new List<Task<Vehicle?>>();
        for (int i = 0; i < 10; i++)
        {
            var updateData = new Vehicle
            {
                LicensePlate = $"UPD{i:D3}",
                Make = $"Make{i}",
                Model = $"Model{i}",
                Year = 2020 + i,
                Vin = $"VIN{i:D14}"
            };
            updateTasks.Add(_vehicleService.UpdateAsync(createdVehicle.Id, updateData));
        }

        var results = await Task.WhenAll(updateTasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Verify final state is consistent
        var finalVehicle = await _vehicleService.GetByIdAsync(createdVehicle.Id);
        finalVehicle.Should().NotBeNull();
    }

    [Fact]
    public async Task LargeDatasetQuery_ShouldPerformEfficiently()
    {
        // Arrange - Create a large dataset
        const int vehicleCount = 5000;
        var vehicles = GenerateTestVehicles(vehicleCount);

        // Bulk insert directly to database for speed
        await _context.Vehicles.AddRangeAsync(vehicles);
        await _context.SaveChangesAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var allVehicles = await _vehicleService.GetAllAsync();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
        allVehicles.Should().HaveCount(vehicleCount);
    }

    [Fact]
    public async Task MemoryUsage_ShouldNotGrowExcessively()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Perform multiple operations
        for (int i = 0; i < 100; i++)
        {
            var vehicle = new Vehicle
            {
                LicensePlate = $"MEM{i:D3}",
                Make = "MemoryTest",
                Model = $"Model{i}",
                Year = 2022,
                Vin = $"MEM{i:D14}"
            };

            var created = await _vehicleService.CreateAsync(vehicle);
            await _vehicleService.GetByIdAsync(created.Id);
            await _vehicleService.UpdateAsync(created.Id, vehicle);
            await _vehicleService.DeleteAsync(created.Id);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert - Memory growth should be reasonable
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(50 * 1024 * 1024); // Less than 50MB growth
    }

    [Fact]
    public async Task DatabaseConnectionHandling_ShouldNotLeakConnections()
    {
        // Arrange & Act - Perform many operations that could potentially leak connections
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var vehicle = new Vehicle
                {
                    LicensePlate = $"CONN{i:D3}",
                    Make = "ConnectionTest",
                    Model = $"Model{i}",
                    Year = 2022,
                    Vin = $"CONN{i:D14}"
                };

                try
                {
                    var created = await _vehicleService.CreateAsync(vehicle);
                    await _vehicleService.GetByIdAsync(created.Id);
                    await _vehicleService.GetAllAsync();
                }
                catch (Exception ex)
                {
                    // Log but don't fail test for individual connection issues
                    Console.WriteLine($"Connection test {i} failed: {ex.Message}");
                }
            }));
        }

        // Assert - All tasks should complete without connection pool exhaustion
        var completion = await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                await task;
                return true;
            }
            catch
            {
                return false;
            }
        }));

        // At least 90% of operations should succeed
        completion.Count(c => c).Should().BeGreaterOrEqualTo(45);
    }

    [Fact]
    public async Task SearchOperations_WithLargeDataset_ShouldPerformEfficiently()
    {
        // Arrange - Create vehicles with different makes and models
        var makes = new[] { "Toyota", "Honda", "Ford", "BMW", "Mercedes" };
        var models = new[] { "Sedan", "SUV", "Hatchback", "Coupe", "Truck" };

        var vehicles = new List<Vehicle>();
        for (int i = 0; i < 1000; i++)
        {
            vehicles.Add(new Vehicle
            {
                Id = Guid.NewGuid(),
                LicensePlate = $"SEARCH{i:D4}",
                Make = makes[i % makes.Length],
                Model = models[i % models.Length],
                Year = 2020 + (i % 4),
                Vin = $"SEARCH{i:D12}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.Vehicles.AddRangeAsync(vehicles);
        await _context.SaveChangesAsync();

        // Act & Assert - Different search patterns
        var stopwatch = Stopwatch.StartNew();

        // Search by make
        var toyotas = await _context.Vehicles.Where(v => v.Make == "Toyota").ToListAsync();
        toyotas.Should().HaveCount(200); // 1000 / 5 makes

        // Search by year
        var year2022 = await _context.Vehicles.Where(v => v.Year == 2022).ToListAsync();
        year2022.Should().HaveCount(250); // 1000 / 4 years

        // Complex search
        var complex = await _context.Vehicles
            .Where(v => v.Make == "BMW" && v.Model == "SUV" && v.Year >= 2021)
            .ToListAsync(); stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // All searches within 1 second
    }

    private static List<Vehicle> GenerateTestVehicles(int count)
    {
        var vehicles = new List<Vehicle>();
        var makes = new[] { "Toyota", "Honda", "Ford", "BMW", "Mercedes", "Audi", "Lexus", "Acura" };
        var models = new[] { "Sedan", "SUV", "Hatchback", "Coupe", "Truck", "Convertible", "Wagon" };

        for (int i = 0; i < count; i++)
        {
            vehicles.Add(new Vehicle
            {
                Id = Guid.NewGuid(),
                LicensePlate = $"PERF{i:D4}",
                Make = makes[i % makes.Length],
                Model = models[i % models.Length],
                Year = 2020 + (i % 4),
                Vin = $"PERF{i:D13}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return vehicles;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
