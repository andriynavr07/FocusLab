using System.ComponentModel;

namespace Frontend.Services;

/// <summary>
/// Переклад текстів інтерфейсу та поточна мова. ViewModel-і отримують його через конструктор,
/// тож тести на тексти не залежать від спільного стану <see cref="UiPreferences.Instance"/>.
/// Зміна мови сповіщається через <see cref="INotifyPropertyChanged"/> (властивість <see cref="Language"/>).
/// </summary>
public interface ILocalizer : INotifyPropertyChanged
{
    /// <summary>«Українська» або «English».</summary>
    string Language { get; }

    bool IsEnglish { get; }

    /// <summary>Переклад українського тексту на поточну мову (без перекладу — повертає текст як є).</summary>
    string Translate(string text);

    /// <summary>Зворотний пошук: український оригінал перекладеного тексту.</summary>
    string SourceText(string text);
}

/// <summary>
/// Локалізатор із фіксованою мовою, без глобального стану: для тестів і місць,
/// де мова відома наперед. Переклади ті самі, що в <see cref="UiPreferences"/>.
/// </summary>
public sealed class FixedLocalizer : ILocalizer
{
    public const string Ukrainian = "Українська";
    public const string English = "English";

    public FixedLocalizer(string language)
    {
        if (language is not (Ukrainian or English))
            throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported UI language.");
        Language = language;
    }

    public string Language { get; }
    public bool IsEnglish => Language == English;

    public string Translate(string text) => UiPreferences.TranslateTo(Language, text);
    public string SourceText(string text) => UiPreferences.SourceTextOf(text);

    // Мова не змінюється, тож сповіщати немає про що.
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
}
