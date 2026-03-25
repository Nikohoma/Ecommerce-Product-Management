namespace ProductWorkflowService.Services
{
    public class WorkflowService
    {
        private readonly CatalogClient _catalogClient;

        public WorkflowService(CatalogClient catalogClient)
        {
            _catalogClient = catalogClient;
        }

        public async Task Submit(int productId)
        {
            await _catalogClient.SubmitProduct(productId);
        }

        public async Task Approve(int productId)
        {
            await _catalogClient.ApproveProduct(productId);
        }

        public async Task Reject(int productId)
        {
            await _catalogClient.RejectProduct(productId);
        }
    }
}