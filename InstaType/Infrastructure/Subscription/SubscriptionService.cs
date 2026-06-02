using InstaType.Models;
using InstaType.Services;

namespace InstaType.Infrastructure.Subscription;

/// <summary>
/// Implements <see cref="ISubscriptionService"/>. Reads the tier from
/// <see cref="IAuthService.CurrentUser"/> and enforces daily limits for Free users.
/// Cached tier is considered stale after 24 hours and re-validated against Supabase.
/// Expired subscriptions downgrade to Free and raise <see cref="TierChanged"/>.
/// </summary>
internal sealed class SubscriptionService : ISubscriptionService
{
    private readonly IAuthService _auth;

    public SubscriptionService(IAuthService auth) => _auth = auth;

    public SubscriptionTier CurrentTier => _auth.CurrentUser?.Tier ?? SubscriptionTier.Free;
    public bool IsDailyLimitReached { get; private set; }
    public int DailyTranscriptionsRemaining { get; private set; } = 50;

    public event EventHandler<SubscriptionTier>? TierChanged;

    public bool HasAccess(SubscriptionTier tier) => CurrentTier >= tier;

    // TODO (F-08): Implement RecordTranscriptionAsync (increment + persist daily count),
    // RefreshAsync (call Supabase, compare tier, raise TierChanged if different).

    public Task RecordTranscriptionAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
