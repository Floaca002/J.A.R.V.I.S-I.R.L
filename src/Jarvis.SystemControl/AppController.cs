using System.Diagnostics;
using System.Text;

namespace Jarvis.SystemControl;

/// <summary>
/// Launch, list and kill Linux processes / apps.
/// </summary>
public sealed class AppController
{
    /// <summary>Launch an app by its executable name (resolved via PATH) or a full path.</summary>
    public int OpenApp(string app, string? arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = app,
            UseShellExecute = false
        };
        if (!string.IsNullOrWhiteSpace(arguments))
            foreach (var arg in SplitArguments(arguments))
                psi.ArgumentList.Add(arg);

        var p = Process.Start(psi);
        return p?.Id ?? -1;
    }

    /// <summary>Open a URL or document with whatever the desktop considers its default handler.</summary>
    public void OpenUrl(string url)
    {
        var psi = new ProcessStartInfo("xdg-open", url) { UseShellExecute = false };
        Process.Start(psi);
    }

    public int CloseApp(string name)
    {
        var killed = 0;
        foreach (var p in Process.GetProcessesByName(name))
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

    /// <summary>Naive whitespace split respecting simple "quoted" segments — no shell is invoked, so no injection risk either way.</summary>
    private static IEnumerable<string> SplitArguments(string arguments)
    {
        var current = new StringBuilder();
        var inQuotes = false;
        foreach (var c in arguments)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0) { yield return current.ToString(); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) yield return current.ToString();
    }
}
