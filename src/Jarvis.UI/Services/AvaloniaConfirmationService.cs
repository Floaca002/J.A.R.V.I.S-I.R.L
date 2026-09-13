using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Jarvis.Core.Security;
using Jarvis.UI.Views;

namespace Jarvis.UI.Services;

/// <summary>Shows a modal HUD-styled dialog and awaits the answer. Safe to call from any thread — marshals to the UI thread itself.</summary>
public sealed class AvaloniaConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmAsync(string title, string details, CancellationToken cancellationToken = default)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dlg = new ConfirmDialog(title, details);
            var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner != null && !ReferenceEquals(owner, dlg))
                return await dlg.ShowDialog<bool>(owner);

            dlg.Show();
            return false;
        });
    }
}
