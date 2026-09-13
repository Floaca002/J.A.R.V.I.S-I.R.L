using Jarvis.Core.Commands;
using Jarvis.Core.Tools;

namespace Jarvis.SelfUpgrade;

/// <summary>
/// High-level "evolve thyself" engine. Given an LLM-produced C# tool source,
/// it compiles it, validates it implements ITool, and registers it live.
/// </summary>
public sealed class SelfUpgradeEngine
{
    private readonly CodeCompiler _compiler = new();
    private readonly ToolDispatcher _dispatcher;
    private readonly string _upgradesDir;
    private readonly List<UpgradeRecord> _history = new();
    private readonly object _gate = new();

    public SelfUpgradeEngine(ToolDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _upgradesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JarvisIRL", "upgrades");
        Directory.CreateDirectory(_upgradesDir);
    }

    public IReadOnlyList<UpgradeRecord> History
    {
        get { lock (_gate) return _history.ToArray(); }
    }

    public UpgradeResult InstallNewTool(string sourceCode, string toolNameHint)
    {
        var asmName = $"JarvisDynTool_{toolNameHint}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var result = _compiler.Compile(sourceCode, asmName);

        if (!result.Success || result.Assembly == null)
            return new UpgradeResult(false, null, string.Join("\n", result.Errors));

        var toolType = result.Assembly.GetTypes()
            .FirstOrDefault(t => typeof(ITool).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        if (toolType == null)
            return new UpgradeResult(false, null, "No type implementing ITool was found in the generated source.");

        ITool tool;
        try
        {
            tool = (ITool)Activator.CreateInstance(toolType)!;
        }
        catch (Exception ex)
        {
            return new UpgradeResult(false, null, $"Failed to instantiate tool: {ex.Message}");
        }

        _dispatcher.Register(tool);

        // Persist source for audit.
        var savePath = Path.Combine(_upgradesDir, $"{tool.Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.cs");
        File.WriteAllText(savePath, sourceCode);

        lock (_gate) _history.Add(new UpgradeRecord(tool.Name, DateTime.UtcNow, savePath));

        return new UpgradeResult(true, tool.Name, $"Tool '{tool.Name}' compiled and loaded. Source saved at {savePath}");
    }

    /// <summary>Unregisters the most recently installed dynamic tool. Returns its name, or null if there was none.</summary>
    public string? RevertLast()
    {
        UpgradeRecord? last;
        lock (_gate)
        {
            if (_history.Count == 0) return null;
            last = _history[^1];
            _history.RemoveAt(_history.Count - 1);
        }
        _dispatcher.Unregister(last.ToolName);
        return last.ToolName;
    }
}

public sealed record UpgradeResult(bool Success, string? ToolName, string Message);
public sealed record UpgradeRecord(string ToolName, DateTime At, string SourcePath);
