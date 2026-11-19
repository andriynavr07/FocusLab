using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Frontend.Services;

public sealed class UiPreferences : INotifyPropertyChanged
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly IReadOnlyDictionary<string, string> EnglishTexts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["СЬОГОДНІ"] = "TODAY",
            ["ЗАВДАНЬ ЦЬОГО МІСЯЦЯ"] = "TASKS THIS MONTH",
            ["ВИКОНАНО"] = "COMPLETED",
            ["Пн"] = "Mon", ["Вт"] = "Tue", ["Ср"] = "Wed", ["Чт"] = "Thu",
            ["Пт"] = "Fri", ["Сб"] = "Sat", ["Нд"] = "Sun",
            ["СПРАВИ НА ДЕНЬ"] = "TASKS FOR THE DAY",
            ["Немає завдань на цей день"] = "No tasks for this day",
            ["Порада від FocusLab"] = "A tip from FocusLab",
            ["КАЛЕНДАР"] = "CALENDAR",
            ["ДОШКА ЗАВДАНЬ"] = "TASK BOARD",
            ["Канбан завдань"] = "Task board",
            ["+ Додати завдання"] = "+ Add task",
            ["Додати завдання"] = "Add task",
            ["Знайти завдання"] = "Search tasks",
            ["Немає нових сповіщень"] = "No new notifications",
            ["Скинути фільтри"] = "Reset filters",
            ["Скинути"] = "Reset",
            ["Зупинити"] = "Stop",
            ["Статус"] = "Status",
            ["СТАТУС"] = "STATUS",
            ["ЧАС"] = "TIME",
            ["СФОКУСУВАННЯ"] = "FOCUS",
            ["РОЗКЛАД НА ДЕНЬ"] = "TODAY'S SCHEDULE",
            ["На сьогодні немає запланованих завдань і фокус-сесій"] = "No tasks or focus sessions are scheduled for today",
            ["Деталі завдання"] = "Task details",
            ["Видалити"] = "Delete",
            ["Видалити завдання"] = "Delete task",
            ["Редагувати"] = "Edit",
            ["Перемкнути виконання"] = "Toggle completion",
            ["Фокус 25 хв"] = "Focus for 25 min",
            ["Проєкт: {0}"] = "Project: {0}",
            ["Пріоритет: {0}"] = "Priority: {0}",
            ["Термін: {0}"] = "Due: {0}",
            ["Оцінка часу: {0}"] = "Time estimate: {0}",
            ["Ресурси: {0}"] = "Resources: {0}",
            ["{0} завдань"] = "{0} tasks",
            ["Теги:"] = "Tags:",
            ["Налаштування"] = "Settings",
            ["ТАЙМЕР"] = "TIMER",
            ["ЗАВДАННЯ"] = "TASKS",
            ["Фокус-режим"] = "Focus mode",
            ["Почати"] = "Start",
            ["ПАУЗА"] = "PAUSE",
            ["ПОЧАТИ"] = "START",
            ["Пропустити"] = "Skip",
            ["Щоденна ціль фокусу"] = "Daily focus goal",
            ["ХРОНОЛОГІЯ СЬОГОДНІШНІХ СЕСІЙ"] = "TODAY'S SESSION HISTORY",
            ["НАЛАШТУВАННЯ ТАЙМЕРА"] = "TIMER SETTINGS",
            ["Параметри"] = "Options",
            ["Тривалість роботи"] = "Work duration",
            ["Коротка перерва"] = "Short break",
            ["Довга перерва"] = "Long break",
            ["Автозапуск наступної сесії"] = "Automatically start next session",
            ["Зберігати перервану сесію"] = "Save interrupted sessions",
            ["Діє для дострокового скидання таймера."] = "Applies when the timer is stopped early.",
            ["Вийти з фокус-режиму"] = "Exit focus mode",
            ["Звуки природи"] = "Nature sounds",
            ["Білий шум"] = "White noise",
            ["Тиша"] = "Silence",
            ["НАЛАШТУВАННЯ"] = "SETTINGS",
            ["Зовнішній вигляд"] = "Appearance",
            ["Тема інтерфейсу"] = "Interface theme",
            ["Темна"] = "Dark",
            ["Світла"] = "Light",
            ["Акцентний колір"] = "Accent color",
            ["Сповіщення"] = "Notifications",
            ["Сповіщення про завдання"] = "Task notifications",
            ["Нагадування про перерву"] = "Break reminders",
            ["Щоденний звіт"] = "Daily report",
            ["Звуки сповіщень"] = "Notification sounds",
            ["Стандартні таймери"] = "Default timers",
            ["Робота за замовчуванням"] = "Default work duration",
            ["Перерва за замовчуванням"] = "Default break duration",
            ["Кількість сесій"] = "Number of sessions",
            ["Система та приватність"] = "System and privacy",
            ["Мова додатку"] = "App language",
            ["Українська"] = "Ukrainian",
            ["English"] = "English",
            ["Керування даними"] = "Data management",
            ["Експортувати дані"] = "Export data",
            ["Небезпечна зона"] = "Danger zone",
            ["Видалити акаунт"] = "Delete account",
            ["Служба підтримки"] = "Support",
            ["Редагувати профіль"] = "Edit profile",
            ["Ім’я"] = "Name",
            ["Ваше ім’я"] = "Your name",
            ["Скасувати"] = "Cancel",
            ["Зберегти"] = "Save",
            ["Видалити акаунт?"] = "Delete account?",
            ["Так, видалити"] = "Yes, delete",
            ["Головна"] = "Home",
            ["Завдання"] = "Tasks",
            ["Календар"] = "Calendar",
            ["Таймер"] = "Timer",
            ["Вийти з акаунта"] = "Sign out",
            ["ВІТАЄМО!"] = "WELCOME!",
            ["ФОКУСУВАННЯ"] = "FOCUS",
            ["Сплануй свій день"] = "Plan your day",
            ["Працюй, розвивайся, контролюй, але не забувай про відпочинок"] = "Work, grow, stay in control, and remember to rest",
            ["© 2026 FocusLab. Фокусуйся, одна сесія за раз."] = "© 2026 FocusLab. Focus one session at a time.",
            ["авторизуйтесь для фокусування на роботі"] = "Sign in to focus on your work",
            ["заповніть форму для початку фокусування на роботі"] = "Complete the form to start focusing on your work",
            ["Ел. пошта"] = "Email",
            ["Пароль"] = "Password",
            ["Введіть пароль"] = "Enter password",
            ["Запамʼятати мене"] = "Remember me",
            ["Забули пароль?"] = "Forgot password?",
            ["Увійти"] = "Sign in",
            ["Ще не зареєстровані?"] = "Don't have an account yet?",
            ["Створити акаунт"] = "Create account",
            ["СТВОРИТИ АКАУНТ"] = "CREATE ACCOUNT",
            ["Імʼя"] = "Name",
            ["Ваше імʼя"] = "Your name",
            ["Підтвердження пароля"] = "Confirm password",
            ["Повторіть пароль"] = "Repeat password",
            ["Зареєструватися"] = "Register",
            ["Вже є акаунт?"] = "Already have an account?",
            ["Світла тема"] = "Light theme",
            ["НАЗВА ЗАВДАННЯ"] = "TASK TITLE",
            ["ПРОЄКТ"] = "PROJECT",
            ["ПРІОРИТЕТ"] = "PRIORITY",
            ["ТЕРМІН"] = "DUE DATE",
            ["Сортувати за:"] = "Sort by:",
            ["Проєкти"] = "Projects",
            ["Теги"] = "Tags",
            ["Нічого не знайдено"] = "Nothing found",
            ["Назва * (1–255)"] = "Title * (1–255)",
            ["Введіть назву завдання..."] = "Enter a task title...",
            ["Опис"] = "Description",
            ["Додайте опис (необов'язково)..."] = "Add a description (optional)...",
            ["Проєкт"] = "Project",
            ["Чек-лист"] = "Checklist",
            ["Нова підзадача"] = "New subtask",
            ["Додати"] = "Add",
            ["Пріоритет"] = "Priority",
            ["Дата виконання"] = "Due date",
            ["Оцінка часу"] = "Time estimate",
            ["Новий проєкт"] = "New project",
            ["Назва"] = "Name",
            ["Колір"] = "Color",
            ["Закрити"] = "Close",
            ["Новий тег"] = "New tag",
            ["Без проєкту"] = "No project",
            ["Нове завдання"] = "New task",
            ["Редагувати завдання"] = "Edit task",
            ["Зберегти зміни"] = "Save changes",
            ["Створити завдання"] = "Create task",
            ["Усі статуси"] = "All statuses",
            ["Усі проєкти"] = "All projects",
            ["Усі пріоритети"] = "All priorities",
            ["Критичний"] = "Critical",
            ["Високий"] = "High",
            ["Середній"] = "Medium",
            ["Низький"] = "Low",
            ["Виконано"] = "Completed",
            ["Виконується"] = "In progress",
            ["На перевірці"] = "In review",
            ["Відкладено"] = "Deferred",
            ["Дедлайном"] = "Due date",
            ["Пріоритетом"] = "Priority",
            ["Датою створення"] = "Creation date",
            ["Назвою"] = "Name",
            ["КРИТИЧНИЙ"] = "CRITICAL",
            ["ВИСОКИЙ"] = "HIGH",
            ["СЕРЕДНІЙ"] = "MEDIUM",
            ["НИЗЬКИЙ"] = "LOW",
            ["До виконання"] = "To do",
            ["У процесі"] = "In progress",
            ["Відкладені"] = "Deferred",
            ["Немає оцінки часу"] = "No time estimate",
            ["Фокус-сесія"] = "Focus session",
            ["фокусу (перервано)"] = "focus (interrupted)",
            ["фокусу"] = "focus",
            ["ШВИДКІ НАЛАШТУВАННЯ"] = "QUICK SETTINGS",
            ["ФОНОВИЙ ШУМ"] = "BACKGROUND SOUND",
            ["Усі ваші завдання, сесії та налаштування буде видалено без можливості відновлення."] = "All your tasks, sessions, and settings will be permanently deleted.",
            ["Опис не додано"] = "No description added",
            ["Завдання ще не збережено в базі даних. Спробуйте за мить."] = "The task has not been saved to the database yet. Try again in a moment.",
            ["завдання"] = "task",
            ["завдань"] = "tasks",
            ["день"] = "day",
            ["днів"] = "days",
            ["година"] = "hour",
            ["годин"] = "hours",
            ["сесій"] = "sessions",
            ["СЕСІЯ"] = "SESSION",
            ["КОРОТКА ПЕРЕРВА"] = "SHORT BREAK",
            ["ДОВГА ПЕРЕРВА"] = "LONG BREAK",
            ["Задача ще зберігається в базі даних. Спробуйте за мить."] = "The task is still being saved. Try again in a moment.",
            ["Таймер уже працює. Зупиніть його, щоб почати фокус на іншій задачі."] = "The timer is already running. Stop it to focus on another task.",
            ["Задача не вибрана"] = "No task selected",
            ["Без задачі"] = "No task",
            ["Рекомендовано"] = "Recommended",
            ["Довга сесія"] = "Long session",
            ["Швидка сесія"] = "Quick session",
            ["Сьогодні"] = "Today",
            ["Назва не може бути довшою за 255 символів."] = "The title cannot exceed 255 characters.",
            ["Введіть назву завдання."] = "Enter a task title.",
            ["Січень"] = "January", ["Лютий"] = "February", ["Березень"] = "March",
            ["Квітень"] = "April", ["Травень"] = "May", ["Червень"] = "June",
            ["Липень"] = "July", ["Серпень"] = "August", ["Вересень"] = "September",
            ["Жовтень"] = "October", ["Листопад"] = "November", ["Грудень"] = "December",
            ["Понеділок"] = "Monday", ["Вівторок"] = "Tuesday", ["Середа"] = "Wednesday",
            ["Четвер"] = "Thursday", ["П’ятниця"] = "Friday", ["Субота"] = "Saturday", ["Неділя"] = "Sunday",
            ["Вільний день. Використайте його для планування або відпочинку."] = "A free day. Use it to plan or rest.",
            ["Рекомендуємо зробити довгу перерву після другої сесії."] = "We recommend taking a long break after the second session.",
            ["Назва завдання не може бути довшою за 255 символів."] = "Task title cannot exceed 255 characters.",
            ["Синій"] = "Blue",
            ["Фіолетовий"] = "Purple",
            ["Зелений"] = "Green",
            ["Помаранчевий"] = "Orange",
            ["Червоний"] = "Red",
            ["Прострочено"] = "Overdue",
            ["Версія v0.0.1"] = "Version v0.0.1",
            ["Відновлення пароля недоступне для локального акаунта."] = "Password recovery is unavailable for a local account.",
            ["Відновлення пароля"] = "Password recovery",
            ["Невірна пошта або пароль."] = "Incorrect email or password.",
            ["Помилка входу"] = "Sign-in error",
            ["Паролі не збігаються."] = "Passwords do not match.",
            ["Не вдалося зареєструватися."] = "Registration failed.",
            ["Помилка реєстрації"] = "Registration error",
            ["Помилка зупинки таймера"] = "Timer stop error",
            ["Налаштування таймера не збережено у файл"] = "Timer settings could not be saved to a file",
            ["Не вдалося зберегти фокус-сесію в базу даних. Дані не втрачено в пам'яті — застосунок повторить спробу після наступної успішної операції."] = "The focus session could not be saved to the database. It is still in memory and the app will retry after the next successful operation.",
            ["Видалення акаунта недоступне: цей застосунок зараз використовує локальний демонстраційний профіль."] = "Account deletion is unavailable because this app currently uses a local demo profile.",
            ["Видалення акаунта"] = "Delete account",
            ["Не вдалося відкрити підтримку"] = "Could not open support",
            ["Помилка збереження"] = "Save error",
            ["Не вдалося зберегти профіль."] = "Could not save the profile.",
            ["Не вдалося запустити застосунок"] = "Could not start the app",
            ["Не вдалося прочитати акаунт"] = "Could not read the account",
            ["Не вдалося завантажити профіль"] = "Could not load the profile",
            ["Не вдалося завантажити фільтри"] = "Could not load filters",
            ["Не вдалося завантажити проєкти й теги"] = "Could not load projects and tags",
            ["Видалити завдання «{0}»?"] = "Delete task “{0}”?",
            ["Підтвердження видалення"] = "Confirm deletion",
            ["Видалити проєкт «{0}»? Завдання залишаться без проєкту."] = "Delete project “{0}”? Tasks will no longer be assigned to a project.",
            ["Видалити тег «{0}»?"] = "Delete tag “{0}”?",
            ["Фокус-сесія ще триває.\n\nТак — закрити й зберегти її як перервану.\nНі — закрити без збереження незавершеної сесії.\nСкасувати — продовжити роботу."] = "A focus session is still running.\n\nYes — close and save it as interrupted.\nNo — close without saving the unfinished session.\nCancel — continue working.",
            ["Попередню фокус-сесію не вдалося записати.\n\nТак — повторити збереження.\nНі — закрити без збереження цієї сесії.\nСкасувати — продовжити роботу."] = "The previous focus session could not be saved.\n\nYes — retry saving.\nNo — close without saving this session.\nCancel — continue working.",
            ["Не вдалося зберегти сесію. Вона залишиться в пам'яті, поки застосунок відкритий."] = "The session could not be saved. It will remain in memory while the app is open.",
            ["Не вдалося записати перервану сесію. Спробуйте закрити ще раз, щоб повторити збереження."] = "The interrupted session could not be saved. Try closing again to retry.",
            ["Не вдалося завершити фокус-сесію. Застосунок залишився відкритим."] = "The focus session could not be stopped. The app remains open."
        };
    private string? _filePath;
    private bool _isLightTheme;
    private string _language = "Українська";

    private UiPreferences() { }

    public static UiPreferences Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLightTheme
    {
        get => _isLightTheme;
        private set
        {
            if (_isLightTheme == value) return;
            _isLightTheme = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeButtonText));
        }
    }

    public string Language
    {
        get => _language;
        private set
        {
            if (_language == value) return;
            _language = value;
            var culture = value == "English"
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("uk-UA");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageButtonText));
        }
    }

    public string ThemeButtonText => IsLightTheme ? "Dark" : "Light";
    public string LanguageButtonText => Language == "Українська" ? "EN" : "UA";

    public string Translate(string text)
        => Language == "English" && EnglishTexts.TryGetValue(text, out var translation) ? translation : text;

    public string SourceText(string text)
        => EnglishTexts.FirstOrDefault(pair => pair.Value == text).Key ?? text;

    public void Initialize(string settingsDirectory)
    {
        _filePath = Path.Combine(settingsDirectory, "ui-preferences.json");
        if (!File.Exists(_filePath))
        {
            ThemeManager.Apply(IsLightTheme);
            return;
        }

        try
        {
            var stored = JsonSerializer.Deserialize<PreferenceData>(File.ReadAllText(_filePath));
            if (stored is not null)
            {
                IsLightTheme = stored.IsLightTheme;
                Language = stored.Language == "English" ? "English" : "Українська";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show($"Не вдалося прочитати налаштування вигляду:\n{ex.Message}",
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        ThemeManager.Apply(IsLightTheme);
    }

    public void ToggleTheme()
        => SetTheme(!IsLightTheme);

    public void SetTheme(bool useLightTheme)
    {
        IsLightTheme = useLightTheme;
        ThemeManager.Apply(IsLightTheme);
        Save();
    }

    public void ToggleLanguage()
        => SetLanguage(Language == "Українська" ? "English" : "Українська");

    public void SetLanguage(string language)
    {
        if (language is not ("Українська" or "English"))
            throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported UI language.");

        Language = language;
        Save();
    }

    private void Save()
    {
        if (_filePath is null) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(
                new PreferenceData(IsLightTheme, Language), JsonOptions));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Не вдалося зберегти налаштування вигляду:\n{ex.Message}",
                "FocusLab", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record PreferenceData(bool IsLightTheme, string Language);
}
