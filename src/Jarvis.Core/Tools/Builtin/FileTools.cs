using System.Text.Json.Nodes;
using Jarvis.SystemControl;

namespace Jarvis.Core.Tools.Builtin;

public sealed class ReadFileTool(FileSystemController fs) : ITool
{
    public string Name => "read_file";
    public string Description => "Read the full text content of a file on the user's machine.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "Absolute or user-relative path to the file." }
      },
      "required": ["path"]
    }
    """;

    public async Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var path = args?["path"]?.GetValue<string>() ?? throw new ArgumentException("'path' required");
        var content = await fs.ReadFileAsync(path, ct).ConfigureAwait(false);
        return content;
    }
}

public sealed class WriteFileTool(FileSystemController fs) : ITool
{
    public string Name => "write_file";
    public string Description => "Create or overwrite a text file with the supplied content.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "path":    { "type": "string", "description": "Destination path." },
        "content": { "type": "string", "description": "Text to write." },
        "append":  { "type": "boolean", "description": "Append instead of overwrite", "default": false }
      },
      "required": ["path", "content"]
    }
    """;

    public async Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var path = args?["path"]?.GetValue<string>() ?? throw new ArgumentException("'path' required");
        var content = args?["content"]?.GetValue<string>() ?? string.Empty;
        var append = args?["append"]?.GetValue<bool>() ?? false;
        await fs.WriteFileAsync(path, content, append, ct).ConfigureAwait(false);
        return $"OK — {(append ? "appended" : "wrote")} {content.Length} chars to {path}";
    }
}

public sealed class ListDirectoryTool(FileSystemController fs) : ITool
{
    public string Name => "list_directory";
    public string Description => "List files and folders in a directory.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "Directory path. Use '~' or 'desktop' shortcuts." }
      },
      "required": ["path"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var path = args?["path"]?.GetValue<string>() ?? throw new ArgumentException("'path' required");
        return Task.FromResult(fs.ListDirectory(path));
    }
}

public sealed class DeletePathTool(FileSystemController fs) : ITool
{
    public string Name => "delete_path";
    public string Description => "Delete a file or folder. Destructive — confirm with user first.";
    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "path":      { "type": "string" },
        "recursive": { "type": "boolean", "default": false }
      },
      "required": ["path"]
    }
    """;

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var path = args?["path"]?.GetValue<string>() ?? throw new ArgumentException("'path' required");
        var recursive = args?["recursive"]?.GetValue<bool>() ?? false;
        fs.Delete(path, recursive);
        return Task.FromResult($"Deleted {path}");
    }
}
