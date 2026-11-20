using System.Windows;

namespace Frontend.Services;

/// <summary>Діалоги підтвердження та помилок. Через інтерфейс сховища не залежать від MessageBox.</summary>
public interface IDialogService
{
    /// <summary>Запитує підтвердження («Так» / «Ні») і повертає true, якщо користувач погодився.</summary>
    bool Confirm(string message, string title);

    void ShowError(string title, Exception ex);
}

public sealed class WpfDialogService : IDialogService
{
    public bool Confirm(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public void ShowError(string title, Exception ex)
        => MessageBox.Show($"{title}:\n{ex.Message}", "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error);
}
