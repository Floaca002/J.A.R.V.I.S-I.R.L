using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jarvis.Core.Config;
using Jarvis.Core.Tools;

namespace Jarvis.Core.AI;

/// <summary>
/// Local Ollama provider — fully offline, free, private.
/// https://ollama.com — run `ollama pull llama3.2` first.
/// </summary>
public sealed class OllamaProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly OllamaConfig _cfg;

    public string Name => "Ollama";

    public OllamaProvider(OllamaConfig cfg, HttpClient? http = null)
    {
        _cfg = cfg;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _cfg.Model,
            stream = false,
            messages = messages.Select(m => new { role = m.Role, content = m.Content ?? string.Empty }).ToArray(),
            options = new { temperature = _cfg.Temperature },
            tools = tools.Count == 0 ? null : tools.Select(BuildToolSchema).ToArray()
        };

        using var resp = await _http.PostAsJsonAsync(_cfg.Endpoint, payload, cancellationToken).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama returned {(int)resp.StatusCode}: {body}");

        var root = JsonNode.Parse(body)!;
        var msg = root["message"];
        var response = new ChatResponse
        {
            Content = msg?["content"]?.GetValue<string>() ?? string.Empty,
            FinishReason = root["done_reason"]?.GetValue<string>() ?? "stop"
        };

        if (msg?["tool_calls"] is JsonArray calls)
        {
            foreach (var call in calls)
            {
                response.ToolCalls.Add(new ToolCall
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Function = new ToolCallFunction
                    {
                        Name = call?["function"]?["name"]?.GetValue<string>() ?? string.Empty,
                        Arguments = call?["function"]?["arguments"]?.ToJsonString() ?? "{}"
                    }
                });
            }
        }

        return response;
    }

    private static object BuildToolSchema(ITool tool) => new
    {
        type = "function",
        function = new
        {
            name = tool.Name,
            description = tool.Description,
            parameters = JsonNode.Parse(tool.JsonSchema)
        }
    };
}
