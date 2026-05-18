using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReportingService.Exceptions;
using ReportingService.Models;

namespace ReportingService.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<ReportRepository> _logger;

        // DI 
        public ReportRepository(ReportingDbContext context, ILogger<ReportRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get products that are approved (Active and Inactive)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<int> GetApprovedCountAsync()
        {
            try
            {
                var count = await _context.ProductReports.CountAsync(p => p.Status == "Active" || p.Status == "Inactive");

                _logger.LogInformation("Approved report count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approved report count");
                throw new ReportQueryException("GetApprovedCount", ex);
            }
        }
        /// <summary>
        /// Retrieve Products that are rejected.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<int> GetRejectedCountAsync()
        {
            try
            {
                var count = await _context.ProductReports.CountAsync(p => p.Status == "Rejected");

                _logger.LogInformation("Rejected report count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rejected report count");
                throw new ReportQueryException("GetRejectedCount", ex);
            }
        }
        /// <summary>
        /// Retrieve Products that are pending (submitted for review)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<int> GetPendingCountAsync()
        {
            try
            {
                var count = await _context.ProductReports
                    .CountAsync(p => p.Status == "Pending" || p.Status == "Submitted");

                _logger.LogInformation("Pending/Submitted report count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending report count");
                throw new ReportQueryException("GetPendingCount", ex);
            }
        }
        /// <summary>
        /// Retrieve reports of all products.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<List<ProductReport>> GetAllReportsAsync()
        {
            try
            {
                var reports = await _context.ProductReports.OrderByDescending(p => p.UpdatedAt).ToListAsync();

                _logger.LogInformation("Fetched {Count} product report(s)", reports.Count);
                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all product reports");
                throw new ReportQueryException("GetAllReports", ex);
            }
        }
        /// <summary>
        /// Retrieve reports based on product.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        /// <exception cref="ReportNotFoundException"></exception>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<List<ProductReport>> GetReportsByProductIdAsync(int productId)
        {
            try
            {
                var reports = await _context.ProductReports.Where(p => p.ProductId == productId).OrderByDescending(p => p.UpdatedAt).ToListAsync();

                if (reports.Count == 0)
                {
                    _logger.LogWarning("No reports found for ProductId {ProductId}", productId);
                    throw new ReportNotFoundException(productId);
                }

                _logger.LogInformation("Fetched {Count} report(s) for ProductId {ProductId}", reports.Count, productId);
                return reports;
            }
            catch (ReportingException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports for ProductId {ProductId}", productId);
                throw new ReportQueryException("GetReportsByProductId", ex);
            }
        }
        /// <summary>
        /// Get the total value of all the products
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            try
            {
                var latestReports = await GetLatestReportsPerProductAsync("GetTotalInventoryValue");

                var total = latestReports.Sum(p => p.Price);
                _logger.LogInformation("Total inventory value computed: {Total}", total);
                return total;
            }
            catch (ReportingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing total inventory value");
                throw new ReportQueryException("GetTotalInventoryValue", ex);
            }
        }
        /// <summary>
        /// Get average price of the latest products
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ReportQueryException"></exception>
        public async Task<decimal> GetAveragePriceAsync()
        {
            try
            {
                var latestReports = await GetLatestReportsPerProductAsync("GetAveragePrice");

                var avg = latestReports.Average(p => p.Price);
                _logger.LogInformation("Average product price computed: {Average}", avg);
                return avg;
            }
            catch (ReportingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing average product price");
                throw new ReportQueryException("GetAveragePrice", ex);
            }
        }
        /// <summary>
        /// Retrieve latest products
        /// </summary>
        /// <param name="callerOperation"></param>
        /// <returns></returns>
        /// <exception cref="EmptyReportSetException"></exception>
        private async Task<List<ProductReport>> GetLatestReportsPerProductAsync(string callerOperation)
        {
            var latestReports = await _context.ProductReports
                .GroupBy(p => p.ProductId)
                .Select(g => g.OrderByDescending(x => x.UpdatedAt).First())
                .ToListAsync();

            if (latestReports.Count == 0)
            {
                _logger.LogWarning("No product reports found for operation '{Operation}'", callerOperation);
                throw new EmptyReportSetException(callerOperation);
            }

            return latestReports;
        }
    }
}