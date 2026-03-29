using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using System.ComponentModel.DataAnnotations;
using WorkflowServices.Services;

namespace WorkflowService.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("test/[controller]")]
    public class WorkflowController : Controller
    {
        public readonly WorkflowServices.Services.WorkflowService _service;

        public WorkflowController(WorkflowServices.Services.WorkflowService service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> SetStatus(int productId,string status)
        {
            Console.WriteLine("Controller HIT");
            if (status.ToLower().Trim() == "submit")
            {
                await _service.Submit(productId);
                return Ok();
            }
            else if(status.ToLower().Trim() == "approve")
            {
                await _service.Approve(productId); return Ok();
            }
            else if (status.ToLower().Trim() == "reject")
            {
                Console.WriteLine("Calling service...");
                await _service.Reject(productId);return Ok();
            }
            return BadRequest();
        }

    }
}
