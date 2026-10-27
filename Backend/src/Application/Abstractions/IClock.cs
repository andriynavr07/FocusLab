namespace FocusLab.Application.Abstractions;

/// <summary>Джерело поточного часу. Дозволяє підмінювати час у тестах.</summary>
public interface IClock
{
    /// <summary>Поточний момент у UTC.</summary>
    DateTime UtcNow { get; }
}
