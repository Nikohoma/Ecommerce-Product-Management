using CartService.DTOs;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CartService.Services.Messaging
{
    public interface IOrderPublisher
    {
        Task SendCartToOrderAsync(CartCheckoutDto cart);
    }

    public class OrderPublisher : IOrderPublisher
    {
        private readonly IConnection _connection;
        private readonly ILogger<OrderPublisher> _logger;  // fix: matches constructor
        private const string QueueName = "cart-checkout-queue";

        public OrderPublisher(IConnection connection, ILogger<OrderPublisher> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task SendCartToOrderAsync(CartCheckoutDto cart)
        {
            if (cart is null)
            {
                _logger.LogWarning("SendCartToOrderAsync received null cart. Skipping publish.");
                return;
            }

            try
            {
                using var channel = await _connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var json = JsonSerializer.Serialize(cart);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QueueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );
                //foreach(var c in cart.Items) { Console.WriteLine(c); }
                _logger.LogInformation("Cart {CartId} published to queue {Queue} with MessageId {MessageId}",cart.CartId, QueueName, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish cart {CartId} to queue {Queue}",
                    cart.CartId, QueueName);
                throw;
            }
        }
    }
}