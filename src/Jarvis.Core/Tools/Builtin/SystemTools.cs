using System.Text.Json.Nodes;
using Jarvis.Core.Config;
using Jarvis.Core.Security;
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

public sealed class RunShellTool(ShellExecutor shell, SecurityConfig security, IConfirmationService confirm) : ITool
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

        if (security.RequireConfirmationForCommands)
        {
            var approved = await confirm.ConfirmAsync(
                "Shell command requested",
                $"Jarvis wants to run:\n\n{cmd}",
                ct).ConfigureAwait(false);
            if (!approved) return "Cancelled by user.";
        }

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

public sealed class OpenUrlTool(AppController apps) : ITool
{
    public string Name => "open_url";
    public string Description => "Open a URL in the user's default web browser.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "url": { "type": "string", "description": "The URL to open, e.g. https://example.com" }
      },
      "required": ["url"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var url = args?["url"]?.GetValue<string>() ?? throw new ArgumentException("'url' required");
        apps.OpenApp(url, null);
        return Task.FromResult($"Opened {url}");
    }
}

public sealed class AdjustVolumeTool(AutomationController auto) : ITool
{
    public string Name => "adjust_volume";
    public string Description => "Tap the system volume up/down/mute key.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "direction": { "type": "string", "enum": ["up", "down", "mute"] }
      },
      "required": ["direction"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var direction = args?["direction"]?.GetValue<string>() ?? throw new ArgumentException("'direction' required");
        auto.AdjustVolume(direction);
        return Task.FromResult($"Volume {direction}");
    }
}

public sealed class WindowControlTool(AutomationController auto) : ITool
{
    public string Name => "control_window";
    public string Description => "Minimize, maximize, restore, focus, or close a running app's main window.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "process": { "type": "string", "description": "Process name without .exe, e.g. 'notepad'." },
        "state":   { "type": "string", "enum": ["minimize", "maximize", "restore", "focus", "close"] }
      },
      "required": ["process", "state"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var process = args?["process"]?.GetValue<string>() ?? throw new ArgumentException("'process' required");
        var state = args?["state"]?.GetValue<string>() ?? throw new ArgumentException("'state' required");
        var ok = auto.SetWindowState(process, state);
        return Task.FromResult(ok ? $"{process} → {state}" : $"No visible window found for process '{process}'");
    }
}

public sealed class TypeTextTool(AutomationController auto) : ITool
{
    public string Name => "type_text";
    public string Description => "Type text into whichever window currently has keyboard focus, as if from the physical keyboard.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": { "type": "string", "description": "Text to type." }
      },
      "required": ["text"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var text = args?["text"]?.GetValue<string>() ?? throw new ArgumentException("'text' required");
        auto.TypeText(text);
        return Task.FromResult($"Typed {text.Length} characters.");
    }
}

public sealed class MouseClickTool(AutomationController auto) : ITool
{
    public string Name => "mouse_click";
    public string Description => "Move the mouse to a screen coordinate and click (left or right button).";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "x": { "type": "integer" },
        "y": { "type": "integer" },
        "right_click": { "type": "boolean", "default": false }
      },
      "required": ["x", "y"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var x = args?["x"]?.GetValue<int>() ?? throw new ArgumentException("'x' required");
        var y = args?["y"]?.GetValue<int>() ?? throw new ArgumentException("'y' required");
        var right = args?["right_click"]?.GetValue<bool>() ?? false;
        auto.MouseClick(x, y, right);
        return Task.FromResult($"{(right ? "Right" : "Left")}-clicked at ({x}, {y}).");
    }
}

public sealed class GetClipboardTool(ClipboardController clipboard) : ITool
{
    public string Name => "get_clipboard";
    public string Description => "Read the current text on the clipboard.";
    public string JsonSchema => """{ "type": "object", "properties": {} }""";

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
        => Task.FromResult(clipboard.GetText());
}

public sealed class SetClipboardTool(ClipboardController clipboard) : ITool
{
    public string Name => "set_clipboard";
    public string Description => "Write text to the clipboard.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": { "type": "string" }
      },
      "required": ["text"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var text = args?["text"]?.GetValue<string>() ?? throw new ArgumentException("'text' required");
        clipboard.SetText(text);
        return Task.FromResult("Clipboard updated.");
    }
}
