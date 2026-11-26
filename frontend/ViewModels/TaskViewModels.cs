using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using Frontend.Services;

namespace Frontend.ViewModels;

public class PriorityOption(string key, string label, ILocalizer localizer) : BoardBase
{
    public string Key { get; } = key;
    public string Label => localizer.Translate(label);

    public void RefreshLanguage() => OnPropertyChanged(nameof(Label));
}

/// <summary>Варіант списку «Оцінка часу».</summary>
public class EstimateOption(int minutes, ILocalizer localizer) : BoardBase
{
    private readonly Uk _uk = new(localizer);

    public int Minutes { get; } = minutes;
    public string Label => _uk.Duration(Minutes);
    public void RefreshLanguage() => OnPropertyChanged(nameof(Label));
}

public class TaskRow : BoardBase
{
    private readonly IAppClock _clock;
    private readonly ILocalizer _localizer;
    private string _status = "Todo";
    private string _title = "";
    private string _priorityKey = "Low";
    private string _description = "";
    private DateTime _dueDate;
    private int _resourceCount;
    private int _id;
    private int? _estimatedMinutes;
    private DateTime? _completedAt;
    private DateTime _createdAt;
    private int[] _tagIds = [];
    private TaskSubtaskDto[] _subtasks = [];

    public TaskRow(IAppClock? clock = null, ILocalizer? localizer = null)
    {
        _clock = clock ?? AppClock.Instance;
        _localizer = localizer ?? UiPreferences.Instance;
        Uk = new Uk(_localizer);
        _dueDate = _clock.Today;
        PropertyChangedEventManager.AddHandler(_localizer, OnPreferencesChanged, nameof(ILocalizer.Language));
    }

    private Uk Uk { get; }

    /// <summary>Мова, якою рядок показує свої тексти (передається далі, напр. у події календаря).</summary>
    public ILocalizer Localizer => _localizer;

    /// <summary>Id задачі в БД; 0 — ще не збережено.</summary>
    public int Id { get => _id; set => SetField(ref _id, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetField(ref _createdAt, value); }
    public int[] TagIds { get => _tagIds; set => SetField(ref _tagIds, value); }
    public TaskSubtaskDto[] Subtasks { get => _subtasks; set => SetField(ref _subtasks, value); }
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string PriorityKey
    {
        get => _priorityKey;
        set
        {
            if (!SetField(ref _priorityKey, value)) return;
            OnPropertyChanged(nameof(PriorityLabel));
            OnPropertyChanged(nameof(PriorityRank));
        }
    }
    public string Description { get => _description; set => SetField(ref _description, value); }
    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (!SetField(ref _dueDate, value)) return;
            OnPropertyChanged(nameof(DueText));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }
    public int ResourceCount { get => _resourceCount; set => SetField(ref _resourceCount, value); }

    /// <summary>Оцінка часу, хв (з форми «Нове завдання»).</summary>
    public int? EstimatedMinutes
    {
        get => _estimatedMinutes;
        set
        {
            if (!SetField(ref _estimatedMinutes, value)) return;
            OnPropertyChanged(nameof(EstimateText));
        }
    }

    /// <summary>Локальний час виконання; потрібен для статистики «виконано сьогодні» та серійного періоду.</summary>
    public DateTime? CompletedAt { get => _completedAt; set => SetField(ref _completedAt, value); }

    public string Status
    {
        get => _status;
        set
        {
            if (!SetField(ref _status, value)) return;
            CompletedAt = value == "Done" ? _clock.Now : null;
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public bool IsDone
    {
        get => Status == "Done";
        set => Status = value ? "Done" : "Todo";
    }

    public string StatusText => _localizer.Translate(Status switch
    {
        "Done" => "Виконано",
        "InProgress" => "Виконується",
        "Review" => "На перевірці",
        "Deferred" => "Відкладено",
        _ => "Завдання"
    });
    public string StatusDisplay => IsDone ? StatusText + " ✓" : StatusText;
    public string PriorityLabel => _localizer.Translate(
        PriorityKey switch { "Critical" => "КРИТИЧНИЙ", "High" => "ВИСОКИЙ", "Medium" => "СЕРЕДНІЙ", _ => "НИЗЬКИЙ" });
    public int PriorityRank => PriorityKey switch { "Critical" => 0, "High" => 1, "Medium" => 2, _ => 3 };
    public string EstimateText => EstimatedMinutes is > 0 ? Uk.Duration(EstimatedMinutes.Value).ToUpperInvariant() : string.Empty;
    public string DueText => DueDate.Date == _clock.Today ? _localizer.Translate("Сьогодні") : Uk.Short(DueDate);
    public bool IsOverdue => !IsDone && DueDate.Date < _clock.Today;

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(PriorityLabel));
        OnPropertyChanged(nameof(EstimateText));
        OnPropertyChanged(nameof(DueText));
    }

