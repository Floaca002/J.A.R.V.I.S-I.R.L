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
- Uses a **cloud-hosted LLM brain** — Groq free tier by default, local Ollama, or Claude (Anthropic) if you want vision and top-tier reasoning.
- Can **see your screen** on request, when you turn that on (Claude only — see [Vision](#vision-lets-jarvis-see-your-screen) below).
- Can **read/write files**, **open/close apps**, **execute shell commands**, and **automate** your Windows machine.
- Has **full access to its own source code** and can **upgrade itself** on command via Roslyn + GitHub.
- Beautiful **Iron Man-style HUD** built in WPF with animated arc-reactor visuals.

---

## Quick Start

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows — Jarvis uses WPF, WinForms and `System.Speech`, all Windows-only).
2. Get a free [Groq API key](https://console.groq.com/keys)
3. Clone the repo:
   ```bash
   git clone https://github.com/Floaca002/J.A.R.V.I.S-I.R.L.git
   cd J.A.R.V.I.S-I.R.L
   ```
4. Build & run:
   ```bash
   dotnet build
   dotnet run --project src/Jarvis.UI
   ```
5. On first launch click **⚙ SETTINGS** in the top bar and paste your Groq API key. It's saved to `%AppData%\JarvisIRL\secrets.json` (never into the repo) — restart Jarvis to pick it up. You can also just edit `config/appsettings.json` directly if you prefer.

See [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md) for the full guide.

---

## Using Claude (Anthropic) instead of Groq

Open **⚙ SETTINGS**, set default provider to **Anthropic**, and paste a Claude API key.

**Important — this is not the same thing as a claude.ai subscription.** A Claude Pro/Max chat plan does not include any API access or credits. The API is a completely separate product, billed per token, with its own key:

1. Go to [console.anthropic.com](https://console.anthropic.com), create an account (or use your existing Anthropic login) and add billing/credits there.
2. Generate an API key under **API Keys**.
3. Paste it into Jarvis's Settings window (or set env var `JARVIS_ANTHROPIC_API_KEY`).

Roughly, as of writing: Claude Opus 5 (the default model Jarvis uses) costs about $5 per million input tokens and $25 per million output tokens — a typical short back-and-forth is a fraction of a cent, but it adds up with heavy use, and more so with [Vision](#vision-lets-jarvis-see-your-screen) turned on. Check [Anthropic's pricing page](https://www.anthropic.com/pricing) for current rates, and set a spending limit in the Console if you want a hard ceiling.

Groq's free tier remains the default specifically because it costs nothing to try — switch to Claude when you want vision, or better reasoning/self-upgrade quality.

## Vision — lets Jarvis see your screen

Turn on **"Let Jarvis see your screen"** in Settings and Jarvis captures a fresh screenshot and attaches it to every message you send it — so you can ask things like *"what's this error on my screen?"* or *"summarize this document I have open."*

A few things worth knowing:

- **Only the Anthropic (Claude) provider currently understands images.** Groq and Ollama will just ignore the screenshot.
- It captures **only when you send a message** — not a continuous stream. There's no "always watching" mode; that's not how these APIs work, and a real 24/7 video stream to a cloud LLM would be both very expensive and a real privacy concern, so Jarvis doesn't do that.
- Screenshots are **never written to disk or saved to conversation history** — they're sent for that one request only.
- It costs extra: every turn with vision on sends a full-screen image's worth of tokens, on top of the text. Leave it off unless you're actively using it.
- The HUD shows a **👁 VISION: ON/OFF** indicator at all times so you always know whether your screen is being shared.

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

- **Conversational AI** with memory and context — Groq, Ollama, or Claude (Anthropic)
- **Screen vision** (Claude only) — attach a live screenshot to every message, opt-in, off by default
- **Tool-use / function-calling** — Jarvis chooses the right action
- **File system control** — read, write, list, create folders, delete (with confirmation)
- **App control** — launch programs/URLs, close windows, list processes
- **Window & input control** — minimize/maximize/focus/close windows, volume up/down/mute, clipboard read/write
- **Shell execution** — run PowerShell, gated behind a confirmation modal
- **System automation** — mouse, keyboard, screenshots
- **Self-upgrade** — the LLM writes a new C# tool, Jarvis compiles it with Roslyn and hot-loads it, with a confirmation modal, an audit trail, and a one-command revert
- **Update checks** — asks GitHub for the latest release and reports it
- **In-app Settings window** — configure provider, API keys, vision, voice (pick from installed voices + preview) and security flags without hand-editing JSON
- **Voice mode** — wake word "Jarvis", continuous listening, natural TTS, with automatic fallback to the best "Jarvis-like" voice installed
- **Iron Man-style HUD** — animated arc reactor, live tool list, self-upgrade log, vision status indicator

---

## License

MIT — do whatever you want, just don't sue Stark Industries.
