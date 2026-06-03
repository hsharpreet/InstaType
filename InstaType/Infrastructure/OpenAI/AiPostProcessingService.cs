using InstaType.Models;
using InstaType.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace InstaType.Infrastructure.OpenAI;

/// <summary>
/// Implements <see cref="IAiPostProcessingService"/> using the OpenAI REST API (GPT-4o-mini).
/// InstaType's API key is retrieved from Windows Credential Manager at runtime —
/// it is never stored in files or environment variables.
/// Falls back to returning the raw text on any network or API error.
/// </summary>
internal sealed class AiPostProcessingService : IAiPostProcessingService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(4) };

    private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string ChunkSystemPrompt =
        "You are a real-time transcription corrector. Fix only homophones and obvious " +
        "mishearings based on context. Do not rephrase, reorder, or add words. " +
        "Return corrected text only — no explanation, no punctuation changes unless fixing a mishearing.";

    // ── Public API ───────────────────────────────────────────────────────────

    public Task<string> RewriteAsync(string rawText, AiRewriteMode mode,
        string? windowTitle = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException(); // F-09

    public Task<string> ApplyVocabularyAsync(string text, IReadOnlyList<string> customVocabulary,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(); // F-09

    public async Task<string> CorrectChunkAsync(string rawWhisper, string precedingContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawWhisper)) return rawWhisper;

        string? apiKey = GetApiKey();
        if (apiKey is null)
        {
            System.Diagnostics.Debug.WriteLine("[AI] No API key in Credential Manager — skipping correction");
            return rawWhisper;
        }

        try
        {
            // 1.5 s hard timeout so we don't delay injection
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(1.5));

            string userContent = string.IsNullOrEmpty(precedingContext)
                ? rawWhisper
                : $"Previous context: \"{precedingContext}\"\nCorrect: \"{rawWhisper}\"";

            var body = new
            {
                model = "gpt-4o-mini",
                max_tokens = 200,
                messages = new[]
                {
                    new { role = "system", content = ChunkSystemPrompt },
                    new { role = "user",   content = userContent }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, ChatEndpoint);
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return rawWhisper;

            using var doc = JsonDocument.Parse(
                await resp.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false));

            string? corrected = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(corrected) ? rawWhisper : corrected.Trim();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AI] CorrectChunkAsync failed: {ex.Message}");
            return rawWhisper;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetApiKey()
    {
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var cred  = vault.Retrieve("InstaType/OpenAI", "apikey");
            cred.RetrievePassword();
            return cred.Password;
        }
        catch
        {
            return null;
        }
    }
}
