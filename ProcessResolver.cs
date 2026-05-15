using System;
using System.IO;
using System.Text;

namespace Gr8scale;

internal static class ProcessResolver
{
    /// <summary>
    /// HWND → exe filename (just the filename, lower-cased) like "notepad.exe". Empty string on failure.
    /// </summary>
    public static string GetExeNameForWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return "";

        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return "";
            return GetExeNameForProcess(pid);
        }
        catch
        {
            return "";
        }
    }

    public static string GetExeNameForProcess(uint pid)
    {
        var path = GetExePathForProcess(pid);
        return string.IsNullOrEmpty(path) ? "" : Path.GetFileName(path).ToLowerInvariant();
    }

    public static string GetExePathForProcess(uint pid)
    {
        if (pid == 0) return "";

        IntPtr handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) return "";

        try
        {
            var sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;
            if (NativeMethods.QueryFullProcessImageNameW(handle, 0, sb, ref size))
            {
                return sb.ToString(0, (int)size);
            }
        }
        catch { }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
        return "";
    }
}
