using System.Globalization;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

/// <summary>
/// A real save in a temp folder, created through ISaveManager exactly as the app does, with the data services the
/// app registers. Probes read the file through their own connection, so assertions never trust the code under test.
/// </summary>
internal sealed class TestSave : IAsyncDisposable
{
    private readonly TempFolder _temp;

    private TestSave(TempFolder temp, ServiceProvider services, OpenSave save)
    {
        _temp = temp;
        Services = services;
        Save = save;
    }

    public ServiceProvider Services { get; }

    public OpenSave Save { get; }

    public string RootPath => _temp.Path;

    public ISaveWriter Writer => Services.GetRequiredService<ISaveWriter>();

    public ISaveReader Reader => Services.GetRequiredService<ISaveReader>();

    public ISaveManager Saves => Services.GetRequiredService<ISaveManager>();

    public IDomainEventDispatcher Events => Services.GetRequiredService<IDomainEventDispatcher>();

    public static async Task<TestSave> CreateAsync(string leagueName = "Torture League")
    {
        var temp = new TempFolder();
        var services = BuildServices(temp.Path);
        var created = await services.GetRequiredService<ISaveManager>().CreateAsync(leagueName, TestContext.Current.CancellationToken);
        created.IsSuccess.ShouldBeTrue(created.ToString());
        return new TestSave(temp, services, created.Value);
    }

    public static ServiceProvider BuildServices(string root) =>
        new ServiceCollection()
            .AddLogging()
            .AddGfoInfrastructure(AppPaths.ForRoot(root), AppIdentity.Parse("0.0.1+test", "Test"))
            .BuildServiceProvider();

    /// <summary>Creates the test-only table in a committed command (the real schema stays clean).</summary>
    public Task CreateProbeTableAsync(string table = "Probe") =>
        Writer.ExecuteAsync("Test.CreateProbeTable", async (uow, ct) =>
        {
            await using var create = uow.CreateCommand($"CREATE TABLE {table} (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);");
            await create.ExecuteNonQueryAsync(ct);
            return Result.Ok();
        });

    /// <summary>A single value read through an independent, unpooled read-only connection.</summary>
    public long Scalar(string sql) => Convert.ToInt64(ScalarObject(sql), CultureInfo.InvariantCulture);

    public string? ScalarText(string sql) => Convert.ToString(ScalarObject(sql), CultureInfo.InvariantCulture);

    public async ValueTask DisposeAsync()
    {
        await Saves.CloseAsync();
        await Services.DisposeAsync();
        _temp.Dispose();
    }

    private object? ScalarObject(string sql)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = Save.FilePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
