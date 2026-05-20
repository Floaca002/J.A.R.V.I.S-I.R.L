using System.Text.Json.Nodes;

namespace Jarvis.Core.Tools;

/// <summary>
/// Every capability Jarvis can invoke is an ITool.
/// The LLM sees Name + Description + JsonSchema and decides when to call it.
/// </summary>
public interface ITool
{
    string Name { get; }
    string Description { get; }

    /// <summary>JSON Schema (draft-07) describing the parameters object.</summary>
    string JsonSchema { get; }

    /// <summary>
    /// Execute the tool. Arguments are the raw JSON object the LLM emitted.
    /// Return a human-readable string that will be looped back to the LLM.
    /// </summary>
    Task<string> RunAsync(JsonNode? arguments, CancellationToken cancellationToken = default);
}
