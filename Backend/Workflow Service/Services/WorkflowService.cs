using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using WorkflowService.Data;
using WorkflowService.Exceptions;
using WorkflowService.Model;

namespace WorkflowServices.Services
{
    public class WorkflowService
    {
        private readonly IPublisher _publisher;
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<WorkflowService> _logger;

        public WorkflowService(IPublisher publisher, WorkflowDbContext context, ILogger<WorkflowService> logger)
        {
            _publisher = publisher;
            _dbContext = context;
            _logger = logger;
        }

        public async Task SubmitAsync(int productId, string name)
            => await ExecuteWorkflowActionAsync(productId, name, "Submit");

        public async Task ApproveAsync(int productId, string name)
            => await ExecuteWorkflowActionAsync(productId, name, "Approve");

        public async Task RejectAsync(int productId, string name)
            => await ExecuteWorkflowActionAsync(productId, name, "Reject");

        // Single method drives all three actions — publish then log, with distinct failure domains
        private async Task ExecuteWorkflowActionAsync(int productId, string name, string action)
        {
            _logger.LogInformation(
                "Workflow action '{Action}' initiated — ProductId: {ProductId}, User: {Name}",
                action, productId, name);

            await PublishActionAsync(productId, action);
            await LogWorkflowEntryAsync(productId, name, action);

            _logger.LogInformation(
                "Workflow action '{Action}' completed — ProductId: {ProductId}",
                action, productId);
        }

        private async Task PublishActionAsync(int productId, string action)
        {
            try
            {
                await _publisher.PublishAsync(new ProductWorkflowEvent
                {
                    ProductId = productId,
                    Action = action
                });

                _logger.LogInformation(
                    "Event published — Action: {Action}, ProductId: {ProductId}",
                    action, productId);
            }
            catch (WorkflowException)
            {
                throw; // PublisherConnectionException, MessagePublishException etc. bubble as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error publishing '{Action}' event for ProductId {ProductId}",
                    action, productId);
                throw new WorkflowPublishException(action, productId, ex);
            }
        }

        private async Task LogWorkflowEntryAsync(int productId, string name, string newStatus)
        {
            try
            {
                var lastEntry = await _dbContext.WorkflowDb
                    .Where(x => x.productId == productId)
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefaultAsync(); // async — was blocking

                var oldStatus = lastEntry?.newStatus ?? "Unknown";

                var workflow = new Workflow
                {
                    Name = name,
                    oldStatus = oldStatus,
                    newStatus = newStatus,
                    productId = productId,
                    Date = DateTime.UtcNow
                };

                _dbContext.WorkflowDb.Add(workflow);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Workflow log saved — ProductId: {ProductId}, {OldStatus} → {NewStatus}, User: {Name}",
                    productId, oldStatus, newStatus, name);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error persisting workflow log for ProductId {ProductId}",
                    productId);
                throw new WorkflowLogException(productId, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error persisting workflow log for ProductId {ProductId}",
                    productId);
                throw new WorkflowLogException(productId, ex);
            }
        }
    }
}