using Shared.Models;
using Shared.Services;
using VehicleInsurance.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VehicleInsurance.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this WebApplication app)
    {
        var vehicles = app.MapGroup("/vehicles").WithTags("Vehicles");

        vehicles.MapGet("/", GetAllVehicles);
        vehicles.MapGet("/{id:guid}", GetVehicleById);
        vehicles.MapPost("/", CreateVehicle);
        vehicles.MapPut("/{id:guid}", UpdateVehicle);
        vehicles.MapDelete("/{id:guid}", DeleteVehicle);
    }

    public static async Task<IResult> GetAllVehicles(
        [FromServices] VehicleService vehicleService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.GetAllVehicles",
            async () =>
            {
                logger.LogInformation("Getting all vehicles");

                var vehicles = await vehicleService.GetAllAsync();

                appInsights.TrackBusinessEvent("Vehicles.Retrieved", new Dictionary<string, string>
                {
                    ["Count"] = vehicles.Count().ToString()
                });

                return Results.Ok(vehicles);
            });
    }

    public static async Task<IResult> GetVehicleById(
        Guid id,
        [FromServices] VehicleService vehicleService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.GetVehicleById",
            async () =>
            {
                logger.LogInformation("Getting vehicle with ID: {VehicleId}", id);

                var vehicle = await vehicleService.GetByIdAsync(id);
                if (vehicle == null)
                {
                    logger.LogWarning("Vehicle not found with ID: {VehicleId}", id);
                    return Results.NotFound();
                }

                appInsights.TrackBusinessEvent("Vehicle.Retrieved", new Dictionary<string, string>
                {
                    ["VehicleId"] = id.ToString()
                });

                return Results.Ok(vehicle);
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }
    public static async Task<IResult> CreateVehicle(
        CreateVehicleRequest request,
        [FromServices] VehicleService vehicleService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.CreateVehicle",
            async () =>
            {
                // Validate request manually since record validation may not work as expected
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);

                // Try record validation first
                bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

                // Add additional manual validation if record validation doesn't work
                if (string.IsNullOrWhiteSpace(request.LicensePlate))
                    validationResults.Add(new ValidationResult("LicensePlate is required and cannot be empty.", new[] { nameof(request.LicensePlate) }));

                if (string.IsNullOrWhiteSpace(request.Make))
                    validationResults.Add(new ValidationResult("Make is required and cannot be empty.", new[] { nameof(request.Make) }));

                if (string.IsNullOrWhiteSpace(request.Model))
                    validationResults.Add(new ValidationResult("Model is required and cannot be empty.", new[] { nameof(request.Model) }));

                if (request.Year < 1900 || request.Year > 2030)
                    validationResults.Add(new ValidationResult("Year must be between 1900 and 2030.", new[] { nameof(request.Year) }));

                if (string.IsNullOrWhiteSpace(request.Vin) || request.Vin.Length != 17)
                    validationResults.Add(new ValidationResult("VIN must be exactly 17 characters long.", new[] { nameof(request.Vin) }));

                if (request.LicensePlate?.Length > 20)
                    validationResults.Add(new ValidationResult("LicensePlate cannot be longer than 20 characters.", new[] { nameof(request.LicensePlate) }));

                if (request.Make?.Length > 100)
                    validationResults.Add(new ValidationResult("Make cannot be longer than 100 characters.", new[] { nameof(request.Make) }));

                if (request.Model?.Length > 100)
                    validationResults.Add(new ValidationResult("Model cannot be longer than 100 characters.", new[] { nameof(request.Model) }));

                if (validationResults.Count > 0)
                {
                    var errors = validationResults.Select(v => v.ErrorMessage).ToArray();
                    return Results.BadRequest(new { Errors = errors });
                }
                logger.LogInformation("Creating vehicle with VIN: {VIN}", request.Vin);

                var vehicle = new Vehicle
                {
                    LicensePlate = request.LicensePlate ?? string.Empty,
                    Make = request.Make ?? string.Empty,
                    Model = request.Model ?? string.Empty,
                    Year = request.Year,
                    Vin = request.Vin ?? string.Empty
                };

                var createdVehicle = await vehicleService.CreateAsync(vehicle);

                appInsights.TrackBusinessEvent("Vehicle.Created", new Dictionary<string, string>
                {
                    ["VehicleId"] = createdVehicle.Id.ToString(),
                    ["Make"] = createdVehicle.Make ?? string.Empty,
                    ["Model"] = createdVehicle.Model ?? string.Empty
                });

                return Results.Created($"/vehicles/{createdVehicle.Id}", createdVehicle);
            },
            new Dictionary<string, string>
            {
                ["VIN"] = request.Vin,
                ["Make"] = request.Make,
                ["Model"] = request.Model
            });
    }
    public static async Task<IResult> UpdateVehicle(
        Guid id,
        UpdateVehicleRequest request,
        [FromServices] VehicleService vehicleService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.UpdateVehicle",
            async () =>
            {
                // Validate request manually since record validation may not work as expected
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);

                // Try record validation first
                bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

                // Add additional manual validation if record validation doesn't work
                if (string.IsNullOrWhiteSpace(request.LicensePlate))
                    validationResults.Add(new ValidationResult("LicensePlate is required and cannot be empty.", new[] { nameof(request.LicensePlate) }));

                if (string.IsNullOrWhiteSpace(request.Make))
                    validationResults.Add(new ValidationResult("Make is required and cannot be empty.", new[] { nameof(request.Make) }));

                if (string.IsNullOrWhiteSpace(request.Model))
                    validationResults.Add(new ValidationResult("Model is required and cannot be empty.", new[] { nameof(request.Model) }));

                if (request.Year < 1900 || request.Year > 2030)
                    validationResults.Add(new ValidationResult("Year must be between 1900 and 2030.", new[] { nameof(request.Year) }));

                if (string.IsNullOrWhiteSpace(request.Vin) || request.Vin.Length != 17)
                    validationResults.Add(new ValidationResult("VIN must be exactly 17 characters long.", new[] { nameof(request.Vin) }));

                if (request.LicensePlate?.Length > 20)
                    validationResults.Add(new ValidationResult("LicensePlate cannot be longer than 20 characters.", new[] { nameof(request.LicensePlate) }));

                if (request.Make?.Length > 100)
                    validationResults.Add(new ValidationResult("Make cannot be longer than 100 characters.", new[] { nameof(request.Make) }));

                if (request.Model?.Length > 100)
                    validationResults.Add(new ValidationResult("Model cannot be longer than 100 characters.", new[] { nameof(request.Model) }));

                if (validationResults.Count > 0)
                {
                    var errors = validationResults.Select(v => v.ErrorMessage).ToArray();
                    return Results.BadRequest(new { Errors = errors });
                }
                logger.LogInformation("Updating vehicle with ID: {VehicleId}", id);

                var updatedVehicle = new Vehicle
                {
                    LicensePlate = request.LicensePlate ?? string.Empty,
                    Make = request.Make ?? string.Empty,
                    Model = request.Model ?? string.Empty,
                    Year = request.Year,
                    Vin = request.Vin ?? string.Empty
                };

                var result = await vehicleService.UpdateAsync(id, updatedVehicle);
                if (result == null)
                {
                    logger.LogWarning("Vehicle not found for update with ID: {VehicleId}", id);
                    return Results.NotFound();
                }

                appInsights.TrackBusinessEvent("Vehicle.Updated", new Dictionary<string, string>
                {
                    ["VehicleId"] = id.ToString(),
                    ["Make"] = result.Make ?? string.Empty,
                    ["Model"] = result.Model ?? string.Empty
                });

                return Results.Ok(result);
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }

    public static async Task<IResult> DeleteVehicle(
        Guid id,
        [FromServices] VehicleService vehicleService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.DeleteVehicle",
            async () =>
            {
                logger.LogInformation("Deleting vehicle with ID: {VehicleId}", id);

                var success = await vehicleService.DeleteAsync(id);
                if (!success)
                {
                    logger.LogWarning("Vehicle not found for deletion with ID: {VehicleId}", id);
                    return Results.NotFound();
                }

                appInsights.TrackBusinessEvent("Vehicle.Deleted", new Dictionary<string, string>
                {
                    ["VehicleId"] = id.ToString()
                });

                return Results.Ok(new { Message = "Vehicle deleted successfully", VehicleId = id });
            },
            new Dictionary<string, string> { ["VehicleId"] = id.ToString() });
    }
}

// Request DTOs for validation
public record CreateVehicleRequest(
    [Required, MinLength(1), StringLength(20, MinimumLength = 1)] string LicensePlate,
    [Required, MinLength(1), StringLength(100, MinimumLength = 1)] string Make,
    [Required, MinLength(1), StringLength(100, MinimumLength = 1)] string Model,
    [Range(1900, 2030)] int Year,
    [Required, MinLength(17), StringLength(17, MinimumLength = 17)] string Vin
);

public record UpdateVehicleRequest(
    [Required, MinLength(1), StringLength(20, MinimumLength = 1)] string LicensePlate,
    [Required, MinLength(1), StringLength(100, MinimumLength = 1)] string Make,
    [Required, MinLength(1), StringLength(100, MinimumLength = 1)] string Model,
    [Range(1900, 2030)] int Year,
    [Required, MinLength(17), StringLength(17, MinimumLength = 17)] string Vin
);
