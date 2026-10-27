using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Dto;

public class TaskDto
{
    public int Id { get; set; }
    public int AccountId { get; set; } = Account.LocalAccountId;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
