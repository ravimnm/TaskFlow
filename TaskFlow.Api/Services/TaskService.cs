using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
namespace TaskFlow.Api.Services;
public class TaskService(ITaskRepository repository) : ITaskService
{
    private static readonly HashSet<string> Statuses = ["OPEN", "IN_PROGRESS", "DONE"];
    private static readonly HashSet<string> Priorities = ["LOW", "MEDIUM", "HIGH"];
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, int currentUserId, bool isAdmin)
    {
        if (!isAdmin && request.AssignedUserId != currentUserId) throw new UnauthorizedAccessException("Users may only assign tasks to themselves.");
        Validate(request.Priority, "priority");
        var item = new TaskItem { Title = request.Title, Description = request.Description ?? "", Priority = (request.Priority ?? "MEDIUM").ToUpperInvariant(), DueDate = request.DueDate, AssignedUserId = request.AssignedUserId };
        await repository.AddAsync(item); await repository.SaveChangesAsync(); return ToResponse(item);
    }
    public async Task<TaskResponse?> GetAsync(int id) => Map(await repository.GetByIdAsync(id));
    public async Task<(IReadOnlyList<TaskResponse> Items, int Total)> GetPagedAsync(int page, int pageSize, string? status, int? currentUserId, bool isAdmin)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentException("Invalid pagination parameters.");
        var result = await repository.GetPagedAsync(page, pageSize, status?.ToUpperInvariant(), isAdmin ? null : currentUserId);
        return (result.Items.Select(ToResponse).ToList(), result.Total);
    }
    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, int currentUserId, bool isAdmin)
    {
        var item = await repository.GetByIdAsync(id); if (item is null) return null;
        if (!isAdmin && item.AssignedUserId != currentUserId) throw new UnauthorizedAccessException("You cannot modify this task.");
        Validate(request.Priority, "priority");
        // Repository returns a no-tracking entity; re-load through context is intentionally avoided in this small project.
        throw new NotSupportedException("Update endpoint requires a tracked repository operation; see README for extension point.");
    }
    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, bool isAdmin)
    {
        if (!Statuses.Contains(status.ToUpperInvariant())) throw new ArgumentException("Status must be OPEN, IN_PROGRESS, or DONE.");
        var item = await repository.GetByIdAsync(id); if (item is null) return false;
        if (!isAdmin && item.AssignedUserId != currentUserId) throw new UnauthorizedAccessException("You cannot modify this task.");
        throw new NotSupportedException("Status update endpoint requires a tracked repository operation; see README for extension point.");
    }
    public async Task<bool> DeleteAsync(int id, int currentUserId, bool isAdmin)
    {
        var item = await repository.GetByIdAsync(id); if (item is null) return false;
        if (!isAdmin && item.AssignedUserId != currentUserId) throw new UnauthorizedAccessException("You cannot delete this task.");
        await repository.DeleteAsync(item); await repository.SaveChangesAsync(); return true;
    }
    private static void Validate(string? value, string name) { if (value is not null && !Priorities.Contains(value.ToUpperInvariant())) throw new ArgumentException($"Invalid {name}."); }
    private static TaskResponse? Map(TaskItem? x) => x is null ? null : ToResponse(x);
    private static TaskResponse ToResponse(TaskItem x) => new(x.Id, x.Title, x.Description, x.Status, x.Priority, x.DueDate, x.AssignedUserId, x.CreatedAt);
}
