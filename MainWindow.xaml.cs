using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    private readonly ForegroundWatcher _watcher;
    private readonly DispatcherTimer _saveDebounce;
    private bool _initializing = true;
    private Border? _filledTrack;
    private Track? _track;

    public ObservableCollection<AppProfile> Profiles { get; }

    private string _currentForegroundExe = "";

    public MainWindow(Settings settings, ForegroundWatcher watcher)
    {
        InitializeComponent();
        _settings = settings;
        _watcher = watcher;

        Profiles = new ObservableCollection<AppProfile>(_settings.AppProfiles);
        foreach (var p in Profiles) p.Icon = IconExtractor.GetIcon(p.ExePath);
        ProfileList.ItemsSource = Profiles;

        IntensitySlider.Value = Math.Clamp(_settings.Intensity, 0, 100);
        AutostartCheck.IsChecked = AutostartService.IsEnabled();

        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounce.Tick += (_, __) =>
        {
            _saveDebounce.Stop();
            // Keep Settings.AppProfiles in sync with the ObservableCollection
            _settings.AppProfiles = Profiles.ToList();
            SettingsService.Save(_settings);
        };

        _watcher.ForegroundAppChanged += OnForegroundAppChanged;

        Loaded += MainWindow_Loaded;
        _initializing = false;
    }

    public void ApplyInitialEffect()
    {
        // Probe the current foreground so the initial intensity matches whatever's already focused.
        var fg = NativeMethods.GetForegroundWindow();
        if (fg != IntPtr.Zero)
        {
            NativeMethods.GetWindowThreadProcessId(fg, out uint pid);
            uint ownPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            if (pid != 0 && pid != ownPid)
            {
                _currentForegroundExe = ProcessResolver.GetExeNameForProcess(pid);
            }
        }

        double v = EffectiveIntensityFor(_currentForegroundExe);
        MagnificationInterop.SetIntensity(v / 100.0);
        UpdatePercentLabel(IntensitySlider.Value);
        UpdateFilledTrack();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _filledTrack = FindVisualChild<Border>(IntensitySlider, "PART_FilledTrack");
        _track = FindVisualChild<Track>(IntensitySlider, "PART_Track");
        UpdateFilledTrack();
    }

    // --- Foreground tracking ----------------------------------------------

    private void OnForegroundAppChanged(string exeName)
    {
        _currentForegroundExe = exeName;
        double v = EffectiveIntensityFor(exeName);
        MagnificationInterop.SetIntensity(v / 100.0);
    }

    private double EffectiveIntensityFor(string exeName)
    {
        if (!string.IsNullOrEmpty(exeName))
        {
            var p = FindProfile(exeName);
            if (p != null) return p.Intensity;
        }
        return _settings.Intensity;
    }

    private AppProfile? FindProfile(string exeName)
        => Profiles.FirstOrDefault(p => string.Equals(p.ExeName, exeName, StringComparison.OrdinalIgnoreCase));

    // --- Default slider ----------------------------------------------------

    private void DefaultSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;

        double v = e.NewValue;
        _settings.Intensity = v;
        // While the user is dragging the default slider, show its effect immediately —
        // even if some other app is currently foreground.
        MagnificationInterop.SetIntensity(v / 100.0);
        UpdatePercentLabel(v);
        UpdateFilledTrack();

        QueueSave();
    }

    // --- Per-profile sliders ----------------------------------------------

    private void ProfileSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;
        if (sender is not Slider s || s.Tag is not AppProfile profile) return;

        // Slider binding already updated profile.Intensity. Apply live (WYSIWYG).
        MagnificationInterop.SetIntensity(profile.Intensity / 100.0);
        QueueSave();
    }

    // --- Profile add/remove -----------------------------------------------

    private void AddApp_Click(object sender, RoutedEventArgs e)
    {
        var picker = new AppPickerWindow(Profiles.Select(p => p.ExeName)) { Owner = this };
        if (picker.ShowDialog() == true && picker.Selected is { } entry)
        {
            var p = new AppProfile
            {
                ExeName = entry.ExeName,
                ExePath = entry.ExePath,
                DisplayName = entry.DisplayName,
                Intensity = IntensitySlider.Value, // seed from default slider
                Icon = entry.Icon,
            };
            Profiles.Add(p);
            QueueSave();
        }
    }

    private void RemoveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is AppProfile p)
        {
            Profiles.Remove(p);
            // If we removed the profile of the foreground app, fall back to default.
            if (string.Equals(p.ExeName, _currentForegroundExe, StringComparison.OrdinalIgnoreCase))
            {
                MagnificationInterop.SetIntensity(_settings.Intensity / 100.0);
            }
            QueueSave();
        }
    }

    // --- Misc plumbing ----------------------------------------------------

    private void QueueSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void UpdatePercentLabel(double v) => PercentLabel.Text = $"{(int)Math.Round(v)}%";

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
