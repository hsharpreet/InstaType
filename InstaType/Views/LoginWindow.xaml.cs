using InstaType.ViewModels;
using System.Windows;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKey = System.Windows.Input.Key;

namespace InstaType.Views;

public partial class LoginWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _vm         = viewModel;
        DataContext = _vm;
    }

    private async void SignInBtn_Click(object sender, RoutedEventArgs e)
        => await AttemptSignInAsync();

    private async void GoogleBtn_Click(object sender, RoutedEventArgs e)
    {
        SetLoading(true);
        bool ok = await _vm.SignInWithGoogleAsync();
        SetLoading(false);
        if (ok) Close();
    }

    private async void Input_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.Return) await AttemptSignInAsync();
    }

    private void ToggleModeBtn_Click(object sender, RoutedEventArgs e)
    {
        _vm.IsCreateMode     = !_vm.IsCreateMode;
        SubtitleText.Text    = _vm.IsCreateMode ? "Create your account" : "Sign in to your account";
        SignInBtn.Content    = _vm.IsCreateMode ? "Create Account" : "Sign In";
    }

    private async Task AttemptSignInAsync()
    {
        SetLoading(true);
        bool ok = await _vm.SignInWithEmailAsync(PasswordBox.Password);
        SetLoading(false);
        if (ok) Close();
    }

    private void SetLoading(bool loading)
    {
        SignInBtn.IsEnabled    = !loading;
        GoogleBtn.IsEnabled    = !loading;
        LoadingRing.Visibility = loading ? Visibility.Visible  : Visibility.Collapsed;
        StatusBlock.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
    }
}
