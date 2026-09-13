# J.A.R.V.I.S I.R.L.

> **Just A Rather Very Intelligent System — In Real Life**
> A self-upgradable Linux AI assistant inspired by Iron Man's Jarvis.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-Linux%2FX11%2FWayland-6B4FBB)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---

## What is this?

J.A.R.V.I.S I.R.L. is a **Linux desktop AI assistant** (developed against CachyOS, but any modern Linux desktop works) that:

- Talks to you with voice (TTS via `espeak-ng` or Piper) and listens via push-to-talk (STT via whisper.cpp) — all **free and offline**.
- Uses a **cloud-hosted LLM brain** — Groq free tier by default, local Ollama, or Claude (Anthropic) if you want vision and top-tier reasoning.
- Can **see your screen** on request, when you turn that on (Claude only — see [Vision](#vision--lets-jarvis-see-your-screen) below).
- Can **read/write files**, **open/close apps**, **execute shell commands**, and **automate your desktop** — screenshots, clipboard, volume, window control, mouse/keyboard — via whatever CLI tools your distro/compositor provides.
- Has **full access to its own source code** and can **upgrade itself** on command via Roslyn + GitHub.
- **Iron Man-style HUD** built with [Avalonia](https://avaloniaui.net/) (cross-platform .NET UI) with an animated arc-reactor visual.

### A note on Linux desktop diversity

There is no single API for screen capture, clipboard, volume, window management, or input injection on Linux — it depends on your display server (X11 vs Wayland) and desktop environment/compositor. Every system-control feature here **detects what's actually installed** on your machine at runtime and uses that (see the table in [Linux platform notes](#linux-platform-notes)); where nothing usable is found, Jarvis tells you exactly what to install rather than silently doing nothing.

---

## Quick Start

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) — on CachyOS/Arch: `sudo pacman -S dotnet-sdk` (currently installs .NET 10; Arch's rolling repos only carry the current release, see [Troubleshooting](BUILD_INSTRUCTIONS.md#8-troubleshooting) if `dotnet` complains about a version mismatch after an update).
2. Get a free [Groq API key](https://console.groq.com/keys).
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
5. On first launch click **⚙ SETTINGS** in the top bar and paste your Groq API key. It's saved to `~/.config/JarvisIRL/secrets.json` (never into the repo) — restart Jarvis to pick it up. You can also just edit `config/appsettings.json` directly if you prefer.

See [`BUILD_INSTRUCTIONS.md`](BUILD_INSTRUCTIONS.md) for the full guide, including which CLI tools to install for full functionality.

---

## Linux platform notes

Every system-control feature tries a chain of CLI tools and uses the first one it finds. None of these are hard dependencies — Jarvis runs fine without any of them installed, those specific tools/tool-calls just report what's missing.

| Feature | Tries, in order | Notes |
|---|---|---|
| Screenshot | `grim` (Wayland) → `spectacle` → `gnome-screenshot` → `scrot` → `import` (ImageMagick) | |
| Clipboard | `wl-copy`/`wl-paste` (Wayland) → `xclip` → `xsel` | |
| Volume | `wpctl` (WirePlumber/PipeWire) → `pactl` (Pulse/pipewire-pulse) → `amixer` (ALSA) | |
| Window control (minimize/maximize/focus/close) | `wmctrl` (+ `xdotool` for minimize) | X11 or XWayland only — most Wayland compositors don't expose generic window control |
| Mouse click | `xdotool` | X11/XWayland only |
| Typing text | `ydotool` (Wayland) → `xdotool` | `ydotool` needs the `ydotoold` daemon running and the user in the right input group |
| Shell commands | `bash` | |
| Text-to-speech | Piper (if `Voice.PiperModelPath` is set) → `espeak-ng`/`espeak` | Piper sounds far more natural; espeak-ng is the always-available fallback |
| Voice input | `arecord`/`parecord` to capture + whisper.cpp to transcribe | Push-to-talk (click mic to start, click again to stop) — there's no lightweight, verifiable "always listening for a wake word" option on Linux without pulling in a full VAD/wake-word model |

**Window control and mouse clicks are the biggest Wayland gap.** Wayland's security model deliberately doesn't let arbitrary apps manage other windows or inject input the way X11 does, and there's no standard cross-compositor tool for it — `xdotool`/`wmctrl` still work for XWayland-backed apps under most compositors, but not for native Wayland windows. Typing text has a real Wayland-native path via `ydotool`.

---

## Groq's free tier is small — what to expect

Groq's free ("on-demand") tier caps every tool-calling-capable chat model (`openai/gpt-oss-20b`, `openai/gpt-oss-120b`, `qwen/qwen3.6-27b`, `qwen/qwen3.8-27b`) at a flat **8,000 tokens/minute, 200,000/day, 1,000 requests/day** — the same limit regardless of model size, so switching models doesn't buy you headroom. Check your current numbers at https://console.groq.com/settings/limits.

Jarvis resends the system prompt and the full JSON schema for every registered tool (~20 of them) on *every single message*, on top of conversation history — that fixed overhead adds up fast against an 8K/minute budget, and a handful of messages can trip a `429 rate_limit_exceeded`. `Groq.MaxTokens` is deliberately kept modest (800) to leave more of that budget for the prompt+tools overhead rather than the reply.

Two Groq models you'll see in your account that **won't work here**: `groq/compound` and `groq/compound-mini` have much higher limits (70K TPM, no daily cap), but they're Groq's own agentic system with built-in tools and reject custom `tools` schemas outright (`400: "tool calling" is not supported with this model`) — since Jarvis's entire tool-dispatch mechanism depends on custom tool schemas, these two aren't usable as a provider here.

If you're hitting the wall regularly: pace your testing (limits reset per minute), see if Groq's Dev Tier plan raises it enough for your use, or switch to **Ollama** (fully local, no rate limit, needs your own hardware) or **Claude** (paid per-token, no free-tier throttling like this) in Settings.

---

## Using Claude (Anthropic) instead of Groq

Open **⚙ SETTINGS**, set default provider to **Anthropic**, and paste a Claude API key.

**Important — this is not the same thing as a claude.ai subscription.** A Claude Pro/Max chat plan does not include any API access or credits. The API is a completely separate product, billed per token, with its own key:

1. Go to [console.anthropic.com](https://console.anthropic.com), create an account (or use your existing Anthropic login) and add billing/credits there.
2. Generate an API key under **API Keys**.
3. Paste it into Jarvis's Settings window (or set env var `JARVIS_ANTHROPIC_API_KEY`).

Roughly, as of writing: Claude Opus 5 (the default model Jarvis uses) costs about $5 per million input tokens and $25 per million output tokens — a typical short back-and-forth is a fraction of a cent, but it adds up with heavy use, and more so with [Vision](#vision--lets-jarvis-see-your-screen) turned on. Check [Anthropic's pricing page](https://www.anthropic.com/pricing) for current rates, and set a spending limit in the Console if you want a hard ceiling.

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
- **Window & input control** — minimize/maximize/focus/close windows (X11/XWayland), volume up/down/mute, clipboard read/write
- **Shell execution** — run bash, gated behind a confirmation modal
- **System automation** — mouse, keyboard, screenshots (see [Linux platform notes](#linux-platform-notes) for what needs what)
- **Self-upgrade** — the LLM writes a new C# tool, Jarvis compiles it with Roslyn and hot-loads it, with a confirmation modal, an audit trail, and a one-command revert
- **Update checks** — asks GitHub for the latest release and reports it
- **In-app Settings window** — configure provider, API keys, vision, voice, and security flags without hand-editing JSON
- **Voice mode** — push-to-talk input (whisper.cpp) and natural TTS (Piper, with an espeak-ng fallback that needs no setup)
- **Iron Man-style HUD** — animated arc reactor, live tool list, self-upgrade log, vision status indicator

---

## License

MIT — do whatever you want, just don't sue Stark Industries.
