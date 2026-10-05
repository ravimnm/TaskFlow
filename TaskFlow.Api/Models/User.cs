namespace TaskFlow.Api.Models;
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
    public List<TaskItem> Tasks { get; set; } = [];
}
