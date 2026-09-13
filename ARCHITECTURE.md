# J.A.R.V.I.S I.R.L. — Full Visual Architecture

This document is the **complete architectural blueprint** for the system: layers, data flow, module responsibilities, threading model, and self-upgrade mechanics.

---

## 1. High-Level Topology

```
                    ┌───────────────────────────────────────┐
                    │           ☁️  CLOUD (Groq)             │
                    │   llama-3.3-70b-versatile             │
                    │   OpenAI-compatible /v1/chat/...      │
                    └──────────────┬────────────────────────┘
                                   │  HTTPS + function-calling JSON
                                   │
┌──────────────────────────────────┼───────────────────────────────────┐
│  💻  YOUR WINDOWS PC             │                                   │
│                                  ▼                                   │
│   ┌──────────────────────────────────────────────────────────────┐   │
│   │                    Jarvis.UI  (WPF .NET 8)                   │   │
│   │   ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌──────────────┐    │   │
│   │   │  HUD    │  │  Chat   │  │  Voice  │  │   Settings   │    │   │
│   │   │  View   │  │  View   │  │  Toggle │  │     View     │    │   │
│   │   └─────────┘  └─────────┘  └─────────┘  └──────────────┘    │   │
│   └──────────────────────────────┬───────────────────────────────┘   │
│                                  │ MVVM bindings                     │
│   ┌──────────────────────────────▼───────────────────────────────┐   │
│   │                  Jarvis.Core  (Orchestrator)                 │   │
│   │                                                              │   │
│   │   ┌────────────────┐    ┌────────────────────────────────┐   │   │
│   │   │ AIOrchestrator │◄──►│  IAIProvider                   │   │   │
│   │   │ • routing      │    │   ├── GroqProvider (default)   │   │   │
│   │   │ • retry        │    │   └── OllamaProvider (local)   │   │   │
│   │   └───────┬────────┘    └────────────────────────────────┘   │   │
│   │           │                                                  │   │
│   │   ┌───────▼────────┐    ┌────────────────────────────────┐   │   │
│   │   │ ToolDispatcher │◄──►│  ITool registry (auto-discov.) │   │   │
│   │   │                │    │   FileTools, AppTools, Shell,  │   │   │
│   │   └───────┬────────┘    │   ScreenshotTool, UpgradeTool  │   │   │
│   │           │             └────────────────────────────────┘   │   │
│   │   ┌───────▼────────────┐                                     │   │
│   │   │ ConversationMemory │  (JSON-backed, rolling window)      │   │
│   │   └────────────────────┘                                     │   │
│   └──────┬──────────────┬──────────────┬──────────────┬──────────┘   │
│          │              │              │              │              │
│   ┌──────▼────┐  ┌──────▼──────┐  ┌───▼──────┐  ┌────▼───────────┐   │
│   │  Voice    │  │  System     │  │   Self   │  │  Config        │   │
│   │  TTS/STT  │  │  Control    │  │  Upgrade │  │  appsettings   │   │
│   │           │  │  Files/Apps │  │  Roslyn  │  │  + Secrets     │   │
│   │           │  │  Shell/Auto │  │  GitHub  │  │                │   │
│   └───────────┘  └─────────────┘  └──────────┘  └────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 2. Project / Assembly Layout

```
JARVIS.IRL.sln
│
├── src/
│   ├── Jarvis.UI                  [WPF .exe — entry point]
│   │   ├── App.xaml(.cs)
│   │   ├── MainWindow.xaml(.cs)
│   │   ├── Views/                 [ChatView, HudView, SettingsView]
│   │   ├── ViewModels/            [MVVM glue]
│   │   ├── Styles/JarvisTheme.xaml
│   │   └── Converters/
│   │
│   ├── Jarvis.Core                [class library — brain]
│   │   ├── AI/                    [IAIProvider, Groq, Ollama, Anthropic, Orchestrator]
│   │   ├── Tools/                 [ITool + built-in tools]
│   │   ├── Commands/              [ToolDispatcher]
│   │   ├── Memory/                [ConversationMemory]
│   │   ├── Security/              [IConfirmationService]
│   │   └── Config/                [JarvisConfig, settings binding]
│   │
│   ├── Jarvis.SystemControl       [class library — hands & feet]
│   │   ├── FileSystemController.cs
│   │   ├── AppController.cs
│   │   ├── ShellExecutor.cs
│   │   └── AutomationController.cs
│   │
│   ├── Jarvis.Voice               [class library — ears & mouth]
│   │   ├── TextToSpeech.cs        [System.Speech.Synthesis]
│   │   └── SpeechToText.cs        [System.Speech.Recognition + wake word]
│   │
│   └── Jarvis.SelfUpgrade         [class library — DNA editor]
│       ├── SelfUpgradeEngine.cs   [orchestrates the whole upgrade flow]
│       ├── CodeCompiler.cs        [Roslyn — compiles C# at runtime]
│       └── GitHubUpdater.cs       [pulls/pushes via Octokit]
│
├── config/
│   └── appsettings.json
└── docs/
    └── diagrams/
```

**Dependency graph (one-way, no cycles):**

```
Jarvis.UI ──► Jarvis.Core ──► Jarvis.SystemControl
                          ──► Jarvis.Voice
                          ──► Jarvis.SelfUpgrade
```

---

## 3. Request Lifecycle (text or voice)

```
┌──────┐     ┌────────────┐     ┌─────────────────┐     ┌──────────┐
│ User │────►│ Jarvis.UI  │────►│  AIOrchestrator │────►│ GroqProv │
│      │     │  ChatView  │     │  build payload  │     │ HTTP POST│
└──────┘     └────────────┘     │  + tool catalog │     └────┬─────┘
   ▲                            └────────┬────────┘          │
   │                                     │                   ▼
   │                                     │           ┌───────────────┐
   │                                     │           │  Groq Cloud   │
   │           ┌────────────┐            │           │   LLM reply   │
   │           │   TTS      │◄───────────┤           └───────┬───────┘
   │           └────────────┘            │                   │
   │                                     ▼                   │
   │                            ┌────────────────┐           │
   └────────────────────────────│  Render reply  │◄──────────┘
                                │   + execute    │
                                │   tool calls   │
                                └────────┬───────┘
                                         │
                              ┌──────────┼──────────┐
                              ▼          ▼          ▼
                         FileTools   AppTools   ShellExec
```

**Step-by-step:**

1. User types or speaks → `MainViewModel.SendMessageAsync(text)`.
2. `AIOrchestrator.Ask(text, memory, tools)`:
   - Builds an OpenAI-format payload (Groq is OpenAI-compatible).
   - Injects the registered tool schemas as `tools: [...]`.
3. Provider POSTs to `https://api.groq.com/openai/v1/chat/completions`.
4. Response is parsed:
   - If `tool_calls` present → `ToolDispatcher` executes each tool, results are looped back to the LLM as `role: tool` messages, until a final assistant message is produced.
   - Otherwise → final answer.
5. Final text is rendered in UI + spoken via `TextToSpeech.Speak()`.
6. Memory is appended to disk (`%AppData%/JarvisIRL/memory.json`).

---

## 4. Tool / Function-Call System

```
                  ┌──────────────────────────────────────┐
                  │           ITool interface            │
                  │  Name, Description, Schema, Run()    │
                  └──────────────────┬───────────────────┘
                                     │ implemented by
       ┌─────────────┬───────────────┼──────────────┬─────────────┐
       ▼             ▼               ▼              ▼             ▼
   ReadFile     WriteFile        OpenApp        RunShell       Upgrade
   ListDir      DeleteFile      KillApp        Screenshot      Self
                                                                Tool
```

**Built-in tools** (all live in `Jarvis.Core/Tools/`):

| Tool name | Backed by | Description |
|---|---|---|
| `read_file` | `FileSystemController` | Read a text file |
| `write_file` | `FileSystemController` | Create/overwrite a file |
| `list_directory` | `FileSystemController` | List files & folders |
| `delete_path` | `FileSystemController` | Delete file/folder |
| `open_app` | `AppController` | Launch an .exe or Start-menu item |
| `close_app` | `AppController` | Kill a process by name |
| `list_processes` | `AppController` | Snapshot of running processes |
| `run_shell` | `ShellExecutor` | Run a PowerShell command |
| `take_screenshot` | `AutomationController` | Capture primary monitor |
| `mouse_click` / `type_text` | `AutomationController` | Win32 SendInput |
| `upgrade_self` | `SelfUpgradeEngine` | Generate + compile + load new tool |
| `update_from_github` | `GitHubUpdater` | Pull latest release & restart |

Each tool publishes a **JSON Schema** consumed by Groq for native function-calling.

---

## 5. Self-Upgrade Pipeline

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│   "Jarvis, add a tool that returns the current CPU temperature"      │
│                                                                      │
└────────────────────────────────┬─────────────────────────────────────┘
                                 │
                                 ▼
                   ┌──────────────────────────────┐
                   │  LLM produces:               │
                   │  1) Tool spec (JSON)         │
                   │  2) C# source for the tool   │
                   └──────────────┬───────────────┘
                                  ▼
                   ┌──────────────────────────────┐
                   │  Diff preview shown to user  │
                   │      [Approve] [Reject]      │
                   └──────────────┬───────────────┘
                                  ▼ approved
                   ┌──────────────────────────────┐
                   │  CodeCompiler (Roslyn)       │
                   │  • parse & validate          │
                   │  • compile to in-memory DLL  │
                   │  • load with AssemblyLoadCtx │
                   └──────────────┬───────────────┘
                                  ▼
                   ┌──────────────────────────────┐
                   │  Tool registered live in     │
                   │  ToolDispatcher              │
                   └──────────────┬───────────────┘
                                  ▼ optional
                   ┌──────────────────────────────┐
                   │  GitHubUpdater commits .cs   │
                   │  to repo → next clone gets   │
                   │  it permanently              │
                   └──────────────────────────────┘
