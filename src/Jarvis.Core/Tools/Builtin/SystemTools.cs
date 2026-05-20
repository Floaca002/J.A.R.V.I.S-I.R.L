using System.Text.Json.Nodes;
using Jarvis.SystemControl;

namespace Jarvis.Core.Tools.Builtin;

public sealed class OpenAppTool(AppController apps) : ITool
{
    public string Name => "open_app";
    public string Description => "Launch a Windows application by name (notepad, calc, chrome, etc.) or full path.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "app":       { "type": "string", "description": "App name or path." },
        "arguments": { "type": "string", "description": "Optional command-line args." }
      },
      "required": ["app"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var app = args?["app"]?.GetValue<string>() ?? throw new ArgumentException("'app' required");
        var a = args?["arguments"]?.GetValue<string>();
        var pid = apps.OpenApp(app, a);
        return Task.FromResult($"Launched '{app}' (pid={pid})");
    }
}

public sealed class CloseAppTool(AppController apps) : ITool
{
    public string Name => "close_app";
    public string Description => "Close/kill a running process by name.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "name": { "type": "string", "description": "Process name without .exe (e.g. 'notepad')." }
      },
      "required": ["name"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var name = args?["name"]?.GetValue<string>() ?? throw new ArgumentException("'name' required");
        var killed = apps.CloseApp(name);
        return Task.FromResult($"Closed {killed} process(es) named '{name}'");
    }
}

public sealed class ListProcessesTool(AppController apps) : ITool
{
    public string Name => "list_processes";
    public string Description => "Return a snapshot of the top running processes (name + memory).";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "top": { "type": "integer", "description": "How many processes to return", "default": 20 }
      }
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var top = args?["top"]?.GetValue<int>() ?? 20;
        return Task.FromResult(apps.ListProcesses(top));
    }
}

public sealed class RunShellTool(ShellExecutor shell) : ITool
{
    public string Name => "run_shell";
    public string Description => "Execute a PowerShell command and return stdout+stderr. Destructive — confirm first.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "command":      { "type": "string", "description": "PowerShell command line." },
        "timeout_secs": { "type": "integer", "default": 30 }
      },
      "required": ["command"]
    }
    """;

    public async Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var cmd = args?["command"]?.GetValue<string>() ?? throw new ArgumentException("'command' required");
        var timeout = args?["timeout_secs"]?.GetValue<int>() ?? 30;
        return await shell.RunPowerShellAsync(cmd, TimeSpan.FromSeconds(timeout), ct).ConfigureAwait(false);
    }
}

public sealed class ScreenshotTool(AutomationController auto) : ITool
{
    public string Name => "take_screenshot";
    public string Description => "Capture the primary monitor to a PNG file and return its path.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "Optional output path; default = %TEMP%/jarvis-shot.png" }
      }
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var path = args?["path"]?.GetValue<string>();
        var saved = auto.TakeScreenshot(path);
        return Task.FromResult($"Saved screenshot to {saved}");
    }
}
