using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using System.Text;
using System.Text.Json;
using CatalogService.Repositories;

public class WorkflowConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;  //To safely use scoped services (like DbContext) inside a singleton BackgroundService
    private readonly IConfiguration _configuration;

    private RabbitMQ.Client.IConnection _connection;
    private IChannel _channel;

    public WorkflowConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory();
        _configuration.GetSection("RabbitMq").Bind(factory);

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "workflow-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        Console.WriteLine("Async Consumer started...");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            Console.WriteLine($"Received: {json}");

            var message = JsonSerializer.Deserialize<ProductWorkflowEvent>(json);
            if (message == null) return;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var action = message.Action.ToLower();

            try
            {
                if (action == "submit")
                    await repo.SubmitProduct(message.ProductId);

                else if (action == "approve")
                    await repo.ApproveProductAsync(message.ProductId);

                else if (action == "reject")
                    await repo.RejectProductAsync(message.ProductId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "workflow-queue",
            autoAck: true,
            consumer: consumer
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}