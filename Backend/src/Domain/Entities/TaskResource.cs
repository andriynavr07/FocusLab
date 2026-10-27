using FocusLab.Domain.Enums;

namespace FocusLab.Domain.Entities;

public class TaskResource
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public ResourceType Type { get; set; }
    public string Path { get; set; } = string.Empty;

    public TaskItem Task { get; set; } = null!;
}
