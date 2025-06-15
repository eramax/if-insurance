using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Generic interface for sending messages to Azure Service Bus queues
/// </summary>
public interface IServiceBusMessagingService
{
    /// <summary>
    /// Sends a message to the specified Service Bus queue
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message object</param>
    /// <param name="queueName">The queue name</param>
    /// <param name="messageSubject">Optional message subject</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendMessageAsync<T>(T message, string queueName, string? messageSubject = null, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Generic Service Bus messaging service for sending messages to Azure Service Bus queues
/// Implements secure messaging using connection string authentication
/// </summary>
public class ServiceBusMessagingService : IServiceBusMessagingService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusMessagingService> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders;

    public ServiceBusMessagingService(
        IConfiguration configuration,
        ILogger<ServiceBusMessagingService> logger)
    {
        _logger = logger;
        _senders = new Dictionary<string, ServiceBusSender>();

        try
        {
            // Get Service Bus connection string from configuration
            var serviceBusConnectionString = configuration["ServiceBusConnectionString"];

            if (string.IsNullOrEmpty(serviceBusConnectionString))
            {
                throw new InvalidOperationException("ServiceBusConnectionString must be configured in application settings");
            }

            // Initialize Service Bus client with connection string
            _client = new ServiceBusClient(serviceBusConnectionString);

            _logger.LogInformation("Service Bus messaging service initialized successfully with connection string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Service Bus messaging service");
            throw;
        }
    }

    /// <summary>
    /// Sends a generic message to the specified Service Bus queue
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message object to send</param>
    /// <param name="queueName">The name of the queue to send the message to</param>
    /// <param name="messageSubject">Optional message subject for filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SendMessageAsync<T>(T message, string queueName, string? messageSubject = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        try
        {
            // Get or create sender for the queue
            var sender = GetOrCreateSender(queueName);

            // Serialize the message with ReferenceHandler.Preserve to handle cycles
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                WriteIndented = false
            };
            var messageBody = JsonSerializer.Serialize(message, options);

            // Create Service Bus message
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json",
                Subject = messageSubject ?? typeof(T).Name
            };

            // Add metadata properties
            serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).Name;
            serviceBusMessage.ApplicationProperties["CreatedAt"] = DateTimeOffset.UtcNow.ToString("O");
            serviceBusMessage.ApplicationProperties["QueueName"] = queueName;

            // Send the message
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation(
                "Message sent successfully to queue '{QueueName}'. MessageId: {MessageId}, MessageType: {MessageType}",
                queueName,
                serviceBusMessage.MessageId,
                typeof(T).Name);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex,
                "Failed to send message to queue '{QueueName}'. MessageType: {MessageType}, Error: {ErrorReason}",
                queueName,
                typeof(T).Name,
                ex.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending message to queue '{QueueName}'. MessageType: {MessageType}",
                queueName,
                typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets an existing sender or creates a new one for the specified queue
    /// </summary>
    /// <param name="queueName">The queue name</param>
    /// <returns>Service Bus sender for the queue</returns>
    private ServiceBusSender GetOrCreateSender(string queueName)
    {
        if (_senders.TryGetValue(queueName, out var existingSender))
        {
            return existingSender;
        }

        var newSender = _client.CreateSender(queueName);
        _senders[queueName] = newSender;

        _logger.LogDebug("Created new Service Bus sender for queue: {QueueName}", queueName);

        return newSender;
    }

    /// <summary>
    /// Dispose of Service Bus resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            // Dispose all senders
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }
            _senders.Clear();

            // Dispose client
            if (_client != null)
            {
                await _client.DisposeAsync();
            }

            _logger.LogInformation("Service Bus messaging service disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Service Bus messaging service");
        }
    }
}
