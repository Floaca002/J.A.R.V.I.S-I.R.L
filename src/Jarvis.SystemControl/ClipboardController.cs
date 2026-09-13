using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Jarvis.SystemControl;

/// <summary>
/// Windows clipboard access. Clipboard APIs require an STA thread, so every call
/// is dispatched onto a short-lived dedicated STA thread rather than assuming the
/// caller's thread apartment state.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ClipboardController
{
    public string GetText()
    {
        string result = string.Empty;
        RunSta(() => result = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty);
        return result;
    }

    public void SetText(string text)
    {
        RunSta(() => Clipboard.SetText(text ?? string.Empty));
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw error;
    }
}
