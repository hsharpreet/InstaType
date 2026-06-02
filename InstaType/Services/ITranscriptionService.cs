namespace InstaType.Services;

/// <summary>
/// Wraps the Const-me/Whisper COM engine (via WhisperNet) for on-device speech-to-text.
/// Audio never leaves the device on Free and Core tiers.
/// Model must be loaded before transcription can begin.
/// </summary>
public interface ITranscriptionService : IDisposable
{
    /// <summary>
    /// Loads the specified GGML model file into GPU/CPU memory.
    /// Should be called once at startup. Model files live in %LOCALAPPDATA%\InstaType\Models\.
    /// </summary>
    /// <param name="modelFileName">Filename only, e.g. "ggml-medium.bin".</param>
    Task LoadModelAsync(string modelFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes raw PCM audio bytes to text.
    /// Returns the raw Whisper output before any AI post-processing.
    /// </summary>
    Task<string> TranscribeAsync(byte[] audioData, string language, CancellationToken cancellationToken = default);

    /// <summary>Whether a model is currently loaded and ready.</summary>
    bool IsModelLoaded { get; }

    /// <summary>Filename of the currently loaded model, or null.</summary>
    string? LoadedModelName { get; }
}
