using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportingService.Exceptions;
using ReportingService.Models;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

public class ReportConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "report-queue";

    public ReportConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ReportConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            IConnectionFactory factory = new ConnectionFactory();
            _configuration.GetSection("RabbitMq").Bind(factory);

            _connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ connection");
            throw new RabbitMqConnectionException(ex);
        }

        try
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("Queue '{QueueName}' declared successfully", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queue '{QueueName}'", QueueName);
            throw new QueueDeclarationException(QueueName, ex);
        }

        await base.StartAsync(cancellationToken);
    }

    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    _logger.LogInformation("ReportConsumer started, listening on '{QueueName}'", QueueName);

    //    var consumer = new AsyncEventingBasicConsumer(_channel);

    //    consumer.ReceivedAsync += async (sender, eventArgs) =>
    //    {
    //        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
    //        _logger.LogDebug("Raw message received: {Json}", json);

    //        ProductStatusChangedEvent? message;

    //        try
    //        {
    //            message = JsonSerializer.Deserialize<ProductStatusChangedEvent>(json);

    //            if (message == null)
    //            {
    //                _logger.LogWarning("Deserialized message was null — skipping. Payload: {Json}", json);
    //                return;
    //            }
    //        }
    //        catch (JsonException ex)
    //        {
    //            _logger.LogError(ex, "Message deserialization failed. Payload: {Json}", json);
    //            throw new MessageDeserializationException(ex);
    //            // Do not ack — let dead-letter or retry policy handle it
    //        }

    //        await PersistReportAsync(message);
    //    };

    //    await _channel.BasicConsumeAsync(
    //        queue: QueueName,
    //        autoAck: true,
    //        consumer: consumer
    //    );

    //    // Keep ExecuteAsync alive until the host requests shutdown
    //    await Task.Delay(Timeout.Infinite, stoppingToken);
    //}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReportConsumer started, listening on '{QueueName}'", QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogDebug("Raw message received: {Json}", json);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("ActivityType", out _))
                {
                    var activity = JsonSerializer.Deserialize<ProductActivityEvent>(json);
                    if (activity is null)
                    {
                        _logger.LogWarning("Deserialized ProductActivityEvent was null — skipping. Payload: {Json}", json);
                        return;
                    }

                    await PersistActivityAsync(activity);
                }
                else
                {
                    var status = JsonSerializer.Deserialize<ProductStatusChangedEvent>(json);
                    if (status is null)
                    {
                        _logger.LogWarning("Deserialized ProductStatusChangedEvent was null — skipping. Payload: {Json}", json);
                        return;
                    }

                    await PersistReportAsync(status);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Message deserialization failed. Payload: {Json}", json);
                throw new MessageDeserializationException(ex);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: true,
            consumer: consumer
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("ReportConsumer is shutting down gracefully.");
        }
    }

    private async Task PersistReportAsync(ProductStatusChangedEvent message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

            var report = new ProductReport
            {
                ProductId = message.ProductId,
                Status = message.Status,
                UpdatedAt = message.UpdatedAt,
                Price = message.Price
            };

            db.ProductReports.Add(report);
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Report saved — ProductId: {ProductId}, Status: {Status}, UpdatedAt: {UpdatedAt}",
                message.ProductId, message.Status, message.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist report for ProductId {ProductId}",
                message.ProductId);
            throw new ReportPersistenceException(message.ProductId, ex);
        }
    }

    private async Task PersistActivityAsync(ProductActivityEvent message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

            var activity = new ProductActivity
            {
                ProductId = message.ProductId,
                ActivityType = message.ActivityType,
                UpdatedAt = message.UpdatedAt,
                OldPrice = message.OldPrice,
                NewPrice = message.NewPrice,
                OldStock = message.OldStock,
                NewStock = message.NewStock,
                MediaUrl = message.MediaUrl
            };

            db.ProductActivities.Add(activity);
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Activity saved — ProductId: {ProductId}, Type: {Type}, UpdatedAt: {UpdatedAt}",
                message.ProductId, message.ActivityType, message.UpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist activity for ProductId {ProductId}",
                message.ProductId);
            throw new ReportPersistenceException(message.ProductId, ex);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}