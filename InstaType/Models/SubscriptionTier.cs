namespace InstaType.Models;

/// <summary>
/// Represents the subscription tier a user is on, used to gate features throughout the app.
/// Free → Core → AIPro (AIPro requires Core).
/// </summary>
public enum SubscriptionTier
{
    /// <summary>No account required. 50 transcriptions/day, tiny model, English only.</summary>
    Free = 0,

    /// <summary>$6.99/month. Unlimited transcriptions, all models, all languages, history, sync.</summary>
    Core = 1,

    /// <summary>$9.99/month (Core + $3). GPT-4o-mini post-processing, AI commands, custom vocabulary.</summary>
    AIPro = 2
}
