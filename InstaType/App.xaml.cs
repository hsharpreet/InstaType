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
    private OverlayWindow? _overlay;
    private NotifyIcon? _trayIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Start hotkey service — listens for double-tap Ctrl via RegisterHotKey.
        var hotkey = _host.Services.GetRequiredService<IHotkeyService>();
        hotkey.Start();

        // Resolve and position overlay at top-center of primary screen.
        _overlay = _host.Services.GetRequiredService<OverlayWindow>();
        _overlay.Left = (SystemParameters.PrimaryScreenWidth / 2) - (_overlay.Width / 2);
        _overlay.Top = 12;
        _overlay.Show();

        // System tray icon.
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "InstaType",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
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

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => Shutdown();

        menu.Items.Add(showHide);
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
        services.AddSingleton<SettingsSyncService>();

        // ── ViewModels ───────────────────────────────────────────────────────
        services.AddTransient<OverlayViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        services.AddTransient<OverlayWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<HistoryWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
