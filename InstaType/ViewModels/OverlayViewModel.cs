using InstaType.Models;
using InstaType.Services;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>OverlayWindow</c>.
/// Drives the three overlay states: Idle → Listening (waveform) → Transcribing → Done.
/// Wires HotkeyService → AudioCaptureService → TranscriptionService → TextInjectionService → HistoryService.
/// All heavy work runs on background tasks; UI-bound properties marshalled to Dispatcher.
/// </summary>
public sealed class OverlayViewModel : ViewModelBase
{
    private readonly IHotkeyService         _hotkey;
    private readonly IAudioCaptureService   _audio;
    private readonly ITranscriptionService  _transcription;
    private readonly ITextInjectionService  _injection;
    private readonly IHistoryService        _history;
    private readonly ISettingsService       _settings;

    private bool   _isListening;
    private string _statusText = "Idle";

    private double _bar1Scale = 0.15, _bar2Scale = 0.15, _bar3Scale = 0.15,
                   _bar4Scale = 0.15, _bar5Scale = 0.15;

    private OverlayState _state = OverlayState.Listening;
    private string _previewText = string.Empty;
    private bool   _showPreview;

    // HWND of the window that was focused when the hotkey fired
    private nint _targetHwnd;

    public OverlayViewModel(
        IHotkeyService        hotkeyService,
        IAudioCaptureService  audioCaptureService,
        ITranscriptionService transcriptionService,
        ITextInjectionService textInjectionService,
        IHistoryService       historyService,
        ISettingsService      settingsService)
    {
        _hotkey        = hotkeyService;
        _audio         = audioCaptureService;
        _transcription = transcriptionService;
        _injection     = textInjectionService;
        _history       = historyService;
        _settings      = settingsService;

        _hotkey.HotkeyTriggered += OnHotkeyTriggered;
        _audio.WaveformSample   += OnWaveformSample;
        _audio.AudioCaptured    += OnAudioCaptured;
    }

    // ── Bindable properties ──────────────────────────────────────────────────

    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (SetProperty(ref _isListening, value))
                StatusText = value ? "Listening…" : "Idle";
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public double Bar1Scale { get => _bar1Scale; private set => SetProperty(ref _bar1Scale, value); }
    public double Bar2Scale { get => _bar2Scale; private set => SetProperty(ref _bar2Scale, value); }
    public double Bar3Scale { get => _bar3Scale; private set => SetProperty(ref _bar3Scale, value); }
    public double Bar4Scale { get => _bar4Scale; private set => SetProperty(ref _bar4Scale, value); }
    public double Bar5Scale { get => _bar5Scale; private set => SetProperty(ref _bar5Scale, value); }

    public OverlayState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public string PreviewText
    {
        get => _previewText;
        set => SetProperty(ref _previewText, value);
    }

    public bool ShowPreview
    {
        get => _showPreview;
        set => SetProperty(ref _showPreview, value);
    }

    // ── Public methods ───────────────────────────────────────────────────────

    public void ToggleMute()
    {
        if (IsListening)
            _ = StopListeningAsync();
    }

    public List<MicrophoneDevice> GetAvailableMicrophones()
        => _audio.GetAvailableMicrophones();

    public void SelectMicrophone(int deviceId) => _audio.SetDevice(deviceId);

    /// <summary>Saves the drag-adjusted overlay position to settings.</summary>
    public void SavePosition(double left, double top)
    {
        _settings.Current.OverlayLeft = left;
        _settings.Current.OverlayTop  = top;
        _ = _settings.SaveAsync();
    }


    // ── Event handlers ───────────────────────────────────────────────────────

    private void OnHotkeyTriggered(object? sender, EventArgs e)
    {
        if (!IsListening)
            _ = StartListeningAsync();
        else
            _ = StopListeningAsync();
    }

    private void OnWaveformSample(object? sender, float[] amplitudes)
    {
        if (amplitudes.Length < 5) return;

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            Bar1Scale = 0.15 + amplitudes[0] * 0.85;
            Bar2Scale = 0.15 + amplitudes[1] * 0.85;
            Bar3Scale = 0.15 + amplitudes[2] * 0.85;
            Bar4Scale = 0.15 + amplitudes[3] * 0.85;
            Bar5Scale = 0.15 + amplitudes[4] * 0.85;
        });
    }

    private async void OnAudioCaptured(object? sender, float[] audioData)
    {
        if (audioData.Length == 0) return;

        System.Windows.Application.Current?.Dispatcher.Invoke(
            () => StatusText = "Transcribing…");

        try
        {
            string text = await _transcription.TranscribeAsync(
                audioData, _settings.Current.TranscriptionLanguage);

            if (!string.IsNullOrWhiteSpace(text))
            {
                await _injection.InjectTextAsync(_targetHwnd, text);

                if (_settings.Current.SaveHistory)
                {
                    await _history.AddAsync(new TranscriptionEntry
                    {
                        RawText              = text,
                        InjectedText         = text,
                        RecordedAt           = DateTimeOffset.UtcNow,
                        ModelName            = _transcription.LoadedModelName ?? "unknown",
                        Locale               = _settings.Current.TranscriptionLanguage,
                        AudioDurationSeconds = audioData.Length / 16000.0,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Pipeline] {ex.Message}");
        }
        finally
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                ResetBars();
                IsListening = false;
            });
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task StartListeningAsync()
    {
        if (!_transcription.IsModelLoaded)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(
                () => StatusText = "Model loading, please wait…");
            return;
        }

        _targetHwnd = _injection.CaptureTargetWindow();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => IsListening = true);

        try
        {
            await _audio.StartAsync(_settings.Current.SilenceTimeoutSeconds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] Start failed: {ex.Message}");
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsListening = false;
                StatusText  = "Mic error — check settings";
            });
        }
    }

    private async Task StopListeningAsync()
    {
        await _audio.StopAsync(); // fires AudioCaptured, which resets state
    }

    private void ResetBars()
    {
        Bar1Scale = Bar2Scale = Bar3Scale = Bar4Scale = Bar5Scale = 0.15;
    }
}

/// <summary>Display states of the overlay window.</summary>
public enum OverlayState
{
    Listening,
    Transcribing,
    Done,
    Cancelled
}
