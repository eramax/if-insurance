using Xunit;
using Microsoft.Extensions.Configuration;
using IntegrationTests.Configuration;
using IntegrationTests.Helpers;
using Shared.Models;
using System.Net;
using VehicleInsuranceModel = Shared.Models.VehicleInsurance;

namespace IntegrationTests.Tests;

public class InsuranceServiceIntegrationTests : IDisposable
{
    private readonly TestConfiguration _config;
    private readonly HttpTestHelper _httpHelper; public InsuranceServiceIntegrationTests()
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
    public async Task CreateInsurance_ShouldReturnCreatedInsurance_WhenValidDataProvided()
    {
        // Arrange - First create a vehicle to use in the insurance
        var newVehicle = new
        {
            vin = $"1HGCM82633A{Random.Shared.Next(100000, 999999)}",
            make = "Honda",
            model = "Civic",
            year = 2023,
            licensePlate = $"INS{Random.Shared.Next(1000, 9999)}"
        };

        var vehicleResponse = await _httpHelper.PostJsonAsync(_config.VehicleServiceUrl, "/vehicles", newVehicle);
        Assert.Equal(HttpStatusCode.Created, vehicleResponse.StatusCode);

        var createdVehicle = await _httpHelper.ReadResponseAsync<Vehicle>(vehicleResponse);
        Assert.NotNull(createdVehicle);

        // Now create insurance for this vehicle
        var newInsurance = new
        {
            userId = _config.TestUserId,
            policyId = _config.TestPolicyId,
            vehicleId = createdVehicle.Id,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddYears(1),
            renewalDate = DateTime.UtcNow.AddYears(1),
            coverageIds = _config.TestCoverageIds,
            deductible = 500,
            notes = "Integration test insurance policy"
        };

        // Act
        var response = await _httpHelper.PostJsonAsync(_config.InsuranceServiceUrl, "/insurances", newInsurance);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdInsurance = await _httpHelper.ReadResponseAsync<VehicleInsuranceModel>(response);
        Assert.NotNull(createdInsurance);
        Assert.NotEqual(Guid.Empty, createdInsurance.Id);
        Assert.Equal(Guid.Parse(newInsurance.userId), createdInsurance.UserId);
        Assert.Equal(Guid.Parse(newInsurance.policyId), createdInsurance.PolicyId);
        Assert.Equal(newInsurance.vehicleId, createdInsurance.VehicleId);
        Assert.Equal(InsuranceStatus.Active, createdInsurance.Status);
    }

    [Fact]
    public async Task GetInsuranceById_ShouldReturnInsurance_WhenInsuranceExists()
    {
        // Arrange - First create a vehicle and insurance
        var newVehicle = new
        {
            vin = $"1HGCM82633A{Random.Shared.Next(100000, 999999)}",
            make = "Ford",
            model = "Focus",
            year = 2021,
            licensePlate = $"GET{Random.Shared.Next(1000, 9999)}"
        };

        var vehicleResponse = await _httpHelper.PostJsonAsync(_config.VehicleServiceUrl, "/vehicles", newVehicle);
        var createdVehicle = await _httpHelper.ReadResponseAsync<Vehicle>(vehicleResponse);

        var newInsurance = new
        {
            userId = _config.TestUserId,
            policyId = _config.TestPolicyId,
            vehicleId = createdVehicle!.Id,
            startDate = DateTime.UtcNow,
            endDate = DateTime.UtcNow.AddYears(1),
            renewalDate = DateTime.UtcNow.AddYears(1),
            coverageIds = _config.TestCoverageIds,
            deductible = 1000,
            notes = "Test insurance for GET operation"
        };

        var insuranceResponse = await _httpHelper.PostJsonAsync(_config.InsuranceServiceUrl, "/insurances", newInsurance);
        var createdInsurance = await _httpHelper.ReadResponseAsync<VehicleInsuranceModel>(insuranceResponse);

        // Act
        var response = await _httpHelper.GetAsync(_config.InsuranceServiceUrl, $"/insurances/{createdInsurance!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrievedInsurance = await _httpHelper.ReadResponseAsync<VehicleInsuranceModel>(response);
        Assert.NotNull(retrievedInsurance);
        Assert.Equal(createdInsurance.Id, retrievedInsurance.Id);
        Assert.Equal(createdInsurance.UserId, retrievedInsurance.UserId);
        Assert.Equal(createdInsurance.VehicleId, retrievedInsurance.VehicleId);
    }

    public void Dispose()
    {
        _httpHelper?.Dispose();
    }
}
