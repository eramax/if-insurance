using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;
using InsuranceManagement.Data;
using InsuranceManagement.Models;
using Shared.Models;
using Microsoft.Data.Sqlite;
using insurance_management_Tests.Data;

namespace insurance_management_Tests.Endpoints;

public class UserInsuranceEndpointsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly InsuranceManagementDbContext _context;
    private readonly SqliteConnection _connection;

    public UserInsuranceEndpointsTests()
    {
        // Create an in-memory SQLite database with a unique connection string
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<InsuranceManagementDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Also remove the DbContext service itself
                var contextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(InsuranceManagementDbContext));
                if (contextDescriptor != null)
                    services.Remove(contextDescriptor);

                // Add SQLite in-memory database for testing with the base context type
                services.AddDbContext<InsuranceManagementDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
            // Add test configuration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("SvbusInvoiceGenQueueName", "test-invoice-queue")
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<InsuranceManagementDbContext>();

        // Ensure the database is created
        _context.Database.EnsureCreated();
    }
    private async Task<(Guid userId, Guid policyId, Guid vehicleId, Guid coverageId, Guid insuranceId)> SeedTestDataAsync()
    {
        var userId = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var vehicle2Id = Guid.NewGuid();
        var coverageId1 = Guid.NewGuid();
        var coverageId2 = Guid.NewGuid();
        var insuranceId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            PersonalId = Guid.NewGuid().ToString("N")[..8], // Use unique 8-character ID
            Name = "Alice Johnson",
            Email = $"alice{userId:N}@example.com",
            PhoneNumber = "555-0123",
            Address = "123 Main St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1990, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = user2Id,
            PersonalId = Guid.NewGuid().ToString("N")[..8], // Use unique 8-character ID
            Name = "Bob Smith",
            Email = $"bob{user2Id:N}@example.com",
            PhoneNumber = "555-0124",
            Address = "456 Oak St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1985, 5, 15),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var policy = new Policy
        {
            Id = policyId,
            Name = "Comprehensive Auto Policy",
            PolicyType = PolicyType.Vehicle,
            Description = "Full coverage auto insurance",
            InsuranceCompany = "Test Insurance Co",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var coverage1 = new Coverage
        {
            Id = coverageId1,
            Name = "Liability Coverage",
            Tier = CoverageTier.Basic,
            Description = "Basic liability coverage",
            Price = 100.00m,
            DurationInMonths = 12,
            Status = CoverageStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }; var coverage2 = new Coverage
        {
            Id = coverageId2,
            Name = "Collision Coverage",
            Tier = CoverageTier.Complementary,
            Description = "Collision coverage",
            Price = 200.00m,
            DurationInMonths = 12,
            Status = CoverageStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var vehicle1 = new Vehicle
        {
            Id = vehicleId,
            LicensePlate = $"TST{vehicleId.ToString("N")[..7].ToUpper()}",
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Vin = $"VIN{vehicleId.ToString("N")[..14].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var vehicle2 = new Vehicle
        {
            Id = vehicle2Id,
            LicensePlate = $"TS2{vehicle2Id.ToString("N")[..7].ToUpper()}",
            Make = "Honda",
            Model = "Civic",
            Year = 2019,
            Vin = $"VIN{vehicle2Id.ToString("N")[..14].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add independent entities first
        await _context.Users.AddRangeAsync(user, user2);
        await _context.Policies.AddAsync(policy);
        await _context.Coverages.AddRangeAsync(coverage1, coverage2);
        await _context.Vehicles.AddRangeAsync(vehicle1, vehicle2);
        await _context.SaveChangesAsync();

        // Now add VehicleInsurance that depends on the entities above
        var insurance = new VehicleInsurance
        {
            Id = insuranceId,
            UserId = userId,
            PolicyId = policyId,
            VehicleId = vehicleId,
            Status = InsuranceStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(12),
            RenewalDate = DateTime.UtcNow.AddMonths(11),
            TotalPremium = 300.00m,
            Deductible = 500.00m,
            Notes = "Test insurance",
            PolicyNumber = "EPTEST001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.VehicleInsurances.AddAsync(insurance);
        await _context.SaveChangesAsync();

        return (userId, policyId, vehicleId, coverageId1, insuranceId);
    }
    [Fact]
    public async Task GetUserInsuranceById_WithValidId_ReturnsOk()
    {
        // Arrange
        var (_, _, _, _, insuranceId) = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/insurances/{insuranceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var insurance = JsonSerializer.Deserialize<VehicleInsuranceDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        insurance.Should().NotBeNull();
        insurance!.Id.Should().Be(insuranceId);
        insurance.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetUserInsuranceById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/insurances/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserInsurancesByPersonalId_WithValidPersonalId_ReturnsOk()
    {
        // Arrange
        var (userId, _, _, _, _) = await SeedTestDataAsync();
        var user = await _context.Users.FindAsync(userId);
        var personalId = user!.PersonalId;

        // Act
        var response = await _client.GetAsync($"/insurances/user/{personalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var userDetails = JsonSerializer.Deserialize<UserInsuranceDetailsDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userDetails.Should().NotBeNull();
        userDetails!.User.PersonalId.Should().Be(personalId);
        userDetails.User.Name.Should().Be("Alice Johnson");
        userDetails.Insurances.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserInsurancesByPersonalId_WithInvalidPersonalId_ReturnsNotFound()
    {
        // Arrange
        var invalidPersonalId = "99999999";

        // Act
        var response = await _client.GetAsync($"/insurances/user/{invalidPersonalId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUserInsurance_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var (_, policyId, _, coverageId, _) = await SeedTestDataAsync();
        // Create a new user for this test
        var newUserId = Guid.NewGuid();
        var newVehicleId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            PersonalId = Guid.NewGuid().ToString("N")[..8],
            Name = "Charlie Brown",
            Email = $"charlie{newUserId:N}@example.com",
            PhoneNumber = "555-0125",
            Address = "789 Pine St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1988, 3, 10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newVehicle = new Vehicle
        {
            Id = newVehicleId,
            LicensePlate = $"NEW{newVehicleId.ToString("N")[..7].ToUpper()}",
            Make = "Tesla",
            Model = "Model 3",
            Year = 2022,
            Vin = $"NEW{newVehicleId.ToString("N")[..14].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser);
        await _context.Vehicles.AddAsync(newVehicle);
        await _context.SaveChangesAsync();

        var request = new CreateInsuranceRequest(
            UserId: newUserId,
            PolicyId: policyId,
            VehicleId: newVehicleId,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            CoverageIds: [coverageId],
            Deductible: 500.00m,
            Notes: "New insurance policy"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");        // Act
        var response = await _client.PostAsync("/insurances", content);

        // Debug: If not successful, output the response content
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201 Created but got {response.StatusCode}. Response: {errorContent}");
        }

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var insurance = JsonSerializer.Deserialize<VehicleInsuranceDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        insurance.Should().NotBeNull();
        insurance!.TotalPremium.Should().Be(100.00m);
    }

    [Fact]
    public async Task CreateUserInsurance_WithConflictingInsurance_ReturnsConflict()
    {
        // Arrange - Try to create insurance for existing user/vehicle combination
        var (userId, policyId, vehicleId, coverageId, _) = await SeedTestDataAsync();

        var request = new CreateInsuranceRequest(
            UserId: userId,
            PolicyId: policyId,
            VehicleId: vehicleId,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            CoverageIds: [coverageId]
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/insurances", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUserInsurance_WithInvalidCoverageIds_ReturnsBadRequest()
    {
        // Arrange
        var (_, policyId, _, _, _) = await SeedTestDataAsync();
        // Create a new user for this test
        var newUserId = Guid.NewGuid();
        var newVehicleId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            PersonalId = Guid.NewGuid().ToString("N")[..8],
            Name = "David Wilson",
            Email = $"david{newUserId:N}@example.com",
            PhoneNumber = "555-0126",
            Address = "456 Elm St",
            Status = UserStatus.Active,
            DateOfBirth = new DateTime(1992, 7, 20),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newVehicle = new Vehicle
        {
            Id = newVehicleId,
            LicensePlate = $"DVW{newVehicleId.ToString("N")[..7].ToUpper()}",
            Make = "BMW",
            Model = "X5",
            Year = 2021,
            Vin = $"DVW{newVehicleId.ToString("N")[..14].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser);
        await _context.Vehicles.AddAsync(newVehicle);
        await _context.SaveChangesAsync();

        var invalidCoverageId = Guid.NewGuid();
        var request = new CreateInsuranceRequest(
            UserId: newUserId,
            PolicyId: policyId,
            VehicleId: newVehicleId,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(12),
            RenewalDate: DateTime.UtcNow.AddMonths(11),
            CoverageIds: [invalidCoverageId]
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/insurances", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        _connection?.Dispose();
    }
}
