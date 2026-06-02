namespace InstaType.Services;

/// <summary>
/// Captures audio from the default Windows microphone via Media Foundation.
/// Provides real-time waveform data for the overlay visualiser and
/// delivers a completed audio buffer to the transcription pipeline.
/// </summary>
public interface IAudioCaptureService : IDisposable
{
    /// <summary>Raised periodically during recording with a normalised amplitude value (0.0–1.0).</summary>
    event EventHandler<float>? WaveformSample;

    /// <summary>Raised when recording stops and audio data is ready for transcription.</summary>
    event EventHandler<byte[]>? AudioCaptured;

    /// <summary>Starts capturing audio. Stops automatically after silence timeout.</summary>
    /// <param name="silenceTimeoutSeconds">Seconds of silence before auto-stop.</param>
    Task StartAsync(int silenceTimeoutSeconds, CancellationToken cancellationToken = default);

    /// <summary>Stops recording immediately and flushes the audio buffer.</summary>
    Task StopAsync();

    /// <summary>Whether audio capture is currently active.</summary>
    bool IsRecording { get; }
}
