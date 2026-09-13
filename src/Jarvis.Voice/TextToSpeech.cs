using System.Diagnostics;
using System.Globalization;
using Jarvis.Core.Config;

namespace Jarvis.Voice;

/// <summary>
/// Linux TTS. Prefers Piper (natural-sounding neural voices, needs a downloaded
/// .onnx model — see README) and falls back to espeak-ng (robotic but installed
/// almost everywhere and needs no model download).
/// </summary>
public sealed class TextToSpeech : IDisposable
{
    /// <summary>A handful of espeak-ng voice variants worth offering in a picker; any espeak-ng "-v" value works.</summary>
    public static readonly IReadOnlyList<string> CommonEspeakVoices =
        new[] { "en-gb+m3", "en-gb+m1", "en-gb-x-rp+m3", "en-us+m3", "en-us+m1" };

    private readonly VoiceConfig _cfg;
    private Process? _current;

    public TextToSpeech(VoiceConfig cfg) => _cfg = cfg;

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Stop();
        _current = SpeakInternal(text, _cfg);
    }

    public void Stop()
    {
        var p = _current;
        _current = null;
        if (p == null) return;
        try { if (!p.HasExited) p.Kill(true); } catch { /* best effort */ }
    }

    public void Dispose() => Stop();

    /// <summary>Speak a sample synchronously — for a Settings "preview" button. Call off the UI thread.</summary>
    public static void Preview(VoiceConfig cfg, string sampleText)
    {
        using var p = SpeakInternal(sampleText, cfg);
        p?.WaitForExit(15000);
    }

    private static Process? SpeakInternal(string text, VoiceConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.PiperModelPath) && File.Exists(cfg.PiperModelPath) && IsOnPath("piper"))
        {
            var wav = Path.Combine(Path.GetTempPath(), $"jarvis-tts-{Guid.NewGuid():N}.wav");
            if (RunPiper(text, cfg, wav) && File.Exists(wav))
                return PlayAndCleanup(wav);
        }
        return SpeakWithEspeak(text, cfg);
    }

    private static bool RunPiper(string text, VoiceConfig cfg, string outWav)
    {
        try
        {
            var psi = new ProcessStartInfo("piper")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(cfg.PiperModelPath);
            psi.ArgumentList.Add("--length_scale");
            psi.ArgumentList.Add(RateToPiperLengthScale(cfg.Rate).ToString(CultureInfo.InvariantCulture));
            psi.ArgumentList.Add("--output_file");
            psi.ArgumentList.Add(outWav);

            using var p = Process.Start(psi);
            if (p == null) return false;
            p.StandardInput.Write(text);
            p.StandardInput.Close();
            return p.WaitForExit(20000) && p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>SAPI-style -10..10 rate → Piper's length_scale, where smaller is faster and 1.0 is Piper's default pace.</summary>
    private static double RateToPiperLengthScale(int rate) => 1.0 - Math.Clamp(rate, -10, 10) * 0.04;

    private static Process? SpeakWithEspeak(string text, VoiceConfig cfg)
    {
        var exe = IsOnPath("espeak-ng") ? "espeak-ng" : IsOnPath("espeak") ? "espeak" : null;
        if (exe == null)
            throw new InvalidOperationException(
                "No text-to-speech engine found. Install `espeak-ng` for basic speech, or configure Piper " +
                "(Settings → Voice → Piper model path) for a much more natural voice — see README.");

        var wpm = Math.Clamp(175 + Math.Clamp(cfg.Rate, -10, 10) * 8, 80, 450);
        var amplitude = Math.Clamp(cfg.Volume, 0, 200);

        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add(string.IsNullOrWhiteSpace(cfg.EspeakVoice) ? "en-gb+m3" : cfg.EspeakVoice);
        psi.ArgumentList.Add("-s");
        psi.ArgumentList.Add(wpm.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-a");
        psi.ArgumentList.Add(amplitude.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add(text);

        return Process.Start(psi);
    }

    private static Process? PlayAndCleanup(string wav)
    {
        var exe = IsOnPath("paplay") ? "paplay" : IsOnPath("aplay") ? "aplay" : IsOnPath("ffplay") ? "ffplay" : null;
        if (exe == null)
        {
            try { File.Delete(wav); } catch { /* best effort */ }
            throw new InvalidOperationException(
                "No audio player found. Install `pipewire-pulse`/`pulseaudio-utils` (paplay) or `alsa-utils` (aplay).");
        }

        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        if (exe == "ffplay") { psi.ArgumentList.Add("-nodisp"); psi.ArgumentList.Add("-autoexit"); }
        psi.ArgumentList.Add(wav);

        var p = Process.Start(psi);
        if (p != null)
            _ = p.WaitForExitAsync().ContinueWith(_ => { try { File.Delete(wav); } catch { } });
        return p;
    }

    private static bool IsOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator).Any(dir => !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, exe)));
    }
}
