# Security & Safety Notes

> Jarvis has the keys to your kingdom: filesystem, processes, shell, mouse/keyboard,
> and the ability to rewrite itself. Treat the configuration accordingly.

## Threat model (in scope)

- A malicious or hallucinating LLM choosing a destructive tool call.
- A self-upgrade producing broken or unsafe code.
- API key leakage from the repo or logs.

## Threat model (out of scope, by design)

- Multi-user / network attacks against the host. This is a single-user desktop app.
- An attacker with physical access to your unlocked machine.

## Built-in mitigations

| Risk | Mitigation |
|---|---|
| Accidental file deletion | `Security.RequireConfirmationForFileWrites` shows a real HUD-styled modal (`ConfirmDialog`) before every write/append/delete, naming the exact path. |
| Unwanted shell command | `Security.RequireConfirmationForCommands` shows the exact command line in a modal before `run_shell` executes. Runs also carry a 30-second timeout by default. |
| Tool-loop runaway | `AIOrchestrator` caps at 6 tool hops per user turn. |
| Bad dynamic code | Roslyn compiles into a collectible `AssemblyLoadContext`; failures don't crash Jarvis. `revert_last_upgrade` unregisters the most recent dynamic tool on demand. |
| API key leakage | `secrets.json` lives in `~/.config/JarvisIRL/`, never in the repo. `.gitignore` blocks it. The in-app Settings window writes there directly, so you never have to hand-edit `appsettings.json`. |
| Self-upgrade misuse | `Security.AllowSelfUpgrade` flag gates whether `upgrade_self` is even registered; when enabled, the same confirmation modal shows the full generated source before it is compiled. |
| Screen vision leaking to the cloud | `Vision.AttachScreenToEveryMessage` is **off by default**. When on, a fresh screenshot is sent with every message to whichever provider is configured — never written to disk, never persisted in `memory.json`. The HUD always shows a 👁 VISION indicator so you know it's on. Only turn this on with a provider you trust with what's on your screen. |
| Shell/tool commands running with your full user permissions | Jarvis doesn't sandbox `run_shell` or the automation tools beyond the confirmation modal — a command runs exactly as if you typed it yourself. Run Jarvis as your normal (non-root) user, never as root. |

All confirmation gating lives behind `Jarvis.Core.Security.IConfirmationService`. The Avalonia app wires up `AvaloniaConfirmationService` (a real modal); a headless host (tests, a future CLI) can use `AutoApproveConfirmationService` instead.

## Recommended posture

1. **Always keep `RequireConfirmationForCommands = true`** until you trust your prompts.
2. **Don't commit `appsettings.json` with a real key** — use env vars or `secrets.json`.
3. **Never run Jarvis as root.** Nothing about the design assumes elevated privileges, and `run_shell`/file tools inherit whatever permissions the process has.
4. **Review `~/.config/JarvisIRL/upgrades/` periodically** to audit dynamic code.

## Disabling capabilities

If you don't trust a tool category, simply don't register it in `App.axaml.cs`. For example, to disable shell:

```csharp
// dispatcher.Register(new RunShellTool(shell, Config.Security, confirmation));   // commented out
```
