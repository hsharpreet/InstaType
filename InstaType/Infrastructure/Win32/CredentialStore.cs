using Windows.Security.Credentials;

namespace InstaType.Infrastructure.Win32;

/// <summary>
/// Thin wrapper around <see cref="PasswordVault"/> (Windows Credential Manager).
/// All sensitive strings (tokens, keys) are stored here — never in files or env vars.
/// </summary>
internal static class CredentialStore
{
    public static void Save(string resource, string username, string secret)
    {
        var vault = new PasswordVault();
        try { vault.Remove(vault.Retrieve(resource, username)); } catch { }
        vault.Add(new PasswordCredential(resource, username, secret));
    }

    public static string? Read(string resource, string username)
    {
        try
        {
            var vault = new PasswordVault();
            var cred  = vault.Retrieve(resource, username);
            cred.RetrievePassword();
            return cred.Password;
        }
        catch { return null; }
    }

    public static void Delete(string resource, string username)
    {
        try
        {
            var vault = new PasswordVault();
            vault.Remove(vault.Retrieve(resource, username));
        }
        catch { }
    }
}
