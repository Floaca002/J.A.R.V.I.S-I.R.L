using System.Diagnostics;
using System.Text;

namespace Jarvis.SystemControl;

/// <summary>
/// Run PowerShell / cmd commands and capture output.
/// </summary>
public sealed class ShellExecutor
{
    public async Task<string> RunPowerShellAsync(string command, TimeSpan timeout, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start PowerShell");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var stdoutTask = p.StandardOutput.ReadToEndAsync(cts.Token).AsTask();
        var stderrTask = p.StandardError.ReadToEndAsync(cts.Token).AsTask();

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
