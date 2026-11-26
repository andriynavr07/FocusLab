using System.Diagnostics;

namespace Frontend.Services;

/// <summary>
/// Відкриває файл, папку чи посилання програмою за замовчуванням.
/// Через інтерфейс ViewModel-і не запускають процеси напряму, і це можна перевірити тестом.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>Відкриває <paramref name="target"/> (шлях або URL). Кидає виняток, якщо відкрити не вдалося.</summary>
    void Open(string target);
}

/// <summary>Реалізація через оболонку Windows (<c>UseShellExecute</c>).</summary>
public sealed class ShellProcessLauncher : IProcessLauncher
{
    public void Open(string target)
        => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
}
