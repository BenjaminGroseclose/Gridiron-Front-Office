# M1 · WP4 builder's guide: the player model and generator

> **Status:** 📝 **Ready to build** (2026-10-22) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP4](M1_THE_LEAGUE.md#wp4--the-player-model-and-generator-l) · **Size:** L · **Written:** 2026-10-22
>
> WP4 turns WP3's numbers into a league: 32 rosters of 53 and about 150 free agents, each player with measurables, 42 ratings, hidden ratings, personality, a bio, draft history and a real contract. Every team sits somewhere on a strength ladder from contender to bottom-feeder, and inside the cap band. It's all pure Application code that returns plain records. Nothing is saved yet: WP5 writes the result in one command. The same seed and season always give the same league.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | [D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application) and [D-082](../DECISIONS.md#d-082-team-strength-from-a-ladder-carried-by-the-young-core) (your answers of 2026-10-22) and the doc updates (F-03, F-12, the plan). The data tuning from the reference runs (§9): `positions.json`'s archetype means (WR −3.5, CB −3, RB −3, QB +1, TE +1) and `contracts.json`'s `curve` 2.5. WP3's data tests still pass. |
| **You build** (§3, §4) | Four slices: the Core pieces and the rules; one player; the league; contracts and the cap trades. The `generation.json` additions (§3.2) go in with your record change, since strict parsing needs both at once. |
| **Claude writes the tests** (§5) | Per slice, as each one lands, each proven to fail on a planted problem. Also the test plumbing: `InternalsVisibleTo` for `Application.Tests` in Infrastructure (to build `EmbeddedSeedData`) and `Verify.XunitV3` in `Application.Tests`. |

**Read first:** F-03 §4 (all of it), F-12 §4.1–4.8, the [WP3 guide](M1_WP3_GUIDE.md) §2.2 (the data contract this guide implements), [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) #2, [D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle), D-081, D-082, and CLAUDE.md's "Determinism".

**AI counterpart:** none. Every team, including the one the user will pick, comes from the same rules.

## 1. What you'll build

```
data/players/generation.json        + roster.mixSwaps, draft { … }, teamStrength { … }                    (§3.2)
src/Core/
├─ Players/PlayerEnums.cs            + PersonalityTrait (D-043)
└─ Random/
   ├─ WeightedPicker.cs              picks by weight (names, hometowns, archetypes, shares)
   └─ Seeds.cs                       + six purpose names
src/Application/
├─ Players/
│  ├─ PlayerData.cs                  + MixSwaps, DraftSettings, AgeRange, TeamStrengthSettings (§3.2)
│  ├─ OverallCalculator.cs           OVR, its display, the potential grade, the archetype label (D-081 #4)
│  ├─ GeneratedLeague.cs             the output records (§3.3)
│  ├─ PlayerGenerator.cs             one player from his slot's seed (§4.2)
│  ├─ PlayerNames.cs                 names, with the blocklist and same-team re-rolls
│  ├─ LeagueGenerator.cs             the pipeline (§4.3)
│  └─ GenerationLog.cs               events 1300–1302
└─ Contracts/
   ├─ GeneratedContract.cs           the contract records
   ├─ ContractGenerator.cs           rookie, undrafted, veteran and minimum deals (F-12 §4.5–4.7)
   └─ CapTrades.cs                   the trades into the band (F-12 §4.8, D-081 #1)
tests/ (Claude)                      Core.Tests/Random, Application.Tests/Players and /Contracts, Infrastructure.Tests/Seed (+ rules)
```

Names are suggestions; keep them if you have no reason not to. If `LeagueGenerator.cs` grows past a few hundred lines, the roster mix, draft classes and jersey numbers are natural files of their own.

## 2. Decisions this guide makes

D-081 settled the four big questions, and D-082 added the strength ladder. These are the smaller choices the guide makes, so the code, the tests and WP5 agree:

| Choice | Rule | Why |
|---|---|---|
| **Team strength** (D-082) | 32 values evenly spaced from −0.7 to +0.7, shuffled across the teams. Each rostered player's quality draw moves by `strength × 1.6` if he's within rookie-deal experience, else `strength × 0.7` | Independent players average out: without it, the best and worst teams' starters are 5 OVR apart. Strength carried by the young core is cheap (rookie deals), so the cap trades keep most of it |
| **Roster mix** | Team *i* is paired with team *i* + 16. Each pair makes `mixSwaps` (3) ±1 position swaps in opposite directions, inside each position's min–max | Every team varies, and the league average equals the target exactly for every seed (F-03 §4.1's "averages, not rules") |
| **Output** | Plain records that refer to each other (`TeamDefinition`, `CollegeDefinition`), with no IDs | WP5 assigns IDs while it writes; generation stays free of persistence |
| **Numbers** | Ratings, measurables and traits are `IReadOnlyDictionary<TEnum, double>` filled in enum order, like WP3's records. `double` everywhere | One numeric type from the data to the save (SQLite's REAL is a double). F-03 §4.9's `float` becomes `double` in WP6 |
| **OVR scale** | The identity: `Σ wᵢ · attributeᵢ` on 0–100. Screens round it half away from zero and cap it at 99 | The weights sum to 1, and the reference run hits the tier targets without a curve (F-03 §4.5 updated) |
| **Age** | The drawn age is the player's age on **July 1** of the season (before camp opens). The birthday is a random day in the year before that | Screens compute age from the birthday and the league date, so a 21-year-old never shows as 20 |
| **Experience** | `age − entry age`, with the entry age uniform in 21–23 and never above the age | F-03 §4.8's rookies at 21–23. F-12 reads it as accrued and credited seasons (D-080 #6) |
| **Free agents** | 150 positions split by the roster averages (largest remainder) | The pool always has a kicker, a punter and a snapper to sign in M3 |
| **Measurables** | Stored unrounded; the Player page formats them (4.48, 6′2″) | Rounding is display, not data |
| **Names and jerseys last** | After the cap trades | Same-team uniqueness must see the final rosters |
| **Seeds** | One purpose per step: per player `(season, slot)`, per team `(season, team)`, per class `(season, entry season)` | A change to one step can't shift any other step's draws |
| **The slot stays with the player** | Trades move players, not slots | The slot is the player's seed identity, not his place on a roster |

