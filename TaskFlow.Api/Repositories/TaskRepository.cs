using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
namespace TaskFlow.Api.Repositories;
public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(int id) => db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    public async Task<(IReadOnlyList<TaskItem> Items, int Total)> GetPagedAsync(int page, int pageSize, string? status, int? userId)
    {
        var query = db.Tasks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (userId.HasValue) query = query.Where(x => x.AssignedUserId == userId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
    public async Task AddAsync(TaskItem item) => await db.Tasks.AddAsync(item);
    public Task DeleteAsync(TaskItem item) { db.Tasks.Remove(item); return Task.CompletedTask; }
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
