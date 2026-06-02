using InstaType.Services;
using System.Windows;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Implements <see cref="IHotkeyService"/> using a WH_KEYBOARD_LL system-wide keyboard hook.
/// Runs on a dedicated STA thread with its own Win32 message pump.
/// Double-tap detection uses GetDoubleClickTime() as the configurable threshold.
/// Always calls CallNextHookEx to avoid blocking other applications.
/// </summary>
internal sealed class HotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyTriggered;
    public bool IsActive { get; private set; }

    // TODO (F-01): Implement keyboard hook thread, double-tap state machine,
    // and dispatcher marshalling back to UI thread.

    public void Start() => throw new NotImplementedException();
    public void Stop() => throw new NotImplementedException();
    public void Dispose() => Stop();
}
