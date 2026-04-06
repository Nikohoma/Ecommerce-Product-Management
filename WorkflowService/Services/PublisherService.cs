using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Contracts;
using System.Text;
using System.Text.Json;
using WorkflowService.Exceptions;

public class Publisher
{
    private readonly IConfiguration _configuration;  // was public — encapsulation fix
    private readonly ILogger<Publisher> _logger;

    private const string QueueName = "workflow-queue";

    public Publisher(IConfiguration configuration, ILogger<Publisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(ProductWorkflowEvent message)
    {
        // Guard — validate before any I/O
        ArgumentNullException.ThrowIfNull(message);

        var body = SerializeMessage(message);

        IConnection connection;
        IChannel channel;

        try
        {
            var factory = new ConnectionFactory();
            _configuration.GetSection("RabbitMq").Bind(factory);

            connection = await factory.CreateConnectionAsync();
            channel = await connection.CreateChannelAsync();

            _logger.LogInformation("RabbitMQ connection established for publishing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw new PublisherConnectionException(ex);
        }

        await using var _ = connection;   // async-safe disposal
        await using var __ = channel;

        try
        {
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation("Queue '{QueueName}' declared", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queue '{QueueName}'", QueueName);
            throw new QueueDeclarationException(QueueName, ex);
        }

        try
        {
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                body: body
            );

            _logger.LogInformation(
                "Message published to '{QueueName}' — ProductId: {ProductId}",
                QueueName, message.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to '{QueueName}'", QueueName);
            throw new MessagePublishException(QueueName, ex);
        }
    }

    // Isolated so a serialization bug never touches the network
    private byte[] SerializeMessage(ProductWorkflowEvent message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            _logger.LogDebug("Message serialized: {Json}", json);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize ProductWorkflowEvent");
            throw new MessageSerializationException(ex);
        }
    }
}