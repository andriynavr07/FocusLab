using System.Globalization;

namespace Frontend.Services;

/// <summary>
/// Форматування чисел, дат і тривалостей з урахуванням мови інтерфейсу.
/// Не статичний: мову бере з переданого <see cref="ILocalizer"/>, а не з глобального стану.
/// </summary>
public sealed class Uk(ILocalizer localizer)
{
    private static readonly string[] MonthNames =
    [
        "січня", "лютого", "березня", "квітня", "травня", "червня",
        "липня", "серпня", "вересня", "жовтня", "листопада", "грудня"
    ];

    private static readonly string[] ShortMonthNames =
    [
        "СІЧ", "ЛЮТ", "БЕР", "КВІ", "ТРА", "ЧЕР",
        "ЛИП", "СЕР", "ВЕР", "ЖОВ", "ЛИС", "ГРУ"
    ];

    private readonly ILocalizer _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

    public string Plural(int value, string one, string few, string many)
    {
        if (_localizer.IsEnglish)
            return _localizer.Translate(value == 1 ? one : many);

        var lastTwoDigits = Math.Abs(value) % 100;
        if (lastTwoDigits is >= 11 and <= 14)
            return many;

        return (Math.Abs(value) % 10) switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many
        };
    }

    /// <summary>«22 ВЕР 2026» — скорочений місяць великими літерами.</summary>
    public string ShortUpper(DateTime date, bool withYear = false)
        => _localizer.IsEnglish
            ? date.ToString(withYear ? "MMM dd yyyy" : "MMM d", CultureInfo.GetCultureInfo("en-US")).ToUpperInvariant()
            : withYear ? $"{date.Day:00} {ShortMonthNames[date.Month - 1]} {date.Year}" : $"{date.Day} {ShortMonthNames[date.Month - 1]}";

    /// <summary>Тривалість у хвилинах: «25 хв», «1 година», «1 год 25 хв».</summary>
    public string Duration(int minutes)
    {
        if (_localizer.IsEnglish)
        {
            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            if (hours == 0) return $"{minutes} min";
            if (remainingMinutes == 0) return $"{hours} {(hours == 1 ? "hour" : "hours")}";
            return $"{hours} hr {remainingMinutes} min";
        }

        if (minutes < 60) return $"{minutes} хв";
        var h = minutes / 60;
        var m = minutes % 60;
        return m == 0 ? $"{h} {Plural(h, "година", "години", "годин")}" : $"{h} год {m} хв";
    }

    public string Short(DateTime date) => _localizer.IsEnglish
        ? date.ToString("MMM d", CultureInfo.GetCultureInfo("en-US"))
        : $"{date.Day} {Month(date)}";

    public string Month(DateTime date) => _localizer.IsEnglish
        ? date.ToString("MMMM", CultureInfo.GetCultureInfo("en-US"))
        : MonthNames[date.Month - 1];
}
