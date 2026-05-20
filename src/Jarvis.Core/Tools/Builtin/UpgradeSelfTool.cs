using System.Text.Json.Nodes;
using Jarvis.Core.Tools;
using Jarvis.SelfUpgrade;

namespace Jarvis.Core.Tools.Builtin;

/// <summary>
/// LLM-driven self-upgrade tool. The LLM passes generated C# source for a new ITool,
/// and Jarvis compiles + loads it at runtime.
/// </summary>
public sealed class UpgradeSelfTool(SelfUpgradeEngine engine) : ITool
{
    public string Name => "upgrade_self";
    public string Description =>
        "Generate and hot-load a new C# tool implementing Jarvis.Core.Tools.ITool. " +
        "Pass the full source code of a public sealed class that implements ITool. " +
        "Use only standard .NET 8 APIs. Namespace must be 'Jarvis.Dynamic'.";

    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "tool_name":   { "type": "string", "description": "snake_case name the new tool will register under." },
        "source_code": { "type": "string", "description": "Full C# source of the ITool implementation." }
      },
      "required": ["tool_name", "source_code"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var toolName = args?["tool_name"]?.GetValue<string>() ?? "anon";
        var source = args?["source_code"]?.GetValue<string>() ?? throw new ArgumentException("'source_code' required");
        var res = engine.InstallNewTool(source, toolName);
        return Task.FromResult($"upgrade success={res.Success}, message={res.Message}");
    }
}
