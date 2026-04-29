using CatalogService.Data;
using CatalogService.DTO.Products;
using CatalogService.Exceptions;
using CatalogService.Models;
using CatalogService.Repositories;
using CatalogService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ProductService.Tests
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<IProductRepository> _repoMock;
        private Mock<ILogger<CatalogService.Services.ProductService>> _loggerMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private ProductDbContext _context;
        private CatalogService.Services.ProductService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _context = new ProductDbContext(options, _httpContextAccessorMock.Object);
            _repoMock = new Mock<IProductRepository>();
            _loggerMock = new Mock<ILogger<CatalogService.Services.ProductService>>();

            _service = new CatalogService.Services.ProductService(_repoMock.Object, _context, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateProduct_WithValidData_ShouldCallRepository()
        {
            // Arrange
            var categoryId = 1;
            _context.Categories.Add(new Category { Id = categoryId, Name = "Electronics" });
            await _context.SaveChangesAsync();

            var dto = new ProductCreateDto
            {
                Name = "Test Product",
                Description = "Description",
                Price = 100,
                Stock = 10,
                CategoryId = categoryId,
                MediaUrls = new List<string> { "http://image.com" },
                Tags = new List<string> { "tag1" }
            };

            // Act
            await _service.CreateProduct(dto);

            // Assert
            _repoMock.Verify(r => r.CreateProductAsync(It.Is<Product>(p => 
                p.Name == dto.Name && 
                p.CategoryId == dto.CategoryId &&
                p.Tags.Contains("tag1")
            )), Times.Once);
        }

        [Test]
        public void CreateProduct_WithInvalidCategory_ShouldThrowException()
        {
            // Arrange
            var dto = new ProductCreateDto
            {
                Name = "Test Product",
                CategoryId = 999, // Non-existent
                MediaUrls = new List<string>(),
                Tags = new List<string>()
            };

            // Act & Assert
            Assert.ThrowsAsync<CategoryNotFoundException>(async () => await _service.CreateProduct(dto));
        }

        [Test]
        public async Task GetProductDetails_ShouldReturnProductFromRepository()
        {
            // Arrange
            var productId = 1;
            var expectedProduct = new Product { Id = productId, Name = "Test" };
            _repoMock.Setup(r => r.GetProductDetailsAsync(productId)).ReturnsAsync(expectedProduct);

            // Act
            var result = await _service.GetProductDetails(productId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProduct));
            Assert.That(result.Name, Is.EqualTo("Test"));
        }

        [Test]
        public async Task SearchProduct_ShouldReturnListFromRepository()
        {
            // Arrange
            var query = "phone";
            var mockList = new List<Product> { new Product { Name = "iPhone" } };
            _repoMock.Setup(r => r.SearchProductAsync(query)).ReturnsAsync(mockList);

            // Act
            var result = await _service.SearchProduct(query);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("iPhone"));
        }
    }
}
