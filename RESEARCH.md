# InstaType – Pre-Build Research Notes

> Compiled 2026-06-01. All findings verified against current docs/source.

---

## 1. Const-me/Whisper — Windows Whisper API

**Source:** https://github.com/Const-me/Whisper, NuGet WhisperNet 1.12.0

### How it works
- C++ COM server exposed to C# via the `WhisperNet` NuGet package (`dotnet add package WhisperNet`)
- Entry point: `Whisper.Library.loadModel(path)` → returns `iModel`
- Create transcription session: `model.createContext()` → returns `Context`
- Implement `Whisper.Callbacks` subclass to receive streaming results

### Model loading
- Supports GGML `.bin` and `.gguf` formats
- **Recommended model:** `ggml-medium.bin` (1.42 GB) — most tested
- Load once at startup, reuse context across sessions (`using` statements required)
- GPU flags: `eGpuModelFlags.Wave32` for NVIDIA/Intel, `Wave64` for AMD

### Streaming / real-time
- Uses Voice Activity Detection (Moattar & Homayoonpoor 2009) + Media Foundation for capture
- **Latency: 5–10 seconds** — inherent, not a bug. Model needs minimum audio duration to perform well
- Streaming mode: no token-level timestamps. Buffered mode: full token timestamps
- v1.12 improved reliability; mic capture less likely to enter "Stalled" state

### Gotchas
- **Windows 8.1+ 64-bit only.** Requires DirectX 11 GPU and AVX1+F16C CPU
- **Automatic language detection is NOT implemented** — must set `language` param explicitly
- Always dispose `iModel`, `Context`, and audio resources via `using`
- Do NOT download from "whisperdesktop.com" — it is an impersonator

### Decision
Use `WhisperNet` NuGet wrapper (not raw C++ COM). Target `ggml-medium.bin`. Accept 5–10s streaming latency; show UI state "Listening…" / "Transcribing…" to mask it.

---

## 2. WPF Glassmorphism — AllowsTransparency Performance

**Source:** Microsoft Learn, dotnet/wpf issues, lepoco/wpfui

### The problem with AllowsTransparency=True
- Forces **software rendering** — kills GPU acceleration for the whole window
- Known bugs: window maximization fails, HWND interop (e.g. WebView2) won't render, text alpha renders incorrectly
- These issues are longstanding and persist in .NET 8

### Recommended approach for Windows 11 (Decision)
Use **DWM window attributes** via P/Invoke — no AllowsTransparency needed:

```csharp
[DllImport("dwmapi.dll")]
private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

// DWMWA_MICA_EFFECT = 1029 (Windows 11 Mica)
// DWMWA_USE_IMMERSIVE_DARK_MODE = 20
```

Set `Background="{x:Null}"` on the Window, then apply Mica/Acrylic via DWM after the HWND is created (`SourceInitialized` event).

### Library: lepoco/wpfui
- `FluentWindow` control with built-in Mica, Acrylic, Tabbed backdrop
- Handles all DWM P/Invoke internally
- Supports dark/light theme switching
- NuGet: `WPF-UI`

### Decision
Use `WPF-UI` (`lepoco/wpfui`) `FluentWindow` with `Mica` backdrop. Do NOT set `AllowsTransparency=True`. Use `ExtendsContentIntoTitleBar="True"` and `Background="{x:Null}"`.

---

## 3. Double-Tap Ctrl Detection

**Source:** Microsoft Learn (RegisterHotKey, SetWindowsHookEx, LowLevelKeyboardProc)

### RegisterHotKey is NOT suitable
- Cannot register modifier keys alone (Ctrl by itself) — the Win32 API rejects it
- Only fires once per combo, not on individual key timing
- Exclusive registration blocks other apps

### Correct approach: WH_KEYBOARD_LL hook
- `SetWindowsHookEx(WH_KEYBOARD_LL, ...)` captures all keyboard events system-wide before processing
- `KBDLLHOOKSTRUCT.time` field gives millisecond timestamp — use for double-tap interval
- Use `GetDoubleClickTime()` (default ~500ms) as the configurable threshold

