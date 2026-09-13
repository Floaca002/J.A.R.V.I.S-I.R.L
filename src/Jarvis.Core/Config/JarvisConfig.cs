using System.Text.Json.Serialization;

namespace Jarvis.Core.Config;

/// <summary>
/// Strongly-typed configuration bound from appsettings.json.
/// </summary>
public sealed class JarvisConfig
{
    public AIConfig AI { get; set; } = new();
    public VoiceConfig Voice { get; set; } = new();
    public VisionConfig Vision { get; set; } = new();
    public GitHubConfig GitHub { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public MemoryConfig Memory { get; set; } = new();
}

public sealed class AIConfig
{
    public string DefaultProvider { get; set; } = "Groq";
    public GroqConfig Groq { get; set; } = new();
    public OllamaConfig Ollama { get; set; } = new();
    public AnthropicConfig Anthropic { get; set; } = new();
}

/// <summary>
/// Claude (Anthropic) API — a paid Claude.ai/Pro/Max chat subscription does NOT
/// include this: it needs its own API key + prepaid credits from
/// https://console.anthropic.com, billed separately per token.
/// </summary>
public sealed class AnthropicConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-opus-5";
    public int MaxTokens { get; set; } = 4096;
}

public sealed class GroqConfig
{
    public string Endpoint { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public double Temperature { get; set; } = 0.4;

    /// <summary>
    /// Kept modest on purpose: Groq's free/on-demand tier caps every gpt-oss/qwen chat
    /// model at a flat 8K tokens-per-minute regardless of size, and rate limiters
    /// typically reserve the full MaxTokens as "requested" capacity whether or not a
    /// reply actually uses it. A smaller reservation leaves more of that tight budget
    /// for the system prompt + tool catalog Jarvis resends on every single turn.
    /// </summary>
    public int MaxTokens { get; set; } = 800;
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

    /// <summary>espeak-ng "-v" voice, used unless PiperModelPath is set and Piper is installed.</summary>
    public string EspeakVoice { get; set; } = "en-gb+m3";

    /// <summary>Path to a downloaded Piper .onnx voice model for natural-sounding speech — see README.</summary>
    public string PiperModelPath { get; set; } = string.Empty;

    /// <summary>Path to a whisper.cpp ggml model (e.g. ggml-base.en.bin) for push-to-talk voice input — see README.</summary>
    public string WhisperModelPath { get; set; } = string.Empty;

    public int Rate { get; set; } = 0;
    public int Volume { get; set; } = 100;
}

/// <summary>
/// When AttachScreenToEveryMessage is on, Jarvis captures a fresh screenshot and
/// sends it along with every user message (vision-capable providers only — currently
/// Anthropic). This costs a vision-sized chunk of input tokens on every single turn,
/// so it's opt-in.
/// </summary>
public sealed class VisionConfig
{
    public bool AttachScreenToEveryMessage { get; set; } = false;
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
