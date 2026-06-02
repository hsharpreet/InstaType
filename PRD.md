# InstaType — Product Requirements Document

**Version:** 1.0  
**Date:** 2026-06-01  
**Owner:** Harry (hs.harpreet101@gmail.com)  
**Status:** In development — pipeline tested and bug-fixed (session 5, 2026-06-02)

---

## 1. Vision Statement

InstaType turns your voice into typed text — instantly, anywhere on Windows.

Double-tap Ctrl, speak, and your words appear in whatever app you're using: email, Slack, Word, browser, terminal. No copy-paste. No switching windows. No cloud required for the core experience. Just speak and type.

InstaType is built for people who think faster than they type — writers, developers, customer support agents, and anyone who spends hours a day in text fields. It runs locally using Whisper AI, respects privacy by default, and layers on optional AI polish for those who want it.

---

## 2. Target Users

| Persona | Pain point | Key value |
|---|---|---|
| **Knowledge worker** | Types long emails and reports all day | Speed — 3× faster than typing |
| **Developer** | Dictating comments, commit messages, docs | Works in terminals and IDEs |
| **Non-native speaker** | Typing in a second language is slow and error-prone | Accurate transcription + AI rewrite |
| **Accessibility user** | Typing is painful or impossible | Hands-free text entry anywhere |
| **Power user** | Wants to automate text workflows | AI commands and custom vocabulary |

---

## 3. Pricing Tiers

### 3.1 Free

No account required. Works indefinitely with limits.

| Feature | Free |
|---|---|
| Transcriptions per day | 50 |
| Whisper model | `ggml-tiny` only |
| Languages | English only |
| Hotkey | Double-tap Ctrl (fixed, not configurable) |
| Text injection target | Any focused window |
| Transcription history | Last 10 entries (session only, not persisted) |
| Overlay UI | Basic (recording indicator only) |
| Cloud sync | None |
| AI post-processing | None |
| Support | Community only |

**Upgrade prompt:** Shown after the 40th transcription of the day and on app launch if daily limit hit.

---

### 3.2 Core — $6.99 / month

All Free features, plus:

| Feature | Core |
|---|---|
| Transcriptions per day | Unlimited |
| Whisper models | `tiny`, `base`, `small`, `medium` (user selects) |
| Languages | All Whisper-supported languages (auto-detect or manual) |
| Hotkey | Fully configurable (any key, double-tap or single) |
| Transcription history | Persistent, last 500 entries, searchable |
| Overlay UI | Full glassmorphism overlay with live waveform |
| Punctuation mode | Auto-punctuation toggle |
| Silence detection | Configurable timeout (1–10 seconds) |
| Startup with Windows | Yes |
| System tray | Yes, with quick settings |
| Cloud sync (settings) | Yes — hotkey, model, language preferences synced via Supabase |
| Profile / account | Email + password or Google OAuth |
| Export history | CSV / plain text |
| Support | Email support |

---

### 3.3 AI Pro — +$3.00 / month (total $9.99/month, requires Core)

All Core features, plus:

| Feature | AI Pro |
|---|---|
| AI post-processing | GPT-4o-mini rewrite after transcription |
| Rewrite modes | Standard, Formal, Casual, Bullet points, Concise |
| Custom vocabulary | User-defined word list (names, jargon, acronyms) applied via AI |
| AI commands | Spoken commands processed by AI: "make this a list", "fix grammar", "translate to French" |
| Clipboard mode | AI-rewrites clipboard content on hotkey |
| Smart capitalization | Proper nouns, sentence starts, acronyms auto-fixed |
| Context injection | Optionally prefix AI prompt with active window title for context |
| Usage dashboard | Token usage, transcription count, model breakdown |
| Support | Priority email support |

---

## 4. Feature Definitions & User Stories

---

### F-01: Double-Tap Ctrl to Record

**Description:** User double-taps the Left or Right Ctrl key to start recording. The app captures audio from the default microphone, transcribes via Whisper, and injects the result into the currently focused text input.

**User Stories:**

- US-01A: As a user, I want to start recording by double-tapping Ctrl so I don't need to leave the keyboard or switch focus.
- US-01B: As a user, I want the hotkey to work in any app (browser, Word, Slack, VS Code, terminal) so I can dictate anywhere.
- US-01C: As a Core user, I want to configure the trigger key and detection mode (double-tap vs. single hotkey) in settings.
- US-01D: As a user, I want a clear visual signal when recording is active so I know the app is listening.
- US-01E: As a user, I want to cancel a recording by pressing Escape so I don't accidentally inject garbage text.

