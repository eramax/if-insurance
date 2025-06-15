using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace Shared.Services;

/// <summary>
/// Application Insights service providing telemetry tracking capabilities
/// Similar to GenericRepository pattern but focused on business operations
/// </summary>
public class ApplicationInsightsService
{
    private readonly TelemetryClient? _telemetryClient;

    public ApplicationInsightsService(TelemetryClient? telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    /// <summary>
    /// Track a business operation with timing and result
    /// </summary>
    public async Task<T> TrackOperationAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, string>? properties = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        try
        {
            // Start operation tracking
            TrackTrace(
                $"Starting operation: {operationName}",
                SeverityLevel.Information,
                AddDefaultProperties(properties, operationName, operationId));

            var result = await operation();

            stopwatch.Stop();

            // Track successful operation
            TrackDependency(new DependencyTelemetry(
                "BusinessOperation",
                operationName,
                operationName,
                operationId)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            });

            TrackMetric(
                $"{operationName}.Duration",
                stopwatch.Elapsed.TotalMilliseconds,
                AddDefaultProperties(properties, operationName, operationId));

            TrackEvent(
                $"{operationName}.Completed",
                AddDefaultProperties(properties, operationName, operationId));

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Track failed operation
            TrackDependency(new DependencyTelemetry(
                "BusinessOperation",
                operationName,
                operationName,
                operationId)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            });

            TrackException(ex, AddDefaultProperties(properties, operationName, operationId));

            throw;
        }
    }

    /// <summary>
    /// Track a business operation without return value
    /// </summary>
    public async Task TrackOperationAsync(
        string operationName,
        Func<Task> operation,
        Dictionary<string, string>? properties = null)
    {
        await TrackOperationAsync(operationName, async () =>
        {
            await operation();
            return true;
        }, properties);
    }

    /// <summary>
    /// Track custom metrics for business KPIs
    /// </summary>
    public void TrackBusinessMetric(string metricName, double value, Dictionary<string, string>? properties = null)
    {
        TrackMetric(metricName, value, properties);
    }

    /// <summary>
    /// Track business events (user actions, system events)
    /// </summary>
    public void TrackBusinessEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        TrackEvent(eventName, properties);
    }

    /// <summary>
    /// Track external service calls (Service Bus, databases, APIs)
    /// </summary>
    public async Task<T> TrackExternalServiceAsync<T>(
        string serviceName,
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, string>? properties = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        try
        {
            var result = await operation();

            stopwatch.Stop();

            TrackDependency(new DependencyTelemetry(
                serviceName,
                operationName,
                operationName,
                operationId)
            {
                Duration = stopwatch.Elapsed,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            TrackDependency(new DependencyTelemetry(
                serviceName,
                operationName,
                operationName,
                operationId)
            {
                Duration = stopwatch.Elapsed,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.Subtract(stopwatch.Elapsed)
            });

            TrackException(ex, AddDefaultProperties(properties, operationName, operationId));
            throw;
        }
    }

    /// <summary>
    /// Track performance counters and system metrics
    /// </summary>
    public void TrackPerformanceMetric(string metricName, double value, string? instance = null)
    {
        var properties = instance != null
            ? new Dictionary<string, string> { ["Instance"] = instance }
            : null;

        TrackMetric($"Performance.{metricName}", value, properties);
    }

    // Private helper methods similar to GenericRepository
    private void TrackTrace(string message, SeverityLevel level, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackTrace(message, level, properties);
    }

    private void TrackDependency(DependencyTelemetry dependency)
    {
        _telemetryClient?.TrackDependency(dependency);
    }

    private void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackException(exception, properties);
    }

    private void TrackMetric(string name, double value, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackMetric(name, value, properties);
    }

    private void TrackEvent(string name, Dictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackEvent(name, properties);
    }

    private static Dictionary<string, string> AddDefaultProperties(
        Dictionary<string, string>? properties,
        string operationName,
        string operationId)
    {
        var defaultProperties = new Dictionary<string, string>
        {
            ["OperationName"] = operationName,
            ["OperationId"] = operationId,
            ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                defaultProperties[prop.Key] = prop.Value;
            }
        }

        return defaultProperties;
    }
}
