using GridironFrontOffice.UI.Components;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Shell;
using GridironFrontOffice.UI.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GridironFrontOffice.UI;

/// <summary>The UI's services, registered by both hosts (ARCHITECTURE.md §6.1).</summary>
public static class UiServices
{
    /// <summary>
    /// Registers the design system's services, the shell's options, the save actions the screens share and the game
    /// state store (one per circuit in the DevHost, one in the desktop app). A host whose app bar is the window's title
    /// bar registers its own <see cref="ShellOptions"/> first (the desktop host, D-068). The store reads through
    /// AddGfoApplication's query services, so the hosts call that first (ARCHITECTURE §6.1).
    /// </summary>
    public static IServiceCollection AddGfoUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddGfoUiComponents();
        services.TryAddSingleton(new ShellOptions());
        services.AddScoped<SaveActions>();
        services.AddScoped<GameStateStore>();
        return services;
    }
}
