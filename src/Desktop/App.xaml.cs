using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Desktop;

/// <summary>
/// The composition root (ARCHITECTURE.md §6.1). Logging, settings, the single-instance guard and the GFO
/// services are wired in M0 WP5 and WP9.
/// </summary>
internal sealed partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        _services = services.BuildServiceProvider();

        new MainWindow(_services).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