**Acceptance Criteria:**
- AC-01A: Double-tapping Ctrl within the system double-click threshold starts recording in ≤ 200ms.
- AC-01B: Recording starts correctly when Notepad, Chrome, VS Code, Windows Terminal, and Slack are the active window.
- AC-01C: [Core] Hotkey configuration persists across app restarts.
- AC-01D: Overlay appears within 100ms of recording start.
- AC-01E: Pressing Escape dismisses overlay and discards any partial transcription; no text is injected.
- AC-01F: A second double-tap while recording stops recording and triggers transcription.

---

### F-02: Audio Capture & Voice Activity Detection

**Description:** Captures microphone audio via Media Foundation (Const-me/Whisper's built-in capture). Records until silence is detected or the user stops manually.

**User Stories:**

- US-02A: As a user, I want recording to stop automatically after a configurable silence period so I don't have to press anything.
- US-02B: As a user, I want to see a live audio waveform so I can tell the app is picking up my voice.
- US-02C: As a Core user, I want to configure the silence timeout (1–10 seconds) to match how I speak.

**Acceptance Criteria:**
- AC-02A: Recording stops automatically when no speech is detected for the configured timeout (default 3 seconds).
- AC-02B: [Core] Live waveform animation is visible in the overlay during capture.
- AC-02C: [Core] Silence timeout is configurable from Settings and persists.
- AC-02D: App correctly captures audio when system default microphone changes (e.g., headset plugged in mid-session).
- AC-02E: Microphone permission denial shows a clear, actionable error message.

---

### F-03: Whisper Transcription

**Description:** Audio is passed to the Const-me/Whisper COM engine (via WhisperNet) for on-device transcription. No audio ever leaves the device (Free and Core tiers).

**User Stories:**

- US-03A: As a user, I want my audio transcribed locally so my words never leave my computer.
- US-03B: As a Core user, I want to choose my Whisper model (tiny/base/small/medium) to balance speed vs. accuracy.
- US-03C: As a Core user, I want to set my language or enable auto-detect so the app handles multilingual use.
- US-03D: As a user, I want transcription to complete as fast as possible after I stop speaking.

**Acceptance Criteria:**
- AC-03A: No network requests are made during transcription (Free and Core). Verified with network monitor.
- AC-03B: [Free] Only `ggml-tiny` model is available; model selector is locked in UI.
- AC-03C: [Core] Model selection UI shows tiny/base/small/medium with approximate speed/accuracy labels.
- AC-03D: On GPU-capable hardware, transcription of a 10-second clip completes in ≤ 5 seconds using `medium` model.
- AC-03E: [Core] Language setting (specific or auto-detect) persists across restarts.
- AC-03F: Missing model file shows a download prompt, not a crash.

---

### F-04: Text Injection

**Description:** Transcribed text is injected into the previously focused window using `SendInput` with `KEYEVENTF_UNICODE`. The InstaType overlay window is never the injection target.

**User Stories:**

- US-04A: As a user, I want transcribed text typed into whatever I was working in so I don't need to paste.
- US-04B: As a user, I want special characters, punctuation, and emoji transcribed correctly.
- US-04C: As a user, I want a brief preview of the transcription before it is injected so I can cancel if Whisper got it wrong.

**Acceptance Criteria:**
- AC-04A: ✅ Text injects into the previously focused window, not the overlay. (`CaptureTargetWindow` + `SetForegroundWindow` before inject)
- AC-04B: ✅ Unicode characters inject correctly; surrogate-pair support for supplementary chars. **Note:** Windows 11 Notepad (WinUI3) has known limitations with raw SendInput — works with all Win32 apps (Chrome, VS Code, Slack, Word, terminal).
- AC-04C: Injection does not occur if the focused window changed to an elevated process (UIPI); user sees a non-blocking toast warning.
- AC-04D: [Core] Preview toast is opt-in (off by default). When enabled in Settings, a 2-second preview toast shows the transcription before injection; Escape cancels, Enter injects immediately.
- AC-04E: ✅ Modifier keys (Ctrl, Alt, Shift) verified clear before injection via `ReleaseModifiers()`.
- AC-04F: Injection into Windows Terminal (v1.16+) works correctly.

---

### F-05: Overlay UI

**Description:** A small floating glassmorphism overlay appears during recording and transcription, showing state and waveform. Uses `WPF-UI` `FluentWindow` with Mica/Acrylic backdrop. No `AllowsTransparency`.

**User Stories:**

- US-05A: As a user, I want a non-intrusive visual indicator during recording so I know the app is active.
- US-05B: As a user, I want the overlay to be visually integrated with Windows 11 aesthetics.
- US-05C: As a user, I want the overlay positioned at the bottom center of the screen (away from my work).
- US-05D: As a user, I want the overlay to disappear automatically after injection is complete.

**Acceptance Criteria:**
- AC-05A: Overlay uses Mica or Acrylic backdrop via DWM P/Invoke; `AllowsTransparency` is False.
- AC-05B: Overlay shows three states: **Listening** (waveform), **Transcribing** (spinner + "Thinking…"), **Done** (checkmark, auto-dismisses in 1.5s).
- AC-05C: Overlay is positioned bottom-center, above the taskbar, regardless of primary monitor resolution.
- AC-05D: Overlay does not steal focus from the target application (uses `ShowWindow` without focus).
- AC-05E: Overlay respects system dark/light mode immediately on toggle.
- AC-05F: [Free] Overlay shows a subtle "Free — 50/day" counter badge.

---

### F-06: Transcription History

**Description:** Keeps a log of all transcriptions with timestamp and app context.

**User Stories:**

- US-06A: As a user, I want to review what I dictated earlier in case I need to re-use it.
- US-06B: As a Core user, I want to search my history by keyword.
- US-06C: As a Core user, I want to export my history to CSV or plain text.
- US-06D: As a Free user, I want to see my last 10 transcriptions in the current session.

**Acceptance Criteria:**
- AC-06A: [Free] Last 10 transcriptions shown in session; cleared on app exit.
- AC-06B: [Core] History persists in a local SQLite database across restarts.
- AC-06C: [Core] History shows: timestamp, transcription text, app name, duration (seconds of audio), model used.
- AC-06D: [Core] Keyword search filters history in ≤ 200ms for up to 500 entries.
- AC-06E: [Core] Export to CSV produces a valid file openable in Excel.
- AC-06F: [Core] History can be cleared (full or date-range) from Settings.

---

### F-07: Settings & Configuration

**Description:** A settings window accessible from the system tray icon.

**User Stories:**

- US-07A: As a user, I want to configure my hotkey, model, silence timeout, and language from a single settings screen.
- US-07B: As a Core user, I want my settings synced so they follow me if I reinstall.
- US-07C: As a user, I want InstaType to start with Windows automatically.

**Acceptance Criteria:**
- AC-07A: ✅ Settings window opens from tray icon right-click > Settings.
- AC-07B: ✅ All settings have sensible defaults (gear popup: mic, always-on-top=ON, save-history=ON, start-with-windows=OFF, theme=System).
- AC-07C: [Core] Settings sync to Supabase on change; restored on fresh install after login. _(F-08 not started)_
- AC-07D: ✅ "Launch at startup" toggle creates/removes Task Scheduler entry via schtasks.exe (not Registry).
- AC-07E: ✅ Gear popup uses dark glassmorphism panel; SettingsWindow uses FluentWindow. **Verified: gear popup opens correctly after DragMove bug fix.**
- AC-07F: [Free] Model and language settings are visible but disabled with an upgrade prompt. _(F-08 gating not started)_

---

### F-08: Authentication & Account

**Description:** Supabase-backed auth for Core and AI Pro users. Free users need no account.

**User Stories:**

- US-08A: As a Core user, I want to sign in with email/password or Google OAuth.
- US-08B: As a user, I want my subscription status checked at launch so features unlock immediately after purchase.
- US-08C: As a user, I want to sign out and have all local credentials cleared.

**Acceptance Criteria:**
- AC-08A: Free tier works with zero account interaction.
- AC-08B: Sign-in window supports email/password and Google OAuth.
- AC-08C: Auth tokens stored in Windows Credential Manager, never in files or registry.
- AC-08D: Subscription tier cached locally (max 24h) so the app works offline after last sync.
- AC-08E: Sign-out clears Windows Credential Manager entry and resets tier to Free.
- AC-08F: Expired/cancelled subscription gracefully downgrades to Free; user sees a toast notification.

---

### F-09: AI Post-Processing (AI Pro only)

**Description:** After Whisper transcription, text is sent to OpenAI GPT-4o-mini for rewriting per the user's selected mode. OpenAI API key is user-supplied or billed via InstaType's subscription.

**User Stories:**

- US-09A: As an AI Pro user, I want to choose a rewrite mode (Standard, Formal, Casual, Bullet points, Concise) before or after transcription.
- US-09B: As an AI Pro user, I want spoken commands like "make this a list" to transform the text automatically.
- US-09C: As an AI Pro user, I want a custom vocabulary list so my company name and product names are always spelled correctly.
- US-09D: As an AI Pro user, I want to preview both the raw Whisper output and the AI-rewritten version before injection.

**Acceptance Criteria:**
- AC-09A: [AI Pro] Rewrite mode selector is visible in the overlay as an icon row; last used mode persists.
- AC-09B: [AI Pro] "Standard" mode fixes obvious Whisper errors and punctuation only; does not alter meaning.
- AC-09C: [AI Pro] Spoken AI commands ("make this formal", "bullet points", "translate to Spanish") are detected and executed.
- AC-09D: [AI Pro] Custom vocabulary applied to every transcription; max 100 entries; stored locally + synced.
- AC-09E: [AI Pro] Preview shows raw vs. AI text side-by-side; user can inject either version.
- AC-09F: [AI Pro] AI post-processing failure (network error, quota exceeded) falls back to raw Whisper text; user sees a toast.
- AC-09G: InstaType's OpenAI API key stored in Windows Credential Manager; never exposed to end users. Users do not supply their own key.

---

### F-10: System Tray & Lifecycle

**Description:** InstaType runs as a background tray application. No persistent window when idle.

**User Stories:**

- US-10A: As a user, I want InstaType to live in the system tray so it doesn't clutter my taskbar.
- US-10B: As a user, I want to temporarily pause InstaType (disable hotkey) without quitting.
- US-10C: As a user, I want to quit InstaType cleanly from the tray menu.

**Acceptance Criteria:**
- AC-10A: App launches to tray; no main window shown unless Settings is opened.
- AC-10B: Tray icon right-click shows: Settings, History, Pause/Resume, Sign In/Out, Quit.
- AC-10C: "Pause" disables the keyboard hook and changes the tray icon to a muted state.
- AC-10D: "Quit" disposes the keyboard hook, Whisper context, and audio capture before exiting.
- AC-10E: App handles Windows shutdown/sleep correctly (disposes resources gracefully).

---

### F-11: Dark / Light Mode

**Description:** App follows Windows system theme automatically; manual override available in settings.

**User Stories:**

- US-11A: As a user, I want InstaType's UI to match my Windows theme automatically.
- US-11B: As a user, I want to force light or dark mode in settings regardless of system theme.

**Acceptance Criteria:**
- AC-11A: ✅ Theme switches immediately when radio button is toggled; UISettings detects system theme on startup.
- AC-11B: ✅ Gear popup offers: System / Light / Dark.
- AC-11C: ✅ ThemeOverride persists in settings.json across restarts.
- AC-11D: Contrast audit deferred; both Light.xaml and Dark.xaml define appropriate foreground/background pairs.

---

### F-12: Model Management

**Description:** Whisper GGML model files are not bundled with the app. Users download them on first use or from Settings.

**User Stories:**

- US-12A: As a new user, I want to be guided to download a model on first launch so the app works immediately.
- US-12B: As a Core user, I want to see which models are downloaded and delete ones I don't use.

**Acceptance Criteria:**
- AC-12A: First launch wizard prompts model download with size info and estimated download time.
- AC-12B: Models downloaded to `%LOCALAPPDATA%\InstaType\Models\`.
- AC-12C: Download progress shown in Settings > Models with cancel option.
- AC-12D: [Core] All four model tiers (tiny/base/small/medium) can be downloaded and deleted from Settings.
- AC-12E: [Free] Only tiny model download is offered; others shown with upgrade prompt.
- AC-12F: Corrupt or incomplete model file detected at load time with a re-download prompt (not a crash).

---

## 5. Technical Requirements & Constraints

### 5.1 Platform
- Windows 10 21H2+ (build 19041) minimum; Windows 11 recommended
- 64-bit only
- .NET 8 self-contained MSIX package

### 5.2 Hardware
- CPU: x64 with AVX1 + F16C instruction support (Whisper requirement)
- GPU: DirectX 11 capable (Whisper runs hybrid CPU/GPU if no compatible GPU found)
- Microphone: any Windows-compatible input device
- RAM: minimum 4 GB; recommended 8 GB (medium model uses ~2 GB GPU VRAM)

### 5.3 Dependencies
| Package | Version | Purpose |
|---|---|---|
| `WhisperNet` | 1.12.0 | On-device transcription |
| `WPF-UI` (lepoco/wpfui) | latest stable | FluentWindow, Mica, ThemeService, controls |
| `Supabase` | 1.1.2 | Auth, settings sync, usage logging |
| `Microsoft.Extensions.Hosting` | 8.x | DI, hosted services, IHostedService for keyboard hook |
| `System.Data.SQLite` or `Microsoft.Data.Sqlite` | latest | Local history database |

### 5.4 Architecture
- **Clean layered architecture:** UI → Services → Infrastructure
- **Services (interfaces + implementations):**
  - `IHotkeyService` — keyboard hook lifecycle
  - `IAudioCaptureService` — microphone capture via Media Foundation
  - `ITranscriptionService` — WhisperNet wrapper
  - `ITextInjectionService` — SendInput wrapper
  - `IAiPostProcessingService` — OpenAI GPT-4o-mini calls
  - `ISettingsService` — local + cloud settings
  - `IAuthService` — Supabase auth wrapper
  - `IHistoryService` — SQLite read/write
  - `ISubscriptionService` — tier gating logic
- **ViewModels:** MVVM with `INotifyPropertyChanged`; no code-behind logic
- **No direct P/Invoke calls from ViewModels or Views** — all Win32 encapsulated in Infrastructure layer

### 5.5 Security & Privacy
- No audio data leaves the device on Free or Core tiers
- AI Pro: transcribed text (not audio) sent to OpenAI; user explicitly consented during AI Pro onboarding
- API keys (Supabase anon key, OpenAI key) stored in Windows Credential Manager
- No telemetry in v1.0; only anonymous usage counts sent to Supabase for billing (transcription count)
- App runs non-elevated; MSIX full-trust capability used for SendInput and keyboard hooks

### 5.6 Performance Targets
| Metric | Target |
|---|---|
| Hotkey response (trigger → overlay visible) | ≤ 200ms |
| Transcription of 10s audio (`medium` model, GPU) | ≤ 5s |
| Text injection start after transcription ready | ≤ 100ms |
| App cold start to tray-ready | ≤ 3s |
| Memory footprint (idle, model loaded) | ≤ 400 MB |
| Settings window open | ≤ 300ms |

### 5.7 Reliability
- App must not crash if the target window closes between transcription and injection
- App must recover from Whisper context failure without requiring a restart
- Keyboard hook must survive Windows sleep/wake cycles
- All unhandled exceptions logged to `%LOCALAPPDATA%\InstaType\Logs\` (rolling, max 7 days)

### 5.8 Localization (i18n)

**Developer note:** App owner is an Alberta, Canada resident. French (Quebec) and Spanish support are required for the target market. All UI strings must be in `.resx` resource files from day one — no hardcoded strings in XAML or C#.

| Locale | Language | Status |
|---|---|---|
| `en` | English | Default |
| `fr-CA` | French (Canadian) | Required — Quebec market, Bill 101 compliance |
| `es` | Spanish (Latin American) | Required |

**Implementation approach:**
- `Strings.resx` (English default), `Strings.fr-CA.resx`, `Strings.es.resx`
- WPF `ObjectDataProvider` binding to resource manager, or `x:Static` bindings
- Language selection in Settings (follows Windows display language by default, user-overridable)
- App detects Windows `CultureInfo` at startup and applies matching locale; falls back to English
- Whisper transcription language is **separate** from UI language — a French-UI user can still dictate in English

**Acceptance Criteria:**
- AC-L01: All visible UI text is sourced from `.resx` files; no hardcoded strings in XAML or code-behind.
- AC-L02: Switching UI language in Settings takes effect immediately without restart.
- AC-L03: French (fr-CA) and Spanish (es) translations cover 100% of UI strings before v1.0 release.
- AC-L04: Date/time formats in history view respect the selected locale.
- AC-L05: App installer (MSIX) supports English, French, and Spanish display names.

---

## 6. Out of Scope — v1.0

| Item | Reason |
|---|---|
| macOS / Linux support | Const-me/Whisper is Windows-only; cross-platform is Phase 3 |
| Streaming partial transcription (word-by-word) | 5–10s Whisper latency makes this misleading in v1 |
| Custom Whisper fine-tuning | Complexity; use standard GGML models |
| Speaker diarization | Phase 2 feature (requires `-tdrz` models) |
| Background ambient transcription (always-on) | Privacy concern; requires explicit trigger in v1 |
| In-app audio playback of recordings | Out of scope; no recordings are persisted |
| Plugin/extension API | Phase 3 |
| Mobile app | Out of scope |
| Windows Registry for any settings | Explicitly forbidden (per project rules) |
| Whisper large model | 3 GB+ is too large for default UX; Phase 2 advanced option |
| Team/enterprise accounts | Phase 3 |
| Per-app profiles (different hotkey per app) | Phase 2 |

---

## 7. Phase 2 Roadmap

| Feature | Description |
|---|---|
| **Streaming partial results** | Show words appearing in real time as Whisper decodes (requires Whisper streaming API improvements) |
| **Speaker diarization** | Detect and label multiple speakers using `-tdrz` models |
| **Per-app profiles** | Different hotkey, model, language per target application |
| **Whisper Large model** | Add `ggml-large` as an advanced download option |
| **AI command macros** | Save and name custom AI instructions, trigger by voice |
| **Context-aware injection** | Detect active app type (email, code editor, chat) and auto-select appropriate AI mode |
| **Clipboard pipeline** | Apply AI rewrite to clipboard contents without transcription |
| **Usage analytics dashboard** | Weekly transcription stats, most-used modes, time saved estimate |
| **Windows Hello auth** | Biometric unlock for the app |
| **Always-on ambient mode** | Opt-in continuous recording with manual commit (for meeting notes use case) |

---

## 8. Phase 3 Roadmap

| Feature | Description |
|---|---|
| **Team / Enterprise tier** | Shared custom vocabulary, usage reporting, SSO |
| **Plugin API** | Allow third parties to add injection targets, AI modes, or language processors |
| **Cross-platform** | Investigate whisper.cpp (not Const-me) for macOS/Linux port |
| **Mobile companion** | iOS/Android app to trigger InstaType on Windows via LAN |
| **Custom local LLM** | Replace OpenAI with local Ollama/LM Studio for fully offline AI Pro |
| **Voice shortcuts** | Trigger OS-level actions ("open Chrome", "copy that", "undo") |
| **Meeting transcription mode** | Full-session transcription saved as a note (Obsidian integration) |

---

## 9. Success Metrics (v1.0)

| Metric | Target (90 days post-launch) |
|---|---|
| Daily active users | 500+ |
| Free → Core conversion rate | ≥ 8% |
| Core → AI Pro attachment rate | ≥ 20% |
| Crash-free rate | ≥ 99.5% |
| Avg transcription satisfaction (in-app thumbs) | ≥ 4.0 / 5.0 |
| Support ticket volume | < 2% of DAU |

---

## 10. Open Questions

| # | Question | Owner | Due |
|---|---|---|---|
| ~~OQ-01~~ | ~~Will OpenAI API usage be billed via InstaType's key (included in $9.99) or user-supplied?~~ | ✅ Resolved | InstaType's own OpenAI key (GPT-4o-mini); cost absorbed in AI Pro tier. Users never supply a key. |
| ~~OQ-02~~ | ~~Which Supabase Auth providers to support at launch?~~ | ✅ Resolved | Email + password and Google OAuth. |
| ~~OQ-03~~ | ~~Where will GGML model files be hosted for download?~~ | ✅ Resolved | Hugging Face CDN (huggingface.co/ggerganov/whisper.cpp). Free, no VPS needed. VPS reserved for landing page / webhook API. |
| ~~OQ-04~~ | ~~Is the 2-second injection preview a hard requirement or opt-in?~~ | ✅ Resolved | Opt-in, off by default. Preview can be enabled in Settings. AC-04D updated accordingly. |
| ~~OQ-05~~ | ~~Payment processor?~~ | ✅ Resolved | LemonSqueezy. Merchant of Record handles Canadian GST, Alberta GST, Quebec QST automatically. Higher per-transaction fee offset by zero tax compliance overhead for AB/QC customers. |
