namespace FocusLab.Domain.Entities;

public class Account
{
    /// <summary>Id локального акаунта, який створюється разом із БД.</summary>
    public const int LocalAccountId = 1;

    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
