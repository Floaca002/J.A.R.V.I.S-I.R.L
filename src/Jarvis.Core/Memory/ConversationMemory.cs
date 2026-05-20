using System.Text.Json;
using Jarvis.Core.AI;
using Jarvis.Core.Config;

namespace Jarvis.Core.Memory;

/// <summary>
/// Rolling conversation buffer. Persists to disk so Jarvis remembers across restarts.
/// </summary>
public sealed class ConversationMemory
{
    private readonly List<ChatMessage> _messages = new();
    private readonly int _maxMessages;
    private readonly string _persistPath;
    private readonly object _gate = new();

    public ConversationMemory(MemoryConfig cfg)
    {
        _maxMessages = Math.Max(8, cfg.MaxMessages);
        _persistPath = string.IsNullOrWhiteSpace(cfg.PersistPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JarvisIRL", "memory.json")
            : cfg.PersistPath;

        Directory.CreateDirectory(Path.GetDirectoryName(_persistPath)!);
        Load();
    }

    public bool IsEmpty
    {
        get { lock (_gate) return _messages.Count == 0; }
    }

    public void Append(ChatMessage msg)
    {
        lock (_gate)
        {
            _messages.Add(msg);
            // Always keep the leading system message, trim the oldest user/assistant pairs.
            if (_messages.Count > _maxMessages)
            {
                var keepSystem = _messages[0].Role == "system";
                var overflow = _messages.Count - _maxMessages;
                _messages.RemoveRange(keepSystem ? 1 : 0, overflow);
            }
            Persist();
        }
    }

    public IReadOnlyList<ChatMessage> Snapshot()
    {
        lock (_gate) return _messages.ToArray();
    }

    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
            Persist();
        }
    }

    private void Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_messages, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(_persistPath, json);
        }
        catch { /* memory persistence is best-effort */ }
    }

    private void Load()
    {
        if (!File.Exists(_persistPath)) return;
        try
        {
            var json = File.ReadAllText(_persistPath);
            var data = JsonSerializer.Deserialize<List<ChatMessage>>(json);
            if (data != null) _messages.AddRange(data);
        }
        catch { /* corrupt file — start fresh */ }
    }
}
