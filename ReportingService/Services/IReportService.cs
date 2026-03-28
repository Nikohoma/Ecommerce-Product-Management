using ReportingService.Models;

namespace ReportingService.Services
{
    public interface IReportService
    {
        Task<int> GetApprovedCountAsync();
        Task<int> GetRejectedCountAsync();
        Task<int> GetPendingCountAsync();

        Task<decimal> GetTotalInventoryValueAsync();
        Task<decimal> GetAveragePriceAsync();

        Task<List<ProductReport>> GetAllReportsAsync();
        Task<List<ProductReport>> GetReportsByProductIdAsync(int productId);
        Task<object> GetDashboardAsync();
        Task<double> GetApprovalRateAsync();
        Task<List<ProductReport>> GetRecentReportsAsync();

    }
}
