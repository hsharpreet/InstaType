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
using System.Windows;

namespace InstaType;

/// <summary>
/// Application entry point. Builds the DI host, registers all services and ViewModels,
/// then starts the tray-resident lifecycle. No main window is shown on startup.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // TODO (F-10): Show tray icon, start IHotkeyService, restore auth session.
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
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
