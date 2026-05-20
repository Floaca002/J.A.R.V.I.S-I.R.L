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
        "You are J.A.R.V.I.S — a witty, capable Windows desktop AI assistant inspired by Iron Man's Jarvis. " +
        "You can read & write files, open & close apps, run shell commands, take screenshots, and even upgrade your own source code. " +
        "Always use a tool when the user's request maps to one. Be concise, sharp, and a little British. " +
        "If a request is destructive (delete, overwrite, shutdown) confirm first unless the user has clearly authorized it.";

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
    /// </summary>
    public async Task<string> AskAsync(string userInput, CancellationToken cancellationToken = default)
    {
        _memory.Append(ChatMessage.User(userInput));

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
