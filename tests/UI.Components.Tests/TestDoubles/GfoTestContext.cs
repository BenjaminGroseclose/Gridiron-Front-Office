using Bunit;
using GridironFrontOffice.UI.Components.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GridironFrontOffice.UI.Components.Tests.TestDoubles;

/// <summary>
/// A bUnit context with Fluent's components, GFO's design system services, and a fake Fluent theme service, so
/// GfoThemeService runs for real against recorded calls. JS interop is loose (Fluent's web components call into JS),
/// and gfo-theme.js reports Windows as dark unless a test says otherwise.
/// </summary>
public abstract class GfoTestContext : BunitContext
{
    private const string ThemeModule = "./_content/GridironFrontOffice.UI.Components/gfo-theme.js";

    private readonly BunitJSModuleInterop _themeModule;

    protected GfoTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _themeModule = JSInterop.SetupModule(ThemeModule);
        WindowsIs(dark: true);
        Applied = _themeModule.SetupVoid("applied", _ => true);
        Applied.SetVoidResult();

        Services.AddFluentUIComponents();
        Services.AddGfoUiComponents();
        Services.AddSingleton<IThemeService>(Fluent);
    }

    internal FakeThemeService Fluent { get; } = new();

    /// <summary>The calls to gfo-theme.js's <c>applied</c>: the mode and density written to the page.</summary>
    protected JSRuntimeInvocationHandler Applied { get; }

    protected GfoThemeService Theme => Services.GetRequiredService<GfoThemeService>();

    /// <summary>What the page reports for the Windows light/dark setting when the theme service starts.</summary>
    protected void WindowsIs(bool dark) => _themeModule.Setup<bool>("start", _ => true).SetResult(dark);
}
