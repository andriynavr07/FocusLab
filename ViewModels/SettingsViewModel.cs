using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Net.Mail;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Frontend.Services;
using Microsoft.Win32;

namespace Frontend.ViewModels;

public class AccentOption : BoardBase
{
    private bool _isSelected;

    public AccentOption(string name, string hex)
    {
        Name = name;
        Color = (Color)ColorConverter.ConvertFromString(hex);
        Brush = new SolidColorBrush(Color);
    }

    public string Name { get; }
    public Color Color { get; }
    public SolidColorBrush Brush { get; }
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
}

public class SettingsViewModel : PageViewModelBase
{
    private static readonly int[] WorkSteps = { 15, 25, 45, 50 };
    private static readonly int[] BreakSteps = { 3, 5, 10, 15 };
    private static readonly int[] SessionSteps = { 2, 3, 4, 6 };

    private string _userName = "Олег Коваленко";
    private string _email = "oleh@focuslab.app";
    private string _editName = "", _editEmail = "";
    private bool _isEditOpen, _isDeleteOpen;
    private bool _notifyTasks = true, _notifyBreaks = true, _dailyReport, _sounds = true;
    private int _workDefault = 25, _breakDefault = 5, _sessionsDefault = 4;
    private string _language = "Українська";
    private readonly TimerViewModel _timer;

    public SettingsViewModel(TimerViewModel timer)
    {
        _timer = timer;
        Accents = new ObservableCollection<AccentOption>
        {
            new("Синій", "#3B6EF5") { IsSelected = true },
            new("Фіолетовий", "#A855F7"),
            new("Зелений", "#34B27B"),
            new("Помаранчевий", "#F0A63A"),
            new("Червоний", "#E5534B")
        };

        SelectAccentCommand = new RelayCommand(p => { if (p is AccentOption o) SelectAccent(o); });
        OpenEditCommand = new RelayCommand(_ => { EditName = UserName; EditEmail = Email; IsEditOpen = true; });
        SaveProfileCommand = new RelayCommand(_ =>
        {
            if (!CanSaveProfile) return;
            UserName = EditName.Trim();
            Email = EditEmail.Trim();
            IsEditOpen = false;
        });
        CloseModalsCommand = new RelayCommand(_ => { IsEditOpen = false; IsDeleteOpen = false; });
        OpenDeleteCommand = new RelayCommand(_ => IsDeleteOpen = true);
        ConfirmDeleteCommand = new RelayCommand(_ =>
        {
            IsDeleteOpen = false;
            MessageBox.Show(
                "Видалення акаунта недоступне: цей застосунок зараз використовує локальний демонстраційний профіль.",
                "Видалення акаунта",
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
                Process.Start(new ProcessStartInfo("https://focuslab.app/support") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Не вдалося відкрити підтримку", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<AccentOption> Accents { get; }
    public string[] Languages { get; } = { "Українська", "English" };

    public string UserName
    {
        get => _userName;
        set { if (SetField(ref _userName, value)) OnPropertyChanged(nameof(Initials)); }
    }

    public string Email { get => _email; set => SetField(ref _email, value); }

    public string Initials
    {
        get
        {
            var parts = UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
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
    public string WorkDefaultText => $"{WorkDefault} хв";
    public string BreakDefaultText => $"{BreakDefault} хв";
    public string SessionsDefaultText => $"{SessionsDefault} {Uk.Plural(SessionsDefault, "сесія", "сесії", "сесій")}";

    private static int Next(int[] steps, int current) => steps[(Array.IndexOf(steps, current) + 1) % steps.Length];

    public string SelectedLanguage { get => _language; set { if (value != null) SetField(ref _language, value); } }
    public string VersionText => "Версія v0.0.1";

    // ----- модалки -----
    public bool IsEditOpen { get => _isEditOpen; set => SetField(ref _isEditOpen, value); }
    public bool IsDeleteOpen { get => _isDeleteOpen; set => SetField(ref _isDeleteOpen, value); }
    public string EditName
    {
        get => _editName;
        set
        {
            if (!SetField(ref _editName, value)) return;
            OnPropertyChanged(nameof(CanSaveProfile));
            OnPropertyChanged(nameof(IsEditNameInvalid));
        }
    }
    public string EditEmail
    {
        get => _editEmail;
        set
        {
            if (!SetField(ref _editEmail, value)) return;
            OnPropertyChanged(nameof(CanSaveProfile));
            OnPropertyChanged(nameof(IsEditEmailInvalid));
        }
    }
    public bool IsEditNameInvalid => string.IsNullOrWhiteSpace(EditName);
    public bool IsEditEmailInvalid => !IsValidEmail(EditEmail);
    public bool CanSaveProfile => !IsEditNameInvalid && !IsEditEmailInvalid;

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
        var dlg = new SaveFileDialog { FileName = "focuslab-settings.json", Filter = "JSON|*.json" };
        if (dlg.ShowDialog() != true) return;

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

        File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email.Trim(), out var address)
        && string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
}