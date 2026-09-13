using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Jarvis.Core.Config;
using Jarvis.Core.Tools;

namespace Jarvis.Core.AI;

/// <summary>
/// Claude (Anthropic) provider — uses the official Anthropic .NET SDK.
/// Supports tool use and image input (screen vision), unlike the default Groq model.
///
/// Note: this is billed separately from a claude.ai Pro/Max chat subscription — it needs
/// its own API key and prepaid credits from https://console.anthropic.com.
/// </summary>
public sealed class AnthropicProvider : IAIProvider
{
    private readonly AnthropicClient _client;
    private readonly AnthropicConfig _cfg;

    public string Name => "Anthropic";

    public AnthropicProvider(AnthropicConfig cfg)
    {
        _cfg = cfg;
        _client = new AnthropicClient { ApiKey = cfg.ApiKey };
    }

    public async Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_cfg.ApiKey))
            throw new InvalidOperationException("Anthropic API key is not configured. Set AI.Anthropic.ApiKey in appsettings.json or env var JARVIS_ANTHROPIC_API_KEY.");

        string? systemPrompt = null;
        var claudeMessages = new List<MessageParam>();

        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            switch (m.Role)
            {
                case "system":
                    systemPrompt = m.Content;
                    break;

                case "user":
                {
                    var content = new List<ContentBlockParam>();
                    if (!string.IsNullOrEmpty(m.ImagePngBase64))
                        content.Add(new ImageBlockParam
                        {
                            Source = new Base64ImageSource { Data = m.ImagePngBase64, MediaType = "image/png" }
                        });
                    content.Add(new TextBlockParam { Text = m.Content ?? string.Empty });
                    claudeMessages.Add(new MessageParam { Role = Role.User, Content = content });
                    break;
                }

                case "assistant":
                {
                    var content = new List<ContentBlockParam>();
                    if (!string.IsNullOrEmpty(m.Content))
                        content.Add(new TextBlockParam { Text = m.Content! });
                    if (m.ToolCalls != null)
                    {
                        foreach (var call in m.ToolCalls)
                        {
                            var input = string.IsNullOrWhiteSpace(call.Function.Arguments)
                                ? new Dictionary<string, JsonElement>()
                                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(call.Function.Arguments)
                                  ?? new Dictionary<string, JsonElement>();
                            content.Add(new ToolUseBlockParam { ID = call.Id, Name = call.Function.Name, Input = input });
                        }
                    }
                    claudeMessages.Add(new MessageParam { Role = Role.Assistant, Content = content });
                    break;
                }

                case "tool":
                {
                    // Claude expects every tool_result for one assistant turn batched into a
                    // single following user message — merge consecutive "tool" messages.
                    var results = new List<ContentBlockParam>
                    {
                        new ToolResultBlockParam { ToolUseID = m.ToolCallId ?? string.Empty, Content = m.Content ?? string.Empty }
                    };
                    while (i + 1 < messages.Count && messages[i + 1].Role == "tool")
                    {
                        i++;
                        results.Add(new ToolResultBlockParam
                        {
                            ToolUseID = messages[i].ToolCallId ?? string.Empty,
                            Content = messages[i].Content ?? string.Empty
                        });
                    }
                    claudeMessages.Add(new MessageParam { Role = Role.User, Content = results });
                    break;
                }
            }
        }

        var parameters = new MessageCreateParams
        {
            Model = _cfg.Model,
            MaxTokens = _cfg.MaxTokens,
            Messages = claudeMessages,
        };
        if (!string.IsNullOrEmpty(systemPrompt))
            parameters.System = systemPrompt;
        if (tools.Count > 0)
            parameters.Tools = tools.Select(t => (ToolUnion)BuildToolSchema(t)).ToArray();

        Message response;
        try
        {
            response = await _client.Messages.Create(parameters).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Anthropic API error: {ex.Message}", ex);
        }

        var text = new StringBuilder();
        var toolCalls = new List<ToolCall>();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
                text.Append(textBlock.Text);
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                toolCalls.Add(new ToolCall
                {
                    Id = toolUse.ID,
                    Type = "function",
                    Function = new ToolCallFunction
                    {
                        Name = toolUse.Name,
                        Arguments = JsonSerializer.Serialize(toolUse.Input)
                    }
                });
        }

        return new ChatResponse
        {
            Content = text.ToString(),
            ToolCalls = toolCalls,
            FinishReason = response.StopReason?.ToString() ?? "stop"
        };
    }

    private static Tool BuildToolSchema(ITool tool)
    {
        using var doc = JsonDocument.Parse(tool.JsonSchema);
        var root = doc.RootElement;

        var properties = new Dictionary<string, JsonElement>();
        if (root.TryGetProperty("properties", out var propsEl))
            foreach (var p in propsEl.EnumerateObject())
                properties[p.Name] = p.Value.Clone();

        List<string>? required = null;
        if (root.TryGetProperty("required", out var reqEl) && reqEl.ValueKind == JsonValueKind.Array)
            required = reqEl.EnumerateArray().Select(e => e.GetString()!).ToList();

        return new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new() { Properties = properties, Required = required },
        };
    }
}
