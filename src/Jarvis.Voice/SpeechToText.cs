using System.Globalization;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using Jarvis.Core.Config;

namespace Jarvis.Voice;

/// <summary>
/// Free, offline STT using Windows built-in System.Speech.
/// Two modes: wake-word listening, and one-shot capture after wake.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SpeechToText : IDisposable
{
    private readonly SpeechRecognitionEngine _engine;
    private readonly string _wakeWord;
    private bool _waitingForCommand;

    public event Action<string>? CommandRecognized;
    public event Action? WakeWordDetected;

    public SpeechToText(VoiceConfig cfg)
    {
        _wakeWord = cfg.WakeWord.ToLowerInvariant();
        _engine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
        _engine.LoadGrammar(new DictationGrammar());
        _engine.SetInputToDefaultAudioDevice();
        _engine.SpeechRecognized += OnRecognized;
    }

    public void StartListening() => _engine.RecognizeAsync(RecognizeMode.Multiple);
    public void StopListening() => _engine.RecognizeAsyncStop();

    private void OnRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var text = (e.Result.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (_waitingForCommand)
        {
            _waitingForCommand = false;
            CommandRecognized?.Invoke(text);
            return;
        }

        if (text.ToLowerInvariant().Contains(_wakeWord))
        {
            // Either the entire utterance contained the wake-word + a command,
            // or it was just the wake-word.
            var stripped = StripWake(text);
            if (!string.IsNullOrWhiteSpace(stripped))
            {
                CommandRecognized?.Invoke(stripped);
            }
            else
            {
                _waitingForCommand = true;
                WakeWordDetected?.Invoke();
            }
        }
    }

    private string StripWake(string text)
    {
        var idx = text.ToLowerInvariant().IndexOf(_wakeWord, StringComparison.Ordinal);
        if (idx < 0) return text;
        var after = text.Substring(idx + _wakeWord.Length).TrimStart(',', ' ', '.');
        return after;
    }

    public void Dispose() => _engine.Dispose();
}
