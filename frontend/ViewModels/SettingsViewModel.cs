using System.Collections.ObjectModel;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FocusLab.Application.Abstractions;
using Frontend.Services;

namespace Frontend.ViewModels;

public class AccentOption : BoardBase
{
    private bool _isSelected;
    private readonly string _name;

    private readonly ILocalizer _localizer;

    public AccentOption(string name, string hex, ILocalizer localizer)
    {
        _name = name;
        _localizer = localizer;
        Color = (Color)ColorConverter.ConvertFromString(hex);
        Brush = new SolidColorBrush(Color);
    }

    public string Name => _localizer.Translate(_name);
    public Color Color { get; }
    public SolidColorBrush Brush { get; }
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public void RefreshLanguage() => OnPropertyChanged(nameof(Name));
}

public class SettingsViewModel : PageViewModelBase
{
    private static readonly int[] WorkSteps = { 15, 25, 45, 50 };
    private static readonly int[] BreakSteps = { 3, 5, 10, 15 };
    private static readonly int[] SessionSteps = { 2, 3, 4, 6 };

    private string _userName = UserSession.Instance.Name;
    private string _email = UserSession.Instance.Email;
    private string _editName = "", _editEmail = "";
    private string _editError = "";
    private bool _isSavingProfile;
    private bool _isEditOpen, _isDeleteOpen;
    private bool _notifyTasks = true, _notifyBreaks = true, _dailyReport, _sounds = true;
    private int _workDefault = 25, _breakDefault = 5, _sessionsDefault = 4;
    private readonly TimerViewModel _timer;
    private readonly IAuthService _auth;
    private readonly IFilePickerService _filePicker;
    private readonly IProcessLauncher _launcher;

    public const string SupportUrl = "https://focuslab.app/support";

