namespace Shared.Config;

public class AppConfig
{
    // SQL Database Configuration (using connection strings instead of managed identity)
    public string SqlConnectionString { get; set; } = string.Empty;
    public string SqlDatabaseName { get; set; } = string.Empty;

    // Service Bus Configuration (supporting both connection strings and managed identity)
    public string ServiceBusConnectionString { get; set; } = string.Empty;
    public string ServiceBusNamespace { get; set; } = string.Empty;
    public string ServiceBusQueueName { get; set; } = string.Empty;

    // Storage Configuration (supporting both connection strings and managed identity)
    public string StorageAccountName { get; set; } = string.Empty;
    public string StorageAccountConnectionString { get; set; } = string.Empty;
    public string StorageContainerName { get; set; } = string.Empty;

    // Application Insights
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;

    // Azure Identity (for Storage and Service Bus, not SQL Database)
    public string AzureClientId { get; set; } = string.Empty;

    // Environment
    public string Environment { get; set; } = string.Empty;

    // Configuration mode - false means use connection strings, true means use managed identity
    public bool UseAzureIdentity { get; set; } = false; // Default to connection strings
    public string InvoicesContainerName { get; set; } = "invoices";
    public const string EmailNotificationQueue = "invoice-email-notification-queue";
    public const string StaticInvoicesContainerName = "invoices";
}
