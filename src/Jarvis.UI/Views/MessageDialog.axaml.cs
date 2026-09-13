using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Jarvis.UI.Views;

/// <summary>Avalonia has no built-in MessageBox; this is the minimal stand-in used wherever WPF's MessageBox.Show was.</summary>
public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    public MessageDialog(string title, string message) : this()
    {
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();

    /// <summary>Show the dialog, marshaling to the UI thread and using the main window as owner if one exists. Safe to call from any thread.</summary>
    public static Task ShowAsync(string title, string message)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dlg = new MessageDialog(title, message);
            var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner != null && !ReferenceEquals(owner, dlg))
                await dlg.ShowDialog(owner);
            else
                dlg.Show();
        });
    }
}
