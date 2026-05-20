# J.A.R.V.I.S I.R.L.

> **Just A Rather Very Intelligent System — In Real Life**
> A self-upgradable Windows AI assistant inspired by Iron Man's Jarvis.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?logo=windows)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---

## What is this?

J.A.R.V.I.S I.R.L. is a **Windows desktop AI assistant** that:

- Talks to you with voice (TTS) and listens (STT) — all **free**, using Windows built-in speech.
- Uses a **cloud-hosted LLM brain** (Groq free tier by default, with local Ollama fallback).
- Can **read/write files**, **open/close apps**, **execute shell commands**, and **automate** your Windows machine.
- Has **full access to its own source code** and can **upgrade itself** on command via Roslyn + GitHub.
- Beautiful **Iron Man-style HUD** built in WPF with animated arc-reactor visuals.

---

## Quick Start

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Get a free [Groq API key](https://console.groq.com/keys)
3. Clone the repo and configure:
   ```bash
   git clone https://github.com/Floaca002/J.A.R.V.I.S-I.R.L.git
   cd J.A.R.V.I.S-I.R.L
   ```
4. Edit `config/appsettings.json` and paste your Groq API key.
5. Build & run:
   ```bash
   dotnet build
   dotnet run --project src/Jarvis.UI
   ```

See [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md) for the full guide.

---

## Documentation

| Document | Description |
|---|---|
| [`PLAN.md`](PLAN.md) | Step-by-step build & deployment plan |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Full visual architecture & component diagrams |
| [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md) | Local build, run, and configuration guide |
| [`SELF_UPGRADE.md`](docs/SELF_UPGRADE.md) | How Jarvis upgrades itself |
| [`SECURITY.md`](docs/SECURITY.md) | Sandbox, permissions, and safety controls |

---

## Features

- **Conversational AI** with memory and context
- **Tool-use / function-calling** — Jarvis chooses the right action
- **File system control** — read, write, search, manage files
- **App control** — launch programs, close windows, list processes
- **Shell execution** — run PowerShell / cmd safely
- **System automation** — mouse, keyboard, screenshots
- **Self-upgrade** — pulls new code from GitHub, recompiles itself, hot-swaps
- **Voice mode** — wake word "Jarvis", continuous listening, natural TTS

---

## License

MIT — do whatever you want, just don't sue Stark Industries.
