using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shared.Models;
using InsuranceManagementSystem.Functions.BillingService.Config;
using InsuranceManagementSystem.Functions.BillingService.Services;

namespace InsuranceManagementSystem.Functions.BillingService
{
    /// <summary>
    /// Function for processing all insurances
    /// </summary>
    public class ProcessAllInsurancesFunction
    {
        private readonly ILogger<ProcessAllInsurancesFunction> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly IBillingService _billingService;
        private readonly AppConfig _appConfig;
        private readonly IServiceBusMessageFactory _messageFactory;

        public ProcessAllInsurancesFunction(
            ILogger<ProcessAllInsurancesFunction> logger,
            TelemetryClient telemetryClient,
            IBillingService billingService,
            AppConfig appConfig,
            IServiceBusMessageFactory messageFactory)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _billingService = billingService;
            _appConfig = appConfig;
            _messageFactory = messageFactory;
        }

        /// <summary>
        /// Function triggered by timer to process all active insurances
        /// </summary>
        [Function("ProcessAllInsurances")]
        [ServiceBusOutput("%SvbusInvoiceEmailQueueName%", Connection = "ServiceBusConnectionString")]
        public async Task<ServiceBusMessage[]> Run(
            [TimerTrigger("0 0 27 * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("Timer triggered function executed at: {ExecutionTime}", DateTime.UtcNow);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);
            }

            try
            {
                // Process all active insurances
                var notifications = await _billingService.ProcessAllInsurancesAsync();

                if (!notifications.Any())
                {
                    _logger.LogInformation("No invoices generated requiring notifications.");
                    return Array.Empty<ServiceBusMessage>();
                }

                // Convert notifications to service bus messages using the message factory
                var messages = notifications.Select(notification =>
                    _messageFactory.CreateMessage(
                        notification,
                        notification.InvoiceId.ToString(),
                        $"Invoice Notification for Insurance",
                        _appConfig.SvbusInvoiceEmailQueueName)
                ).ToArray();

                _logger.LogInformation("Sending {Count} email notification messages to the service bus queue {QueueName}",
                    messages.Length, _appConfig.SvbusInvoiceEmailQueueName);

                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing all insurances for billing");
                _telemetryClient.TrackException(ex);
                return Array.Empty<ServiceBusMessage>();
            }
        }
    }
}
