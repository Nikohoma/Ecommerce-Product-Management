using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkflowService.Exceptions;
using WorkflowServices.Services;

namespace WorkflowService.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase  
    {
        private readonly WorkflowServices.Services.WorkflowService _service; 
        private readonly ILogger<WorkflowController> _logger;

        private static readonly HashSet<string> ValidStatuses =new(StringComparer.OrdinalIgnoreCase) { "submit", "approve", "reject" };

        public WorkflowController(WorkflowServices.Services.WorkflowService service, ILogger<WorkflowController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SetStatusAsync(int productId, string status)
        {
            var name = User.Identity?.Name ?? "System";

            if (string.IsNullOrWhiteSpace(status) || !ValidStatuses.Contains(status))
            {
                _logger.LogWarning("Invalid status '{Status}' received for ProductId {ProductId} by {User}",status, productId, name);

                return BadRequest(new
                {
                    error = $"Invalid status '{status}'. Allowed values: submit, approve, reject."
                });
            }

            var normalizedStatus = status.Trim().ToLower();

            _logger.LogInformation("Workflow action '{Status}' requested — ProductId: {ProductId}, User: {User}",normalizedStatus, productId, name);

            try
            {
                var task = normalizedStatus switch
                {
                    "submit" => _service.SubmitAsync(productId, name),
                    "approve" => _service.ApproveAsync(productId, name),
                    "reject" => _service.RejectAsync(productId, name),
                    _ => throw new InvalidOperationException($"Unhandled status: {normalizedStatus}")
                };

                await task;

                _logger.LogInformation("Workflow action '{Status}' succeeded — ProductId: {ProductId}, User: {User}",normalizedStatus, productId, name);

                return Ok(new
                {
                    productId,
                    status = normalizedStatus,
                    updatedBy = name
                });
            }
            catch (WorkflowLogException ex)
            {
                _logger.LogError(ex,"Workflow log persistence failed for ProductId {ProductId}",productId);
                return StatusCode(503, new { error = ex.Message });
            }
            catch (WorkflowPublishException ex)
            {
                _logger.LogError(ex,"Workflow event publish failed for ProductId {ProductId}",productId);
                return StatusCode(503, new { error = ex.Message });
            }
            catch (WorkflowException ex)
            {
                _logger.LogError(ex,"Workflow error for ProductId {ProductId}",productId);
                return StatusCode(503, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error processing workflow action for ProductId {ProductId}",productId);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}