using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace InstaType.Infrastructure.Supabase;

/// <summary>Maps to the Supabase <c>profiles</c> table (FK to auth.users.id).</summary>
[Table("profiles")]
internal class SupabaseProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("tier")]
    public string? Tier { get; set; }

    [Column("subscription_expires_at")]
    public DateTime? SubscriptionExpiresAt { get; set; }
}
