using System.Diagnostics;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.DevHost;
using GridironFrontOffice.DevHost.Components;
using GridironFrontOffice.Infrastructure;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.Infrastructure.Settings;
using GridironFrontOffice.UI;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;
using Serilog.Extensions.Logging;

// The DevHost's composition root mirrors the desktop one (LOGGING.md §4), against its own data folder:
// %LOCALAPPDATA%\GridironFrontOffice-Dev, so development never touches real saves, settings or logs.
var paths = AppPaths.ForDevelopment().EnsureCreated();
using var settings = SettingsStore.Load(paths);
#if DEBUG
var identity = AppIdentity.From(typeof(App).Assembly, "Debug");
#else
var identity = AppIdentity.From(typeof(App).Assembly, "Release");
#endif

Log.Logger = GfoLogging.CreateLogger(paths, settings.Current, identity);
var hostLog = new SerilogLoggerFactory(Log.Logger).CreateLogger("GridironFrontOffice.DevHost");
var banner = StartupBanner.Build(identity, new HostDescription("DevHost", WebView2Version: null, Screen: "browser"), paths, settings.Current);
HostLog.Banner(hostLog, banner);
if (settings.Created)
{
    HostLog.SettingsCreated(hostLog, paths.SettingsFile);
}

if (settings.LoadWarning is { } problem)
{
    HostLog.SettingsUnreadable(hostLog, problem);
}

// No crash dialog in the DevHost: log the failure, flush, and let the process end.
GlobalExceptionHooks.Install(hostLog, _ => Log.CloseAndFlush());

try
{
    var builder = WebApplication.CreateBuilder(args);

    // The console logger stays (dev tooling reads it); Serilog adds the rolling file.
    builder.Logging.AddSerilog(Log.Logger);
    builder.Services.AddGfoInfrastructure(paths, identity);   // paths, the save manager and the data layer
    builder.Services.AddSingleton<IPlatformServices, DevPlatformServices>();   // a .trash folder, not the real Recycle Bin
    builder.Services.AddSingleton<ISettingsStore>(settings);
    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddFluentUIComponents();
    builder.Services.AddGfoUi();

    // Exit on the main menu stops this host (DevPlatformServices.Quit). The default 30 s drain would keep the process
    // alive while the open Blazor circuits linger; a dev host can just go.
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(2));

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error", createScopeForErrors: true);
    }

    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(AppRoot).Assembly);

    await app.RunAsync();
    var minutes = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMinutes;
    HostLog.Exiting(hostLog, 0, minutes);
}
finally
{
    await Log.CloseAndFlushAsync();
}
