using FocusLab.Domain.Enums;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<TaskResource> Resources { get; set; } = new List<TaskResource>();
    public ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
}
