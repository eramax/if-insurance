using Shared.Models;
using Shared.Extensions;
using VehicleInsurance.Endpoints;
using VehicleInsurance.Services;
using Microsoft.EntityFrameworkCore;
using VehicleInsurance.Data;

var builder = WebApplication.CreateBuilder(args);

var sqlConnectionString = builder.Configuration["SqlConnectionString"];

if (string.IsNullOrEmpty(sqlConnectionString))
{
    builder.Services.AddDbContext<VehicleDbContext>(options =>
        options.UseInMemoryDatabase("VehicleInsuranceDb"));
}
else
{
    builder.Services.AddDbContext<VehicleDbContext>(options =>
        options.UseSqlServer(sqlConnectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Configure Application Insights first with connection string from configuration
builder.Services.AddApplicationInsightsService(builder.Configuration);

// Register other services
builder.Services.AddServiceBusMessaging();

// Register services
builder.Services.AddScoped<VehicleService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await InitializeDatabaseAsync(app.Services, app.Logger);

// API endpoints
app.MapVehicleEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();

// Helper method to initialize database
static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<VehicleDbContext>();

    try
    {
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("Database initialized successfully");

        var vehicleCount = await dbContext.Vehicles.CountAsync();
        logger.LogInformation("Current vehicle count: {Count}", vehicleCount);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Make Program class accessible for testing
public partial class Program { }
