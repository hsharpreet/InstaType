using InstaType.Models;
using InstaType.Services;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// WH_KEYBOARD_LL hook that fires <see cref="HotkeyTriggered"/> on a valid double-tap.
///
/// Valid double-tap:
///   DOWN₁ → UP₁ (hold &lt; 300 ms) → DOWN₂ → UP₂ (hold &lt; 300 ms)
///   where (DOWN₂ − UP₁) is 0–400 ms.
///
/// Auto-repeat is suppressed by the <c>_triggerDown</c> flag:
///   once DOWN₁ is recorded, all subsequent KeyDown events for that key
///   are ignored until KeyUp fires (single hardware release = single UP).
///
/// Ctrl+X / Shift+C etc. are rejected via <c>_triggerUsedAsModifier</c>.
/// On settings change the key mapping updates live — no restart needed.
/// </summary>
internal sealed class HotkeyService : IHotkeyService
{
    public event EventHandler? HotkeyTriggered;
    public bool IsActive { get; private set; }

    private readonly ISettingsService _settings;

    private Thread?     _hookThread;
    private Dispatcher? _hookDispatcher;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;
    private volatile nint _hookHandle;
    private bool _disposed;

    // ── Double-tap state (all touched only on hook thread) ───────────────────
    private bool     _triggerDown;
    private bool     _triggerUsedAsModifier;
    private DateTime _lastDownAt  = DateTime.MinValue;  // time of most recent valid DOWN
    private DateTime _prevDownAt  = DateTime.MinValue;  // time of the DOWN before that
    private DateTime _prevUpAt    = DateTime.MinValue;  // time of the UP of the first tap

    // CtrlSpace chord
    private bool _ctrlDown;

    // ── VK constants ─────────────────────────────────────────────────────────
    private const uint VK_LSHIFT  = 0xA0;
    private const uint VK_RSHIFT  = 0xA1;
    private const uint VK_LMENU  = 0xA4;
    private const uint VK_RMENU  = 0xA5;
    private const uint VK_CAPITAL = 0x14;
    private const uint VK_SPACE   = 0x20;
    private const uint VK_ESCAPE  = 0x1B;

