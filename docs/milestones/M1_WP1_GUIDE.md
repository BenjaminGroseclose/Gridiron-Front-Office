# M1 · WP1 builder's guide: Core building blocks

> **Status:** ✅ **Done 2026-10-20** (§6 has the review) · **For:** the owner, who builds M1 ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP1](M1_THE_LEAGUE.md#wp1--core-ids-positions-and-the-prng-m--done-2026-10-20) · **Size:** M · **Written:** 2026-10-20
>
> Everything later in M1 stands on this: the random number generator every draw comes from, the seeds that make a league repeatable, the first typed IDs, the football enums, and the cap-hit formula. It's all pure Core code with no dependencies, so it's the easiest place to start and the most important to get exactly right.

---

## 0. Before you start

- You're on `main` (version `0.1.0`; no branches, D-076), and `./build.ps1` is green.
- Read: [D-012](../DECISIONS.md#d-012-deterministic-simulation-with-an-in-house-prng) (the PRNG), [D-015](../DECISIONS.md#d-015-integer-keys-with-strongly-typed-ids) (typed IDs), [D-074](../DECISIONS.md#d-074-contracts-are-real-from-m1-contract-actions-arrive-in-m4) (contracts), [F-03 §4.1–4.2](../features/F-03_PLAYERS.md#41-positions) (positions and the 42 attributes), and CLAUDE.md's "Determinism".
- **The guardrails will stop you if you slip:** Core can't reference any project or package (architecture tests), and `System.Random`, `DateTime.Now`/`UtcNow`, `Thread.Sleep` and blocking waits fail the build there (`build/BannedSymbols.Pure.txt`). Warnings are errors.

## 1. What you'll build

```
src/Core/
├─ Random/
│  ├─ SplitMix64.cs          the seeder, and its finalizer (Mix) that the seed hash reuses
│  ├─ GfoRandom.cs           xoshiro256**: NextUInt64, NextDouble, Next(min, max), Chance, Normal
│  ├─ Seeds.cs               Derive(leagueSeed, purpose, parts…) and the purpose names
├─ IDs/                      TeamID, PlayerID, ConferenceID, DivisionID, StadiumID, CollegeID, ContractID
├─ Players/                  Position, PlayerAttribute (42), PlayerStatus, PotentialGrade
├─ Leagues/                  CrestShape, CrestStripe, Roof, Surface, ClimateZone
└─ Contracts/
   └─ CapMath.cs             the cap hit per year (D-074)
tests/Core.Tests/            Random/, Players/, Contracts/ (Core.Tests has no tests yet)
```

Names are suggestions; keep them if you have no reason not to, so later guides line up.

## 2. The random number generator (D-012)

### 2.1 The algorithms

All three are public domain (xoshiro256\*\* and SplitMix64 by Blackman and Vigna, FNV-1a by Fowler, Noll and Vo). Use `ulong` everywhere. C# integer arithmetic is unchecked by default here, so multiplication wraps as these algorithms need. `System.Numerics.BitOperations.RotateLeft` does the rotations.

**SplitMix64** (seeding only):
```
next():  state += 0x9E3779B97F4A7C15
         return Mix(state)
Mix(z):  z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9
         z = (z ^ (z >> 27)) * 0x94D049BB133111EB
         return z ^ (z >> 31)
```

**xoshiro256\*\*** (`GfoRandom`): four `ulong`s of state, `s0..s3`, filled with **four successive SplitMix64 outputs** from the seed.
```
NextUInt64(): result = RotateLeft(s1 * 5, 7) * 9
              t = s1 << 17
              s2 ^= s0;  s3 ^= s1;  s1 ^= s2;  s0 ^= s3
              s2 ^= t
              s3 = RotateLeft(s3, 45)
              return result
```

### 2.2 The draws

| Method | How | Notes |
|---|---|---|
| `double NextDouble()` | `(NextUInt64() >> 11) * (1.0 / (1UL << 53))` | `[0, 1)`, all 53 bits of a double |
| `int Next(int minInclusive, int maxExclusive)` | **Lemire's method:** `range = (ulong)(max − min)`; `high = Math.BigMul(NextUInt64(), range, out low)`; if `low < range`, compute `threshold = (0 − range) % range` and redraw while `low < threshold`; return `min + (int)high` | Unbiased. `x % range` is biased; don't use it. Throw if `max <= min`. |
| `bool Chance(double p)` | `NextDouble() < p` | |
| `double Normal(double mean = 0, double sd = 1)` | **Box–Muller:** `u1 = 1.0 − NextDouble()` (so it's never 0), `u2 = NextDouble()`, `z = Math.Sqrt(−2 · Math.Log(u1)) · Math.Cos(2π · u2)`; return `mean + sd · z` | Don't cache the second value: the state stays just the four `ulong`s, which keeps "same seed, same draws" simple to reason about. |

Not in WP1: weighted picks. WP4's guide adds them when the generator needs them. (Correlated multivariate draws were dropped, D-078.)

**One instance per purpose, never shared across threads.** `GfoRandom` isn't thread-safe, and sharing one between, say, two teams' generation makes the results depend on order. Derive a seed per unit of work instead (§2.4).

### 2.3 Reference values

Computed with an independent implementation; your tests should match them exactly. (SplitMix64(0)'s first output, `0xE220A8397B1DCDAF`, is the published reference value.)

| Seed | Initial state `s0, s1, s2, s3` |
|---|---|
| 0 | `0xE220A8397B1DCDAF`, `0x6E789E6AA1B965F4`, `0x06C45D188009454F`, `0xF88BB8A8724C81EC` |
| 42 | `0xBDD732262FEB6E95`, `0x28EFE333B266F103`, `0x47526757130F9F52`, `0x581CE1FF0E4AE394` |

| Seed | First five `NextUInt64()` |
|---|---|
| 0 | `11091344671253066420`, `13793997310169335082`, `1900383378846508768`, `7684712102626143532`, `13521403990117723737` |
| 42 | `1546998764402558742`, `6990951692964543102`, `12544586762248559009`, `17057574109182124193`, `18295552978065317476` |

| Seed | First three `NextDouble()` (fresh generator) |
|---|---|
| 0 | `0.6012629994179048`, `0.7477740925472398`, `0.10301998939503632` |
| 42 | `0.08386297105988216`, `0.3789802506626686`, `0.6800434110281394` |

### 2.4 Derived seeds (`Seeds.Derive`)

Every independent stream of randomness gets its own seed from the league seed, a **purpose** and some **ids**: `Derive(seed, "gen", season, slot)` for a generated player, `Derive(seed, "perception", viewer, player, season)` for fog of war (M6). Changing one player's draws then can't shift anyone else's.

```csharp
public static ulong Derive(ulong leagueSeed, string purpose, params ReadOnlySpan<long> parts)
```

**Exactly this**, so the values below hold:
1. FNV-1a 64 over these bytes, in order: `leagueSeed` as 8 bytes little-endian; `purpose` as UTF-8; then for each part, a separator byte `0x1F` followed by the part as 8 bytes little-endian (a `long` reinterpreted as `ulong`).
   FNV-1a 64: `h = 0xCBF29CE484222325`; for each byte: `h ^= byte; h *= 0x100000001B3`.
2. Return `SplitMix64.Mix(h)`.

| Call | Result |
|---|---|
| `Derive(42, "gen", 2026, 0)` | `5954498215675448378` |
| `Derive(42, "gen", 2026, 1)` | `1676545436668552088` |
| `Derive(43, "gen", 2026, 0)` | `16717909402313610759` |
| `Derive(42, "perception", 7, 1234, 2026)` | `18163511695943298027` |

- **Never** `string.GetHashCode()` or `HashCode.Combine`: .NET randomizes them per process, so a league would differ on every launch.
- Keep the purposes as constants in one place (`SeedPurpose.Generation = "gen"`, …). A typo in a purpose string silently makes a different league.
- `BinaryPrimitives.WriteUInt64LittleEndian` and `Encoding.UTF8.GetBytes` (or a stackalloc span) do the byte work.

### 2.5 Tests that define "done"

| Test | Checks |
|---|---|
| The reference values (§2.3, §2.4) | Exact matches, for both seeds and every `Derive` row |
| Same seed, same sequence; different seeds differ | 1,000 draws |
| `NextDouble` | Always in `[0, 1)`; the mean of 100,000 is 0.5 ± 0.005 |
| `Next(min, max)` | Always in bounds; 100,000 draws into 10 buckets land within ±5% of 10,000 each; `max <= min` throws |
| `Normal` | The mean of 100,000 is 0 ± 0.02 and the SD 1 ± 0.02; `mean` and `sd` shift and scale it |
| `Derive` | Any changed input changes the result; part order matters (`(…, 1, 2)` ≠ `(…, 2, 1)`); `("gen")` ≠ `("gen", 0)` |

Statistical tests use a fixed seed, so they're deterministic, never flaky.

## 3. Typed IDs (D-015)

All in **one file**, `src/Core/IDs/EntityIDs.cs`, one line each (owner, 2026-10-20: one file per ID was overkill):

```csharp
public readonly record struct TeamID(int Value) : IStronglyTypedID;
```

The same for `PlayerID`, `ConferenceID`, `DivisionID`, `StadiumID`, `CollegeID` and `ContractID`. That's all a new ID needs: Infrastructure's converter scans the Core assembly for `IStronglyTypedID` types and stores each as a plain `INTEGER` column (the key gets `AUTOINCREMENT`; M0, `TypedIDs.cs`). Write `ID`, never `Id` (D-069); a test enforces it.

- **Structs, not classes:** an ID costs what an `int` costs (no heap allocation; the sim creates millions), and it can't be null unless you write `TeamID?` on purpose ("no team").
- **No `ToString` override**, to keep each ID one line: logs show `PlayerID { Value = 812 }`. Use `id.Value` where only the number should appear.
- **Sort in memory by `.Value`** (a record struct isn't comparable); inside an EF query, `OrderBy(p => p.ID)` is fine.
- **Tests** (`tests/Core.Tests/IDs/TypedIDTests.cs`, done): the list of IDs, and each one's shape (name ends in `ID`, a public `(int)` constructor, `readonly`).

## 4. The enums

**Done** (2026-10-20), in two files: `src/Core/Players/PlayerEnums.cs` (`Position`, `PlayerStatus`, `PotentialGrade`, `AttributeGroup`, `PlayerAttribute`) and `src/Core/Leagues/TeamEnums.cs` (`CrestShape`, `CrestStripe`, `Roof`, `Surface`, `ClimateZone`).

**Enums are stored by name in the save** (M0's `LeagueDbContext` converts every enum to its name), so reordering or adding members is safe, but renaming one needs a migration.

| Enum | Members | Why this way |
|---|---|---|
| `Position` | `QB, RB, FB, WR, TE, OT, OG, C, EDGE, DT, LB, CB, S, K, P, LS` | Each name is its abbreviation, so `ToString()` is the screen label and no display map is needed |
| `PlayerStatus` | `Prospect, Active, InjuredReserve, FreeAgent, Retired` | `TeamID` is set for Active and InjuredReserve and null for the rest (`PracticeSquad` left with D-080) |
| `PotentialGrade` | `F, D, CMinus, C, CPlus, BMinus, B, BPlus, AMinus, A, APlus` | **Worst to best**, so `grade >= PotentialGrade.B` means "B or better"; "A+" and "A−" come from a display map. The bands that map peak OVR to a grade come in WP4. |
| `AttributeGroup` | `Physical, Mental, Passing, BallCarrying, Receiving, Blocking, Defense, SpecialTeams` | F-03 §4.2's groups, the Player page's order (WP9). Mapping each attribute to its group comes with that page. |
| `PlayerAttribute` | The 42 from F-03 §4.2, in its order | Not `Attribute` (collides with `System.Attribute`). The name trips CA1711 (types ending in `Attribute` are reserved for attribute classes), so the enum carries a justified suppression rather than renaming the concept every doc calls an attribute. |
| `CrestShape` | `Shield, Roundel, Hexagon, Diamond, Banner` | |
| `CrestStripe` | `None, Bar, TwinBars, Chevron, Sash` | F-02 said `Single` and `Double`, which fail CA1720 (they're .NET type names); F-02 now uses these names |
| `Roof` / `Surface` / `ClimateZone` | `Open, Dome, Retractable` / `Grass, Turf` / `Cold, Temperate, Hot` | |

**Tests** (`tests/Core.Tests/Players/PlayerEnumTests.cs`, done): 42 attributes and 16 positions (D-050 hangs on those counts), position names are the abbreviations, and grades run worst to best.

## 5. The cap-hit formula (D-074)

```csharp
public static class CapMath
{
    /// The signing bonus counted against the cap in year yearIndex (0-based) of a contract of years seasons.
    public static long ProratedBonus(long signingBonus, int years, int yearIndex);

    /// A year's cap hit: its base salary plus that year's share of the signing bonus.
    public static long CapHit(long baseSalary, long signingBonus, int years, int yearIndex);
}
```

- **Proration:** the bonus is spread evenly over the first `min(years, 5)` seasons and counts nothing after that. Money is `long` dollars, so the division leaves a remainder: **put the remainder in the first year**, so the prorated shares always add up to exactly the bonus.
- **Throw** for `years` outside 1–7, a `yearIndex` outside the contract, or negative amounts. These are programming errors, not domain failures.
- **Tests:** a 3-year, $9,000,000 bonus prorates $3,000,000 a year; a 7-year deal spreads its bonus over 5 years and nothing in years 6–7; $10,000,001 over 3 years gives $3,333,335, $3,333,333, $3,333,333; for any contract, the shares sum to the bonus; `CapHit` = base + share.
- Guarantees, dead money and roster bonuses arrive with F-12's M1 spec (structure) and M4 (actions); this formula doesn't change for them.

## 6. Done when

- [x] Every test in §2.5, §3–5 passes, and `./build.ps1` is green: 42 Core tests.
- [x] No banned API in Core, no `Id` in a name.
- [x] **Reviewed** (2026-10-20). What the review found and fixed:
  - **A bug in `CapMath.ProratedBonus`:** it checked `yearIndex` against the 7-year maximum instead of `years`, so a contract of any length was accepted. Fixed; the test `Impossible_contracts_throw` (8 years) now guards it.
  - **A wrong comment:** proration stops at 5 seasons because that's the league's rule, not because "the last year is never prorated" (a 3-year deal prorates all three). `MaxContractYear` became `MaxContractYears`.
  - **Tidying:** the constructor's doc comment had landed on a field; an unused `using`; tabs converted to spaces (`.editorconfig`).
- **What was built:** `Random/` (`SplitMix64`, `GfoRandom`, `Seeds` + `SeedPurpose`), `IDs/EntityIDs.cs`, `Players/PlayerEnums.cs`, `Leagues/TeamEnums.cs`, `Contracts/CapMath.cs`, and `tests/Core.Tests/` (`Random/`, `IDs/`, `Players/`, `Contracts/`).

## 7. While you build this

I'll start preparing **WP2's data**: `teams.json` from F-02's Appendix A, the fictional colleges, the blocklists, and the F-02 guardrail references (each metro's real NFL colors). I'll also draft **F-01's and F-12's M1 specs** for your review. The name and place downloads come with WP3, and I'll list each one (file, source, size) for your approval first.
