namespace TaskFlow.Api.Services;
public interface ITokenService { string CreateToken(int userId, string email, string role); }
