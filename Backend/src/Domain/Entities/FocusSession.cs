namespace FocusLab.Domain.Entities;

public class FocusSession
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime StartDateTime { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsCompleted { get; set; }

    public TaskItem Task { get; set; } = null!;
}
