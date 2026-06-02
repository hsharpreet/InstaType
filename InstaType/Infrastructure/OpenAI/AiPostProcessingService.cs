using InstaType.Models;
using InstaType.Services;

namespace InstaType.Infrastructure.OpenAI;

/// <summary>
/// Implements <see cref="IAiPostProcessingService"/> using the OpenAI REST API (GPT-4o-mini).
/// InstaType's API key is retrieved from Windows Credential Manager at runtime —
/// it is never stored in files or environment variables.
/// Falls back to returning the raw text on any network or API error.
/// </summary>
internal sealed class AiPostProcessingService : IAiPostProcessingService
{
    // TODO (F-09): Implement RewriteAsync (build system prompt per AiRewriteMode,
    // call GPT-4o-mini, return result or rawText on failure) and ApplyVocabularyAsync.

    public Task<string> RewriteAsync(string rawText, AiRewriteMode mode, string? windowTitle = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<string> ApplyVocabularyAsync(string text, IReadOnlyList<string> customVocabulary, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
