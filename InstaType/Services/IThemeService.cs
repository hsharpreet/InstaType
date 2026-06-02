namespace InstaType.Services;

/// <summary>
/// Detects the Windows system theme (dark/light) and applies it to WPF ResourceDictionaries.
/// Uses UISettings.ColorValuesChanged (WinRT) with a registry-poll fallback.
/// Theme changes are applied immediately without an app restart.
/// All XAML colors must use DynamicResource, never StaticResource.
/// </summary>
public interface IThemeService
{
    /// <summary>Whether the current effective theme is dark mode.</summary>
    bool IsDarkMode { get; }

    /// <summary>Raised on the UI thread whenever the effective theme changes.</summary>
    event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// Starts monitoring system theme changes.
    /// Applies the initial theme immediately.
    /// </summary>
    void Start();

    /// <summary>
    /// Forces a specific theme, overriding the system setting.
    /// Pass null to revert to following the system theme.
    /// </summary>
    void SetThemeOverride(bool? forceDark);
}
