using InstaType.Services;
using System.Runtime.InteropServices;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Implements <see cref="ITextInjectionService"/> using SendInput with KEYEVENTF_UNICODE.
/// Handles BMP characters (2 events each) and surrogate pairs (4 events each).
/// Verifies that Ctrl/Alt/Shift are released before injecting to prevent garbled output.
/// Returns false instead of throwing when UIPI blocks injection.
/// </summary>
internal sealed class TextInjectionService : ITextInjectionService
{
    // ── Public API ───────────────────────────────────────────────────────────

    public nint CaptureTargetWindow() => NativeMethods.GetForegroundWindow();

    public bool Inject(nint targetWindowHandle, string text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        // Release any held modifier keys so they don't garble the injection
        ReleaseModifiers();

        var inputs = BuildInputs(text);
        if (inputs.Length == 0) return true;

        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs,
            Marshal.SizeOf<NativeMethods.INPUT>());

        return sent == inputs.Length;
    }

    public async Task InjectTextAsync(nint targetWindowHandle, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Bring the target window to foreground before injecting so SendInput reaches it.
        if (targetWindowHandle != 0)
        {
            NativeMethods.SetForegroundWindow(targetWindowHandle);
            await Task.Delay(50).ConfigureAwait(false);
        }

        ReleaseModifiers();

        // Inject one Unicode code-point at a time with a 10 ms delay
        var enumerator = EnumerateCodePoints(text);
        while (enumerator.MoveNext())
        {
            var inputs = BuildInputsForCodePoint(enumerator.Current);
            NativeMethods.SendInput((uint)inputs.Length, inputs,
                Marshal.SizeOf<NativeMethods.INPUT>());

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static NativeMethods.INPUT[] BuildInputs(string text)
    {
        var list = new List<NativeMethods.INPUT>();
        var en   = EnumerateCodePoints(text);
        while (en.MoveNext())
            list.AddRange(BuildInputsForCodePoint(en.Current));
        return list.ToArray();
    }

    private static NativeMethods.INPUT[] BuildInputsForCodePoint(int codePoint)
    {
        if (codePoint < 0x10000)
        {
            // BMP character — one key-down + one key-up
            return
            [
                MakeUnicodeInput((ushort)codePoint, keyUp: false),
                MakeUnicodeInput((ushort)codePoint, keyUp: true),
            ];
        }
        else
        {
            // Supplementary character — emit surrogate pair (4 events)
            char hi = (char)(0xD800 + ((codePoint - 0x10000) >> 10));
            char lo = (char)(0xDC00 + ((codePoint - 0x10000) & 0x3FF));
            return
            [
                MakeUnicodeInput(hi, keyUp: false),
                MakeUnicodeInput(hi, keyUp: true),
                MakeUnicodeInput(lo, keyUp: false),
                MakeUnicodeInput(lo, keyUp: true),
            ];
        }
    }

    private static NativeMethods.INPUT MakeUnicodeInput(ushort scanCode, bool keyUp)
    {
        uint flags = NativeMethods.KEYEVENTF_UNICODE;
        if (keyUp) flags |= NativeMethods.KEYEVENTF_KEYUP;

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            ki   = new NativeMethods.KEYBDINPUT
            {
                wVk       = 0,
                wScan     = scanCode,
                dwFlags   = flags,
                time      = 0,
                dwExtraInfo = 0,
            }
        };
    }

    // Check high bit of GetAsyncKeyState — bit 15 set means key is down
    private static bool IsKeyDown(int vk) =>
        (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    private static void ReleaseModifiers()
    {
        // If a modifier is held, send synthetic key-up so it doesn't corrupt the injected text.
        const int VK_LSHIFT   = 0xA0;
        const int VK_RSHIFT   = 0xA1;
        const int VK_LMENU    = 0xA4;
        const int VK_RMENU    = 0xA5;
        const int VK_LCONTROL = 0xA2;
        const int VK_RCONTROL = 0xA3;

        var mods = new[] { VK_LSHIFT, VK_RSHIFT, VK_LMENU, VK_RMENU, VK_LCONTROL, VK_RCONTROL };
        var releases = mods
            .Where(IsKeyDown)
            .Select(vk => new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                ki   = new NativeMethods.KEYBDINPUT
                {
                    wVk     = (ushort)vk,
                    wScan   = 0,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP,
                }
            })
            .ToArray();

        if (releases.Length > 0)
            NativeMethods.SendInput((uint)releases.Length, releases,
                Marshal.SizeOf<NativeMethods.INPUT>());
    }

    /// <summary>Iterates over Unicode code points (not char units) in a string.</summary>
    private static IEnumerator<int> EnumerateCodePoints(string s)
    {
        for (int i = 0; i < s.Length; )
        {
            int cp = char.ConvertToUtf32(s, i);
            yield return cp;
            i += char.IsSurrogatePair(s, i) ? 2 : 1;
        }
    }
}
