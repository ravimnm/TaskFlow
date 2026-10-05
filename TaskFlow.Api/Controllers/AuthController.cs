using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Services;
namespace TaskFlow.Api.Controllers;
[ApiController, Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(x => x.Email == request.Email)) return Conflict(new { error = "Email already registered." });
        var user = new Models.User { Email = request.Email, PasswordHash = Hash(request.Password) };
        db.Users.Add(user); await db.SaveChangesAsync(); return Ok(new AuthResponse(tokens.CreateToken(user.Id, user.Email, user.Role), user.Role));
    }
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (user is null || user.PasswordHash != Hash(request.Password)) return Unauthorized(new { error = "Invalid credentials." });
        return Ok(new AuthResponse(tokens.CreateToken(user.Id, user.Email, user.Role), user.Role));
    }
    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
