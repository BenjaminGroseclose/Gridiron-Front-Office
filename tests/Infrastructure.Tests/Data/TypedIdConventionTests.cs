using GridironFrontOffice.Core.Ids;
using GridironFrontOffice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Infrastructure.Tests.Data;

/// <summary>
/// D-015 and the Spike S0a finding: typed IDs map to INTEGER through one convention, and generated typed-ID keys get
/// AUTOINCREMENT so SQLite never reuses an ID that history may still reference. The first real IDs arrive in M1, so
/// this uses a test-only WidgetId with the same convention LeagueDbContext applies.
/// </summary>
public sealed class TypedIdConventionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void A_generated_typed_ID_key_is_an_AUTOINCREMENT_integer()
    {
        using var temp = new TempFolder();
        using var db = WidgetContext.At(Path.Combine(temp.Path, "widgets.db"));

        var script = db.Database.GenerateCreateScript();

        script.ShouldContain("\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Widgets\" PRIMARY KEY AUTOINCREMENT");
    }

    [Fact]
    public async Task IDs_round_trip_and_a_deleted_top_ID_is_never_reused()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "widgets.db");
        await using (var db = WidgetContext.At(path))
        {
            await db.Database.EnsureCreatedAsync(Ct);
            db.Widgets.AddRange(new Widget { Name = "a" }, new Widget { Name = "b" }, new Widget { Name = "c" });
            await db.SaveChangesAsync(Ct);
            db.Widgets.Remove(await db.Widgets.SingleAsync(w => w.Id == new WidgetId(3), Ct));
            await db.SaveChangesAsync(Ct);
            db.Widgets.Add(new Widget { Name = "d" });
            await db.SaveChangesAsync(Ct);
        }

        await using var reread = WidgetContext.At(path);
        var widgets = await reread.Widgets.OrderBy(w => w.Id).Select(w => new { w.Id, w.Name }).ToListAsync(Ct);
        widgets.Select(w => w.Id).ShouldBe([new WidgetId(1), new WidgetId(2), new WidgetId(4)]);
        widgets.Last().Name.ShouldBe("d");
    }

    internal readonly record struct WidgetId(int Value) : IStronglyTypedId;

    internal sealed class Widget
    {
        public WidgetId Id { get; set; }

        public string Name { get; set; } = "";
    }

    private sealed class WidgetContext(DbContextOptions<WidgetContext> options) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        public static WidgetContext At(string path) =>
            new(new DbContextOptionsBuilder<WidgetContext>().UseSqlite($"Data Source={path};Pooling=False").Options);

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            TypedIds.ApplyConversions(configurationBuilder, [typeof(WidgetId)]);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>().Property(w => w.Id).ValueGeneratedOnAdd();
            TypedIds.UseAutoincrementForGeneratedKeys(modelBuilder);
        }
    }
}
