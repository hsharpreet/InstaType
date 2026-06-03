using InstaType.Models;
using InstaType.Services;

namespace InstaType.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;

    private string _email        = string.Empty;
    private string _statusText   = string.Empty;
    private bool   _isLoading;
    private bool   _isCreateMode;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsCreateMode
    {
        get => _isCreateMode;
        set
        {
            if (SetProperty(ref _isCreateMode, value))
                OnPropertyChanged(nameof(ToggleModeLabel));
        }
    }

    public string ToggleModeLabel =>
        _isCreateMode ? "Already have an account? Sign in" : "No account? Create one";

    public UserProfile? SignedInUser { get; private set; }

    public LoginViewModel(IAuthService authService)
    {
        _auth = authService;
    }

    public async Task<bool> SignInWithEmailAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            StatusText = "Email and password are required.";
            return false;
        }

        IsLoading  = true;
        StatusText = string.Empty;
        try
        {
            SignedInUser = await _auth.SignInWithEmailAsync(Email.Trim(), password);
            return true;
        }
        catch (Exception ex)
        {
            StatusText = SimplifyError(ex.Message);
            return false;
        }
        finally { IsLoading = false; }
    }

    public async Task<bool> SignInWithGoogleAsync()
    {
        IsLoading  = true;
        StatusText = "Opening browser…";
        try
        {
            SignedInUser = await _auth.SignInWithGoogleAsync();
            return true;
        }
        catch (Exception ex)
        {
            StatusText = SimplifyError(ex.Message);
            return false;
        }
        finally { IsLoading = false; }
    }

    private static string SimplifyError(string message)
    {
        if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
            return "Incorrect email or password.";
        if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
            return "Please confirm your email before signing in.";
        if (message.Contains("anon key", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Credential Manager", StringComparison.OrdinalIgnoreCase))
            return "App not configured. Contact support.";
        return message.Length > 120 ? message[..120] + "…" : message;
    }
}
