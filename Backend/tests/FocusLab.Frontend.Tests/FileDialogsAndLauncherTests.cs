using System.IO;
using System.Text.Json;
using FluentAssertions;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Application.Focus;
using Frontend.Services;
using Frontend.ViewModels;
using NSubstitute;
using Xunit;

namespace FocusLab.Frontend.Tests;

/// <summary>
/// #107: файлові діалоги й запуск процесів ідуть через IFilePickerService / IProcessLauncher,
/// тож вкладення, експорт і посилання перевіряються без вікон ОС.
/// </summary>
public sealed class FileDialogsAndLauncherTests : IDisposable
{
    private const int TaskId = 42;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "focuslab-tests-" + Guid.NewGuid().ToString("N"));
    private readonly IFilePickerService _picker = Substitute.For<IFilePickerService>();
    private readonly IProcessLauncher _launcher = Substitute.For<IProcessLauncher>();
    private readonly FakeClock _clock = new(new DateTime(2026, 11, 19, 14, 30, 0));
    private readonly TaskBoardStore _store;
    private readonly TimerViewModel _timer;

    public FileDialogsAndLauncherTests()
    {
        Directory.CreateDirectory(_dir);
        _store = new TaskBoardStore(
            Substitute.For<ITaskService>(),
            Substitute.For<IDialogService>(),
            _clock,
            new TaskAttachmentStore(Path.Combine(_dir, "attachments.json")));

        var settings = Substitute.For<IFocusSettingsProvider>();
        settings.Current.Returns(new FocusSettings());
        _timer = new TimerViewModel(Substitute.For<IFocusTimerService>(), settings, _store);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private DashboardViewModel CreateDashboardWithOpenTask()
    {
        _store.Rows.Add(new TaskRow(_clock) { Id = TaskId, Title = "Задача", DueDate = _clock.Today.AddHours(18) });

        var catalog = Substitute.For<ICatalogService>();
        catalog.GetTagsAsync().Returns(new List<TagDto>());
        var dashboard = new DashboardViewModel(_store, _timer, Substitute.For<IActivityService>(), catalog, _picker, _launcher);

        var card = dashboard.Columns.SelectMany(column => column.Tasks).Single(task => task.Row.Id == TaskId);
        card.OpenCommand.Execute(null);
        return dashboard;
    }

    private string CreateFile(string name, int sizeBytes)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, new byte[sizeBytes]);
        return path;
    }

    private SettingsViewModel CreateSettings()
        => new(_timer, Substitute.For<IAuthService>(), _picker, _launcher);

    // ───── вкладення на головній ─────

    [Fact]
    public void AttachFile_AddsPickedFiles_WithoutOsDialog()
    {
        var first = CreateFile("report.pdf", 2048);
        var second = CreateFile("notes.txt", 10);
        _picker.PickFiles().Returns(new[] { first, second });
        var dashboard = CreateDashboardWithOpenTask();

        dashboard.AttachFileCommand.Execute(null);

        dashboard.Files.Select(file => file.Model.Name).Should().Equal("report.pdf", "notes.txt");
        var stored = _store.Attachments.GetFor(TaskId);
        stored.Should().HaveCount(2);
        stored[0].Kind.Should().Be(AttachmentKind.File);
        stored[0].Target.Should().Be(first);
        stored[0].SizeBytes.Should().Be(2048);
    }

    [Fact]
    public void AttachFile_Cancelled_AddsNothing()
    {
        _picker.PickFiles().Returns(Array.Empty<string>());
        var dashboard = CreateDashboardWithOpenTask();

        dashboard.AttachFileCommand.Execute(null);

        dashboard.Files.Should().BeEmpty();
        _store.Attachments.GetFor(TaskId).Should().BeEmpty();
    }

    [Fact]
    public void AttachFolder_AddsPickedFolder()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir, "src")).FullName;
        _picker.PickFolder().Returns(folder);
        var dashboard = CreateDashboardWithOpenTask();

        dashboard.AttachFolderCommand.Execute(null);

        var stored = _store.Attachments.GetFor(TaskId).Should().ContainSingle().Subject;
        stored.Kind.Should().Be(AttachmentKind.Folder);
        stored.Name.Should().Be("src");
        stored.Target.Should().Be(folder);
    }

    [Fact]
    public void OpenAttachment_UsesLauncher_WithAttachmentTarget()
    {
        var file = CreateFile("plan.docx", 1);
        _picker.PickFiles().Returns(new[] { file });
        var dashboard = CreateDashboardWithOpenTask();
        dashboard.AttachFileCommand.Execute(null);

        dashboard.OpenAttachmentCommand.Execute(dashboard.Files.Single());

        _launcher.Received(1).Open(file);
    }

    // ───── налаштування ─────

    [Fact]
    public void OpenSupport_UsesLauncher_WithSupportUrl()
    {
        var settings = CreateSettings();

        settings.OpenSupportCommand.Execute(null);

        _launcher.Received(1).Open(SettingsViewModel.SupportUrl);
    }

    [Fact]
    public void Export_WritesJson_ToPickedPath()
    {
        var path = Path.Combine(_dir, "export.json");
        _picker.PickSaveFile(Arg.Any<string>(), Arg.Any<string>()).Returns(path);
        var settings = CreateSettings();

        settings.ExportCommand.Execute(null);

        File.Exists(path).Should().BeTrue();
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        json.RootElement.GetProperty("WorkDefault").GetInt32().Should().Be(settings.WorkDefault);
        json.RootElement.GetProperty("SessionsDefault").GetInt32().Should().Be(settings.SessionsDefault);
    }

    [Fact]
    public void Export_Cancelled_WritesNothing()
    {
        _picker.PickSaveFile(Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        var settings = CreateSettings();

        settings.ExportCommand.Execute(null);

        Directory.GetFiles(_dir, "*.json").Should().BeEmpty();
    }
}
