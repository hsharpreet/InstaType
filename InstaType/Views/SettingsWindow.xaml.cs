using InstaType.Models;
using InstaType.Services;
using InstaType.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace InstaType.Views;

/// <summary>
/// Settings window opened from the system tray icon.
/// Uses WPF-UI FluentWindow with Mica backdrop.
/// All settings bound to <see cref="SettingsViewModel"/>.
/// </summary>
public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly IAudioCaptureService _audio;
    private readonly IHistoryService      _history;

    public SettingsWindow(SettingsViewModel viewModel,
                          IAudioCaptureService audioCaptureService,
                          IHistoryService historyService)
    {
        InitializeComponent();
        DataContext = viewModel;

        _audio   = audioCaptureService;
        _history = historyService;

        // Version label
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = $"InstaType v{ver?.Major}.{ver?.Minor}.{ver?.Build}";

        // Populate microphone list
        var mics = _audio.GetAvailableMicrophones();
        MicrophoneCombo.ItemsSource       = mics;
        MicrophoneCombo.DisplayMemberPath = nameof(MicrophoneDevice.Name);
        if (mics.Count > 0) MicrophoneCombo.SelectedIndex = 0;
        MicrophoneCombo.SelectionChanged += MicrophoneCombo_SelectionChanged;
    }

    private void MicrophoneCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MicrophoneCombo.SelectedItem is MicrophoneDevice mic)
            _audio.SetDevice(mic.Id);
    }

    private async void ClearHistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Delete all transcription history?",
            "InstaType",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _history.ClearAsync();
            System.Windows.MessageBox.Show("History cleared.", "InstaType",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
