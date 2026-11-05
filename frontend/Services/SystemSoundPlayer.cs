using System.Media;
using FocusLab.Application.Abstractions;

namespace Frontend.Services;

/// <summary>Системний звук завершення інтервалу (без додаткових файлів — працює офлайн).</summary>
public sealed class SystemSoundPlayer : ISoundPlayer
{
    public void PlayIntervalCompleted() => SystemSounds.Exclamation.Play();
}
