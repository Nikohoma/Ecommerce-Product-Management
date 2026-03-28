using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportingService.Models;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

public class ReportConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    private IConnection _connection;
    private IChannel _channel;

    public ReportConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        //var factory = new RabbitMQ.Client.IConnectionFactory();
        RabbitMQ.Client.IConnectionFactory factory = new RabbitMQ.Client.ConnectionFactory();
        _configuration.GetSection("RabbitMq").Bind(factory);

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: "report-queue",
            durable: false,
            exclusive: false,
            autoDelete: false
        );

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Console.WriteLine("Reporting Consumer started");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            Console.WriteLine($"Received: {json}");

            var message = JsonSerializer.Deserialize<ProductStatusChangedEvent>(json);

            if (message == null)
                return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

            try
            {
                var report = new ProductReport
                {
                    ProductId = message.ProductId,
                    Status = message.Status,
                    UpdatedAt = message.UpdatedAt,
                    Price = message.Price
                };

                db.ProductReports.Add(report);
                await db.SaveChangesAsync();

                Console.WriteLine("Report saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "report-queue",
            autoAck: true,
            consumer: consumer
        );
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}