```

**Key safety controls:**
- Every generated change shows a diff in a modal — the user must approve.
- Compilation runs in a `collectible` `AssemblyLoadContext` so failures don't crash Jarvis.
- All upgrades are logged to `%AppData%/JarvisIRL/upgrades.log`.
- A "panic rollback" command restores the last working assembly snapshot.

---

## 6. Voice Pipeline

```
🎤 Mic ─► System.Speech.Recognition ─► WakeWordDetector
                                            │
                                            ▼  match "jarvis"
                                       Continuous mode (5s window)
                                            │
                                            ▼ recognized text
                                    AIOrchestrator.Ask(...)
                                            │
                                            ▼ reply
                                    System.Speech.Synthesis ─► 🔊 Speakers
```

- **Latency target**: < 1.5 s wake-word → response start.
- **No cloud voice**: all STT/TTS runs locally on Windows — zero cost.
- **Future**: swap in Whisper.cpp + Piper TTS for higher quality.

---

## 7. Threading Model

| Thread | Purpose |
|---|---|
| UI (Dispatcher) | All XAML bindings, animations |
| `Task.Run` workers | HTTP calls, tool execution |
| Speech recognition thread | Owned by `SpeechRecognitionEngine`, marshals back to UI |
| Roslyn compile | Background, cancellable |

UI updates from background threads go through `Application.Current.Dispatcher.InvokeAsync(...)`.

---

## 8. Configuration & Secrets

```
config/appsettings.json   ← committed, no secrets
%AppData%/JarvisIRL/secrets.json ← per-machine, API keys, GitHub PAT
```

`JarvisConfig` merges both; user secrets override committed defaults.

---

## 9. Logging & Observability

- **Serilog** writes to `%AppData%/JarvisIRL/logs/jarvis-YYYYMMDD.log`.
- Every LLM request + tool call is logged with timing.
- A debug overlay (toggle with `F12`) shows the live log inside the HUD.

---

## 10. Roadmap (post-MVP)

1. Plugin marketplace (drop `.dll` into `plugins/`)
2. ~~Multi-modal: vision~~ — shipped, via `AnthropicProvider` + `Vision.AttachScreenToEveryMessage` (see README). Groq vision models are a possible future addition.
3. Local-only mode with Ollama + Whisper.cpp + Piper
4. Mobile companion (push notifications, remote commands)
5. Home automation bridge (Home Assistant)

---

*"Sometimes you gotta run before you can walk." — T. Stark*
