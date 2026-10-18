using System.Globalization;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.LiveEvents;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using GridironFrontOffice.Infrastructure.Tests.Saves;
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

    public ISaveCatalog Catalog => Services.GetRequiredService<ISaveCatalog>();

    public ISnapshotService Snapshots => Services.GetRequiredService<ISnapshotService>();

    public FakePlatformServices Platform => (FakePlatformServices)Services.GetRequiredService<IPlatformServices>();

    public FakeLiveEvents LiveEvents => (FakeLiveEvents)Services.GetRequiredService<ILiveEventStatus>();

    public string SavesPath => Path.Combine(RootPath, "Saves");

    public string SnapshotsPath => Path.Combine(RootPath, "Snapshots", Save.SaveId.ToString());

    public static async Task<TestSave> CreateAsync(string leagueName = "Torture League", Action<IServiceCollection>? configure = null)
    {
        var temp = new TempFolder();
        var services = BuildServices(temp.Path, configure);
        var created = await services.GetRequiredService<ISaveManager>().CreateAsync(leagueName, TestContext.Current.CancellationToken);
        created.IsSuccess.ShouldBeTrue(created.ToString());
        return new TestSave(temp, services, created.Value);
    }

    /// <summary>The app's registrations, plus the test fakes for the platform and live events.</summary>
    public static ServiceProvider BuildServices(string root, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPlatformServices>(new FakePlatformServices(Path.Combine(root, ".trash")))
            .AddSingleton<ILiveEventStatus>(new FakeLiveEvents())
            .AddGfoInfrastructure(AppPaths.ForRoot(root), AppIdentity.Parse("0.0.1+test", "Test"));
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>Creates the test-only table in a committed command (the real schema stays clean).</summary>
    public Task CreateProbeTableAsync(string table = "Probe") =>
        Writer.ExecuteAsync("Test.CreateProbeTable", async (uow, ct) =>
        {
            await using var create = uow.CreateCommand($"CREATE TABLE {table} (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);");
            await create.ExecuteNonQueryAsync(ct);
            return Result.Ok();
        });

    /// <summary>Inserts a probe row in its own command.</summary>
    public Task InsertProbeAsync(string value, string table = "Probe") =>
        Writer.ExecuteAsync("Test.InsertProbe", async (uow, ct) =>
        {
            await using var insert = uow.CreateCommand($"INSERT INTO {table} (Value) VALUES ($value);");
            insert.Parameters.Add(new SqliteParameter("$value", value));
            await insert.ExecuteNonQueryAsync(ct);
            return Result.Ok();
        });

    /// <summary>A single value read through an independent, unpooled read-only connection.</summary>
    public long Scalar(string sql) => Convert.ToInt64(ScalarObject(sql), CultureInfo.InvariantCulture);

    public string? ScalarText(string sql) => Convert.ToString(ScalarObject(sql), CultureInfo.InvariantCulture);

    /// <summary>The same, against any file (a snapshot, another save).</summary>
    public static string? ScalarText(string path, string sql)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    /// <summary>Changes a closed file directly (a pragma, a row), as another app or a broken disk would.</summary>
    public static void Tamper(string path, string sql)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>Keeps page 1 (the header and schema, so it still looks like a GFO save) and scrambles every other page.</summary>
    public static async Task ScrambleAsync(string path)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Write);
        file.Seek(4096, SeekOrigin.Begin);
        await file.WriteAsync(Enumerable.Repeat((byte)0xEE, (int)file.Length - 4096).ToArray(), TestContext.Current.CancellationToken);
    }

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
