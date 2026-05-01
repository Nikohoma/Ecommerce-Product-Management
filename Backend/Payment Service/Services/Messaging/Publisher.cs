using PaymentService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PaymentService.Services.Messaging
{
    public class Publisher
    {
        private readonly IConnection _connection;
        public Publisher(IConnection connection)
        {
            _connection = connection;
        }
        public async Task PaymentResult(Payment msg)
        {
            using var channel = await _connection.CreateChannelAsync();
            await channel.QueueDeclareAsync("Payment-Result", true, false, false, null);
            var json = JsonSerializer.Serialize(msg);
            var body = Encoding.UTF8.GetBytes(json);

            var prop = new BasicProperties { Persistent=true, ContentType="application/json",MessageId=Guid.NewGuid().ToString(),Timestamp= new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) };

            await channel.BasicPublishAsync("", "Payment-Result", false, prop, body);
            Console.WriteLine("Published Successfully.");
        }
    }
}
