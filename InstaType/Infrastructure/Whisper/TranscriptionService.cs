using InstaType.Services;

namespace InstaType.Infrastructure.Whisper;

/// <summary>
/// Implements <see cref="ITranscriptionService"/> via the WhisperNet NuGet wrapper
/// (Const-me/Whisper COM server). Loads GGML model files from
/// %LOCALAPPDATA%\InstaType\Models\. Reuses the context across transcriptions.
/// Accepts 5–10s transcription latency as a known constraint of the Whisper engine.
/// </summary>
internal sealed class TranscriptionService : ITranscriptionService
{
    public bool IsModelLoaded { get; private set; }
    public string? LoadedModelName { get; private set; }

    // TODO (F-03): Implement LoadModelAsync (Whisper.Library.loadModel),
    // TranscribeAsync (context.transcribe), and Dispose (model + context).

    public Task LoadModelAsync(string modelFileName, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<string> TranscribeAsync(byte[] audioData, string language, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public void Dispose() { }
}
