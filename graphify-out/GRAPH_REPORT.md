# Graph Report - C:\Users\hshar\OneDrive\ClaudeCode\InstaType\InstaType  (2026-06-02)

## Corpus Check
- Corpus is ~9,113 words - fits in a single context window. You may not need a graph.

## Summary
- 366 nodes · 474 edges · 30 communities (23 shown, 7 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_ViewModel Base & State|ViewModel Base & State]]
- [[_COMMUNITY_Overlay Window UI|Overlay Window UI]]
- [[_COMMUNITY_Audio Capture Service|Audio Capture Service]]
- [[_COMMUNITY_Audio Service Interface|Audio Service Interface]]
- [[_COMMUNITY_Whisper Transcription|Whisper Transcription]]
- [[_COMMUNITY_Win32 PInvoke & Hotkeys|Win32 P/Invoke & Hotkeys]]
- [[_COMMUNITY_History & Settings Storage|History & Settings Storage]]
- [[_COMMUNITY_Auth & Subscription|Auth & Subscription]]
- [[_COMMUNITY_App Entry & Tray|App Entry & Tray]]
- [[_COMMUNITY_AI Post-Processing|AI Post-Processing]]
- [[_COMMUNITY_Text Injection|Text Injection]]
- [[_COMMUNITY_Settings Service|Settings Service]]
- [[_COMMUNITY_Overlay ViewModel Logic|Overlay ViewModel Logic]]
- [[_COMMUNITY_Microphone Models|Microphone Models]]
- [[_COMMUNITY_XAML Resources & Themes|XAML Resources & Themes]]
- [[_COMMUNITY_History Window|History Window]]
- [[_COMMUNITY_Settings Window|Settings Window]]
- [[_COMMUNITY_Transcription Entry Model|Transcription Entry Model]]
- [[_COMMUNITY_App Settings Model|App Settings Model]]
- [[_COMMUNITY_Hotkey Service|Hotkey Service]]
- [[_COMMUNITY_Service Interfaces Misc|Service Interfaces Misc]]
- [[_COMMUNITY_DI Registration|DI Registration]]
- [[_COMMUNITY_Localization Resources|Localization Resources]]
- [[_COMMUNITY_XAML Controls|XAML Controls]]
- [[_COMMUNITY_Injection Models|Injection Models]]
- [[_COMMUNITY_Supabase Auth|Supabase Auth]]
- [[_COMMUNITY_HistoryViewModel|HistoryViewModel]]
- [[_COMMUNITY_SettingsViewModel|SettingsViewModel]]

## God Nodes (most connected - your core abstractions)
1. `OverlayWindow` - 29 edges
2. `OverlayViewModel` - 25 edges
3. `AudioCaptureService` - 20 edges
4. `HotkeyService` - 13 edges
5. `NativeMethods` - 13 edges
6. `TranscriptionService` - 12 edges
7. `App` - 11 edges
8. `TextInjectionService` - 11 edges
9. `RoutedEventArgs` - 11 edges
10. `HistoryService` - 10 edges

## Surprising Connections (you probably didn't know these)
- `AiPostProcessingService` --implements--> `IAiPostProcessingService`  [EXTRACTED]
  Infrastructure/OpenAI/AiPostProcessingService.cs → Services/IAiPostProcessingService.cs
- `SubscriptionService` --implements--> `ISubscriptionService`  [EXTRACTED]
  Infrastructure/Subscription/SubscriptionService.cs → Services/ISubscriptionService.cs
- `HistoryViewModel` --inherits--> `ViewModelBase`  [EXTRACTED]
  ViewModels/HistoryViewModel.cs → ViewModels/ViewModelBase.cs
- `OverlayViewModel` --inherits--> `ViewModelBase`  [EXTRACTED]
  ViewModels/OverlayViewModel.cs → ViewModels/ViewModelBase.cs
- `SettingsViewModel` --inherits--> `ViewModelBase`  [EXTRACTED]
  ViewModels/SettingsViewModel.cs → ViewModels/ViewModelBase.cs

## Import Cycles
- None detected.

## Communities (30 total, 7 thin omitted)

### Community 0 - "ViewModel Base & State"
Cohesion: 0.06
Nodes (23): double, INotifyPropertyChanged, OverlayState, T, bool, EventArgs, IAudioCaptureService, IHistoryService (+15 more)

### Community 1 - "Overlay Window UI"
Cohesion: 0.08
Nodes (14): IServiceProvider, MouseButtonEventArgs, OverlayViewModel, PropertyChangedEventArgs, SolidColorBrush, Storyboard, bool, EventArgs (+6 more)

### Community 2 - "Audio Capture Service"
Cohesion: 0.12
Nodes (13): AudioCaptureService, CancellationTokenSource, float, IAudioCaptureService, CancellationToken, int, List, MicrophoneDevice (+5 more)

### Community 3 - "Audio Service Interface"
Cohesion: 0.11
Nodes (10): IDisposable, CancellationToken, List, MicrophoneDevice, Task, IAudioCaptureService, IHotkeyService, CancellationToken (+2 more)

