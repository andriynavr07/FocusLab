using System.Text.Json.Nodes;
using FluentAssertions;
using FocusLab.Application.Focus;
using FocusLab.Infrastructure.SystemServices;

namespace FocusLab.Infrastructure.Tests;

public sealed class JsonFocusSettingsProviderTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"focuslab-settings-{Guid.NewGuid():N}");

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public JsonFocusSettingsProviderTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void MissingFile_ReturnsDefaults()
    {
        var provider = new JsonFocusSettingsProvider(SettingsPath);

        provider.Current.Should().Be(new FocusSettings());
    }

    [Fact]
    public void CorruptedJson_ReturnsDefaults()
    {
        File.WriteAllText(SettingsPath, "{ this is not json");

        var provider = new JsonFocusSettingsProvider(SettingsPath);

        provider.Current.Should().Be(new FocusSettings());
    }

    [Fact]
    public void Save_KeepsOtherSections()
    {
        File.WriteAllText(SettingsPath, """{ "theme": "dark", "focus": { "FocusMinutes": 30 } }""");
        var provider = new JsonFocusSettingsProvider(SettingsPath);

        provider.Save(new FocusSettings { FocusMinutes = 40 });

        var root = JsonNode.Parse(File.ReadAllText(SettingsPath))!.AsObject();
        root["theme"]!.GetValue<string>().Should().Be("dark");
        root["focus"]!["FocusMinutes"]!.GetValue<int>().Should().Be(40);
    }

    [Fact]
    public void Save_ClampsOutOfRange()
    {
        var provider = new JsonFocusSettingsProvider(SettingsPath);

        provider.Save(new FocusSettings { FocusMinutes = 500, SessionsBeforeLongBreak = 1 });

        provider.Current.FocusMinutes.Should().Be(180);
        provider.Current.SessionsBeforeLongBreak.Should().Be(2);
    }

    [Fact]
    public void Save_ThenNewInstance_LoadsSaved()
    {
        var saved = new FocusSettings
        {
            FocusMinutes = 50,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 30,
            SessionsBeforeLongBreak = 3,
            AutoStartNext = false,
            SaveInterruptedSessions = false
        };
        new JsonFocusSettingsProvider(SettingsPath).Save(saved);

        var reloaded = new JsonFocusSettingsProvider(SettingsPath);

        reloaded.Current.Should().Be(saved);
    }
}
