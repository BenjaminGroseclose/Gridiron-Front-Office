// The process the "killed process leaves no partial write" torture test launches and kills (DATA_ACCESS_GUIDE.md §9).
// Usage: Crasher <data root> <save path>. Opens the save, starts ONE command that inserts 100,000 rows into the test-only
// CrashProbe table, prints "WRITING" halfway through, then waits forever inside the still-open transaction. The test
// kills it there and checks the save is intact with no rows added.
using GridironFrontOffice.Application.Data;
using GridironFrontOffice.Application.Saves;
using GridironFrontOffice.Core.Results;
using GridironFrontOffice.Infrastructure;
using GridironFrontOffice.Infrastructure.Logging;
using GridironFrontOffice.Infrastructure.Paths;
using Microsoft.Extensions.DependencyInjection;

const int Rows = 100_000;

var services = new ServiceCollection()
    .AddLogging()
    .AddGfoInfrastructure(AppPaths.ForRoot(args[0]), AppIdentity.Parse("0.0.0-crasher", "Test"));
await using var provider = services.BuildServiceProvider();

var opened = await provider.GetRequiredService<ISaveManager>().OpenAsync(args[1]);
if (opened.IsFailure)
{
    await Console.Error.WriteLineAsync(opened.Error!.ToString());
    return 2;
}

await provider.GetRequiredService<ISaveWriter>().ExecuteAsync("Crasher.Insert", async (uow, ct) =>
{
    await using var insert = uow.CreateCommand("INSERT INTO CrashProbe (Value) VALUES ($value);");
    var value = insert.CreateParameter();
    value.ParameterName = "$value";
    insert.Parameters.Add(value);
    await insert.PrepareAsync(ct);

    for (var i = 0; i < Rows; i++)
    {
        value.Value = i;
        await insert.ExecuteNonQueryAsync(ct);
        if (i == Rows / 2)
        {
            await Console.Out.WriteLineAsync("WRITING");
            await Console.Out.FlushAsync(ct);
        }
    }

    await Task.Delay(Timeout.Infinite, ct);   // never commits: the test kills the process first
    return Result.Ok();
});

return 0;
