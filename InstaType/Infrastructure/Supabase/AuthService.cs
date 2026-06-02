using InstaType.Models;
using InstaType.Services;

namespace InstaType.Infrastructure.Supabase;

/// <summary>
/// Implements <see cref="IAuthService"/> using the Supabase C# SDK v1.1.2.
/// Supports email/password and Google OAuth.
/// JWT tokens are stored in Windows Credential Manager (never in files).
/// Subscription tier is read from the Supabase <c>profiles</c> table after sign-in.
/// </summary>
internal sealed class AuthService : IAuthService
{
    public UserProfile? CurrentUser { get; private set; }
    public bool IsSignedIn => CurrentUser is not null;

    public event EventHandler<UserProfile?>? AuthStateChanged;

    // TODO (F-08): Implement using Supabase.Client. Store token via
    // Windows Credential Manager. Read tier from profiles table.

    public Task<UserProfile> SignInWithEmailAsync(string email, string password, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<UserProfile> SignInWithGoogleAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SignOutAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
