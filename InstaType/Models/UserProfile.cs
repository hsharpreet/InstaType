namespace InstaType.Models;

/// <summary>
/// Authenticated user's profile and subscription state, hydrated from Supabase Auth.
/// Cached locally for offline operation (max 24 hours).
/// </summary>
public sealed class UserProfile
{
    /// <summary>Supabase Auth user ID (UUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>User's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name from OAuth provider or manually set.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Current subscription tier, validated against Supabase at login and cached.</summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

    /// <summary>UTC timestamp when the cached tier was last verified against Supabase.</summary>
    public DateTimeOffset TierVerifiedAt { get; set; }

    /// <summary>UTC expiry of the active subscription. Null for Free tier.</summary>
    public DateTimeOffset? SubscriptionExpiresAt { get; set; }

    /// <summary>Number of transcriptions used today (Free tier gate).</summary>
    public int DailyTranscriptionCount { get; set; }

    /// <summary>UTC date the daily counter was last reset.</summary>
    public DateOnly DailyCounterDate { get; set; }
}
