using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Serilog;
using Wpf.Ui.Controls;

namespace StatisticsAnalysisTool.Common;

/// <summary>
/// Applies a Windows 11 Mica backdrop to a window that uses this app's own custom chrome
/// (WindowStyle="None", AllowsTransparency="False"), and rounds the window's OS-drawn corners
/// to match the app's rounded/soft visual identity.
/// </summary>
public static class WindowBackdropController
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    public static void ApplyMicaBackdrop(Window window)
    {
        if (window is null)
        {
            return;
        }

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            Apply(window);
        }
        else
        {
            window.SourceInitialized += (_, _) => Apply(window);
        }
    }

    private static void Apply(Window window)
    {
        try
        {
            var applied = WindowBackdrop.ApplyBackdrop(window, WindowBackdropType.Mica);
            if (applied)
            {
                WindowBackdrop.RemoveBackground(window);
            }

            SetRoundedCorners(window);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to apply Mica backdrop to {Window}", window.GetType().Name);
        }
    }

    private static void SetRoundedCorners(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var preference = DwmwcpRound;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }
}
