# J.A.R.V.I.S I.R.L. — Step-by-Step Build Plan

This document walks you through every phase of bringing J.A.R.V.I.S to life, from zero to a fully self-upgradable AI assistant on your Linux machine.

---

## Phase 0 — Prerequisites (one-time setup)

| # | Task | Time |
|---|------|------|
| 0.1 | Install the **.NET SDK** → `sudo pacman -S dotnet-sdk` (CachyOS/Arch, tracks the current release) or https://dotnet.microsoft.com/download | 5 min |
| 0.2 | Install a C# editor — VS Code with the C# Dev Kit works well; JetBrains Rider is another good option | 15 min |
| 0.3 | Get a free **Groq API key** → https://console.groq.com/keys | 2 min |
| 0.4 | *(Optional)* Install **Ollama** for offline LLM → https://ollama.com/download | 5 min |
| 0.5 | *(Optional)* `ollama pull llama3.2` for local inference | 3 min |
| 0.6 | *(Optional)* Install desktop CLI tools for full functionality — see [README's Linux platform notes](README.md#linux-platform-notes) | 5 min |

---

## Phase 1 — Get the Code

```bash
git clone https://github.com/Floaca002/J.A.R.V.I.S-I.R.L.git
cd J.A.R.V.I.S-I.R.L
```

Once it's running you can configure everything from the **⚙ SETTINGS** window in the app (saved to `~/.config/JarvisIRL/secrets.json`). Or, to configure before the first run, open `config/appsettings.json` and set:

```json
{
  "AI": {
    "DefaultProvider": "Groq",
    "Groq": {
      "ApiKey": "gsk_YOUR_KEY_HERE",
      "Model": "llama-3.3-70b-versatile"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    }
  },
  "Voice": {
    "Enabled": true,
    "EspeakVoice": "en-gb+m3"
  },
  "GitHub": {
    "Repo": "Floaca002/J.A.R.V.I.S-I.R.L",
    "Token": ""
  },
  "Security": {
    "RequireConfirmationForCommands": true,
    "AllowSelfUpgrade": true
  }
}
```

---

## Phase 2 — Build & Run

```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/Jarvis.UI
```

An Avalonia window opens with the Iron Man HUD. Press the **mic** button or type and press Enter.

---

## Phase 3 — First Conversation

Try:

> *"What processes are using the most memory?"*
> *"Open Firefox."*
> *"List the files on my desktop."*
> *"Create a folder called 'Stark Industries' in my home directory."*

Jarvis will:
1. Send your message + the tool catalog to Groq.
2. Receive a function-call response (e.g. `open_app("firefox")`).
3. Execute it via `Jarvis.SystemControl`.
4. Speak the result back through TTS (if `espeak-ng` or Piper is installed).

---

## Phase 4 — Enable Voice Mode

Click the **🎤 mic** button in the HUD to start recording, click it again to stop — Jarvis transcribes what you said with whisper.cpp and sends it as your message. This is push-to-talk, not always-listening: there's no lightweight, verifiable "wake word" story on Linux without pulling in a full VAD/wake-word model, so Jarvis doesn't fake one.

Voice stack used:
- **STT**: whisper.cpp (`whisper-cli`) + `arecord`/`parecord` to capture the microphone — needs a model downloaded separately (see README)
- **TTS**: `espeak-ng` out of the box (robotic but zero setup), or Piper for a natural-sounding voice (needs a downloaded model)

---

## Phase 5 — Self-Upgrade

You can tell Jarvis:

> *"Jarvis, upgrade yourself — add a new tool that tells me CPU temperature."*

What happens:
1. Jarvis asks the LLM to generate a new C# class implementing `ITool`.
2. `Jarvis.SelfUpgrade.CodeCompiler` compiles it with **Roslyn** at runtime.
3. The new tool is hot-loaded into the running process — no restart.
4. *(Optional)* `Jarvis.SelfUpgrade.GitHubUpdater` commits the new file to your repo.

You can also check for a newer release:

> *"Jarvis, check for updates."*

This calls the GitHub Releases API and tells you the latest tag and download link — installing it is currently a manual step (see [`SELF_UPGRADE.md`](docs/SELF_UPGRADE.md)).

If a self-installed tool misbehaves:

> *"Jarvis, revert your last upgrade."*

> **⚠️ Safety:** Every `run_shell`, file write/delete, and `upgrade_self` call shows you a HUD modal with the exact command/path/source and waits for you to authorize it, unless you disable the matching `Security.RequireConfirmation*` flag.

---

## Phase 6 — Going Further

| Goal | How |
|---|---|
| Swap Groq for GPT-5.2 | Add `OpenAIProvider.cs` in `Jarvis.Core/AI/` |
| Run fully offline | Set `DefaultProvider: "Ollama"` |
| Add a new tool (e.g. Spotify) | Create class implementing `ITool` in `Jarvis.Core/Tools/` |
| Auto-start with your session | Add a `.desktop` file to `~/.config/autostart/` |
| Package for your distro | `dotnet publish -r linux-x64 --self-contained` (see `BUILD_INSTRUCTIONS.md`), then wrap in a PKGBUILD/AppImage/Flatpak as you prefer |

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────────────────┐
│                    YOUR LINUX DESKTOP                    │
│                                                          │
│  ┌────────────────────────────────────────────────┐     │
│  │  Jarvis.UI  (Avalonia Iron Man HUD)            │     │
│  │  • Chat • Voice • Status • Settings            │     │
│  └────────────┬───────────────────────────────────┘     │
│               │                                          │
│  ┌────────────┴───────────────────────────────────┐     │
│  │  Jarvis.Core  (Orchestrator)                   │     │
│  │  • AI Provider abstraction                     │     │
│  │  • Tool registry + dispatcher                  │     │
│  │  • Conversation memory                         │     │
│  └────┬──────────┬──────────┬─────────┬───────────┘     │
│       │          │          │         │                  │
│  ┌────┴────┐ ┌───┴─────┐ ┌──┴────┐ ┌──┴──────────┐      │
│  │ Voice   │ │ System  │ │ Self  │ │ Memory      │      │
│  │ TTS/STT │ │ Control │ │Upgrade│ │ (JSON)      │      │
│  │(CLI tool│ │(CLI tool│ │Roslyn │ │             │      │
│  │ chains) │ │ chains) │ │       │ │             │      │
│  └─────────┘ └─────────┘ └───┬───┘ └─────────────┘      │
│                              │                           │
└──────────────────────────────┼───────────────────────────┘
                               │   HTTPS
              ┌────────────────┴─────────────────┐
              │   Cloud LLM Brain (Groq/Claude)  │
              └──────────────────────────────────┘
```

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full picture.
