using RabbitMQ.Client;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace CatalogService.Services.Messaging
{
    public class PublisherForReport:IPublisherForReport
    {
        private readonly IConnection _connection;
        private readonly ILogger<PublisherForReport> _logger;

        public PublisherForReport(IConnection connection, ILogger<PublisherForReport> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task SendProductForReporting(ProductStatusChangedEvent product)
        {
            if (product is null)
            {
                _logger.LogWarning("SendProductForReporting called with null event — skipping publish.");
                return;
            }

            try
            {
                // Channel is lightweight — create one per publish, not one per app
                using var channel = await _connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: "report-queue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var json = JsonSerializer.Serialize(product);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "report-queue",
                    body: body
                );

                _logger.LogInformation("Published ProductStatusChangedEvent for Product {ProductId} — Status: {Status}.",
                    product.ProductId, product.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish ProductStatusChangedEvent for Product {ProductId}.", product.ProductId);
                throw;
            }
        }

        public async Task SendProductActivityForReporting(ProductActivityEvent evt)
        {
            if (evt is null)
            {
                _logger.LogWarning("SendProductActivityForReporting called with null event — skipping publish.");
                return;
            }

            try
            {
                using var channel = await _connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: "report-queue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var json = JsonSerializer.Serialize(evt);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "report-queue",
                    body: body
                );

                _logger.LogInformation(
                    "Published ProductActivityEvent for Product {ProductId} — Type: {Type}.",
                    evt.ProductId, evt.ActivityType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish ProductActivityEvent for Product {ProductId}.", evt.ProductId);
                throw;
            }
        }
    }
}