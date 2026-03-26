using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

public class Publisher
{
    public readonly IConfiguration _configuration;

    public Publisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task Publish(ProductWorkflowEvent message)
    {
        var factory = new ConnectionFactory();
        _configuration.GetSection("RabbitMq").Bind(factory);

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "workflow-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
            );
        if (message != null)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            try
            {
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "workflow-queue",
                    body: body
                );

                Console.WriteLine($"Sent: {json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Publish failed: {ex.Message}");
            }
        }
    }

}