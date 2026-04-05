using RabbitMQ.Client;
using Shared.Contracts;
using WorkflowService.Data;
using WorkflowService.Model;

namespace WorkflowServices.Services
{
    public class WorkflowService
    {
        private readonly Publisher _publisher;
        private readonly WorkflowDbContext _dbContext;

        public WorkflowService(Publisher publisher, WorkflowDbContext context)
        {
            _publisher = publisher;
            _dbContext = context;
        }

        public async Task Submit(int productId,string name)
        {
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Submit"
            });
            await LogInformation(name, "Submit", productId);
            
        }

        public async Task Approve(int productId, string name)
        {
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Approve"
            });
            await LogInformation(name, "Approve", productId);

        }

        public async Task Reject(int productId, string name)
        {
            //Console.WriteLine("Reject working");
            await _publisher.Publish(new ProductWorkflowEvent
            {
                ProductId = productId,
                Action = "Reject"
            });
            await LogInformation(name, "Reject", productId);

        }

        public async Task LogInformation(string name, string newStatus, int productId)
        {
            var lastEntry = _dbContext.WorkflowDb.Where(x => x.productId == productId).OrderByDescending(x => x.Date).FirstOrDefault();
            var oldStatus = lastEntry?.newStatus ?? "Unknown";
            var workflow = new Workflow()
            {
                Name = name,
                oldStatus = oldStatus,
                newStatus = newStatus,
                productId = productId,
                Date = DateTime.UtcNow
            };

            if (workflow!= null)
            {
                _dbContext.WorkflowDb.Add(workflow);
                await _dbContext.SaveChangesAsync();
            }
            else { return; }
        }
    }
}
