using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;
namespace TaskFlow.Api.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<TaskItem>().HasIndex(x => new { x.AssignedUserId, x.Status, x.CreatedAt });
        modelBuilder.Entity<TaskItem>().HasOne(x => x.AssignedUser).WithMany(x => x.Tasks).HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