    public HotkeyService(ISettingsService settings)
    {
        _settings = settings;
        _settings.SettingsChanged += OnSettingsChanged;
    }

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
        _settings.SettingsChanged -= OnSettingsChanged;
        Stop();
    }

    // ── Hook thread ──────────────────────────────────────────────────────────

    private void HookThreadMain(ManualResetEventSlim ready)
    {
        _hookDispatcher = Dispatcher.CurrentDispatcher;

        var p = new HwndSourceParameters("InstaTypeHook")
        {
            Width = 0, Height = 0,
            PositionX = -32000, PositionY = -32000,
            WindowStyle = unchecked((int)0x80000000),
        };
        using var hwndSource = new HwndSource(p);

        _hookProc   = HookCallback;
        nint hMod   = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookProc, hMod, 0);

        IsActive = _hookHandle != 0;
        System.Diagnostics.Debug.WriteLine(
            $"[HK] Configured for {KeyDisplayName(_settings.Current.ActivateHotkey)} double-tap");
        ready.Set();

        if (_hookHandle != 0)
            Dispatcher.Run();

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
            uint vk  = kbs.vkCode;
            int  msg = (int)wParam;

            string activateKey = _settings.Current.ActivateHotkey;
            string stopKey     = _settings.Current.StopHotkey;

            bool isTrigger = IsTriggerKey(vk, activateKey);
            bool isCtrlKey = vk == (uint)NativeMethods.VK_LCONTROL || vk == (uint)NativeMethods.VK_RCONTROL;

            if (activateKey == "CtrlSpace")
                HandleCtrlSpace(vk, msg, isCtrlKey);
            else
                HandleDoubleTap(vk, msg, isTrigger);

            // Independent stop key (not same as activate)
            if (stopKey != "SameAsActivate" && IsStopKey(vk, msg, stopKey))
                FireHotkeyTriggered();
        }

        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }

    // ── Double-tap state machine ─────────────────────────────────────────────

    private void HandleDoubleTap(uint vk, int msg, bool isTrigger)
    {
        if (msg == NativeMethods.WM_KEYDOWN)
        {
            if (isTrigger)
            {
                if (_triggerDown)
                    return; // auto-repeat — _triggerDown was set on the real DOWN and never cleared
                _triggerDown           = true;
                _triggerUsedAsModifier = false;
                _lastDownAt            = DateTime.UtcNow;
                System.Diagnostics.Debug.WriteLine(
                    $"[HK] DOWN at {_lastDownAt:HH:mm:ss.fff}");
            }
            else if (_triggerDown)
            {
                _triggerUsedAsModifier = true;
            }
        }
        else if (msg == NativeMethods.WM_KEYUP && isTrigger)
        {
            var  now         = DateTime.UtcNow;
            bool wasModifier = _triggerUsedAsModifier;
            _triggerDown           = false;
            _triggerUsedAsModifier = false;

            double holdMs = (now - _lastDownAt).TotalMilliseconds;
            System.Diagnostics.Debug.WriteLine($"[HK] UP holdDuration={holdMs:F0}ms");

            if (wasModifier)
            {
                System.Diagnostics.Debug.WriteLine("[HK] UP — modifier tap, reset");
                ResetDoubleTap();
                return;
            }

            if (holdMs > 300)
            {
                System.Diagnostics.Debug.WriteLine($"[HK] UP holdDuration={holdMs:F0}ms — HELD, reset");
                ResetDoubleTap();
                return;
            }

            // Check for second tap
            double tapGapMs    = (_lastDownAt - _prevUpAt).TotalMilliseconds;
            double firstHoldMs = (_prevUpAt - _prevDownAt).TotalMilliseconds;
            bool   havePrevTap = _prevUpAt > DateTime.MinValue;
            bool   isDoubleTap = havePrevTap && tapGapMs > 0 && tapGapMs < 400 && firstHoldMs < 300;

            if (isDoubleTap)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HK] UP tapGap={tapGapMs:F0}ms firstHold={firstHoldMs:F0}ms → FIRE");
                ResetDoubleTap();
                FireHotkeyTriggered();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HK] UP tapGap={tapGapMs:F0}ms → first tap, waiting for second");
                _prevDownAt = _lastDownAt;
                _prevUpAt   = now;
                _lastDownAt = DateTime.MinValue; // ready for second DOWN
            }
        }
    }

    private void ResetDoubleTap()
    {
        _prevDownAt = _prevUpAt = _lastDownAt = DateTime.MinValue;
    }

    // ── Ctrl+Space chord ─────────────────────────────────────────────────────

    private void HandleCtrlSpace(uint vk, int msg, bool isCtrlKey)
    {
        bool isSpace = vk == VK_SPACE;

        if (msg == NativeMethods.WM_KEYDOWN)
        {
            if (isCtrlKey && !_ctrlDown)
            {
                _ctrlDown = true;
                _triggerUsedAsModifier = false;
            }
            else if (!isCtrlKey && !isSpace && _ctrlDown)
            {
                _triggerUsedAsModifier = true;
            }
            else if (isSpace && _ctrlDown && !_triggerUsedAsModifier)
            {
                FireHotkeyTriggered();
            }
        }
        else if (msg == NativeMethods.WM_KEYUP && isCtrlKey)
        {
            _ctrlDown = false;
            _triggerUsedAsModifier = false;
        }
    }

    private void FireHotkeyTriggered()
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            () => HotkeyTriggered?.Invoke(this, EventArgs.Empty));
    }

    // ── Settings-change handler ───────────────────────────────────────────────

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        string keyName = KeyDisplayName(settings.ActivateHotkey);
        System.Diagnostics.Debug.WriteLine(
            $"[HK] Settings changed — now watching for {keyName} double-tap");
        ResetDoubleTap(); // clear stale first-tap state
    }

    // ── Key classification ────────────────────────────────────────────────────

    private static bool IsTriggerKey(uint vk, string setting) => setting switch
    {
        "DoubleShift"    => vk == VK_LSHIFT  || vk == VK_RSHIFT,
        "DoubleAlt"      => vk == VK_LMENU   || vk == VK_RMENU,
        "DoubleCapsLock" => vk == VK_CAPITAL,
        "CtrlSpace"      => false,
        _                => vk == (uint)NativeMethods.VK_LCONTROL // "DoubleCtrl" (default)
                         || vk == (uint)NativeMethods.VK_RCONTROL,
    };

    private static bool IsStopKey(uint vk, int msg, string setting) => setting switch
    {
        "SingleCtrl"  => (vk == (uint)NativeMethods.VK_LCONTROL || vk == (uint)NativeMethods.VK_RCONTROL)
                         && msg == NativeMethods.WM_KEYUP,
        "SingleShift" => (vk == VK_LSHIFT || vk == VK_RSHIFT) && msg == NativeMethods.WM_KEYUP,
        "Escape"      => vk == VK_ESCAPE && msg == NativeMethods.WM_KEYDOWN,
        _             => false,
    };

    private static string KeyDisplayName(string setting) => setting switch
    {
        "DoubleShift"    => "LShiftKey",
        "DoubleAlt"      => "LMenu (Alt)",
        "DoubleCapsLock" => "CapsLock",
        "CtrlSpace"      => "Ctrl+Space",
        _                => "LControlKey (Ctrl)",
    };
}
