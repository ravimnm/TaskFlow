using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Services;
namespace TaskFlow.Api.Controllers;
[ApiController, Route("api/tasks"), Authorize]
public class TasksController(ITaskService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        var result = await service.GetPagedAsync(page, pageSize, status, UserId, IsAdmin); return Ok(new { result.Items, result.Total, page, pageSize });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => (await service.GetAsync(id)) is { } item ? Ok(item) : NotFound();
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var item = await service.CreateAsync(request, UserId, IsAdmin); return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) => await service.DeleteAsync(id, UserId, IsAdmin) ? NoContent() : NotFound();
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateTaskRequest request) => Ok(await service.UpdateAsync(id, request, UserId, IsAdmin));
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request) => Ok(await service.UpdateStatusAsync(id, request.Status, UserId, IsAdmin));
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("ADMIN");
}
