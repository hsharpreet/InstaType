using InstaType.Models;
using InstaType.Services;
using System.Threading.Channels;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>OverlayWindow</c>.
/// Drives the three overlay states: Idle → Listening (waveform) → Transcribing → Done.
/// Wires HotkeyService → AudioCaptureService → TranscriptionService → TextInjectionService → HistoryService.
/// All heavy work runs on background tasks; UI-bound properties marshalled to Dispatcher.
/// </summary>
public sealed class OverlayViewModel : ViewModelBase
{
    private readonly IHotkeyService           _hotkey;
    private readonly IAudioCaptureService     _audio;
    private readonly ITranscriptionService    _transcription;
    private readonly ITextInjectionService    _injection;
    private readonly IAiPostProcessingService _ai;
    private readonly IHistoryService          _history;
    private readonly ISettingsService         _settings;

    private bool   _isListening;
    private bool   _isMuted;
    private string _statusText = "Idle";

    private double _bar1Scale = 0.15, _bar2Scale = 0.15, _bar3Scale = 0.15,
                   _bar4Scale = 0.15, _bar5Scale = 0.15;

    private OverlayState _state = OverlayState.Listening;
    private string _previewText = string.Empty;
    private bool   _showPreview;

    // HWND of the window that was focused when the hotkey fired
    private nint _targetHwnd;

    // Streaming session tracking
    private int    _sessionChunkCount;
    private string _sessionInjectedText = string.Empty;
    private readonly HashSet<string> _injectedThisSession = new(StringComparer.Ordinal);

    // Channel-based chunk queue: one producer (OnAudioChunkReady), one consumer (ProcessChunkQueueAsync)
    private Channel<float[]>? _chunkChannel;
    private Task?              _consumerTask;

    public OverlayViewModel(
        IHotkeyService           hotkeyService,
        IAudioCaptureService     audioCaptureService,
        ITranscriptionService    transcriptionService,
        ITextInjectionService    textInjectionService,
        IAiPostProcessingService aiPostProcessingService,
        IHistoryService          historyService,
        ISettingsService         settingsService)
    {
        _hotkey        = hotkeyService;
        _audio         = audioCaptureService;
        _transcription = transcriptionService;
        _injection     = textInjectionService;
        _ai            = aiPostProcessingService;
        _history       = historyService;
        _settings      = settingsService;

        // BUG 3: defensive init — clear all session state in constructor
        _sessionChunkCount   = 0;
        _sessionInjectedText = string.Empty;
        _injectedThisSession.Clear();
        System.Diagnostics.Debug.WriteLine("[Init] OverlayViewModel — session state clean, no stale audio");

        _hotkey.HotkeyTriggered  += OnHotkeyTriggered;
        _audio.WaveformSample    += OnWaveformSample;
        _audio.AudioCaptured     += OnAudioCaptured;
        _audio.AudioChunkReady   += OnAudioChunkReady;
    }

    // ── Bindable properties ──────────────────────────────────────────────────

    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (SetProperty(ref _isListening, value))
                StatusText = value ? "Listening…" : (_isMuted ? "Muted" : "Idle");
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        private set
        {
            if (SetProperty(ref _isMuted, value))
                StatusText = value ? "Muted" : "Idle";
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
        IsMuted = !IsMuted;
        if (IsMuted && IsListening)
            _ = StopListeningAsync();
    }

    public List<MicrophoneDevice> GetAvailableMicrophones()
        => _audio.GetAvailableMicrophones();

    public void SelectMicrophone(int deviceId)
    {
        _audio.SetDevice(deviceId);
        StartMicMonitor(deviceId);
    }

    public void StartMicMonitor(int deviceId)
    {
        if (!_audio.IsRecording)
            _ = _audio.StartMonitorAsync(deviceId);
    }

