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
WhisperNet                          1.12.0
WPF-UI (lepoco/wpfui)               latest stable
Supabase                            1.1.2
Microsoft.Extensions.Hosting        8.x
Microsoft.Data.Sqlite               latest
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

### Phase: Pre-scaffold

| # | Feature | Status | Notes |
|---|---|---|---|
| — | Research | ✅ Complete | See RESEARCH.md |
| — | PRD | ✅ Complete | See PRD.md |
| — | Solution scaffold | ✅ Complete | InstaType.slnx + InstaType.csproj, net8.0-windows10.0.19041.0, dotnet build → 0 errors |
| F-01 | Double-tap Ctrl hotkey | ⬜ Not started | |
| F-02 | Audio capture + VAD | ⬜ Not started | |
| F-03 | Whisper transcription | ⬜ Not started | |
| F-04 | Text injection | ⬜ Not started | |
| F-05 | Overlay UI | ⬜ Not started | |
| F-06 | Transcription history | ⬜ Not started | |
| F-07 | Settings & configuration | ⬜ Not started | |
| F-08 | Auth & account | ⬜ Not started | |
| F-09 | AI post-processing | ⬜ Not started | |
| F-10 | System tray & lifecycle | ⬜ Not started | |
| F-11 | Dark / light mode | ⬜ Not started | |
| F-12 | Model management | ⬜ Not started | |

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
