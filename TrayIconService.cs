using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using H.NotifyIcon;

namespace Gr8scale;

internal sealed class TrayIconService : IDisposable
{
    private readonly MainWindow _window;
    private readonly TaskbarIcon _icon;

    public TrayIconService(MainWindow window)
    {
        _window = window;

        var menu = new ContextMenu();
        var showItem = new MenuItem { Header = "Show" };
        showItem.Click += (_, __) => ShowWindow();
        menu.Items.Add(showItem);
        menu.Items.Add(new Separator());
        var quitItem = new MenuItem { Header = "Quit" };
        quitItem.Click += (_, __) => Application.Current.Shutdown();
        menu.Items.Add(quitItem);

        _icon = new TaskbarIcon
        {
            IconSource = LoadIconSource(),
            ToolTipText = "gr8scale",
            ContextMenu = menu,
            Visibility = Visibility.Visible,
        };
        _icon.TrayMouseDoubleClick += (_, __) => ShowWindow();
        // Force the underlying Win32 NOTIFYICONDATA registration immediately;
        // without this, an icon created in code may never appear in the shell tray.
        _icon.ForceCreate();
    }

    private static ImageSource? LoadIconSource()
    {
        try
        {
            // pack:// URI is what H.NotifyIcon needs to convert to an HICON internally.
            // Force synchronous load so the bytes are in memory before H.NotifyIcon
            // tries to read them on its background icon-encoding task.
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch
        {
            return null;
        }
    }

    public void Show() => _icon.Visibility = Visibility.Visible;

    private void ShowWindow()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
        _window.Focus();
    }

    public void Dispose()
    {
        try { _icon.Visibility = Visibility.Collapsed; } catch { }
        _icon.Dispose();
    }
}
