using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shared.Contracts;
using WorkflowService.Data;
using WorkflowService.Model;
using WorkflowServices.Services;

namespace WorkflowService.Tests
{
    [TestFixture]
    public class WorkflowServiceTests
    {
        private Mock<IPublisher> _publisherMock;
        private Mock<ILogger<WorkflowServices.Services.WorkflowService>> _loggerMock;
        private WorkflowDbContext _context;
        private WorkflowServices.Services.WorkflowService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WorkflowDbContext(options);
            _publisherMock = new Mock<IPublisher>();
            _loggerMock = new Mock<ILogger<WorkflowServices.Services.WorkflowService>>();

            _service = new WorkflowServices.Services.WorkflowService(_publisherMock.Object, _context, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task SubmitAsync_ShouldPublishEventAndLogEntry()
        {
            // Arrange
            var productId = 101;
            var userName = "TestUser";

            // Act
            await _service.SubmitAsync(productId, userName);

            // Assert
            _publisherMock.Verify(p => p.PublishAsync(It.Is<ProductWorkflowEvent>(e => 
                e.ProductId == productId && e.Action == "Submit")), Times.Once);

            var logEntry = await _context.WorkflowDb.FirstOrDefaultAsync(w => w.productId == productId);
            Assert.That(logEntry, Is.Not.Null);
            Assert.That(logEntry.newStatus, Is.EqualTo("Submit"));
            Assert.That(logEntry.Name, Is.EqualTo(userName));
        }

        [Test]
        public async Task ApproveAsync_ShouldUsePreviousStatusFromLog()
        {
            // Arrange
            var productId = 202;
            var userName = "AdminUser";
            
            _context.WorkflowDb.Add(new Workflow 
            { 
                productId = productId, 
                newStatus = "Submit", 
                Date = DateTime.UtcNow.AddMinutes(-10),
                Name = "OtherUser",
                oldStatus = "Draft"
            });
            await _context.SaveChangesAsync();

            // Act
            await _service.ApproveAsync(productId, userName);

            // Assert
            _publisherMock.Verify(p => p.PublishAsync(It.Is<ProductWorkflowEvent>(e => 
                e.ProductId == productId && e.Action == "Approve")), Times.Once);

            var logEntry = await _context.WorkflowDb
                .OrderByDescending(w => w.Date)
                .FirstOrDefaultAsync(w => w.productId == productId);
            
            Assert.That(logEntry, Is.Not.Null);
            Assert.That(logEntry.oldStatus, Is.EqualTo("Submit"));
            Assert.That(logEntry.newStatus, Is.EqualTo("Approve"));
        }

        [Test]
        public async Task RejectAsync_ShouldPublishAndLog()
        {
            // Arrange
            var productId = 303;
            var userName = "Reviewer";

            // Act
            await _service.RejectAsync(productId, userName);

            // Assert
            _publisherMock.Verify(p => p.PublishAsync(It.Is<ProductWorkflowEvent>(e => 
                e.ProductId == productId && e.Action == "Reject")), Times.Once);

            var logEntry = await _context.WorkflowDb.FirstOrDefaultAsync(w => w.productId == productId);
            Assert.That(logEntry, Is.Not.Null);
            Assert.That(logEntry.newStatus, Is.EqualTo("Reject"));
        }
    }
}
