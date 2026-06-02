using InstaType.Models;
using InstaType.Services;
using System.IO;
using System.Text.Json;

namespace InstaType.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="ISettingsService"/> using JSON at %LOCALAPPDATA%\InstaType\settings.json.
/// Supabase sync is intentionally omitted until F-08 (Auth) is implemented.
/// </summary>
internal sealed class SettingsService : ISettingsService
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "InstaType", "settings.json");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public AppSettings Current { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

        if (!File.Exists(SettingsPath))
        {
            Current = new AppSettings();
            return;
        }

        try
        {
            await using var fs = File.OpenRead(SettingsPath);
            Current = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOpts, cancellationToken)
                      ?? new AppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Load failed: {ex.Message}");
            Current = new AppSettings();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await using var fs = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(fs, Current, JsonOpts, cancellationToken);
        SettingsChanged?.Invoke(this, Current);
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        Current = new AppSettings();
        await SaveAsync(cancellationToken);
    }
}
