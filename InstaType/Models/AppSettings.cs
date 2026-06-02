namespace InstaType.Models;

/// <summary>
/// All user-configurable settings for InstaType.
/// Persisted locally and synced to Supabase (Core+).
/// Never store API keys here — use Windows Credential Manager.
/// </summary>
public sealed class AppSettings
{
    // ── Hotkey ──────────────────────────────────────────────────────────────

    /// <summary>Virtual key code of the trigger key (default: VK_CONTROL = 0x11).</summary>
    public int HotkeyVirtualKey { get; set; } = 0x11;

    /// <summary>Whether a double-tap (vs. single press) is required to trigger recording.</summary>
    public bool HotkeyRequiresDoubleTap { get; set; } = true;

    // ── Whisper ──────────────────────────────────────────────────────────────

    /// <summary>Filename of the GGML model to use (e.g. "ggml-medium.bin").</summary>
    public string WhisperModel { get; set; } = "ggml-tiny.bin";

    /// <summary>BCP-47 language code for transcription, or "auto" for auto-detect (Core+).</summary>
    public string TranscriptionLanguage { get; set; } = "en";

    /// <summary>Seconds of silence before recording stops automatically (1–10).</summary>
    public int SilenceTimeoutSeconds { get; set; } = 3;

    // ── Injection ────────────────────────────────────────────────────────────

    /// <summary>When true, show a preview toast before injecting (Core+, off by default).</summary>
    public bool ShowInjectionPreview { get; set; } = false;

    // ── AI Pro ───────────────────────────────────────────────────────────────

    /// <summary>AI rewrite mode applied after transcription (AI Pro only).</summary>
    public AiRewriteMode DefaultRewriteMode { get; set; } = AiRewriteMode.Standard;

    // ── UI ───────────────────────────────────────────────────────────────────

    /// <summary>UI theme override. Null = follow Windows system theme.</summary>
    public string? ThemeOverride { get; set; } = null;

    /// <summary>BCP-47 locale for the app UI (e.g. "en", "fr-CA", "es"). Null = follow Windows.</summary>
    public string? UiLocale { get; set; } = null;

    /// <summary>Whether InstaType should launch when Windows starts.</summary>
    public bool LaunchAtStartup { get; set; } = false;

    // ── Overlay ──────────────────────────────────────────────────────────────

    /// <summary>NAudio device index of the selected microphone.</summary>
    public int SelectedMicDeviceId { get; set; } = 0;

    /// <summary>Pixel X position of the overlay. -1 = auto-centre on primary screen.</summary>
    public double OverlayLeft { get; set; } = -1;

    /// <summary>Pixel Y position of the overlay.</summary>
    public double OverlayTop { get; set; } = 12;

    /// <summary>Whether the overlay stays above all other windows.</summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>Whether transcriptions are written to the local SQLite history database.</summary>
    public bool SaveHistory { get; set; } = true;
}

/// <summary>AI post-processing rewrite modes available to AI Pro subscribers.</summary>
public enum AiRewriteMode
{
    /// <summary>Fix obvious errors and punctuation only; do not alter meaning.</summary>
    Standard,
    /// <summary>Rewrite in a formal, professional tone.</summary>
    Formal,
    /// <summary>Rewrite in a casual, conversational tone.</summary>
    Casual,
    /// <summary>Convert to a bullet-point list.</summary>
    BulletPoints,
    /// <summary>Condense to the shortest clear version.</summary>
    Concise
}
