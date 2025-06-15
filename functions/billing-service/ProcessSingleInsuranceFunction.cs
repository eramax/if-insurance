using Azure.Messaging.ServiceBus;
using InsuranceManagementSystem.Functions.BillingService.Config;
using InsuranceManagementSystem.Functions.BillingService.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace InsuranceManagementSystem.Functions.BillingService
{    /// <summary>
     /// Function for processing a single insurance
     /// </summary>
    public class ProcessSingleInsuranceFunction
    {
        private readonly ILogger<ProcessSingleInsuranceFunction> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly IBillingService _billingService;
        private readonly AppConfig _appConfig; private readonly IServiceBusMessageFactory _messageFactory;

        public ProcessSingleInsuranceFunction(
            ILogger<ProcessSingleInsuranceFunction> logger,
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
        /// Function triggered by service bus message to process a single insurance
        /// </summary>
        [Function("ProcessSingleInsurance")]
        [ServiceBusOutput("%SvbusInvoiceEmailQueueName%", Connection = "ServiceBusConnectionString")]
        public async Task<ServiceBusMessage?> Run(
            [ServiceBusTrigger("%SvbusInvoiceGenQueueName%", Connection = "ServiceBusConnectionString")]
            InvoiceGenerationMessage message)
        {
            _logger.LogInformation("Service bus triggered function executed for insurance {InsuranceId} at {ExecutionTime}",
                message.VehicleInsuranceId, DateTime.UtcNow);

            try
            {
                // Process the single insurance
                var notification = await _billingService.ProcessSingleInsuranceAsync(message);

                if (notification == null)
                {
                    _logger.LogInformation("No invoice generated for insurance {InsuranceId}", message.VehicleInsuranceId);
                    return null;
                }                
                // Use the message factory to create a consistent Service Bus message
                var serviceBusMessage = _messageFactory.CreateMessage(
                    notification,
                    notification.InvoiceId.ToString(),
                    $"Invoice Notification for Insurance {message.VehicleInsuranceId}",
                    _appConfig.SvbusInvoiceEmailQueueName);

                _logger.LogInformation("Sending email notification message to the service bus queue {QueueName} for insurance {InsuranceId}",
                    _appConfig.SvbusInvoiceEmailQueueName, message.VehicleInsuranceId);

                return serviceBusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing insurance {InsuranceId} for billing", message.VehicleInsuranceId);
                _telemetryClient.TrackException(ex);
                return null;
            }
        }
    }
}
