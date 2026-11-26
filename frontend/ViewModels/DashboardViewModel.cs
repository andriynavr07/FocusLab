using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using Frontend.Services;

namespace Frontend.ViewModels;

public class DashboardViewModel : PageViewModelBase, IDisposable
{
    private readonly TaskBoardStore _store;
    private readonly TimerViewModel _timer;
    private readonly IActivityService _activity;
    private readonly ICatalogService _catalog;
    private readonly IFilePickerService _filePicker;
    private readonly IProcessLauncher _launcher;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _activityDebounce;
    private readonly DashboardStatistic _doneToday;
    private readonly DashboardStatistic _focusToday;
    private readonly DashboardStatistic _streak;

    private TaskRow? _selectedTask;
    private bool _isDetailsOpen;
    private string _scheduleDateText = string.Empty;
    private IReadOnlyList<FocusSessionDto> _sessionsToday = Array.Empty<FocusSessionDto>();
    private int _focusMinutesToday;
    private int _streakDays;
    private bool _activityLoading;
    private bool _activityDirty;
    private bool _disposed;
    private string _selectedPriorityFilter = "Усі пріоритети";

    public DashboardViewModel(
        TaskBoardStore store,
        TimerViewModel timer,
        IActivityService activity,
        ICatalogService catalog,
        IFilePickerService filePicker,
        IProcessLauncher launcher)
        : base(store.Localizer)
    {
        _store = store;
        _doneToday = new("Виконано сьогодні", "0 завдань", Localizer);
        _focusToday = new("Час фокусування", "0 хв", Localizer);
        _streak = new("Активний серійний період", "0 днів", Localizer);
        _timer = timer;
        _activity = activity;
        _catalog = catalog;
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _store.Changed += OnStoreChanged;
        _timer.FocusSessionFinished += OnFocusSessionFinished;

        CloseDetailsCommand = new RelayCommand(_ => IsDetailsOpen = false);
        AttachFileCommand = new RelayCommand(_ => AttachFile());
        AttachFolderCommand = new RelayCommand(_ => AttachFolder());
        RemoveAttachmentCommand = new RelayCommand(p => RemoveAttachment(p as AttachmentItem));
        OpenAttachmentCommand = new RelayCommand(p => OpenAttachment(p as AttachmentItem));
        StartTaskFocusCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is { IsDone: false })
                SelectedTask.Status = "InProgress";
            _timer.StartFocusFor(SelectedTask);
        });
        EditTaskCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is { } task)
            {
                IsDetailsOpen = false;
                EditTaskRequested?.Invoke(task);
            }
        });
        ToggleTaskCompleteCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is not null)
                SelectedTask.IsDone = !SelectedTask.IsDone;
        });
        DeleteTaskCommand = new RelayCommand(_ =>
        {
            if (SelectedTask is not null)
            {
                if (_store.Remove(SelectedTask))
                    IsDetailsOpen = false;
            }
        });
        ResetBoardFiltersCommand = new RelayCommand(_ =>
        {
            SearchText = string.Empty;
            SelectedPriorityFilter = PriorityFilters[0];
            foreach (var tag in TagFilters)
                tag.IsSelected = false;
        });

        Statistics = new ObservableCollection<DashboardStatistic> { _doneToday, _focusToday, _streak };

        // Дані з БД (фокус-сесії, серійний період) підтягуємо з невеликою затримкою після змін
        // і раз на хвилину — так само оновлюється розклад, дата та «поточний» пункт.
        _activityDebounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _activityDebounce.Tick += (_, _) =>
        {
            _activityDebounce.Stop();
            _ = RefreshActivityAsync();
        };

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _clockTimer.Tick += (_, _) =>
        {
            RefreshSchedule();
            RefreshRowStatistics();
            _ = RefreshActivityAsync();
        };
        _clockTimer.Start();

        RefreshBoard();
        RefreshRowStatistics();
        RefreshSchedule();
        _ = RefreshActivityAsync();
        _ = RefreshCatalogAsync();
    }

    public ObservableCollection<DashboardColumn> Columns { get; } = new();
    public ObservableCollection<TagOption> TagFilters { get; } = [];
    private static readonly string[] PriorityFilterTexts = ["Усі пріоритети", "Критичний", "Високий", "Середній", "Низький"];
    public string[] PriorityFilters => PriorityFilterTexts.Select(Localizer.Translate).ToArray();
    public event Action<TaskRow>? EditTaskRequested;
    public TimerViewModel Timer => _timer;
    public int TotalTaskCount => Columns.Sum(column => column.Tasks.Count);

    public TaskRow? SelectedTask
    {
        get => _selectedTask;
        private set
        {
            SetField(ref _selectedTask, value);
        }
    }

    // ───── вкладення задачі (файли, папки, посилання) ─────

    public ObservableCollection<AttachmentItem> Files { get; } = [];
    public ObservableCollection<AttachmentItem> Links { get; } = [];
    public string FilesCountText => $"{Files.Count} {Uk.Plural(Files.Count, "ресурс", "ресурси", "ресурсів")}";
    public string LinksCountText => $"{Links.Count} {Uk.Plural(Links.Count, "посилання", "посилання", "посилань")}";
    public bool HasFiles => Files.Count > 0;
    public bool HasLinks => Links.Count > 0;

    public ICommand AttachFileCommand { get; }
    public ICommand AttachFolderCommand { get; }
    public ICommand RemoveAttachmentCommand { get; }
    public ICommand OpenAttachmentCommand { get; }

    private void LoadAttachments()
    {
        Files.Clear();
        Links.Clear();
        if (SelectedTask is { Id: not 0 } task)
        {
            foreach (var attachment in _store.Attachments.GetFor(task.Id))
                (attachment.Kind == AttachmentKind.Link ? Links : Files).Add(new AttachmentItem(attachment, Localizer));
        }

        OnPropertyChanged(nameof(FilesCountText));
        OnPropertyChanged(nameof(LinksCountText));
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasLinks));
    }

    private void AttachFile()
    {
        if (SelectedTask is not { Id: not 0 } task) return;
        var paths = _filePicker.PickFiles();
        if (paths.Count == 0) return;

        foreach (var path in paths)
        {
            _store.Attachments.Add(new TaskAttachment
            {
                TaskId = task.Id,
                Kind = AttachmentKind.File,
                Name = Path.GetFileName(path),
                Target = path,
                SizeBytes = new FileInfo(path).Length
            });
        }

        LoadAttachments();
    }

    private void AttachFolder()
    {
        if (SelectedTask is not { Id: not 0 } task) return;
        var folder = _filePicker.PickFolder();
        if (string.IsNullOrEmpty(folder)) return;

        _store.Attachments.Add(new TaskAttachment
        {
            TaskId = task.Id,
            Kind = AttachmentKind.Folder,
            Name = new DirectoryInfo(folder).Name,
            Target = folder
        });
        LoadAttachments();
    }

    private void RemoveAttachment(AttachmentItem? item)
    {
        if (item is null) return;
        _store.Attachments.Remove(item.Model.Id);
        LoadAttachments();
    }

    private void OpenAttachment(AttachmentItem? item)
    {
        if (item is null) return;
        try
        {
            _launcher.Open(item.Target);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Localizer.Translate("Не вдалося відкрити"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public bool IsDetailsOpen { get => _isDetailsOpen; set => SetField(ref _isDetailsOpen, value); }
    public string SelectedPriorityFilter
    {
        get => Localizer.Translate(_selectedPriorityFilter);
        set
        {
            if (string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
            var source = PriorityFilterTexts.FirstOrDefault(item => Localizer.Translate(item) == value) ?? value;
            if (SetField(ref _selectedPriorityFilter, source)) RefreshBoard();
        }
    }
    public string[] TaskStatuses { get; } = { "Todo", "InProgress", "Review", "Deferred", "Done" };

    public ICommand CloseDetailsCommand { get; }
    public ICommand StartTaskFocusCommand { get; }
    public ICommand ToggleTaskCompleteCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand EditTaskCommand { get; }
    public ICommand ResetBoardFiltersCommand { get; }
    public ICommand ToggleTimerCommand => _timer.ToggleCommand;
    public ICommand ResetTimerCommand => _timer.ResetCommand;
    public ICommand PauseTimerCommand => _timer.PauseCommand;

    /// <summary>«Виконано сьогодні», «Час фокусування», «Активний серійний період» — рахуються з даних.</summary>
    public ObservableCollection<DashboardStatistic> Statistics { get; }

    /// <summary>Розклад на сьогодні: задачі з дедлайном сьогодні + фокус-сесії з БД.</summary>
    public ObservableCollection<DashboardScheduleItem> Schedule { get; } = new();

    public string ScheduleDateText { get => _scheduleDateText; private set => SetField(ref _scheduleDateText, value); }
    public bool IsScheduleEmpty => Schedule.Count == 0;

    protected override void OnSearchChanged() => RefreshBoard();

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(PriorityFilters));
        OnPropertyChanged(nameof(SelectedPriorityFilter));
        foreach (var statistic in Statistics)
            statistic.RefreshLanguage();
        foreach (var column in Columns)
            column.RefreshLanguage();
        RefreshBoard();
        RefreshRowStatistics();
        RefreshSchedule();
    }

    public async Task RefreshCatalogAsync()
    {
        try
        {
            var tags = await _catalog.GetTagsAsync();
            var selectedTagIds = TagFilters.Where(tag => tag.IsSelected).Select(tag => tag.Id).ToHashSet();
            TagFilters.Clear();
            foreach (var tag in tags)
            {
                var option = new TagOption(tag.Id, tag.Name, tag.Color);
                option.IsSelected = selectedTagIds.Contains(tag.Id);
                option.PropertyChanged += (_, _) => RefreshBoard();
                TagFilters.Add(option);
            }
            RefreshBoard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Localizer.Translate("Не вдалося завантажити фільтри")}:\n{ex.Message}",
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ═════════ Drag-and-drop ═════════

    /// <summary>
    /// Переносить картку в іншу колонку: змінює Status рядка, а TaskBoardStore сам зберігає це в БД
    /// і сповіщає дошку (RefreshBoard перебудовує колонки).
    /// </summary>
    public void MoveTask(DashboardTask card, DashboardColumn target)
    {
        var row = card.Row;
        if (row.Status == target.Status)
            return;

        row.Status = target.Status;
    }

    public void ClearDragHighlight()
    {
        foreach (var column in Columns)
            column.IsDragOver = false;
    }

    // ═════════ Оновлення ═════════

    private void OnStoreChanged(object? sender, EventArgs e)
    {
        _clockTimer.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            if (_disposed)
                return;

            RefreshBoard();
            RefreshRowStatistics();
            RefreshSchedule();
            ScheduleActivityRefresh();
        }));
    }

    private void OnFocusSessionFinished(object? sender, EventArgs e) => ScheduleActivityRefresh();

    private void ScheduleActivityRefresh()
    {
        if (_disposed) return;
        _activityDebounce.Stop();
        _activityDebounce.Start();
    }

    private void OpenTask(TaskRow row)
    {
        SelectedTask = row;
        LoadAttachments();
        IsDetailsOpen = true;
    }

    private void RefreshBoard()
    {
        // Порядок колонок — як на макеті: До виконання → У процесі → На перевірці → Виконано → Відкладені.
        var columns = new[]
        {
            ("До виконання", "Todo"),
            ("У процесі", "InProgress"),
            ("На перевірці", "Review"),
            ("Виконано", "Done"),
            ("Відкладені", "Deferred")
        };

        Columns.Clear();
        foreach (var (title, status) in columns)
        {
            var tasks = _store.Rows
                .Where(row => row.Status == status && MatchesSearch(row) && MatchesBoardFilters(row))
                .Select(row => new DashboardTask(
                    Localizer,
                    row,
                    new RelayCommand(_ => OpenTask(row)),
                    StartTaskFocusCommand))
                .ToList();
            Columns.Add(new DashboardColumn(title, status, tasks, Localizer));
        }

        OnPropertyChanged(nameof(TotalTaskCount));
    }

    /// <summary>Статистика, яку можна порахувати одразу з задач у пам'яті.</summary>
    private void RefreshRowStatistics()
    {
        var today = DateTime.Today;
        var done = _store.Rows.Count(r => r.IsDone && r.CompletedAt?.Date == today);
        _doneToday.Value = $"{done} {Uk.Plural(done, "завдання", "завдання", "завдань")}";
    }

    private async Task RefreshActivityAsync()
    {
        if (_disposed) return;

        // Якщо запит уже виконується, а дані змінилися — повторимо його одразу після завершення.
        if (_activityLoading)
        {
            _activityDirty = true;
            return;
        }

        _activityLoading = true;
        try
        {
            do
            {
                _activityDirty = false;
                var summary = await _activity.GetSummaryAsync();
                if (_disposed) return;

                _sessionsToday = summary.SessionsToday;
                _focusMinutesToday = summary.FocusMinutesToday;
                _streakDays = summary.StreakDays;

                _focusToday.Value = Uk.Duration(_focusMinutesToday);
                _streak.Value = $"{_streakDays} {Uk.Plural(_streakDays, "день", "дні", "днів")}";
                UserSession.Instance.SetStreak(_streakDays);
                RefreshSchedule();
            }
            while (_activityDirty && !_disposed);
        }
        catch
        {
            // Статистика — допоміжна: залишаємо попередні значення, застосунок працює далі.
        }
        finally
        {
            _activityLoading = false;
        }
    }

    private void RefreshSchedule()
    {
        var now = DateTime.Now;
        var today = now.Date;
        ScheduleDateText = Uk.ShortUpper(today);

        var entries = new List<(DateTime Start, DashboardScheduleItem Item)>();

        foreach (var row in _store.Rows.Where(r => r.DueDate.Date == today))
        {
            var hasTime = row.DueDate.TimeOfDay != TimeSpan.Zero;
            var duration = row.EstimatedMinutes is > 0
                ? Uk.Duration(row.EstimatedMinutes.Value)
                : Localizer.Translate("Без оцінки часу");
            entries.Add((
                row.DueDate,
                new DashboardScheduleItem(hasTime ? row.DueDate.ToString("HH:mm") : "—", row.Title, duration, row.IsDone)));
        }

        foreach (var session in _sessionsToday)
        {
            var start = session.StartUtc.ToLocalTime();
            var title = string.IsNullOrWhiteSpace(session.TaskTitle)
                ? Localizer.Translate("Фокус-сесія")
                : session.TaskTitle;
            var suffix = Localizer.Translate(session.IsCompleted ? "фокусу" : "фокусу (перервано)");
            var duration = Localizer.IsEnglish
                ? $"{session.DurationMinutes} min {suffix}"
                : $"{session.DurationMinutes} хв {suffix}";
            entries.Add((
                start,
                new DashboardScheduleItem(start.ToString("HH:mm"), title, duration, session.IsCompleted)));
        }

        var ordered = entries.OrderBy(e => e.Start).ToList();

        // Найближчий невиконаний пункт підсвічуємо як «наступний».
        var next = ordered.FirstOrDefault(e => !e.Item.IsDone && e.Start >= now.AddMinutes(-1));
        if (next.Item is not null)
            next.Item.IsNext = true;

        Schedule.Clear();
        foreach (var entry in ordered)
            Schedule.Add(entry.Item);

        OnPropertyChanged(nameof(IsScheduleEmpty));
    }

    private bool MatchesSearch(TaskRow row) =>
        string.IsNullOrWhiteSpace(SearchText)
        || row.Title.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
        || row.Description.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);

    private bool MatchesBoardFilters(TaskRow row)
    {
        if (_selectedPriorityFilter != "Усі пріоритети"
            && !row.PriorityLabel.Equals(Localizer.Translate(_selectedPriorityFilter), StringComparison.OrdinalIgnoreCase))
            return false;
        var selectedTagIds = TagFilters.Where(tag => tag.IsSelected).Select(tag => tag.Id).ToArray();
        return selectedTagIds.Length == 0 || row.TagIds.Intersect(selectedTagIds).Any();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _clockTimer.Stop();
        _activityDebounce.Stop();
        _store.Changed -= OnStoreChanged;
        _timer.FocusSessionFinished -= OnFocusSessionFinished;
    }
}

