using InsuranceManagement.Data;
using InsuranceManagement.Endpoints;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

var sqlConnectionString = builder.Configuration["SqlConnectionString"];

if (string.IsNullOrEmpty(sqlConnectionString))
{
    builder.Services.AddDbContext<InsuranceManagementDbContext>(options =>
        options.UseInMemoryDatabase("InsuranceManagementDb"));
}
else
{
    builder.Services.AddDbContext<InsuranceManagementDbContext>(options =>
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
builder.Services.AddScoped<InsuranceManagement.Services.InsuranceService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await InitializeDatabaseAsync(app.Services, app.Logger);

// Test Application Insights configuration
using (var scope = app.Services.CreateScope())
{
    var telemetryClient = scope.ServiceProvider.GetService<Microsoft.ApplicationInsights.TelemetryClient>();
    var appInsightsService = scope.ServiceProvider.GetService<Shared.Services.ApplicationInsightsService>();

    if (telemetryClient != null)
    {
        app.Logger.LogInformation("✅ Application Insights TelemetryClient is properly configured");
        telemetryClient.TrackEvent("Application_Started");
    }
    else
    {
        app.Logger.LogWarning("❌ Application Insights TelemetryClient is not configured");
    }

    if (appInsightsService != null)
    {
        app.Logger.LogInformation("✅ Custom ApplicationInsightsService is properly configured");
    }
    else
    {
        app.Logger.LogWarning("❌ Custom ApplicationInsightsService is not configured");
    }
}

app.MapUserInsuranceEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();

static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InsuranceManagementDbContext>();
    try
    {
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database");
        throw;
    }
}

public partial class Program { }