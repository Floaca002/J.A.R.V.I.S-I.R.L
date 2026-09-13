using System.Text.Json.Nodes;
using Jarvis.Core.Config;
using Jarvis.Core.Security;
using Jarvis.Core.Tools;
using Jarvis.SelfUpgrade;

namespace Jarvis.UI.Tools;

/// <summary>
/// LLM-driven self-upgrade tool. The LLM passes generated C# source for a new ITool,
/// and Jarvis compiles + loads it at runtime.
///
/// Lives in Jarvis.UI (not Jarvis.Core) because it needs both Jarvis.Core.Tools.ITool
/// and Jarvis.SelfUpgrade.SelfUpgradeEngine — and Jarvis.SelfUpgrade already depends on
/// Jarvis.Core, so Core cannot depend back on SelfUpgrade without a circular reference.
/// </summary>
public sealed class UpgradeSelfTool(SelfUpgradeEngine engine, SecurityConfig security, IConfirmationService confirm) : ITool
{
    public string Name => "upgrade_self";
    public string Description =>
        "Generate and hot-load a new C# tool implementing Jarvis.Core.Tools.ITool. " +
        "Pass the full source code of a public sealed class that implements ITool. " +
        "Use only standard .NET 8 APIs. Namespace must be 'Jarvis.Dynamic'.";

    public string JsonSchema => """
    {
      "type": "object",
      "properties": {
        "tool_name":   { "type": "string", "description": "snake_case name the new tool will register under." },
        "source_code": { "type": "string", "description": "Full C# source of the ITool implementation." }
      },
      "required": ["tool_name", "source_code"]
    }
    """;

    public async Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var toolName = args?["tool_name"]?.GetValue<string>() ?? "anon";
        var source = args?["source_code"]?.GetValue<string>() ?? throw new ArgumentException("'source_code' required");

        if (security.RequireConfirmationForCommands)
        {
            var approved = await confirm.ConfirmAsync(
                $"Self-upgrade requested: '{toolName}'",
                $"Jarvis wants to compile and hot-load a new tool called '{toolName}'.\n\n{source}",
                ct).ConfigureAwait(false);
            if (!approved) return "Cancelled by user.";
        }

        var res = engine.InstallNewTool(source, toolName);
        return $"upgrade success={res.Success}, message={res.Message}";
    }
}

public sealed class RevertLastUpgradeTool(SelfUpgradeEngine engine) : ITool
{
    public string Name => "revert_last_upgrade";
    public string Description => "Unregister the most recently self-installed dynamic tool (panic button for a bad upgrade).";
    public string JsonSchema => """{ "type": "object", "properties": {} }""";

    public Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var reverted = engine.RevertLast();
        return Task.FromResult(reverted == null
            ? "No dynamic upgrades to revert."
            : $"Reverted '{reverted}'. It has been unregistered (source kept on disk for audit).");
    }
}

public sealed class CheckForUpdatesTool(GitHubUpdater updater) : ITool
{
    public string Name => "check_for_updates";
    public string Description => "Check GitHub for a newer released version of Jarvis and report it (does not install).";
    public string JsonSchema => """{ "type": "object", "properties": {} }""";

    public async Task<string> RunAsync(JsonNode? args, CancellationToken ct = default)
    {
        var release = await updater.GetLatestReleaseAsync().ConfigureAwait(false);
        if (release == null)
            return "No GitHub repo configured, or no releases found.";
        return $"Latest release: {release.Tag} ({release.Name}). " +
               (release.ZipUrl != null ? $"Download: {release.ZipUrl}" : "No zip asset attached to the release.");
    }
}
