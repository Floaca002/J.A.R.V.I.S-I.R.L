using System.Text.Json.Serialization;

namespace Jarvis.Core.Config;

/// <summary>
/// Strongly-typed configuration bound from appsettings.json.
/// </summary>
public sealed class JarvisConfig
{
    public AIConfig AI { get; set; } = new();
    public VoiceConfig Voice { get; set; } = new();
    public GitHubConfig GitHub { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public MemoryConfig Memory { get; set; } = new();
}

public sealed class AIConfig
{
    public string DefaultProvider { get; set; } = "Groq";
    public GroqConfig Groq { get; set; } = new();
    public OllamaConfig Ollama { get; set; } = new();
}

public sealed class GroqConfig
{
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public double Temperature { get; set; } = 0.4;
    public int MaxTokens { get; set; } = 2048;
}

public sealed class OllamaConfig
{
    public string Endpoint { get; set; } = "http://localhost:11434/api/chat";
    public string Model { get; set; } = "llama3.2";
    public double Temperature { get; set; } = 0.4;
}

public sealed class VoiceConfig
{
    public bool Enabled { get; set; } = true;
    public string WakeWord { get; set; } = "jarvis";
    public string Voice { get; set; } = "Microsoft David Desktop";
    public int Rate { get; set; } = 0;
    public int Volume { get; set; } = 100;
}

public sealed class GitHubConfig
{
    public string Repo { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public sealed class SecurityConfig
{
    public bool RequireConfirmationForCommands { get; set; } = true;
    public bool RequireConfirmationForFileWrites { get; set; } = true;
    public bool AllowSelfUpgrade { get; set; } = true;
    public string SandboxRoot { get; set; } = string.Empty;
}

public sealed class MemoryConfig
{
    public int MaxMessages { get; set; } = 40;
    public string PersistPath { get; set; } = string.Empty;
}
