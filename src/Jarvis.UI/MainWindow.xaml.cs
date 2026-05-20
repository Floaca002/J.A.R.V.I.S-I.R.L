using System.Windows;
using Jarvis.UI.ViewModels;

namespace Jarvis.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.AttachScrollToBottom(() => ChatScroll.ScrollToBottom());
    }
}
