using InstaType.ViewModels;
using Wpf.Ui;

namespace InstaType.Views;

/// <summary>
/// The floating glassmorphism overlay shown during recording and transcription.
/// Uses WPF-UI FluentWindow with Mica backdrop (no AllowsTransparency).
/// Does not steal focus from the target application.
/// Positioned bottom-center above the taskbar.
/// </summary>
public partial class OverlayWindow : Wpf.Ui.Controls.FluentWindow
{
    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
