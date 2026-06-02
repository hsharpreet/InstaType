using InstaType.Models;
using InstaType.Services;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>SettingsWindow</c>.
/// Exposes all <see cref="AppSettings"/> properties as bindable fields.
/// Persists via <see cref="ISettingsService.SaveAsync"/> on Apply.
/// Tier-gates UI elements via <see cref="ISubscriptionService.HasAccess"/>.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private string _whisperModel = "ggml-tiny.bin";
    private string _transcriptionLanguage = "en";
    private int _silenceTimeout = 3;
    private bool _showInjectionPreview;
    private bool _launchAtStartup;
    private string? _uiLocale;
    private string? _themeOverride;

    public string WhisperModel          { get => _whisperModel;          set => SetProperty(ref _whisperModel, value); }
    public string TranscriptionLanguage { get => _transcriptionLanguage; set => SetProperty(ref _transcriptionLanguage, value); }
    public int    SilenceTimeout        { get => _silenceTimeout;        set => SetProperty(ref _silenceTimeout, value); }
    public bool   ShowInjectionPreview  { get => _showInjectionPreview;  set => SetProperty(ref _showInjectionPreview, value); }
    public bool   LaunchAtStartup       { get => _launchAtStartup;       set => SetProperty(ref _launchAtStartup, value); }
    public string? UiLocale             { get => _uiLocale;              set => SetProperty(ref _uiLocale, value); }
    public string? ThemeOverride        { get => _themeOverride;         set => SetProperty(ref _themeOverride, value); }

    // TODO (F-07): Inject ISettingsService, ISubscriptionService. Load from Current on init.
    // ApplyCommand saves and triggers hotkey/model reload as needed.
}
