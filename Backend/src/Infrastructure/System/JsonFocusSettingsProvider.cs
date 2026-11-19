using System.Text.Json;
using System.Text.Json.Nodes;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;

namespace FocusLab.Infrastructure.SystemServices;

/// <summary>
/// Налаштування таймера в секції "focus" файлу settings.json (%AppData%\FocusLab).
/// Інші секції файлу не чіпає. Пошкоджений або відсутній файл → значення за замовчуванням.
/// </summary>
public sealed class JsonFocusSettingsProvider : IFocusSettingsProvider
{
    private const string SectionName = "focus";
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly object _gate = new();
    private readonly string _path;
    private FocusSettings _current;

    public JsonFocusSettingsProvider(string path)
    {
        _path = path;
        _current = Load();
    }

    public FocusSettings Current
    {
        get { lock (_gate) return _current; }
    }

    public void Save(FocusSettings settings)
    {
        var normalized = Normalize(settings);

        lock (_gate)
        {
            _current = normalized; // у пам'яті діє одразу, навіть якщо запис на диск не вдасться

            var root = ReadRoot();
            root[SectionName] = JsonNode.Parse(JsonSerializer.Serialize(normalized));

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, root.ToJsonString(WriteOptions));
            File.Move(tmp, _path, overwrite: true);
        }
    }

    private FocusSettings Load()
    {
        try
        {
            var section = ReadRoot()[SectionName];
            if (section is null) return new FocusSettings();

            var loaded = JsonSerializer.Deserialize<FocusSettings>(section.ToJsonString());
            return loaded is null ? new FocusSettings() : Normalize(loaded);
        }
        catch (JsonException)
        {
            return new FocusSettings();
        }
    }

    private JsonObject ReadRoot()
    {
        try
        {
            if (!File.Exists(_path)) return new JsonObject();
            return JsonNode.Parse(File.ReadAllText(_path)) as JsonObject ?? new JsonObject();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new JsonObject();
        }
    }

    private static FocusSettings Normalize(FocusSettings s) => s with
    {
        FocusMinutes = Math.Clamp(s.FocusMinutes, 1, 180),
        ShortBreakMinutes = Math.Clamp(s.ShortBreakMinutes, 1, 60),
        LongBreakMinutes = Math.Clamp(s.LongBreakMinutes, 1, 120),
        SessionsBeforeLongBreak = Math.Clamp(s.SessionsBeforeLongBreak, 2, 10)
    };
}
