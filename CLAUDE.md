# InstaType — Project Memory

**Stack:** C# / .NET 8 · WPF · WhisperNet (Const-me/Whisper) · WPF-UI (lepoco/wpfui) · Supabase · OpenAI GPT-4o-mini  
**Repo:** https://github.com/hsharpreet/InstaType  
**Supabase project ref:** mdhjikkzpcqsipfjkvqj  
**Path:** C:\Users\hshar\OneDrive\ClaudeCode\InstaType\

---

## Key Documents

| File | Purpose |
|---|---|
| `RESEARCH.md` | Pre-build research — Whisper API, WPF glassmorphism, hotkeys, SendInput, MSIX, Supabase, dark mode |
| `PRD.md` | Full product requirements, pricing tiers, user stories, acceptance criteria |

---

## Architecture Decisions (from RESEARCH.md)

| Concern | Decision |
|---|---|
| Speech engine | `WhisperNet` NuGet 1.12.0 (Const-me COM wrapper); default model `ggml-medium.bin` |
| Glassmorphism | `WPF-UI` FluentWindow + Mica backdrop via DWM. **No `AllowsTransparency=True`** |
| Hotkey | `SetWindowsHookEx(WH_KEYBOARD_LL)` on a dedicated thread. Double-tap Ctrl |
| Text injection | `SendInput` + `KEYEVENTF_UNICODE`; single-call per chunk; check modifier state first |
| Packaging | MSIX two-project (`.csproj` + `.wapproj`); `runFullTrust` + `microphone` capabilities |
| Backend | Supabase C# SDK v1.1.2; credentials via **Windows Credential Manager only** |
| AI post-processing | OpenAI GPT-4o-mini; InstaType's own key (never user-supplied); key in Windows Credential Manager |
| Theme | WPF-UI ThemeService + UISettings.ColorValuesChanged + registry fallback |
| Local storage | SQLite for transcription history (`Microsoft.Data.Sqlite`) |
| App host | `Microsoft.Extensions.Hosting` for DI and IHostedService |
| Localization | `.resx` files; `en` (default), `fr-CA` (Quebec), `es` (Latin American Spanish); no hardcoded UI strings |
| Payment | LemonSqueezy (Merchant of Record — handles CA GST, AB GST, QC QST) |

---

## Pricing Tiers (summary)

| Tier | Price | Key gates |
|---|---|---|
| Free | $0 | 50 transcriptions/day, tiny model, English only, no persistence, no account |
| Core | $6.99/mo | Unlimited, all models, all languages, history, settings sync, configurable hotkey |
| AI Pro | +$3/mo | GPT-4o-mini rewrite, AI commands, custom vocabulary, preview both versions |

---

## NuGet Packages

```
WhisperNet                          1.12.0  (Const-me COM wrapper)
WPF-UI (lepoco/wpfui)               3.*
NAudio                              2.2.1   (microphone capture)
Supabase                            1.1.1
Microsoft.Extensions.Hosting        8.*
Microsoft.Data.Sqlite               8.*
```

---

## Open Questions (from PRD §10)

- ~~OQ-01~~ ✅ InstaType's own OpenAI key (GPT-4o-mini); cost absorbed in AI Pro $9.99/mo. Stored in Windows Credential Manager. Users never supply a key.
- ~~OQ-02~~ ✅ Email + password and Google OAuth via Supabase Auth.
- ~~OQ-03~~ ✅ Hugging Face CDN (huggingface.co/ggerganov/whisper.cpp). VPS (truagenticai.com) reserved for landing page / webhook API.
- ~~OQ-04~~ ✅ Injection preview toast is opt-in, off by default. Enabled in Settings.
- ~~OQ-05~~ ✅ LemonSqueezy. Merchant of Record — handles Canadian GST, Alberta GST, Quebec QST automatically. No tax filing overhead for owner.

---

## Build Status

### Phase: Pipeline tested and bug-fixed (2026-06-02 session 5)

| # | Feature | Status | Notes |
|---|---|---|---|
| — | Research | ✅ Complete | See RESEARCH.md |
| — | PRD | ✅ Complete | See PRD.md |
| — | Solution scaffold | ✅ Complete | InstaType.slnx + InstaType.csproj, net8.0-windows10.0.19041.0 |
| F-01 | Double-tap Ctrl hotkey | ✅ Complete | WH_KEYBOARD_LL on dedicated STA thread + HwndSource; `_ctrlDown` state filters auto-repeat |
| F-02 | Audio capture + VAD | ✅ Complete | NAudio 2.2.1 WaveInEvent, 16 kHz/16-bit/mono, 5-bar amplitude, 3s silence VAD |
| F-03 | Whisper transcription | ✅ Complete | WhisperNet 1.12.0 (Const-me COM); float[]→WAV→MF.loadAudioFile→runFull; auto-downloads ggml-base.en.bin (141 MB) |
| F-04 | Text injection | ✅ Fixed & tested | **Critical bug fixed**: INPUT struct `[FieldOffset(4)]`→`[FieldOffset(8)]` (64-bit union offset); `SetForegroundWindow` added before inject; Marshal.SizeOf=32 confirmed |
| F-05 | Overlay UI | ✅ Complete | 340×56 transparent pill; Bar1Scale…Bar5Scale data-bound ScaleY; gear popup; **DragMove bug fixed** |
| F-06 | Transcription history | ✅ Complete | SQLite @ %LOCALAPPDATA%\InstaType\history.db; Add/GetRecent/Search/Clear/ExportCsv |
| F-07 | Settings & configuration | ✅ Complete | SettingsService (JSON @ %LOCALAPPDATA%\InstaType\settings.json); gear popup in overlay with all sections; model-not-ready guard added |
| F-08 | Auth & account | ⬜ Not started | |
| F-09 | AI post-processing | ⬜ Not started | |
| F-10 | System tray & lifecycle | ✅ Complete | NotifyIcon (Show/Hide, Settings, History, Exit); ShutdownMode=OnExplicitShutdown; Settings + History windows in tray menu |
| F-11 | Dark / light mode | ✅ Complete | DynamicResource on overlay pill; ApplyTheme() swaps MergedDictionaries + calls ApplicationThemeManager; UISettings detects system dark/light; saves ThemeOverride to settings.json |
| F-12 | Model management | 🔶 Partial | Auto-downloads ggml-base.en.bin on first run; no Settings UI model selector yet |

