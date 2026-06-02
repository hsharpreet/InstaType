namespace InstaType.Services;

/// <summary>
/// Wraps the Whisper on-device speech-to-text engine.
/// Accepts raw 16 kHz mono float[] PCM audio and returns a transcript string.
/// Audio never leaves the device on Free and Core tiers.
/// Model must be loaded before transcription can begin.
/// </summary>
public interface ITranscriptionService : IDisposable
{
    /// <summary>
    /// Loads the specified GGML model file from %LOCALAPPDATA%\InstaType\Models\.
    /// Downloads automatically if the file is not present.
    /// </summary>
    Task LoadModelAsync(string modelFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes 16 kHz mono float[] PCM audio to text.
    /// Returns the raw Whisper output. Returns empty string on error.
    /// </summary>
    Task<string> TranscribeAsync(float[] audio, string language = "en", CancellationToken cancellationToken = default);

    /// <summary>Whether a model is currently loaded and ready.</summary>
    bool IsModelLoaded { get; }

    /// <summary>Filename of the currently loaded model, or null.</summary>
    string? LoadedModelName { get; }
}
