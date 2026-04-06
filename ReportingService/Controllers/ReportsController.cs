using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ReportingService.Exceptions;
using ReportingService.Services;

namespace ReportingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,ProductManager")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService service, ILogger<ReportsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var approved = await _service.GetApprovedCountAsync();
                var rejected = await _service.GetRejectedCountAsync();
                var pending = await _service.GetPendingCountAsync();
                var totalValue = await _service.GetTotalInventoryValueAsync();
                var avgPrice = await _service.GetAveragePriceAsync();

                _logger.LogInformation("Dashboard requested — Approved: {Approved}, Rejected: {Rejected}, " +"Pending: {Pending}, TotalValue: {TotalValue}, AvgPrice: {AvgPrice}",approved, rejected, pending, totalValue, avgPrice);

                return Ok(new
                {
                    Approved = approved,
                    Rejected = rejected,
                    Pending = pending,
                    TotalInventoryValue = totalValue,
                    AveragePrice = avgPrice
                });
            }
            catch (EmptyReportSetException ex)
            {
                _logger.LogWarning(ex, "Dashboard requested but no report data exists");
                return Ok(new
                {
                    Approved = 0,
                    Rejected = 0,
                    Pending = 0,
                    TotalInventoryValue = 0m,
                    AveragePrice = 0m
                });
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error building dashboard");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error building dashboard");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("approved-count")]
        public async Task<IActionResult> GetApprovedCount()
        {
            try
            {
                var count = await _service.GetApprovedCountAsync();
                _logger.LogInformation("Approved count requested: {Count}", count);
                return Ok(count);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching approved count");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching approved count");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("rejected-count")]
        public async Task<IActionResult> GetRejectedCount()
        {
            try
            {
                var count = await _service.GetRejectedCountAsync();
                _logger.LogInformation("Rejected count requested: {Count}", count);
                return Ok(count);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching rejected count");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching rejected count");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                var count = await _service.GetPendingCountAsync();
                _logger.LogInformation("Pending count requested: {Count}", count);
                return Ok(count);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching pending count");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching pending count");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("total-value")]
        public async Task<IActionResult> GetTotalValue()
        {
            try
            {
                var value = await _service.GetTotalInventoryValueAsync();
                _logger.LogInformation("Total inventory value requested: {Value}", value);
                return Ok(value);
            }
            catch (EmptyReportSetException ex)
            {
                _logger.LogWarning(ex, "Total value requested but no reports exist");
                return Ok(0m);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching total inventory value");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching total inventory value");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("average-price")]
        public async Task<IActionResult> GetAveragePrice()
        {
            try
            {
                var avg = await _service.GetAveragePriceAsync();
                _logger.LogInformation("Average price requested: {Average}", avg);
                return Ok(avg);
            }
            catch (EmptyReportSetException ex)
            {
                _logger.LogWarning(ex, "Average price requested but no reports exist");
                return Ok(0m);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching average price");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching average price");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            try
            {
                var reports = await _service.GetAllReportsAsync();
                _logger.LogInformation("All reports requested — {Count} record(s) returned", reports.Count);
                return Ok(reports);
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching all reports");
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching all reports");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetReportsByProduct(int productId)
        {
            try
            {
                var reports = await _service.GetReportsByProductIdAsync(productId);
                _logger.LogInformation("Reports requested for ProductId {ProductId} — {Count} record(s) returned",productId, reports.Count);
                return Ok(reports);
            }
            catch (ReportNotFoundException ex)
            {
                _logger.LogWarning(ex, "No reports found for ProductId {ProductId}", productId);
                return NotFound(new { error = ex.Message });
            }
            catch (ReportingException ex)
            {
                _logger.LogError(ex, "Service error fetching reports for ProductId {ProductId}", productId);
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching reports for ProductId {ProductId}", productId);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}