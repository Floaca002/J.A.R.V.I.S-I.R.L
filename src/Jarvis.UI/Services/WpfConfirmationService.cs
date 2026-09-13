using System.Windows;
using Jarvis.Core.Security;
using Jarvis.UI.Views;

namespace Jarvis.UI.Services;

/// <summary>Shows a modal HUD-styled dialog and blocks the calling (background) thread until answered.</summary>
public sealed class WpfConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmAsync(string title, string details, CancellationToken cancellationToken = default)
    {
        var approved = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dlg = new ConfirmDialog(title, details);
            if (Application.Current.MainWindow is { IsLoaded: true } main && !ReferenceEquals(main, dlg))
                dlg.Owner = main;
            approved = dlg.ShowDialog() == true;
        });
        return Task.FromResult(approved);
    }
}
