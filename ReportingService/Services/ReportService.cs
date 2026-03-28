using ReportingService.Models;
using ReportingService.Repository;

namespace ReportingService.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repository;

        public ReportService(IReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> GetApprovedCountAsync()
        {
            return await _repository.GetApprovedCountAsync();
        }

        public async Task<int> GetRejectedCountAsync()
        {
            return await _repository.GetRejectedCountAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _repository.GetPendingCountAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _repository.GetTotalInventoryValueAsync();
        }

        public async Task<decimal> GetAveragePriceAsync()
        {
            return await _repository.GetAveragePriceAsync();
        }

        public async Task<List<ProductReport>> GetAllReportsAsync()
        {
            return await _repository.GetAllReportsAsync();
        }

        public async Task<List<ProductReport>> GetReportsByProductIdAsync(int productId)
        {
            return await _repository.GetReportsByProductIdAsync(productId);
        }

        public async Task<object> GetDashboardAsync()
        {
            var approved = await _repository.GetApprovedCountAsync();
            var rejected = await _repository.GetRejectedCountAsync();
            var pending = await _repository.GetPendingCountAsync();
            var totalValue = await _repository.GetTotalInventoryValueAsync();

            return new
            {
                Approved = approved,
                Rejected = rejected,
                Pending = pending,
                TotalInventoryValue = totalValue
            };
        }

        public async Task<double> GetApprovalRateAsync()
        {
            var approved = await _repository.GetApprovedCountAsync();
            var rejected = await _repository.GetRejectedCountAsync();

            var total = approved + rejected;

            if (total == 0) return 0;

            return (double)approved / total * 100;
        }

        public async Task<List<ProductReport>> GetRecentReportsAsync()
        {
            var reports = await _repository.GetAllReportsAsync();

            return reports
                .Where(r => r.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
                .ToList();
        }
    }
}