    public void StopMicMonitor()
    {
        _ = _audio.StopMonitorAsync();
        System.Windows.Application.Current?.Dispatcher.Invoke(ResetBars);
    }

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
        if (IsMuted)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                () => StatusText = "Muted — click 🎤 to unmute");
            return;
        }
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

    // BUG 4: chunk ready is now just an enqueue — no async processing here
    private void OnAudioChunkReady(object? sender, float[] chunkData)
    {
        if (!IsListening || chunkData.Length == 0) return;

        bool written = _chunkChannel?.Writer.TryWrite(chunkData) ?? false;
        int  depth   = _chunkChannel?.Reader.Count ?? 0;
        System.Diagnostics.Debug.WriteLine(
            $"[Queue] Chunk enqueued={written} — queue depth={depth}");
    }

    // BUG 4: single sequential consumer processes chunks without overlap
    private async Task ProcessChunkQueueAsync(ChannelReader<float[]> reader)
    {
        int chunkIndex = 0;
        await foreach (var chunkData in reader.ReadAllAsync())
        {
            chunkIndex++;
            System.Diagnostics.Debug.WriteLine(
                $"[Queue] Transcribing chunk {chunkIndex}, depth={reader.Count}...");
            try
            {
                string raw = await _transcription.TranscribeAsync(
                    chunkData, _settings.Current.TranscriptionLanguage);

                System.Diagnostics.Debug.WriteLine($"[Queue] chunk {chunkIndex} raw='{raw}'");

                if (string.IsNullOrWhiteSpace(raw)) continue;

                string toInject = _sessionChunkCount > 0 ? " " + raw : raw;

                if (_injectedThisSession.Contains(toInject))
                {
                    System.Diagnostics.Debug.WriteLine($"[Dedup] Skipped: '{toInject[..Math.Min(toInject.Length, 40)]}'");
                    continue;
                }
                _injectedThisSession.Add(toInject);

                // BUG 5: inject raw immediately, then optionally correct in background
                System.Diagnostics.Debug.WriteLine($"[Queue] Injecting: '{toInject[..Math.Min(toInject.Length, 40)]}'");
                await _injection.InjectTextAsync(_targetHwnd, toInject);

                if (_settings.Current.AiCorrectionEnabled)
                {
                    string corrected = await _ai.CorrectChunkAsync(raw, _sessionInjectedText);
                    if (corrected != raw && !string.IsNullOrWhiteSpace(corrected))
                    {
                        int backspaces = CountCodePoints(toInject);
                        System.Diagnostics.Debug.WriteLine(
                            $"[AI] Correcting: '{raw}' → '{corrected}' (backspace {backspaces})");
                        await _injection.InjectBackspacesAsync(_targetHwnd, backspaces);
                        string correctedToInject = _sessionChunkCount > 0 ? " " + corrected : corrected;
                        await _injection.InjectTextAsync(_targetHwnd, correctedToInject);
                        toInject = correctedToInject;
                    }
                }

                _sessionInjectedText += toInject;
                _sessionChunkCount++;

                string display = _sessionChunkCount > 0 && toInject.Length > 1
                    ? toInject.TrimStart()
                    : toInject;
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    () => StatusText = $"✓ {display[..Math.Min(display.Length, 30)]}…");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Queue] EXCEPTION chunk {chunkIndex}: {ex.Message}");
            }
        }
        System.Diagnostics.Debug.WriteLine("[Queue] Consumer drained.");
    }

    private static int CountCodePoints(string s)
    {
        int n = 0;
        for (int i = 0; i < s.Length; )
        {
            n++;
            i += char.IsSurrogatePair(s, i) ? 2 : 1;
        }
        return n;
    }

    // Fired when recording stops — final full-buffer transcription
    private async void OnAudioCaptured(object? sender, float[] audioData)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[Pipeline] AudioCaptured: {audioData.Length} samples ({audioData.Length / 16000.0:F1}s), chunks={_sessionChunkCount}");

        if (audioData.Length == 0)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                () => StatusText = "Error: empty audio buffer");
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => IsListening = false);
            return;
        }

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            () => StatusText = "Transcribing…");

        string text = string.Empty;
        try
        {
            // If chunks already handled injection, only transcribe the full buffer
            // for history logging (skip injection to avoid duplicates).
            bool chunksHandled = _sessionChunkCount > 0;

            if (!chunksHandled)
            {
                // Short recording (< 2.5 s) — full-buffer transcription and injection
                text = await _transcription.TranscribeAsync(
                    audioData, _settings.Current.TranscriptionLanguage);

                System.Diagnostics.Debug.WriteLine($"[Pipeline] full-buffer result: '{text}'");

                // ── STEP 1 diagnostic: always show result in StatusText for 3 s ──
                string display = string.IsNullOrWhiteSpace(text) ? "(empty)" : text;
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    () => StatusText = $"Got: '{display}'");
                _ = Task.Delay(3000).ContinueWith(_ =>
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        if (_statusText.StartsWith("Got:"))
                            StatusText = _isMuted ? "Muted" : "Idle";
                    }));

                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Optional AI correction
                    if (_settings.Current.AiCorrectionEnabled)
                        text = await _ai.CorrectChunkAsync(text, string.Empty);

                    if (_injectedThisSession.Contains(text))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Dedup] Skipped full: '{text[..Math.Min(text.Length, 40)]}'");
                    }
                    else
                    {
                        _injectedThisSession.Add(text);
                        System.Diagnostics.Debug.WriteLine($"[Inject] Attempting full: '{text[..Math.Min(text.Length, 40)]}'");
                        await _injection.InjectTextAsync(_targetHwnd, text);
                        System.Diagnostics.Debug.WriteLine($"[Inject] Full done");
                    }
                }
            }
            else
            {
                // Chunks already injected — transcribe full buffer only for history
                text = await _transcription.TranscribeAsync(
                    audioData, _settings.Current.TranscriptionLanguage);
            }

            // History logging uses full-buffer text regardless
            if (!string.IsNullOrWhiteSpace(text) && _settings.Current.SaveHistory)
            {
                await _history.AddAsync(new TranscriptionEntry
                {
                    RawText              = text,
                    InjectedText         = chunksHandled ? _sessionInjectedText : text,
                    RecordedAt           = DateTimeOffset.UtcNow,
                    ModelName            = _transcription.LoadedModelName ?? "unknown",
                    Locale               = _settings.Current.TranscriptionLanguage,
                    AudioDurationSeconds = audioData.Length / 16000.0,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Pipeline] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                () => StatusText = $"Error: {ex.Message[..Math.Min(ex.Message.Length, 28)]}");
        }
        finally
        {
            // Reset session state
            _sessionChunkCount   = 0;
            _sessionInjectedText = string.Empty;

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
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
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                () => StatusText = "Model loading, please wait…");
            return;
        }

        _targetHwnd = _injection.CaptureTargetWindow();
        System.Diagnostics.Debug.WriteLine($"[Target] Captured hwnd={_targetHwnd}");

        // Diagnostic: log which mic is active
        var mics = _audio.GetAvailableMicrophones();
        var selectedMic = mics.FirstOrDefault(m => m.Id == _settings.Current.SelectedMicDeviceId);
        string micName = selectedMic?.Name ?? "(default)";
        System.Diagnostics.Debug.WriteLine(
            $"[Hotkey] Starting — mic: {micName} (id={_settings.Current.SelectedMicDeviceId})");

        // Reset session tracking
        _sessionChunkCount   = 0;
        _sessionInjectedText = string.Empty;
        _injectedThisSession.Clear();

        // BUG 4: fresh channel for this recording session
        _chunkChannel = Channel.CreateUnbounded<float[]>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        _consumerTask = Task.Run(() => ProcessChunkQueueAsync(_chunkChannel.Reader));

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            _isListening = true;
            OnPropertyChanged(nameof(IsListening));
            StatusText = $"Mic: {micName}";
        });

        try
        {
            await _audio.StartAsync(_settings.Current.SilenceTimeoutSeconds);
            System.Diagnostics.Debug.WriteLine("[Hotkey] WaveInEvent started OK");

            // After 2 s revert to normal label
            _ = Task.Delay(2000).ContinueWith(_ =>
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    if (IsListening && !_statusText.StartsWith("Got:"))
                        StatusText = "Listening…";
                }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Hotkey] WaveInEvent FAILED: {ex.Message}");
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                IsListening = false;
                StatusText  = "Mic error — check settings";
            });
        }
    }

    private async Task StopListeningAsync()
    {
        // Close the channel so the consumer knows no more chunks are coming
        _chunkChannel?.Writer.TryComplete();

        // Stop recording → triggers OnRecordingStopped → AudioCaptured → OnAudioCaptured
        await _audio.StopAsync();

        // Drain remaining queued chunks before OnAudioCaptured starts full-buffer path
        if (_consumerTask is not null)
        {
            await _consumerTask;
            _consumerTask = null;
        }
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
