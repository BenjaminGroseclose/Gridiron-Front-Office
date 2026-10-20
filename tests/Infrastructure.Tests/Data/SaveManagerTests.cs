using System.Diagnostics;
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Platform;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Leagues;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure.Saves;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

public sealed class SaveManagerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_new_save_is_a_WAL_file_signed_GFO1_with_its_SaveInfo_row()
    {
        await using var save = await TestSave.CreateAsync("Wolves Dynasty");

        Path.GetFileName(save.Save.FilePath).ShouldBe("Wolves Dynasty.gfo");
        save.Save.SchemaVersion.ShouldBeGreaterThan(0);
        save.ScalarText("PRAGMA journal_mode").ShouldBe("wal");
        save.Scalar("PRAGMA application_id").ShouldBe(SqliteFile.ApplicationID);
        save.Scalar("PRAGMA user_version").ShouldBe(save.Save.SchemaVersion);

        var info = await save.Reader.QueryAsync((db, ct) => db.SaveInfo.SingleAsync(ct), Ct);
        info.SaveID.ShouldBe(save.Save.SaveID);
        info.LeagueName.ShouldBe("Wolves Dynasty");
        info.SchemaVersion.ShouldBe(save.Save.SchemaVersion);
        info.AppVersionCreated.ShouldBe("0.0.1+test");
        (info.IsIronman, info.ClockMode, info.ClockPreset, info.InjuryFrequency, info.CareerCanEnd)
            .ShouldBe((false, ClockMode.Live, ClockPreset.Standard, InjuryFrequency.Realistic, false));   // the decided defaults
        save.ScalarText("SELECT ClockMode FROM SaveInfo").ShouldBe("Live");   // enums stored by name
        info.Generation.ShouldBe(1);   // the creating command
    }

    [Fact]
    public async Task Names_are_validated_and_files_never_collide()
    {
        await using var save = await TestSave.CreateAsync("Wolves: Year 1?");

        Path.GetFileName(save.Save.FilePath).ShouldBe("Wolves_ Year 1_.gfo");
        (await save.Saves.CreateAsync("   ", Ct)).Error!.Code.ShouldBe("InvalidLeagueName");
        (await save.Saves.CreateAsync(new string('x', SaveRules.MaxLeagueNameLength + 1), Ct)).Error!.Code.ShouldBe("InvalidLeagueName");

        var second = await save.Saves.CreateAsync("Wolves: Year 1?", Ct);
        Path.GetFileName(second.Value.FilePath).ShouldBe("Wolves_ Year 1_ (2).gfo");
    }

    [Fact]
    public async Task Closing_releases_the_file_and_records_the_session()
    {
        await using var save = await TestSave.CreateAsync();
        var path = save.Save.FilePath;
        var events = new List<object>();
        using var closed = save.Events.Subscribe<SaveClosed>(events.Add);

        await save.Saves.CloseAsync(Ct);

        save.Saves.Current.ShouldBeNull();
        events.ShouldBe([new SaveClosed(save.Save.SaveID)]);
        File.Exists(path + "-wal").ShouldBeFalse("The WAL is checkpointed into the file.");
        save.Scalar("SELECT Generation FROM SaveInfo").ShouldBe(2);   // create + the close that records play time
        File.Delete(path);                                              // throws if anything still holds the file
        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public async Task A_closed_save_reopens_and_the_data_layer_follows_it()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Saves.CloseAsync(Ct);
        await Should.ThrowAsync<InvalidOperationException>(() => save.Reader.QueryAsync((db, ct) => db.SaveInfo.CountAsync(ct), Ct));

        var reopened = await save.Saves.OpenAsync(save.Save.FilePath, Ct);

        reopened.IsSuccess.ShouldBeTrue(reopened.ToString());
        reopened.Value.ShouldBe(save.Save with { Upgraded = false });
        save.Saves.Current.ShouldBe(reopened.Value);
        (await save.Reader.QueryAsync((db, ct) => db.SaveInfo.Select(s => s.Generation).SingleAsync(ct), Ct)).ShouldBe(3);
    }

    [Fact]
    public async Task Opening_another_save_closes_the_current_one_first()
    {
        await using var first = await TestSave.CreateAsync("First");
        var second = await first.Saves.CreateAsync("Second", Ct);
        await first.Saves.CloseAsync(Ct);
        var order = new List<string>();
        using var opened = first.Events.Subscribe<SaveOpened>(e => order.Add("opened " + e.Save.LeagueName));
        using var closed = first.Events.Subscribe<SaveClosed>(e => order.Add("closed"));
        await first.Saves.OpenAsync(first.Save.FilePath, Ct);

        var result = await first.Saves.OpenAsync(second.Value.FilePath, Ct);

        result.Value.LeagueName.ShouldBe("Second");
        order.ShouldBe(["opened First", "closed", "opened Second"]);
        File.Delete(first.Save.FilePath);   // the first file was released
    }

    [Fact]
    public async Task A_missing_file_is_refused()
    {
        await using var save = await TestSave.CreateAsync();

        var result = await save.Saves.OpenAsync(Path.Combine(save.RootPath, "Saves", "Nope.gfo"), Ct);

        result.Error!.Code.ShouldBe("SaveNotFound");
        save.Saves.Current.ShouldBe(null, "Opening closes the current save first, even when the new one fails.");
    }

    [Fact]
    public async Task A_file_that_is_not_SQLite_is_refused()
    {
        await using var save = await TestSave.CreateAsync();
        var path = Path.Combine(save.RootPath, "Saves", "Notes.gfo");
        await File.WriteAllTextAsync(path, "This is a text file with a .gfo extension, not a save.", Ct);

        (await save.Saves.OpenAsync(path, Ct)).Error!.Code.ShouldBe("NotAGfoSave");
    }

    [Fact]
    public async Task Another_apps_SQLite_database_is_refused()
    {
        await using var save = await TestSave.CreateAsync();
        var path = Path.Combine(save.RootPath, "Saves", "Other.gfo");
        await using (var other = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
        {
            await other.OpenAsync(Ct);
            await using var create = other.CreateCommand();
            create.CommandText = "CREATE TABLE Recipes (Name TEXT);";
            await create.ExecuteNonQueryAsync(Ct);
        }

        (await save.Saves.OpenAsync(path, Ct)).Error!.Code.ShouldBe("NotAGfoSave");
    }

    [Fact]
    public async Task A_damaged_save_is_refused()
    {
        await using var save = await TestSave.CreateAsync();
        var path = save.Save.FilePath;
        await save.Saves.CloseAsync(Ct);
        await TestSave.ScrambleAsync(path);

        var result = await save.Saves.OpenAsync(path, Ct);

        result.Error!.Code.ShouldBe("SaveDamaged");
    }

    [Fact]
    public async Task A_save_from_a_newer_app_is_refused()
    {
        await using var save = await TestSave.CreateAsync();
        await save.Writer.ExecuteAsync("Test.FromTheFuture", async (uow, ct) =>
        {
            await using var future = uow.CreateCommand(
                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29991231000000_FromTheFuture', '99.0.0');");
            await future.ExecuteNonQueryAsync(ct);
            (await uow.Db.SaveInfo.SingleAsync(ct)).AppVersionLastOpened = "9.9.0";
            return Result.Ok();
        }, Ct);
        await save.Saves.CloseAsync(Ct);

        var result = await save.Saves.OpenAsync(save.Save.FilePath, Ct);

        result.Error!.Code.ShouldBe("SaveFromNewerVersion");
        result.Error.Message.ShouldContain("(9.9.0)");
        File.Delete(save.Save.FilePath);   // refused saves are released too
    }

    [Fact]
    public async Task With_no_save_open_the_data_layer_says_so()
    {
        using var temp = new TempFolder();
        await using var services = TestSave.BuildServices(temp.Path);

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            services.GetRequiredService<ISaveWriter>().ExecuteAsync("Test.NoSave", (_, _) => Task.FromResult(Result.Ok()), Ct));

        error.Message.ShouldContain("No save is open");
    }

    [Fact]
    public void No_DbContext_is_registered_in_DI()
    {
        using var temp = new TempFolder();
        var services = new ServiceCollection()
            .AddLogging()
            .AddGfoInfrastructure(Infrastructure.Paths.AppPaths.ForRoot(temp.Path), AppIdentity.Parse("0.0.1", "Test"));

        services.Where(d => typeof(DbContext).IsAssignableFrom(d.ServiceType)
                            || d.ServiceType.Name.StartsWith("DbContextOptions", StringComparison.Ordinal)
                            || d.ServiceType.Name.StartsWith("IDbContextFactory", StringComparison.Ordinal))
            .ShouldBeEmpty("Contexts are created per command and per query from the open save (DATA_ACCESS_GUIDE.md §3.6).");
    }

    [Fact]
    public async Task A_process_killed_mid_command_leaves_the_save_intact_and_unchanged()
    {
        await using var save = await TestSave.CreateAsync();
        await save.CreateProbeTableAsync("CrashProbe");
        await save.Saves.CloseAsync(Ct);

        using (var crasher = Process.Start(new ProcessStartInfo("dotnet")
        {
            ArgumentList = { CrasherPath(), save.RootPath, save.Save.FilePath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!)
        {
            try
            {
                var line = await crasher.StandardOutput.ReadLineAsync(Ct).AsTask().WaitAsync(TimeSpan.FromSeconds(30), Ct);
                if (line != "WRITING")
                {
                    // Only now read stderr: the Crasher never exits on its own, so reading it eagerly would hang.
                    crasher.Kill(entireProcessTree: true);
                    line.ShouldBe("WRITING", await crasher.StandardError.ReadToEndAsync(Ct));
                }
            }
            finally
            {
                crasher.Kill(entireProcessTree: true);   // mid-transaction, 50,000 rows in (and never an orphan)
                await crasher.WaitForExitAsync(Ct);
            }
        }

        save.ScalarText("PRAGMA integrity_check").ShouldBe("ok");
        save.Scalar("SELECT COUNT(*) FROM CrashProbe").ShouldBe(0);
        (await save.Saves.OpenAsync(save.Save.FilePath, Ct)).IsSuccess.ShouldBeTrue("The save still opens normally.");
    }

    /// <summary>The Crasher is built alongside the tests (a non-referencing project reference) into its own bin folder.</summary>
    private static string CrasherPath()
    {
        var testsBin = AppContext.BaseDirectory;   // …\tests\Infrastructure.Tests\bin\<Configuration>\net10.0\
        var sep = Path.DirectorySeparatorChar;
        var crasherBin = testsBin.Replace($"{sep}Infrastructure.Tests{sep}bin{sep}", $"{sep}Infrastructure.Tests.Crasher{sep}bin{sep}", StringComparison.Ordinal);
        var dll = Path.Combine(crasherBin, "GridironFrontOffice.Infrastructure.Tests.Crasher.dll");
        File.Exists(dll).ShouldBeTrue($"The Crasher wasn't built at {dll}.");
        return dll;
    }
}
