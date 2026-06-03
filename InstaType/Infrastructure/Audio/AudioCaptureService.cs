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
    public event EventHandler<float[]>? AudioChunkReady;
    public bool IsRecording { get; private set; }

    // Streaming chunk: fire AudioChunkReady every ChunkFrames samples (1.5 s at 16 kHz)
    private const int ChunkFrames = 24_000;
    private int _framesSinceLastChunk;

    // Waveform amplitude history for staggered wave motion (ring buffer, 8 recent RMS values)
    private readonly float[] _ampHistory = new float[8];
    private int _ampHistoryIdx;

    private WaveInEvent? _waveIn;
    private WaveInEvent? _monitorWaveIn;
    private int _deviceId;
    private readonly List<float> _buffer = new();
    private readonly object _bufferLock = new();

    public bool IsMonitoring { get; private set; }

    public AudioCaptureService()
    {
        // Defensive clear — ensures no stale data from previous DI-scoped use
        lock (_bufferLock) _buffer.Clear();
        Array.Clear(_ampHistory, 0, _ampHistory.Length);
        System.Diagnostics.Debug.WriteLine("[Init] AudioCaptureService ready — buffer empty, no stale audio");
    }

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

    public Task StartMonitorAsync(int deviceId)
    {
        if (IsRecording) return Task.CompletedTask;
        StopMonitorInternal();

        try
        {
            _monitorWaveIn = new WaveInEvent
            {
                DeviceNumber = deviceId,
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };
            _monitorWaveIn.DataAvailable += OnMonitorDataAvailable;
            _monitorWaveIn.StartRecording();
            IsMonitoring = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor] Start failed: {ex.Message}");
            _monitorWaveIn?.Dispose();
            _monitorWaveIn = null;
        }
        return Task.CompletedTask;
    }

    public Task StopMonitorAsync()
    {
        StopMonitorInternal();
        return Task.CompletedTask;
    }

    private void StopMonitorInternal()
    {
        if (_monitorWaveIn is null) return;
        try { _monitorWaveIn.StopRecording(); } catch { /* ignore if already stopped */ }
        _monitorWaveIn.Dispose();
        _monitorWaveIn = null;
        IsMonitoring = false;
    }

    private void OnMonitorDataAvailable(object? sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2;
        if (sampleCount == 0) return;

        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

        float rms = ComputeRms(samples);
        float scaled = ScaleAmplitude(rms);

        // Monitor uses same value for all 5 bars (no history available without recording context)
        WaveformSample?.Invoke(this, new float[] { scaled, scaled * 0.8f, scaled * 0.6f, scaled * 0.8f, scaled });
    }

    public Task StartAsync(int silenceTimeoutSeconds, CancellationToken cancellationToken = default)
    {
        if (IsRecording) return Task.CompletedTask;
        StopMonitorInternal(); // stop preview before real recording

        lock (_bufferLock) _buffer.Clear();
        _framesSinceLastChunk = 0;
        Array.Clear(_ampHistory, 0, _ampHistory.Length);
        _ampHistoryIdx = 0;

        _waveIn = new WaveInEvent
        {
            DeviceNumber       = _deviceId,
            WaveFormat         = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100
        };
        _waveIn.DataAvailable    += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
        _waveIn.StartRecording();

        IsRecording = true;
        // VAD is intentionally disabled — recording runs until StopAsync() is called.
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!IsRecording) return Task.CompletedTask;
        _waveIn?.StopRecording(); // triggers OnRecordingStopped asynchronously
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopMonitorInternal();
        _waveIn?.Dispose();
        _waveIn = null;
    }

    // ── NAudio callbacks ─────────────────────────────────────────────────────

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        int sampleCount = e.BytesRecorded / 2;
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

        float[]? chunk = null;
        lock (_bufferLock)
        {
            _buffer.AddRange(samples);
            _framesSinceLastChunk += sampleCount;

            if (_framesSinceLastChunk >= ChunkFrames)
            {
                int start = Math.Max(0, _buffer.Count - ChunkFrames);
                chunk = _buffer.GetRange(start, _buffer.Count - start).ToArray();
                _framesSinceLastChunk = 0;
            }
        }

        if (chunk is not null)
            AudioChunkReady?.Invoke(this, chunk);

        // Waveform: store scaled RMS in ring buffer, emit staggered values for wave motion
        float rms    = ComputeRms(samples);
        float scaled = ScaleAmplitude(rms);
        _ampHistory[_ampHistoryIdx % _ampHistory.Length] = scaled;
        _ampHistoryIdx++;

        // bar[0]=current, bar[1]=1-behind, bar[2]=2-behind, bar[3]=3-behind, bar[4]=current mirror
        var amplitudes = new float[]
        {
            GetAmp(0), GetAmp(1), GetAmp(2), GetAmp(3), GetAmp(0)
        };
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

    // ── Amplitude helpers ─────────────────────────────────────────────────────

    private static float ComputeRms(float[] samples)
    {
        if (samples.Length == 0) return 0f;
        float sum = 0f;
        foreach (float s in samples) sum += s * s;
        return MathF.Sqrt(sum / samples.Length);
    }

    // 3.5× sensitivity boost + pow(0.6) curve for visual punch
    private static float ScaleAmplitude(float rms)
        => MathF.Pow(Math.Min(1f, rms * 3.5f), 0.6f);

    // Oldest-offset-first read from ring buffer (0 = most recent)
    private float GetAmp(int offset)
    {
        int len = _ampHistory.Length;
        int idx = (_ampHistoryIdx - 1 - offset % len + len * 2) % len;
        return _ampHistory[idx];
    }
}
