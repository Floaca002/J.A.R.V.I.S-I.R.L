using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Jarvis.SystemControl;

/// <summary>
/// Mouse, keyboard, and screen capture automation via Win32 + WinForms.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AutomationController
{
    public string TakeScreenshot(string? path)
    {
        path ??= Path.Combine(Path.GetTempPath(), $"jarvis-shot-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    /// <summary>Capture the primary monitor straight to a base64 PNG string (no temp file).</summary>
    public string CaptureScreenPngBase64()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    public void TypeText(string text) => SendKeys.SendWait(text);

    public void MouseClick(int x, int y, bool rightClick = false)
    {
        SetCursorPos(x, y);
        const uint LD = 0x0002, LU = 0x0004, RD = 0x0008, RU = 0x0010;
        mouse_event(rightClick ? RD : LD, 0, 0, 0, UIntPtr.Zero);
        mouse_event(rightClick ? RU : LU, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>Tap a media volume key. direction: "up" | "down" | "mute".</summary>
    public void AdjustVolume(string direction)
    {
        const byte VK_VOLUME_MUTE = 0xAD, VK_VOLUME_DOWN = 0xAE, VK_VOLUME_UP = 0xAF;
        const uint KEYEVENTF_KEYUP = 0x0002;

        byte vk = direction.ToLowerInvariant() switch
        {
            "up" => VK_VOLUME_UP,
            "down" => VK_VOLUME_DOWN,
            "mute" => VK_VOLUME_MUTE,
            _ => throw new ArgumentException("direction must be 'up', 'down' or 'mute'")
        };

        keybd_event(vk, 0, 0, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    /// <summary>Minimize/maximize/restore/focus/close the main window of the first process matching <paramref name="processName"/>.</summary>
    public bool SetWindowState(string processName, string state)
    {
        var name = processName.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
        var proc = Process.GetProcessesByName(name).FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
        if (proc == null) return false;

        const int SW_RESTORE = 9, SW_MINIMIZE = 6, SW_MAXIMIZE = 3;
        switch (state.ToLowerInvariant())
        {
            case "minimize": ShowWindow(proc.MainWindowHandle, SW_MINIMIZE); break;
            case "maximize": ShowWindow(proc.MainWindowHandle, SW_MAXIMIZE); break;
            case "restore": ShowWindow(proc.MainWindowHandle, SW_RESTORE); break;
            case "focus": SetForegroundWindow(proc.MainWindowHandle); break;
            case "close": proc.CloseMainWindow(); break;
            default: throw new ArgumentException("state must be minimize|maximize|restore|focus|close");
        }
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
