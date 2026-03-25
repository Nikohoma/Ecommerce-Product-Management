using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductWorkflowService.Services;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly WorkflowService _service;

    public WorkflowController(WorkflowService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Admin,ProductManager")]
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        await _service.Submit(id);
        return Ok($"Product {id} submitted");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _service.Approve(id);
        return Ok($"Product {id} approved");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        await _service.Reject(id);
        return Ok($"Product {id} rejected");
    }
}