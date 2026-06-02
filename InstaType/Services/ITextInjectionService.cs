namespace InstaType.Services;

/// <summary>
/// Injects Unicode text into the previously focused window using
/// SendInput with KEYEVENTF_UNICODE. All Win32 P/Invoke is in
/// <c>Infrastructure.Win32.TextInjectionService</c>.
/// </summary>
public interface ITextInjectionService
{
    /// <summary>
    /// Injects <paramref name="text"/> into the target window identified by
    /// <paramref name="targetWindowHandle"/>. Checks modifier key state before injecting.
    /// Handles surrogate pairs for supplementary Unicode characters (emoji etc.).
    /// </summary>
    /// <param name="targetWindowHandle">HWND captured just before the overlay was shown.</param>
    /// <param name="text">Text to inject. Must not be null or empty.</param>
    /// <returns>True if injection succeeded; false if blocked by UIPI or target lost focus.</returns>
    bool Inject(nint targetWindowHandle, string text);

    /// <summary>
    /// Captures and returns the HWND of the currently focused window.
    /// Call this immediately before showing the overlay so focus is not lost to InstaType.
    /// </summary>
    nint CaptureTargetWindow();
}
