# InstaType

> Double-tap Ctrl. Speak. Your words appear — anywhere on Windows.

InstaType is a Windows voice-to-text desktop app. It runs Whisper AI locally on your PC, injects transcribed text directly into any focused window, and optionally polishes your words with GPT-4o-mini.

---

## Quick start (development)

**Requirements:** Windows 10 21H2+, .NET 8 SDK, Visual Studio 2022, x64 CPU with AVX1+F16C, DirectX 11 GPU

```powershell
# Clone
git clone https://github.com/hsharpreet/InstaType
cd InstaType

# Build
dotnet build InstaType/InstaType.csproj

# Download a Whisper model (first run)
# Place ggml-tiny.bin in %LOCALAPPDATA%\InstaType\Models\
# Download from: https://huggingface.co/ggerganov/whisper.cpp
```

---

## Project structure

```
InstaType/
├── Models/          Domain models and enums
├── Services/        Service interfaces (IHotkeyService, ITranscriptionService, …)
├── Infrastructure/  Implementations
│   ├── Win32/       Keyboard hook, SendInput text injection
│   ├── Whisper/     WhisperNet wrapper
│   ├── Audio/       Microphone capture
│   ├── OpenAI/      GPT-4o-mini post-processing (AI Pro)
│   ├── Supabase/    Auth + settings sync
│   ├── Storage/     SQLite history
│   └── Subscription/ Tier gating
├── ViewModels/      MVVM ViewModels
├── Views/           WPF XAML windows (FluentWindow, no AllowsTransparency)
├── Themes/          Light.xaml / Dark.xaml ResourceDictionaries
└── Resources/       Strings.resx (en), Strings.fr-CA.resx, Strings.es.resx
```

---

## Pricing

| Tier | Price | Key features |
|---|---|---|
| Free | $0 | 50 transcriptions/day, tiny model, English |
| Core | $6.99/mo | Unlimited, all models, all languages, history, sync |
| AI Pro | $9.99/mo | GPT-4o-mini rewrite, AI commands, custom vocabulary |

---

## Key technical decisions

- **Speech:** WhisperNet 1.12.0 (Const-me/Whisper COM, local, no network)
- **Glassmorphism:** WPF-UI FluentWindow + Mica (DWM P/Invoke, no AllowsTransparency)
- **Hotkey:** WH_KEYBOARD_LL hook on dedicated thread, double-tap Ctrl
- **Text injection:** SendInput + KEYEVENTF_UNICODE
- **Backend:** Supabase (auth + sync) + LemonSqueezy (payments)
- **Secrets:** Windows Credential Manager only — never files, registry, or env vars
- **Localization:** en (default), fr-CA, es via .resx files

See `RESEARCH.md` for detailed findings and `PRD.md` for full requirements.

---

## Build status

See `CLAUDE.md` for feature-by-feature build tracking.
