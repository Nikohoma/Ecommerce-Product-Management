using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using System.Text;
using System.Text.Json;
using CatalogService.Repositories;
using CatalogService.Exceptions;

public class WorkflowConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly ILogger<WorkflowConsumer> _logger;

    private IChannel _channel;

    public WorkflowConsumer(IServiceScopeFactory scopeFactory, IConnection connection, ILogger<WorkflowConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;        
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: "workflow-queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation("WorkflowConsumer started, listening on 'workflow-queue'.");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += HandleMessageAsync;

            await _channel.BasicConsumeAsync(
                queue: "workflow-queue",
                autoAck: false,          
                consumer: consumer
            );

            // Keep the background service alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "WorkflowConsumer failed to start or lost connection.");
            throw;   // Crash the host — a consumer that can't start should not run silently
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        _logger.LogInformation("Message received on 'workflow-queue': {Json}.", json);

        ProductWorkflowEvent message;

        try
        {
            message = JsonSerializer.Deserialize<ProductWorkflowEvent>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message — dead-lettering. Payload: {Json}.", json);
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Deserialized message is null — skipping. Payload: {Json}.", json);
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var action = message.Action?.ToLower();

            switch (action)
            {
                case "submit":
                    await repo.SubmitProduct(message.ProductId);
                    _logger.LogInformation("Product {ProductId} submitted via workflow.", message.ProductId);
                    break;

                case "approve":
                    await repo.ApproveProductAsync(message.ProductId);
                    _logger.LogInformation("Product {ProductId} approved via workflow.", message.ProductId);
                    break;

                case "reject":
                    await repo.RejectProductAsync(message.ProductId);
                    _logger.LogInformation("Product {ProductId} rejected via workflow.", message.ProductId);
                    break;

                default:
                    _logger.LogWarning("Unknown workflow action '{Action}' for Product {ProductId} — discarding.",
                        message.Action, message.ProductId);
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
            }

            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (CatalogException ex)
        {
            // Domain exceptions (wrong status, not found, etc.) : don't requeue, the same message will fail again
            _logger.LogWarning(ex, "Domain error processing workflow action '{Action}' for Product {ProductId} — discarding message.",
                message.Action, message.ProductId);
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // Infrastructure failures (DB down, timeout), requeue so it retries
            _logger.LogError(ex, "Unexpected error processing workflow action '{Action}' for Product {ProductId} — requeueing.",
                message.Action, message.ProductId);
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WorkflowConsumer stopping.");

        if (_channel is not null)
            await _channel.CloseAsync();

        // Do NOT close _connection here — it's a shared singleton managed by DI
        await base.StopAsync(cancellationToken);
    }
}