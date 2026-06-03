using InstaType.Infrastructure.Audio;
using InstaType.Infrastructure.OpenAI;
using InstaType.Infrastructure.Storage;
using InstaType.Infrastructure.Supabase;
using InstaType.Infrastructure.Subscription;
using InstaType.Infrastructure.Win32;
using InstaType.Infrastructure.Whisper;
using InstaType.Services;
using InstaType.ViewModels;
using InstaType.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace InstaType;

/// <summary>
/// Application entry point. Builds the DI host, registers all services and ViewModels,
/// then starts the tray-resident lifecycle.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private OverlayWindow?       _overlay;
    private CursorOverlayWindow? _cursorOverlay;
    private NotifyIcon? _trayIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Diagnostic trace log — captures all Debug.WriteLine to a readable file.
        string logPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "instatype_diag.log");
        System.IO.File.WriteAllText(logPath, $"=== InstaType started {DateTime.Now} ===\n");
        System.Diagnostics.Trace.Listeners.Add(
            new System.Diagnostics.TextWriterTraceListener(logPath, "DiagFile")
            { TraceOutputOptions = System.Diagnostics.TraceOptions.None });
        System.Diagnostics.Trace.AutoFlush = true;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Load persisted settings before starting any service.
        var settings = _host.Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        // Apply stored theme (replaces the default Dark.xaml if user chose differently).
        ApplyTheme(settings.Current.ThemeOverride ?? "System");

        // Apply saved microphone device.
        var audioSvc = _host.Services.GetRequiredService<IAudioCaptureService>();
        audioSvc.SetDevice(settings.Current.SelectedMicDeviceId);

        // Load Whisper model in background (downloads ggml-base.en.bin if not present).
        var transcription = _host.Services.GetRequiredService<ITranscriptionService>();
        _ = transcription.LoadModelAsync("ggml-base.en.bin");

        // Attempt to restore saved Supabase session silently.
        var auth = _host.Services.GetRequiredService<IAuthService>();
        _ = auth.TryRestoreSessionAsync();

        // Start hotkey service — listens for double-tap Ctrl via WH_KEYBOARD_LL.
        var hotkey = _host.Services.GetRequiredService<IHotkeyService>();
        hotkey.Start();

        // Resolve and position overlay.
        _overlay = _host.Services.GetRequiredService<OverlayWindow>();
        _overlay.Topmost = settings.Current.AlwaysOnTop;

        if (settings.Current.OverlayLeft >= 0)
        {
            _overlay.Left = settings.Current.OverlayLeft;
            _overlay.Top  = settings.Current.OverlayTop;
        }
        else
        {
            _overlay.Left = (SystemParameters.PrimaryScreenWidth  / 2) - (_overlay.Width  / 2);
            _overlay.Top  = 12;
        }

        _overlay.Show();

        // Cursor bubble — shown/hidden by CursorOverlayWindow itself via IsListening.
        _cursorOverlay = _host.Services.GetRequiredService<CursorOverlayWindow>();

        // System tray icon.
        _trayIcon = new NotifyIcon
        {
            Icon    = SystemIcons.Application,
            Text    = "InstaType",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
    }

    /// <summary>Applies our custom theme ResourceDictionary and WPF-UI theme.</summary>
    internal static void ApplyTheme(string theme)
    {
        // Resolve "System" to actual Dark/Light based on Windows accent colour.
        string resolved = theme switch
        {
            "Light" => "Light",
            "Dark"  => "Dark",
            _       => IsSystemDarkMode() ? "Dark" : "Light"
        };

        // Swap the custom ResourceDictionary in MergedDictionaries.
        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("/Themes/") == true ||
            d.Source?.OriginalString.Contains("Themes/")  == true);
        if (existing != null) merged.Remove(existing);
        merged.Add(new System.Windows.ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{resolved}.xaml")
        });

        // Apply WPF-UI theme so FluentWindow (SettingsWindow) follows.
        try
        {
            var wpfUiTheme = resolved == "Dark"
                ? Wpf.Ui.Appearance.ApplicationTheme.Dark
                : Wpf.Ui.Appearance.ApplicationTheme.Light;
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(wpfUiTheme);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Theme] WPF-UI apply failed: {ex.Message}");
        }
    }

    internal static bool IsSystemDarkMode()
    {
        try
        {
            var ui = new global::Windows.UI.ViewManagement.UISettings();
            var bg = ui.GetColorValue(global::Windows.UI.ViewManagement.UIColorType.Background);
            return bg.R < 128; // dark background → dark mode
        }
        catch { return false; }
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        var showHide = new ToolStripMenuItem("Show / Hide");
        showHide.Click += (_, _) =>
        {
            if (_overlay is null) return;
            if (_overlay.IsVisible) _overlay.Hide();
            else _overlay.Show();
        };

        var settings = new ToolStripMenuItem("Settings");
        settings.Click += (_, _) =>
        {
            var win = _host!.Services.GetRequiredService<SettingsWindow>();
            win.Show();
        };

        var account = new ToolStripMenuItem("Account");
        account.Click += (_, _) =>
        {
            var auth = _host!.Services.GetRequiredService<IAuthService>();
            if (auth.IsSignedIn)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Signed in as {auth.CurrentUser!.Email}\n\nSign out?",
                    "InstaType Account",
                    System.Windows.MessageBoxButton.YesNo);
                if (result == System.Windows.MessageBoxResult.Yes)
                    _ = auth.SignOutAsync();
            }
            else
            {
                var win = _host!.Services.GetRequiredService<LoginWindow>();
                win.Show();
            }
        };

        var history = new ToolStripMenuItem("History");
        history.Click += (_, _) =>
        {
            var win = _host!.Services.GetRequiredService<HistoryWindow>();
            win.Show();
        };

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(showHide);
        menu.Items.Add(settings);
        menu.Items.Add(account);
        menu.Items.Add(history);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);
        return menu;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Infrastructure ───────────────────────────────────────────────────
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<ITranscriptionService, TranscriptionService>();
        services.AddSingleton<ITextInjectionService, TextInjectionService>();
        services.AddSingleton<IAiPostProcessingService, AiPostProcessingService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<SettingsSyncService>();

        // ── ViewModels ───────────────────────────────────────────────────────
        services.AddSingleton<OverlayViewModel>(); // singleton: OverlayWindow + CursorOverlayWindow share one instance
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<LoginViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        services.AddTransient<OverlayWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<HistoryWindow>();
        services.AddTransient<LoginWindow>();
        services.AddSingleton<CursorOverlayWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _cursorOverlay?.Close();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
