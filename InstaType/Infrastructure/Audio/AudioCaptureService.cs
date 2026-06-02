using InstaType.Services;

namespace InstaType.Infrastructure.Audio;

/// <summary>
/// Implements <see cref="IAudioCaptureService"/> using Media Foundation (via Const-me/Whisper's
/// built-in capture) or NAudio as a fallback. Applies VAD (Voice Activity Detection)
/// to auto-stop after the configured silence timeout. Emits WaveformSample events
/// for the overlay visualiser at ~30 Hz.
/// </summary>
internal sealed class AudioCaptureService : IAudioCaptureService
{
    public event EventHandler<float>? WaveformSample;
    public event EventHandler<byte[]>? AudioCaptured;
    public bool IsRecording { get; private set; }

    // TODO (F-02): Implement StartAsync (open mic, VAD loop, emit WaveformSample),
    // StopAsync (flush buffer, raise AudioCaptured), and Dispose.

    public Task StartAsync(int silenceTimeoutSeconds, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task StopAsync() => throw new NotImplementedException();

    public void Dispose() { }
}