### Community 4 - "Whisper Transcription"
Cohesion: 0.15
Nodes (11): Callbacks, Context, iMediaFoundation, iModel, CancellationToken, string, Task, ITranscriptionService (+3 more)

### Community 5 - "Win32 P/Invoke & Hotkeys"
Cohesion: 0.16
Nodes (8): DllImport, INPUT, int, LowLevelKeyboardProc, MarshalAs, StringBuilder, uint, NativeMethods

### Community 6 - "History & Settings Storage"
Cohesion: 0.20
Nodes (9): IHistoryService, CancellationToken, DateTimeOffset, IReadOnlyList, Task, TranscriptionEntry, SqliteDataReader, HistoryService (+1 more)

### Community 7 - "Auth & Subscription"
Cohesion: 0.15
Nodes (9): CancellationToken, IAuthService, SubscriptionTier, Task, CancellationToken, SubscriptionTier, Task, ISubscriptionService (+1 more)

### Community 8 - "App Entry & Tray"
Cohesion: 0.17
Nodes (9): App, Application, ContextMenuStrip, ExitEventArgs, IHost, IServiceCollection, NotifyIcon, OverlayWindow (+1 more)

### Community 9 - "AI Post-Processing"
Cohesion: 0.16
Nodes (10): AiRewriteMode, CancellationToken, IReadOnlyList, Task, AiPostProcessingService, AiRewriteMode, CancellationToken, IReadOnlyList (+2 more)

### Community 10 - "Text Injection"
Cohesion: 0.15
Nodes (9): Dispatcher, IHotkeyService, bool, LowLevelKeyboardProc, nint, long, ManualResetEventSlim, Thread (+1 more)

### Community 11 - "Settings Service"
Cohesion: 0.23
Nodes (5): IEnumerator, INPUT, Task, ITextInjectionService, TextInjectionService

### Community 12 - "Overlay ViewModel Logic"
Cohesion: 0.15
Nodes (8): FluentWindow, RoutedEventArgs, HistoryWindow, IAudioCaptureService, IHistoryService, RoutedEventArgs, SelectionChangedEventArgs, SettingsWindow

### Community 13 - "Microphone Models"
Cohesion: 0.29
Nodes (7): IAsyncDisposable, CancellationToken, DateTimeOffset, IReadOnlyList, Task, TranscriptionEntry, IHistoryService

### Community 14 - "XAML Resources & Themes"
Cohesion: 0.36
Nodes (5): IAuthService, CancellationToken, Task, UserProfile, AuthService

### Community 15 - "History Window"
Cohesion: 0.22
Nodes (8): net8.0-windows10.0.19041.0, Microsoft.Data.Sqlite (8.*), Microsoft.Extensions.Hosting (8.*), NAudio (2.2.1), Supabase (1.1.1), WhisperNet (1.12.0), WPF-UI (3.*), Microsoft.NET.Sdk

### Community 16 - "Settings Window"
Cohesion: 0.36
Nodes (5): CancellationToken, Task, ISettingsService, JsonSerializerOptions, SettingsService

### Community 17 - "Transcription Entry Model"
Cohesion: 0.42
Nodes (4): CancellationToken, Task, UserProfile, IAuthService

### Community 18 - "App Settings Model"
Cohesion: 0.43
Nodes (4): AppSettings, CancellationToken, Task, SettingsSyncService

### Community 19 - "Hotkey Service"
Cohesion: 0.48
Nodes (3): CancellationToken, Task, ISettingsService

### Community 20 - "Service Interfaces Misc"
Cohesion: 0.29
Nodes (5): bool, IReadOnlyList, string, Task, HistoryViewModel

## Knowledge Gaps
- **99 isolated node(s):** `allow`, `IHost`, `OverlayWindow`, `NotifyIcon`, `StartupEventArgs` (+94 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ViewModelBase` connect `ViewModel Base & State` to `Service Interfaces Misc`?**
  _High betweenness centrality (0.009) - this node is a cross-community bridge._
- **What connects `allow`, `IHost`, `OverlayWindow` to the rest of the system?**
  _99 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ViewModel Base & State` be split into smaller, more focused modules?**
  _Cohesion score 0.06025641025641026 - nodes in this community are weakly interconnected._
- **Should `Overlay Window UI` be split into smaller, more focused modules?**
  _Cohesion score 0.08235294117647059 - nodes in this community are weakly interconnected._
- **Should `Audio Capture Service` be split into smaller, more focused modules?**
  _Cohesion score 0.12307692307692308 - nodes in this community are weakly interconnected._
- **Should `Audio Service Interface` be split into smaller, more focused modules?**
  _Cohesion score 0.1067193675889328 - nodes in this community are weakly interconnected._
- **Should `Auth & Subscription` be split into smaller, more focused modules?**
  _Cohesion score 0.14705882352941177 - nodes in this community are weakly interconnected._