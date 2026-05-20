using Jarvis.Core.Tools;

namespace Jarvis.Core.AI;

/// <summary>
/// Abstraction over an LLM backend. Implementations: Groq (cloud), Ollama (local).
/// </summary>
public interface IAIProvider
{
    /// <summary>Name shown in UI / logs.</summary>
    string Name { get; }

    /// <summary>
    /// Send a chat completion request with the supplied messages and optional tool schemas.
    /// </summary>
    Task<ChatResponse> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ITool> tools,
        CancellationToken cancellationToken = default);
}
