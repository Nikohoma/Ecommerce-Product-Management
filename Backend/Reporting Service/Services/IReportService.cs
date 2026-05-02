using ReportingService.DTO;
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
        Task<DashboardDto> GetDashboardAsync();
        Task<double> GetApprovalRateAsync();
        Task<List<ProductReport>> GetRecentReportsAsync();
        Task<List<ProductActivity>> GetRecentActivitiesAsync(string category);
        Task<PaginatedResult<ProductReport>> GetRecentReportsPagedAsync(int page, int pageSize);
        Task<PaginatedResult<ProductActivity>> GetRecentActivitiesPagedAsync(string category, int page, int pageSize);

    }
}
