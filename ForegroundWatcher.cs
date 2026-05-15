using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Gr8scale;

/// <summary>
/// Watches the desktop foreground window and raises <see cref="ForegroundAppChanged"/> on the UI
/// dispatcher whenever the foreground app's exe changes. Events caused by gr8scale itself are filtered out.
/// </summary>
public sealed class ForegroundWatcher : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly uint _ownPid;

    // Held as a strong field so the GC can't collect the delegate while the hook is live —
    // this is the #1 cause of SetWinEventHook crashes.
    private NativeMethods.WinEventDelegate? _callback;
    private IntPtr _hookHandle = IntPtr.Zero;
    private string _lastExeName = "";

    public event Action<string>? ForegroundAppChanged;

    public string CurrentExeName => _lastExeName;

    public ForegroundWatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _ownPid = (uint)Process.GetCurrentProcess().Id;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero) return;

        _callback = OnWinEvent;
        _hookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _callback,
            idProcess: 0,
            idThread: 0,
            dwFlags: NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;

        // Resolve PID/exe outside the dispatcher (cheap, doesn't touch UI).
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0 || pid == _ownPid) return;

        string exe = ProcessResolver.GetExeNameForProcess(pid);
        if (string.IsNullOrEmpty(exe)) return;
        if (exe == _lastExeName) return;

        _lastExeName = exe;
        _dispatcher.BeginInvoke(new Action(() => ForegroundAppChanged?.Invoke(exe)));
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        _callback = null;
    }
}
