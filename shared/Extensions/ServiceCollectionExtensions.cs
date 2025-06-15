using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Repository;
using Shared.Services;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for registering services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a generic repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGenericRepository<TEntity, TKey, TContext>(this IServiceCollection services)
        where TEntity : class
        where TContext : DbContext
    {
        services.AddScoped<IGenericRepository<TEntity, TKey>>(serviceProvider =>
        {
            var context = serviceProvider.GetRequiredService<TContext>();
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            return new GenericRepository<TEntity, TKey>(context, telemetryClient);
        });

        return services;
    }

    /// <summary>
    /// Registers a generic repository for the specified entity type with DbContext interface
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGenericRepository<TEntity, TKey>(this IServiceCollection services)
        where TEntity : class
    {
        services.AddScoped<IGenericRepository<TEntity, TKey>>(serviceProvider =>
        {
            var context = serviceProvider.GetRequiredService<DbContext>();
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            return new GenericRepository<TEntity, TKey>(context, telemetryClient);
        });

        return services;
    }

    /// <summary>
    /// Registers the Service Bus messaging service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServiceBusMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusMessagingService, ServiceBusMessagingService>();
        return services;
    }    /// <summary>
         /// Registers the Application Insights service for telemetry tracking
         /// </summary>
         /// <param name="services">Service collection</param>
         /// <param name="configuration">Configuration to read Application Insights connection string</param>
         /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApplicationInsightsService(this IServiceCollection services, IConfiguration configuration)
    {
        // Get the Application Insights connection string from configuration
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine($"✅ Application Insights connection string found: {connectionString[..50]}...");

            // Register Application Insights telemetry services with connection string
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
            });
        }
        else
        {
            Console.WriteLine("❌ Application Insights connection string not found in configuration");

            // Fallback to default configuration (will use environment variables or other sources)
            services.AddApplicationInsightsTelemetry();
        }

        // Register our custom Application Insights service
        services.AddScoped<ApplicationInsightsService>(serviceProvider =>
        {
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            return new ApplicationInsightsService(telemetryClient);
        });
        return services;
    }

    /// <summary>
    /// Registers a generic repository factory that can create repositories for any entity type
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGenericRepositoryFactory(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepositoryFactory, GenericRepositoryFactory>();
        return services;
    }
}

/// <summary>
/// Factory interface for creating generic repositories
/// </summary>
public interface IGenericRepositoryFactory
{
    /// <summary>
    /// Creates a generic repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    /// <returns>Generic repository instance</returns>
    IGenericRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
        where TEntity : class;
}

/// <summary>
/// Implementation of the generic repository factory
/// </summary>
public class GenericRepositoryFactory : IGenericRepositoryFactory
{
    private readonly DbContext _context;
    private readonly TelemetryClient _telemetryClient;

    public GenericRepositoryFactory(DbContext context, TelemetryClient telemetryClient)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public IGenericRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
        where TEntity : class
    {
        return new GenericRepository<TEntity, TKey>(_context, _telemetryClient);
    }
}
