using System.Windows;
using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public abstract class PageViewModelBase : BoardBase
{
    private static bool _isLight;
    private static bool _isUa = true;
    private string _searchText = "";
    private bool _isNotificationsOpen;

    /// <summary>Сторінки без власної форми «Нове завдання» просять Workspace відкрити її.</summary>
    public event Action? NewTaskRequested;
    protected void RequestNewTask() => NewTaskRequested?.Invoke();

    protected PageViewModelBase()
    {
        // Ім'я, пошта та серійний період змінюються з різних місць — оновлюємо верхню панель.
        WeakEventManager<UserSession, EventArgs>.AddHandler(UserSession.Instance, nameof(UserSession.Changed), OnSessionChanged);
        ToggleThemeCommand = new RelayCommand(_ => SetTheme(!_isLight));
        ToggleLanguageCommand = new RelayCommand(_ => { _isUa = !_isUa; OnPropertyChanged(nameof(LanguageButtonText)); });
        OpenNotificationsCommand = new RelayCommand(_ => IsNotificationsOpen = true);
        OpenNewTaskCommand = new RelayCommand(_ => RequestNewTask());
    }

    protected static bool IsLightTheme => _isLight;

    protected void SetTheme(bool light)
    {
        _isLight = light;
        ThemeManager.Apply(light);
        OnPropertyChanged(nameof(ThemeButtonText));
        OnThemeChanged();
    }

    protected virtual void OnThemeChanged() { }
    protected virtual void OnSearchChanged() { }

    public string ThemeButtonText => _isLight ? "Dark" : "Light";
    public string LanguageButtonText => _isUa ? "EN" : "UA";
    public string StreakText => UserSession.Instance.StreakText;
    public string AvatarInitials => UserSession.Instance.Initials;

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StreakText));
        OnPropertyChanged(nameof(AvatarInitials));
        OnUserSessionChanged();
    }

    protected virtual void OnUserSessionChanged() { }

    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) OnSearchChanged(); }
    }

    public bool IsNotificationsOpen { get => _isNotificationsOpen; set => SetField(ref _isNotificationsOpen, value); }

    public ICommand ToggleThemeCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand OpenNotificationsCommand { get; }
    public ICommand OpenNewTaskCommand { get; protected set; }
}