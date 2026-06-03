using InstaType.Infrastructure.Win32;
using InstaType.Models;
using InstaType.Services;
using System.Net;
using System.Text.Json;

// Aliases to avoid collision with our own namespace (InstaType.Infrastructure.Supabase)
using SupabaseClient  = global::Supabase.Client;
using SupabaseOptions = global::Supabase.SupabaseOptions;
using GotrueSession   = global::Supabase.Gotrue.Session;
using GotrueUser      = global::Supabase.Gotrue.User;
using Provider        = global::Supabase.Gotrue.Constants.Provider;
using OAuthFlowType   = global::Supabase.Gotrue.Constants.OAuthFlowType;
using SignInOptions   = global::Supabase.Gotrue.SignInOptions;
using ProviderState   = global::Supabase.Gotrue.ProviderAuthState;

namespace InstaType.Infrastructure.Supabase;

/// <summary>
/// Implements <see cref="IAuthService"/> using the Supabase C# SDK.
/// Supports email/password and Google OAuth (PKCE, localhost redirect).
/// JWTs are persisted in Windows Credential Manager — never on disk.
/// </summary>
internal sealed class AuthService : IAuthService
{
    // ── Credential Manager keys ────────────────────────────────────────────
    private const string AnonKeyResource  = "InstaType/Supabase";
    private const string AnonKeyUsername  = "anonkey";

    private const string SupabaseUrl      = "https://mdhjikkzpcqsipfjkvqj.supabase.co";
    private const string OAuthRedirectUri = "http://localhost:54321/callback";

    // ── State ───────────────────────────────────────────────────────────────
    private SupabaseClient? _client;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public UserProfile?  CurrentUser { get; private set; }
    public bool          IsSignedIn  => CurrentUser is not null;
    public event EventHandler<UserProfile?>? AuthStateChanged;

    // ── IAuthService ────────────────────────────────────────────────────────

    public async Task<UserProfile> SignInWithEmailAsync(
        string email, string password,
        CancellationToken cancellationToken = default)
    {
        var client  = await GetClientAsync(cancellationToken);
        var session = await client.Auth.SignIn(email, password);
        return await BuildProfileAsync(client, session
            ?? throw new InvalidOperationException("Sign-in returned no session."),
            cancellationToken);
    }

    public async Task<UserProfile> SignInWithGoogleAsync(
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);

        ProviderState state = await client.Auth.SignIn(
            Provider.Google,
            new SignInOptions
            {
                RedirectTo = OAuthRedirectUri,
                FlowType   = OAuthFlowType.PKCE
            });

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = state.Uri?.ToString()
                              ?? throw new InvalidOperationException("No OAuth URI returned."),
            UseShellExecute = true
        });

        string code = await WaitForOAuthCodeAsync(OAuthRedirectUri, cancellationToken);

        var session = await client.Auth.ExchangeCodeForSession(
            state.PKCEVerifier ?? throw new InvalidOperationException("No PKCE verifier."),
            code);

        return await BuildProfileAsync(client, session
            ?? throw new InvalidOperationException("OAuth code exchange returned no session."),
            cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        if (_client is not null)
            try { await _client.Auth.SignOut(); } catch { }

        CurrentUser = null;
        AuthStateChanged?.Invoke(this, null);
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken);
            client.Auth.LoadSession();
            var session = client.Auth.CurrentSession;
            if (session?.User is null) return false;
            CurrentUser = await BuildProfileAsync(client, session, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth] TryRestore failed: {ex.Message}");
            return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<SupabaseClient> GetClientAsync(CancellationToken ct)
    {
        if (_initialized && _client is not null) return _client;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized && _client is not null) return _client;

            string anonKey = CredentialStore.Read(AnonKeyResource, AnonKeyUsername)
                ?? throw new InvalidOperationException(
                    "Supabase anon key not found. Store it with resource='InstaType/Supabase', username='anonkey'.");

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false,
                SessionHandler      = new CredentialSessionHandler()
            };

            _client = new SupabaseClient(SupabaseUrl, anonKey, options);
            await _client.InitializeAsync();
            _initialized = true;
            return _client;
        }
        finally { _initLock.Release(); }
    }

    private async Task<UserProfile> BuildProfileAsync(
        SupabaseClient client, GotrueSession session,
        CancellationToken ct)
    {
        var user = session.User ?? throw new InvalidOperationException("Session has no user.");

        SubscriptionTier tier      = SubscriptionTier.Free;
        DateTimeOffset?  expiresAt = null;

        try
        {
            var row = await client.From<SupabaseProfile>()
                .Where(p => p.Id == user.Id)
                .Single();

            if (row is not null)
            {
                tier = row.Tier switch
                {
                    "core"  => SubscriptionTier.Core,
                    "aipro" => SubscriptionTier.AIPro,
                    _       => SubscriptionTier.Free
                };
                expiresAt = row.SubscriptionExpiresAt.HasValue
                    ? new DateTimeOffset(row.SubscriptionExpiresAt.Value, TimeSpan.Zero)
                    : null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth] Profile fetch failed: {ex.Message}");
        }

        var profile = new UserProfile
        {
            UserId                = user.Id ?? string.Empty,
            Email                 = user.Email ?? string.Empty,
            DisplayName           = user.UserMetadata?.GetValueOrDefault("full_name")?.ToString(),
            Tier                  = tier,
            TierVerifiedAt        = DateTimeOffset.UtcNow,
            SubscriptionExpiresAt = expiresAt,
        };

        CurrentUser = profile;
        AuthStateChanged?.Invoke(this, profile);
        return profile;
    }

    // ── OAuth localhost redirect listener ────────────────────────────────

    private static async Task<string> WaitForOAuthCodeAsync(
        string redirectUri, CancellationToken ct)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri.TrimEnd('/') + "/");
        listener.Start();
        ct.Register(listener.Stop);

        HttpListenerContext ctx;
        try   { ctx = await listener.GetContextAsync(); }
        catch { throw new OperationCanceledException("OAuth listener cancelled.", ct); }

        using var response = ctx.Response;
        const string html =
            "<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
            "<h2>&#10003; Signed in to InstaType</h2><p>You can close this tab.</p></body></html>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        response.ContentType    = "text/html";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, ct);

        return ctx.Request.QueryString["code"]
            ?? throw new InvalidOperationException("OAuth callback did not include 'code'.");
    }

    // ── Session persistence ────────────────────────────────────────────────

    private sealed class CredentialSessionHandler
        : global::Supabase.Gotrue.Interfaces.IGotrueSessionPersistence<GotrueSession>
    {
        private const string Resource = "InstaType/Auth";
        private const string Username = "session";

        public void SaveSession(GotrueSession session)
        {
            try
            {
                CredentialStore.Save(Resource, Username, JsonSerializer.Serialize(session));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Auth] SaveSession failed: {ex.Message}");
            }
        }

        public void DestroySession() => CredentialStore.Delete(Resource, Username);

        public GotrueSession? LoadSession()
        {
            try
            {
                var json = CredentialStore.Read(Resource, Username);
                return json is not null
                    ? JsonSerializer.Deserialize<GotrueSession>(json)
                    : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Auth] LoadSession failed: {ex.Message}");
                return null;
            }
        }
    }
}