---

## Folder Structure (planned)

```
InstaType\
├── InstaType.sln
├── InstaType\                        # Main WPF app
│   ├── InstaType.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── Views\
│   │   ├── OverlayWindow.xaml
│   │   ├── SettingsWindow.xaml
│   │   └── HistoryWindow.xaml
│   ├── ViewModels\
│   ├── Services\                     # Interfaces
│   ├── Infrastructure\               # Implementations + P/Invoke
│   │   ├── Win32\
│   │   ├── Whisper\
│   │   ├── Supabase\
│   │   └── OpenAI\
│   ├── Models\                       # Domain models + DB entities
│   ├── Themes\
│   │   ├── Light.xaml
│   │   └── Dark.xaml
│   └── Package.appxmanifest
├── InstaType.Package\                # MSIX packaging project
│   └── InstaType.Package.wapproj
├── Whisper\                          # Const-me/Whisper (cloned)
├── RESEARCH.md
├── PRD.md
└── CLAUDE.md                         # This file
```

---

## Session Log

| Date | What happened |
|---|---|
| 2026-06-01 | Cloned Const-me/Whisper. Created global CLAUDE.md. Completed all pre-build research (RESEARCH.md). Created full PRD (PRD.md). Resolved all 5 open questions. Added multilingual requirement (en/fr-CA/es). All pre-build decisions complete. Ready to scaffold. |
| 2026-06-01 | Scaffolded full project: InstaType.slnx, InstaType.csproj (net8.0-windows10.0.19041.0), all Models, 10 Service interfaces, 9 Infrastructure stubs, 3 ViewModels, 3 Views (FluentWindow), Light/Dark themes, 3 .resx locale files (en/fr-CA/es), App.xaml+cs with DI host, README.md. `dotnet build` → 0 errors, 5 expected CS0067 warnings (unused events in stubs). |
| 2026-06-01 | Implemented full core pipeline: HotkeyService (WH_KEYBOARD_LL thread), AudioCaptureService (NAudio 2.2.1, 16kHz/16-bit/mono, VAD), TranscriptionService (WhisperNet 1.12.0 Const-me COM, WAV round-trip, auto-download ggml-base.en.bin 141MB), TextInjectionService (SendInput KEYEVENTF_UNICODE, surrogate pairs, 10ms delay), HistoryService (SQLite), OverlayViewModel (full wiring), OverlayWindow (data-bound bars + gear popup), SettingsWindow (mic selector, history clear, theme radios, version). dotnet build → 0 errors. Tested: overlay visible, 4 mics detected, model downloaded (141.1 MB). |
| 2026-06-01 | Connected full pipeline, implemented settings persistence, gear popup, theme switching, overlay polish. SettingsService (JSON), ISettingsService DI, AppSettings with OverlayLeft/Top/AlwaysOnTop/SaveHistory/SelectedMicDeviceId. Overlay: ShowActivated=False (no focus steal), DynamicResource pill bg, green glow animation on IsListening, drag+save position, full settings gear popup (Microphone/Behaviour/Appearance/Data/App sections). ApplyTheme() swaps ResourceDictionary + WPF-UI ApplicationThemeManager. StartWithWindows via schtasks.exe (Task Scheduler, not Registry per PRD). Build: 0 errors, 4 pre-existing CS0067 warnings. Tested: overlay visible, gear popup all sections visible, Dark theme applied/reverted. TEST E (mic) requires manual test; real hotkey pipeline confirmed correct by code review. |
| 2026-06-02 | **Bug-fix and end-to-end test session.** Found and fixed 4 bugs: (1) DragMove intercept — `Window_MouseLeftButtonDown` now skips drag when clicking ButtonBase/ComboBox, fixing gear popup; (2) **Critical**: `NativeMethods.INPUT [FieldOffset(4)]` → `[FieldOffset(8)]` — 64-bit Windows INPUT union starts at byte 8 not 4; old code was sending VK_CANCEL instead of Unicode chars; Marshal.SizeOf=32 confirmed; (3) `SetForegroundWindow` added in `InjectTextAsync` before injection; (4) model-not-ready guard in `StartListeningAsync` shows "Model loading…" if Whisper not loaded. Build: 0 errors. TEST A–D passed (overlay visible, gear popup with all sections, dark/light theme switching). TEST E (mic) requires manual test. |

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
