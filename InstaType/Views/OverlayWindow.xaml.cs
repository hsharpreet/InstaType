using InstaType.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace InstaType.Views;

/// <summary>
/// The floating pill overlay shown during recording and transcription.
/// Transparent, always-on-top, no taskbar entry. Positioned top-centre at startup.
/// Starts/stops the waveform Storyboard in response to OverlayViewModel.IsListening.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _viewModel;
    private readonly Storyboard _waveAnim;

    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _waveAnim = (Storyboard)Resources["WaveAnim"];
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.IsListening)) return;

        if (_viewModel.IsListening)
            _waveAnim.Begin(this, isControllable: true);
        else
            _waveAnim.Stop(this);
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
        => _viewModel.ToggleMute();
}