    public SettingsViewModel(
        TimerViewModel timer,
        IAuthService auth,
        IFilePickerService filePicker,
        IProcessLauncher launcher,
        ILocalizer localizer)
        : base(localizer)
    {
        _timer = timer;
        _auth = auth;
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        Accents = new ObservableCollection<AccentOption>
        {
            new("Синій", "#3B6EF5", Localizer) { IsSelected = true },
            new("Фіолетовий", "#A855F7", Localizer),
            new("Зелений", "#34B27B", Localizer),
            new("Помаранчевий", "#F0A63A", Localizer),
            new("Червоний", "#E5534B", Localizer)
        };

        SelectAccentCommand = new RelayCommand(p => { if (p is AccentOption o) SelectAccent(o); });
        OpenEditCommand = new RelayCommand(_ => { EditName = UserName; EditEmail = Email; EditError = ""; IsEditOpen = true; });
        SaveProfileCommand = new RelayCommand(async _ => await SaveProfileAsync());
        CloseModalsCommand = new RelayCommand(_ => { IsEditOpen = false; IsDeleteOpen = false; });
        OpenDeleteCommand = new RelayCommand(_ => IsDeleteOpen = true);
        ConfirmDeleteCommand = new RelayCommand(_ =>
        {
            IsDeleteOpen = false;
            MessageBox.Show(
                Localizer.Translate("Видалення акаунта недоступне: цей застосунок зараз використовує локальний демонстраційний профіль."),
                Localizer.Translate("Видалення акаунта"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });

        CycleWorkCommand = new RelayCommand(_ =>
        {
            WorkDefault = Next(WorkSteps, WorkDefault);
            _timer.WorkMinutes = WorkDefault;
        });
        CycleBreakCommand = new RelayCommand(_ =>
        {
            BreakDefault = Next(BreakSteps, BreakDefault);
            _timer.ShortBreakMinutes = BreakDefault;
        });
        CycleSessionsCommand = new RelayCommand(_ =>
        {
            SessionsDefault = Next(SessionSteps, SessionsDefault);
            _timer.SessionsPerRound = SessionsDefault;
        });

        ExportCommand = new RelayCommand(_ => Export());
        OpenSupportCommand = new RelayCommand(_ =>
        {
            try
            {
                _launcher.Open(SupportUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Localizer.Translate("Не вдалося відкрити підтримку"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<AccentOption> Accents { get; }
    public string[] Languages => new[] { "Українська", "English" }
        .Select(Localizer.Translate).ToArray();

    public string UserName
    {
        get => _userName;
        set { if (SetField(ref _userName, value)) OnPropertyChanged(nameof(Initials)); }
    }

    public string Email { get => _email; set => SetField(ref _email, value); }

    public string Initials => UserSession.Instance.Initials;

    protected override void OnUserSessionChanged()
    {
        // Профіль змінено/завантажено з БД — підтягуємо ім'я та пошту.
        UserName = UserSession.Instance.Name;
        Email = UserSession.Instance.Email;
        OnPropertyChanged(nameof(Initials));
    }

    /// <summary>Зберігає ім'я та пошту в БД (акаунт, створений під час реєстрації).</summary>
    private async Task SaveProfileAsync()
    {
        if (!CanSaveProfile) return;

        _isSavingProfile = true;
        EditError = "";
        try
        {
            var result = await _auth.UpdateProfileAsync(EditName, EditEmail);
            if (!result.Success)
            {
                EditError = result.Error ?? "Не вдалося зберегти профіль.";
                return;
            }

            // Ім'я та пошта нормалізуються сервісом — читаємо їх назад із БД.
            var profile = await _auth.GetCurrentProfileAsync();
            if (profile is not null)
                UserSession.Instance.SetProfile(profile.Name, profile.Email);
            IsEditOpen = false;
        }
        catch (Exception ex)
        {
            EditError = Localizer.Translate("Помилка збереження") + ": " + ex.Message;
        }
        finally
        {
            _isSavingProfile = false;
            OnPropertyChanged(nameof(CanSaveProfile));
        }
    }

    // ----- тема -----
    public bool IsDarkTheme
    {
        get => !IsLightTheme;
        set { if (value && IsLightTheme) SetTheme(false); }
    }

    public bool IsLightThemeSelected
    {
        get => IsLightTheme;
        set { if (value && !IsLightTheme) SetTheme(true); }
    }

    protected override void OnThemeChanged()
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightThemeSelected));
    }

    private static void SelectAccentColor(Color c)
    {
        var res = Application.Current.Resources;
        res["FlAccent"] = new SolidColorBrush(c);
        res["FlAccentSoft"] = new SolidColorBrush(Color.FromArgb(0x33, c.R, c.G, c.B));
    }

    private void SelectAccent(AccentOption o)
    {
        foreach (var a in Accents) a.IsSelected = ReferenceEquals(a, o);
        SelectAccentColor(o.Color);
    }

    // ----- перемикачі -----
    public bool NotifyTasks { get => _notifyTasks; set => SetField(ref _notifyTasks, value); }
    public bool NotifyBreaks { get => _notifyBreaks; set => SetField(ref _notifyBreaks, value); }
    public bool DailyReport { get => _dailyReport; set => SetField(ref _dailyReport, value); }
    public bool Sounds { get => _sounds; set => SetField(ref _sounds, value); }

    // ----- стандартні таймери -----
    public int WorkDefault { get => _workDefault; set { if (SetField(ref _workDefault, value)) OnPropertyChanged(nameof(WorkDefaultText)); } }
    public int BreakDefault { get => _breakDefault; set { if (SetField(ref _breakDefault, value)) OnPropertyChanged(nameof(BreakDefaultText)); } }
    public int SessionsDefault { get => _sessionsDefault; set { if (SetField(ref _sessionsDefault, value)) OnPropertyChanged(nameof(SessionsDefaultText)); } }
    public string WorkDefaultText => Uk.Duration(WorkDefault);
    public string BreakDefaultText => Uk.Duration(BreakDefault);
    public string SessionsDefaultText => $"{SessionsDefault} {Uk.Plural(SessionsDefault, "сесія", "сесії", "сесій")}";

    private static int Next(int[] steps, int current) => steps[(Array.IndexOf(steps, current) + 1) % steps.Length];

    public string SelectedLanguage
    {
        get => Localizer.Translate(Localizer.Language);
        set
        {
            var source = Localizer.SourceText(value ?? string.Empty);
            if (source is "Українська" or "English" && source != Localizer.Language)
                UiPreferences.Instance.SetLanguage(source); // зміна мови — глобальне налаштування застосунку
        }
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(Languages));
        OnPropertyChanged(nameof(WorkDefaultText));
        OnPropertyChanged(nameof(BreakDefaultText));
        OnPropertyChanged(nameof(SessionsDefaultText));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(EditError));
        foreach (var accent in Accents)
            accent.RefreshLanguage();
    }

    public string VersionText => Localizer.Translate("Версія v0.0.1");

    // ----- модалки -----
    public bool IsEditOpen { get => _isEditOpen; set => SetField(ref _isEditOpen, value); }
    public bool IsDeleteOpen { get => _isDeleteOpen; set => SetField(ref _isDeleteOpen, value); }
    public string EditName { get => _editName; set { if (SetField(ref _editName, value)) OnPropertyChanged(nameof(CanSaveProfile)); } }
    public string EditEmail { get => _editEmail; set { if (SetField(ref _editEmail, value)) OnPropertyChanged(nameof(CanSaveProfile)); } }
    public string EditError
    {
        get => Localizer.Translate(_editError);
        private set => SetField(ref _editError, value);
    }
    public bool CanSaveProfile => !_isSavingProfile && EditName.Trim().Length > 0 && EditEmail.Contains('@');

    public ICommand SelectAccentCommand { get; }
    public ICommand OpenEditCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand CloseModalsCommand { get; }
    public ICommand OpenDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CycleWorkCommand { get; }
    public ICommand CycleBreakCommand { get; }
    public ICommand CycleSessionsCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenSupportCommand { get; }

    private void Export()
    {
        var path = _filePicker.PickSaveFile("focuslab-settings.json", "JSON|*.json");
        if (string.IsNullOrEmpty(path)) return;

        var data = new
        {
            UserName,
            Email,
            Language = SelectedLanguage,
            DarkTheme = IsDarkTheme,
            NotifyTasks,
            NotifyBreaks,
            DailyReport,
            Sounds,
            WorkDefault,
            BreakDefault,
            SessionsDefault
        };

        File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }
}
