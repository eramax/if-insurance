namespace InsuranceManagementSystem.Functions.BillingService.Config;

/// <summary>
/// Configuration settings for the Billing Service Function
/// </summary>
public class AppConfig
{
    // SQL Database Configuration
    public string SqlConnectionString { get; set; } = string.Empty;
    public string SqlDatabaseName { get; set; } = string.Empty;

    // Service Bus Configuration
    public string ServiceBusConnectionString { get; set; } = string.Empty;
    public string ServiceBusNamespace { get; set; } = string.Empty;
    public string SvbusInvoiceGenQueueName { get; set; } = string.Empty;
    public string SvbusInvoiceEmailQueueName { get; set; } = string.Empty;

    // Storage Configuration
    public string StorageAccountName { get; set; } = string.Empty;
    public string StorageAccountConnectionString { get; set; } = string.Empty;
    public string StorageContainerName { get; set; } = string.Empty;
    public string InvoicesContainerName { get; set; } = string.Empty;

    // Application Insights
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
}
