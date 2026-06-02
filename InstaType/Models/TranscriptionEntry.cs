namespace InstaType.Models;

/// <summary>
/// A single completed transcription, persisted to SQLite (Core+) or held in memory (Free).
/// Maps to the <c>transcription_entries</c> table.
/// </summary>
public sealed class TranscriptionEntry
{
    /// <summary>Auto-increment primary key.</summary>
    public long Id { get; set; }

    /// <summary>UTC timestamp when recording started.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Raw text output from Whisper before any AI post-processing.</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>AI-rewritten text (AI Pro only). Null when AI post-processing was not applied.</summary>
    public string? AiText { get; set; }

    /// <summary>The text that was actually injected into the target application.</summary>
    public string InjectedText { get; set; } = string.Empty;

    /// <summary>Friendly name of the target application window (e.g. "Notepad", "Slack").</summary>
    public string? TargetAppName { get; set; }

    /// <summary>Duration of the recorded audio in seconds.</summary>
    public double AudioDurationSeconds { get; set; }

    /// <summary>Whisper model used (e.g. "tiny", "medium").</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>UI language locale active at the time (e.g. "en", "fr-CA").</summary>
    public string Locale { get; set; } = "en";
}
