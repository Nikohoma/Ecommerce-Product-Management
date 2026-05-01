using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Data;
using OrderService.Models;
using OrderService.Repository;

namespace OrderService.Tests
{
    [TestFixture]
    public class OrderRepositoryTests
    {
        private OrderDbContext _context;
        private Mock<ILogger<OrderRepository>> _mockLogger;
        private OrderRepository _repository;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderDbContext(options);
            _mockLogger = new Mock<ILogger<OrderRepository>>();
            _repository = new OrderRepository(_context, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task CreateOrderAsync_ValidOrder_SavesToDatabase()
        {
            // Arrange
            var order = new Order
            {
                UserId = "user1",
                TotalAmount = 50.0m,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.CreateOrderAsync(order);

            // Assert
            Assert.That(result.Id, Is.GreaterThan(0));
            var savedOrder = await _context.Orders.FindAsync(result.Id);
            Assert.That(savedOrder, Is.Not.Null);
            Assert.That(savedOrder!.UserId, Is.EqualTo("user1"));
        }

        [Test]
        public async Task GetOrderByIdAsync_ExistingId_ReturnsOrderWithItems()
        {
            // Arrange
            var order = new Order
            {
                UserId = "user1",
                Status = "Pending",
                OrderItems = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, Price = 10.0m } }
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOrderByIdAsync(order.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.OrderItems, Is.Not.Empty);
            Assert.That(result.OrderItems.First().ProductId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetOrdersByUserAsync_ExistingUser_ReturnsOrders()
        {
            // Arrange
            var userId = "user1";
            _context.Orders.AddRange(new List<Order>
            {
                new Order { UserId = userId, Status = "Pending", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                new Order { UserId = userId, Status = "Pending", CreatedAt = DateTime.UtcNow },
                new Order { UserId = "other", Status = "Pending", CreatedAt = DateTime.UtcNow }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOrdersByUserAsync(userId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.First().CreatedAt, Is.GreaterThan(result.Last().CreatedAt)); // Ordered by descending
        }

        [Test]
        public async Task UpdateOrderStatusAsync_ExistingOrder_UpdatesStatus()
        {
            // Arrange
            var order = new Order { UserId = "user1", Status = "Pending" };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateOrderStatusAsync(order.Id, "Completed");

            // Assert
            var updatedOrder = await _context.Orders.FindAsync(order.Id);
            Assert.That(updatedOrder!.Status, Is.EqualTo("Completed"));
        }
    }
}
