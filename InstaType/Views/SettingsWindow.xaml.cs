using InstaType.ViewModels;

namespace InstaType.Views;

/// <summary>
/// Settings window opened from the system tray icon.
/// Uses WPF-UI FluentWindow with Mica backdrop.
/// All settings bound to <see cref="SettingsViewModel"/>.
/// </summary>
public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
