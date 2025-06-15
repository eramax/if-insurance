using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Config;
using Shared.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Configure AppConfig - Using standard Azure Functions configuration
        var appConfig = new AppConfig();
        context.Configuration.Bind(appConfig);
        services.AddSingleton(appConfig);

        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = appConfig.ApplicationInsightsConnectionString;
        });

        // Add our custom Application Insights service
        services.AddScoped<ApplicationInsightsService>();

        // Add Blob Storage
        services.AddSingleton(_ => new BlobServiceClient(appConfig.StorageAccountConnectionString));
        // Configure Services
        services.AddScoped<IServiceBusMessagingService, ServiceBusMessagingService>();
    })
    .Build();

await host.RunAsync();
