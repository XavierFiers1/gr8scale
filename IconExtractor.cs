using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gr8scale;

internal static class IconExtractor
{
    /// <summary>
    /// Extract a small icon for the given exe path as a frozen WPF ImageSource.
    /// Returns null on failure (e.g. file inaccessible, exotic exe with no icon resources).
    /// </summary>
    public static ImageSource? GetIcon(string exePath)
    {
        if (string.IsNullOrEmpty(exePath)) return null;

        var sfi = new NativeMethods.SHFILEINFOW();
        uint flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON;

        if (!File.Exists(exePath))
        {
            // SHGFI_USEFILEATTRIBUTES lets us still get the generic exe icon
            flags |= NativeMethods.SHGFI_USEFILEATTRIBUTES;
        }

        IntPtr res = NativeMethods.SHGetFileInfoW(
            exePath, NativeMethods.FILE_ATTRIBUTE_NORMAL, ref sfi,
            (uint)System.Runtime.InteropServices.Marshal.SizeOf(sfi), flags);

        if (res == IntPtr.Zero || sfi.hIcon == IntPtr.Zero) return null;

        try
        {
            var src = Imaging.CreateBitmapSourceFromHIcon(
                sfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        catch
        {
            return null;
        }
        finally
        {
            NativeMethods.DestroyIcon(sfi.hIcon);
        }
    }
}
