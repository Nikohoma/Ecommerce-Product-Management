using Microsoft.EntityFrameworkCore;
using ReportingService.Models;

namespace ReportingService.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly ReportingDbContext _context;

        public ReportRepository(ReportingDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetApprovedCountAsync()
        {
            return await _context.ProductReports
                .CountAsync(p => p.Status == "Approved");
        }

        public async Task<int> GetRejectedCountAsync()
        {
            return await _context.ProductReports
                .CountAsync(p => p.Status == "Rejected");
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ProductReports
                .CountAsync(p => p.Status == "Pending" || p.Status == "Submitted");
        }

        public async Task<List<ProductReport>> GetAllReportsAsync()
        {
            return await _context.ProductReports
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<ProductReport>> GetReportsByProductIdAsync(int productId)
        {
            return await _context.ProductReports
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            var latestReports = await _context.ProductReports
                .GroupBy(p => p.ProductId)
                .Select(g => g.OrderByDescending(x => x.UpdatedAt).First())
                .ToListAsync();

            return latestReports.Sum(p => p.Price);
        }

        public async Task<decimal> GetAveragePriceAsync()
        {
            var latestReports = await _context.ProductReports
                .GroupBy(p => p.ProductId)
                .Select(g => g.OrderByDescending(x => x.UpdatedAt).First())
                .ToListAsync();

            return latestReports.Average(p => p.Price);
        }
    }
}
