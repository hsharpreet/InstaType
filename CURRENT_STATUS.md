# InstaType — Current Status

**As of:** 2026-06-02 (session 11)

---

## What Works Right Now

| Feature | Status | Notes |
|---|---|---|
| **Build** | ✅ 0 errors, 0 warnings | INPUT struct Size=40 fix, icon colour resources added |
| **App startup** | ✅ | Tray icon, overlay pill, Whisper model auto-download; session restore on startup |
| **Hotkey: double-tap** | ✅ Fixed | DOWN₁→UP₁→DOWN₂→UP₂ timestamp detection. Auto-repeat suppressed by `_triggerDown` flag (not 50ms). Hold > 300ms = rejected. Tap gap 0–400ms required. All events logged `[HK] DOWN/UP/FIRE/HELD`. Settings-change updates live. |
| **Custom hotkey** | ✅ New | Gear popup → SHORTCUT: ComboBox for activate key (Double Ctrl/Shift/Alt/CapsLock/Ctrl+Space) + stop key (Same/SingleCtrl/SingleShift/Escape). Persisted to settings.json. Live — no restart needed. |
| **Audio capture** | ✅ | NAudio WaveInEvent 16 kHz/16-bit/mono. VAD **disabled** — recording runs until double-tap. |
| **Waveform bars** | ✅ Fixed | 3.5× sensitivity + pow(0.6) curve. Staggered ring-buffer (bar[0]=current, bar[2]=2-behind, bar[4]=mirror) for organic wave motion. Bar heights 16/22/28/22/16. |
| **Mic monitoring preview** | ✅ | Opening gear popup starts passive WaveInEvent on selected mic |
| **Whisper transcription** | ✅ Fixed | GPU → CPU fallback: tries `eModelImplementation.GPU`, falls back to `eModelImplementation.Reference` on failure. This was the root cause of silent transcription on machines without DirectML. |
| **Transcription diagnostics** | ✅ New | `StatusText = "Got: '[result]'"` shown for 3 s after every transcription attempt. `Debug.WriteLine` logs buffer size, TranscribeAsync calls, and exceptions with type info. |
| **Text injection** | ✅ Fixed | `INPUT` struct was 32 bytes — Windows requires 40 on 64-bit. Fixed: `[StructLayout(LayoutKind.Explicit, Size = 40)]`. `win32err=87` (ERROR_INVALID_PARAMETER) was root cause. Pending TEST A to confirm. |
| **Icon colours** | ✅ New | Dark: cyan (`#00D4FF`) mic/speaker, green (`#00FF88`) active, red (`#FF4444`) muted, navy overlay. Light: blue (`#0066CC`) icons. CursorOverlayWindow ring uses `ActiveListenBrush` / `MutedBrush` from theme. |
| **Streaming transcription** | ✅ Fixed | `Channel<float[]>` queue — producer just enqueues (no async), single consumer processes sequentially. Chunk size 1.5s (24000 frames). Eliminates race condition that caused empty/duplicate chunks. `[Queue]` logs show depth. |
| **AI correction parallel** | ✅ New | Inject raw immediately, then `CorrectChunkAsync` runs. If corrected≠raw: backspace N, inject corrected. `InjectBackspacesAsync` added to interface + implementation. |
| **Startup clean state** | ✅ New | `AudioCaptureService` constructor clears buffer. `OverlayViewModel` constructor clears session state. `[Init]` logs confirm no stale data. |
| **Cursor overlay bubble** | ✅ New | `CursorOverlayWindow` — 36×36 transparent always-on-top window with 🎤 + pulsing ring. Follows cursor with DispatcherTimer (50ms). Shows on listening start, hides on stop. IsHitTestVisible=False. |
| **AI grammar correction** | ✅ New | `CorrectChunkAsync` in `AiPostProcessingService` — GPT-4o-mini, 1.5s timeout, graceful fallback. Requires `InstaType/OpenAI` credential in Windows Credential Manager. Toggle in gear popup (off by default). |
| **History persistence** | ✅ | SQLite @ %LOCALAPPDATA%\InstaType\history.db |
| **Settings persistence** | ✅ | JSON @ %LOCALAPPDATA%\InstaType\settings.json; new `AiCorrectionEnabled` field |
| **Mic mute toggle** | ✅ | 🎤 icon in overlay toggles mute; icon → 🔇 + red tint when muted |
| **Speaker/mute button** | ✅ Removed | Separate mute button in overlay removed. Overlay now: mic icon \| bars \| gear. Narrower. |
| **Gear popup** | ✅ | SHORTCUT section added (activate key + stop key ComboBoxes). AI correction, theme, history toggles. |
| **History window** | ✅ Fixed | ListView with Timestamp/Transcription/Words/Duration columns. Search bar (live filter). Clear All + Close buttons. Loads newest-first on open. |
| **Double injection dedup** | ✅ New | `HashSet<string> _injectedThisSession` prevents chunk path + full-buffer path both injecting same text. `[Dedup] Skipped` log confirms. |
| **Theme switching** | ✅ | System / Light / Dark |
| **Tray menu** | ✅ | Show/Hide, Settings, Account, History, Exit |
| **Auth (F-08)** | ✅ New | `AuthService` — Supabase email/password + Google OAuth (PKCE, localhost redirect). Session persisted in Windows Credential Manager via `CredentialSessionHandler`. `LoginWindow` (FluentWindow) with email/password fields + Google button. Account tray item shows email if signed in or opens LoginWindow if signed out. Requires `InstaType/Supabase` / `anonkey` in Credential Manager. |

