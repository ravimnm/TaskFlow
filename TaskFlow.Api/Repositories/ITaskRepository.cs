using TaskFlow.Api.Models;
namespace TaskFlow.Api.Repositories;
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id);
    Task<(IReadOnlyList<TaskItem> Items, int Total)> GetPagedAsync(int page, int pageSize, string? status, int? userId);
    Task AddAsync(TaskItem item);
    Task DeleteAsync(TaskItem item);
    Task SaveChangesAsync();
}
