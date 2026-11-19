using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Frontend.Services;

namespace Frontend.ViewModels;

public abstract class PageViewModelBase : BoardBase
{
    private string _searchText = "";
    private bool _isNotificationsOpen;

    /// <summary>Сторінки без власної форми «Нове завдання» просять Workspace відкрити її.</summary>
    public event Action? NewTaskRequested;
    protected void RequestNewTask() => NewTaskRequested?.Invoke();

    protected PageViewModelBase()
    {
        // Ім'я, пошта та серійний період змінюються з різних місць — оновлюємо верхню панель.
        WeakEventManager<UserSession, EventArgs>.AddHandler(UserSession.Instance, nameof(UserSession.Changed), OnSessionChanged);
        PropertyChangedEventManager.AddHandler(UiPreferences.Instance, OnPreferencesChanged, string.Empty);
        ToggleThemeCommand = new RelayCommand(_ => UiPreferences.Instance.ToggleTheme());
        ToggleLanguageCommand = new RelayCommand(_ => UiPreferences.Instance.ToggleLanguage());
        OpenNotificationsCommand = new RelayCommand(_ => IsNotificationsOpen = true);
        OpenNewTaskCommand = new RelayCommand(_ => RequestNewTask());
    }

    protected static bool IsLightTheme => UiPreferences.Instance.IsLightTheme;

    protected void SetTheme(bool light)
    {
        if (UiPreferences.Instance.IsLightTheme != light)
            UiPreferences.Instance.SetTheme(light);
    }

    protected virtual void OnThemeChanged() { }
    protected virtual void OnSearchChanged() { }

    public string ThemeButtonText => UiPreferences.Instance.ThemeButtonText;
    public string LanguageButtonText => UiPreferences.Instance.LanguageButtonText;
    public string StreakText => UserSession.Instance.StreakText;
    public string AvatarInitials => UserSession.Instance.Initials;

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StreakText));
        OnPropertyChanged(nameof(AvatarInitials));
        OnUserSessionChanged();
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UiPreferences.ThemeButtonText) or nameof(UiPreferences.IsLightTheme))
        {
            OnPropertyChanged(nameof(ThemeButtonText));
            OnThemeChanged();
        }

        if (e.PropertyName is nameof(UiPreferences.LanguageButtonText) or nameof(UiPreferences.Language))
        {
            OnPropertyChanged(nameof(LanguageButtonText));
            OnLanguageChanged();
        }
    }

    protected virtual void OnUserSessionChanged() { }
    protected virtual void OnLanguageChanged() { }

    public string SearchText
    {
        get => _searchText;
        set
        {
            value ??= string.Empty;
            if (!SetField(ref _searchText, value)) return;
            OnSearchChanged();
            SearchTextChanged?.Invoke(value);
        }
    }

    /// <summary>Пошук спільний для всіх сторінок: Workspace переносить введений текст на решту.</summary>
    public event Action<string>? SearchTextChanged;

    public bool IsNotificationsOpen { get => _isNotificationsOpen; set => SetField(ref _isNotificationsOpen, value); }

    public ICommand ToggleThemeCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand OpenNotificationsCommand { get; }
    public ICommand OpenNewTaskCommand { get; protected set; }
}