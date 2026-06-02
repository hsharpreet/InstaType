# InstaType — Current Status

**As of:** 2026-06-02 (session 5)

---

## What Works Right Now

| Feature | Status | Notes |
|---|---|---|
| **Build** | ✅ 0 errors | 4 pre-existing CS0067 warnings (stub events in AuthService / SubscriptionService) |
| **App startup** | ✅ | Tray icon, overlay pill, Whisper model auto-download |
| **Hotkey detection** | ✅ | Double-tap Ctrl via WH_KEYBOARD_LL on dedicated STA thread |
| **Audio capture** | ✅ | NAudio WaveInEvent 16 kHz/16-bit/mono + 3s silence VAD |
| **Waveform bars** | ✅ | 5-bar ScaleY animation driven by real amplitude |
| **Whisper transcription** | ✅ | WhisperNet 1.12.0 Const-me COM; ggml-base.en.bin (141 MB); auto-downloads on first run |
| **Text injection** | ✅ Fixed | SendInput KEYEVENTF_UNICODE, surrogate-pair support, 10 ms inter-char delay. **Critical struct bug fixed (FieldOffset 4→8).** |
| **History persistence** | ✅ | SQLite @ %LOCALAPPDATA%\InstaType\history.db; gated by SaveHistory setting |
| **Settings persistence** | ✅ | JSON @ %LOCALAPPDATA%\InstaType\settings.json; loads on startup, saves on change |
| **Gear popup** | ✅ Fixed | All sections: Microphone, Behaviour, Appearance, Data, App. **DragMove bug fixed.** |
| **Theme switching** | ✅ | System / Light / Dark; swaps ResourceDictionary + WPF-UI ApplicationThemeManager; persists |
| **Always-on-top toggle** | ✅ | Updates Topmost, persists to settings |
| **Start with Windows** | ✅ | Creates/removes Task Scheduler entry via schtasks.exe |
| **Save history toggle** | ✅ | Gates SQLite writes |
| **Mic selection** | ✅ | ComboBox in gear popup; saves device ID |
| **Overlay drag** | ✅ | DragMove on left-click (skips buttons); position saved to settings.json |
| **Green glow on IsListening** | ✅ | ColorAnimation pulsing green border; stops on idle |
| **Overlay does not steal focus** | ✅ | ShowActivated="False"; target app keeps focus for injection |
| **Mic error handling** | ✅ | StatusText = "Mic error — check settings" on StartAsync failure |
| **Model-not-ready guard** | ✅ | Shows "Model loading, please wait…" if Whisper hasn't loaded yet |
| **Tray menu** | ✅ | Show/Hide, Settings, History, Exit |
| **Empty-transcription guard** | ✅ | IsNullOrWhiteSpace check; no injection on silence |

---

## Bugs Fixed in Session 5 (2026-06-02)

1. **INPUT struct field offset (critical)** — `[FieldOffset(4)]` → `[FieldOffset(8)]` in `NativeMethods.INPUT`. On 64-bit Windows the INPUT union sits at byte offset 8 (4 bytes for `type` + 4 bytes implicit pointer-alignment padding). The old offset sent `wVk=0x0004` (VK_CANCEL) instead of the Unicode character, silently dropping every injected character. `Marshal.SizeOf<INPUT>()` now correctly returns 32.

2. **DragMove intercept** — `Window_MouseLeftButtonDown` called `DragMove()` unconditionally, intercepting all button clicks and preventing the gear popup from opening. Fixed by walking the WPF visual tree and skipping drag when any ancestor is a `ButtonBase` or `ComboBox`.

3. **Model-not-ready guard** — `StartListeningAsync` now shows "Model loading, please wait…" instead of silently producing empty transcriptions when Whisper hasn't finished loading.

4. **SetForegroundWindow before injection** — `InjectTextAsync` now calls `SetForegroundWindow(targetHwnd)` + 50 ms settle delay before injecting characters.

---

## How to Run

```
cd C:\Users\hshar\OneDrive\ClaudeCode\InstaType
dotnet run --project InstaType\InstaType.csproj
```

Or launch directly:
```
InstaType\bin\Debug\net8.0-windows10.0.19041.0\win-x64\InstaType.exe
```

**First run:** Whisper model (ggml-base.en.bin, 141 MB) downloads automatically to `%LOCALAPPDATA%\InstaType\Models\`.

**Hotkey:** Double-tap Left or Right Ctrl → starts recording. Double-tap again → stops and transcribes. Silence VAD auto-stops after 3 s.

---

## Known Issues

| Issue | Severity | Notes |
|---|---|---|
| Windows 11 Notepad (WinUI3) doesn't accept raw SendInput | Low | Known platform limitation. All Win32 apps (Chrome, VS Code, Slack, Word, terminal, Notepad++) work. |
| TEST E (mic) not yet automated | Medium | End-to-end hotkey→speak→transcribe→inject requires manual verification with a microphone. |
| SettingsWindow (FluentWindow) not fully wired to ISettingsService | Medium | Opens from tray menu but settings changed via gear popup only. |
| F-08 Auth, F-09 AI Pro not started | — | Per plan |
| F-12 model selector | Partial | Auto-downloads ggml-base.en.bin; no UI for switching models yet. |

---

## Next Steps

| Priority | Feature | Effort |
|---|---|---|
| **High** | Manual TEST E — double-tap Ctrl, speak, verify text in target app | Manual |
| **High** | F-12: Model selector in settings (tiny/base/small/medium) | Medium |
| **High** | F-08: Supabase Auth (email + Google OAuth) | Large |
| **Medium** | F-09: AI Pro (GPT-4o-mini post-processing) | Large |
| **Medium** | Wire SettingsWindow to ISettingsService | Small |
| **Low** | Full localization pass (fr-CA, es strings) | Medium |
| **Low** | MSIX packaging (.wapproj) | Small |
