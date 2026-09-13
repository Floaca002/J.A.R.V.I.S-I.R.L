using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Jarvis.UI.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string details) : this()
    {
        TitleTextBlock.Text = title;
        DetailsTextBlock.Text = details;
    }

    private void OnAuthorize(object? sender, RoutedEventArgs e) => Close(true);

    private void OnDeny(object? sender, RoutedEventArgs e) => Close(false);
}
