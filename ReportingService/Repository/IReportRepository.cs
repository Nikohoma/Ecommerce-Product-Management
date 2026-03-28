using ReportingService.Models;

namespace ReportingService.Repository
{
    public interface IReportRepository
    {
        Task<int> GetApprovedCountAsync();
        Task<int> GetRejectedCountAsync();
        Task<int> GetPendingCountAsync();

        Task<List<ProductReport>> GetAllReportsAsync();
        Task<List<ProductReport>> GetReportsByProductIdAsync(int productId);

        Task<decimal> GetTotalInventoryValueAsync();
        Task<decimal> GetAveragePriceAsync();
    }
}
