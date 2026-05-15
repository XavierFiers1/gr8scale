using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Gr8scale;

internal static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "gr8scale";

    private static string ExecutablePath
    {
        get
        {
            // For published single-file exe, MainModule.FileName is the exe path.
            var p = Process.GetCurrentProcess().MainModule?.FileName;
            return string.IsNullOrEmpty(p) ? Environment.ProcessPath ?? "" : p;
        }
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            var v = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(v);
        }
        catch { return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (key == null) return;
            if (enabled)
            {
                var path = ExecutablePath;
                if (!string.IsNullOrEmpty(path))
                    key.SetValue(ValueName, $"\"{path}\"");
            }
            else
            {
                if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName);
            }
        }
        catch { }
    }
}
