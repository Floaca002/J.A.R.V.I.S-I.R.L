# Build & Run Instructions

## 1. Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A free [Groq API key](https://console.groq.com/keys)

## 2. Clone

```powershell
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

Or set the environment variable `JARVIS_GROQ_API_KEY` (recommended).

## 4. Build

```powershell
dotnet restore
dotnet build -c Release
```

## 5. Run

```powershell
dotnet run --project src/Jarvis.UI -c Release
```

Or open `JARVIS.IRL.sln` in Visual Studio 2022 and press F5.

## 6. Create a single-file deployment (optional)

```powershell
dotnet publish src/Jarvis.UI -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output `Jarvis.UI.exe` will be in `src/Jarvis.UI/bin/Release/net8.0-windows/win-x64/publish/`.

## 7. First-run checklist

- [ ] The HUD window appears with an arc-reactor animation.
- [ ] Typing "Hello Jarvis" and pressing Enter produces a spoken + written reply.
- [ ] Clicking the mic icon enables wake-word listening.
- [ ] "Open Notepad" actually launches Notepad.
- [ ] `%AppData%/JarvisIRL/logs/` contains a fresh log file.

## 8. Troubleshooting

| Symptom | Fix |
|---|---|
| `Unauthorized` from Groq | Re-check API key, no extra spaces |
| Voice doesn't work | Settings → Privacy → Microphone → allow apps |
| Tools not executing | Check `Security.RequireConfirmationForCommands` |
| Self-upgrade fails | Ensure `Microsoft.CodeAnalysis.CSharp` NuGet restored |
