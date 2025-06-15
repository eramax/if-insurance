using Azure.Storage.Blobs;
using InsuranceManagementSystem.Functions.BillingService.Config;
using InsuranceManagementSystem.Functions.BillingService.Data;
using InsuranceManagementSystem.Functions.BillingService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Shared.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Configure AppConfig - Using standard Azure Functions configuration
        var appConfig = new AppConfig();
        context.Configuration.Bind(appConfig);
        services.AddSingleton(appConfig);

        // Configure Database
        services.AddDbContext<InsuranceDbContext>(options =>
            options.UseSqlServer(appConfig.SqlConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = appConfig.ApplicationInsightsConnectionString;
        });

        // Add our custom Application Insights service
        services.AddScoped<ApplicationInsightsService>();

        // Add Blob Storage
        services.AddSingleton(_ => new BlobServiceClient(appConfig.StorageAccountConnectionString));        
        // Configure Services
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IInvoiceGenerator, InvoiceGenerator>();
        services.AddScoped<IServiceBusMessagingService, ServiceBusMessagingService>();
        services.AddSingleton<IServiceBusMessageFactory, ServiceBusMessageFactory>();
    })
    .Build();

await host.RunAsync();
