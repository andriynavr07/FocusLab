using FocusLab.Application.Focus;

namespace FocusLab.Application.Abstractions;

/// <summary>Нативні Windows-сповіщення (toast). Реалізація — у проєкті frontend (потрібен Windows TFM).</summary>
public interface INotificationService
{
    void NotifyIntervalCompleted(TimerPhase finished, TimerPhase next);
}
