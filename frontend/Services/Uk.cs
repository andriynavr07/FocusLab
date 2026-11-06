namespace Frontend.Services;

public static class Uk
{
    private static readonly string[] MonthNames =
    [
        "січня", "лютого", "березня", "квітня", "травня", "червня",
        "липня", "серпня", "вересня", "жовтня", "листопада", "грудня"
    ];

    public static string Plural(int value, string one, string few, string many)
    {
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

    private static readonly string[] ShortMonthNames =
    [
        "СІЧ", "ЛЮТ", "БЕР", "КВІ", "ТРА", "ЧЕР",
        "ЛИП", "СЕР", "ВЕР", "ЖОВ", "ЛИС", "ГРУ"
    ];

    /// <summary>«22 ВЕР 2026» — скорочений місяць великими літерами.</summary>
    public static string ShortUpper(DateTime date, bool withYear = false)
        => withYear ? $"{date.Day:00} {ShortMonthNames[date.Month - 1]} {date.Year}" : $"{date.Day} {ShortMonthNames[date.Month - 1]}";

    /// <summary>Тривалість у хвилинах: «25 хв», «1 година», «1 год 25 хв».</summary>
    public static string Duration(int minutes)
    {
        if (minutes < 60) return $"{minutes} хв";
        var h = minutes / 60;
        var m = minutes % 60;
        return m == 0 ? $"{h} {Plural(h, "година", "години", "годин")}" : $"{h} год {m} хв";
    }

    public static string Short(DateTime date) => $"{date.Day} {Month(date)}";

    public static string Month(DateTime date) => MonthNames[date.Month - 1];
}
