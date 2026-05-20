using System.Text;

namespace Jarvis.SystemControl;

/// <summary>
/// File operations Jarvis can perform. All paths support `~` and `desktop` shortcuts.
/// </summary>
public sealed class FileSystemController
{
    public async Task<string> ReadFileAsync(string path, CancellationToken ct = default)
    {
        path = Resolve(path);
        return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
    }

    public async Task WriteFileAsync(string path, string content, bool append, CancellationToken ct = default)
    {
        path = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        if (append)
            await File.AppendAllTextAsync(path, content, ct).ConfigureAwait(false);
        else
            await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
    }

    public string ListDirectory(string path)
    {
        path = Resolve(path);
        var dir = new DirectoryInfo(path);
        if (!dir.Exists) return $"Directory not found: {path}";

        var sb = new StringBuilder();
        sb.AppendLine($"📁 {dir.FullName}");
        foreach (var d in dir.EnumerateDirectories().OrderBy(x => x.Name))
            sb.AppendLine($"  📁 {d.Name}/");
        foreach (var f in dir.EnumerateFiles().OrderBy(x => x.Name))
            sb.AppendLine($"  📄 {f.Name}  ({f.Length:N0} B)");
        return sb.ToString();
    }

    public void Delete(string path, bool recursive)
    {
        path = Resolve(path);
        if (File.Exists(path)) File.Delete(path);
        else if (Directory.Exists(path)) Directory.Delete(path, recursive);
        else throw new FileNotFoundException("Path not found", path);
    }

    public void CreateDirectory(string path)
    {
        path = Resolve(path);
        Directory.CreateDirectory(path);
    }

    private static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        var trimmed = path.Trim().Trim('"');

        if (trimmed.StartsWith("~"))
            trimmed = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                      + trimmed.Substring(1);

        if (trimmed.Equals("desktop", StringComparison.OrdinalIgnoreCase))
            trimmed = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        if (trimmed.StartsWith("desktop/", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("desktop\\", StringComparison.OrdinalIgnoreCase))
            trimmed = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                trimmed.Substring("desktop/".Length));

        return Environment.ExpandEnvironmentVariables(trimmed);
    }
}
