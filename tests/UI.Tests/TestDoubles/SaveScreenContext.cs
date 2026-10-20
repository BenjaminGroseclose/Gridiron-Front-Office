using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Application.Settings;
using GridironFrontOffice.UI.Components.Overlays;
using GridironFrontOffice.UI.Components.Tests.TestDoubles;
using GridironFrontOffice.UI.Tests.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.UI.Tests.TestDoubles;

/// <summary>
/// A bUnit context for the menu screens and the shell: the UI's services with fakes for saves, the save list, the
/// event hub, the platform, dialogs, toasts and settings.
/// </summary>
public abstract class SaveScreenContext : GfoTestContext
{
    protected SaveScreenContext()
    {
        Saves = new FakeSaveManager(Events);
        Services.AddGfoUi();
        Services.AddSingleton<ISaveManager>(Saves);
        Services.AddSingleton<ISaveCatalog>(Catalog);
        Services.AddSingleton<IDomainEventDispatcher>(Events);
        Services.AddSingleton<IPlatformServices>(Platform);
        Services.AddSingleton<IGfoDialogs>(Dialogs);
        Services.AddSingleton<IGfoToasts>(Toasts);
        Services.AddSingleton<ISettingsStore>(new FakeSettingsStore(new AppSettings()));
        Services.AddSingleton(App);
    }

    /// <summary>The build the screens show: version 0.0.1 from commit 94b9338, a Debug build.</summary>
    internal static AppIdentity App { get; } = AppIdentity.Parse("0.0.1+94b9338c0ffee", "Debug");

    internal FakeDomainEvents Events { get; } = new();

    internal FakeSaveManager Saves { get; }

    internal FakeSaveCatalog Catalog { get; } = new();

    internal FakePlatformServices Platform { get; } = new();

    internal FakeDialogs Dialogs { get; } = new();

    internal FakeToasts Toasts { get; } = new();

    /// <summary>Where the app is now, relative to the base: "", "league", "load"…</summary>
    protected string Location
    {
        get
        {
            var navigation = Services.GetRequiredService<NavigationManager>();
            return navigation.ToBaseRelativePath(navigation.Uri);
        }
    }
}
