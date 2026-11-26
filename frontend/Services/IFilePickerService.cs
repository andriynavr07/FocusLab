using Microsoft.Win32;

namespace Frontend.Services;

/// <summary>
/// Вибір файлів і папок. ViewModel-і отримують шляхи через цей інтерфейс,
/// тож у тестах замість вікон ОС підставляється заглушка.
/// </summary>
public interface IFilePickerService
{
    /// <summary>Повертає вибрані файли або порожній список, якщо користувач скасував вибір.</summary>
    IReadOnlyList<string> PickFiles();

    /// <summary>Повертає вибрану папку або null, якщо користувач скасував вибір.</summary>
    string? PickFolder();

    /// <summary>Повертає шлях для збереження або null, якщо користувач скасував вибір.</summary>
    string? PickSaveFile(string defaultFileName, string filter);
}

/// <summary>Реалізація на стандартних діалогах Windows.</summary>
public sealed class WpfFilePickerService : IFilePickerService
{
    public IReadOnlyList<string> PickFiles()
    {
        var dialog = new OpenFileDialog { Multiselect = true };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? PickFolder()
    {
        var dialog = new OpenFolderDialog();
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? PickSaveFile(string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog { FileName = defaultFileName, Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
