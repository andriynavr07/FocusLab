using FocusLab.Application.Focus;

namespace FocusLab.Application.Abstractions;

/// <summary>Налаштування таймера (settings.json). Значення читаються на початку кожної фази.</summary>
public interface IFocusSettingsProvider
{
    FocusSettings Current { get; }

    /// <summary>Нормалізує, застосовує в пам'яті та записує у файл. Може кинути IOException.</summary>
    void Save(FocusSettings settings);
}
