namespace InstaType.Services;

/// <summary>
/// Injects Unicode text into a target window using SendInput with KEYEVENTF_UNICODE.
/// All Win32 P/Invoke is in <c>Infrastructure.Win32.TextInjectionService</c>.
/// </summary>
public interface ITextInjectionService
{
    /// <summary>
    /// Captures and returns the HWND of the currently focused window.
    /// Call this immediately before showing the overlay so focus is not lost to InstaType.
    /// </summary>
    nint CaptureTargetWindow();

    /// <summary>
    /// Synchronously injects <paramref name="text"/> into the target window.
    /// Returns false if blocked by UIPI or target is lost.
    /// </summary>
    bool Inject(nint targetWindowHandle, string text);

    /// <summary>
    /// Asynchronously injects <paramref name="text"/> one character at a time with
    /// a 10 ms inter-character delay so slow target apps receive every event.
    /// </summary>
    Task InjectTextAsync(nint targetWindowHandle, string text);

    /// <summary>
    /// Sends <paramref name="count"/> Backspace key events, erasing the last N injected
    /// Unicode code points. Used by AI correction to replace raw transcription with
    /// the corrected version.
    /// </summary>
    Task InjectBackspacesAsync(nint targetWindowHandle, int count);
}
