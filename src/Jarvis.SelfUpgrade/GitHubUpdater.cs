using Octokit;
using Jarvis.Core.Config;

namespace Jarvis.SelfUpgrade;

/// <summary>
/// Pulls the latest release zip from GitHub and stages it for install.
/// Optionally commits generated upgrade source files back to the repo.
/// </summary>
public sealed class GitHubUpdater
{
    private readonly GitHubConfig _cfg;
    private readonly GitHubClient _client;

    public GitHubUpdater(GitHubConfig cfg)
    {
        _cfg = cfg;
        _client = new GitHubClient(new ProductHeaderValue("Jarvis-IRL"));
        if (!string.IsNullOrWhiteSpace(cfg.Token))
            _client.Credentials = new Credentials(cfg.Token);
    }

    public async Task<ReleaseInfo?> GetLatestReleaseAsync()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Repo)) return null;
        var (owner, name) = SplitRepo(_cfg.Repo);
        var rel = await _client.Repository.Release.GetLatest(owner, name).ConfigureAwait(false);
        var asset = rel.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        return new ReleaseInfo(rel.TagName, rel.Name, asset?.BrowserDownloadUrl);
    }

    public async Task CommitUpgradeFileAsync(string pathInRepo, string content, string commitMessage)
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            throw new InvalidOperationException("GitHub token not configured.");

        var (owner, name) = SplitRepo(_cfg.Repo);
        try
        {
            var existing = await _client.Repository.Content.GetAllContents(owner, name, pathInRepo).ConfigureAwait(false);
            var sha = existing.First().Sha;
            await _client.Repository.Content.UpdateFile(owner, name, pathInRepo,
                new UpdateFileRequest(commitMessage, content, sha)).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            await _client.Repository.Content.CreateFile(owner, name, pathInRepo,
                new CreateFileRequest(commitMessage, content)).ConfigureAwait(false);
        }
    }

    private static (string owner, string name) SplitRepo(string slug)
    {
        var parts = slug.Split('/', 2);
        return (parts[0], parts[1]);
    }
}

public sealed record ReleaseInfo(string Tag, string Name, string? ZipUrl);
