using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Gr8scale;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly DispatcherTimer _saveDebounce;
    private bool _initializing = true;
    private Border? _filledTrack;
    private Track? _track;

    public MainWindow(Settings settings)
    {
        InitializeComponent();
        _settings = settings;

        IntensitySlider.Value = Math.Clamp(_settings.Intensity, 0, 100);
        AutostartCheck.IsChecked = AutostartService.IsEnabled();

        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounce.Tick += (_, __) =>
        {
            _saveDebounce.Stop();
            SettingsService.Save(_settings);
        };

        Loaded += MainWindow_Loaded;
        _initializing = false;
    }

    public void ApplyInitialEffect()
    {
        MagnificationInterop.SetIntensity(IntensitySlider.Value / 100.0);
        UpdatePercentLabel(IntensitySlider.Value);
        UpdateFilledTrack();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Find PART_FilledTrack so we can manually update its width
        _filledTrack = FindVisualChild<Border>(IntensitySlider, "PART_FilledTrack");
        _track = FindVisualChild<Track>(IntensitySlider, "PART_Track");
        UpdateFilledTrack();
    }

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;

        double v = e.NewValue;
        MagnificationInterop.SetIntensity(v / 100.0);
        UpdatePercentLabel(v);
        UpdateFilledTrack();

        _settings.Intensity = v;
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void UpdatePercentLabel(double v)
    {
        PercentLabel.Text = $"{(int)Math.Round(v)}%";
    }

    private void UpdateFilledTrack()
    {
        if (_filledTrack == null || _track == null) return;
        double trackWidth = _track.ActualWidth;
        if (trackWidth <= 0) return;
        double pct = (IntensitySlider.Value - IntensitySlider.Minimum) /
                     (IntensitySlider.Maximum - IntensitySlider.Minimum);
        _filledTrack.Width = Math.Max(0, trackWidth * pct);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateFilledTrack();
    }

    private void Autostart_Checked(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        AutostartService.SetEnabled(true);
    }

    private void Autostart_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        AutostartService.SetEnabled(false);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => Hide();
    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(CancelEventArgs e)
    {
        // X already routes to Hide() via Close_Click; this is the system close fallback.
        e.Cancel = true;
        Hide();
    }

    // --- Visual tree helper ---
    private static T? FindVisualChild<T>(DependencyObject parent, string? name = null) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t && (name == null || (child is FrameworkElement fe && fe.Name == name)))
                return t;
            var deeper = FindVisualChild<T>(child, name);
            if (deeper != null) return deeper;
        }
        return null;
    }
}
