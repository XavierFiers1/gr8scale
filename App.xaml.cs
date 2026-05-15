using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Gr8scale;

public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private const string MutexName = "Global\\gr8scale.singleinstance.v1";

    private MainWindow? _mainWindow;
    private TrayIconService? _tray;
    private bool _magInitialized;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            BringExistingInstanceToFront();
            Shutdown();
            return;
        }

        if (!MagnificationInterop.Initialize())
        {
            MessageBox.Show("Failed to initialize the Windows Magnification API.\n\ngr8scale needs this API to apply the grayscale effect.",
                "gr8scale", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        _magInitialized = true;

        var settings = SettingsService.Load();

        _mainWindow = new MainWindow(settings);
        MainWindow = _mainWindow;
        _mainWindow.Show();

        _tray = new TrayIconService(_mainWindow);
        _tray.Show();

        _mainWindow.ApplyInitialEffect();

        // After the UI is up, release startup allocations back to the OS on a
        // background thread (GC.Collect on the UI thread can freeze the window).
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500); // let startup settle
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            NativeMethods.TrimWorkingSet();
        });
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        try { _tray?.Dispose(); } catch { }
        try
        {
            if (_magInitialized)
            {
                MagnificationInterop.SetIdentity();
                MagnificationInterop.Uninitialize();
            }
        }
        catch { }
        try { _instanceMutex?.ReleaseMutex(); } catch { }
        _instanceMutex?.Dispose();
    }

    private static void BringExistingInstanceToFront()
    {
        var current = Process.GetCurrentProcess();
        foreach (var p in Process.GetProcessesByName(current.ProcessName))
        {
            if (p.Id == current.Id) continue;
            if (p.MainWindowHandle != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(p.MainWindowHandle);
            }
        }
    }
}
