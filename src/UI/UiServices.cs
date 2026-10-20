using GridironFrontOffice.UI.Components;
using GridironFrontOffice.UI.Menu;
using GridironFrontOffice.UI.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GridironFrontOffice.UI;

/// <summary>The UI's services, registered by both hosts (ARCHITECTURE.md §6.1).</summary>
public static class UiServices
{
    /// <summary>
    /// Registers the design system's services, the shell's options and the save actions the screens share. A host whose
    /// app bar is the window's title bar registers its own <see cref="ShellOptions"/> first (the desktop host, D-068).
    /// </summary>
    public static IServiceCollection AddGfoUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddGfoUiComponents();
        services.TryAddSingleton(new ShellOptions());
        services.AddScoped<SaveActions>();
        return services;
    }
}
