using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Tests
{
    [TestFixture]
    public class OrderControllerTests
    {
        private Mock<IOrderService> _mockService;
        private Mock<ILogger<OrderController>> _mockLogger;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private OrderDbContext _context;
        private OrderController _controller;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderDbContext(options);
            _mockService = new Mock<IOrderService>();
            _mockLogger = new Mock<ILogger<OrderController>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _controller = new OrderController(_mockService.Object, _mockLogger.Object, _mockHttpClientFactory.Object, _context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetOrdersByUser_ReturnsOkWithOrders()
        {
            // Arrange
            var userId = "user1";
            var orders = new List<OrderResponseDto> { new OrderResponseDto { Id = 1, UserId = userId } };
            _mockService.Setup(s => s.GetOrdersByUser(userId)).ReturnsAsync(orders);

            // Act
            var result = await _controller.GetOrdersByUser(userId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(orders));
        }

        [Test]
        public async Task GetOrder_ReturnsOkWithOrder()
        {
            // Arrange
            var orderId = 1;
            var order = new OrderResponseDto { Id = orderId };
            _mockService.Setup(s => s.GetOrder(orderId)).ReturnsAsync(order);

            // Act
            var result = await _controller.GetOrder(orderId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(order));
        }

        [Test]
        public async Task UpdateOrderStatus_ReturnsTrue()
        {
            // Arrange
            var orderId = 1;
            var status = "Shipped";
            _mockService.Setup(s => s.UpdateOrderStatus(orderId, status)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateOrderStatus(orderId, status);

            // Assert
            Assert.That(result, Is.True);
            _mockService.Verify(s => s.UpdateOrderStatus(orderId, status), Times.Once);
        }

        [Test]
        public async Task ProceedToPayment_OrderNotFound_ReturnsNotFound()
        {
            // Act
            var result = await _controller.ProceedToPayment(99);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ProceedToPayment_OrderNotPending_ReturnsBadRequest()
        {
            // Arrange
            var order = new Order { Id = 1, Status = "Completed", UserId = "user1" };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ProceedToPayment(1);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task ProceedToPayment_PaymentServiceSuccess_ReturnsOk()
        {
            // Arrange
            var order = new Order { Id = 1, Status = "Pending", TotalAmount = 100m, UserId = "user1" };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var paymentResponse = new PaymentResponseDto { Id = "tx123", Amount = 100 };
            
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(paymentResponse)
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://paymentservice")
            };

            _mockHttpClientFactory.Setup(_ => _.CreateClient("PaymentService")).Returns(httpClient);

            // Act
            var result = await _controller.ProceedToPayment(1);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as PaymentResponseDto;
            Assert.That(response!.Id, Is.EqualTo("tx123"));
            Assert.That(response.Amount, Is.EqualTo(100));
        }
    }
}
