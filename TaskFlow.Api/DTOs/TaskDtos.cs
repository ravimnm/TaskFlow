using System.ComponentModel.DataAnnotations;
namespace TaskFlow.Api.DTOs;
public record CreateTaskRequest([Required, MaxLength(100)] string Title, string? Description, string? Priority, DateTime? DueDate, int AssignedUserId);
public record UpdateTaskRequest([Required, MaxLength(100)] string Title, string? Description, string? Priority, DateTime? DueDate);
public record UpdateStatusRequest([Required] string Status);
public record TaskResponse(int Id, string Title, string Description, string Status, string Priority, DateTime? DueDate, int AssignedUserId, DateTime CreatedAt);
