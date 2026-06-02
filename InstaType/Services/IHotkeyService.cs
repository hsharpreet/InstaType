namespace InstaType.Services;

/// <summary>
/// Manages a system-wide low-level keyboard hook (WH_KEYBOARD_LL) to detect the
/// configured trigger hotkey (default: double-tap Ctrl).
/// Runs on a dedicated thread with its own message loop.
/// All Win32 P/Invoke is encapsulated in <c>Infrastructure.Win32.HotkeyService</c>.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>Raised on the UI thread when the trigger hotkey is detected.</summary>
    event EventHandler? HotkeyTriggered;

    /// <summary>Installs the keyboard hook and begins listening.</summary>
    void Start();

    /// <summary>Removes the keyboard hook and stops listening.</summary>
    void Stop();

    /// <summary>Whether the hook is currently active.</summary>
    bool IsActive { get; }
}