### Timing logic
```
On VK_LCONTROL / VK_RCONTROL WM_KEYDOWN:
  if waitingForSecondTap && (time - lastPressTime) < threshold → DOUBLE TAP
  else → set lastPressTime, waitingForSecondTap = true
```

### Gotchas
- **Windows enforces 1000ms max** for hook procedure execution — offload any work to a background thread immediately
- Always call `CallNextHookEx` to avoid blocking other applications
- `KeyEventArgs` modifier properties are unreliable inside hooks; use `GetAsyncKeyState()` if needed
- Run the hook on a dedicated thread with its own message loop

### Decision
Use `SetWindowsHookEx(WH_KEYBOARD_LL)` on a dedicated thread. Default threshold: `GetDoubleClickTime()` (user-configurable). Detect both `VK_LCONTROL` and `VK_RCONTROL`.

---

## 4. SendInput Unicode Text Injection

**Source:** Microsoft Learn (SendInput, KEYBDINPUT), PInvoke.net, Windows Terminal issue #12977

### Correct INPUT structure
```csharp
[StructLayout(LayoutKind.Explicit)]
struct INPUT {
    [FieldOffset(0)] public uint type;      // INPUT_KEYBOARD = 1
    [FieldOffset(4)] public KEYBDINPUT ki;
}
struct KEYBDINPUT {
    public ushort wVk;        // Must be 0 for Unicode
    public ushort wScan;      // Unicode character goes here
    public uint dwFlags;      // KEYEVENTF_UNICODE = 0x0004
    public uint time;         // Always 0
    public IntPtr dwExtraInfo;
}
```

### Sending Unicode
- Each character = 2 events: KeyDown + KeyUp with `KEYEVENTF_UNICODE`
- Surrogate pairs (emoji, U+10000+): send high surrogate then low surrogate (4 events total)
- Send all events for one character in a **single `SendInput` call** for atomicity

### Gotchas
- **`cbSize` must be exactly `Marshal.SizeOf(typeof(INPUT))`** — wrong size = silent failure (returns 0)
- Always check return value of `SendInput`; 0 = failure
- **UIPI**: Cannot inject into elevated processes from a non-elevated app — silently fails
- **Windows Terminal**: Unicode injection bug was fixed in v1.16+; earlier versions mangle characters
- **Ctrl/modifier state**: If user holds Ctrl during injection, characters become shortcuts — check `GetAsyncKeyState(VK_CONTROL)` first
- Multiple `SendInput` calls can interleave with other input — use one call per logical unit

### Decision
Implement `UnicodeTextInjector` service wrapping `SendInput`. Check `GetAsyncKeyState` for modifier keys before injecting. Document UIPI limitation (app runs non-elevated by default). Use single-call approach per word/chunk.

---

## 5. WPF MSIX Packaging (.NET 8)

**Source:** Microsoft Learn, meziantou.net

