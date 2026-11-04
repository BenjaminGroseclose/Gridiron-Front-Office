# M1 · WP6 builder's guide: perception and read models

> **Status:** ✅ **Done 2026-11-04** (written 2026-11-01; all three slices built, tested and green, awaiting your commits, §6) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP6](M1_THE_LEAGUE.md#wp6--perception-and-read-models-m) · **Size:** M
>
> WP6 is what every screen reads. `IPerception` is the fog-of-war boundary ([D-032](../DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted)): every rating a screen or an AI sees passes through it, as an estimate and a sigma. Until M6 every sigma is 0, so the numbers are exact, but the read models already carry the sigma, and M6 changes data, not shapes ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) #13). Two query services turn a save into read models: the league's header, structure and team summaries, and its players. `GameStateStore` v0 keeps "where am I" for the shell. Nothing on screen changes yet: WP8's wizard and WP9's screens are the first callers. When WP6 is done, a seed-42 save reads back as the records those screens will bind to, through the user's eyes.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | [D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) and [D-093](../DECISIONS.md#d-093-ui-tests-read-a-real-save-a-rows-ratings-are-read-back-line-by-line-team-identities-arent-cached) (your answers of 2026-11-01), the updates to F-02, F-03, F-12 and the plan, and this guide. |
| **You build** (§3, §4) | Three slices: perception; the queries; `GameStateStore`. |
| **Claude writes the tests** (§5) | Per slice, as each one lands, each proven to fail on a planted problem. |

