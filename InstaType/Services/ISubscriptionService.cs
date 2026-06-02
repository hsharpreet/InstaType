using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Enforces subscription tier gates for all features.
/// Tier is validated against Supabase at login and cached locally (max 24h).
/// Expired subscriptions gracefully downgrade to Free.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>The user's current active tier.</summary>
    SubscriptionTier CurrentTier { get; }

    /// <summary>Whether the user has reached the Free tier daily transcription limit (50/day).</summary>
    bool IsDailyLimitReached { get; }

    /// <summary>Number of transcriptions remaining today (always MaxValue for Core+).</summary>
    int DailyTranscriptionsRemaining { get; }

    /// <summary>Returns true if <paramref name="tier"/> is accessible with the current subscription.</summary>
    bool HasAccess(SubscriptionTier tier);

    /// <summary>Increments the daily transcription counter. No-op for Core+.</summary>
    Task RecordTranscriptionAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-validates the subscription tier against Supabase.</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Raised when the tier changes (e.g. subscription expired or upgraded).</summary>
    event EventHandler<SubscriptionTier>? TierChanged;
}
