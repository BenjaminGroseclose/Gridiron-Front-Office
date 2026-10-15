using System.Windows;

namespace GridironFrontOffice.Desktop;

internal sealed partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        WebView.Services = services;
    }
}