    public ICommand DeleteCommand { get; set; } = null!;
    public ICommand EditCommand { get; set; } = null!;
}

public class TasksViewModel : PageViewModelBase
{
    private readonly TaskBoardStore _store;
    private readonly ICatalogService _catalog;
    private string _selStatus = "Усі статуси";
    private string _selPriority = "Усі пріоритети";
    private string _selSort = "Дедлайном";
    private bool _isAddOpen;
    private string _newTitle = "";
    private string _newDescription = "";
    private DateTime _newDueDate;
    private int _newDueHour = 18;
    private int _newDueMinute;
    private bool _isDatePickerOpen;
    private TaskRow? _editingTask;
    private string _formError = string.Empty;
    private bool _isTagManagerOpen;
    private string _catalogName = string.Empty;
    private string _catalogColor = "#3B6EF5";
    private string _catalogError = string.Empty;
    private TagDto? _selectedTagRecord;
    private string _newSubtaskTitle = string.Empty;
    private PriorityOption _newPriority;
    private EstimateOption _newEstimate;

    public TasksViewModel(TaskBoardStore store, ICatalogService catalog)
        : base(store.Localizer)
    {
        _store = store;
        _newDueDate = store.Clock.Today;
        _catalog = catalog;
        // Порядок і назви — як на макеті модалки «Нове завдання».
        Priorities = new[]
        {
            new PriorityOption("Medium", "Середній", Localizer),
            new PriorityOption("Low", "Низький", Localizer),
            new PriorityOption("High", "Високий", Localizer),
            new PriorityOption("Critical", "Критичний", Localizer)
        };
        EstimateOptions = new[] { 15, 25, 30, 45, 60, 90, 120, 180 }.Select(m => new EstimateOption(m, Localizer)).ToArray();
        _newPriority = Priorities[0];
        _newEstimate = EstimateOptions.First(o => o.Minutes == 60);
        Hours = Enumerable.Range(0, 24).ToArray();
        Minutes = new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 };

        _store.Changed += (_, _) => PostRefresh();
        // Нове/видалене завдання одразу потрапляє у список.
        _store.Rows.CollectionChanged += (_, _) => RefreshView();
        RefreshView();

