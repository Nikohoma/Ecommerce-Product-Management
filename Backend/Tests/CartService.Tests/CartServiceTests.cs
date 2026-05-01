using System.Net;
using System.Text;
using System.Text.Json;
using CartService.DTOs;
using CartService.Exceptions;
using CartService.Models;
using CartService.Repositories;
using CartService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace CartService.Tests
{
    [TestFixture]
    public class CartServiceTests
    {
        private Mock<ICartRepository> _cartRepoMock;
        private Mock<IHttpClientFactory> _httpFactoryMock;
        private Mock<ILogger<cartService>> _loggerMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private cartService _service;

        [SetUp]
        public void Setup()
        {
            _cartRepoMock = new Mock<ICartRepository>();
            _httpFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<cartService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _service = new cartService(
                _cartRepoMock.Object, 
                _httpFactoryMock.Object, 
                _loggerMock.Object, 
                _httpContextAccessorMock.Object);
        }

        [Test]
        public async Task GetCartAsync_ShouldReturnCart_WhenFound()
        {
            var userId = "user123";
            var expectedCart = new Models.Cart { UserId = userId, Items = new List<CartItem>() };
            _cartRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(expectedCart);

            var result = await _service.GetCartAsync(userId);

            Assert.That(result, Is.EqualTo(expectedCart));
        }

        [Test]
        public void GetCartAsync_ShouldThrowNotFound_WhenMissing()
        {
            var userId = "missing";
            _cartRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Models.Cart)null);

            Assert.ThrowsAsync<CartNotFoundException>(async () => await _service.GetCartAsync(userId));
        }

        [Test]
        public async Task AddItemAsync_ShouldAddNewItem_WhenProductExists()
        {
            var userId = "user1";
            var productId = 101;
            var cart = new Models.Cart { Id = 1, UserId = userId, Status = CartStatus.Active, Items = new List<CartItem>() };
            
            _cartRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(cart);
            
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ \"id\": 101, \"name\": \"Test\", \"price\": 50 }", Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://catalog/") };
            _httpFactoryMock.Setup(f => f.CreateClient("Catalog")).Returns(httpClient);

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer token";
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

            _cartRepoMock.Setup(r => r.AddItemAsync(It.IsAny<CartItem>()))
                .ReturnsAsync((CartItem item) => { item.Id = 1; return item; });

            var result = await _service.AddItemAsync(userId, productId, 2);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ProductId, Is.EqualTo(productId));
            Assert.That(result.Quantity, Is.EqualTo(2));
            Assert.That(result.PriceSnapshot, Is.EqualTo(50));
            _cartRepoMock.Verify(r => r.AddItemAsync(It.Is<CartItem>(i => i.ProductId == productId)), Times.Once);
        }

        [Test]
        public void AddItemAsync_ShouldThrow_WhenQuantityIsInvalid()
        {
            Assert.ThrowsAsync<InvalidQuantityException>(async () => await _service.AddItemAsync("u", 1, 0));
        }
    }
}
