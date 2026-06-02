using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Sends transcribed text to OpenAI GPT-4o-mini for rewriting (AI Pro tier only).
/// The OpenAI API key is InstaType's own key, retrieved from Windows Credential Manager.
/// Falls back gracefully to the raw Whisper text on any API failure.
/// </summary>
public interface IAiPostProcessingService
{
    /// <summary>
    /// Rewrites <paramref name="rawText"/> using the specified <paramref name="mode"/>.
    /// Optionally injects <paramref name="windowTitle"/> as context.
    /// </summary>
    /// <returns>Rewritten text, or <paramref name="rawText"/> if the API call fails.</returns>
    Task<string> RewriteAsync(
        string rawText,
        AiRewriteMode mode,
        string? windowTitle = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies <paramref name="customVocabulary"/> corrections to the text without
    /// a full rewrite (used when mode is <see cref="AiRewriteMode.Standard"/>).
    /// </summary>
    Task<string> ApplyVocabularyAsync(
        string text,
        IReadOnlyList<string> customVocabulary,
        CancellationToken cancellationToken = default);
}
