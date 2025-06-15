using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Config;
using System.Text.Json;

namespace InsuranceManagementSystem.Functions.NotificationService
{
    public class NotificationServiceFunction
    {
        private readonly ILogger<NotificationServiceFunction> _logger;

        public NotificationServiceFunction(ILogger<NotificationServiceFunction> logger)
        {
            _logger = logger;
        }

        [Function("NotificationService")]
        public async Task Run(
            [ServiceBusTrigger("%SvbusInvoiceEmailQueueName%", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            FunctionContext context)
        {
            _logger.LogInformation("C# ServiceBus queue trigger function processed message: {MessageId}", message.MessageId);

            try
            {
                var messageSubject = message.Subject ?? "No Subject";
                _logger.LogInformation("Processing email notification: {messageSubject}", messageSubject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}: {ErrorMessage}", message.MessageId, ex.Message);
                throw; // Re-throw to trigger retry or dead letter queue
            }
        }
    }
}