## 3. Data and records

### 3.1 Already done by Claude

- `data/players/positions.json`: every archetype skill mean shifted by position: **WR −3.5, CB −3, RB −3, QB +1, TE +1**. Speed-heavy OVR weights put receivers, corners and backs 3–4 points over every tier target, and quarterbacks and tight ends ran slightly under (§9).
- `data/contracts/contracts.json`: `curve` 3.5 → **2.5** (D-081 #2). R10 is now 1.21 × cap, still inside its 0.8–1.3 band.

### 3.2 `generation.json` grows (you, with the records)

```json
"roster": { "active": 53, "freeAgents": 150, "mixSwaps": 3 },
"draft": { "draftedShare": 0.70, "rankNoiseSd": 6, "entryAge": { "min": 21, "max": 23 } },
"teamStrength": { "range": 0.7, "youngWeight": 1.6, "veteranWeight": 0.7 }
```

`draft` and `teamStrength` go after `targets`, at the end of the file. The records in `PlayerData.cs`:

```csharp
public sealed record RosterSizes(int Active, int FreeAgents, int MixSwaps);

/// <summary>How generated players got their draft history (D-081 #3): ranked entry classes, the top share drafted.</summary>
public sealed record DraftSettings(double DraftedShare, double RankNoiseSd, AgeRange EntryAge);

public sealed record AgeRange(int Min, int Max);

/// <summary>
/// The league's strength ladder (D-082): evenly spaced from −Range to +Range, in units of the quality draw. A player's
/// shift is his team's strength times YoungWeight within rookie-deal experience, else times VeteranWeight.
/// </summary>
public sealed record TeamStrengthSettings(double Range, double YoungWeight, double VeteranWeight);

// GenerationSettings gains two last parameters: DraftSettings Draft, TeamStrengthSettings TeamStrength
```

These fields are **required**, not optional. D-077's defaults let an M11 import read older team sets and colleges, but `generation.json` ships only inside the app and is never imported, so no older copy can exist ([D-083](../DECISIONS.md#d-083-optional-fields-with-defaults-are-for-importable-data-formats-only)). The header comment in `PlayerData.cs` (and `ContractData.cs`) still says every later field gets a default; reword it to D-083's rule when you add these.

### 3.3 The output records (`Application/Players/GeneratedLeague.cs`, `Application/Contracts/GeneratedContract.cs`)

```csharp
/// <summary>A whole generated league before it's saved (WP5 writes it in one command). Same seed and season, same league.</summary>
public sealed record GeneratedLeague(ulong Seed, int Season, IReadOnlyList<GeneratedTeam> Teams, IReadOnlyList<GeneratedPlayer> FreeAgents);

/// <summary>
/// A team's 53 in depth order: positions in enum order, the generation tier's Starters first. Strength is its place on
/// the ladder (D-082), for tests and the golden snapshot; it isn't saved.
/// </summary>
public sealed record GeneratedTeam(TeamDefinition Definition, double Strength, IReadOnlyList<GeneratedPlayer> Players);

public sealed record GeneratedPlayer(
    int Slot,                      // his seed identity: Seeds.Derive(seed, SeedPurpose.Generation, season, Slot)
    Position Position,
    PlayerTier Tier,               // the generation tier that shaped his skills (F-03 §4.10)
    string Archetype,              // the profile drawn; for tests only, not saved (screens recompute the label)
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    int Age,                       // on July 1 of the season (§2); not saved, the birthday is
    int Experience,
    CollegeDefinition College,
    Hometown Hometown,
    DraftInfo? Draft,              // null: undrafted
    int? JerseyNumber,             // null for free agents
    IReadOnlyDictionary<Measurable, double> Measurables,
    IReadOnlyDictionary<PlayerAttribute, double> Ratings,
    HiddenRatings Hidden,
    IReadOnlyDictionary<PersonalityTrait, double> Personality,
    GeneratedContract? Contract);  // null for free agents

/// <summary>Pick is the overall number (1 to rounds × teams); the pick within the round is derived.</summary>
public sealed record DraftInfo(int Season, int Round, int Pick, TeamDefinition Team);

public sealed record HiddenRatings(double Potential, DevelopmentCurve DevelopmentCurve, double BustRisk,
    double PeakAgeOffset, double InjuryProneness);

/// <summary>A contract with every season written, past ones included (D-074). Years = Seasons.Count.</summary>
public sealed record GeneratedContract(ContractType Type, int SignedSeason, long TotalValue, long SigningBonus,
    long GuaranteedTotal, IReadOnlyList<ContractSeason> Seasons);

public sealed record ContractSeason(int Season, long BaseSalary, long ProratedBonus, long GuaranteedAmount, long CapHit);
```

The steps after the players exist fill `Draft`, `Contract`, `JerseyNumber` and the names with `with` expressions. `PlayerGenerator` leaves them empty.

## 4. The code, slice by slice

### 4.1 Slice 1: the Core pieces and the rules

**`PersonalityTrait`** (in `PlayerEnums.cs`, F-03 §4.7's order): `WorkEthic, Coachability, Greed, Loyalty, Competitiveness, Ego`. Saves will store the names (WP5's columns), so the same rename rule as `PlayerAttribute` applies.

**`WeightedPicker<T>`** (`Core/Random`). The plan's WP1 listed a weighted pick; it arrives now with its first consumers.

```csharp
/// <summary>
/// Picks items in proportion to their weights: names by frequency, hometowns by population, archetypes and tiers by
/// share. Build it once per list; each pick is one NextDouble and a binary search over the running totals.
/// </summary>
public sealed class WeightedPicker<T>
{
    public WeightedPicker(IEnumerable<(T Item, double Weight)> items);   // every weight ≥ 0, at least one > 0, else ArgumentException
    public T Pick(GfoRandom random);
}
```

The running totals `c₀ ≤ c₁ ≤ …`; draw `x = NextDouble() × total` and return the first item with `x < cᵢ`. A zero-weight item can never be picked.

**Seed purposes** (`SeedPurpose`, never renamed):

```csharp
public const string RosterMix = "gen.mix";     // (season, pair)
public const string TeamStrength = "gen.strength";  // (season)
public const string Names = "gen.names";       // (season, slot)
public const string Draft = "gen.draft";       // (season, entry season)
public const string Jerseys = "gen.jersey";    // (season, team index)
public const string Contract = "contract";     // (season, slot), F-12 §4.8
```

**`Draw`** (`Application/Players`, an extension next to `Distribution`): `random.Draw(distribution)` is `Normal(Mean, Sd)` clamped to `Min`–`Max` when they're given.

**`OverallCalculator`** (`Application/Players`, static, D-081 #4):

```csharp
/// <summary>OVR on the internal 0–100 scale: Σ wᵢ · attributeᵢ over the position's weights, in the file's order (F-03 §4.5).</summary>
public static double Overall(PositionProfile profile, IReadOnlyDictionary<PlayerAttribute, double> ratings);

/// <summary>The 0–99 integer screens show (D-034): rounded half away from zero, within 0–99.</summary>
public static int Display(double rating);

/// <summary>The grade of a peak OVR: the first band, best first, whose MinPeakOvr the displayed peak reaches.</summary>
public static PotentialGrade Grade(double peakOverall, IReadOnlyList<GradeBand> bands);

/// <summary>The archetype the ratings fit best (F-03 §4.5): recomputed, never stored, so it stays true after development.</summary>
public static string Archetype(PositionProfile profile, SkillSettings skills, IReadOnlyDictionary<PlayerAttribute, double> ratings);
```

The archetype label removes the player's overall level first, so a great and a poor route runner both read as route runners:

```
U = every skill any of the position's archetypes lists
for each archetype A, in file order:
    level       = mean over A's skills of (rating(a) − A.Skills[a])
    expected(a) = A.Skills[a] + level        if A lists a
                = skills.Common[a] + level   if a is a common skill (Stamina, Toughness, Awareness, Discipline)
                = skills.OffPosition.Mean    otherwise (off-position skills get no level)
    misfit      = Σ over U of (rating(a) − expected(a))²
label = the archetype with the smallest misfit (the first on a tie); a position with one archetype always gets it
```

Comparing over all of `U`, not just A's own skills, matters. A center's two archetypes are Anchor (run block, pass block, awareness) and Athletic (the same three plus lead block). Judged only on its own three skills, Anchor fits an Athletic center just as well. Over `U`, his lead block of ~58 against Anchor's off-position ~25 settles it. In the reference run the label matches the drawn archetype 97.5% of the time.

### 4.2 Slice 2: one player

```csharp
public sealed class PlayerGenerator(ISeedData data)
{
    /// <summary>
    /// One player, everything but his names, draft history, jersey and contract (the league fills those).
    /// <paramref name="teamStrength"/> is his team's place on the ladder (D-082); 0 for a free agent.
    /// </summary>
    public GeneratedPlayer Generate(ulong leagueSeed, int season, int slot, Position position, PlayerTier tier, double teamStrength);

    /// <summary>A measurable's straight line to a physical rating, through the map's best and worst points (no noise).</summary>
    public static double PhysicalLine(PhysicalRating map, double value);
}
```

Build the pickers once, in the constructor: archetypes per position (by `share`), the college tier per player tier, the colleges of each tier (in file order), the development curve, and the hometowns (by population).

`random = new GfoRandom(Seeds.Derive(leagueSeed, SeedPurpose.Generation, season, slot))`, then **draw in exactly this order**. The order is part of the league: a draw added in the middle changes every later value of that player.

| # | Draw | Rule |
|---|---|---|
| 1 | Quality `q` | `Normal()`: how good he is within his tier |
| 2 | Age | `Draw(profile.Age)`, rounded half away from zero (the distribution clamps it to min–max) |
| 3 | Birthday | July 1 of the season, minus `age` years, minus `Next(0, 365)` days |
| 4 | Entry age | `Next(entryAge.Min, Min(age, entryAge.Max) + 1)`; `experience = age − entry age`. Then the team's strength moves `q` (D-082, no draw): `q += teamStrength × (experience < rookieScale.years ? youngWeight : veteranWeight)` |
| 5 | Athletic draw `A` | `Normal()` |
| 6 | Measurables | In enum order, one `Normal()` each, called ε. The six drills (Forty, TenSplit, ThreeCone, Shuttle, Vertical, BroadJump) use `z = √ρ·A + √(1 − ρ)·ε` with ρ = `drillCorrelation`, so any two drills correlate at ρ; the others use `z = ε`. The tier's `athleticShift` (s) applies to the six drills and Bench, not to the body (Height, Weight, ArmLength, HandSize). `value = mean + sign · sd · (z + s)`, with `sign = −1` for the four timed drills (lower is better), else +1 |
| 7 | Physical ratings | Speed, Acceleration, Agility, Strength in enum order: the average of `PhysicalLine` over the maps that feed it (Agility has two), plus `Normal(0, physicalNoiseSd)`, clamped to 0–100 |
| 8 | Archetype | The position's picker |
| 9 | Skills | Every other attribute, in enum order. The mean is the archetype's if it lists the attribute, else `skills.common`'s. Then `clamp(mean + tier.SkillShift + q · tier.Spread + Normal(0, noiseSd), 0, 100)`. An attribute neither lists is off-position: `Draw(skills.OffPosition)`, with no tier shift and no `q` |
| 10 | Potential | `Overall + Draw(growth)`, where the growth band is the first `potentialGrowth` with `age ≤ throughAge` (it draws even when its sd is 0) |
| 11 | Hidden | Development curve (picker), then `Draw` bust risk, peak age offset and injury proneness |
| 12 | Personality | Six `Draw(personality.Value)` in enum order |
| 13 | College | The college-tier picker for his tier, then `Next(0, count)` among that tier's colleges |
| 14 | Hometown | The population picker |

**`PlayerNames`** (`Application/Players`):

```csharp
public sealed class PlayerNames(NameList firstNames, NameList lastNames, IReadOnlyList<string> famousPeople)
{
    /// <summary>A first and last name; re-rolls a famous name (F-03 §4.11) or one already in <paramref name="taken"/>.</summary>
    public (string First, string Last) Pick(GfoRandom random, ISet<string> taken);

    /// <summary>The comparer for full names: case- and accent-insensitive, so "Tom Brädy" is "Tom Brady".</summary>
    public static StringComparer FullNames { get; } =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
}
```

Pick a first name, then a last name, from their pickers. If `"{first} {last}"` is in the blocklist or in `taken`, pick both again. There's no attempt limit: 2,000 × 5,000 names against 1,063 blocked can't loop.

### 4.3 Slice 3: the league

```csharp
public sealed class LeagueGenerator(ISeedData data, ILogger<LeagueGenerator> log, TimeProvider time)
{
    public GeneratedLeague Generate(ulong seed, int season);
}
```

Nothing registers it yet: WP5's `CreateLeague` is its first caller, and the tests construct it directly. The `TimeProvider` only times event 1300. The steps run in order:

| # | Step | Rule |
|---|---|---|
| 1 | **Roster mix** | Every team starts at each position's `roster.average` (whole numbers; a data test checks). For pair *k* (teams *k* and *k* + 16), `random = Derive(seed, RosterMix, season, k)`, repeated `mixSwaps` times: *up* = a random position (enum order, `Next`) where team *k* is under `max` and its partner over `min`; *down* = a random other position where team *k* is over `min` and its partner under `max`. Team *k* gets +1 *up* and −1 *down*, and its partner the opposite. If either list is empty, that swap is skipped |
| 2 | **Team strength** (D-082) | `random = Derive(seed, TeamStrength, season)`. The ladder `range × (2(i + 0.5) / teams − 1)` for i = 0–31 (−0.68 to +0.68, mean exactly 0), shuffled across the teams in their file order by Fisher–Yates (`Next(0, i + 1)` from the last index down). Free agents have none |
| 3 | **Slots and tiers** | Team *t*'s players: positions in enum order, the first `roster.starters` at each **Starter**, the rest **Backup**; `slot = t × active + i`. Free agents: the 150 split by `average / 53` (largest remainder, ties in enum order), all **Fringe**, `slot = teams × active + j` |
| 4 | **Players** | `PlayerGenerator.Generate` for every slot, with his team's strength (0 for free agents) |
| 5 | **Draft classes** (D-081 #3) | Per entry season `E = season − experience`, ascending, over every player (free agents too), in slot order: `random = Derive(seed, Draft, season, E)`; `score = Overall + Normal(0, rankNoiseSd)`; order by score descending, then slot. With `picks = rounds × teams` (7 × 32 = 224) and `n = Min(picks, round(draftedShare × class size))`, the *k*-th (from 0, while *k* < *n*) gets `Pick = k × picks / n + 1` (integer division) and `Round = (Pick − 1) / teams + 1`. The drafting team is his own if he's on a roster and `experience < rookieScale.years`; otherwise `Teams[random.Next(0, teams)]`, drawn in this order. The rest are undrafted |
| 6 | **Contracts** | Slice 4 (§4.4) |
| 7 | **Cap trades** | Slice 4 (§4.4) |
| 8 | **Jersey numbers** | Per team *t*, `random = Derive(seed, Jerseys, season, t)`. Positions go fewest allowed numbers first (LS, QB, the line …), then in enum order, players in roster order. Each player takes `free[random.Next(0, free.Count)]` from his rule's untaken numbers, ascending. If none is free, throw `InvalidOperationException` (a data bug; a test proves it never happens). Free agents get no number |
| 9 | **Names** | Teams in order, players in roster order: `PlayerNames.Pick(new GfoRandom(Derive(seed, Names, season, slot)), taken)`, with `taken` the team's names so far (comparer `PlayerNames.FullNames`). Then the free agents, with an empty `taken` |
| 10 | **Log** | Event 1300 with the player count and the elapsed time |

Until slice 4 lands, steps 6 and 7 are empty and every contract is null.

### 4.4 Slice 4: contracts and the cap trades

**Who gets which deal** (step 6). First the **league-wide rank** (F-12 §4.7): per position, the rostered players sorted by `Overall` ascending, then by slot. With `N` players, the one at index *i* has `t = i / (N − 1)`, and his **contract tier** is Starter if `i ≥ N − teams × roster.starters`, else Backup. Then, for each rostered player:

| Player | Deal |
|---|---|
| Drafted, `experience < rookieScale.years` (0–3) | `Rookie` |
| Undrafted, `experience < undrafted.years` (0–2) | `Undrafted` (type `Minimum`) |
| Everyone else | `Veteran(DrawVeteranTerms(new GfoRandom(Derive(seed, Contract, season, slot)), …))`, which may come back as a minimum deal |
| Free agents | None |

**`ContractGenerator`** (`Application/Contracts`). Money is `long`; a formula may use `double`, rounded to `long` once with `MidpointRounding.AwayFromZero`. `split(amount, weights)` gives each season `floor(amount × wᵢ / Σw)` and the last season the remainder, so the parts add up exactly.

```csharp
public sealed class ContractGenerator(ContractData data, int picksPerRound)
{
    public long Minimum(int creditedSeasons);   // the last step at or below max(0, creditedSeasons)
    public GeneratedContract Rookie(DraftInfo draft, int experience, int season);
    public GeneratedContract Undrafted(int experience, int season);
    public VeteranTerms DrawVeteranTerms(GfoRandom random, Position position, double rank, PlayerTier contractTier, int age, int experience);
    public GeneratedContract Veteran(VeteranTerms terms, int experience, int season);
}

/// <summary>A veteran deal's drawn terms (F-12 §4.7). Apy already includes the wiggle.</summary>
public sealed record VeteranTerms(long Apy, int Years, int Elapsed, double BonusShare, double GuaranteeShare);
```

| Deal | Rule (F-12) |
|---|---|
| **Rookie** (§4.5) | The round's row; the pick within the round `k = Pick − (Round − 1) × picksPerRound`; `total = round(firstPick + (lastPick − firstPick) × (k − 1) / (picksPerRound − 1))`; `bonus = round(total × bonusShare)`; the minimums `mᵢ = Minimum(i)` for i = 0–3; `baseᵢ = mᵢ + split(total − bonus − Σm, baseWeights)ᵢ`; signed in `draft.Season` (= `season − experience`) |
| **Undrafted** (§4.6) | `undrafted.years` seasons signed in `season − experience`; `baseᵢ = Minimum(i)`; the bonus is `undrafted.bonus`; nothing else guaranteed; type `Minimum` |
| **Veteran terms** (§4.7), drawn in this order | The wiggle `w = clamp(Normal(0, wiggle.sd), ±wiggle.max)`; the years `Next(min, max + 1)` from the length band for (contract tier, age); `elapsed = Next(0, Min(years − 1, experience) + 1)`; the bonus share and guarantee share, each `min + NextDouble() × (max − min)` from its tier band. `Apy = round((m + (maxApy − m) × t^curve) × (1 + w))`, with `m = Minimum(experience)` |
| **Veteran** (§4.7) | If `Apy ≤ 1.05 × Minimum(experience)`: a **minimum deal**, one season (this one), base = his minimum, no bonus, nothing guaranteed, type `Minimum`. Otherwise `total = Apy × years`; `bonus = round(total × bonusShare)`; `baseᵢ = split(total − bonus, 1 + i × baseEscalation)ᵢ`; raise every base under `Minimum(experience − elapsed + i)` to it, then take the sum of the raises back out of the last season, but not below its own minimum; `total = bonus + Σ base` (F-12 §10: the floor can push it over the draw); signed in `season − elapsed` |
| **Every deal** | Guarantees: `left = max(0, round(total × guaranteeShare) − bonus)`; season by season from the first, `guaranteedᵢ = min(baseᵢ, left)`, `left −= guaranteedᵢ`; `GuaranteedTotal = bonus + Σ guaranteed`. Each season's `ProratedBonus = CapMath.ProratedBonus(bonus, years, i)` and `CapHit = CapMath.CapHit(baseᵢ, bonus, years, i)`, with `Season = signed + i` |

**`CapTrades`** (`Application/Contracts`, step 7, D-081 #1, F-12 §4.8):

```csharp
/// <summary>
/// Trades veterans between teams until every team's cap total is inside the band (D-081). Players keep their contracts
/// and their slots. Runs once, at league creation. Returns the number of trades.
/// </summary>
public static int Balance(IReadOnlyList<List<GeneratedPlayer>> rosters, int season, ContractData contracts, int maxTrades = 200);
```

```
repeat up to maxTrades times:
    total(team) = Σ this season's CapHit over its 53
    hi = the highest total, lo = the lowest (the lower team index on ties)
    if total(lo) ≥ min × cap and total(hi) ≤ max × cap: done
    gap = (total(hi) − total(lo)) / 2
    candidates: h on hi and l on lo, both Type == Veteran, same Position, same Tier, hit(h) > hit(l)
    take the pair with |hit(h) − hit(l) − gap| smallest (ties: h's slot, then l's slot); none: stop
    swap them in place: same position and tier, so each roster keeps its depth order
```

`LeagueGenerator` then logs event 1301 with the trades and the utilization, plus a 1302 Warning for each team still outside the band.

### 4.5 Logging (`Application/Players/GenerationLog.cs`)

`[LoggerMessage]` works in Application: EF Core brings `Microsoft.Extensions.Logging.Abstractions`, and its generator runs (checked 2026-10-22). Put computed arguments in locals first (CA1873).

| Event | Level | Message |
|---|---|---|
| 1300 | Information | `League generated for season {Season} from seed {Seed}: {PlayerCount} players in {ElapsedMs} ms` |
| 1301 | Information | `Cap fitted with {TradeCount} trades: teams at {MinPercent}% to {MaxPercent}% of the cap, mean {MeanPercent}%` |
| 1302 | Warning | `{Team} is outside the cap band after the trades: {Percent}% of the cap` |

Percentages are rounded to one decimal. Claude adds the 1300–1302 line to LOGGING.md §8.2 with the review.

## 5. The tests (Claude writes them)

Each list of offenders is reported in full, as in WP3. The 20-seed tests share one fixture (seeds 1–20, season 2026, generated once). They stay in the fast build if the fixture takes under about 5 s, otherwise they get `[Trait("Category", "Slow")]`.

**Slice 1**

| Test | Rule |
|---|---|
| `WeightedPickerTests` (Core) | Frequencies follow the weights over 100,000 picks; a zero weight is never picked; one item is always picked; negative or all-zero weights throw; same seed, same picks |
| `OverallCalculatorTests` | A hand-built QB's OVR equals Σ w · a; all-50 ratings give 50 at every position (the weights sum to 1); raising a weighted attribute raises OVR and an unweighted one doesn't; `Display` rounds 74.5 → 75, 99.6 → 99, −0.4 → 0; every F-03 §4.6 band boundary (92 → A+, 91 → A, 57 → F) |
| `ArchetypeLabelTests` | For every archetype in `positions.json`, a player built exactly at its expected profile, at levels −10, 0 and +15, gets its label |
| `PlayerDataTests` (+ rules) | The roster averages are whole numbers (the mirrored mix needs them); no archetype lists Speed, Acceleration, Agility or Strength (they come only from measurables); `0 < draftedShare ≤ 1`; `rankNoiseSd ≥ 0`; `entryAge.min ≤ entryAge.max` and `entryAge.min` ≤ every position's `age.min`; `mixSwaps ≥ 0`; `teamStrength`'s range and weights ≥ 0 |

**Slice 2**

| Test | Rule |
|---|---|
| Determinism | The same inputs give an equal player, value by value |
| Ranges | Every rating and trait within 0–100; the age within the position's min–max; `experience ≥ 0` and `age − experience` within the entry ages; the birthday gives the drawn age on July 1; `potential ≥ OVR` |
| The combine | `PhysicalLine` hits the map's best and worst points and is monotonic. Over 5,000 players, Speed minus `PhysicalLine(Forty)` has mean 0 ± 0.3 and SD 3 ± 0.3 (F-03 §4.3's near-exact ±3), and Agility the same with its two maps averaged. Any two drills' z-scores correlate at 0.6 ± 0.05. A Starter's 40 averages faster than a Fringe player's at the same position |
| Tiers and colleges | College tiers follow `collegeTiers` within ±3 percentage points per player tier; players 22 or younger average about +8 of growth |
| The label | The recomputed archetype matches the drawn one for ≥ 90% of players at positions with two or more archetypes (97.5% in the reference run) |
| Team strength | The same slot with strength +0.5 has a higher OVR than with 0, and the same draws otherwise; the lift is `youngWeight / veteranWeight` times larger for a player within rookie-deal experience |
| `PlayerNamesTests` | First names [Tom], last names [Brady (weight 1,000,000), Smith (1)], blocklist [Tom Brady]: always Tom Smith (D-078 #3); "Tom Brädy" and "TOM BRADY" are blocked too; a name in `taken` is re-rolled; an empty `taken` lets a team duplicate through |

**Slice 3**

| Test | Rule |
|---|---|
| Shape (seed 42) | 32 teams of exactly 53; 150 free agents, split by largest remainder; every team within each position's min–max and at or above its floor; each team's Starters per position equal `roster.starters` |
| Roster mix | Every seed's league average per position equals the target exactly (stricter than F-03 §4.1's ±0.3) |
| The ladder | Sorted, the 32 strengths are the evenly spaced values, with mean 0; another seed shuffles them differently. Averaged over 20 seeds, the teams in the ladder's top quarter have Starters at least 3 OVR better than the bottom quarter's, after the cap trades (4.2 in the reference run, 2.9–5.3 by seed): D-082's point, that the trades don't erase the ladder |
| Draft classes | Picks unique per class; each class drafted at `round(0.7 × size)` (at most 224); better players go earlier: over the classes of 30 or more, the mean Spearman correlation of OVR to pick is below −0.6 (−0.73 in the reference run; small classes go as low as −0.5, which is why it's a mean); anyone on his rookie deal was drafted by his own team |
| Jerseys and names | Unique numbers per team, each allowed by the rule; free agents have none; no two teammates share a full name; no blocklisted name anywhere; 20 seeds without the jersey exception |
| Determinism | The same seed and season give identical leagues (serialized and compared); another seed or another season gives a different one |
| **Distributions** (20 seeds) | Per position and tier, the OVR mean within ±3 of its target and the SD within ±2. **K, P and LS** check only the Starter mean, with the written reason: their OVR is ~90% two kicking skills, so the shared quality draw spreads it wider (SD ~9.5) and the one-deep tiers sit further apart; only ~3 of each are free agents. Players at 90+ average 15–25 per league; the mean age is within 26.5 ± 1 |

**Slice 4**

| Test | Rule |
|---|---|
| `ContractGeneratorTests` (units) | **Rookie:** 4 years signed in the draft season; pick 1 of round 1 totals $54.4M, pick 32 $16.1M, pick 16 on the line; bonus = share × total; bases ≥ the minimums for career seasons 0–3; round 1 fully guaranteed, rounds 4–7 the bonus only. **Undrafted:** 3 years, `Minimum`, bases = the minimums, bonus $20,000. **Veteran:** Σ bases + bonus = total; each base ≥ its season's minimum; bases rise except where a floor lifts one; guarantees ≤ total and front-loaded (no guaranteed season after an unguaranteed one); within 5% of the minimum it's a one-year minimum deal; the curve without the wiggle never decreases in `t` |
| Every contract (20 seeds) | Every rostered player has one and no free agent does; the deal type follows §4.4's table; every `CapHit = CapMath.CapHit`; `elapsed ≤ experience`, and the contract has this season or later left |
| **The cap band** (20 seeds) | Every team between 85% and 97% after the trades; no 1302 Warning; every trade swapped two Veteran contracts at the same position and tier |
| **Golden** (`Verify`) | `Generate(seed: 42, season: 2026)` as a summary: per team its abbreviation, strength, position counts, Starters' mean OVR, cap total and space; the league's player count, 90+ count and mean age, and the five highest cap hits (name, team, position, type); team one's first five players in full |

## 6. Done when

- [ ] Slice 1: `PersonalityTrait`, `WeightedPicker`, the seed purposes, `Draw`, `OverallCalculator`, and the `generation.json` additions with their records.
- [ ] Slice 2: `PlayerGenerator` and `PlayerNames`.
- [ ] Slice 3: `LeagueGenerator` through step 10 (contracts empty), and event 1300.
- [ ] Slice 4: `ContractGenerator`, `CapTrades`, steps 6–7, and events 1301–1302.
- [ ] Every §5 test passes, each proven to fail on a planted problem.
- [ ] Reviewed by Claude against this guide, F-03 and F-12; `./build.ps1` green.
- [ ] LOGGING.md §8.2's 1300–1302 line and CLAUDE.md's gotchas updated; the plan's WP4 marked done; committed by you.

## 7. Order of work

One commit per slice (D-076), each with its tests:

1. **Slice 1**, the smallest and the base of everything: tell Claude when the records are in, and the data and tests follow.
2. **Slice 2.** Look at one generated player in a test's output before going on; most bugs show up there.
3. **Slice 3.** The distribution tests are the moment of truth for the data. If a target misses, tune `positions.json` or `generation.json` (Claude can), never a tolerance without a written reason (the plan's §9).
4. **Slice 4**, then the golden snapshot last, approved once you've read it.

Generation should take well under the plan's 2 s (the reference run in Python, far slower than C#, was not the bottleneck). Read it from event 1300; nothing asserts it (D-078 #4).

## 8. Gotchas

- **The draw order is the league.** Never reorder draws. Add a new draw at the end of its step, or behind a new seed purpose, and expect the golden snapshot to change.
- **Iterate only ordered things for draws:** enums, lists, the files' order. A `Dictionary<TEnum, …>` filled in enum order enumerates in that order as long as nothing is removed; a `HashSet` has no order you may rely on.
- **`Math.Round` rounds half to even by default:** pass `MidpointRounding.AwayFromZero` everywhere.
- **`Math.Pow`, `Log` and `Cos` can differ in the last bit between .NET versions:** a runtime update may require re-approving the golden snapshot. Saves are unaffected, because a league is stored, never regenerated.
- **Money:** `double` only inside a formula, rounded to `long` once.
- **`PlayerGenerator` knows nothing about teams:** names and jerseys come after the trades.
- **Strict JSON:** `generation.json` and its records change in one commit.
- **Seed purposes are constants:** never write `"gen.names"` inline.
- **Ages are measured on July 1** (§2), because M1–M4 leagues open at Training Camp in late July. M5's offseason start opens before July 1, so the reference date moves with it then, or some players show a year younger than drawn.
- **The DevHost locks the DLLs:** stop it before building (CLAUDE.md).

## 9. The reference runs (2026-10-22)

Two rough Python models of this guide's rules, 20 seeds each, outside the repository. WP4's C# tests are the real check; these only set the data's starting point.

**Players**, with the tuned `positions.json`:

| Tier (target) | Range of position means | Notes |
|---|---|---|
| Starter (74, SD 7) | 73.3 (TE) – 76.3 (LS) | SDs 6.1–8.6, except K, P, LS at 9.1–9.7 |
| Backup (64, SD 6) | 61.7 (FB, TE) – 65.3 (WR) | SDs 4.3–5.8 |
| Fringe (56, SD 5) | 53.8 (FB) – 58.3 (CB) | K, P, LS at 51.4–52.6 (§5's exemption) |

Players at 90+ averaged 20.9 per league (6–31); the mean age was 26.9; the archetype label matched 97.5%. Before tuning, receivers, corners and backs ran 3–4 points over every target.

**Contracts**, with OVR drawn from the tier targets:

| Model | Teams' cap totals before fitting | After |
|---|---|---|
| F-12 as drafted (curve 3.5, scaling) | 37–119% of the cap, mean 78% | ~80% of teams scaled, a third by more than 20% (up to ×3.4) |
| **D-081** (curve 2.5, trades) | Mean 92%, SD 14% | ~25 trades per league (34 at most); all 640 teams in band, spread evenly from 85% to 96% |

With D-081 a roster is about 26% rookie deals, 19% minimum deals and 55% veteran deals. The best quarterback's cap hit is about $47M, and the median Starter's about $5M.

**Team strength** (D-082), as the gap between the best and worst teams' Starters (mean OVR) after the cap trades:

| Model | Best vs. worst | Trades per league |
|---|---|---|
| No ladder | 5 OVR (6 before the trades) | ~24 |
| Ladder only | 8 | ~43 |
| **Ladder ±0.7 + young core** (D-082) | **9** (12 before the trades) | ~39 |
| Ladder + bargains (a team-biased wiggle; not chosen, it bends pay) | 9.5 | ~32 |

All 20 leagues fitted the band in each. The ladder widens each tier's OVR spread by about 0.5, still inside §5's tolerances, but it pushed players at 90+ to 26 per league, so the Starter tier's `spread` went from 9 to **8.5** (back to 21.6). Evenly spaced strengths gave the same result as normal-curve ones, without an inverse-normal function.

## 10. Questions (answered 2026-10-22)

All three as recommended, with one confirmation ([D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application)):
1. ~~How do teams land in the cap band?~~ → **Trades, with the curve at 2.5.** *"Trade to fit sounds right, this is during the initial creation of the league only right?"* Yes: once, at creation; M4's rules take over afterwards.
2. ~~Where does draft history come from?~~ → **Ranked classes.**
3. ~~Where do OVR, the grade and the archetype label live?~~ → **`Application/Players`.**
4. ~~Will every team come out the same?~~ (the owner, walking through the guide: *"Is there a way to have it exist from day one?"*) → **A strength ladder carried by the young core** ([D-082](../DECISIONS.md#d-082-team-strength-from-a-ladder-carried-by-the-young-core)).
5. ~~Should cap space follow strength?~~ → **Not now:** one band for every team; parked in IDEAS.md for M4.
6. ~~Must the new `generation.json` fields be optional (D-077)?~~ → **No, required:** defaults are for the formats M11 imports ([D-083](../DECISIONS.md#d-083-optional-fields-with-defaults-are-for-importable-data-formats-only)).

The owner walked through every choice in §2 one by one on 2026-10-22 and kept them all; D-082 and D-083 came out of that walkthrough.
