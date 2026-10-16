using System.Windows;
using GridironFrontOffice.Desktop.Spike;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

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
        SpikeProbe.Mark("app-startup");
        base.OnStartup(e);

        var options = SpikeOptions.Parse(e.Args);
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddFluentUIComponents();
        services.AddSingleton(options);         // Spike S0c only
        services.AddScoped<SpikeTheme>();       // Spike S0c only
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        _services = services.BuildServiceProvider();
        SpikeProbe.Mark("services-built");

        var window = new MainWindow(_services, options);
        SpikeProbe.Mark("window-constructed");
        window.Show();
        SpikeProbe.Mark("window-show-returned");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
