# M1 · WP5 builder's guide: the schema and `CreateLeague`

> **Status:** 📝 **Ready to build** (2026-10-29; checked against the code 2026-10-30, §2 walked through with the owner) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP5](M1_THE_LEAGUE.md#wp5--schema-and-createleague-m) · **Size:** M · **Written:** 2026-10-29
>
> WP5 puts WP4's league in the save. One migration adds the league's eight tables and `SaveInfo`'s league position. `CreateLeague` generates a league outside any transaction, then writes the save and the league in one command ([D-089](../DECISIONS.md#d-089-a-league-and-its-save-are-written-in-one-command-the-cap-lives-in-a-seasons-table)), so a save never exists without its league. A second, small command records the user's team. Nothing on screen changes yet: WP8's wizard is the first caller. When WP5 is done, a seed-42 league round-trips through a real `.gfo` file, column by column.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | [D-089](../DECISIONS.md#d-089-a-league-and-its-save-are-written-in-one-command-the-cap-lives-in-a-seasons-table) (your answers of 2026-10-29) and the doc updates: PERSISTENCE §2.1 and §5, F-12 §5 (the `Seasons` row), F-03 §5 (the column names below) and the plan. Two probes of EF Core 10 + SQLite (§9). |
| **You build** (§3, §4) | Three slices: the schema; writing a league; the user's team. |
| **Claude writes the tests** (§5) | Per slice, as each one lands, each proven to fail on a planted problem. |

**Read first:**
- [DATA_ACCESS_GUIDE.md](../architecture/DATA_ACCESS_GUIDE.md) §5.3–5.4 (operations and bulk writes) and §10 (the checklist);
- PERSISTENCE §5, F-02 §5, F-03 §5 and F-12 §5 (the tables);
- [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) #6, #7, #9 and #12, and D-089;
- CLAUDE.md's "Data access".

**AI counterpart:** none. No team acts yet, and every team is written by the same code.

## 1. What you'll build

```
src/Core/
├─ IDs/EntityIDs.cs                  − StadiumID (D-078 #6)
├─ Leagues/
│  ├─ LeagueEntities.cs              Conference, Division, Team, College, LeagueSeason (§3.1)
│  ├─ Phase.cs                       the ten phases of CALENDAR_AND_PHASES §2 (§3.2)
│  ├─ LeagueCalendar.cs              TrainingCampOpens(season) (§3.2)
│  └─ SaveInfo.cs                    + UserTeamID, Seed, CurrentSeason, CurrentPhase, CurrentDate (§3.2)
├─ Players/Player.cs                 the wide Players row (§3.1)
└─ Contracts/Contract.cs             Contract, ContractYear (§3.1)
src/Application/
├─ Data/DataContracts.cs             + eight DbSets on ILeagueDb, eight queryables on ILeagueReadDb
├─ Saves/ISaveManager.cs             + CreateAsync(name, write, ct) (D-089)
└─ Leagues/
   ├─ CreateLeague.cs                NewLeague, LeagueCreationStep, CreateLeague (§4.2)
   ├─ LeagueWriter.cs                the bulk insert, an operation on IUnitOfWork (§4.2)
   ├─ LeagueCommands.cs              PickUserTeamAsync, UserTeamPicked, LeagueErrors (§4.3)
   └─ LeagueLog.cs                   events 1304–1305 (§4.4)
src/Infrastructure/
├─ Data/
│  ├─ LeagueDbContext.cs             + the DbSets, deletes restricted (§3.3)
│  ├─ TypedIDs.cs                    every typed-ID key generated and AUTOINCREMENT (§3.3)
│  ├─ Configurations/                + LeagueConfigurations.cs, PlayerConfiguration.cs, ContractConfigurations.cs; SaveInfo's FK
│  └─ Migrations/                    + TheLeague (schema 2)
└─ Saves/SaveManager.cs              CreateAsync(name, write): the write step inside Save.Create (§4.2)
tests/ (Claude)                      Core.Tests/Leagues, Infrastructure.Tests/Data and /Leagues
```

Names are suggestions: keep them unless you have a reason not to. The configuration files follow the entity files, one configuration class per entity, like M0's `SaveInfoConfiguration`.

## 2. Decisions this guide makes

D-089 settled the two questions. These are the smaller choices, so the code, the tests and WP6 agree:

| Choice | Rule | Why |
|---|---|---|
| **IDs** | Every typed-ID key is generated and AUTOINCREMENT, set in one place (`TypedIDs`), so no entity can forget. The bulk insert still writes its own IDs: conferences, divisions and teams numbered from 1 in the team set's order, colleges in the list's order, **`PlayerID = slot + 1`**, contracts in player order. SQLite moves its counter past explicit IDs, so M4's contracts and M5's prospects continue from the highest (probed, §9) | IDs are known before the insert, so no read-back, and the same seed gives the same IDs. The plan preferred the convention for rows added later; both are needed |
| **Columns are C# names** | The 11 measurables, 42 attributes and 6 traits are columns named after their enum members, each a `double` property on `Player`. The hidden ratings take `HiddenRatings`' names. The bulk insert builds its column list from `Enum.GetNames`, so the SQL can't drift from the enums, and a test checks that `Player` has a property for every member | One name from the data files to the save. `PlayerAttribute`'s summary already says a rename is a schema change |
| **Spec names, settled** | `Experience` (F-12's name; F-03 said `ExperienceSeasons`), `DevelopmentCurve` (F-03 said `DevCurve`), `HometownName` + `HometownState`, and the draft as `DraftSeason`, `DraftRound`, `DraftPick` (overall) and `DraftTeamID`, all four null when undrafted | The code's names (`GeneratedPlayer`, `HiddenRatings`). F-03 and PERSISTENCE are updated |
| **No navigations** | Entities carry foreign-key properties only (`Player.TeamID`), configured with `HasOne<Team>().WithMany().HasForeignKey(…)` | WP6's queries join where they need to. A navigation can be added later without a migration |
| **Deletes are restricted** | Every foreign key is `DeleteBehavior.Restrict`: one loop in `OnModelCreating` | Nothing deletes in M1, and history (players, contracts) is kept forever, so a delete must never cascade by accident |
| **Not saved** | Generation's tier, archetype, age, slot and team strength; the hometown's population; the team's metro (D-078 #12) | Nothing reads them: screens recompute the archetype label and the age, and the slot is `PlayerID − 1` |
| **The league position** | `CurrentSeason` is the generation season, `CurrentPhase` is `TrainingCamp`, and `CurrentDate` is `LeagueCalendar.TrainingCampOpens(season)`: the fourth Tuesday of July (July 22–28), after the July 1 that ages are drawn on. `Seed` is the `ulong` itself, stored as INTEGER bit for bit (probed). `UserTeamID` stays null until the pick | D-049 (M1–M4 leagues open at camp), D-073 (seed + season = the league) |
| **The pick is its own command** | `PickUserTeamAsync(TeamID)` sets `SaveInfo.UserTeamID`, refuses an unknown team, and raises `UserTeamPicked`. There's no once-only rule, because no screen offers a second pick | The plan. WP6's `GameStateStore` reloads on the event |
| **The `created` snapshot** | Taken after the league is written, before the pick. Restoring it brings back the team picker (WP8 sends a save with no user team there) | D-089 |
| **Progress** | `IProgress<LeagueCreationStep>` with two steps, `Generating` and `Saving`, reported by `CreateLeague` | Generation takes about 0.4 s in Debug and the write well under a second: two labels are all the wizard can show. M5's simulated prior season adds steps |
| **Cancel** | `CreateLeague` takes a `CancellationToken`. Generation is pure and isn't interrupted; a cancel while saving rolls back and deletes the file | F-01's cancel, with nothing half-made |
| **The season comes from the caller** | `NewLeague(Name, Seed, Season)`: the wizard passes this year (D-073). Application never reads the clock for it | Deterministic and testable, for the same reason generation takes it |
| **No UI and no DI yet** | M0's New League page keeps creating an empty save through `CreateAsync(name)` until WP8's wizard replaces it. `CreateLeague` and `LeagueCommands` are registered when WP8 first injects them | Their first consumer is WP8 (D-078). WP5 is proven by tests |

## 3. The schema

### 3.1 The entities (Core)

Plain classes with `{ get; set; }`, like `SaveInfo`. They're rows, not definitions: Application's `TeamDefinition` and `CollegeDefinition` stay the files' shapes.

```csharp
// Core/Leagues/LeagueEntities.cs: the league's structure, copied from the team set and the college list at creation
// (F-02 §5, F-03 §5), so a save stays self-contained.

public sealed class Conference
{
    public ConferenceID ConferenceID { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class Division
{
    public DivisionID DivisionID { get; set; }
    public ConferenceID ConferenceID { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
}

/// <summary>A team, with its stadium as columns (D-078 #6). No metro and no strength: nothing reads them.</summary>
public sealed class Team
{
    public TeamID TeamID { get; set; }
    public DivisionID DivisionID { get; set; }
    public string Abbreviation { get; set; } = "";
    public string City { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string PrimaryColor { get; set; } = "";       // #RRGGBB, as in teams.json
    public string SecondaryColor { get; set; } = "";
    public string AccentColor { get; set; } = "";
    public CrestShape CrestShape { get; set; }
    public string CrestMonogram { get; set; } = "";
    public CrestStripe CrestStripe { get; set; }
    public string StadiumName { get; set; } = "";
    public int StadiumCapacity { get; set; }
    public Roof StadiumRoof { get; set; }
    public Surface StadiumSurface { get; set; }
    public ClimateZone StadiumClimate { get; set; }
}

public sealed class College
{
    public CollegeID CollegeID { get; set; }
    public string Name { get; set; } = "";
    public CollegeRegion Region { get; set; }
    public CollegeTier Tier { get; set; }
}

/// <summary>One row per season (D-089): the cap now; M3 and M4 add the rest.</summary>
public sealed class LeagueSeason
{
    public int Season { get; set; }
    public long Cap { get; set; }
}
```

```csharp
// Core/Players/Player.cs
/// <summary>
/// A player's row (F-03 §5): bio and draft history, then one column per Measurable, PlayerAttribute and
/// PersonalityTrait, named after the member, and the hidden ratings. True values: screens see them through
/// IPerception (WP6).
/// </summary>
public sealed class Player
{
    public PlayerID PlayerID { get; set; }
    public TeamID? TeamID { get; set; }                  // null: a free agent
    public PlayerStatus Status { get; set; }             // Active or FreeAgent at creation
    public Position Position { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public CollegeID CollegeID { get; set; }
    public string HometownName { get; set; } = "";
    public string HometownState { get; set; } = "";
    public int Experience { get; set; }
    public int? DraftSeason { get; set; }                // the four draft columns are all null when undrafted
    public int? DraftRound { get; set; }
    public int? DraftPick { get; set; }                  // overall, 1–224
    public TeamID? DraftTeamID { get; set; }
    public int? JerseyNumber { get; set; }               // null for free agents

    // Measurable's members, in its order: inches, pounds, seconds, reps of 225 lb
    public double Height { get; set; }
    // … Weight, Forty, TenSplit, ThreeCone, Shuttle, Bench, Vertical, BroadJump, ArmLength, HandSize

    // PlayerAttribute's 42 members, in its order (0–100, unrounded)
    public double Speed { get; set; }
    // … Acceleration through SnapAccuracy

    // HiddenRatings' fields
    public double Potential { get; set; }
    public DevelopmentCurve DevelopmentCurve { get; set; }
    public double BustRisk { get; set; }
    public double PeakAgeOffset { get; set; }
    public double InjuryProneness { get; set; }

    // PersonalityTrait's members, in its order
    public double WorkEthic { get; set; }
    // … Coachability, Greed, Loyalty, Competitiveness, Ego
}
```

```csharp
// Core/Contracts/Contract.cs (F-12 §5)
public sealed class Contract
{
    public ContractID ContractID { get; set; }
    public PlayerID PlayerID { get; set; }
    public TeamID TeamID { get; set; }                   // his team after the creation trades
    public ContractType Type { get; set; }
    public int SignedSeason { get; set; }
    public int Years { get; set; }
    public long TotalValue { get; set; }
    public long SigningBonus { get; set; }
    public long GuaranteedTotal { get; set; }
    public bool IsActive { get; set; }                   // every M1 contract is
}

/// <summary>One season of a contract, past seasons included (D-074). In M1, CapHit equals CapMath.CapHit (F-12 §5).</summary>
public sealed class ContractYear
{
    public ContractID ContractID { get; set; }
    public int Season { get; set; }
    public long BaseSalary { get; set; }
    public long ProratedBonus { get; set; }
    public long GuaranteedAmount { get; set; }
    public long CapHit { get; set; }
}
```

### 3.2 `SaveInfo`, `Phase` and the calendar (Core)

`SaveInfo` gains the league position (PERSISTENCE §5.1). Update its summary too, since "arrives with league generation in M1" is now.

```csharp
    /// <summary>The user's team, stored only here (D-078 #7). Null until the team is picked (WP5 §4.3).</summary>
    public TeamID? UserTeamID { get; set; }

    /// <summary>The league seed (D-012). With the first season, it is the league (D-073).</summary>
    public ulong Seed { get; set; }

    public int CurrentSeason { get; set; }

    public Phase CurrentPhase { get; set; }

    /// <summary>The league date: it moves only on Advance (CLAUDE.md, "Two clocks").</summary>
    public DateOnly CurrentDate { get; set; }
```

```csharp
// Core/Leagues/Phase.cs
/// <summary>
/// The league year's phases in calendar order (CALENDAR_AND_PHASES.md §2). Saves store the name. M1–M4 leagues open
/// at TrainingCamp (D-049); the phases move from M3 on.
/// </summary>
public enum Phase
{
    TrainingCamp, RegularSeason, Playoffs, SeasonWrap, ResignWindow, Combine, FreeAgency, PreDraft, Draft, OffseasonProgram,
}

// Core/Leagues/LeagueCalendar.cs
/// <summary>The calendar's dates (CALENDAR_AND_PHASES.md §2). M1 needs one; M3 adds the rest.</summary>
public static class LeagueCalendar
{
    /// <summary>The fourth Tuesday of July (July 22–28), when veterans report: after July 1, the date ages are drawn on.</summary>
    public static DateOnly TrainingCampOpens(int season);
}
```

### 3.3 The configurations and the migration (Infrastructure)

One `IEntityTypeConfiguration<T>` per entity. Each one sets the table name (plural), the key and the foreign keys; EF's conventions do the rest: a column per property, REAL for `double`, INTEGER for `int`, `long`, `bool` and the typed IDs, TEXT for strings, `DateOnly` and enums (by name). Non-nullable strings are NOT NULL by convention.

```csharp
internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(x => x.PlayerID);
        builder.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamID);
        builder.HasOne<Team>().WithMany().HasForeignKey(x => x.DraftTeamID);
        builder.HasOne<College>().WithMany().HasForeignKey(x => x.CollegeID);
    }
}
```

| Table | Key | Foreign keys |
|---|---|---|
| `Conferences` | `ConferenceID` | — |
| `Divisions` | `DivisionID` | `ConferenceID` |
| `Teams` | `TeamID` | `DivisionID` |
| `Colleges` | `CollegeID` | — |
| `Seasons` | `Season`, **`ValueGeneratedNever()`**: EF makes an `int` key AUTOINCREMENT otherwise (probed) | — |
| `Players` | `PlayerID` | `TeamID?`, `DraftTeamID?` (both to `Teams`), `CollegeID` |
| `Contracts` | `ContractID` | `PlayerID`, `TeamID` |
| `ContractYears` | `(ContractID, Season)` | `ContractID` |
| `SaveInfo` | `SaveID` (as now) | **`UserTeamID?`** to `Teams` |

EF adds an index for every foreign key, which covers M1's reads (a team's players, a player's contract). WP6 adds others if a query plan asks for one (PERSISTENCE §5.6).

**`LeagueDbContext`** gets the eight `DbSet`s, the explicit `ILeagueReadDb` queryables beside them (like `SaveInfo`'s), and two lines in `OnModelCreating`:

```csharp
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeagueDbContext).Assembly);   // Fluent API keeps Core EF-free

        // History is kept forever, so a delete never cascades (WP5 §2)
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        TypedIDs.UseAutoincrementForTypedIDKeys(modelBuilder);
    }
```

**`TypedIDs`**: `UseAutoincrementForGeneratedKeys` becomes `UseAutoincrementForTypedIDKeys`. It drops the `ValueGenerated == OnAdd` filter and sets both, so a typed-ID key never needs opting in (the plan's "preferred" fix). Reword its summary to say so: EF treats a converted key as not generated, so this sets every typed-ID key generated and AUTOINCREMENT, and a bulk insert may still write its own IDs.

```csharp
            modelBuilder.Entity(entity.ClrType).Property(property.Name).ValueGeneratedOnAdd().UseAutoincrement();
```

**The migration** (stop the DevHost first; `dotnet ef` builds Infrastructure):

```powershell
dotnet tool restore
dotnet ef migrations add TheLeague --project src/Infrastructure --output-dir Data/Migrations
```

Then make **one edit** in the generated file: `CurrentPhase`'s `AddColumn` gets `defaultValue: "TrainingCamp"` instead of `""`. An M0 save upgrades through this migration, and EF can't read an empty string as a `Phase`, so the open would throw. Read the rest of the file before you commit it. SQLite can't add a foreign key to an existing table, so EF rebuilds `SaveInfo`; that's expected. The schema version becomes 2.

## 4. The code, slice by slice

### 4.1 Slice 1: the schema

§3 in full: the entities, `SaveInfo`, `Phase`, `LeagueCalendar`, the configurations, `LeagueDbContext`, `TypedIDs`, the DbSets on `ILeagueDb` and `ILeagueReadDb`, `StadiumID` removed, and the migration. No behavior changes yet, so every M0 test should still pass, apart from two that Claude updates: `TypedIDConventionTests` (the convention) and `TypedIDTests.The_M1_IDs_are_all_there`, which lists `StadiumID` and fails as soon as it's gone.

### 4.2 Slice 2: writing a league

**`ISaveManager`** gains the overload D-089 describes. The M0 one stays, with a summary that says what it now is.

```csharp
    /// <summary>
    /// Creates a new save in the saves folder with <paramref name="write"/>'s content, opens it and takes its
    /// <c>created</c> snapshot (D-089). <paramref name="write"/> runs in the same command as the SaveInfo row, after
    /// it's flushed; if it throws or is cancelled, nothing is committed and the file is deleted.
    /// </summary>
    Task<Result<OpenSave>> CreateAsync(string leagueName, Func<IUnitOfWork, CancellationToken, Task> write,
        CancellationToken cancellationToken = default);

    /// <summary>An empty save, with no league: the save-lifecycle tests use it, and M0's New League page until WP8.</summary>
    Task<Result<OpenSave>> CreateAsync(string leagueName, CancellationToken cancellationToken = default);
```

**`SaveManager`** changes in three places:
1. The M0 overload calls the new one with a write step that does nothing (`(_, _) => Task.CompletedTask`).
2. Inside `Save.Create`, after `SaveInfo.Add(…)`: `await uow.Db.SaveChangesAsync(token)`, then `await write(uow, token)`. The flush lets the write step query the row like any command. Today's lambda is synchronous and discards its token (`(uow, _) => { …; return Task.FromResult(Result.Ok()); }`), so it becomes `async (uow, token) => { …; return Result.Ok(); }`, with `ConfigureAwait(false)` on both awaits.
3. Nothing else. The existing `catch` already releases and deletes the file on any exception, cancellation included, and the snapshot already comes after the command.

**`LeagueWriter`** (`Application/Leagues`) is an operation (DATA_ACCESS_GUIDE §5.3), public so the tests can hand it a league:

```csharp
/// <summary>
/// Writes a generated league into the save being created (D-089): an operation inside SaveManager's Save.Create
/// command, through prepared raw inserts (DATA_ACCESS_GUIDE.md §5.4). It assigns the IDs, in order (WP5 §2).
/// </summary>
public static class LeagueWriter
{
    public static Task WriteAsync(IUnitOfWork uow, GeneratedLeague league, ISeedData data, CancellationToken ct);
}
```

Parents before children: every connection has foreign keys on, so the order is the table's.

| Table | Rows (seed 42) | IDs | Values |
|---|---|---|---|
| `Seasons` | 1 | `Season` = the league's | `Cap` = `data.Contracts.Cap` |
| `Conferences` | 2 | 1… in `data.TeamSet.Conferences`' order | key, name |
| `Divisions` | 8 | 1…, conference by conference | key, name, its conference's ID |
| `Teams` | 32 | 1… in `data.TeamSet.Teams`' order, which is also `league.Teams`' | every `TeamDefinition` field but `Metro`; `DivisionID` from the division that lists its `Abbr` |
| `Colleges` | 230 | 1… in `data.Colleges.Colleges`' order | name, region, tier |
| `Players` | 1,846 | `Slot + 1`, in slot order | `TeamID` his team's (null for free agents), `Status` `Active` or `FreeAgent`, `CollegeID` by college name, `DraftTeamID` by `Abbr` |
| `Contracts` | 1,696 | 1…, in `PlayerID` order | `Years` = `Seasons.Count`, `IsActive` = true, `TeamID` his team's |
| `ContractYears` | about 6,500 | `(ContractID, Season)` | each `ContractSeason` |
| `SaveInfo` | the row | — | `Seed`, `CurrentSeason`, `CurrentPhase = TrainingCamp`, `CurrentDate`: a tracked change, which SaveWriter saves at the end |

Look the IDs up through dictionaries built as you insert: team by `Abbr`, college by `Name` (WP2's tests keep both unique). Each table is one prepared statement executed once per row. A small helper keeps the eight inserts short:

```csharp
    /// <summary>One prepared INSERT, executed once per row; each row's values come in the columns' order.</summary>
    private static async Task InsertAsync<T>(IUnitOfWork uow, string table, IReadOnlyList<string> columns,
        IEnumerable<T> rows, Func<T, object?[]> values, CancellationToken ct)
    {
        // The table and column names are constants and enum names, never input
        var command = uow.CreateCommand(
            $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => "$" + c))})");
        await using (command.ConfigureAwait(false))
        {
            var parameters = columns.Select(column =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "$" + column;
                command.Parameters.Add(parameter);
                return parameter;
            }).ToArray();
            command.Prepare();

            foreach (var row in rows)
            {
                var rowValues = values(row);
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameters[i].Value = rowValues[i] ?? DBNull.Value;
                }

                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }
    }
```

The players' columns come from the enums, so they can't drift:

```csharp
    private static readonly string[] PlayerColumns =
    [
        "PlayerID", "TeamID", "Status", "Position", "FirstName", "LastName", "BirthDate", "CollegeID", "HometownName",
        "HometownState", "Experience", "DraftSeason", "DraftRound", "DraftPick", "DraftTeamID", "JerseyNumber",
        .. Enum.GetNames<Measurable>(),
        .. Enum.GetNames<PlayerAttribute>(),
        "Potential", "DevelopmentCurve", "BustRisk", "PeakAgeOffset", "InjuryProneness",
        .. Enum.GetNames<PersonalityTrait>(),
    ];
```

A row's values follow the same order: the bio, then `Enum.GetValues<Measurable>().Select(m => (object?)player.Measurables[m])`, the attributes and the traits the same way. Raw SQL skips EF's converters, so write **enums by name** (`ToString()`) and **typed IDs as `.Value`**. `DateOnly`, `ulong` and `null` (as `DBNull.Value`) bind as EF stores them (§9).

**`CreateLeague`** (`Application/Leagues`):

```csharp
/// <summary>What the wizard asks for (F-01 v0): a name, the seed (random unless typed) and the season (D-073: this year).</summary>
public sealed record NewLeague(string Name, ulong Seed, int Season);

/// <summary>What CreateLeague is doing, for the wizard's progress.</summary>
public enum LeagueCreationStep { Generating, Saving }

/// <summary>
/// New League (WP5): generates the league outside any transaction, then creates the save and the league in one command
/// (D-089). The same seed and season always make the same league. The user's team is picked afterwards, in its own
/// command (LeagueCommands).
/// </summary>
public sealed class CreateLeague(ISaveManager saves, LeagueGenerator generator, ISeedData data, TimeProvider time,
    ILogger<CreateLeague> log)
{
    public async Task<Result<OpenSave>> RunAsync(NewLeague league, IProgress<LeagueCreationStep>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(league);
        var started = time.GetTimestamp();

        progress?.Report(LeagueCreationStep.Generating);
        // CPU only, so off the caller's thread: the wizard keeps painting while it runs
        var generated = await Task.Run(() => generator.Generate(league.Seed, league.Season), cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(LeagueCreationStep.Saving);
        var created = await saves.CreateAsync(league.Name,
            (uow, ct) => LeagueWriter.WriteAsync(uow, generated, data, ct), cancellationToken).ConfigureAwait(false);

        if (created.IsSuccess)
        {
            // event 1304 (§4.4), with the counts and the time since started
        }

        return created;
    }
}
```

A bad name comes back as `SaveErrors.InvalidName`, from `CreateAsync` as before. It's checked after generation, which costs a fraction of a second, and the wizard checks the name first anyway.

### 4.3 Slice 3: the user's team

```csharp
/// <summary>Raised when the user's team is picked; WP6's GameStateStore reloads on it.</summary>
public sealed record UserTeamPicked(TeamID Team) : IDomainEvent;

/// <summary>The league's commands. M1 has one: the user's team, stored only in SaveInfo (D-078 #7).</summary>
public sealed class LeagueCommands(ISaveWriter writer, ILogger<LeagueCommands> log)
{
    /// <summary>Makes <paramref name="team"/> the user's team; an unknown team is refused.</summary>
    public Task<Result<Unit>> PickUserTeamAsync(TeamID team, CancellationToken cancellationToken = default);
}

public static class LeagueErrors
{
    public static DomainError UnknownTeam(TeamID team) => new("UnknownTeam", $"There's no team {team.Value} in this league.");
}
```

Inside `League.PickUserTeam`: read the team's abbreviation (`SingleOrDefaultAsync`; null means unknown, so `Result.Fail`), set `SaveInfo.UserTeamID`, `uow.Raise(new UserTeamPicked(team))`, and return the abbreviation for event 1305, logged after the commit. The command is the pattern of DATA_ACCESS_GUIDE §5.1.

### 4.4 Logging (`Application/Leagues/LeagueLog.cs`)

In the 1300–1329 range, after WP4's 1303:

| ID | Level | Message |
|---|---|---|
| 1304 | Information | `League "{LeagueName}" created: {Teams} teams, {Players} players, {Contracts} contracts, seed {Seed}, season {Season}, in {ElapsedMs} ms` |
| 1305 | Information | `{Abbreviation} is the user's team` |

1304's time covers generation, the write and the `created` snapshot: the plan's < 5 s is read here and measured at the exit (§10), not asserted (D-078 #4). Generation alone is still 1300, and the command alone is SaveWriter's 2000 (or a 2003 warning past 1 s).

## 5. The tests (Claude writes them)

Per slice, each proven to fail on a planted problem. Every save is a real file in a temp folder, through `TestSave`'s services.

| Slice | Test | Checks |
|---|---|---|
| **1** | `LeagueSchemaTests` | The model has no pending changes (`HasPendingModelChanges`: an entity change without a migration fails); every typed-ID key is `AUTOINCREMENT` and `Seasons` and `ContractYears` aren't; no foreign key cascades; `Player` has a `double` property for every `Measurable`, `PlayerAttribute` and `PersonalityTrait` member, and a property of the same name and type for every `HiddenRatings` field (`DevelopmentCurve` is the enum), and each one is a column of `Players` |
| | `TypedIDConventionTests` (updated) | A typed-ID key is generated and AUTOINCREMENT without opting in |
| | `TypedIDTests` (updated) | The M1 IDs without `StadiumID` (D-078 #6) |
| | `SaveUpgradeTests` | A schema-1 save (built by migrating to `InitialCreate` only) opens: upgraded, a `pre-upgrade-v1` snapshot, and `CurrentPhase` readable. The first real upgrade the app has |
| | `LeagueCalendarTests` (Core) | The fourth Tuesday of July for 2026–2060, always July 22–28 |
| **2** | `CreateLeagueTests`: the round trip | Seed 42, season 2026, read back through EF (`ILeagueReadDb`) and compared with the `GeneratedLeague` field by field: the structure against the team set, the colleges, every player column (enum names, dates, nulls, doubles exact), every contract and year, the cap, and `SaveInfo`'s position (`UserTeamID` null) |
| | IDs and counts | 2, 8, 32, 230, 1,846, one contract per rostered player, the years' sum; `PlayerID = slot + 1`; `PRAGMA foreign_key_check` empty; every `CapHit` equals `CapMath.CapHit` (F-12 §5) |
| | Later rows continue | After creation, a contract added through EF gets the highest `ContractID` + 1 |
| | All or nothing | A write step that fails partway (a player whose college isn't in the list) leaves no `.gfo`, `-wal`, `-shm` or snapshot folder, and nothing open, and the exception surfaces; a cancel while saving does the same |
| | The `created` snapshot | Holds the league (read-only, its player count) |
| | Deterministic | Two saves from the same seed and season hold the same rows, apart from `SaveID` and the timestamps |
| | The rest | Progress reports `Generating`, then `Saving`; a blank name is refused with no file; 1304's counts |
| **3** | `LeagueCommandsTests` | The pick sets `UserTeamID`, raises `UserTeamPicked` after the commit and logs 1305; an unknown team is refused, with nothing changed and no event; a second pick replaces the first; restoring `created` brings `UserTeamID` back to null |
| **All** | The torture tests | Unchanged, still green |

## 6. Done when

- [ ] Slice 1: the entities, `SaveInfo`'s position, `Phase`, `LeagueCalendar`, the configurations, the restricted deletes, the `TypedIDs` change, the DbSets, `StadiumID` gone, and the migration (schema 2, with the `CurrentPhase` default).
- [ ] Slice 2: `ISaveManager.CreateAsync(name, write)`, `SaveManager`, `LeagueWriter`, `CreateLeague` and event 1304.
- [ ] Slice 3: `LeagueCommands.PickUserTeamAsync`, `UserTeamPicked`, `LeagueErrors` and event 1305.
- [ ] Every §5 test passes, each proven to fail on a planted problem; the torture tests stay green.
- [ ] Reviewed by Claude against this guide, PERSISTENCE §5, F-02, F-03 and F-12 §5, and DATA_ACCESS_GUIDE §10; `./build.ps1` green.
- [ ] PERSISTENCE §5 marked as built, LOGGING.md §8.2's 1304–1305, CLAUDE.md's gotchas; the plan's WP5 marked done; committed by you.

## 7. Order of work

One commit per slice (D-076), each with its tests:

1. **Slice 1.** Generate the migration last, after the entities and configurations settle, and read it. If you change an entity afterwards, delete the migration and add it again (nothing has shipped).
2. **Slice 2.** `SaveManager`'s change first (Claude's all-or-nothing tests need only it and a failing write step), then `LeagueWriter` and `CreateLeague`. The round trip is the moment of truth: most mismatches are a column name or an enum written as a number.
3. **Slice 3**, the smallest.

Seed 42 should write well under a second: the bulk path costs about 3 µs a row (Spike S0a), and this is about 10,300 rows. Read it from 1304 and SaveWriter's 2000.

## 8. Gotchas

- **Raw SQL skips EF's converters.** Enums go in by name and typed IDs as `.Value`. The round trip reads everything back through EF, so a mismatch fails there, not on a screen in WP9.
- **Foreign keys are on** for every connection (`LeagueSession`), so parents go in before children: §4.2's table order.
- **An `int` key is AUTOINCREMENT by EF's convention; a typed-ID key isn't** (probed, §9). `Seasons.Season` needs `ValueGeneratedNever()`, and `TypedIDs` covers every typed-ID key.
- **The migration's `CurrentPhase` default.** EF writes `""` for a new non-null string column, and an empty string isn't a `Phase`: change it to `"TrainingCamp"`, or every M0 save fails to open.
- **M0 saves upgrade into a save with no league.** Pre-1.0 that's allowed (PERSISTENCE §7): delete your M0 test saves once WP8's screens arrive, since they have nothing to show.
- **Flush before you query.** `SaveManager` flushes `SaveInfo` before the write step so `LeagueWriter` can read it (DATA_ACCESS_GUIDE §5.4's rule).
- **No `try`/`catch` in `LeagueWriter`** (W5): a failure must reach SaveWriter, which rolls back, and `SaveManager`, which deletes the file.
- **Generation stays outside the command.** The writer's gate is held for the whole write step, so anything slow inside it blocks every other command. `CreateLeague` generates first, on the thread pool.
- **`ConfigureAwait(false)` on every await,** and `await using (x.ConfigureAwait(false))`, like `SaveManager`: these are Application's first async lines.
- **`Player` has two relationships to `Teams`** (`TeamID` and `DraftTeamID`). Give each its own `HasForeignKey`, then check that the migration puts two foreign keys on `Players`, on those two columns, and adds no column of its own.
- **The DevHost locks the DLLs,** and `dotnet ef` builds Infrastructure: stop the DevHost before adding the migration.

## 9. The probes (2026-10-29)

Two throwaway EF Core 10.0.13 + SQLite programs, outside the repository, on the same packages as the app:

| Question | Answer |
|---|---|
| Does a `ulong` seed above `long.MaxValue` survive? | Yes, with no converter. EF and a raw parameter both store it as the signed INTEGER with the same bits (`ulong.MaxValue` is stored as −1) and read it back exactly. Only SQL ordering by seed would be wrong, and nothing sorts by seed |
| Does a raw `DateOnly` parameter match EF? | Yes: both store `2026-07-28` as TEXT |
| Do explicit IDs break AUTOINCREMENT? | No: inserting IDs 1–3 by hand set `sqlite_sequence` to 3, and EF's next insert got 4 |
| Does a nullable typed ID get the converter? | Yes: `TeamID?` maps to a nullable INTEGER and round-trips null and a value |
| Is a converted typed-ID key generated? | No (`ValueGenerated.Never`), so its table gets a plain `PRIMARY KEY`: hence §3.3's change |
| Is an `int` key generated? | Yes (`OnAdd`), so `Seasons` would become AUTOINCREMENT: hence `ValueGeneratedNever()` |
| What does an optional foreign key do on delete? | Nothing in SQL (no `ON DELETE` clause); a required one would cascade: hence the restrict loop |

## 10. Questions (answered 2026-10-29)

Both as recommended ([D-089](../DECISIONS.md#d-089-a-league-and-its-save-are-written-in-one-command-the-cap-lives-in-a-seasons-table)):
1. ~~How do the save and its league exist together or not at all?~~ → **One command:** `CreateAsync` runs the league's write step inside `Save.Create`, and the `created` snapshot holds the league.
2. ~~Where does the cap live?~~ → **A `Seasons` table,** with one row per season, so the save keeps its own cap.

**Still owed, before WP8:** F-01's M1 spec (WP0). WP5 doesn't need it: the wizard's steps, its cancel and its error cases are WP8's.
