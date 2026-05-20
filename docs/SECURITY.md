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
| Accidental file deletion | `Security.RequireConfirmationForFileWrites` shows a modal before write/delete. |
| Runaway shell command | `run_shell` enforces a 30-second timeout by default. |
| Tool-loop runaway | `AIOrchestrator` caps at 6 tool hops per user turn. |
| Bad dynamic code | Roslyn compiles into a collectible context; failures don't crash Jarvis. |
| API key leakage | `secrets.json` lives in `%AppData%`, never in the repo. `.gitignore` blocks it. |
| Self-upgrade misuse | `Security.AllowSelfUpgrade` flag, plus diff-preview modal. |

## Recommended posture

1. **Always keep `RequireConfirmationForCommands = true`** until you trust your prompts.
2. **Don't commit `appsettings.json` with a real key** — use env vars or `secrets.json`.
3. **Use a non-admin Windows account** for daily Jarvis use.
4. **Review `%AppData%/JarvisIRL/upgrades/` periodically** to audit dynamic code.

## Disabling capabilities

If you don't trust a tool category, simply don't register it in `App.xaml.cs`. For example, to disable shell:

```csharp
// dispatcher.Register(new RunShellTool(shell));   // commented out
```
