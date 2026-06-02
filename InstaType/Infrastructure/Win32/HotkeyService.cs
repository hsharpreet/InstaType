using InstaType.Services;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Implements <see cref="IHotkeyService"/> using a WH_KEYBOARD_LL system-wide keyboard hook.
/// Runs on a dedicated STA thread with an HwndSource message window and its own Dispatcher.
/// Double-tap detection: two separate Ctrl key-down events within 400 ms fire
/// <see cref="HotkeyTriggered"/> on the UI thread.
/// Auto-repeat is suppressed via a <c>_ctrlDown</c> state flag.
/// </summary>
internal sealed class HotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyTriggered;
    public bool IsActive { get; private set; }

    private const long DoubleTapMs = 400;

    private Thread? _hookThread;
    private Dispatcher? _hookDispatcher;

    // Held reference prevents the GC collecting the delegate while hook is live.
    private NativeMethods.LowLevelKeyboardProc? _hookProc;
    private volatile nint _hookHandle;

    private bool _ctrlDown;          // true while Ctrl is physically held — suppresses auto-repeat
    private long _lastCtrlTick;      // Environment.TickCount64 of the most recent fresh Ctrl press
    private bool _disposed;

    // ── Public API ───────────────────────────────────────────────────────────

    public void Start()
    {
        if (IsActive || _disposed) return;

        var ready = new ManualResetEventSlim(false);

        _hookThread = new Thread(() => HookThreadMain(ready))
        {
            IsBackground = true,
            Name = "InstaType-HotkeyHook"
        };
        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();

        // Block until the hook is installed so callers know it's active on return.
        ready.Wait();
    }

    public void Stop()
    {
        if (!IsActive) return;
        var disp = _hookDispatcher;
        _hookDispatcher = null;
        if (disp is not null && !disp.HasShutdownStarted)
            disp.BeginInvokeShutdown(DispatcherPriority.Normal);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ── Hook thread ──────────────────────────────────────────────────────────

    private void HookThreadMain(ManualResetEventSlim ready)
    {
        _hookDispatcher = Dispatcher.CurrentDispatcher;

        // HwndSource creates a hidden Win32 window on this thread, providing the
        // message pump that WH_KEYBOARD_LL requires to deliver hook callbacks.
        var p = new HwndSourceParameters("InstaTypeHook")
        {
            Width       = 0,
            Height      = 0,
            PositionX   = -32000,
            PositionY   = -32000,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP — no caption, no taskbar
        };
        using var hwndSource = new HwndSource(p);

        _hookProc = HookCallback;
        nint hMod = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookProc, hMod, 0);

        IsActive = _hookHandle != 0;
        ready.Set(); // unblock Start()

        if (_hookHandle != 0)
            Dispatcher.Run(); // blocks until BeginInvokeShutdown()

        // Cleanup after message loop exits.
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
        IsActive = false;
    }

    // ── Hook callback (runs on hook thread) ──────────────────────────────────

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var kbs = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool isCtrl = kbs.vkCode == (uint)NativeMethods.VK_LCONTROL
                       || kbs.vkCode == (uint)NativeMethods.VK_RCONTROL;

            if (isCtrl)
            {
                int msg = (int)wParam;

                if (msg == NativeMethods.WM_KEYDOWN)
                {
                    if (!_ctrlDown) // ignore auto-repeat
                    {
                        _ctrlDown = true;
                        long now     = Environment.TickCount64;
                        long elapsed = _lastCtrlTick == 0 ? long.MaxValue : now - _lastCtrlTick;

                        if (elapsed <= DoubleTapMs)
                        {
                            _lastCtrlTick = 0; // reset so triple-tap doesn't fire again
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                                () => HotkeyTriggered?.Invoke(this, EventArgs.Empty));
                        }
                        else
                        {
                            _lastCtrlTick = now;
                        }
                    }
                }
                else if (msg == NativeMethods.WM_KEYUP)
                {
                    _ctrlDown = false;
                }
            }
        }

        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }
}
