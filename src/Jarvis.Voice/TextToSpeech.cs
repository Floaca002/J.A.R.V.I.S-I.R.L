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
                // Voice not installed — fall back to first male voice if any.
                var male = _synth.GetInstalledVoices()
                    .FirstOrDefault(v => v.VoiceInfo.Gender == VoiceGender.Male);
                if (male != null) _synth.SelectVoice(male.VoiceInfo.Name);
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
}
