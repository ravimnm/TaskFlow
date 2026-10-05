using TaskFlow.Api.DTOs;
namespace TaskFlow.Api.Services;
public interface ITaskService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, int currentUserId, bool isAdmin);
    Task<TaskResponse?> GetAsync(int id);
    Task<(IReadOnlyList<TaskResponse> Items, int Total)> GetPagedAsync(int page, int pageSize, string? status, int? currentUserId, bool isAdmin);
    Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, int currentUserId, bool isAdmin);
    Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, bool isAdmin);
    Task<bool> DeleteAsync(int id, int currentUserId, bool isAdmin);
}
