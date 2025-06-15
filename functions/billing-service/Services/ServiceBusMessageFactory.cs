using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsuranceManagementSystem.Functions.BillingService.Services
{
    /// <summary>
    /// Interface for creating Service Bus messages
    /// </summary>
    public interface IServiceBusMessageFactory
    {
        /// <summary>
        /// Creates a Service Bus message from the provided object
        /// </summary>
        /// <typeparam name="T">Type of the message object</typeparam>
        /// <param name="messageObject">The object to serialize into the message</param>
        /// <param name="messageId">Optional message ID (defaults to new GUID)</param>
        /// <param name="subject">Optional message subject</param>
        /// <param name="queueName">The name of the destination queue</param>
        /// <returns>A properly formatted ServiceBusMessage</returns>
        ServiceBusMessage CreateMessage<T>(T messageObject, string? messageId = null, string? subject = null, string? queueName = null);
    }

    /// <summary>
    /// Factory for creating consistent Service Bus messages
    /// </summary>
    public class ServiceBusMessageFactory : IServiceBusMessageFactory
    {
        private readonly ILogger<ServiceBusMessageFactory> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public ServiceBusMessageFactory(ILogger<ServiceBusMessageFactory> logger)
        {
            _logger = logger;
            _serializerOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Allows HTML characters
                WriteIndented = false
            };
        }

        /// <summary>
        /// Creates a Service Bus message from the provided object
        /// </summary>
        public ServiceBusMessage CreateMessage<T>(T messageObject, string? messageId = null, string? subject = null, string? queueName = null)
        {
            try
            {
                // Serialize the message with consistent options
                var messageBody = JsonSerializer.Serialize(messageObject, _serializerOptions);

                // Create Service Bus message
                var serviceBusMessage = new ServiceBusMessage(messageBody)
                {
                    MessageId = messageId ?? Guid.NewGuid().ToString(),
                    ContentType = "application/json",
                    Subject = subject ?? typeof(T).Name
                };

                // Add metadata properties
                serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).Name;
                serviceBusMessage.ApplicationProperties["CreatedAt"] = DateTimeOffset.UtcNow.ToString("O");

                if (!string.IsNullOrEmpty(queueName))
                {
                    serviceBusMessage.ApplicationProperties["QueueName"] = queueName;
                }

                return serviceBusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Service Bus message for {MessageType}", typeof(T).Name);
                throw;
            }
        }
    }
}
