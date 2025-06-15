using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using VehicleInsurance.Data;
using VehicleInsurance.Endpoints;
using Xunit;
using FluentAssertions;

namespace VehicleInsurance.Tests.Endpoints;

/// <summary>
/// Integration tests for Vehicle endpoints using WebApplicationFactory
/// </summary>
public class VehicleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public VehicleEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = "TestDatabase_" + Guid.NewGuid();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<VehicleDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<VehicleDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllVehicles_WithNoVehicles_ShouldReturnEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/vehicles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var vehicles = JsonSerializer.Deserialize<Vehicle[]>(content, GetJsonOptions());
        vehicles.Should().NotBeNull();
        vehicles.Should().BeEmpty();
    }
    [Fact]
    public async Task GetAllVehicles_WithExistingVehicles_ShouldReturnVehicles()
    {
        // Arrange - Create vehicles through the API to ensure they exist in the correct context
        var vehicle1 = new CreateVehicleRequest("ABC123", "Honda", "Civic", 2020, "1HGBH41JXMN109186");
        var vehicle2 = new CreateVehicleRequest("XYZ789", "Toyota", "Camry", 2021, "4T1BF1FK0CU123456");

        var content1 = new StringContent(JsonSerializer.Serialize(vehicle1), Encoding.UTF8, "application/json");
        var content2 = new StringContent(JsonSerializer.Serialize(vehicle2), Encoding.UTF8, "application/json");

        await _client.PostAsync("/vehicles", content1);
        await _client.PostAsync("/vehicles", content2);

        // Act
        var response = await _client.GetAsync("/vehicles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var vehicles = JsonSerializer.Deserialize<Vehicle[]>(content, GetJsonOptions());
        vehicles.Should().NotBeNull();
        vehicles.Should().HaveCount(2);
    }
    [Fact]
    public async Task GetVehicleById_WithValidId_ShouldReturnVehicle()
    {
        // Arrange - Create vehicle through the API to get its actual ID
        var vehicleRequest = new CreateVehicleRequest("TESLA1", "Tesla", "Model 3", 2022, "5YJ3E1EA4JF123456");
        var content = new StringContent(JsonSerializer.Serialize(vehicleRequest), Encoding.UTF8, "application/json");
        var createResponse = await _client.PostAsync("/vehicles", content);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdVehicle = JsonSerializer.Deserialize<Vehicle>(createContent, GetJsonOptions());
        createdVehicle.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/vehicles/{createdVehicle!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var returnedVehicle = JsonSerializer.Deserialize<Vehicle>(responseContent, GetJsonOptions());
        returnedVehicle.Should().NotBeNull();
        returnedVehicle!.Id.Should().Be(createdVehicle.Id);
        returnedVehicle.Make.Should().Be("Tesla");
        returnedVehicle.Model.Should().Be("Model 3");
    }

    [Fact]
    public async Task GetVehicleById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/vehicles/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVehicle_WithValidData_ShouldCreateVehicle()
    {
        // Arrange
        var createRequest = new CreateVehicleRequest(
            LicensePlate: "NEW123",
            Make: "BMW",
            Model: "X3",
            Year: 2023,
            Vin: "WBXPC9C50WP042386"
        );

        var json = JsonSerializer.Serialize(createRequest, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/vehicles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created); var responseContent = await response.Content.ReadAsStringAsync();
        var createdVehicle = JsonSerializer.Deserialize<Vehicle>(responseContent, GetJsonOptions());

        createdVehicle.Should().NotBeNull();
        createdVehicle!.Make.Should().Be("BMW");
        createdVehicle.Model.Should().Be("X3");
        createdVehicle.LicensePlate.Should().Be("NEW123");
        createdVehicle.Id.Should().NotBeEmpty();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/vehicles/{createdVehicle.Id}");
    }
    [Fact]
    public async Task CreateVehicle_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Invalid data that should fail validation
        var invalidRequest = new CreateVehicleRequest(
            LicensePlate: "", // Invalid: empty (required, min length 1)
            Make: "", // Invalid: empty (required, min length 1)  
            Model: "X3",
            Year: 1800, // Invalid: too old (range 1900-2030)
            Vin: "SHORT" // Invalid: too short (required exactly 17 characters)
        );

        var json = JsonSerializer.Serialize(invalidRequest, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/vehicles", content);        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Optionally verify the error response contains validation messages
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("errors"); // Should contain validation errors
    }
    [Fact]
    public async Task UpdateVehicle_WithValidData_ShouldUpdateVehicle()
    {
        // Arrange - Create vehicle through the API
        var vehicleRequest = new CreateVehicleRequest("OLD123", "Original", "Model", 2020, "ORIGINAL123456789");
        var createContent = new StringContent(JsonSerializer.Serialize(vehicleRequest), Encoding.UTF8, "application/json");
        var createResponse = await _client.PostAsync("/vehicles", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        var createdVehicle = JsonSerializer.Deserialize<Vehicle>(createResponseContent, GetJsonOptions());
        createdVehicle.Should().NotBeNull();

        var updateRequest = new UpdateVehicleRequest(
            LicensePlate: "UPD123",
            Make: "Updated",
            Model: "ModelUpdated",
            Year: 2024,
            Vin: "UPDATED1234567890"
        );

        var json = JsonSerializer.Serialize(updateRequest, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/vehicles/{createdVehicle!.Id}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var updatedVehicle = JsonSerializer.Deserialize<Vehicle>(responseContent, GetJsonOptions());

        updatedVehicle.Should().NotBeNull();
        updatedVehicle!.Id.Should().Be(createdVehicle.Id);
        updatedVehicle.Make.Should().Be("Updated");
        updatedVehicle.Model.Should().Be("ModelUpdated");
        updatedVehicle.LicensePlate.Should().Be("UPD123");
    }

    [Fact]
    public async Task UpdateVehicle_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateVehicleRequest(
            LicensePlate: "UPD123",
            Make: "Updated",
            Model: "ModelUpdated",
            Year: 2024,
            Vin: "UPDATED1234567890"
        );

        var json = JsonSerializer.Serialize(updateRequest, GetJsonOptions());
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/vehicles/{nonExistentId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task DeleteVehicle_WithValidId_ShouldDeleteVehicle()
    {
        // Arrange - Create vehicle through the API
        var vehicleRequest = new CreateVehicleRequest("DEL123", "ToDelete", "Model", 2020, "DELETE123456789AB"); // Fixed to 17 characters
        var createContent = new StringContent(JsonSerializer.Serialize(vehicleRequest), Encoding.UTF8, "application/json");
        var createResponse = await _client.PostAsync("/vehicles", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        var createdVehicle = JsonSerializer.Deserialize<Vehicle>(createResponseContent, GetJsonOptions());
        createdVehicle.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/vehicles/{createdVehicle!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify vehicle was deleted by trying to get it
        var getResponse = await _client.GetAsync($"/vehicles/{createdVehicle.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVehicle_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/vehicles/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Helper method to create a test vehicle
    /// </summary>
    private static Vehicle CreateTestVehicle(string make, string model, string licensePlate, string vin)
    {
        return new Vehicle
        {
            Id = Guid.NewGuid(),
            Make = make,
            Model = model,
            LicensePlate = licensePlate,
            Year = 2022,
            Vin = vin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get JSON serialization options for consistent testing
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
    public void Dispose()
    {
        _client.Dispose();
    }
}
