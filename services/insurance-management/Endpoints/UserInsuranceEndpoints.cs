using Shared.Models;
using Shared.Services;
using InsuranceManagement.Services;
using InsuranceManagement.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceManagement.Endpoints;

public static class UserInsuranceEndpoints
{
    public static void MapUserInsuranceEndpoints(this WebApplication app)
    {
        var userInsurances = app.MapGroup("/insurances").WithTags("Insurances");

        userInsurances.MapGet("/{id:guid}", GetUserInsuranceById);
        userInsurances.MapGet("/user/{personalId}", GetUserInsurancesByPersonalId);
        userInsurances.MapPost("/", CreateUserInsurance);
    }

    public static async Task<IResult> GetUserInsuranceById(
        Guid id,
        [FromServices] InsuranceService insuranceService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.GetUserInsuranceById",
            async () =>
            {
                logger.LogInformation("Getting user insurance with ID: {UserInsuranceId}", id);

                var insurance = await insuranceService.GetByIdAsync(id);
                if (insurance == null)
                {
                    logger.LogWarning("User insurance not found with ID: {UserInsuranceId}", id);
                    return Results.NotFound();
                }

                appInsights.TrackBusinessEvent("Insurance.Retrieved", new Dictionary<string, string>
                {
                    ["InsuranceId"] = id.ToString()
                });

                return Results.Ok(insurance);
            },
            new Dictionary<string, string> { ["InsuranceId"] = id.ToString() });
    }

    public static async Task<IResult> GetUserInsurancesByPersonalId(
        string personalId,
        [FromServices] InsuranceService insuranceService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.GetUserInsurancesByPersonalId",
            async () =>
            {
                logger.LogInformation("Getting user insurances for personal ID: {PersonalId}", personalId);

                var userDetails = await insuranceService.GetByPersonalIdAsync(personalId);
                if (userDetails == null)
                {
                    logger.LogWarning("User not found with personal ID: {PersonalId}", personalId);
                    return Results.NotFound();
                }
                appInsights.TrackBusinessEvent("UserInsurances.Retrieved", new Dictionary<string, string>
                {
                    ["PersonalId"] = personalId,
                    ["InsuranceCount"] = userDetails.Insurances.Count.ToString()
                });

                return Results.Ok(userDetails);
            },
            new Dictionary<string, string> { ["PersonalId"] = personalId });
    }
    public static async Task<IResult> CreateUserInsurance(
        [FromBody] CreateInsuranceRequest request,
        [FromServices] InsuranceService insuranceService,
        [FromServices] ApplicationInsightsService appInsights,
        [FromServices] ILogger<Program> logger)
    {
        return await appInsights.TrackOperationAsync(
            "Endpoint.CreateUserInsurance",
            async () =>
            {
                logger.LogInformation("Creating user insurance for user: {UserId}", request.UserId);

                var result = await insuranceService.CreateAsync(request);

                if (!result.IsSuccess)
                {
                    // Track the failure reason
                    appInsights.TrackBusinessEvent("Insurance.CreationFailed", new Dictionary<string, string>
                    {
                        ["UserId"] = request.UserId.ToString(),
                        ["ErrorCode"] = result.ErrorCode ?? "UNKNOWN",
                        ["ErrorMessage"] = result.ErrorMessage ?? "Unknown error"
                    });

                    return result.ErrorCode switch
                    {
                        "CONFLICT" => Results.Conflict(new { error = result.ErrorMessage, code = result.ErrorCode }),
                        "VALIDATION_ERROR" => Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode }),
                        "NOT_FOUND" => Results.NotFound(new { error = result.ErrorMessage, code = result.ErrorCode }),
                        _ => Results.Problem(
                            detail: result.ErrorMessage,
                            statusCode: 500,
                            title: "Internal Server Error")
                    };
                }

                appInsights.TrackBusinessEvent("Insurance.CreatedViaEndpoint", new Dictionary<string, string>
                {
                    ["InsuranceId"] = result.Value!.Id.ToString(),
                    ["UserId"] = request.UserId.ToString()
                });

                return Results.Created($"/insurances/{result.Value.Id}", result.Value);
            },
            new Dictionary<string, string>
            {
                ["UserId"] = request.UserId.ToString(),
                ["PolicyId"] = request.PolicyId.ToString(),
                ["VehicleId"] = request.VehicleId.ToString()
            });
    }
}
