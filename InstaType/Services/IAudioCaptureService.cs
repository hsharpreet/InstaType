using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Captures audio from a Windows microphone via NAudio.
/// Provides real-time waveform data (5 amplitude values per tick) for the overlay
/// visualiser and delivers a completed float[] PCM buffer to the transcription pipeline.
/// </summary>
public interface IAudioCaptureService : IDisposable
{
    /// <summary>
    /// Raised periodically during recording with 5 normalised amplitude values (0.0–1.0),
    /// one per waveform bar.
    /// </summary>
    event EventHandler<float[]>? WaveformSample;

    /// <summary>
    /// Raised when recording stops (silence timeout or <see cref="StopAsync"/>).
    /// Payload is a 16 kHz mono float[] array ready for Whisper.
    /// </summary>
    event EventHandler<float[]>? AudioCaptured;

    /// <summary>Starts capturing audio. Auto-stops after <paramref name="silenceTimeoutSeconds"/> of silence.</summary>
    Task StartAsync(int silenceTimeoutSeconds, CancellationToken cancellationToken = default);

    /// <summary>Stops recording immediately and flushes the buffer, raising <see cref="AudioCaptured"/>.</summary>
    Task StopAsync();

    /// <summary>Whether audio capture is currently active.</summary>
    bool IsRecording { get; }

    /// <summary>Returns all available microphone input devices.</summary>
    List<MicrophoneDevice> GetAvailableMicrophones();

    /// <summary>Switches the active capture device. Safe to call when not recording.</summary>
    void SetDevice(int deviceId);
}