### Key .csproj properties
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<EnableMsixTooling>true</EnableMsixTooling>
<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
```

### Windows App SDK: NOT required for WPF
- WPF + MSIX uses the traditional Desktop Bridge (full-trust AppContainer)
- Windows App SDK only required for WinUI 3 apps
- **Single-project MSIX is WinUI 3 only** — WPF requires a separate `.wapproj` packaging project

### Project structure
```
InstaType.csproj           ← main WPF app
InstaType.Package.wapproj  ← MSIX packaging project
Package.appxmanifest       ← capabilities, identity, icons
```

### Required manifest capability
```xml
<rescap:Capability Name="runFullTrust"/>
<DeviceCapability Name="microphone"/>
```

### Gotchas
- Must declare `microphone` DeviceCapability for Whisper audio capture
- `runFullTrust` needed for `SendInput`, global keyboard hooks, and file system access
- Self-contained publish (`<SelfContained>true</SelfContained>`) is recommended so users don't need .NET 8 runtime installed separately

### Decision
Use two-project setup (`.csproj` + `.wapproj`). Target `net8.0-windows10.0.19041.0` (Windows 10 21H2+). Declare `microphone` + `runFullTrust`. Self-contained publish for distribution.

---

## 6. Supabase C# SDK

**Source:** https://supabase.com/docs/reference/csharp/introduction, NuGet `Supabase` v1.1.2

### NuGet
```
dotnet add package Supabase
```
Current version: **1.1.2** (Sept 2025). Targets .NET Standard 2.0 → fully compatible with .NET 8.

### Initialization
```csharp
var supabase = new Supabase.Client(url, key, new SupabaseOptions {
    AutoConnectRealtime = true,
    AutoRefreshToken = true
});
await supabase.InitializeAsync();
```
Store `SUPABASE_URL` and `SUPABASE_KEY` in `appsettings.json` or Windows Credential Manager — never hardcoded.

### Models
All models must inherit `BaseModel` and use `[Table]` / `[Column]` / `[PrimaryKey]` attributes.

### Key operations
- `await supabase.From<T>().Get()` — select
- `await supabase.From<T>().Insert(model)` — insert
- `await supabase.From<T>().Upsert(model)` — upsert
- `await supabase.From<T>().On(ListenType.All, handler)` — realtime
- Auth: `supabase.Auth.SignIn(email, password)` / OAuth via `supabase.Auth.SignIn(Provider.Github)`

### Gotchas
- **Realtime is disabled by default** in Supabase project settings — must enable explicitly
- Default row limit is 1,000 rows per query
- LINQ expressions don't support embedded resource columns — use string selectors
- Namespace changed from `Postgrest` → `Supabase.Postgrest` in v1.x

### Decision
Use `Supabase` v1.1.2. Initialize as singleton (DI or static). Store credentials via Windows Credential Manager (never appsettings.json for keys — per Harry's rules). Use for user auth, usage logging, and settings sync.

---

## 7. Windows Dark Mode Detection (WPF)

**Source:** Microsoft Learn (UISettings), meziantou.net, GitHub vb2ae/WPFLightDarkMode

### Two approaches (use both, registry as fallback)

**Primary: UISettings.ColorValuesChanged (WinRT)**
```csharp
var uiSettings = new Windows.UI.ViewManagement.UISettings();
uiSettings.ColorValuesChanged += (s, _) => {
    var isDark = s.GetColorValue(UIColorType.Background) == Colors.Black;
    // dispatch to UI thread and swap ResourceDictionary
};
```
Requires `TargetFramework` with Windows SDK version, e.g. `net8.0-windows10.0.19041.0`.

**Fallback: Registry polling**
```
HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
Value: AppsUseLightTheme  (0 = Dark, 1 = Light)
```

### Applying at runtime
- Use `DynamicResource` (not `StaticResource`) for all theme-sensitive colors/brushes in XAML
- Swap `ResourceDictionary` in `Application.Current.Resources.MergedDictionaries` on theme change
- WPF-UI (`lepoco/wpfui`) handles this automatically when using `FluentWindow`

### Gotchas
- `ColorValuesChanged` is not 100% reliable on all Windows versions — always implement registry fallback
- Must dispatch to UI thread: `Application.Current.Dispatcher.Invoke(...)`
- Target framework must include Windows SDK version for WinRT types to be available

### Decision
Use `UISettings.ColorValuesChanged` as primary with 500ms registry-poll fallback. WPF-UI's `ThemeService` wraps this cleanly — prefer it over manual implementation.

---

## Summary: Key Architectural Decisions

| Concern | Decision |
|---|---|
| Speech engine | `WhisperNet` NuGet, `ggml-medium.bin`, accept 5–10s latency |
| Glassmorphism | `WPF-UI` FluentWindow + Mica (no AllowsTransparency) |
| Hotkey trigger | `WH_KEYBOARD_LL` hook, double-tap Ctrl, dedicated thread |
| Text injection | `SendInput` + `KEYEVENTF_UNICODE`, single-call per chunk |
| Packaging | MSIX two-project, `runFullTrust` + `microphone` capability |
| Backend | Supabase C# SDK v1.1.2, credentials via Windows Credential Manager |
| Theme | WPF-UI ThemeService + UISettings + registry fallback |

---

## NuGet Packages Required

```
WhisperNet            (1.12.0)   — Whisper speech recognition
WPF-UI                (latest)   — FluentWindow, Mica, ThemeService, controls
Supabase              (1.1.2)    — backend auth + database
Microsoft.Extensions.Hosting     — DI/hosted services
```

**Ready to scaffold.**
