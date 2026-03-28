
using CatalogService.Data;
using CatalogService.Models;
using RabbitMQ.Client;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace CatalogService.Services.Messaging
{
    public class PublisherForReport
    {
        private readonly IConfiguration _configuration;
     

        public PublisherForReport(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendProductForReporting(ProductStatusChangedEvent product)
        {
            // 1. Create a connection factory
            var factory = new ConnectionFactory();
            _configuration.GetSection("RabbitMq").Bind(factory);

            // 2. Open connection and channel
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // 3. Declare a queue (creates it if it doesn't exist)
            await channel.QueueDeclareAsync(
                queue: "report-queue",
                durable: false,      // survives broker restart if true
                exclusive: false,
                autoDelete: false,
                arguments: null
        );


            if (product != null)
            {
                // 4. Publish a message
                string json = JsonSerializer.Serialize(product);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: "",          // default exchange
                    routingKey: "report-queue",
                    body: body
                );

                Console.WriteLine($"Sent: {json}");
            }
            else { Console.WriteLine("No Products To Show"); return; }
        }
    }
}
