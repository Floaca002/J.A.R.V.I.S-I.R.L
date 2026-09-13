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
   Source persisted to ~/.config/JarvisIRL/upgrades/
```

The new tool is **immediately available** in the next conversation turn. No restart.

## 2. Full release upgrades (cold) — check only, for now

Triggered when you ask: *"Jarvis, check for updates."*
- The `check_for_updates` tool calls `GitHubUpdater.GetLatestReleaseAsync()`, which queries the GitHub Releases API for the repo configured under `GitHub.Repo`.
- Jarvis reports the latest tag/name and the release zip's download URL in the chat.

Downloading, extracting, and swapping the running binaries automatically is **not** wired up yet — replacing files an app is currently running from is inherently risky to get right blind, so today's `check_for_updates` stops at "here's what's new and where to get it," and installing a new release is a manual step (download the zip, close Jarvis, extract over the install directory, relaunch). If you want to automate that last mile, `GitHubUpdater` already gives you the release URL to build on.

## Safety

- Every dynamic tool source is saved with a timestamp — full audit trail (`~/.config/JarvisIRL/upgrades/`).
- Generated code runs in a **collectible** `AssemblyLoadContext` so a buggy tool can, in principle, be unloaded.
- If `Security.RequireConfirmationForCommands == true`, the UI shows the full generated source in a modal and waits for you to authorize it before compiling.
- The `revert_last_upgrade` tool (say "Jarvis, revert your last upgrade") unregisters the most recently installed dynamic tool. The source file is kept on disk for audit even after reverting.

## Pushing upgrades back to the repo

`GitHubUpdater.CommitUpgradeFileAsync(...)` can commit a generated tool source to a folder like `src/Jarvis.Core/Tools/Dynamic/` in your repo so subsequent users (or future clones on your other machines) inherit the upgrade.
