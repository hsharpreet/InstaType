using InstaType.Models;
using InstaType.Services;
using NAudio.Wave;

namespace InstaType.Infrastructure.Audio;

/// <summary>
/// Implements <see cref="IAudioCaptureService"/> using NAudio WaveInEvent.
/// Records at 16 kHz / 16-bit / mono (Whisper's required format).
/// Fires <see cref="WaveformSample"/> with 5 amplitude values per NAudio callback (~10× per second).
/// Fires <see cref="AudioCaptured"/> with float[] PCM when recording stops.
/// Basic VAD: auto-stops after <c>silenceTimeoutSeconds</c> of RMS below threshold.
/// </summary>
internal sealed class AudioCaptureService : IAudioCaptureService
{
    public event EventHandler<float[]>? WaveformSample;
    public event EventHandler<float[]>? AudioCaptured;
    public bool IsRecording { get; private set; }

    // Silence detection: RMS below this is considered silence
    private const float SilenceThreshold = 0.01f;

    private WaveInEvent? _waveIn;
    private int _deviceId;
    private readonly List<float> _buffer = new();
    private readonly object _bufferLock = new();

    private CancellationTokenSource? _vadCts;
    private int _silenceTimeoutSeconds;

    // ── Public API ───────────────────────────────────────────────────────────

    public List<MicrophoneDevice> GetAvailableMicrophones()
    {
        var list = new List<MicrophoneDevice>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            list.Add(new MicrophoneDevice { Id = i, Name = caps.ProductName });
        }
        return list;
    }

    public void SetDevice(int deviceId) => _deviceId = deviceId;

    public Task StartAsync(int silenceTimeoutSeconds, CancellationToken cancellationToken = default)
    {
        if (IsRecording) return Task.CompletedTask;

        _silenceTimeoutSeconds = silenceTimeoutSeconds;

        lock (_bufferLock) _buffer.Clear();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = _deviceId,
            WaveFormat  = new WaveFormat(16000, 16, 1), // 16 kHz, 16-bit, mono
            BufferMilliseconds = 100                     // ~10 callbacks per second
        };
        _waveIn.DataAvailable    += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
        _waveIn.StartRecording();

        IsRecording = true;

        // VAD watchdog: start timer that monitors silence
        _vadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = RunVadAsync(_vadCts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!IsRecording) return Task.CompletedTask;
        _vadCts?.Cancel();
        _waveIn?.StopRecording(); // triggers OnRecordingStopped asynchronously
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _vadCts?.Cancel();
        _waveIn?.Dispose();
        _waveIn = null;
    }

    // ── NAudio callbacks ─────────────────────────────────────────────────────

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2; // 16-bit = 2 bytes per sample
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

        lock (_bufferLock)
            _buffer.AddRange(samples);

        // Compute 5 amplitude blocks for the waveform visualiser
        int blockSize = Math.Max(1, sampleCount / 5);
        var amplitudes = new float[5];
        for (int b = 0; b < 5; b++)
        {
            float peak = 0f;
            int start = b * blockSize;
            int end   = Math.Min(start + blockSize, sampleCount);
            for (int i = start; i < end; i++)
                peak = Math.Max(peak, Math.Abs(samples[i]));
            amplitudes[b] = Math.Min(1f, peak * 3f); // boost for visual clarity
        }
        WaveformSample?.Invoke(this, amplitudes);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsRecording = false;

        float[] audio;
        lock (_bufferLock)
            audio = _buffer.ToArray();

        if (audio.Length > 0)
            AudioCaptured?.Invoke(this, audio);

        _waveIn?.Dispose();
        _waveIn = null;
    }

    // ── VAD watchdog ─────────────────────────────────────────────────────────

    private async Task RunVadAsync(CancellationToken ct)
    {
        var lastSoundTime = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(200, ct).ConfigureAwait(false);

            float rms;
            lock (_bufferLock)
            {
                if (_buffer.Count == 0) continue;
                // Compute RMS over the last 200 ms of samples (3200 at 16 kHz)
                int tail = Math.Min(3200, _buffer.Count);
                float sum = 0f;
                int start = _buffer.Count - tail;
                for (int i = start; i < _buffer.Count; i++)
                    sum += _buffer[i] * _buffer[i];
                rms = MathF.Sqrt(sum / tail);
            }

            if (rms > SilenceThreshold)
                lastSoundTime = DateTime.UtcNow;

            if ((DateTime.UtcNow - lastSoundTime).TotalSeconds >= _silenceTimeoutSeconds)
            {
                await StopAsync().ConfigureAwait(false);
                return;
            }
        }
    }
}
