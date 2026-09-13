using System.Windows;

namespace Jarvis.UI.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string details)
    {
        InitializeComponent();
        TitleTextBlock.Text = title;
        DetailsTextBlock.Text = details;
    }

    private void OnAuthorize(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnDeny(object sender, RoutedEventArgs e) => DialogResult = false;
}
