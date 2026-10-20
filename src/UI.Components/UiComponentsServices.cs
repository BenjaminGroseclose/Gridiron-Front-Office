using GridironFrontOffice.UI.Components.Overlays;
using GridironFrontOffice.UI.Components.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Components;

/// <summary>The design system's services. The hosts reach it through the UI layer's <c>AddGfoUi</c>.</summary>
public static class UiComponentsServices
{
    /// <summary>
    /// Registers GfoThemeService, IGfoDialogs and IGfoToasts. Scoped, like Fluent's services they sit on: one per page
    /// (a DevHost circuit), which in the desktop host is the app's lifetime.
    /// </summary>
    public static IServiceCollection AddGfoUiComponents(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<GfoThemeService>();
        services.AddScoped<IGfoDialogs, GfoDialogs>();
        services.AddScoped<IGfoToasts, GfoToasts>();
        return services;
    }
}
