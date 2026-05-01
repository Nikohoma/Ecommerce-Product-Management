using OrderService.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderService.Services.Messaging
{
    public class CartConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly ILogger<CartConsumer> _logger;

        private IChannel _channel;
        private const string QueueName = "cart-checkout-queue";

        public CartConsumer(
            IServiceScopeFactory scopeFactory,
            IConnection connection,
            ILogger<CartConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _connection = connection;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Checkout(stoppingToken);
        }

        public async Task Checkout(CancellationToken stoppingToken)
        {
            try
            {
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("Cart Consumer Started...");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;

                await _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cart Consumer stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cart Consumer failed.");
                throw;
            }
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"Message Received: {message}");

                var checkoutData = JsonSerializer.Deserialize<CartCheckoutDto>(message);

                if (checkoutData == null)
                {
                    _logger.LogWarning("Invalid message format.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                // Create scope for DI services (DbContext, etc.)
                using var scope = _scopeFactory.CreateScope();

                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                await orderService.CreateOrderAsync(checkoutData);

                // ACK after success
                await _channel.BasicAckAsync(ea.DeliveryTag, false);

                _logger.LogInformation("Message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                // Decide retry strategy
                await _channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true); // or false if you don't want infinite retries
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Cart Consumer...");

            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);
        }
    }
}