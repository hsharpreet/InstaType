using InstaType.ViewModels;

namespace InstaType.Views;

/// <summary>
/// Transcription history browser, opened from the system tray menu.
/// Core+ shows persistent searchable history; Free shows last 10 in-memory entries.
/// </summary>
public partial class HistoryWindow : Wpf.Ui.Controls.FluentWindow
{
    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
