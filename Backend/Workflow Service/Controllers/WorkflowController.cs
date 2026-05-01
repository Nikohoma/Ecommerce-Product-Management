using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            if (normalizedStatus == "submit")
                await _service.SubmitAsync(productId, name);
            else if (normalizedStatus == "approve")
                await _service.ApproveAsync(productId, name);
            else
                await _service.RejectAsync(productId, name);

            _logger.LogInformation("Workflow action '{Status}' succeeded — ProductId: {ProductId}, User: {User}",normalizedStatus, productId, name);

            return Ok(new
            {
                productId,
                status = normalizedStatus,
                updatedBy = name
            });
        }
    }
}