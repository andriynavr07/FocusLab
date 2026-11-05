using System.Windows.Input;

namespace Frontend.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly Action _onLogout;
    private PageViewModelBase _currentPage;

    public PageViewModelBase CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage is not null)
                _currentPage.NewTaskRequested -= OpenNewTask;

            _currentPage = value;
            _currentPage.NewTaskRequested += OpenNewTask;
            OnPropertyChanged();
        }
    }

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowTasksCommand { get; }
    public ICommand ShowCalendarCommand { get; }
    public ICommand ShowTimerCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand LogoutCommand { get; }

    public WorkspaceViewModel(Action onLogout)
    {
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));
        _currentPage = new DashboardViewModel();
        _currentPage.NewTaskRequested += OpenNewTask;

        ShowDashboardCommand = new RelayCommand(_ => CurrentPage = new DashboardViewModel());
        ShowTasksCommand = new RelayCommand(_ => CurrentPage = new TasksViewModel());
        ShowCalendarCommand = new RelayCommand(_ => CurrentPage = new CalendarViewModel());
        ShowTimerCommand = new RelayCommand(_ => CurrentPage = new TimerViewModel());
        ShowSettingsCommand = new RelayCommand(_ => CurrentPage = new SettingsViewModel());
        LogoutCommand = new RelayCommand(_ => _onLogout());
    }

    private void OpenNewTask()
    {
        var tasks = new TasksViewModel();
        CurrentPage = tasks;
        tasks.OpenNewTaskCommand.Execute(null);
    }
}
