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

    public void TypeText(string text) => SendKeys.SendWait(text);

    public void MouseClick(int x, int y, bool rightClick = false)
    {
        SetCursorPos(x, y);
        const uint LD = 0x0002, LU = 0x0004, RD = 0x0008, RU = 0x0010;
        mouse_event(rightClick ? RD : LD, 0, 0, 0, UIntPtr.Zero);
        mouse_event(rightClick ? RU : LU, 0, 0, 0, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
