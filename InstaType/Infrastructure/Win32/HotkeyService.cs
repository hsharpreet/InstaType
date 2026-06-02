using InstaType.Services;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Detects double-tap Left/Right Ctrl via RegisterHotKey on a hidden HwndSource message window.
/// Two WM_HOTKEY messages arriving within <see cref="DoubleTapMs"/> fire
/// <see cref="HotkeyTriggered"/> on the UI thread. No global keyboard hook is installed.
/// </summary>
internal sealed class HotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyTriggered;
    public bool IsActive { get; private set; }

    private const int  WM_HOTKEY    = 0x0312;
    private const uint MOD_NOREPEAT = 0x4000;   // suppress auto-repeat; no modifier requirement
    private const uint VK_LCONTROL  = 0xA2;
    private const uint VK_RCONTROL  = 0xA3;
    private const int  ID_LCTRL     = 9001;
    private const int  ID_RCTRL     = 9002;
    private const long DoubleTapMs  = 400;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private long _lastTapTick;          // TickCount64 of most recent Ctrl WM_HOTKEY
    private bool _disposed;

    public void Start()
    {
        if (IsActive || _disposed) return;
        System.Windows.Application.Current.Dispatcher.Invoke(CreateMessageWindow);
        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive) return;
        IsActive = false;
        System.Windows.Application.Current?.Dispatcher.Invoke(DestroyMessageWindow);
        _lastTapTick = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void CreateMessageWindow()
    {
        // WS_POPUP (0x80000000): frameless popup — no taskbar entry, no caption.
        // No WS_VISIBLE: window stays hidden. Positioned far offscreen for safety.
        var p = new HwndSourceParameters("InstaTypeHotkey")
        {
            Width       = 1,
            Height      = 1,
            PositionX   = -32000,
            PositionY   = -32000,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP
        };

        _hwndSource = new HwndSource(p);
        _hwndSource.AddHook(WndProc);

        IntPtr hwnd = _hwndSource.Handle;
        RegisterHotKey(hwnd, ID_LCTRL, MOD_NOREPEAT, VK_LCONTROL);
        RegisterHotKey(hwnd, ID_RCTRL, MOD_NOREPEAT, VK_RCONTROL);
    }

    private void DestroyMessageWindow()
    {
        if (_hwndSource is null) return;
        IntPtr hwnd = _hwndSource.Handle;
        UnregisterHotKey(hwnd, ID_LCTRL);
        UnregisterHotKey(hwnd, ID_RCTRL);
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
        _hwndSource = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return IntPtr.Zero;

        int id = wParam.ToInt32();
        if (id != ID_LCTRL && id != ID_RCTRL) return IntPtr.Zero;

        long now     = Environment.TickCount64;
        long elapsed = _lastTapTick == 0 ? long.MaxValue : now - _lastTapTick;

        if (elapsed <= DoubleTapMs)
        {
            _lastTapTick = 0;   // reset so a triple-tap doesn't fire a second time
            handled = true;
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _lastTapTick = now;
        }

        return IntPtr.Zero;
    }
}
