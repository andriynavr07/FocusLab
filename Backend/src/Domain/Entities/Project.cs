namespace FocusLab.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B6EF5";

    public Account Account { get; set; } = null!;
}
