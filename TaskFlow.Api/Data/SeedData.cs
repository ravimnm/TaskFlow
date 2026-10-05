using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace TaskFlow.Api.Data;
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Users.AnyAsync()) return;
        db.Users.AddRange(
            new Models.User { Email = "admin@taskflow.local", PasswordHash = Hash("Admin@123"), Role = "ADMIN" },
            new Models.User { Email = "user@taskflow.local", PasswordHash = Hash("User@123"), Role = "USER" });
        await db.SaveChangesAsync();
    }
    public static string Hash(string password) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
}
