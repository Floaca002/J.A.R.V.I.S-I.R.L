using System.Text.Json;
using System.Text.Json.Nodes;
using Jarvis.Core.AI;

namespace Jarvis.Core.Commands;

/// <summary>
/// Holds the live tool catalog and dispatches tool_calls from the LLM.
/// New tools (including LLM-generated ones via self-upgrade) can be registered at runtime.
/// </summary>
public sealed class ToolDispatcher
{
    private readonly Dictionary<string, Tools.ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public IReadOnlyList<Tools.ITool> Tools
    {
        get { lock (_gate) return _tools.Values.ToArray(); }
    }

    public void Register(Tools.ITool tool)
    {
        lock (_gate) _tools[tool.Name] = tool;
    }

    public void Unregister(string name)
    {
        lock (_gate) _tools.Remove(name);
    }

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        Tools.ITool? tool;
        lock (_gate) _tools.TryGetValue(call.Function.Name, out tool);

        if (tool == null)
            return $"{{\"error\":\"Unknown tool '{call.Function.Name}'\"}}";

        try
        {
            JsonNode? args = null;
            if (!string.IsNullOrWhiteSpace(call.Function.Arguments))
                args = JsonNode.Parse(call.Function.Arguments);

            var result = await tool.RunAsync(args, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, tool = tool.Name });
        }
    }
}
