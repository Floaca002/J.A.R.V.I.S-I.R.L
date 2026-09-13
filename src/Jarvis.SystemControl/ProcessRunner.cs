using System.Diagnostics;

namespace Jarvis.SystemControl;

/// <summary>
/// Small shared helper for shelling out to Linux desktop CLI tools (screenshot,
/// clipboard, volume, window, input utilities all vary by distro/compositor, so
/// every controller in this project detects what's installed at runtime and picks
/// the first one that works, rather than hard-depending on one toolchain).
/// </summary>
internal static class ProcessRunner
{
    public static bool IsOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator)
            .Any(dir => !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, exe)));
    }

    /// <summary>Run a command, optionally feeding stdin, and return whether it exited 0.</summary>
    public static bool Run(string exe, IEnumerable<string> args, int timeoutMs = 8000, string? stdin = null)
    {
        try
        {
            using var p = Process.Start(BuildPsi(exe, args, stdin != null));
            if (p == null) return false;
            if (stdin != null)
            {
                p.StandardInput.Write(stdin);
                p.StandardInput.Close();
            }
            if (!p.WaitForExit(timeoutMs))
            {
                TryKill(p);
                return false;
            }
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Run a command and return its stdout, or null if it failed/timed out/wasn't found.</summary>
    public static string? Capture(string exe, IEnumerable<string> args, int timeoutMs = 8000)
    {
        try
        {
            using var p = Process.Start(BuildPsi(exe, args, false));
            if (p == null) return null;
            var output = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(timeoutMs))
            {
                TryKill(p);
                return null;
            }
            return p.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static ProcessStartInfo BuildPsi(string exe, IEnumerable<string> args, bool redirectIn)
    {
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = redirectIn,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }

    private static void TryKill(Process p)
    {
        try { p.Kill(true); } catch { /* best effort */ }
    }

    public static bool IsWayland() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) ||
        string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase);
}
