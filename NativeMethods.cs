using System;
using System.Runtime.InteropServices;

namespace Gr8scale;

internal static class NativeMethods
{
    public const int SW_RESTORE = 9;
    public const int SW_SHOW = 5;
    public const int SW_HIDE = 0;

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    // Passing (UIntPtr)(-1) for min and max asks Windows to trim the working set
    // of the current process — pages we aren't actively touching get released.
    [DllImport("kernel32.dll")]
    public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMin, IntPtr dwMax);

    public static void TrimWorkingSet()
    {
        SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
    }
}
