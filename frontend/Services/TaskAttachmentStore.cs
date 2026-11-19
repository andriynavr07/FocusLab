using System.IO;
using System.Text.Json;
using FocusLab.Infrastructure.SystemServices;

namespace Frontend.Services;

public enum AttachmentKind
{
    File,
    Folder,
    Link
}

public sealed class TaskAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TaskId { get; set; }
    public AttachmentKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Шлях до файлу/папки або URL.</summary>
    public string Target { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Локальне сховище вкладень задач (файли, папки, посилання): %AppData%\FocusLab\attachments.json.
/// У БД поки немає таблиці вкладень, тому дані зберігаються на боці клієнта.
/// Коли бекенд додасть сутність Attachment, достатньо замінити реалізацію цього класу.
/// </summary>
public sealed class TaskAttachmentStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _file = Path.Combine(AppPaths.Root, "attachments.json");
    private List<TaskAttachment>? _items;

    public static TaskAttachmentStore Instance { get; } = new();

    public IReadOnlyList<TaskAttachment> GetFor(int taskId)
        => Items().Where(a => a.TaskId == taskId).OrderBy(a => a.AddedAt).ToList();

    public void Add(TaskAttachment attachment)
    {
        Items().Add(attachment);
        Save();
    }

    public void Remove(Guid id)
    {
        if (Items().RemoveAll(a => a.Id == id) > 0)
            Save();
    }

    public void RemoveForTask(int taskId)
    {
        if (Items().RemoveAll(a => a.TaskId == taskId) > 0)
            Save();
    }

    private List<TaskAttachment> Items()
    {
        if (_items is not null)
            return _items;

        try
        {
            _items = File.Exists(_file)
                ? JsonSerializer.Deserialize<List<TaskAttachment>>(File.ReadAllText(_file)) ?? []
                : [];
        }
        catch
        {
            _items = [];
        }

        return _items;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, JsonSerializer.Serialize(_items, Json));
        }
        catch
        {
            // Вкладення — допоміжна функція: помилка запису не повинна ламати роботу із задачею.
        }
    }
}
