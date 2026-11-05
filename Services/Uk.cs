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

    public static string Short(DateTime date) => $"{date.Day} {Month(date)}";

    public static string Month(DateTime date) => MonthNames[date.Month - 1];
}
