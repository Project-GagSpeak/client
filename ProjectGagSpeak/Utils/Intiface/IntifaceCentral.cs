using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TerraFX.Interop.Windows;

namespace GagSpeak.Utils;

public static partial class IntifaceCentral
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

    public static void OpenIntiface(bool pushToForeground)
    {
        // search for the intiface celtral window
        var windowHandle = FindWindowByRegex(@"Intiface\u00AE Central*");
        // if it's present, place it to the foreground
        if (windowHandle != IntPtr.Zero)
        {
            if (pushToForeground)
            {
                Svc.Logger.Debug("Intiface Central found, bringing to foreground.");
                TerraFX.Interop.Windows.Windows.ShowWindow(windowHandle, 0);
                TerraFX.Interop.Windows.Windows.SetForegroundWindow(windowHandle);
            }
        }
        // otherwise, start the process to open intiface central
        else if (!string.IsNullOrEmpty(AppPath) && File.Exists(AppPath))
        {
            Svc.Logger.Information("Starting Intiface Central");
            Process.Start(AppPath);
        }
        // or just open the installer if it doesnt exist.
        else
        {
            Svc.Logger.Warning("Application not found, redirecting you to download installer.\n" +
                $"Current App Path is: {AppPath}");
            Util.OpenLink("https://intiface.com/");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ResultStruct
    {
        public fixed char WindowName[512];
        public HWND FoundWindow;
        public GCHandle RegexPtr;
    }

    public static unsafe HWND FindWindowByRegex(string pattern)
    {
        var result = new ResultStruct
        {
            FoundWindow = HWND.NULL,
            RegexPtr = GCHandle.Alloc(new Regex(pattern, RegexOptions.IgnoreCase))
        };

        try
        {
            TerraFX.Interop.Windows.Windows.EnumWindows(&EnumWindowsCallback, (LPARAM)(&result));
        }
        finally
        {
            result.RegexPtr.Free();
        }

        return result.FoundWindow;
    }

    [UnmanagedCallersOnly]
    private static unsafe BOOL EnumWindowsCallback(HWND handle, LPARAM lParam)
    {
        var data = (ResultStruct*)lParam;
        var regex = (Regex)data->RegexPtr.Target!;

        if (TerraFX.Interop.Windows.Windows.IsWindowVisible(handle))
        {
            int len = TerraFX.Interop.Windows.Windows.GetWindowText(handle, data->WindowName, 512);
            string windowTitle = new string(data->WindowName, 0, len);

            if (regex.IsMatch(windowTitle))
            {
                data->FoundWindow = handle;
                return BOOL.FALSE; // stop enumeration
            }
        }
        return BOOL.TRUE; // continue
    }

    public static unsafe string GetActiveWindowTitle()
    {
        var hwnd = TerraFX.Interop.Windows.Windows.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return "No active window";

        uint processId;
        TerraFX.Interop.Windows.Windows.GetWindowThreadProcessId(hwnd, &processId);
        var process = Process.GetProcessById((int)processId);

        return process.MainWindowTitle;
    }
}
