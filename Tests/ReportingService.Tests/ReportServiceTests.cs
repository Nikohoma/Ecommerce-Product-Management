using ReportingService.DTO;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ReportingService.Models;
using ReportingService.Repository;
using ReportingService.Services;
using ReportingService.Exceptions;

namespace ReportingService.Tests
{
    [TestFixture]
    public class ReportServiceTests
    {
        private Mock<IReportRepository> _repoMock;
        private Mock<ILogger<ReportService>> _loggerMock;
        private ReportService _service;

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IReportRepository>();
            _loggerMock = new Mock<ILogger<ReportService>>();
            _service = new ReportService(_repoMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetDashboardAsync_ShouldReturnCorrectAggregates()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(10);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(2);
            _repoMock.Setup(r => r.GetPendingCountAsync()).ReturnsAsync(3);
            _repoMock.Setup(r => r.GetTotalInventoryValueAsync()).ReturnsAsync(5000.50m);

            // Act
            var result = await _service.GetDashboardAsync();

            // Assert
            Assert.That(result.Approved, Is.EqualTo(10));
            Assert.That(result.Rejected, Is.EqualTo(2));
            Assert.That(result.Pending, Is.EqualTo(3));
            Assert.That(result.TotalInventoryValue, Is.EqualTo(5000.50m));
        }

        [Test]
        public async Task GetApprovalRateAsync_ShouldReturnCorrectPercentage()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(8);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(2); // total 10

            // Act
            var rate = await _service.GetApprovalRateAsync();

            // Assert
            Assert.That(rate, Is.EqualTo(80.0));
        }

        [Test]
        public async Task GetApprovalRateAsync_WhenNoReports_ShouldReturnZero()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ReturnsAsync(0);
            _repoMock.Setup(r => r.GetRejectedCountAsync()).ReturnsAsync(0);

            // Act
            var rate = await _service.GetApprovalRateAsync();

            // Assert
            Assert.That(rate, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRecentReportsAsync_ShouldFilterByDate()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var reports = new List<ProductReport>
            {
                new ProductReport { Id = 1, UpdatedAt = now.AddDays(-2) }, // recent
                new ProductReport { Id = 2, UpdatedAt = now.AddDays(-10) } // old
            };
            _repoMock.Setup(r => r.GetAllReportsAsync()).ReturnsAsync(reports);

            // Act
            var recent = await _service.GetRecentReportsAsync();

            // Assert
            Assert.That(recent.Count, Is.EqualTo(1));
            Assert.That(recent[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void GetDashboardAsync_WhenRepositoryThrows_ShouldThrowAggregationException()
        {
            // Arrange
            _repoMock.Setup(r => r.GetApprovedCountAsync()).ThrowsAsync(new Exception("DB Failure"));

            // Act & Assert
            Assert.ThrowsAsync<DashboardAggregationException>(async () => await _service.GetDashboardAsync());
        }
    }
}
