using InstaType.ViewModels;
using System.Windows;

namespace InstaType.Views;

/// <summary>
/// Transcription history browser, opened from the system tray menu or gear popup.
/// Loads entries newest-first on open; supports keyword search and clear-all.
/// </summary>
public partial class HistoryWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly HistoryViewModel _vm;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _vm         = viewModel;
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();

    private async void ClearAllBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Delete all transcription history?",
            "InstaType",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            await _vm.ClearAllAsync();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
        => Close();
}
