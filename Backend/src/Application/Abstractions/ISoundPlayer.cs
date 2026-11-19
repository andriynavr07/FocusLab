namespace FocusLab.Application.Abstractions;

/// <summary>
/// Звук завершення інтервалу. Окремо від toast, бо Windows може приховати сповіщення
/// (Focus Assist / «Не турбувати»), а звук за US 3.2 має лунати завжди.
/// </summary>
public interface ISoundPlayer
{
    void PlayIntervalCompleted();
}
