using Dalamud.Utility;
using PInvoke;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GagSpeak.CkCommons.Intiface;

public static class Intiface
{
    // the path to intiface central.exe
    public static string AppPath = string.Empty;

    /// <summary> Gets the application running path for Intiface Central.exe if installed.</summary>
    public static void GetApplicationPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AppPath = Path.Combine(appData, "IntifaceCentral", "intiface_central.exe");
            return;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Adjust the path according to where the application resides on macOS
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            AppPath = Path.Combine(homePath, "Applications", "IntifaceCentral", "intiface_central.app");
            return;
        }
    }

    public static void OpenIntiface(ILogger logger, bool pushToForeground)
    {
        // search for the intiface celtral window
        var windowHandle = FindWindowByRegex(logger, @"Intiface\u00AE Central*");
        // if it's present, place it to the foreground
        if (windowHandle != IntPtr.Zero)
        {
            if (pushToForeground)
            {
                logger.LogDebug("Intiface Central found, bringing to foreground.", LoggerType.ToyboxDevices);
                User32.ShowWindow(windowHandle, User32.WindowShowStyle.SW_RESTORE);
                User32.SetForegroundWindow(windowHandle);
            }
        }
        // otherwise, start the process to open intiface central
        else if (!string.IsNullOrEmpty(AppPath) && File.Exists(AppPath))
        {
            logger.LogInformation("Starting Intiface Central", LoggerType.ToyboxDevices);
            Process.Start(AppPath);
        }
        // or just open the installer if it doesnt exist.
        else
        {
            logger.LogWarning("Application not found, redirecting you to download installer." + Environment.NewLine
                + "Current App Path is: " + AppPath);
            Util.OpenLink("https://intiface.com/central/");
        }
    }

    public static IntPtr FindWindowByRegex(ILogger logger, string pattern)
    {
        var matchedWindowHandle = IntPtr.Zero;
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        User32.EnumWindows((hWnd, lParam) =>
        {
            if (User32.IsWindowVisible(hWnd))
            {
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);
                var windowTitle = sb.ToString();

                if (regex.IsMatch(windowTitle))
                {
                    matchedWindowHandle = hWnd;
                    return false; // Stop enumerating windows once a match is found
                }
            }
            return true; // Continue enumerating windows
        }, IntPtr.Zero);

        return matchedWindowHandle;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static string GetActiveWindowTitle()
    {
        var hwnd = User32.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return "No active window";

        User32.GetWindowThreadProcessId(hwnd, out var processId);
        var process = Process.GetProcessById(processId);

        return process.MainWindowTitle;
    }
}