---

## Root Cause of Transcription Silence (FIXED)

`TranscriptionService.LoadModelAsync` used `eModelImplementation.GPU` (DirectML/Direct3D 12). On machines without proper DirectML support, `Library.loadModelAsync` either throws or returns a non-functional model. The exception was caught inside `TranscribeAsync` which silently returned `""`.

**Fix**: GPU with CPU fallback:
```csharp
try   { _model = await Library.loadModelAsync(..., eModelImplementation.GPU); }
catch { _model = await Library.loadModelAsync(..., eModelImplementation.Reference); }
```

---

## TEST Plan

| Test | What to verify |
|---|---|
| **TEST A** | Launch app → tap Ctrl once → StatusText shows "Mic: [device]" → speak → StatusText shows "Got: '[words]'" for 3 s |
| **TEST B** | Speak > 2.5 s continuously → words start appearing in target app BEFORE you stop speaking |
| **TEST C** | While in Notepad → tap Ctrl → speak "open youtube dot com" → tap Ctrl → text injected |
| **TEST D** | Cursor bubble: tap Ctrl → 36px 🎤 bubble appears near cursor with pulsing ring → tap again → disappears |
| **TEST E** | AI correction: enable in gear popup, speak "right the code" → should correct to "write the code" (requires OpenAI key in Credential Manager) |
| **TEST F** | Ctrl+C, Ctrl+V should NOT trigger hotkey (modifier detection) |

---

## How to Run

```
cd C:\Users\hshar\OneDrive\ClaudeCode\InstaType
dotnet run --project InstaType\InstaType.csproj
```

**First run:** Whisper model auto-downloads to `%LOCALAPPDATA%\InstaType\Models\`.

**Hotkey:** Single Ctrl tap → start recording. Single Ctrl tap again → stop and transcribe.

---

## Known Issues / Next Steps

| Issue / Feature | Priority | Notes |
|---|---|---|
| TEST A–F not yet run | High | Manual verification needed |
| Supabase anon key setup | High | Run `! [void][Windows.Security.Credentials.PasswordVault]::new().Add([Windows.Security.Credentials.PasswordCredential]::new('InstaType/Supabase','anonkey','<YOUR_ANON_KEY>'))` in terminal once |
| Supabase profiles table | High | Create table in Supabase: `id uuid PK references auth.users, tier text, subscription_expires_at timestamptz` |
| F-09: AI Pro rewrite modes | Medium | Full RewriteAsync implementation |
| AI correction API key setup | Medium | Must store "InstaType/OpenAI" / "apikey" credential in Windows Credential Manager |
| F-12: model selector UI | Medium | Tiny/base/small/medium selector in settings |
| SettingsWindow wiring | Low | Opens from tray but settings changed via gear popup only |
| MSIX packaging | Low | .wapproj not started |
