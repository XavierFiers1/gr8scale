using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Application = System.Windows.Application;
using WindowState = System.Windows.WindowState;
using WinForms = System.Windows.Forms;

namespace Gr8scale;

internal sealed class TrayIconService : IDisposable
{
    private readonly MainWindow _window;
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly WinForms.ContextMenuStrip _menu;

    public TrayIconService(MainWindow window)
    {
        _window = window;
        _menu = new WinForms.ContextMenuStrip();
        var show = _menu.Items.Add("Show");
        show.Click += (_, __) => ShowWindow();
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        var quit = _menu.Items.Add("Quit");
        quit.Click += (_, __) => Application.Current.Shutdown();

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "gr8scale",
            Visible = false,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.DoubleClick += (_, __) => ShowWindow();
    }

    private static Icon LoadIcon()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gr8scale.icon.ico");
            if (stream != null) return new Icon(stream);
        }
        catch { }
        return SystemIcons.Application;
    }

    public void Show() => _notifyIcon.Visible = true;

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
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
