using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace TaskFlow.Api.Services;
public class TokenService(IConfiguration configuration) : ITokenService
{
    public string CreateToken(int userId, string email, string role)
    {
        var key = configuration["Jwt:Key"]!; var issuer = configuration["Jwt:Issuer"]!;
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Role, role) };
        var token = new JwtSecurityToken(issuer, issuer, claims, expires: DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpiryMinutes", 60)), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
