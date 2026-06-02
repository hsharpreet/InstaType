using InstaType.Services;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>OverlayWindow</c>.
/// Drives the three overlay states: Listening (waveform), Transcribing (spinner), Done (checkmark).
/// Exposes waveform amplitude data for the live visualiser.
/// Handles Escape-to-cancel and the optional 2-second injection preview (opt-in).
/// </summary>
public sealed class OverlayViewModel : ViewModelBase
{
    private readonly IHotkeyService _hotkeyService;

    private bool _isListening;
    private string _statusText = "Idle";
    private OverlayState _state = OverlayState.Listening;
    private float _waveformAmplitude;
    private string _previewText = string.Empty;
    private bool _showPreview;

    public OverlayViewModel(IHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;
        _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
    }

    /// <summary>True while the microphone is active and waveform should animate.</summary>
    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (SetProperty(ref _isListening, value))
                StatusText = value ? "Listening…" : "Idle";
        }
    }

    /// <summary>Human-readable status label shown below the waveform bars.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Current display state of the overlay.</summary>
    public OverlayState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    /// <summary>Normalised audio amplitude (0.0–1.0) for the waveform visualiser.</summary>
    public float WaveformAmplitude
    {
        get => _waveformAmplitude;
        set => SetProperty(ref _waveformAmplitude, value);
    }

    /// <summary>Transcribed text shown in the preview toast (opt-in, off by default).</summary>
    public string PreviewText
    {
        get => _previewText;
        set => SetProperty(ref _previewText, value);
    }

    /// <summary>Whether the injection preview toast is currently visible.</summary>
    public bool ShowPreview
    {
        get => _showPreview;
        set => SetProperty(ref _showPreview, value);
    }

    /// <summary>Stops listening — called by the mute button.</summary>
    public void ToggleMute()
    {
        if (IsListening) IsListening = false;
    }

    private void OnHotkeyTriggered(object? sender, EventArgs e)
        => IsListening = !IsListening;
}

/// <summary>Display states of the overlay window.</summary>
public enum OverlayState
{
    /// <summary>Microphone is active; waveform is animating.</summary>
    Listening,
    /// <summary>Audio captured; Whisper is processing.</summary>
    Transcribing,
    /// <summary>Text injected successfully; overlay will auto-dismiss.</summary>
    Done,
    /// <summary>User pressed Escape; no text was injected.</summary>
    Cancelled
}
