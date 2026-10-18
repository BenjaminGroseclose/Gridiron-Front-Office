using GridironFrontOffice.Application.Data;
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
    /// Registers paths, the save manager and the data layer. Deliberately absent: AddDbContext and
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
        services.AddSingleton<SaveManager>();
        services.AddSingleton<ISaveManager>(sp => sp.GetRequiredService<SaveManager>());
        return services;
    }
}