        OpenNewTaskCommand = new RelayCommand(_ => OpenAdd());
        CloseAddCommand = new RelayCommand(_ => { IsDatePickerOpen = false; IsAddOpen = false; });
        SelectPriorityCommand = new RelayCommand(p => { if (p is PriorityOption o) NewPriority = o; });
        CreateTaskCommand = new RelayCommand(_ => Create());
        EditTaskCommand = new RelayCommand(task => { if (task is TaskRow row) OpenEdit(row); });
        OpenTagManagerCommand = new RelayCommand(async _ => await OpenTagManagerAsync());
        CloseTagManagerCommand = new RelayCommand(_ => IsTagManagerOpen = false);
        NewTagCommand = new RelayCommand(_ => BeginNewTag());
        SaveTagCommand = new RelayCommand(async _ => await SaveTagAsync());
        DeleteTagCommand = new RelayCommand(async _ => await DeleteTagAsync());
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        AddSubtaskCommand = new RelayCommand(_ => AddSubtask());
        RemoveSubtaskCommand = new RelayCommand(item => { if (item is EditableSubtask subtask) ChecklistItems.Remove(subtask); });
        MoveSubtaskUpCommand = new RelayCommand(item => MoveSubtask(item as EditableSubtask, -1));
        MoveSubtaskDownCommand = new RelayCommand(item => MoveSubtask(item as EditableSubtask, 1));
        Rows.CollectionChanged += (_, _) => PrepareEditCommands();
        PrepareEditCommands();
        _ = LoadCatalogAsync();
        ToggleAllCommand = new RelayCommand(_ =>
        {
            var target = !AllDone;
            foreach (var r in VisibleRows.ToList()) r.IsDone = target;
        });
    }

    public ObservableCollection<TaskRow> Rows => _store.Rows;
    /// <summary>Відфільтровані та відсортовані рядки; перебудовується явно при кожній зміні сховища.</summary>
    public ObservableCollection<TaskRow> VisibleRows { get; } = [];

    private static readonly string[] StatusFilterTexts = { "Усі статуси", "Завдання", "Виконується", "На перевірці", "Відкладено", "Виконано" };
    private static readonly string[] PriorityFilterTexts = { "Усі пріоритети", "Критичний", "Високий", "Середній", "Низький" };
    private static readonly string[] SortOptionTexts = { "Дедлайном", "Пріоритетом", "Датою створення", "Назвою" };

    public string[] StatusFilters => StatusFilterTexts.Select(Localizer.Translate).ToArray();
    public string[] PriorityFilters => PriorityFilterTexts.Select(Localizer.Translate).ToArray();
    public string[] SortOptions => SortOptionTexts.Select(Localizer.Translate).ToArray();

    public ObservableCollection<TagDto> TagRecords { get; } = [];
    public ObservableCollection<TagOption> TaskTagOptions { get; } = [];
    public ObservableCollection<TagOption> FilterTagOptions { get; } = [];
    public ObservableCollection<EditableSubtask> ChecklistItems { get; } = [];
    public string[] ColorOptions { get; } =
        ["#3B6EF5", "#10B981", "#F59E0B", "#EF4444", "#A855F7", "#06B6D4", "#EC4899"];
    public PriorityOption[] Priorities { get; }
    public EstimateOption[] EstimateOptions { get; }
    public int[] Hours { get; }
    public int[] Minutes { get; }

    public string SelectedStatus
    {
        get => Localizer.Translate(_selStatus);
        set
        {
            // ComboBox пише null, коли список джерела перезавантажується — це не вибір користувача.
            if (string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
            if (SetField(ref _selStatus, ResolveOption(value, StatusFilterTexts))) RefreshView();
        }
    }
    public string SelectedPriority
    {
        get => Localizer.Translate(_selPriority);
        set
        {
            // ComboBox пише null, коли список джерела перезавантажується — це не вибір користувача.
            if (string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
            if (SetField(ref _selPriority, ResolveOption(value, PriorityFilterTexts))) RefreshView();
        }
    }
    public string SelectedSort
    {
        get => Localizer.Translate(_selSort);
        set
        {
            // ComboBox пише null, коли список джерела перезавантажується — це не вибір користувача.
            if (string.IsNullOrEmpty(value)) { OnPropertyChanged(); return; }
            if (SetField(ref _selSort, ResolveOption(value, SortOptionTexts))) ApplySort();
        }
    }

    public bool AllDone => VisibleRows.Count > 0 && VisibleRows.All(r => r.IsDone);
    public bool IsEmpty => VisibleRows.Count == 0;

    public bool IsAddOpen { get => _isAddOpen; set => SetField(ref _isAddOpen, value); }
    public bool IsTagManagerOpen { get => _isTagManagerOpen; set => SetField(ref _isTagManagerOpen, value); }
    public string CatalogName { get => _catalogName; set => SetField(ref _catalogName, value); }
    public string CatalogColor { get => _catalogColor; set => SetField(ref _catalogColor, value); }
    public string CatalogError { get => _catalogError; private set => SetField(ref _catalogError, value); }
    public TagDto? SelectedTagRecord
    {
        get => _selectedTagRecord;
        set
        {
            if (!SetField(ref _selectedTagRecord, value) || value is null) return;
            CatalogName = value.Name;
            CatalogColor = value.Color;
            CatalogError = string.Empty;
        }
    }
    public bool IsEditing => _editingTask is not null;
    public string FormTitle => Localizer.Translate(IsEditing ? "Редагувати завдання" : "Нове завдання");
    public string SubmitButtonText => Localizer.Translate(IsEditing ? "Зберегти зміни" : "Створити завдання");
    public string FormError
    {
        get => Localizer.Translate(_formError);
        private set
        {
            if (SetField(ref _formError, value))
                OnPropertyChanged(nameof(TitleValidationMessage));
        }
    }
    public string NewSubtaskTitle
    {
        get => _newSubtaskTitle;
        set
        {
            if (SetField(ref _newSubtaskTitle, value))
                OnPropertyChanged(nameof(CanAddSubtask));
        }
    }
    public bool CanAddSubtask => !string.IsNullOrWhiteSpace(NewSubtaskTitle) && NewSubtaskTitle.Trim().Length <= 255;
    public string NewTitle
    {
        get => _newTitle;
        set
        {
            if (!SetField(ref _newTitle, value)) return;
            OnPropertyChanged(nameof(CanCreate));
            OnPropertyChanged(nameof(TitleValidationMessage));
            if (CanCreate && !string.IsNullOrEmpty(FormError))
                FormError = string.Empty;
        }
    }
    public string NewDescription { get => _newDescription; set => SetField(ref _newDescription, value); }
    public DateTime NewDueDate
    {
        get => _newDueDate;
        set { if (SetField(ref _newDueDate, value.Date)) OnPropertyChanged(nameof(NewDueText)); }
    }

    /// <summary>Проксі для Calendar.SelectedDate (він nullable): повторний клік по даті не скидає вибір.</summary>
    public DateTime? NewDueDatePick
    {
        get => _newDueDate;
        set
        {
            if (value.HasValue)
            {
                NewDueDate = value.Value;
                IsDatePickerOpen = false;
            }
            OnPropertyChanged();
        }
    }

    public int NewDueHour { get => _newDueHour; set { if (SetField(ref _newDueHour, value)) OnPropertyChanged(nameof(NewDueText)); } }
    public int NewDueMinute { get => _newDueMinute; set { if (SetField(ref _newDueMinute, value)) OnPropertyChanged(nameof(NewDueText)); } }
    public bool IsDatePickerOpen { get => _isDatePickerOpen; set => SetField(ref _isDatePickerOpen, value); }

    /// <summary>Повний термін (дата + час) із форми.</summary>
    public DateTime NewDueDateTime => _newDueDate.Date.AddHours(_newDueHour).AddMinutes(_newDueMinute);

    /// <summary>«22 ВЕР 2026 · 18:00».</summary>
    public string NewDueText => $"{Uk.ShortUpper(NewDueDate, withYear: true)} · {NewDueHour:00}:{NewDueMinute:00}";

    public PriorityOption NewPriority { get => _newPriority; set { if (value != null) SetField(ref _newPriority, value); } }
    public EstimateOption NewEstimate { get => _newEstimate; set { if (value != null) SetField(ref _newEstimate, value); } }
    public bool CanCreate => NewTitle.Trim().Length is >= 1 and <= 255;
    public string TitleValidationMessage => CanCreate
        ? FormError
        : Localizer.Translate(string.IsNullOrWhiteSpace(NewTitle)
            ? "Введіть назву завдання."
            : "Назва не може бути довшою за 255 символів.");

    public ICommand CloseAddCommand { get; }
    public ICommand SelectPriorityCommand { get; }
    public ICommand CreateTaskCommand { get; }
    public ICommand EditTaskCommand { get; }
    public ICommand OpenTagManagerCommand { get; }
    public ICommand CloseTagManagerCommand { get; }
    public ICommand NewTagCommand { get; }
    public ICommand SaveTagCommand { get; }
    public ICommand DeleteTagCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand AddSubtaskCommand { get; }
    public ICommand RemoveSubtaskCommand { get; }
    public ICommand MoveSubtaskUpCommand { get; }
    public ICommand MoveSubtaskDownCommand { get; }
    public ICommand ToggleAllCommand { get; }

    protected override void OnSearchChanged() => RefreshView();

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(StatusFilters));
        OnPropertyChanged(nameof(PriorityFilters));
        OnPropertyChanged(nameof(SortOptions));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedPriority));
        OnPropertyChanged(nameof(SelectedSort));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SubmitButtonText));
        OnPropertyChanged(nameof(TitleValidationMessage));
        foreach (var priority in Priorities)
            priority.RefreshLanguage();
        foreach (var estimate in EstimateOptions)
            estimate.RefreshLanguage();

        RefreshView();
    }

    /// <summary>Оновлює список у потоці UI; без застосунку (юніт-тести) — одразу.</summary>
    private void PostRefresh()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
            RefreshView();
        else
            dispatcher.InvokeAsync(RefreshView);
    }

    private void RefreshView()
    {
        IEnumerable<TaskRow> query = Rows.Where(Filter);
        query = _selSort switch
        {
            "Назвою" => query.OrderBy(r => r.IsDone).ThenBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase),
            "Пріоритетом" => query.OrderBy(r => r.IsDone).ThenBy(r => r.PriorityRank),
            "Датою створення" => query.OrderBy(r => r.IsDone).ThenBy(r => r.CreatedAt),
            _ => query.OrderBy(r => r.IsDone).ThenBy(r => r.DueDate)
        };
        var items = query.ToList();
        VisibleRows.Clear();
        foreach (var item in items)
            VisibleRows.Add(item);
        RaiseList();
    }

    private bool Filter(TaskRow r)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            if (!r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !r.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) return false;
        }
        if (SelectedStatus != StatusFilters[0] && r.StatusText != SelectedStatus) return false;
        if (SelectedPriority != PriorityFilters[0] &&
            !r.PriorityLabel.Equals(SelectedPriority, StringComparison.OrdinalIgnoreCase)) return false;
        var selectedTagIds = FilterTagOptions.Where(option => option.IsSelected).Select(option => option.Id).ToArray();
        if (selectedTagIds.Length > 0 && !r.TagIds.Intersect(selectedTagIds).Any()) return false;
        return true;
    }

    private void ApplySort() => RefreshView();

    private void RaiseList()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(AllDone));
    }

    private void AddRow(TaskRow row) => _store.Add(row);

    private void OpenAdd()
    {
        _editingTask = null;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SubmitButtonText));
        FormError = string.Empty;
        SetTaskTagSelection([]);
        ChecklistItems.Clear();
        NewSubtaskTitle = string.Empty;
        NewTitle = "";
        NewDescription = "";
        // За замовчуванням — сьогодні о 18:00 (якщо вже пізніше — наступна повна година).
        var clock = _store.Clock;
        var due = clock.Today.AddHours(18);
        if (due <= clock.Now)
            due = clock.Today.AddHours(clock.Now.Hour + 1);
        NewDueDate = due.Date;
        NewDueHour = due.Hour;
        NewDueMinute = 0;
        NewPriority = Priorities[0];
        NewEstimate = EstimateOptions.First(o => o.Minutes == 60);
        IsDatePickerOpen = false;
        IsAddOpen = true;
    }

    private void OpenEdit(TaskRow row)
    {
        _editingTask = row;
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SubmitButtonText));
        FormError = string.Empty;
        NewTitle = row.Title;
        NewDescription = row.Description;
        NewDueDate = row.DueDate.Date;
        NewDueHour = row.DueDate.Hour;
        NewDueMinute = row.DueDate.Minute;
        NewPriority = Priorities.First(option => option.Key == row.PriorityKey);
        SetTaskTagSelection(row.TagIds);
        ChecklistItems.Clear();
        foreach (var subtask in row.Subtasks)
            ChecklistItems.Add(new EditableSubtask(subtask));
        NewSubtaskTitle = string.Empty;
        NewEstimate = EstimateOptions.FirstOrDefault(option => option.Minutes == row.EstimatedMinutes)
            ?? new EstimateOption(row.EstimatedMinutes ?? 60, Localizer);
        IsDatePickerOpen = false;
        IsAddOpen = true;
    }

    public void OpenEditTask(TaskRow row) => OpenEdit(row);

    private void Create()
    {
        if (!CanCreate)
        {
            FormError = string.IsNullOrWhiteSpace(NewTitle)
                ? "Введіть назву завдання."
                : "Назва не може бути довшою за 255 символів.";
            return;
        }

        FormError = string.Empty;
        if (_editingTask is { } task)
        {
            task.Title = NewTitle.Trim();
            task.Description = NewDescription.Trim();
            task.PriorityKey = NewPriority.Key;
            task.DueDate = NewDueDateTime;
            task.EstimatedMinutes = NewEstimate.Minutes;
            task.TagIds = TaskTagOptions.Where(option => option.IsSelected).Select(option => option.Id).ToArray();
            task.Subtasks = ChecklistItems.Select(item => item.ToDto()).ToArray();
        }
        else
        {
            AddRow(new TaskRow(_store.Clock, Localizer)
            {
                Title = NewTitle.Trim(),
                Description = NewDescription.Trim(),
                PriorityKey = NewPriority.Key,
                DueDate = NewDueDateTime,
                CreatedAt = _store.Clock.Now,
                EstimatedMinutes = NewEstimate.Minutes,
                TagIds = TaskTagOptions.Where(option => option.IsSelected).Select(option => option.Id).ToArray(),
                Subtasks = ChecklistItems.Select(item => item.ToDto()).ToArray()
            });
        }

        IsDatePickerOpen = false;
        IsAddOpen = false;
    }

    private void PrepareEditCommands()
    {
        foreach (var row in Rows)
            row.EditCommand = EditTaskCommand;
    }

    private async Task LoadCatalogAsync()
    {
        try
        {
            var tags = await _catalog.GetTagsAsync();
            ReplaceTags(tags);
            RefreshView();
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show($"{Localizer.Translate("Не вдалося завантажити теги")}:\n{ex.Message}",
                    "FocusLab", MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }

    private void ReplaceTags(IEnumerable<TagDto> tags)
    {
        TagRecords.Clear();
        TaskTagOptions.Clear();
        FilterTagOptions.Clear();
        foreach (var tag in tags)
        {
            TagRecords.Add(tag);
            TaskTagOptions.Add(new TagOption(tag.Id, tag.Name, tag.Color));
            var filterOption = new TagOption(tag.Id, tag.Name, tag.Color);
            filterOption.PropertyChanged += (_, _) => RefreshView();
            FilterTagOptions.Add(filterOption);
        }
    }

    private void SetTaskTagSelection(IEnumerable<int> selectedIds)
    {
        var selected = selectedIds.ToHashSet();
        foreach (var option in TaskTagOptions)
            option.IsSelected = selected.Contains(option.Id);
    }

    private async Task OpenTagManagerAsync()
    {
        await LoadCatalogAsync();
        CatalogError = string.Empty;
        IsTagManagerOpen = true;
    }

    private void BeginNewTag()
    {
        SelectedTagRecord = null;
        CatalogName = string.Empty;
        CatalogColor = ColorOptions[0];
        CatalogError = string.Empty;
    }

    private async Task SaveTagAsync()
    {
        try
        {
            var saved = await _catalog.SaveTagAsync(new TagDto(SelectedTagRecord?.Id ?? 0, CatalogName, CatalogColor));
            await LoadCatalogAsync();
            SelectedTagRecord = TagRecords.First(item => item.Id == saved.Id);
            CatalogError = string.Empty;
        }
        catch (Exception ex)
        {
            CatalogError = ex.Message;
        }
    }

    private async Task DeleteTagAsync()
    {
        if (SelectedTagRecord is not { } tag) return;
        if (MessageBox.Show(string.Format(Localizer.Translate("Видалити тег «{0}»?"), tag.Name),
                Localizer.Translate("Підтвердження видалення"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await _catalog.DeleteTagAsync(tag.Id);
            foreach (var row in Rows.Where(row => row.TagIds.Contains(tag.Id)))
                row.TagIds = row.TagIds.Where(id => id != tag.Id).ToArray();
            await LoadCatalogAsync();
            BeginNewTag();
        }
        catch (Exception ex)
        {
            CatalogError = ex.Message;
        }
    }

    private void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedStatus = StatusFilters[0];
        SelectedPriority = PriorityFilters[0];
        foreach (var option in FilterTagOptions)
            option.IsSelected = false;
        RefreshView();
    }

    private void AddSubtask()
    {
        if (!CanAddSubtask) return;
        ChecklistItems.Add(new EditableSubtask(Guid.NewGuid().ToString("N"), NewSubtaskTitle.Trim(), false));
        NewSubtaskTitle = string.Empty;
    }

    private void MoveSubtask(EditableSubtask? item, int offset)
    {
        if (item is null) return;
        var index = ChecklistItems.IndexOf(item);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= ChecklistItems.Count) return;
        ChecklistItems.Move(index, target);
    }

    private string ResolveOption(string value, IEnumerable<string> options)
        => options.FirstOrDefault(option => Localizer.Translate(option) == value) ?? value;

}

public sealed class TagOption(int id, string name, string color) : BoardBase
{
    private bool _isSelected;

    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Color { get; } = color;
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
}

public sealed class EditableSubtask : BoardBase
{
    private string _title;
    private bool _isDone;

    public EditableSubtask(TaskSubtaskDto item) : this(item.Id, item.Title, item.IsDone) { }

    public EditableSubtask(string id, string title, bool isDone)
    {
        Id = id;
        _title = title;
        _isDone = isDone;
    }

    public string Id { get; }
    public string Title { get => _title; set => SetField(ref _title, value); }
    public bool IsDone { get => _isDone; set => SetField(ref _isDone, value); }
    public TaskSubtaskDto ToDto() => new(Id, Title, IsDone);
}