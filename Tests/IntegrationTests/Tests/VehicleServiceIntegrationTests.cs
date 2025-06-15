using Xunit;
using Microsoft.Extensions.Configuration;
using IntegrationTests.Configuration;
using IntegrationTests.Helpers;
using Shared.Models;
using System.Net;

namespace IntegrationTests.Tests;

public class VehicleServiceIntegrationTests : IDisposable
{
    private readonly TestConfiguration _config;
    private readonly HttpTestHelper _httpHelper; public VehicleServiceIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _config = new TestConfiguration();
        configuration.GetSection("TestConfiguration").Bind(_config);

        _httpHelper = new HttpTestHelper(_config);
    }

    [Fact]
    public async Task CreateVehicle_ShouldReturnCreatedVehicle_WhenValidDataProvided()
    {
        // Arrange
        var newVehicle = new
        {
            vin = $"1HGCM82633A{Random.Shared.Next(100000, 999999)}",
            make = "Honda",
            model = "Accord",
            year = Random.Shared.Next(2015, 2024),
            licensePlate = $"ABC{Random.Shared.Next(1000, 9999)}"
        };

        // Act
        var response = await _httpHelper.PostJsonAsync(_config.VehicleServiceUrl, "/vehicles", newVehicle);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdVehicle = await _httpHelper.ReadResponseAsync<Vehicle>(response);
        Assert.NotNull(createdVehicle);
        Assert.NotEqual(Guid.Empty, createdVehicle.Id);
        Assert.Equal(newVehicle.vin, createdVehicle.Vin);
        Assert.Equal(newVehicle.make, createdVehicle.Make);
        Assert.Equal(newVehicle.model, createdVehicle.Model);
        Assert.Equal(newVehicle.year, createdVehicle.Year);
        Assert.Equal(newVehicle.licensePlate, createdVehicle.LicensePlate);
    }

    [Fact]
    public async Task GetAllVehicles_ShouldReturnVehiclesList_WhenCalled()
    {
        // Arrange
        // First create a vehicle to ensure we have at least one
        var newVehicle = new
        {
            vin = $"1HGCM82633A{Random.Shared.Next(100000, 999999)}",
            make = "Toyota",
            model = "Camry",
            year = 2022,
            licensePlate = $"XYZ{Random.Shared.Next(1000, 9999)}"
        };

        await _httpHelper.PostJsonAsync(_config.VehicleServiceUrl, "/vehicles", newVehicle);

        // Act
        var response = await _httpHelper.GetAsync(_config.VehicleServiceUrl, "/vehicles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var vehicles = await _httpHelper.ReadResponseAsync<List<Vehicle>>(response);
        Assert.NotNull(vehicles);
        Assert.NotEmpty(vehicles);
    }

    public void Dispose()
    {
        _httpHelper?.Dispose();
    }
}
