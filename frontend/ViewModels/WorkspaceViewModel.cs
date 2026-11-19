using System.Windows.Input;
using FocusLab.Application.Abstractions;
using Frontend.Services;

namespace Frontend.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly Action _onLogout;
    private readonly ICatalogService _catalog;
    private readonly TaskBoardStore _taskStore;
    private readonly DashboardViewModel _dashboard;
    private readonly TasksViewModel _tasks;
    private readonly CalendarViewModel _calendar;
    private readonly TimerViewModel _timer;
    private readonly SettingsViewModel _settings;
    private PageViewModelBase _currentPage;

    public PageViewModelBase CurrentPage
    {
        get => _currentPage;
        private set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Кількість невиконаних завдань — показується бейджем біля пункту «Завдання».</summary>
    public int PendingTaskCount => _taskStore.Rows.Count(r => !r.IsDone);

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowTasksCommand { get; }
    public ICommand ShowCalendarCommand { get; }
    public ICommand ShowTimerCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand LogoutCommand { get; }

    public WorkspaceViewModel(
        TaskBoardStore taskStore,
        TimerViewModel timer,
        IActivityService activity,
        IAuthService auth,
        ICatalogService catalog,
        Action onLogout)
    {
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _dashboard = new DashboardViewModel(
            _taskStore, _timer, activity ?? throw new ArgumentNullException(nameof(activity)), _catalog);
        _tasks = new TasksViewModel(_taskStore, _catalog);
        _calendar = new CalendarViewModel(_taskStore);
        _settings = new SettingsViewModel(_timer, auth ?? throw new ArgumentNullException(nameof(auth)));
        _currentPage = _dashboard;
        _taskStore.Changed += OnTasksChanged;
        _dashboard.EditTaskRequested += OpenEditTask;

        var pages = new PageViewModelBase[] { _dashboard, _tasks, _calendar, _timer, _settings };
        foreach (var page in pages)
        {
            page.NewTaskRequested += OpenNewTask;
            page.SearchTextChanged += text =>
            {
                foreach (var other in pages)
                    if (other.SearchText != text)
                        other.SearchText = text;
            };
        }

        ShowDashboardCommand = new RelayCommand(async _ =>
        {
            CurrentPage = _dashboard;
            await _dashboard.RefreshCatalogAsync();
        });
        ShowTasksCommand = new RelayCommand(_ => CurrentPage = _tasks);
        ShowCalendarCommand = new RelayCommand(_ => CurrentPage = _calendar);
        ShowTimerCommand = new RelayCommand(_ => CurrentPage = _timer);
        ShowSettingsCommand = new RelayCommand(_ => CurrentPage = _settings);
        LogoutCommand = new RelayCommand(_ =>
        {
            _timer.PauseCommand.Execute(null);
            // TimerViewModel підписаний на події singleton-сервісу таймера — відписуємось,
            // інакше після кожного виходу старий VM лишався б у пам'яті.
            _taskStore.Changed -= OnTasksChanged;
            _timer.Dispose();
            _dashboard.Dispose();
            _onLogout();
        });
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        // Зміна може прийти не з UI-потоку (після завантаження з БД) — оновлюємо через Dispatcher.
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            OnPropertyChanged(nameof(PendingTaskCount));
        else
            dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(PendingTaskCount)));
    }

    public TasksViewModel Tasks => _tasks;

    private void OpenNewTask()
    {
        // Форму показує TaskFormModal поверх поточної сторінки — залишаємось там, де користувач був.
        _tasks.OpenNewTaskCommand.Execute(null);
    }

    private void OpenEditTask(TaskRow row)
    {
        _tasks.OpenEditTask(row);
    }
}
