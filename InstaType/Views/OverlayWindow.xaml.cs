using InstaType.Models;
using InstaType.Services;
using InstaType.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace InstaType.Views;

/// <summary>
/// The floating pill overlay shown during recording and transcription.
/// Transparent, always-on-top, no taskbar entry.
/// Bar heights driven by data-bound OverlayViewModel.Bar1Scale…Bar5Scale.
/// Gear button opens a full settings popup (microphone, behaviour, appearance, data, app).
/// Pill border pulses green while the microphone is active.
/// Left-click-drag repositions the overlay; position is saved to settings.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel  _viewModel;
    private readonly ISettingsService  _settings;
    private readonly IHistoryService   _history;
    private readonly IServiceProvider  _services;

    // Glow animation state
    private SolidColorBrush _glowBrush = null!;
    private Storyboard?     _glowStoryboard;

    // Guards against recursive checkbox events while we initialise the popup
    private bool _suppressSettingsEvents;

    public OverlayWindow(
        OverlayViewModel viewModel,
        ISettingsService settingsService,
        IHistoryService  historyService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _settings  = settingsService;
        _history   = historyService;
        _services  = serviceProvider;

        DataContext = viewModel;

        // Prepare the animated glow border brush.
        _glowBrush = new SolidColorBrush(Colors.Transparent);
        PillBorder.BorderBrush = _glowBrush;

        // Version label in popup.
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        PopupVersionLabel.Text = $"InstaType v{ver?.Major}.{ver?.Minor}.{ver?.Build}";

        // Subscribe to IsListening changes to start/stop glow.
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    // ── Drag to reposition ───────────────────────────────────────────────────

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount != 1) return;

        // Walk up the visual tree — skip drag if the click hit a button or combobox,
        // so GearButton, MuteButton and popup controls receive their Click events.
        DependencyObject? hit = e.OriginalSource as DependencyObject;
        while (hit is not null)
        {
            if (hit is System.Windows.Controls.Primitives.ButtonBase or
                System.Windows.Controls.ComboBox)
                return;
            hit = VisualTreeHelper.GetParent(hit);
        }

        DragMove();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        _viewModel.SavePosition(Left, Top);
    }

    // ── Glow animation ───────────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.IsListening)) return;

        if (_viewModel.IsListening) StartGlowAnimation();
        else StopGlowAnimation();
    }

    private void StartGlowAnimation()
    {
        _glowStoryboard?.Stop();

        var anim = new ColorAnimation
        {
            From           = System.Windows.Media.Color.FromArgb(0,   0, 255, 136),
            To             = System.Windows.Media.Color.FromArgb(150, 0, 255, 136),
            Duration       = new Duration(TimeSpan.FromSeconds(0.8)),
            AutoReverse    = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        _glowStoryboard = new Storyboard();
        _glowStoryboard.Children.Add(anim);
        Storyboard.SetTarget(anim, _glowBrush);
        Storyboard.SetTargetProperty(anim, new PropertyPath(SolidColorBrush.ColorProperty));
        _glowStoryboard.Begin();
    }

    private void StopGlowAnimation()
    {
        _glowStoryboard?.Stop();
        _glowBrush.Color = Colors.Transparent;
    }

    // ── Mute button ──────────────────────────────────────────────────────────

    private void MuteButton_Click(object sender, RoutedEventArgs e)
        => _viewModel.ToggleMute();

    // ── Gear button — opens settings popup ───────────────────────────────────

    private void GearButton_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsPopup.IsOpen) { SettingsPopup.IsOpen = false; return; }

        PopulateSettingsPopup();
        SettingsPopup.IsOpen = true;
    }

    private void PopulateSettingsPopup()
    {
        _suppressSettingsEvents = true;
        try
        {
            // Microphone list
            var mics = _viewModel.GetAvailableMicrophones();
            SettingsMicCombo.ItemsSource       = mics;
            SettingsMicCombo.DisplayMemberPath = nameof(MicrophoneDevice.Name);
            int savedId = _settings.Current.SelectedMicDeviceId;
            var selected = mics.FirstOrDefault(m => m.Id == savedId) ?? mics.FirstOrDefault();
            if (selected != null)
                SettingsMicCombo.SelectedItem = selected;

            // Behaviour toggles
            AlwaysOnTopCheck.IsChecked       = _settings.Current.AlwaysOnTop;
            StartWithWindowsCheck.IsChecked  = _settings.Current.LaunchAtStartup;
            SaveHistoryCheck.IsChecked       = _settings.Current.SaveHistory;

            // Theme radio
            string theme = _settings.Current.ThemeOverride ?? "System";
            ThemeSystemRadio.IsChecked = theme == "System";
            ThemeLightRadio.IsChecked  = theme == "Light";
            ThemeDarkRadio.IsChecked   = theme == "Dark";
        }
        finally
        {
            _suppressSettingsEvents = false;
        }
    }

    // ── Microphone ───────────────────────────────────────────────────────────

    private void SettingsMicCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        if (SettingsMicCombo.SelectedItem is not MicrophoneDevice mic) return;

        _viewModel.SelectMicrophone(mic.Id);
        _settings.Current.SelectedMicDeviceId = mic.Id;
        _ = _settings.SaveAsync();
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    private void AlwaysOnTopCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        bool val = AlwaysOnTopCheck.IsChecked == true;
        Topmost = val;
        _settings.Current.AlwaysOnTop = val;
        _ = _settings.SaveAsync();
    }

    private void StartWithWindowsCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        bool val = StartWithWindowsCheck.IsChecked == true;
        SetStartWithWindows(val);
        _settings.Current.LaunchAtStartup = val;
        _ = _settings.SaveAsync();
    }

    private void SaveHistoryCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents) return;
        bool val = SaveHistoryCheck.IsChecked == true;
        _settings.Current.SaveHistory = val;
        _ = _settings.SaveAsync();
    }

    private static void SetStartWithWindows(bool enable)
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        try
        {
            ProcessStartInfo psi;
            if (enable)
            {
                psi = new ProcessStartInfo("schtasks.exe",
                    $"/Create /TN \"InstaType\" /TR \"\"{exePath}\"\" /SC ONLOGON /RL LIMITED /F")
                {
                    CreateNoWindow  = true,
                    UseShellExecute = false
                };
            }
            else
            {
                psi = new ProcessStartInfo("schtasks.exe", "/Delete /TN \"InstaType\" /F")
                {
                    CreateNoWindow  = true,
                    UseShellExecute = false
                };
            }
            Process.Start(psi)?.WaitForExit(3000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartWithWindows] {ex.Message}");
        }
    }

    // ── Appearance ────────────────────────────────────────────────────────────

    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingsEvents) return;

        string theme = "System";
        if (ThemeLightRadio.IsChecked == true)  theme = "Light";
        else if (ThemeDarkRadio.IsChecked == true) theme = "Dark";

        App.ApplyTheme(theme);

        _settings.Current.ThemeOverride = theme == "System" ? null : theme;
        _ = _settings.SaveAsync();
    }

    // ── Data ─────────────────────────────────────────────────────────────────

    private async void ClearHistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;
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

    private void ViewHistoryBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;
        var win = _services.GetService(typeof(HistoryWindow)) as HistoryWindow
                  ?? throw new InvalidOperationException("HistoryWindow not registered.");
        win.Show();
    }

    // ── App ──────────────────────────────────────────────────────────────────

    private void HelpBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;
        Process.Start(new ProcessStartInfo("https://github.com/hsharpreet/InstaType/issues")
        {
            UseShellExecute = true
        });
    }

    private void ExitBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;
        System.Windows.Application.Current.Shutdown();
    }

}
