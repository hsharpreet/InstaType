using InstaType.Models;

namespace InstaType.Infrastructure.Supabase;

/// <summary>
/// Syncs <see cref="AppSettings"/> to and from the Supabase <c>user_settings</c> table (Core+).
/// Called by <c>SettingsService</c> after every local save.
/// Silently no-ops for Free tier users or when offline.
/// </summary>
internal sealed class SettingsSyncService
{
    // TODO (F-07): Implement PushAsync (upsert to user_settings) and
    // PullAsync (fetch latest settings, merge with local).

    public Task PushAsync(AppSettings settings, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<AppSettings?> PullAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
