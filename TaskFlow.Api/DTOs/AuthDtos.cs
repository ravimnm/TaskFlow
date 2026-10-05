using System.ComponentModel.DataAnnotations;
namespace TaskFlow.Api.DTOs;
public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(6)] string Password);
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record AuthResponse(string Token, string Role);
