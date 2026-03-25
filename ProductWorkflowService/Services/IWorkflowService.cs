
namespace ProductWorkflowService.Services
{
    public interface IWorkflowService
    {
        Task SubmitProduct(int productId);
        Task ApproveProduct(int productId);
        Task RejectProduct(int productId);
        Task ArchiveProduct(int productId);
    }
}
