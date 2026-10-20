using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.LiveEvents;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Infrastructure.Data;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Saves;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GridironFrontOffice.Infrastructure;

/// <summary>DATA_ACCESS_GUIDE.md §3.6: the persistence services both hosts register.</summary>
public static class InfrastructureServices
{
    /// <summary>
    /// Registers paths, the save manager, catalog and snapshots, and the data layer. The host registers
    /// <see cref="IPlatformServices"/> (the Recycle Bin and Explorer). Deliberately absent: AddDbContext and
    /// AddDbContextFactory. Contexts are created per command and per query from the open save (a test checks this).
    /// </summary>
    public static IServiceCollection AddGfoInfrastructure(this IServiceCollection services, IAppPaths paths, AppIdentity app)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(app);

        services.AddSingleton(paths);
        services.AddSingleton(app);
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<SqlitePragmaInterceptor>();
        services.AddSingleton<LeagueSessionAccessor>();
        services.AddSingleton<DomainEventDispatcher>();
        services.AddSingleton<IDomainEventDispatcher>(sp => sp.GetRequiredService<DomainEventDispatcher>());
        services.AddSingleton<SaveWriter>();
        services.AddSingleton<ISaveWriter>(sp => sp.GetRequiredService<SaveWriter>());
        services.AddSingleton<ISaveReader, SaveReader>();
        services.AddSingleton<SaveCatalog>();
        services.AddSingleton<ISaveCatalog>(sp => sp.GetRequiredService<SaveCatalog>());
        services.TryAddSingleton<SnapshotOptions>();
        services.AddSingleton<SnapshotService>();
        services.AddSingleton<ISnapshotService>(sp => sp.GetRequiredService<SnapshotService>());
        services.AddSingleton<SaveManager>();
        services.AddSingleton<ISaveManager>(sp => sp.GetRequiredService<SaveManager>());

        // Until the live event runner arrives (M2), nothing is ever running. WP9's AddGfoApplication takes this over.
        services.TryAddSingleton<ILiveEventStatus, NoLiveEvents>();
        return services;
    }
}
