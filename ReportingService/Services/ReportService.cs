using Microsoft.Extensions.Logging;
using ReportingService.Exceptions;
using ReportingService.Models;
using ReportingService.Repository;

namespace ReportingService.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IReportRepository repository, ILogger<ReportService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // Pure pass-throughs — repo already logs and throws; no value adding a wrapper here
        public Task<int> GetApprovedCountAsync() => _repository.GetApprovedCountAsync();
        public Task<int> GetRejectedCountAsync() => _repository.GetRejectedCountAsync();
        public Task<int> GetPendingCountAsync() => _repository.GetPendingCountAsync();
        public Task<decimal> GetTotalInventoryValueAsync() => _repository.GetTotalInventoryValueAsync();
        public Task<decimal> GetAveragePriceAsync() => _repository.GetAveragePriceAsync();
        public Task<List<ProductReport>> GetAllReportsAsync() => _repository.GetAllReportsAsync();
        public Task<List<ProductReport>> GetReportsByProductIdAsync(int productId)
            => _repository.GetReportsByProductIdAsync(productId);

        public async Task<object> GetDashboardAsync()
        {
            try
            {
                var approved = await _repository.GetApprovedCountAsync();
                var rejected = await _repository.GetRejectedCountAsync();
                var pending = await _repository.GetPendingCountAsync();
                var totalValue = await _repository.GetTotalInventoryValueAsync();

                _logger.LogInformation(
                    "Dashboard aggregated — Approved: {Approved}, Rejected: {Rejected}, " +
                    "Pending: {Pending}, TotalValue: {TotalValue}",
                    approved, rejected, pending, totalValue);

                return new
                {
                    Approved = approved,
                    Rejected = rejected,
                    Pending = pending,
                    TotalInventoryValue = totalValue
                };
            }
            catch (ReportingException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error aggregating dashboard data");
                throw new DashboardAggregationException(ex);
            }
        }

        public async Task<double> GetApprovalRateAsync()
        {
            try
            {
                var approved = await _repository.GetApprovedCountAsync();
                var rejected = await _repository.GetRejectedCountAsync();

                var total = approved + rejected;

                if (total == 0)
                {
                    _logger.LogWarning("Approval rate requested but no approved/rejected reports exist");
                    return 0; 
                }

                var rate = (double)approved / total * 100;
                _logger.LogInformation("Approval rate computed: {Rate:F2}%", rate);
                return rate;
            }
            catch (ReportingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error computing approval rate");
                throw new ApprovalRateException(ex);
            }
        }

        public async Task<List<ProductReport>> GetRecentReportsAsync()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-7);
                var reports = await _repository.GetAllReportsAsync();

                var recent = reports
                    .Where(r => r.UpdatedAt >= cutoff)
                    .ToList();

                _logger.LogInformation(
                    "Recent reports fetched — {Count} report(s) updated since {Cutoff:O}",
                    recent.Count, cutoff);

                return recent;
            }
            catch (ReportingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching recent reports");
                throw new RecentReportsException(ex);
            }
        }
    }
}