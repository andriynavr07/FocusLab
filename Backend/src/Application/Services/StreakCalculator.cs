namespace FocusLab.Application.Services;

/// <summary>
/// Серійний період — кількість календарних днів поспіль із активністю
/// (завершена фокус-сесія або виконана задача).
/// Якщо сьогодні активності ще немає, серія не обривається до кінця дня:
/// рахуємо дні поспіль, що закінчуються вчора.
/// </summary>
public static class StreakCalculator
{
    public static int Calculate(IEnumerable<DateTime> activeDays, DateTime today)
    {
        var days = new HashSet<DateTime>(activeDays.Select(d => d.Date));
        var cursor = today.Date;

        if (!days.Contains(cursor))
            cursor = cursor.AddDays(-1);

        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
