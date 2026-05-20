using System.Diagnostics;
using System.Text;

namespace Jarvis.SystemControl;

/// <summary>
/// Launch, list and kill Windows processes / apps.
/// </summary>
public sealed class AppController
{
    public int OpenApp(string app, string? arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = app,
            Arguments = arguments ?? string.Empty,
            UseShellExecute = true
        };
        var p = Process.Start(psi);
        return p?.Id ?? -1;
    }

    public int CloseApp(string name)
    {
        var killed = 0;
        foreach (var p in Process.GetProcessesByName(name.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                p.Kill(entireProcessTree: true);
                killed++;
            }
            catch { /* ignore */ }
        }
        return killed;
    }

    public string ListProcesses(int top)
    {
        var procs = Process.GetProcesses()
            .Where(p => { try { return p.WorkingSet64 > 0; } catch { return false; } })
            .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
            .Take(top);

        var sb = new StringBuilder();
        sb.AppendLine("PID     Memory(MB)   Name");
        foreach (var p in procs)
        {
            try
            {
                sb.AppendLine($"{p.Id,-7} {p.WorkingSet64 / 1024 / 1024,-12} {p.ProcessName}");
            }
            catch { }
        }
        return sb.ToString();
    }
}
