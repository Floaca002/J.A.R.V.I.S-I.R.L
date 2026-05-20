# Self-Upgrade Mechanics

Jarvis can extend itself in two ways:

## 1. Runtime tool generation (hot)

Triggered when you say things like:
> *"Jarvis, add a tool that pings a host and returns the latency."*

The LLM is instructed (system prompt + `upgrade_self` tool schema) to emit a C# class implementing `Jarvis.Core.Tools.ITool`. The flow:

```
LLM → upgrade_self(tool_name, source_code)
        ↓
   CodeCompiler.Compile()           ← Roslyn
        ↓
   AssemblyLoadContext.LoadFromStream()
        ↓
   ToolDispatcher.Register(tool)    ← live in this process
        ↓
   Source persisted to %AppData%/JarvisIRL/upgrades/
```

The new tool is **immediately available** in the next conversation turn. No restart.

## 2. Full release upgrades (cold)

Triggered when you ask: *"Jarvis, check for updates."*
- `GitHubUpdater.GetLatestReleaseAsync()` queries the GitHub Releases API for the repo `Floaca002/J.A.R.V.I.S-I.R.L`.
- If a newer tag exists, Jarvis downloads the zip asset, extracts it next to the running exe, and launches a small updater stub that swaps the binaries and relaunches.

## Safety

- Every dynamic tool source is saved with a timestamp — full audit trail.
- Generated code runs in a **collectible** `AssemblyLoadContext` so a buggy tool can be unloaded.
- If `Security.RequireConfirmationForCommands == true`, the UI shows a diff modal before compiling.
- A panic phrase ("Jarvis, revert") unregisters the most recent dynamic tool.

## Pushing upgrades back to the repo

`GitHubUpdater.CommitUpgradeFileAsync(...)` can commit a generated tool source to a folder like `src/Jarvis.Core/Tools/Dynamic/` in your repo so subsequent users (or future clones on your other machines) inherit the upgrade.
