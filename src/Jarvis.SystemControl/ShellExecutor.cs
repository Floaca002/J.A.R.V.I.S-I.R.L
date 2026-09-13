using System.Diagnostics;
using System.Text;

namespace Jarvis.SystemControl;

/// <summary>
/// Run shell commands and capture output.
/// </summary>
public sealed class ShellExecutor
{
    public async Task<string> RunShellAsync(string command, TimeSpan timeout, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // ArgumentList (not a concatenated Arguments string) so the command reaches
        // bash -c as a single argv element — no manual quote-escaping, no injection risk.
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(command);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start bash");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var stdoutTask = p.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = p.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await p.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(true); } catch { }
            return "(command timed out)";
        }

        var sb = new StringBuilder();
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stdout)) sb.AppendLine(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) sb.Append("STDERR: ").AppendLine(stderr);
        sb.Append("(exit ").Append(p.ExitCode).Append(')');
        return sb.ToString();
    }
}
