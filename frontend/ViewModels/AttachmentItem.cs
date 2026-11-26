using System.IO;
using System.Windows.Media;
using Frontend.Services;

namespace Frontend.ViewModels;

/// <summary>Рядок у блоках «Файли та папки» / «Закріплені посилання» панелі деталей завдання.</summary>
public sealed class AttachmentItem
{
    private static readonly Brush Pdf = Frozen("#F87171");
    private static readonly Brush Folder = Frozen("#A78BFA");
    private static readonly Brush Word = Frozen("#60A5FA");
    private static readonly Brush Excel = Frozen("#34D399");
    private static readonly Brush Other = Frozen("#64748B");

    public AttachmentItem(TaskAttachment model, ILocalizer localizer)
    {
        Model = model;
        Title = model.Name;
        Target = model.Target;

        switch (model.Kind)
        {
            case AttachmentKind.Folder:
                BadgeText = "DIR";
                BadgeBrush = Folder;
                Subtitle = $"{localizer.Translate("Локальна папка")} · {model.Target}";
                break;
            case AttachmentKind.Link:
                Domain = Uri.TryCreate(model.Target, UriKind.Absolute, out var uri) ? uri.Host : model.Target;
                Subtitle = model.Target;
                break;
            default:
                var ext = Path.GetExtension(model.Name).TrimStart('.').ToUpperInvariant();
                BadgeText = string.IsNullOrEmpty(ext) ? "FILE" : ext.Length > 4 ? ext[..4] : ext;
                BadgeBrush = ext switch
                {
                    "PDF" => Pdf,
                    "DOC" or "DOCX" => Word,
                    "XLS" or "XLSX" or "CSV" => Excel,
                    _ => Other
                };
                Subtitle = $"{FormatSize(model.SizeBytes, localizer)} · {ModifiedText(model.Target, localizer)}";
                break;
        }
    }

    public TaskAttachment Model { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Target { get; }
    public string BadgeText { get; } = string.Empty;
    public Brush BadgeBrush { get; } = Other;
    public string Domain { get; } = string.Empty;

    private static string ModifiedText(string path, ILocalizer localizer)
    {
        try
        {
            var modified = File.GetLastWriteTime(path).Date;
            return modified == DateTime.Today
                ? localizer.Translate("Оновлено сьогодні")
                : $"{localizer.Translate("Оновлено")} {modified:dd.MM.yyyy}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatSize(long bytes, ILocalizer localizer) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:0.#} {localizer.Translate("МБ")}",
        >= 1024 => $"{bytes / 1024d:0.#} {localizer.Translate("КБ")}",
        _ => $"{bytes} {localizer.Translate("Б")}"
    };

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
