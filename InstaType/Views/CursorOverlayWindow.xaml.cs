using InstaType.ViewModels;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace InstaType.Views;

/// <summary>
/// A 36×36 transparent always-on-top window that follows the cursor while recording.
/// Shows a pulsing 🎤 ring to signal that InstaType is listening.
/// Appears on IsListening = true, disappears on false.
/// IsHitTestVisible = false so it never captures mouse events.
/// </summary>
public partial class CursorOverlayWindow : Window
{
    private readonly OverlayViewModel _viewModel;
    private readonly DispatcherTimer  _positionTimer;
    private Storyboard? _pulseStoryboard;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    public CursorOverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _positionTimer.Tick += OnPositionTick;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.IsListening))
        {
            if (_viewModel.IsListening)
            {
                UpdateRingColor();
                UpdatePosition();
                Show();
                _positionTimer.Start();
                StartPulse();
            }
            else
            {
                _positionTimer.Stop();
                StopPulse();
                Hide();
            }
        }
        else if (e.PropertyName == nameof(OverlayViewModel.IsMuted))
        {
            UpdateRingColor();
        }
    }

    private void UpdateRingColor()
    {
        string brushKey = _viewModel.IsMuted ? "MutedBrush" : "ActiveListenBrush";
        if (System.Windows.Application.Current?.Resources[brushKey] is System.Windows.Media.Brush b)
            PulseRing.Stroke = b;
    }

    private void OnPositionTick(object? sender, EventArgs e) => UpdatePosition();

    private void UpdatePosition()
    {
        if (!GetCursorPos(out var pt)) return;
        Left = pt.X + 16;
        Top  = pt.Y + 16;
    }

    private void StartPulse()
    {
        _pulseStoryboard?.Stop();

        var scaleAnim = new DoubleAnimation
        {
            From           = 0.9,
            To             = 1.15,
            Duration       = new Duration(TimeSpan.FromSeconds(0.7)),
            AutoReverse    = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        _pulseStoryboard = new Storyboard();
        _pulseStoryboard.Children.Add(scaleAnim);
        Storyboard.SetTarget(scaleAnim, PulseScale);
        Storyboard.SetTargetProperty(scaleAnim, new PropertyPath("ScaleX"));

        var scaleAnimY = scaleAnim.Clone();
        Storyboard.SetTarget(scaleAnimY, PulseScale);
        Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("ScaleY"));
        _pulseStoryboard.Children.Add(scaleAnimY);

        _pulseStoryboard.Begin();
    }

    private void StopPulse()
    {
        _pulseStoryboard?.Stop();
        PulseScale.ScaleX = 1;
        PulseScale.ScaleY = 1;
    }
}
