namespace FocusLab.Application.Focus;

public enum TimerPhase { Focus, ShortBreak, LongBreak }

/// <summary>Idle = фаза обрана, але ще не запущена.</summary>
public enum TimerStatus { Idle, Running, Paused }

/// <summary>Налаштування таймера (панель «Параметри» на екрані «Таймер»).</summary>
public sealed record FocusSettings
{
    public int FocusMinutes { get; init; } = 25;
    public int ShortBreakMinutes { get; init; } = 5;
    public int LongBreakMinutes { get; init; } = 15;
    public int SessionsBeforeLongBreak { get; init; } = 4;

    /// <summary>Перемикач «Автозапуск наступної сесії».</summary>
    public bool AutoStartNext { get; init; } = true;

    /// <summary>
    /// Записувати достроково зупинену сесію (IsCompleted = false) із фактичною тривалістю.
    /// Рішення з розділу «Питання, які ще потрібно вирішити» — узгодити з командою.
    /// </summary>
    public bool SaveInterruptedSessions { get; init; } = true;
}

public sealed record IntervalCompletedEventArgs(TimerPhase Finished, TimerPhase Next, int? TaskId);

public sealed record TimerErrorEventArgs(string Operation, Exception Exception);

/// <summary>Сесія, яку не вдалося записати в БД (SC-07, A2) — лежить у пам'яті до повторної спроби.</summary>
public sealed record PendingSession(
    int TaskId,
    DateTime StartUtc,
    int EstimatedMinutes,
    int DurationMinutes,
    bool IsCompleted);
