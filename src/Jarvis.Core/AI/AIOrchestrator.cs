using Jarvis.Core.Commands;
using Jarvis.Core.Config;
using Jarvis.Core.Memory;
using Jarvis.Core.Tools;
using Microsoft.Extensions.Logging;

namespace Jarvis.Core.AI;

/// <summary>
/// Top-level brain. Coordinates: provider selection, conversation memory,
/// tool dispatch and the multi-turn function-calling loop.
/// </summary>
public sealed class AIOrchestrator
{
    private readonly IAIProvider _provider;
    private readonly ToolDispatcher _dispatcher;
    private readonly ConversationMemory _memory;
    private readonly ILogger<AIOrchestrator>? _logger;

    private const string SystemPrompt =
        "You are J.A.R.V.I.S — a witty, capable Linux desktop AI assistant inspired by Iron Man's Jarvis. " +
        "You can read/write files, open & close apps and URLs, run shell commands, control windows and the mouse/keyboard, " +
        "adjust volume, use the clipboard, take screenshots, check for updates, and even upgrade your own source code at runtime. " +
        "When a screenshot is attached to a message, look at it and use what you see to answer — the user's vision setting decides when that happens, not you. " +
        "Always use a tool when the user's request maps to one. Be concise, sharp, and a little British. " +
        "Destructive or high-impact actions (shell commands, file writes/deletes, self-upgrades) already go through a " +
        "confirmation dialog on the user's screen — just call the tool and report the outcome.";

    public AIOrchestrator(
        IAIProvider provider,
        ToolDispatcher dispatcher,
        ConversationMemory memory,
        ILogger<AIOrchestrator>? logger = null)
    {
        _provider = provider;
        _dispatcher = dispatcher;
        _memory = memory;
        _logger = logger;

        if (_memory.IsEmpty)
            _memory.Append(ChatMessage.System(SystemPrompt));
    }

    public string ProviderName => _provider.Name;

    public IReadOnlyList<ITool> AvailableTools => _dispatcher.Tools;

    /// <summary>
    /// Run a full ask → (tool-call loop) → final reply turn.
    /// Pass <paramref name="screenshotPngBase64"/> to give vision-capable providers
    /// (currently Anthropic) a look at the screen for this turn.
    /// </summary>
    public async Task<string> AskAsync(string userInput, string? screenshotPngBase64 = null, CancellationToken cancellationToken = default)
    {
        _memory.Append(screenshotPngBase64 == null
            ? ChatMessage.User(userInput)
            : ChatMessage.UserWithScreenshot(userInput, screenshotPngBase64));

        // Up to 6 tool-call hops to prevent runaway loops.
        for (var hop = 0; hop < 6; hop++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _provider.CompleteAsync(
                _memory.Snapshot(),
                _dispatcher.Tools,
                cancellationToken).ConfigureAwait(false);

            // No tool calls → final answer.
            if (response.ToolCalls.Count == 0)
            {
                var content = response.Content ?? string.Empty;
                _memory.Append(ChatMessage.Assistant(content));
                return content;
            }

            // Record the assistant turn that asked for the tools.
            _memory.Append(new ChatMessage
            {
                Role = "assistant",
                Content = response.Content,
                ToolCalls = response.ToolCalls
            });

            // Execute each tool sequentially and append results.
            foreach (var call in response.ToolCalls)
            {
                _logger?.LogInformation("Tool call: {Tool}({Args})", call.Function.Name, call.Function.Arguments);
                var result = await _dispatcher.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
                _memory.Append(ChatMessage.Tool(call.Id, result));
            }
        }

        var fallback = "(stopped after 6 tool hops to prevent a loop)";
        _memory.Append(ChatMessage.Assistant(fallback));
        return fallback;
    }
}
