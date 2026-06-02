using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Reads and writes <see cref="AppSettings"/>.
/// Settings are stored in %LOCALAPPDATA%\InstaType\settings.json locally,
/// and synced to Supabase on every save (Core+).
/// </summary>
public interface ISettingsService
{
    /// <summary>The currently active settings. Always non-null after <see cref="LoadAsync"/>.</summary>
    AppSettings Current { get; }

    /// <summary>Loads settings from local storage, applying defaults for missing values.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists current settings locally and queues a Supabase sync (Core+).</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets all settings to their defaults and saves.</summary>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>Raised whenever settings are saved.</summary>
    event EventHandler<AppSettings>? SettingsChanged;
}
