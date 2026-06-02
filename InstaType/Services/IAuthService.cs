using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Wraps Supabase Auth for sign-in, sign-out, and session management.
/// Supports email/password and Google OAuth.
/// Auth tokens are stored in Windows Credential Manager, never in files.
/// </summary>
public interface IAuthService
{
    /// <summary>The currently authenticated user, or null if signed out.</summary>
    UserProfile? CurrentUser { get; }

    /// <summary>Whether a user is currently signed in.</summary>
    bool IsSignedIn { get; }

    /// <summary>Raised when sign-in state changes.</summary>
    event EventHandler<UserProfile?>? AuthStateChanged;

    /// <summary>Signs in with email and password.</summary>
    Task<UserProfile> SignInWithEmailAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates Google OAuth sign-in. Opens the system browser.
    /// Returns after the redirect URI is handled.
    /// </summary>
    Task<UserProfile> SignInWithGoogleAsync(CancellationToken cancellationToken = default);

    /// <summary>Signs out, clears the Windows Credential Manager entry, and resets to Free tier.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>Restores a saved session from Windows Credential Manager (called at app startup).</summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
}
