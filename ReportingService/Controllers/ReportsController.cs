using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Services;

namespace ReportingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var approved = await _service.GetApprovedCountAsync();
            var rejected = await _service.GetRejectedCountAsync();
            var pending = await _service.GetPendingCountAsync();
            var totalValue = await _service.GetTotalInventoryValueAsync();
            var avgPrice = await _service.GetAveragePriceAsync();

            return Ok(new
            {
                Approved = approved,
                Rejected = rejected,
                Pending = pending,
                TotalInventoryValue = totalValue,
                AveragePrice = avgPrice
            });
        }

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("approved-count")]
        public async Task<IActionResult> GetApprovedCount()
            => Ok(await _service.GetApprovedCountAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("rejected-count")]
        public async Task<IActionResult> GetRejectedCount()
            => Ok(await _service.GetRejectedCountAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount()
            => Ok(await _service.GetPendingCountAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("total-value")]
        public async Task<IActionResult> GetTotalValue()
            => Ok(await _service.GetTotalInventoryValueAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("average-price")]
        public async Task<IActionResult> GetAveragePrice()
            => Ok(await _service.GetAveragePriceAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet]
        public async Task<IActionResult> GetAllReports()
            => Ok(await _service.GetAllReportsAsync());

        [Authorize(Roles = "Admin,ProductManager")]
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetReportsByProduct(int productId)
            => Ok(await _service.GetReportsByProductIdAsync(productId));
    }
}
