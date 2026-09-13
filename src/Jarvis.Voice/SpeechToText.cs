using System.Diagnostics;
using System.Text.RegularExpressions;
using Jarvis.Core.Config;

namespace Jarvis.Voice;

/// <summary>
/// Push-to-talk speech-to-text via whisper.cpp. There's no universal, dependency-free
/// "always listening for a wake word" story on Linux without pulling in a full VAD/
/// wake-word model, so this is press-to-start, press-to-stop instead: <see cref="StartListening"/>
/// begins recording the microphone, <see cref="StopListening"/> stops it and transcribes.
/// </summary>
public sealed class SpeechToText : IDisposable
{
    private readonly string? _whisperModel;
    private readonly string _whisperBinary;
    private Process? _recorder;
    private string? _recordingPath;

    public event Action<string>? CommandRecognized;
    public event Action? RecordingStarted;
    public event Action<string>? TranscriptionFailed;

    /// <summary>True only if a whisper.cpp binary, a model file, and a recorder (arecord/parecord) were all found.</summary>
    public bool IsAvailable { get; }

    public SpeechToText(VoiceConfig cfg)
    {
        _whisperModel = string.IsNullOrWhiteSpace(cfg.WhisperModelPath) ? null : cfg.WhisperModelPath;
        _whisperBinary = new[] { "whisper-cli", "whisper", "main" }.FirstOrDefault(IsOnPath) ?? "whisper-cli";
        IsAvailable = _whisperModel != null
                      && File.Exists(_whisperModel)
                      && IsOnPath(_whisperBinary)
                      && (IsOnPath("arecord") || IsOnPath("parecord"));
    }

    public void StartListening()
    {
        if (_recorder != null) return;

        _recordingPath = Path.Combine(Path.GetTempPath(), $"jarvis-rec-{Guid.NewGuid():N}.wav");
        var useArecord = IsOnPath("arecord");
        var psi = new ProcessStartInfo(useArecord ? "arecord" : "parecord")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var a in useArecord
                     ? new[] { "-q", "-f", "cd", "-t", "wav", _recordingPath }
                     : new[] { "--file-format=wav", _recordingPath })
            psi.ArgumentList.Add(a);

        try
        {
            _recorder = Process.Start(psi);
            if (_recorder == null)
            {
                TranscriptionFailed?.Invoke("Couldn't start microphone recording.");
                return;
            }
            RecordingStarted?.Invoke();
        }
        catch (Exception ex)
        {
            _recorder = null;
            TranscriptionFailed?.Invoke($"Couldn't start microphone recording: {ex.Message}. Install `alsa-utils` (arecord) or `pulseaudio-utils`/`pipewire-pulse` (parecord).");
        }
    }

    public void StopListening()
    {
        var recorder = _recorder;
        var path = _recordingPath;
        _recorder = null;
        _recordingPath = null;
        if (recorder == null || path == null) return;

        StopRecordingGracefully(recorder);
        _ = TranscribeAsync(path);
    }

    private static void StopRecordingGracefully(Process recorder)
    {
        // arecord/parecord finalize the WAV header on SIGTERM; SIGKILL (Process.Kill on
        // Unix) would leave a truncated/corrupt file, so ask nicely first.
        try
        {
            var killPsi = new ProcessStartInfo("kill") { UseShellExecute = false, CreateNoWindow = true };
            killPsi.ArgumentList.Add("-TERM");
            killPsi.ArgumentList.Add(recorder.Id.ToString());
            using var kill = Process.Start(killPsi);
            kill?.WaitForExit(1000);

            if (!recorder.WaitForExit(2000))
                recorder.Kill(true);
        }
        catch
        {
            try { recorder.Kill(true); } catch { /* best effort */ }
        }
    }

    private async Task TranscribeAsync(string wavPath)
    {
        try
        {
            if (!IsAvailable)
            {
                TranscriptionFailed?.Invoke(
                    "Voice input needs whisper.cpp (`whisper-cli`) and a model — set Voice.WhisperModelPath in Settings. See README.");
                return;
            }

            var psi = new ProcessStartInfo(_whisperBinary)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add(_whisperModel!);
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add(wavPath);
            psi.ArgumentList.Add("-nt");

            using var p = Process.Start(psi);
            if (p == null)
            {
                TranscriptionFailed?.Invoke("Failed to start whisper.cpp.");
                return;
            }
            var stdout = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await p.WaitForExitAsync().ConfigureAwait(false);

            var text = CleanTranscript(stdout);
            if (string.IsNullOrWhiteSpace(text))
                TranscriptionFailed?.Invoke("Didn't catch that — no speech recognized.");
            else
                CommandRecognized?.Invoke(text);
        }
        catch (Exception ex)
        {
            TranscriptionFailed?.Invoke($"Transcription failed: {ex.Message}");
        }
        finally
        {
            try { File.Delete(wavPath); } catch { /* best effort */ }
        }
    }

    /// <summary>Strips leading "[00:00:00.000 --&gt; 00:00:02.000]"-style timestamps some whisper.cpp builds print even with -nt.</summary>
    private static string CleanTranscript(string raw) =>
        string.Join(' ', raw.Split('\n')
            .Select(line => Regex.Replace(line, @"^\s*\[[^\]]*\]\s*", string.Empty).Trim())
            .Where(line => line.Length > 0));

    private static bool IsOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator).Any(dir => !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, exe)));
    }

    public void Dispose()
    {
        if (_recorder != null)
        {
            try { _recorder.Kill(true); } catch { /* best effort */ }
        }
    }
}
