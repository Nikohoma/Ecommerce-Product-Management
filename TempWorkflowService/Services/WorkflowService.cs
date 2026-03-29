using Shared.Contracts;

namespace WorkflowServices.Services
{
    public class WorkflowService
    {
        private readonly Publisher _publisher;

        public WorkflowService(Publisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Submit(int productId)
        {
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Submit"
            });
        }

        public async Task Approve(int productId)
        {
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Approve"
            });
        }

        public async Task Reject(int productId)
        {
            Console.WriteLine("Inside Reject()");
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Reject"
            });
        }
    }
}
