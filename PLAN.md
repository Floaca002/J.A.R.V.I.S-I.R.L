# J.A.R.V.I.S I.R.L. — Step-by-Step Build Plan

This document walks you through every phase of bringing J.A.R.V.I.S to life, from zero to a fully self-upgradable AI assistant on your Windows machine.

---

## Phase 0 — Prerequisites (one-time setup)

| # | Task | Time |
|---|------|------|
| 0.1 | Install **.NET 8 SDK** → https://dotnet.microsoft.com/download/dotnet/8.0 | 5 min |
| 0.2 | Install **Visual Studio 2022 Community** (free) with the *.NET desktop development* workload, OR use VS Code with the C# Dev Kit | 15 min |
| 0.3 | Install **Git for Windows** → https://git-scm.com/download/win | 3 min |
| 0.4 | Get a free **Groq API key** → https://console.groq.com/keys | 2 min |
| 0.5 | *(Optional)* Install **Ollama** for offline LLM → https://ollama.com/download | 5 min |
| 0.6 | *(Optional)* `ollama pull llama3.2` for local inference | 3 min |

---

## Phase 1 — Get the Code

```powershell
git clone https://github.com/Floaca002/J.A.R.V.I.S-I.R.L.git
cd J.A.R.V.I.S-I.R.L
```

Open `config/appsettings.json` and set:

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
    "WakeWord": "jarvis"
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

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/Jarvis.UI
```

A WPF window opens with the Iron Man HUD. Press the **mic** button or type and press Enter.

---

## Phase 3 — First Conversation

Try:

> *"Jarvis, what's the weather like in my command prompt?"*
> *"Open Notepad."*
> *"List the files on my desktop."*
> *"Create a folder called 'Stark Industries' on my desktop."*

Jarvis will:
1. Send your message + the tool catalog to Groq.
2. Receive a function-call response (e.g. `open_app("notepad")`).
3. Execute it via `Jarvis.SystemControl`.
4. Speak the result back through TTS.

---

## Phase 4 — Enable Voice Mode

Click the **🎤 wake** toggle in the HUD. Jarvis now listens continuously for the wake word **"Jarvis"**. After the wake word, your next sentence is captured, sent to the LLM, and answered out loud.

Voice stack used:
- **STT**: `System.Speech.Recognition` (built into Windows, free, offline)
- **TTS**: `System.Speech.Synthesis` (built into Windows, free, offline)
- Optional upgrade path: Whisper.cpp + Piper TTS (see `docs/UPGRADES.md`)

---

## Phase 5 — Self-Upgrade

You can tell Jarvis:

> *"Jarvis, upgrade yourself — add a new tool that tells me CPU temperature."*

What happens:
1. Jarvis asks the LLM to generate a new C# class implementing `ITool`.
2. `Jarvis.SelfUpgrade.CodeCompiler` compiles it with **Roslyn** at runtime.
3. The new tool is hot-loaded into the running process — no restart.
4. *(Optional)* `Jarvis.SelfUpgrade.GitHubUpdater` commits the new file to your repo.

You can also pull a full release upgrade:

> *"Jarvis, check for updates."*

This calls the GitHub Releases API, downloads the new build, and restarts itself into the new version.

> **⚠️ Safety:** Self-upgrades are sandboxed. Every generated change is shown to you with a diff and requires confirmation unless you disable `RequireConfirmationForCommands`.

---

## Phase 6 — Going Further

| Goal | How |
|---|---|
| Swap Groq for GPT-5.2 | Add `OpenAIProvider.cs` in `Jarvis.Core/AI/` |
| Run fully offline | Set `DefaultProvider: "Ollama"` |
| Add a new tool (e.g. Spotify) | Create class implementing `ITool` in `Jarvis.Core/Tools/` |
| Run as a system tray app | Build with `OutputType=WinExe` and minimize to tray |
| Auto-start with Windows | Add registry entry under `Run` |

---

## Architecture at a Glance

```
┌──────────────────────────────────────────────────────────┐
│                    YOUR WINDOWS PC                       │
│                                                          │
│  ┌────────────────────────────────────────────────┐     │
│  │  Jarvis.UI  (WPF Iron Man HUD)                 │     │
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
│  └─────────┘ └─────────┘ └───┬───┘ └─────────────┘      │
│                              │                           │
└──────────────────────────────┼───────────────────────────┘
                               │   HTTPS
              ┌────────────────┴─────────────────┐
              │   Cloud LLM Brain (Groq API)     │
              │   llama-3.3-70b-versatile        │
              └──────────────────────────────────┘
```

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full picture.
