namespace TaskFlow.Api.Models;
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN";
    public string Priority { get; set; } = "MEDIUM";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
}