**Read first:**
- [F-03](../features/F-03_PLAYERS.md) §4.5–4.9 (OVR, the potential grade, personality, perception) and §5 (the read models);
- [F-02](../features/F-02_LEAGUE_AND_TEAMS.md) §5 (the team read models) and [F-12](../features/F-12_CONTRACTS.md) §8 (what the contract panel shows);
- [DATA_ACCESS_GUIDE.md](../architecture/DATA_ACCESS_GUIDE.md) §4 (how to read: rows then read models, columns, joins without navigations, perception, and the rules; its examples are `LeagueQueries`' identities and structure) and [D-070](../DECISIONS.md#d-070-read-models-are-records-named-dto);
- [UI_DESIGN_SYSTEM.md](../architecture/UI_DESIGN_SYSTEM.md) §9 (the stores) and CLAUDE.md's "Fog of war".

**AI counterpart:** none acts yet. The AI reads through the same `IPerception` with its own team as the viewer (F-03 §7), so the boundary built here is the one it uses from M4.

## 1. What you'll build

```
src/Core/Players/
├─ PlayerEnums.cs                    + TraitLevel (Low, Average, High)
├─ SideOfBall.cs                     SideOfBall (Offense, Defense, SpecialTeams) and Position.Side() (D-092 #3)
└─ Player.cs                         + AgeOn(birthDate, date): whole years on a league date; + IsUndrafted (§4.2)
src/Application/
├─ ApplicationServices.cs            AddGfoApplication(): perception and the query services
├─ Players/
│  ├─ Perception.cs                  PlayerSnapshot, PerceivedRating, PerceivedRosterRating, PerceivedPlayer, IPerception, Perception (§4.1)
│  ├─ PlayerRows.cs                  + ToSnapshot: a Players row back into enum-keyed ratings (§4.1)
│  ├─ PlayerQueries.cs               GetRosterAsync, GetLeaguePlayersAsync, GetPlayerAsync (§4.2)
│  ├─ PlayerReadModels.cs            PlayerRowDto, PlayerDetailDto, PotentialRange, DraftInfoDto, ContractSummaryDto, ContractYearDto
│  └─ PlayerLog.cs                   renamed from PlayerGenerationLog: 1300–1303, + 1306 (§4.4)
└─ Leagues/
   ├─ LeagueQueries.cs               GetHeaderAsync, GetStructureAsync, GetTeamIdentitiesAsync, GetTeamSummariesAsync, GetTeamSummaryAsync (§4.2)
   └─ LeagueReadModels.cs            LeagueHeaderDto, LeagueStructureDto, ConferenceDto, DivisionDto, TeamIdentityDto, TeamSummaryDto
src/Infrastructure/Data/
├─ Configurations/PlayerConfiguration.cs   + the Players(TeamID, Status) index (§3)
└─ Migrations/                       InitialCreate, regenerated (D-090)
src/UI/
├─ State/GameStateStore.cs           v0: the open league's header, refreshed on open, pick and close (§4.3)
├─ Diagnostics/UiLog.cs              + 8004: a header load that failed (§4.4)
└─ UiServices.cs                     + GameStateStore (scoped)
src/Desktop/App.xaml.cs, src/DevHost/Program.cs   + AddGfoApplication() (ARCHITECTURE §6.1's order)
tests/ (Claude)                      Core.Tests/Players, Application.Tests/Players, Infrastructure.Tests/Leagues (+ QueriedSeed42,
                                     the save they share) and Data, UI.Tests/State (+ TestDoubles/RealApp, and a test-only
                                     reference to Infrastructure, D-093 #1)
```

Names are suggestions: keep them unless you have a reason not to. The read models sit next to the query service that fills them (D-070), one file per feature folder.

## 2. Decisions this guide makes

D-092 and D-093 settled the owner's questions. These are the smaller choices, so the code, the tests and WP8–WP9 agree:

| Choice | Rule | Why |
|---|---|---|
| **Two query services** | `LeagueQueries` (the header, the structure, the identities, the team summaries) and `PlayerQueries` (a roster, the league's players, one player). F-02's `LeagueStructureQueries` becomes `LeagueQueries`, and F-12's `ContractQueries` folds in: a team's cap totals ride on `TeamSummaryDto`, a player's contract on `PlayerDetailDto`, and the Team page's five biggest cap hits are its roster's rows sorted (WP9) | One class per thing a screen reads. No third service for two fields |
| **A league-wide list** | `GetLeaguePlayersAsync()`: every player, rostered and free agent, as rows. It's WP9's 2,000-player Roster grid (the plan's exit checks its budget). `SearchPlayers` (a name search) stays WP10's (D-092) | The grid needs the list before the palette needs a search |
| **Perception returns OVR and potential** | `PerceivedPlayer` holds each attribute, the OVR and the potential as `PerceivedRating(Estimate, Sigma)`, and the personality levels or null. M1's `Perception` sets every sigma to 0, OVR to `OverallCalculator.Overall` over the ratings, and the potential to the row's. The queries derive the rest from it: the displayed OVR, the grade range and the archetype label | F-03 §4.9 propagates σ through the OVR weights with the error model's shared term, so the propagation lives with the error model, and M6 changes one class |
| **`double`, not `float`** | `PerceivedRating(double Estimate, double Sigma)`. F-03 §4.9's sketch is updated | Every rating is a `double` on `Player` and in `OverallCalculator` |
| **The viewer** | The queries read `SaveInfo.UserTeamID` and pass it: null before the pick, an outsider (D-092 #1). A query never takes a viewer from its caller | One rule for every screen; the AI calls `IPerception` itself |
| **Personality** | The viewer's own players: each trait's `TraitLevel` (Low below 35, High above 65, from `generation.json`). Anyone else's, or no viewer: null, which the screens show as "Unknown" (D-092 #2) | D-032 from the first screen |
| **Measurables are public** | Shown exactly for every pro. Prospects (M5) get them from the combine (F-17) | A pro's combine is history; F-03 §4.3 hides only a prospect's |
| **The potential grade** | `PotentialRange(Grade, Low, High)`: `OverallCalculator.Grade` of the estimate, and of the estimate ∓ 1.28σ (F-03 §4.9's 80% interval). In M1 all three are the same grade | "Ceiling: B+, likely B to A−" from M6 without a new field |
| **Roster strength** | The starters' mean perceived OVR (D-092 #3) on offense, on defense and overall: for each position, the team's best players by OVR estimate up to `positions.json`'s `starters`, averaged with equal weight, the 11 offensive starters for `Offense`, the 11 defensive ones for `Defense` and all 25 for `Overall` (K, P and LS count only there). Each sigma is its starters' mean sigma (0 in M1) | The data already has the count; M6's error model may narrow the sigma |
| **The cap** | `Cap` is the current season's `Seasons` row. `CapCommitted` is the current season's cap hit of every active contract on the team (all 53: Top 51 arrives in M4, F-12 §4.3), and `CapSpace` is the difference, negative when over | F-12 §4.3's "what counts" at creation |
| **The contract panel** | `ContractSummaryDto`: type, signed season, years, total, APY (`TotalValue / Years`, rounded down), this season's cap hit, the years left (this season and later), the guarantees left (their sum over the same seasons), and every season's row | F-12 §8's line, "3 years left · $12.4M APY · cap hit this season $14.1M · $9.0M guaranteed remaining", plus its table |
| **Age is on the league date** | `Player.AgeOn(BirthDate, SaveInfo.CurrentDate)`, never the clock | Determinism. Generation draws ages on July 1, so a player born July 2–28 is a year older at camp than the age he was drawn at |
| **OVR isn't a column** | A query loads the rows it needs, perceives them, then sorts and averages in memory: 53 for a roster, about 1,850 for the league | OVR is recomputed from the ratings (F-03 §4.5), so the database can't sort by it |
| **Orders** | The structure in data order (IDs). A roster by position (the `Position` enum's order), then OVR estimate descending, then last and first name. The league's players by `PlayerID`. Team summaries by `TeamID`. In memory, sort an ID by its `.Value` and an OVR by its `.Estimate` (§8) | Stable for tests; the grid sorts on its own |
| **No identity cache** | Each call reads the 32 rows (D-093 #3). F-02's "cached per session" is updated | About a millisecond a page; a cache waits for a profile that needs it |
| **No interfaces for tests** | The query services are concrete classes. UI.Tests gets a test-only reference to Infrastructure and reads a real seed-42 save (D-093 #1) | No test-only code in Application |
| **Registration** | `AddGfoApplication()` (Application) registers `IPerception` and the two query services as singletons, called by both hosts between `AddGfoInfrastructure` and `AddGfoUi` (ARCHITECTURE §6.1). `CreateLeague` and `LeagueCommands` join it in WP8, with their first consumer | `ISaveReader` and `ISeedData` are singletons; the services hold no state. `IServiceCollection` compiles in Application (probed, §9) |
| **`GameStateStore` v0** | A scoped UI service holding the open league's `LeagueHeaderDto` (null at the main menu). It loads when a save opens (and at construction if one is open), again on `UserTeamPicked`, and clears on `SaveClosed`. Other events refresh it from M3 (D-078 #14) | WP8 opens the save before the pick, so the store must hear the pick |

## 3. The schema

One change: `PlayerConfiguration` gains `builder.HasIndex(x => new { x.TeamID, x.Status });` for `GetRosterAsync` (PERSISTENCE §5.6). Regenerate `InitialCreate` (D-090) and read it: the new index replaces EF's own `IX_Players_TeamID`, since an index that starts with a foreign key's column covers it. Your dev saves go stale again; delete them on the load screen.

**Done 2026-11-04** by Claude, who from now on regenerates `InitialCreate` after every entity or configuration change: `20261104054118_InitialCreate`, whose only difference from the last one is `IX_Players_TeamID` becoming `IX_Players_TeamID_Status` on `(TeamID, Status)`.

## 4. The slices

### 4.1 Slice 1: perception (pure)

```csharp
// Core/Players/PlayerEnums.cs
/// <summary>A personality trait as screens show it (F-03 §4.7): the hidden 0–100 value, banded.</summary>
public enum TraitLevel { Low, Average, High }

// Core/Players/Player.cs
public sealed class Player
{
    /// <summary>
    /// Whole years from <paramref name="birthDate"/> to <paramref name="date"/>, the league date (never the clock). A
    /// February 29 birthday counts on February 28 in other years.
    /// </summary>
    public static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        return birthDate.AddYears(age) > date ? age - 1 : age;
    }
}

// Application/Players/Perception.cs
/// <summary>What perception needs of a player: who he is, whose he is, and his true ratings.</summary>
public sealed record PlayerSnapshot(
    PlayerID PlayerID,
    TeamID? TeamID,
    Position Position,
    IReadOnlyDictionary<PlayerAttribute, double> Attributes,
    double Potential,
    IReadOnlyDictionary<PersonalityTrait, double> Personality);

/// <summary>A rating as a viewer sees it. Sigma 0 means exact (M1–M5).</summary>
public readonly record struct PerceivedRating(double Estimate, double Sigma);

/// <summary>
/// A roster's strength as a viewer sees it (D-092 #3): the starters' mean OVR on offense, on defense, and over all 25
/// (K, P and LS count only in <paramref name="Overall"/>).
/// </summary>
public readonly record struct PerceivedRosterRating(PerceivedRating Offense, PerceivedRating Defense, PerceivedRating Overall);

/// <summary>A player through one viewer's eyes. <paramref name="Personality"/> is null when the viewer can't see it.</summary>
public sealed record PerceivedPlayer(
    PlayerID PlayerID,
    IReadOnlyDictionary<PlayerAttribute, PerceivedRating> Attributes,
    PerceivedRating Overall,
    PerceivedRating Potential,
    IReadOnlyDictionary<PersonalityTrait, TraitLevel>? Personality);

/// <summary>The fog-of-war boundary (F-03 §4.9, D-032). The sim never calls it.</summary>
public interface IPerception
{
    /// <summary>The user's team for screens, an AI team for its own decisions; null is an outsider, before the pick (D-092).</summary>
    PerceivedPlayer Perceive(TeamID? viewer, PlayerSnapshot player);

    /// <summary>The strength of one team's <paramref name="players"/> through the viewer's eyes, built from <see cref="Perceive"/>.</summary>
    PerceivedRosterRating PerceiveRoster(TeamID? viewer, IReadOnlyList<PlayerSnapshot> players);
}

public sealed class Perception(ISeedData data) : IPerception { … }
```

**`Perception` in M1:** every sigma is 0. Each attribute's estimate is the true rating. `Overall` is `OverallCalculator.Overall(profile, player.Attributes)` for the player's position profile (look the profile up once per position, not per call). `Potential` is the row's. `Personality` is the levels when `viewer is not null && viewer == player.TeamID`, otherwise null; a level is Low below `LowBelow`, High above `HighAbove`, Average between (35 and 65 themselves are Average).

**`PlayerRows.ToSnapshot(Player row)`:** the mirror of `ToPlayer`, by hand (D-093 #2): one line per attribute and per trait, `[PlayerAttribute.Speed] = row.Speed`, grouped like `ToPlayer`. A crossed pair compiles; the round-trip test (§5) catches it. Measurables don't go into the snapshot (they're public, §2); `PlayerQueries` reads them for the detail with eleven lines of their own.

### 4.2 Slice 2: the queries

```csharp
// Application/Leagues/LeagueReadModels.cs
public sealed record TeamIdentityDto(TeamID TeamID, string Abbreviation, string City, string Nickname,
    string PrimaryColor, string SecondaryColor, string AccentColor, CrestShape CrestShape, string CrestMonogram,
    CrestStripe CrestStripe);

public sealed record TeamSummaryDto(TeamIdentityDto Identity, string ConferenceName, string DivisionName,
    string StadiumName, int StadiumCapacity, Roof StadiumRoof, PerceivedRosterRating RosterStrength,
    long Cap, long CapCommitted, long CapSpace);

public sealed record LeagueStructureDto(IReadOnlyList<ConferenceDto> Conferences);
public sealed record ConferenceDto(ConferenceID ConferenceID, string Key, string Name, IReadOnlyList<DivisionDto> Divisions);
public sealed record DivisionDto(DivisionID DivisionID, string Key, string Name, IReadOnlyList<TeamIdentityDto> Teams);

/// <summary>"Where am I": what the shell and Home show of the open league.</summary>
public sealed record LeagueHeaderDto(string LeagueName, int Season, Phase Phase, DateOnly Date, TeamSummaryDto? UserTeam);

// Application/Leagues/LeagueQueries.cs
public sealed class LeagueQueries(ISaveReader reader, IPerception perception)
{
    public Task<LeagueHeaderDto> GetHeaderAsync(CancellationToken cancellationToken = default);
    public Task<LeagueStructureDto> GetStructureAsync(CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TeamIdentityDto>> GetTeamIdentitiesAsync(CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TeamSummaryDto>> GetTeamSummariesAsync(CancellationToken cancellationToken = default);   // WP8's picker: all 32
    public Task<TeamSummaryDto?> GetTeamSummaryAsync(TeamID team, CancellationToken cancellationToken = default);     // null: no such team
}

// Application/Players/PlayerReadModels.cs
public readonly record struct PotentialRange(PotentialGrade Grade, PotentialGrade Low, PotentialGrade High);

public sealed record PlayerRowDto(PlayerID PlayerID, string FirstName, string LastName, Position Position,
    int? JerseyNumber, int Age, TeamID? TeamID, PerceivedRating Overall, PotentialRange Potential, string Archetype,
    long? CapHit);   // CapHit: this season's, null for a free agent

public sealed record PlayerDetailDto(PlayerRowDto Row, DateOnly BirthDate, string College, string HometownName,
    string HometownState, int Experience, DraftInfoDto? Draft,
    IReadOnlyDictionary<Measurable, double> Measurables,
    IReadOnlyDictionary<PlayerAttribute, PerceivedRating> Attributes,
    IReadOnlyDictionary<PersonalityTrait, TraitLevel>? Personality,   // null: "Unknown"
    ContractSummaryDto? Contract);                                    // null for a free agent

public sealed record DraftInfoDto(int Season, int Round, int Pick, TeamID Team);   // null when undrafted

public sealed record ContractSummaryDto(ContractType Type, int SignedSeason, int Years, long TotalValue, long Apy,
    long CapHit, int YearsLeft, long GuaranteedLeft, IReadOnlyList<ContractYearDto> Seasons);

public sealed record ContractYearDto(int Season, long BaseSalary, long ProratedBonus, long GuaranteedAmount, long CapHit);

// Application/Players/PlayerQueries.cs
public sealed class PlayerQueries(ISaveReader reader, IPerception perception, ISeedData data, TimeProvider time,
    ILogger<PlayerQueries> log)
{
    public Task<IReadOnlyList<PlayerRowDto>> GetRosterAsync(TeamID team, CancellationToken cancellationToken = default);   // Active players
    public Task<IReadOnlyList<PlayerRowDto>> GetLeaguePlayersAsync(CancellationToken cancellationToken = default);         // everyone, 1306
    public Task<PlayerDetailDto?> GetPlayerAsync(PlayerID player, CancellationToken cancellationToken = default);          // null: no such player
}
```

**Inside each query:** one `reader.QueryAsync`. Read `SaveInfo` for the viewer, the season and the date; read the rows; turn each `Player` into a snapshot, perceive it, and build the DTO. Entities never leave the lambda (D-070). There are no navigations, so the cap hits are a second query: this season's `ContractYears` joined to the active `Contracts`, by `PlayerID`. A roster or the league's list is three reads at most (`SaveInfo`, the players, the cap hits), never one per player.

**A row, the same way everywhere:** one private helper builds a `PlayerRowDto` from the row, its perceived player, the age date and the cap hit, so the roster, the league's list and the detail agree: the OVR is the perceived `Overall`, the grade range comes from the perceived `Potential` (§2), and the archetype is `OverallCalculator.Archetype` over the attribute estimates.

**Cap hits by player:** build the dictionary as `ToDictionaryAsync(x => x.PlayerID, x => (long?)x.CapHit, ct)`, so `GetValueOrDefault` gives a free agent null. With `long` values it gives 0, and the row shows a $0 cap hit (§8).

**An identity, the same way everywhere:** one private `ToIdentity(Team)` in `LeagueQueries` builds every `TeamIdentityDto` (the header, the structure, the identities and the summaries). It stays private because only `LeagueQueries` builds one: a player's row carries a `TeamID`, and a screen looks the team up in `GetTeamIdentitiesAsync`. Once a second query service needs one, it moves beside the DTO as an extension, the way `PlayerRows.ToSnapshot` is shared.

**Team summaries:** one private `SummariesAsync(db, saveInfo, only, ct)` builds them for all three callers: every team when `only` is null (`GetTeamSummariesAsync`), one team otherwise (`GetHeaderAsync`, `GetTeamSummaryAsync`). It's four statements however many teams: the teams joined to their division and conference names, the season's cap, the committed cap summed by team in SQL, and the active rostered players, split by team in memory. Each roster is perceived with `saveInfo.UserTeamID` as the viewer, null before the pick, never the team itself (§2). On seed 42 the header's summary matches the generated league's starters and an in-memory sum of the contracts, and each `GetTeamSummaryAsync` equals its entry in `GetTeamSummariesAsync`.

**The detail:** `GetPlayerAsync` is five statements: `SaveInfo`; the player (null when there's none); the college's name; the active contract (none for a free agent); and its seasons, in order. `CollegeID` is a required foreign key that restricts deletes, so the college is always there. The player is perceived once, so the row, the attributes and the personality share one view. The row's cap hit is the contract summary's, so it matches the roster's. `ContractSummaryDto.CapHit` is this season's, and `YearsLeft` and `GuaranteedLeft` count from this season on. On seed 42 every active contract has a 2026 season. The draft is null when `Player.IsUndrafted`.

**`Player.IsUndrafted`:** true unless all four draft columns are set, and marked `[MemberNotNullWhen(false, …)]`, so after `!player.IsUndrafted` the four `.Value`s need no `!`. The compiler doesn't check the body against the attribute for `int?` members, so a test pins it (§5). It works only in memory: EF doesn't map a get-only property, so in a query write `p.DraftSeason == null`.

### 4.3 Slice 3: `GameStateStore` v0 (UI)

```csharp
// UI/State/GameStateStore.cs
/// <summary>The open league's header for the shell and Home (UI_DESIGN_SYSTEM §9). Null at the main menu.</summary>
public sealed class GameStateStore : IDisposable
{
    public GameStateStore(ISaveManager saves, IDomainEventDispatcher events, LeagueQueries queries, ILogger<GameStateStore> log);

    public LeagueHeaderDto? Header { get; }

    /// <summary>The latest load, already complete when none is running. It never faults.</summary>
    public Task Loading { get; }

    /// <summary>Raised after Header changes, on whichever thread loaded it: components marshal with InvokeAsync.</summary>
    public event Action? Changed;
}
```

It subscribes to `SaveOpened` and `UserTeamPicked` (load) and `SaveClosed` (clear), and loads at construction when `saves.Current` isn't null. Subscribers run on the thread that finished the command (CLAUDE.md), so a load starts there and must not block it: start the load as a task and keep it (no `async void`). SQLite's async calls complete synchronously, so the load starts with `Task.Run`; called directly, the whole read would run on the command's thread. `Loading` keeps the latest one, for a screen (or a test) that waits for it. Two loads can overlap (open, then a quick pick): number each load and each clear, and keep only the latest one's result, so a load that finishes after a close stays out too. A failed load logs 8004 (§4.4) with its exception and leaves the header as it was; an overtaken load's failure isn't logged. `Changed` fires only when the header differs (the read models are records), and `Dispose` unsubscribes and drops a load still running.

**Built 2026-11-04** by Claude, at the owner's request, as `UI/State/GameStateStore.cs`, with 8004 in `UiLog`.

Register it scoped in `AddGfoUi` (one per circuit in the DevHost, one in the desktop app). Nothing renders it in WP6; WP8's wizard and WP9's shell and Home are its first readers.

### 4.4 Logging

| ID | Level | Message |
|---|---|---|
| 1306 | Debug | `Read {Players} players for {Viewer} in {ElapsedMs} ms` |
| 8004 | Error | `Couldn't load the league header after {Trigger}` |

Logged by `GetLeaguePlayersAsync` (`PlayerLog`, in the 1300–1329 range after `LeagueLog`'s 1305). It's F-03 §12's "perception for a 2,000-row grid < 20 ms", read here, not asserted (D-078 #4); the time covers the read and the perception, measured with the injected `TimeProvider` like `CreateLeague`'s 1304. `{Viewer}` is the team's ID, or `none` before the pick. `PlayerLog` also holds generation's 1300–1303 (the class was `PlayerGenerationLog` in WP4). §9 has what 1306 read on seed 42.

8004 is `GameStateStore`'s (`UiLog`, after 8003), with the exception attached. `{Trigger}` is `SaveOpened`, `UserTeamPicked` or `construction`. It's an Error because a failed read is a failed operation with the app still running (LOGGING §5). It goes into LOGGING §8.2 with slice 3.

No other read logs. Reads run on every page and every refresh, a failure reaches the global handlers with its stack trace (LOGGING §8), and a null result is a caller's unknown ID, not a problem. If every read ever needs a timing, it goes in `SaveReader`, once, the way `SaveWriter` has 2000 and 2003.

## 5. The tests (Claude writes them)

Per slice, each proven to fail on a planted problem (§9). ✅ marks a test that's written; all are, as of 2026-11-04.

| Slice | Test | Checks |
|---|---|---|
| **1** | ✅ `PlayerAgeTests` (Core, 13) | The day before, on and after a birthday; February 29 in a non-leap year; the camp date against a July 1 draw; every birth date of eight years, the day before and on its birthday in ten seasons |
| | ✅ `PlayerRowsTests` (one case added) | Every attribute, trait and the potential of seed 1's players survive `ToPlayer` then `ToSnapshot`, each under its own key, with the IDs and position passed through |
| | ✅ `PerceptionTests` (Application, 11) | Every sigma is 0 and every estimate the true rating for viewers 1, 2 and none, over seed 1's players and free agents; `Overall` equals `OverallCalculator.Overall`; personality shows only for the viewer's own players (not a rival's, a free agent's, or anyone's to no viewer), banded at 34.9, 35, 65 and 65.1 (and 0 and 100); `PerceiveRoster` equals each team's starters' means written out from `positions.json`'s counts (11, 11 and 25), a 99-rated fullback or a 1-rated fourth receiver moves nothing, a 99-rated corner raises the defense and the overall but not the offense, K, P and LS count only in `Overall`, and an empty list is (0, 0) on each side |
| **2** | ✅ `LeagueQueriesTests` (Infrastructure, 8) | On a seed-42 save: the structure is 2 → 4 → 4 in the team set's order with each identity as in `teams.json`, and the identities are the same 32 in TeamID order; the header has the name, 2026, Training Camp, the camp date and no user team, then team 5's summary after the pick; each summary's names, stadium, cap, committed and space match the team set and the generated contracts (and every team sits inside F-12's band), and its roster strength equals the starters' means computed from the `GeneratedLeague`, on each side and overall; the 32 summaries come in TeamID order, each `GetTeamSummaryAsync` equals its entry, the pick moves no number (σ = 0), and an unknown team is null |
| | ✅ `PlayerQueriesTests` (Infrastructure, 7) | The league's list has every player once (1,846) in `PlayerID` order, each row (OVR, grade, archetype, age on the camp date, this season's cap hit) as generated, with free agents' cap hits null, not 0; every team's roster is its 53 in position, OVR descending, then name order; `GetPlayerAsync` returns every bio field, draft, measurable, attribute and contract year as generated for all 1,846, its row equals his row in the league's list, and personality shows only for the user's team, banded (none before the pick, even for team 5); an unknown ID is null; 1306 is logged once per read, Debug, with 1,846 and `none`, then `5`, and a time within the call's |
| | ✅ `PlayerTests` (Core, 16) | `IsUndrafted` is false only when all four draft columns are set, and true for each of the other 15 combinations |
| | ✅ `LeagueSchemaTests` (one case) | `Players`' indexes are `CollegeID`, `DraftTeamID` and `(TeamID, Status)`, with no separate `TeamID` one |
| **3** | ✅ `GameStateStoreTests` (UI, 11) | On a real save (D-093): null at the menu with nothing loading; the header after open; the user team after a pick; null after close; restoring `created` brings back no user team; a store built while a save is open loads it; a load overtaken by a later one, or by a close, is dropped (held open by `HeaderGate`); a failed load logs 8004 (Error, the exception, `UserTeamPicked`) and keeps the header; `Changed` fires once per change; disposing unsubscribes; `AddGfoUi` gives one store per scope |
| **All** | The torture tests | Unchanged, still green |

## 6. Done when

✅ **Done 2026-11-04**, except your commits. All three slices are built, every §5 test passes and was proven to fail on planted problems, and `./build.ps1` is green (749 tests, 55 of them new).

**Slice 1: perception**
- [x] `TraitLevel`, `SideOfBall`, `Player.AgeOn` (whole years), `PlayerRows.ToSnapshot`, the perception types, `Perception` and `PerceiveRoster`.
- [x] `Perception.Perceive` tidied: the unused `profile` local is gone. The `using …Application.Leagues` stays, since `ISeedData` lives there.
- [x] `PlayerAgeTests`, `PerceptionTests`, the `ToSnapshot` round trip in `PlayerRowsTests`, and `PlayerTests` for `IsUndrafted`.

**Slice 2: the queries**
- [x] The read models; all five `LeagueQueries` and all three `PlayerQueries`; `Player.IsUndrafted`; event 1306 in `PlayerLog`; the `Players(TeamID, Status)` index, with `InitialCreate` regenerated; `ConfigureAwait(false)` on every await; and `AddGfoApplication`, with both hosts' calls.
- [x] The three sorts that threw (§8) now run in SQL: the league's players by `PlayerID`, the structure's conferences, divisions and teams by ID, and the identities by `TeamID`.
- [x] The roster's order: position, then OVR estimate descending, then last and first name (ordinal).
- [x] The empty `Application/Data/CommonQueries.cs` is gone.
- [x] `LeagueQueriesTests`, `PlayerQueriesTests` (sharing one seed-42 save, `QueriedSeed42`) and the `LeagueSchemaTests` case.

**Slice 3: `GameStateStore` v0**
- [x] `GameStateStore` (`UI/State`), registered scoped in `AddGfoUi`, with 8004 in `UiLog` (§4.3).
- [x] `GameStateStoreTests` on real saves, through `TestDoubles/RealApp` and UI.Tests' test-only Infrastructure reference (D-093 #1).

**Close-out**
- [x] Every §5 test passes, each proven to fail on a planted problem (§9); the torture tests stay green.
- [x] Reviewed by Claude against this guide, F-02 §5, F-03 §4.9 and §5, F-12 §8, D-070, D-092 and D-093; `./build.ps1` green.
- [x] LOGGING.md §8.2's 1306 and 8004, `PlayerLog`'s new name, and F-03 §11's 1306.
- [x] CLAUDE.md's gotchas (read models: the viewer, the sorts, nullable lookups, `IsUndrafted`, the store's `Task.Run`, `RealApp`); UI_DESIGN_SYSTEM §9's store, TESTING_AND_QUALITY's UI.Tests row and DATA_ACCESS_GUIDE §4.1's in-memory sort; the plan's WP6 marked done.
- [ ] Committed by you: one commit per slice, then the docs. Everything is staged together, so stage by file. `Player.cs` carries slice 1's `AgeOn` and slice 2's `IsUndrafted`, so it goes in slice 1's commit, with `PlayerTests`.

## 7. Order of work

One commit per slice (D-076), each with its tests:

1. **Slice 1,** all pure: no save, no EF. Build `Player.AgeOn` and `ToSnapshot` first, since `Perception`'s tests stand on them.
2. **Slice 2.** The index and the regenerated migration first (a model change fails every save test until it's done), then `LeagueQueries` (the structure and identities are the simplest reads), then `PlayerQueries`.
3. **Slice 3,** the smallest.

## 8. Gotchas

- **The ratings are the save's; the weights, archetypes and grade bands are the app's** (`ISeedData`). If `positions.json` changes, every league's OVRs move with the update. That's intended: M2's SimLab recalibrates the weights, and OVR should summarize what the engine rewards (F-03 §4.5).
- **OVR can't be sorted or filtered in SQL.** Load, perceive, then sort. Don't add an `Overall` column to make a query easier: it would go stale the moment ratings develop (M5).
- **No navigations** (WP5 §2): join `Contracts` and `ContractYears` yourself, and read the season from `SaveInfo`, not from a contract.
- **Domain-event handlers run on the command's thread, after the commit.** `GameStateStore` must start its load and return; a component that listens to `Changed` calls `InvokeAsync(StateHasChanged)`, as `MainLayout` does for `SaveOpened`.
- **A restore reopens the save** (`SaveOpened`), so the store reloads, and restoring `created` brings back "no user team".
- **`ConfigureAwait(false)` on every await in Application,** as in WP5, including the outer `await reader.QueryAsync(…)` of an `async` method.
- **Claude regenerates `InitialCreate`** after any entity or configuration change, yours included, then reads the diff and runs the build. Each regeneration makes your dev saves stale (D-090): delete them on the load screen.
- **Typed IDs and `PerceivedRating` don't sort in memory.** They're record structs without `IComparable`, so `OrderBy(t => t.TeamID)` or `ThenBy(r => r.Overall)` on a list throws "Failed to compare two elements in the array" once there are two items. Sort by `.Value` and `.Estimate`, or sort IDs in SQL before `ToListAsync`, where EF's converter handles them.
- **`GetValueOrDefault` on a `Dictionary<_, long>` returns 0.** Make the values `long?` so a missing player's cap hit is null (§4.2).
- **`IsUndrafted` doesn't translate to SQL.** In a query, write `p.DraftSeason == null`.
- **The camp date moves ages.** Generation draws an age on July 1, and camp opens July 22–28, so some players are a year older on screen than the age the generator drew. Tests compare with `Player.AgeOn`, not with the drawn age.
- **A free agent has no team, so no viewer is "his" team:** his personality is always Unknown, his cap hit null.

## 9. The probes

Each one ran on a scratch copy of the repository, never in it. The first is from 2026-11-01, the rest from 2026-11-04, on a seed-42 save.

| Question | Answer |
|---|---|
| Can Application register its own services? | Yes: `IServiceCollection` compiles in Application through EF Core's own dependencies, with no new package (a planted `AddProbe` built clean, analyzers on) |
| Does `AddGfoApplication` resolve? | Yes: with the hosts' registrations and `ValidateOnBuild`, `IPerception`, `LeagueQueries` and `PlayerQueries` all resolve |
| Does `GetPlayerAsync` match the generated league? | Yes, for all 1,846 players: the row, the bio, the 11 measurables, the attributes, 1,290 drafts and 1,696 contracts (every season, the APY, the cap hit, the years and guarantees left). Personality shows for team 5's 53 only, and for no one before the pick. About 1.3 ms a call |
| Do typed IDs sort in memory? | No: all three ID sorts threw "Failed to compare two elements in the array". Sorting by `.Value` works, and with it the league's 150 free agents all have a null cap hit |
| Does `[MemberNotNullWhen]` work on `int?` members? | It removes CS8629 at the caller, but the compiler doesn't check the body against it: an `&&` body compiled too |
| Does `QueryAsync` infer `PlayerDetailDto?` from `return null` and a DTO? | Yes, with no type argument |
| What does 1306 read? | About 160 ms on the first call and 70 ms after, in Debug, reads included. F-03 §12's 20 ms is for the perception alone; measure in Release before tuning |
| Does a store's load from an event handler hold the command's thread? | It would: Microsoft.Data.Sqlite's async calls complete synchronously, so `GetHeaderAsync` called straight from the handler runs to the end on that thread. Hence `Task.Run` (§4.3). The events themselves go out after every lock is released (`SaveWriter` after its gate, `SaveManager` after its lifecycle lock), so a load can't deadlock with the command that raised it |
| Does each test catch its planted bug? | Yes, all 34, each in a scratch copy, one at a time. Perception (7): personality for everyone, 35 as Low, σ = 0.5, the specialists on offense, one starter too many, and a crossed pair and the wrong potential in `ToSnapshot`. Core (2): `AgeOn` a day early, `IsUndrafted` with `&&`. Queries (16): the index on `TeamID` alone; reversed division teams and identities; the committed cap from base salaries; the next team's roster; a header without the user team; one summary ignoring its team; the roster's OVR ascending; free agents at $0; ages on July 1; the list descending; guarantees from every season; the shuttle as the three-cone; the draft team as his team; the detail's row without a cap hit; 1306 always `none`. Store (9): every load kept, deaf to the pick or the close, no load at construction, a failed load thrown, 8004's wrong trigger, `Changed` with no change, dispose keeping the subscriptions, one store for the app. Not planted: the roster's name tiebreak (two equal OVR estimates at a position are too rare on seed 42 to show a swap) |
| Can a store bug hang the build? | It did, once: with the pick's subscription planted away, the overlap test awaited `store.Loading`, which was still the load `HeaderGate` held, so the run stopped there instead of failing. Every wait in `GameStateStoreTests` is now bounded at 10 s, then a `TimeoutException`: `Settled` for the store's loads, and the gate's own `HeldAsync`. (VSTHRD003 refuses a test that awaits a `TaskCompletionSource`'s task, even through a property, so the gate does that wait itself, under one justified suppression.) `RealApp` releases the gate when it's disposed. A UI test that awaits a store or a gate bounds the wait the same way |

## 10. Questions (answered 2026-11-01)

All as recommended, except where your words chose otherwise ([D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr), [D-093](../DECISIONS.md#d-093-ui-tests-read-a-real-save-a-rows-ratings-are-read-back-line-by-line-team-identities-arent-cached)):
1. ~~Whose eyes before the pick?~~ → **An outsider's:** `Perceive(TeamID? viewer, …)`.
2. ~~Other teams' personality in M1?~~ → **Unknown;** only your roster's shows.
3. ~~Roster strength?~~ → **The starters' mean OVR,** by `positions.json`'s counts, on offense, on defense and overall.
4. ~~`SearchPlayers`?~~ → **WP10,** with the palette.
5. ~~How do UI tests read models?~~ → **No test-only code in Application:** UI.Tests reads a real save.
6. ~~A row's ratings back into dictionaries?~~ → **59 lines by hand,** the mirror of `ToPlayer`.
7. ~~Cache the team identities?~~ → **No:** read each time.
