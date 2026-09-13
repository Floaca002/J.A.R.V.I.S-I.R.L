using System.Runtime.Versioning;
using System.Speech.Synthesis;
using Jarvis.Core.Config;

namespace Jarvis.Voice;

/// <summary>
/// Free, offline TTS using Windows built-in System.Speech.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TextToSpeech : IDisposable
{
    private readonly SpeechSynthesizer _synth;

    public TextToSpeech(VoiceConfig cfg)
    {
        _synth = new SpeechSynthesizer();
        _synth.SetOutputToDefaultAudioDevice();
        _synth.Rate = Math.Clamp(cfg.Rate, -10, 10);
        _synth.Volume = Math.Clamp(cfg.Volume, 0, 100);

        if (!string.IsNullOrWhiteSpace(cfg.Voice))
        {
            try { _synth.SelectVoice(cfg.Voice); }
            catch
            {
                var picked = PickBestFallbackVoice(_synth.GetInstalledVoices());
                if (picked != null) _synth.SelectVoice(picked);
            }
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _synth.SpeakAsyncCancelAll();
        _synth.SpeakAsync(text);
    }

    public void Stop() => _synth.SpeakAsyncCancelAll();

    public void Dispose() => _synth.Dispose();

    /// <summary>Names of every enabled voice installed on this machine (for a Settings picker).</summary>
    public static IReadOnlyList<string> ListInstalledVoiceNames()
    {
        using var synth = new SpeechSynthesizer();
        return synth.GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => v.VoiceInfo.Name)
            .ToList();
    }

    /// <summary>Speaks a short sample synchronously with the given settings — for a Settings "preview" button. Call off the UI thread.</summary>
    public static void Preview(string? voiceName, int rate, int volume, string sampleText)
    {
        using var synth = new SpeechSynthesizer();
        synth.SetOutputToDefaultAudioDevice();
        synth.Rate = Math.Clamp(rate, -10, 10);
        synth.Volume = Math.Clamp(volume, 0, 100);
        if (!string.IsNullOrWhiteSpace(voiceName))
        {
            try { synth.SelectVoice(voiceName); }
            catch { /* fall back to the default voice */ }
        }
        synth.Speak(sampleText);
    }

    /// <summary>
    /// Windows' modern "Natural" voices (Ryan, Guy, Aria...) sound far more like a
    /// movie AI assistant than the classic SAPI5 desktop voices (David/Zira/Mark), but
    /// aren't always registered under the exact name a user might configure. Prefer the
    /// closest match to "Jarvis" available, then any male voice, before giving up.
    /// </summary>
    private static string? PickBestFallbackVoice(IEnumerable<InstalledVoice> installed)
    {
        var enabled = installed.Where(v => v.Enabled).ToList();
        string[] preferredOrder = { "Ryan", "Guy", "George", "James", "David", "Mark" };

        foreach (var name in preferredOrder)
        {
            var match = enabled.FirstOrDefault(v => v.VoiceInfo.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match.VoiceInfo.Name;
        }

        return enabled.FirstOrDefault(v => v.VoiceInfo.Gender == VoiceGender.Male)?.VoiceInfo.Name;
    }
}
