using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Data;
using OrderService.DTO;
using OrderService.DTOs;
using OrderService.Exceptions;
using OrderService.Models;
using OrderService.Repository;
using OrderService.Services;

namespace OrderService.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<ILogger<OrderService.Services.OrderService>> _mockLogger;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<IOrderRepository> _mockRepo;
        private OrderDbContext _context;
        private OrderService.Services.OrderService _service;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderDbContext(options);
            _mockLogger = new Mock<ILogger<OrderService.Services.OrderService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockRepo = new Mock<IOrderRepository>();

            _service = new OrderService.Services.OrderService(_context, _mockLogger.Object, _mockHttpClientFactory.Object, _mockRepo.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task CreateOrderAsync_ValidDto_AddsOrderToContext()
        {
            // Arrange
            var checkoutDto = new CartCheckoutDto
            {
                UserId = "user123",
                TotalAmount = 100.0m,
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = 1, Quantity = 2, PriceSnapshot = 50.0m }
                }
            };

            // Act
            await _service.CreateOrderAsync(checkoutDto);

            // Assert
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.UserId == "user123");
            Assert.That(order, Is.Not.Null);
            Assert.That(order.TotalAmount, Is.EqualTo(100.0m));
            Assert.That(order.OrderItems.Count, Is.EqualTo(1));
            Assert.That(order.OrderItems[0].ProductId, Is.EqualTo(1));
        }

        [Test]
        public void CreateOrderAsync_NullDto_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateOrderAsync(null!));
        }

        [Test]
        public void CreateOrderAsync_EmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var checkoutDto = new CartCheckoutDto
            {
                UserId = "user123",
                TotalAmount = 100.0m,
                Items = new List<CartItemDto>()
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateOrderAsync(checkoutDto));
        }

        [Test]
        public async Task GetOrdersByUser_ExistingOrders_ReturnsMappedDtos()
        {
            // Arrange
            var userId = "user123";
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    UserId = userId,
                    TotalAmount = 100.0m,
                    Status = "Pending",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 2, Price = 50.0m }
                    }
                }
            };

            _mockRepo.Setup(r => r.GetOrdersByUserAsync(userId)).ReturnsAsync(orders);

            // Act
            var result = await _service.GetOrdersByUser(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            var orderDto = result.First();
            Assert.That(orderDto.Id, Is.EqualTo(1));
            Assert.That(orderDto.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetOrdersByUser_NoOrders_ThrowsNotFoundException()
        {
            // Arrange
            var userId = "user123";
            _mockRepo.Setup(r => r.GetOrdersByUserAsync(userId)).ReturnsAsync(new List<Order>());

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetOrdersByUser(userId));
        }

        [Test]
        public async Task GetOrder_ExistingId_ReturnsMappedDto()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                UserId = "user123",
                TotalAmount = 100.0m,
                Status = "Pending",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Price = 50.0m }
                }
            };

            _mockRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.GetOrder(orderId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(orderId));
        }

        [Test]
        public void GetOrder_NonExistingId_ThrowsNotFoundException()
        {
            // Arrange
            var orderId = 99;
            _mockRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync((Order)null!);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetOrder(orderId));
        }

        [Test]
        public async Task UpdateOrderStatus_ExistingOrder_CallsRepoUpdate()
        {
            // Arrange
            var orderId = 1;
            var status = "Shipped";
            var order = new Order { Id = orderId };

            _mockRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
            _mockRepo.Setup(r => r.UpdateOrderStatusAsync(orderId, status)).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateOrderStatus(orderId, status);

            // Assert
            _mockRepo.Verify(r => r.UpdateOrderStatusAsync(orderId, status), Times.Once);
        }

        [Test]
        public void UpdateOrderStatus_NonExistingOrder_ThrowsNotFoundException()
        {
            // Arrange
            var orderId = 99;
            _mockRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync((Order)null!);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(async () => await _service.UpdateOrderStatus(orderId, "Shipped"));
        }
    }
}
