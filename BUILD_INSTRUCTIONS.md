# Build & Run Instructions

## 1. Prerequisites

- Linux with a desktop session (X11 or Wayland) — developed against CachyOS/Arch, but anything with a matching .NET SDK and the CLI tools below works.
- The [.NET SDK](https://dotnet.microsoft.com/download) matching the project's `TargetFramework` (currently `net10.0`) — on Arch/CachyOS: `sudo pacman -S dotnet-sdk`. Arch's rolling repos only ever carry the *current* .NET release (no side-by-side old LTS packages), so `dotnet-sdk` always tracks the newest version — if this repo's `TargetFramework` ever lags behind what `pacman` installs, bump the `TargetFramework` in every `.csproj` under `src/` to match rather than trying to install an older runtime that isn't packaged.
- A free [Groq API key](https://console.groq.com/keys) (or a Claude API key from console.anthropic.com — see README)
- Recommended CLI tools so every feature actually works (all optional — Jarvis tells you exactly what's missing if a tool call needs one it can't find):

  ```bash
  # CachyOS / Arch
  sudo pacman -S wl-clipboard xclip wmctrl xdotool wireplumber pipewire-pulse \
                 espeak-ng alsa-utils grim scrot

  # Piper (natural TTS) and whisper.cpp (voice input) aren't in most distro repos —
  # install from https://github.com/rhasspy/piper and https://github.com/ggml-org/whisper.cpp,
  # or check the AUR (piper-tts, whisper.cpp).
  ```

## 2. Clone

```bash
git clone https://github.com/Floaca002/J.A.R.V.I.S-I.R.L.git
cd J.A.R.V.I.S-I.R.L
```

## 3. Configure

Edit `config/appsettings.json`:

```json
{
  "AI": {
    "DefaultProvider": "Groq",
    "Groq": { "ApiKey": "gsk_..." }
  }
}
```

Or set the environment variable `JARVIS_GROQ_API_KEY` (recommended), or just use the in-app **⚙ SETTINGS** window after first launch.

## 4. Build

```bash
dotnet restore
dotnet build -c Release
```

## 5. Run

```bash
dotnet run --project src/Jarvis.UI -c Release
```

## 6. Create a single-file deployment (optional)

```bash
dotnet publish src/Jarvis.UI -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output `Jarvis` binary will be in `src/Jarvis.UI/bin/Release/net10.0/linux-x64/publish/`.

## 7. First-run checklist

- [ ] The HUD window appears with an arc-reactor animation.
- [ ] Typing "Hello Jarvis" and pressing Enter produces a written reply (+ spoken, if `espeak-ng` or Piper is installed).
- [ ] "Open Notepad"-equivalent, e.g. "open firefox", actually launches it.
- [ ] `~/.config/JarvisIRL/logs/` contains a fresh log file.
- [ ] The 👁 VISION indicator in the HUD reads OFF unless you turned it on.

## 8. Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet build`/`dotnet run` fails with "You must install or update .NET" naming a framework version that isn't installed | Your SDK and runtime packages drifted out of sync — very common on Arch/CachyOS after a partial upgrade. Run a full `sudo pacman -Syu` (not just `pacman -S dotnet-sdk` on its own — partial upgrades are unsupported on Arch), then `sudo pacman -S dotnet-sdk dotnet-runtime aspnet-runtime` to make sure all three are reinstalled at the same version. Check with `dotnet --list-sdks` / `dotnet --list-runtimes`. |
| `Unauthorized` from Groq | Re-check API key, no extra spaces |
| "No screenshot tool found" | Install `grim` (Wayland) or `scrot`/`spectacle`/`gnome-screenshot` |
| "No clipboard tool found" | Install `wl-clipboard` (Wayland) or `xclip`/`xsel` (X11) |
| "No volume control tool found" | Install `wireplumber` (wpctl) or `pipewire-pulse`/`pulseaudio-utils` (pactl) |
| Window control / mouse-click errors on Wayland | These need `wmctrl`/`xdotool`, which only reliably work under X11 or XWayland — see README's Linux platform notes |
| No text-to-speech | Install `espeak-ng`, or configure a Piper model in Settings for a much better voice |
| Mic button says voice unavailable | Set a whisper.cpp model path in Settings and install `arecord` (alsa-utils) or `parecord` |
| Tools not executing | Check `Security.RequireConfirmationForCommands` and answer the on-screen authorization dialog |
| Self-upgrade fails | Check the error message returned by `upgrade_self` — it includes the Roslyn compiler errors |
