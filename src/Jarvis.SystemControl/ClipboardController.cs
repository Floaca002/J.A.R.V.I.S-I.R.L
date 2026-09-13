namespace Jarvis.SystemControl;

/// <summary>
/// Clipboard access via whichever CLI tool matches the display server:
/// wl-clipboard (Wayland) or xclip/xsel (X11).
/// </summary>
public sealed class ClipboardController
{
    public string GetText()
    {
        if (ProcessRunner.IsWayland() && ProcessRunner.IsOnPath("wl-paste"))
            return ProcessRunner.Capture("wl-paste", new[] { "--no-newline" }) ?? string.Empty;
        if (ProcessRunner.IsOnPath("xclip"))
            return ProcessRunner.Capture("xclip", new[] { "-selection", "clipboard", "-o" }) ?? string.Empty;
        if (ProcessRunner.IsOnPath("xsel"))
            return ProcessRunner.Capture("xsel", new[] { "--clipboard", "--output" }) ?? string.Empty;

        throw new InvalidOperationException(
            "No clipboard tool found. Install `wl-clipboard` (Wayland) or `xclip`/`xsel` (X11).");
    }

    public void SetText(string text)
    {
        if (ProcessRunner.IsWayland() && ProcessRunner.IsOnPath("wl-copy") && ProcessRunner.Run("wl-copy", Array.Empty<string>(), stdin: text))
            return;
        if (ProcessRunner.IsOnPath("xclip") && ProcessRunner.Run("xclip", new[] { "-selection", "clipboard" }, stdin: text))
            return;
        if (ProcessRunner.IsOnPath("xsel") && ProcessRunner.Run("xsel", new[] { "--clipboard", "--input" }, stdin: text))
            return;

        throw new InvalidOperationException(
            "No clipboard tool found. Install `wl-clipboard` (Wayland) or `xclip`/`xsel` (X11).");
    }
}
