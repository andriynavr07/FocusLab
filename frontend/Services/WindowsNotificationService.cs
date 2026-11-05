using FocusLab.Application.Abstractions;
using FocusLab.Application.Focus;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Frontend.Services;

/// <summary>
/// Нативні Windows toast-сповіщення про завершення інтервалу (US 3.2).
/// Звук у toast вимкнено (silent) — його програє ISoundPlayer, щоб звук не дублювався
/// і лунав навіть тоді, коли Windows ховає сповіщення.
/// </summary>
public sealed class WindowsNotificationService : INotificationService
{
    private readonly ILogger<WindowsNotificationService> _logger;

    public WindowsNotificationService(ILogger<WindowsNotificationService> logger) => _logger = logger;

    public void NotifyIntervalCompleted(TimerPhase finished, TimerPhase next)
    {
        // TODO: винести рядки в .resx разом з рештою локалізації.
        var (title, body) = (finished, next) switch
        {
            (TimerPhase.Focus, TimerPhase.LongBreak) => ("Фокус-сесію завершено", "Час для довгої перерви. Ви це заслужили!"),
            (TimerPhase.Focus, _) => ("Фокус-сесію завершено", "Час для короткої перерви."),
            _ => ("Перерва закінчилась", "Повертаємось до роботи.")
        };

        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default"), silent: true)
                .Show();
        }
        catch (Exception ex)
        {
            // Збій сповіщення не повинен ламати таймер.
            _logger.LogWarning(ex, "Не вдалося показати Windows-сповіщення");
        }
    }
}
