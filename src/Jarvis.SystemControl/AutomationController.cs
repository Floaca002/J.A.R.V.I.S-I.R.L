using System.Diagnostics;

namespace Jarvis.SystemControl;

/// <summary>
/// Screen capture, volume, window and input automation.
///
/// Linux has no single API for any of this — it varies by display server (X11 vs
/// Wayland) and desktop environment, so every method here detects whichever CLI
/// tool is actually installed and uses that, rather than assuming one toolchain.
/// Where nothing usable is found, methods throw with a message naming exactly what
/// to install, instead of silently doing nothing.
/// </summary>
public sealed class AutomationController
{
    public string TakeScreenshot(string? path)
    {
        path ??= Path.Combine(Path.GetTempPath(), $"jarvis-shot-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        if (!TryCapture(path))
            throw new InvalidOperationException(
                "No screenshot tool found. Install one of: grim (Wayland), spectacle (KDE), " +
                "gnome-screenshot (GNOME), or scrot / ImageMagick's `import` (X11).");
        return path;
    }

    /// <summary>Capture the screen straight to a base64 PNG string (no file left behind).</summary>
    public string CaptureScreenPngBase64()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), $"jarvis-shot-{Guid.NewGuid():N}.png");
        try
        {
            if (!TryCapture(tmpPath))
                throw new InvalidOperationException(
                    "No screenshot tool found. Install one of: grim (Wayland), spectacle (KDE), " +
                    "gnome-screenshot (GNOME), or scrot / ImageMagick's `import` (X11).");
            return Convert.ToBase64String(File.ReadAllBytes(tmpPath));
        }
        finally
        {
            try { File.Delete(tmpPath); } catch { /* best effort */ }
        }
    }

    private static bool TryCapture(string outPath)
    {
        if (ProcessRunner.IsWayland() && ProcessRunner.IsOnPath("grim") && ProcessRunner.Run("grim", new[] { outPath }))
            return true;
        if (ProcessRunner.IsOnPath("spectacle") && ProcessRunner.Run("spectacle", new[] { "-b", "-n", "-o", outPath }))
            return true;
        if (ProcessRunner.IsOnPath("gnome-screenshot") && ProcessRunner.Run("gnome-screenshot", new[] { "-f", outPath }))
            return true;
        if (ProcessRunner.IsOnPath("scrot") && ProcessRunner.Run("scrot", new[] { "-o", outPath }))
            return true;
        if (ProcessRunner.IsOnPath("import") && ProcessRunner.Run("import", new[] { "-window", "root", outPath }))
            return true;
        return false;
    }

    /// <summary>Type text into whichever window has focus.</summary>
    public void TypeText(string text)
    {
        if (ProcessRunner.IsOnPath("ydotool") && ProcessRunner.Run("ydotool", new[] { "type", "--", text }))
            return;
        if (ProcessRunner.IsOnPath("xdotool") && ProcessRunner.Run("xdotool", new[] { "type", "--", text }))
            return;
        throw new InvalidOperationException(
            "Typing needs `ydotool` (Wayland — requires the ydotoold service and input-group " +
            "permissions) or `xdotool` (X11/XWayland).");
    }

    /// <summary>Move the mouse and click. Reliable support currently requires X11 or XWayland (`xdotool`).</summary>
    public void MouseClick(int x, int y, bool rightClick = false)
    {
        if (ProcessRunner.IsOnPath("xdotool") &&
            ProcessRunner.Run("xdotool", new[] { "mousemove", "--sync", x.ToString(), y.ToString(), "click", rightClick ? "3" : "1" }))
            return;
        throw new InvalidOperationException(
            "Mouse control needs `xdotool` (X11, or XWayland apps under most Wayland compositors). " +
            "Native Wayland windows generally can't be clicked this way.");
    }

    /// <summary>Tap the system volume up/down/mute. direction: "up" | "down" | "mute".</summary>
    public void AdjustVolume(string direction)
    {
        var dir = direction.ToLowerInvariant();
        if (dir is not ("up" or "down" or "mute"))
            throw new ArgumentException("direction must be 'up', 'down' or 'mute'");

        if (ProcessRunner.IsOnPath("wpctl"))
        {
            var args = dir switch
            {
                "up" => new[] { "set-volume", "@DEFAULT_AUDIO_SINK@", "5%+" },
                "down" => new[] { "set-volume", "@DEFAULT_AUDIO_SINK@", "5%-" },
                _ => new[] { "set-mute", "@DEFAULT_AUDIO_SINK@", "toggle" }
            };
            if (ProcessRunner.Run("wpctl", args)) return;
        }
        if (ProcessRunner.IsOnPath("pactl"))
        {
            var args = dir switch
            {
                "up" => new[] { "set-sink-volume", "@DEFAULT_SINK@", "+5%" },
                "down" => new[] { "set-sink-volume", "@DEFAULT_SINK@", "-5%" },
                _ => new[] { "set-sink-mute", "@DEFAULT_SINK@", "toggle" }
            };
            if (ProcessRunner.Run("pactl", args)) return;
        }
        if (ProcessRunner.IsOnPath("amixer"))
        {
            var args = dir switch
            {
                "up" => new[] { "-q", "sset", "Master", "5%+" },
                "down" => new[] { "-q", "sset", "Master", "5%-" },
                _ => new[] { "-q", "sset", "Master", "toggle" }
            };
            if (ProcessRunner.Run("amixer", args)) return;
        }
        throw new InvalidOperationException(
            "No volume control tool found. Install `wireplumber` (wpctl), `pulseaudio-utils`/`pipewire-pulse` (pactl), or `alsa-utils` (amixer).");
    }

    /// <summary>Minimize/maximize/restore/focus/close the first window belonging to a process. Requires X11/XWayland (`wmctrl`).</summary>
    public bool SetWindowState(string processName, string state)
    {
        if (!ProcessRunner.IsOnPath("wmctrl"))
            throw new InvalidOperationException(
                "Window control needs `wmctrl` (works under X11, or XWayland apps under most Wayland compositors — " +
                "native Wayland windows generally can't be managed this way without compositor-specific tooling).");

        var windowId = FindWindowId(processName);
        if (windowId == null) return false;

        return state.ToLowerInvariant() switch
        {
            "minimize" => ProcessRunner.IsOnPath("xdotool") && ProcessRunner.Run("xdotool", new[] { "windowminimize", windowId }),
            "maximize" => ProcessRunner.Run("wmctrl", new[] { "-i", "-r", windowId, "-b", "add,maximized_vert,maximized_horz" }),
            "restore" => ProcessRunner.Run("wmctrl", new[] { "-i", "-r", windowId, "-b", "remove,maximized_vert,maximized_horz" }),
            "focus" => ProcessRunner.Run("wmctrl", new[] { "-i", "-a", windowId }),
            "close" => ProcessRunner.Run("wmctrl", new[] { "-i", "-c", windowId }),
            _ => throw new ArgumentException("state must be minimize|maximize|restore|focus|close")
        };
    }

    private static string? FindWindowId(string processName)
    {
        var listing = ProcessRunner.Capture("wmctrl", new[] { "-lp" });
        if (listing == null) return null;

        var pids = Process.GetProcessesByName(processName).Select(p => p.Id).ToHashSet();
        foreach (var line in listing.Split('\n'))
        {
            // wmctrl -lp columns: <window-id> <desktop> <pid> <host> <title...>
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && int.TryParse(parts[2], out var pid) && pids.Contains(pid))
                return parts[0];
        }
        return null;
    }
}
