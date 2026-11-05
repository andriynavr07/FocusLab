using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly Action _onLogout;
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

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowTasksCommand { get; }
    public ICommand ShowCalendarCommand { get; }
    public ICommand ShowTimerCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand LogoutCommand { get; }

    public WorkspaceViewModel(TaskBoardStore taskStore, TimerViewModel timer, Action onLogout)
    {
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _dashboard = new DashboardViewModel(_taskStore, _timer);
        _tasks = new TasksViewModel(_taskStore);
        _calendar = new CalendarViewModel(_taskStore);
        _settings = new SettingsViewModel(_timer);
        _currentPage = _dashboard;

        foreach (var page in new PageViewModelBase[] { _dashboard, _tasks, _calendar, _timer, _settings })
            page.NewTaskRequested += OpenNewTask;

        ShowDashboardCommand = new RelayCommand(_ => CurrentPage = _dashboard);
        ShowTasksCommand = new RelayCommand(_ => CurrentPage = _tasks);
        ShowCalendarCommand = new RelayCommand(_ => CurrentPage = _calendar);
        ShowTimerCommand = new RelayCommand(_ => CurrentPage = _timer);
        ShowSettingsCommand = new RelayCommand(_ => CurrentPage = _settings);
        LogoutCommand = new RelayCommand(_ =>
        {
            _timer.PauseCommand.Execute(null);
            // TimerViewModel підписаний на події singleton-сервісу таймера — відписуємось,
            // інакше після кожного виходу старий VM лишався б у пам'яті.
            _timer.Dispose();
            _onLogout();
        });
    }

    private void OpenNewTask()
    {
        CurrentPage = _tasks;
        _tasks.OpenNewTaskCommand.Execute(null);
    }
}
