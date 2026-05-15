using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gr8scale;

public sealed class PickerEntry
{
    public string ExeName { get; init; } = "";       // lowercase filename, e.g. "notepad.exe"
    public string ExePath { get; init; } = "";       // full path
    public string DisplayName { get; init; } = "";   // friendly label
    public ImageSource? Icon { get; init; }
}

public partial class AppPickerWindow : Window
{
    public PickerEntry? Selected { get; private set; }

    private readonly HashSet<string> _excludedExes;

    public AppPickerWindow(IEnumerable<string> excludedExeNames)
    {
        InitializeComponent();
        _excludedExes = new HashSet<string>(
            excludedExeNames.Select(e => e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        Loaded += (_, __) => PopulateList();
    }

    private void PopulateList()
    {
        var ownPid = (uint)Process.GetCurrentProcess().Id;
        var entries = new Dictionary<string, PickerEntry>(StringComparer.OrdinalIgnoreCase);

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!IsCandidateWindow(hwnd)) return true;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0 || pid == ownPid) return true;

            string exePath = ProcessResolver.GetExePathForProcess(pid);
            if (string.IsNullOrEmpty(exePath)) return true;

            string exeName = Path.GetFileName(exePath).ToLowerInvariant();
            if (_excludedExes.Contains(exeName)) return true;
            if (entries.ContainsKey(exeName)) return true; // first-window-wins per exe

            // Friendly display name: prefer FileDescription, fall back to window title, then exe.
            string display = TryGetFileDescription(exePath);
            if (string.IsNullOrWhiteSpace(display)) display = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(display)) display = Path.GetFileNameWithoutExtension(exeName);

            entries[exeName] = new PickerEntry
            {
                ExeName = exeName,
                ExePath = exePath,
                DisplayName = display,
                Icon = IconExtractor.GetIcon(exePath),
            };

            return true;
        }, IntPtr.Zero);

        AppList.ItemsSource = entries.Values
            .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCandidateWindow(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindowVisible(hwnd)) return false;
        if (NativeMethods.GetWindowTextLength(hwnd) == 0) return false;
        if (NativeMethods.GetWindow(hwnd, NativeMethods.GW_OWNER) != IntPtr.Zero) return false;

        long ex = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((ex & NativeMethods.WS_EX_TOOLWINDOW) != 0 && (ex & NativeMethods.WS_EX_APPWINDOW) == 0)
            return false;

        // Filter DWM-cloaked windows (invisible UWP host frames, etc.)
        if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
            && cloaked != 0)
        {
            return false;
        }
        return true;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int len = NativeMethods.GetWindowTextLength(hwnd);
        if (len == 0) return "";
        var sb = new StringBuilder(len + 1);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string TryGetFileDescription(string exePath)
    {
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrWhiteSpace(vi.FileDescription)) return vi.FileDescription;
            if (!string.IsNullOrWhiteSpace(vi.ProductName))     return vi.ProductName;
        }
        catch { }
        return "";
    }

    private void AppRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is PickerEntry entry)
        {
            Selected = entry;
            DialogResult = true;
            Close();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }
}
