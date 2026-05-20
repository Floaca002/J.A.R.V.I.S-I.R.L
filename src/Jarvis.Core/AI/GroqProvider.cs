using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Jarvis.Core.Config;
using Jarvis.Core.Tools;

namespace Jarvis.Core.AI;

/// <summary>
/// Groq Cloud provider — OpenAI-compatible chat completions with native function calling.
/// Free tier: https://console.groq.com/keys
/// </summary>
public sealed class GroqProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly GroqConfig _cfg;

    public string Name => "Groq";

    public GroqProvider(GroqConfig cfg, HttpClient? http = null)
    {
        _cfg = cfg;
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_cfg.ApiKey))
            throw new InvalidOperationException("Groq API key is not configured. Set AI.Groq.ApiKey in appsettings.json or env var JARVIS_GROQ_API_KEY.");

        var payload = new
        {
            model = _cfg.Model,
            messages = messages,
            temperature = _cfg.Temperature,
            max_tokens = _cfg.MaxTokens,
            tools = tools.Count == 0 ? null : tools.Select(BuildToolSchema).ToArray(),
            tool_choice = tools.Count == 0 ? null : (object)"auto"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _cfg.Endpoint);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cfg.ApiKey);
        req.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Groq returned {(int)resp.StatusCode}: {body}");

        return ParseResponse(body);
    }

    private static ChatResponse ParseResponse(string body)
    {
        var root = JsonNode.Parse(body)!;
        var choice = root["choices"]?[0];
        var msg = choice?["message"];

        var resp = new ChatResponse
        {
            Content = msg?["content"]?.GetValue<string>() ?? string.Empty,
            FinishReason = choice?["finish_reason"]?.GetValue<string>() ?? "stop"
        };

        if (msg?["tool_calls"] is JsonArray calls)
        {
            foreach (var call in calls)
            {
                resp.ToolCalls.Add(new ToolCall
                {
                    Id = call?["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                    Type = call?["type"]?.GetValue<string>() ?? "function",
                    Function = new ToolCallFunction
                    {
                        Name = call?["function"]?["name"]?.GetValue<string>() ?? string.Empty,
                        Arguments = call?["function"]?["arguments"]?.GetValue<string>() ?? "{}"
                    }
                });
            }
        }

        return resp;
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