public class DashboardColumn(string title, string status, IEnumerable<DashboardTask> tasks, ILocalizer localizer) : BoardBase
{
    private bool _isDragOver;

    public string Title => localizer.Translate(title);
    public void RefreshLanguage() => OnPropertyChanged(nameof(Title));

    /// <summary>Значення TaskRow.Status, яке отримає картка після перенесення в цю колонку.</summary>
    public string Status { get; } = status;

    public ObservableCollection<DashboardTask> Tasks { get; } = new(tasks);

    /// <summary>Колонка підсвічується, поки над нею тримають картку.</summary>
    public bool IsDragOver { get => _isDragOver; set => SetField(ref _isDragOver, value); }
}

public class DashboardTask(ILocalizer localizer, TaskRow task, ICommand openCommand, ICommand focusCommand)
{
    public TaskRow Row { get; } = task;
    public string Priority => Row.PriorityLabel;
    public string Title => Row.Title;
    public string DueDate => Row.IsDone
        ? localizer.Translate("Виконано")
        : Row.IsOverdue ? localizer.Translate("Прострочено") : Row.DueText;
    public string EstimateText => Row.EstimateText;
    public bool IsCompleted => Row.IsDone;
    public bool IsOverdue => Row.IsOverdue;
    public int ResourceCount => Row.ResourceCount;
    public ICommand OpenCommand { get; } = openCommand;
    public ICommand FocusCommand { get; } = focusCommand;
}

public class DashboardStatistic(string label, string value, ILocalizer localizer) : BoardBase
{
    private string _value = value;

    public string Label => localizer.Translate(label);
    public string Value { get => _value; set => SetField(ref _value, value); }
    public void RefreshLanguage() => OnPropertyChanged(nameof(Label));
}

public class DashboardScheduleItem(string time, string title, string duration, bool isDone) : BoardBase
{
    private bool _isNext;

    public string Time { get; } = time;
    public string Title { get; } = title;
    public string Duration { get; } = duration;
    public bool IsDone { get; } = isDone;
    public bool IsNext { get => _isNext; set => SetField(ref _isNext, value); }
}
