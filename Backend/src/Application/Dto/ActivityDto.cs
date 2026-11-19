namespace FocusLab.Application.Dto;

/// <summary>Фокус-сесія для відображення в розкладі дня.</summary>
public sealed record FocusSessionDto(
    int TaskId,
    string TaskTitle,
    DateTime StartUtc,
    int DurationMinutes,
    bool IsCompleted);

/// <summary>Зведення активності: серійний період, фокус і виконані задачі за сьогодні.</summary>
public sealed record ActivitySummary(
    int StreakDays,
    int FocusMinutesToday,
    int TasksCompletedToday,
    IReadOnlyList<FocusSessionDto> SessionsToday);
