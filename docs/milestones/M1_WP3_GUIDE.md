# M1 · WP3 builder's guide: player data

> **Status:** Built 2026-10-21 (§3 scaffolded by Claude for the owner's review; §4 tests green; `contracts.json` built against F-12's M1 slice, D-080) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP3](M1_THE_LEAGUE.md#wp3--player-data-ml) · **Size:** M–L · **Written:** 2026-10-21
>
> WP3 puts every number the player generator (WP4) needs into data files with first-pass values: each position's roster targets, body and athletic testing, OVR weights and archetypes; the league-wide generation settings; the jersey rule; the name and hometown lists; and the famous-people blocklist. Nothing generates players yet. WP3 only loads the numbers and proves them consistent, so WP4 can tune data instead of code.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepares** (§2, §7) | The data files: `positions.json`, `generation.json`, `jersey-rules.json` (checked against the real NFL rule), `famous-people.json`, and, after each download is approved, `first.json`, `last.json` and `hometowns.json`. `contracts.json` follows F-12's M1 spec (§8): its shape and rules are [F-12 §4.9–4.10](../features/F-12_CONTRACTS.md). |
| **You build** (§3) | Three Core enums (plus `ContractType`), the definition records, `ISeedData`'s new properties, the loader's new files and one log event |
| **Claude writes the tests** (§4) | Every file parses; the numbers are consistent (weights sum to 1, roster targets add up to 53, every attribute is used; the contract rules R1–R10 of F-12 §4.10) |

**Read first:** F-03 §4.1–4.11 and §5, [D-043](../DECISIONS.md#d-043-limited-personality-model-only-traits-with-a-consumer), [D-050](../DECISIONS.md#d-050-every-player-attribute-must-be-read-by-the-sim), [D-077](../DECISIONS.md#d-077-data-files-carry-no-version-numbers-they-change-only-additively), [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) (#2: skills are archetype + shared quality + noise), and the [WP2 guide](M1_WP2_GUIDE.md) §3.4 (the loader you're extending).

## 1. What you'll build

```
data/
├─ players/      positions.json, generation.json, jersey-rules.json      (Claude)
├─ names/        first.json, last.json                                     (Claude, after approved downloads)
├─ places/       hometowns.json                                            (Claude, after an approved download)
├─ blocklists/   famous-people.json   (ships in the app: the generator re-rolls a famous name)
└─ contracts/    contracts.json       (Claude, from F-12 §4.9)
src/
├─ Core/Players/GenerationEnums.cs     Measurable, PlayerTier, DevelopmentCurve (next to PlayerEnums.cs)
├─ Core/Contracts/ContractType.cs      Rookie, Veteran, Minimum (F-12 §4.9)
├─ Application/Players/PlayerData.cs   the definition records (§3.2)
├─ Application/Contracts/ContractData.cs   the contract records (F-12 §4.9)
├─ Application/Leagues/ISeedData.cs    + eight properties
└─ Infrastructure/
   ├─ GridironFrontOffice.Infrastructure.csproj   + eight embedded files
   └─ Seed/EmbeddedSeedData.cs, SeedLog.cs        + one Lazy per file, event 1202
tests/Infrastructure.Tests/Seed/   PlayerDataTests, NameDataTests, ContractDataTests   (Claude)
```

## 2. The data formats

Like WP2's files: camelCase field names, enum values **and enum dictionary keys** written as the C# names (`"CB"`, `"RouteRunning"`, `"Forty"`), no version field, and every format only grows (D-077). Ratings are on the internal 0–100 scale (F-03 §4.2).

### 2.1 `data/players/positions.json`: one entry per position

```json
{
  "positions": [
    {
      "position": "CB",
      "roster": { "average": 6, "min": 5, "max": 7, "floor": 4, "starters": 3 },
      "age": { "mean": 26, "sd": 3, "min": 21, "max": 35 },
      "measurables": {
        "Height": { "mean": 71.5, "sd": 1.6 }, "Weight": { "mean": 193, "sd": 8 },
        "Forty": { "mean": 4.48, "sd": 0.08 }, "TenSplit": { "mean": 1.54, "sd": 0.04 },
        "ThreeCone": { "mean": 6.95, "sd": 0.15 }, "Shuttle": { "mean": 4.20, "sd": 0.10 },
        "Bench": { "mean": 14, "sd": 4 }, "Vertical": { "mean": 36.5, "sd": 2.5 },
        "BroadJump": { "mean": 124, "sd": 5 }, "ArmLength": { "mean": 31.5, "sd": 0.9 }, "HandSize": { "mean": 9.1, "sd": 0.4 }
      },
      "ovrWeights": { "ManCoverage": 0.20, "ZoneCoverage": 0.18, "Speed": 0.15, "Press": 0.10, "BallSkills": 0.10, "…": "sum to 1" },
      "archetypes": [
        { "name": "Man", "share": 0.40, "skills": { "ManCoverage": 70, "Press": 68, "ZoneCoverage": 58, "BallSkills": 62, "Tackling": 55 } },
        { "name": "Zone", "share": 0.35, "skills": { "ZoneCoverage": 70, "Awareness": 66, "ManCoverage": 58, "BallSkills": 65, "Tackling": 58 } },
        { "name": "Slot", "share": 0.25, "skills": { "ManCoverage": 66, "ZoneCoverage": 64, "Tackling": 60, "Return": 60 } }
      ]
    }
  ]
}
```

| Field | Meaning | How WP4 uses it |
|---|---|---|
| `roster` | F-03 §4.1: the league `average` of 53, a team's typical `min`–`max`, the `floor` every team keeps, and how many `starters` the position fields | A team's count is drawn within `min`–`max` so the league averages hold; the top `starters` are the Starter tier (§2.2) |
| `age` | Ages by position (RB young, K/P old); rookies are 21–23 | Drawn, rounded, clamped to `min`–`max` |
| `measurables` | The 11 combine numbers, in inches, pounds, seconds and reps (§3.1) | Drawn per player (§2.2's `athletic`), stored, and four of them become physical ratings |
| `ovrWeights` | F-03 §4.5's hand-set first pass, summing to 1. SimLab replaces them in M2 | `OVR = Σ wᵢ · attributeᵢ`, onto the 0–99 display scale |
| `archetypes` | A few profiles per position: how often each appears (`share`, summing to 1), and the mean of each skill it cares about **for a Backup-tier player** | A player draws one archetype; each listed skill starts from its mean (§2.2) |

### 2.2 `data/players/generation.json`: the league-wide settings

```json
{
  "roster": { "active": 53, "freeAgents": 150 },
  "tiers": {
    "Starter": { "skillShift": 10.5, "spread": 9, "athleticShift": 0.5, "target": { "mean": 74, "sd": 7 } },
    "Backup":  { "skillShift": -3.5, "spread": 6, "athleticShift": 0,   "target": { "mean": 64, "sd": 6 } },
    "Fringe":  { "skillShift": -12.5, "spread": 5, "athleticShift": -0.5, "target": { "mean": 56, "sd": 5 } }
  },
  "skills": {
    "common": { "Stamina": 70, "Toughness": 70, "Awareness": 60, "Discipline": 65 },
    "offPosition": { "mean": 25, "sd": 8, "min": 0, "max": 100 },
    "noiseSd": 4
  },
  "athletic": {
    "drillCorrelation": 0.6,
    "physicalNoiseSd": 3,
    "ratings": [
      { "measurable": "Forty", "attribute": "Speed", "best": { "value": 4.25, "rating": 99 }, "worst": { "value": 5.45, "rating": 40 } },
      { "measurable": "TenSplit", "attribute": "Acceleration", "best": { "value": 1.45, "rating": 99 }, "worst": { "value": 1.95, "rating": 40 } },
      { "measurable": "ThreeCone", "attribute": "Agility", "best": { "value": 6.55, "rating": 99 }, "worst": { "value": 8.20, "rating": 40 } },
      { "measurable": "Shuttle", "attribute": "Agility", "best": { "value": 3.90, "rating": 99 }, "worst": { "value": 4.95, "rating": 40 } },
      { "measurable": "Bench", "attribute": "Strength", "best": { "value": 38, "rating": 99 }, "worst": { "value": 5, "rating": 40 } }
    ]
  },
  "hidden": {
    "potentialGrowth": [
      { "throughAge": 22, "growth": { "mean": 8, "sd": 5, "min": 0 } },
      { "throughAge": 25, "growth": { "mean": 4, "sd": 4, "min": 0 } },
      { "throughAge": 27, "growth": { "mean": 1, "sd": 2, "min": 0 } },
      { "throughAge": 99, "growth": { "mean": 0, "sd": 0 } }
    ],
    "potentialGrades": [ { "grade": "APlus", "minPeakOvr": 92 }, { "grade": "A", "minPeakOvr": 88 }, "…", { "grade": "F", "minPeakOvr": 0 } ],
    "developmentCurve": { "Early": 0.2, "Normal": 0.6, "Late": 0.2 },
    "bustRisk": { "mean": 0.15, "sd": 0.10, "min": 0, "max": 1 },
    "peakAgeOffset": { "mean": 0, "sd": 1, "min": -3, "max": 3 },
    "injuryProneness": { "mean": 1, "sd": 0.2, "min": 0.5, "max": 2 }
  },
  "personality": { "value": { "mean": 50, "sd": 18, "min": 0, "max": 100 }, "lowBelow": 35, "highAbove": 65 },
  "collegeTiers": {
    "Starter": { "Power": 0.60, "Major": 0.30, "Small": 0.10 },
    "Backup":  { "Power": 0.40, "Major": 0.40, "Small": 0.20 },
    "Fringe":  { "Power": 0.25, "Major": 0.40, "Small": 0.35 }
  },
  "targets": { "ovr90Plus": { "min": 15, "max": 25 }, "meanAge": { "value": 26.5, "tolerance": 1 }, "rosterMixTolerance": 0.3 }
}
```

**How WP4 turns these into a player** (the contract between this data and WP4's code; its guide has the details):

| Step | Rule |
|---|---|
| Tier | The top `roster.starters` at a position are **Starter**, the rest of the 53 **Backup**, free agents **Fringe** (no practice squad in 1.0, D-080) |
| Quality | One draw `q ~ N(0, 1)` per player: how good they are within their tier |
| Measurables | Each from its position's `mean` and `sd`, shifted by the tier's `athleticShift` (in SDs, toward "better"; for timed drills better is lower). The six drills (Forty, TenSplit, ThreeCone, Shuttle, Vertical, BroadJump) share one athletic draw with `drillCorrelation`, so a fast player also tests well elsewhere |
| Physical ratings | Speed, Acceleration, Agility and Strength: the straight line through `best` and `worst`, averaged when two measurables feed one rating (Agility), plus `N(0, physicalNoiseSd)` (F-03 §4.3's ±3), clamped to 0–100 |
| Skills (D-078 #2) | Each attribute's mean comes from the archetype if it lists it, else `skills.common`, else it's off-position (`offPosition`, no tier shift). Then `rating = mean + tier.skillShift + q · tier.spread + N(0, noiseSd)`, clamped |
| OVR | `Σ wᵢ · attributeᵢ`. The tier `target`s are what WP4's distribution tests check, not inputs |
| Potential | Current OVR plus the growth band for the player's age (F-03 §4.6: at least current OVR when young); shown through `potentialGrades` |
| Hidden ratings, personality | Each drawn from its distribution and clamped. Personality is one distribution for all six traits; `lowBelow`/`highAbove` make the Low/Average/High descriptors (F-03 §4.7) |
| College | Its tier from `collegeTiers` by the player's tier, then a college of that tier |
| Targets | Read only by WP4's tests: 15–25 players at 90+, mean age 26.5 ± 1, roster mix within ±0.3 (F-03 §4.10) |

### 2.3 `data/players/jersey-rules.json`

```json
{ "rules": [ { "position": "QB", "numbers": [ { "from": 0, "to": 19 } ] }, { "position": "WR", "numbers": [ { "from": 0, "to": 49 }, { "from": 80, "to": 89 } ] } ] }
```

The current NFL rule by position, **checked against the real rule when the file is written** (F-03 §4.8). The values above only show the shape. Long snappers follow the rule for the position they're listed at.

### 2.4 Names, places and famous people

| File | Shape | Source |
|---|---|---|
| `data/names/first.json` | `{ "source": "…", "names": [ { "name": "Michael", "weight": 886370 } ] }` | SSA baby names (public domain): male births 1986–2005 (the birth years of today's players), the top 2,000 names given to boys at least as often as to girls, placeholders like "Baby" removed, weighted by births |
| `data/names/last.json` | Same shape | Census 2010 surnames (public domain): the top 5,000, weighted by count |
| `data/places/hometowns.json` | `{ "source": "…", "places": [ { "name": "Akron", "state": "OH", "population": 190469 } ] }` | Census city and town population estimates (public domain): places of 10,000 people or more, weighted by population. *(The Gazetteer was the plan, but its current editions carry no population.)* |
| `data/blocklists/famous-people.json` | `["Jerry Rice", …]`, like the other blocklists | Compiled: Pro Football Hall of Fame members, notable players of the last ~25 seasons, presidents, and other very famous athletes and entertainers (F-03 §4.11) |

`famous-people.json` is the one blocklist that **ships**: the generator needs it at runtime to re-roll a famous name. Names are facts, so the list is fine to ship.

## 3. The code

### 3.1 Core enums (`Core/Players/`, next to `PlayerEnums.cs`)

```csharp
/// <summary>The combine numbers stored for every player (F-03 §4.3). Units: inches, pounds, seconds, reps of 225 lb.</summary>
public enum Measurable { Height, Weight, Forty, TenSplit, ThreeCone, Shuttle, Bench, Vertical, BroadJump, ArmLength, HandSize }

/// <summary>Where a generated player sits on the depth chart, worst to best (F-03 §4.10).</summary>
public enum PlayerTier { Fringe, Backup, Starter }

/// <summary>When a player peaks relative to the position's curve (F-03 §4.6; read by F-20 in M5).</summary>
public enum DevelopmentCurve { Early, Normal, Late }
```

The three are what the data files key on. The six personality traits get their enum in WP4, with the columns that store them (D-078 #9).

### 3.2 The definition records (`Application/Players/PlayerData.cs`)

The same pattern as WP2's `TeamSet.cs`: positional records that mirror the files, `IReadOnlyList`/`IReadOnlyDictionary`, and an optional field only where the file can leave it out (`Min`, `Max`).

```csharp
public sealed record PositionData(IReadOnlyList<PositionProfile> Positions);
public sealed record PositionProfile(Position Position, RosterTarget Roster, Distribution Age,
    IReadOnlyDictionary<Measurable, Distribution> Measurables, IReadOnlyDictionary<PlayerAttribute, double> OvrWeights,
    IReadOnlyList<Archetype> Archetypes);
public sealed record RosterTarget(double Average, int Min, int Max, int Floor, int Starters);
public sealed record Archetype(string Name, double Share, IReadOnlyDictionary<PlayerAttribute, double> Skills);

/// <summary>A normal distribution, clamped to Min–Max when they're given.</summary>
public sealed record Distribution(double Mean, double Sd, double? Min = null, double? Max = null);

public sealed record GenerationSettings(RosterSizes Roster, IReadOnlyDictionary<PlayerTier, TierSettings> Tiers,
    SkillSettings Skills, AthleticSettings Athletic, HiddenSettings Hidden, PersonalitySettings Personality,
    IReadOnlyDictionary<PlayerTier, IReadOnlyDictionary<CollegeTier, double>> CollegeTiers, GenerationTargets Targets);
public sealed record RosterSizes(int Active, int FreeAgents);
public sealed record TierSettings(double SkillShift, double Spread, double AthleticShift, Distribution Target);
public sealed record SkillSettings(IReadOnlyDictionary<PlayerAttribute, double> Common, Distribution OffPosition, double NoiseSd);
public sealed record AthleticSettings(double DrillCorrelation, double PhysicalNoiseSd, IReadOnlyList<PhysicalRating> Ratings);
public sealed record PhysicalRating(Measurable Measurable, PlayerAttribute Attribute, RatingPoint Best, RatingPoint Worst);
public sealed record RatingPoint(double Value, double Rating);
public sealed record HiddenSettings(IReadOnlyList<PotentialGrowth> PotentialGrowth, IReadOnlyList<GradeBand> PotentialGrades,
    IReadOnlyDictionary<DevelopmentCurve, double> DevelopmentCurve, Distribution BustRisk, Distribution PeakAgeOffset,
    Distribution InjuryProneness);
public sealed record PotentialGrowth(int ThroughAge, Distribution Growth);
public sealed record GradeBand(PotentialGrade Grade, double MinPeakOvr);
public sealed record PersonalitySettings(Distribution Value, double LowBelow, double HighAbove);
public sealed record GenerationTargets(CountRange Ovr90Plus, ValueTarget MeanAge, double RosterMixTolerance);
public sealed record CountRange(int Min, int Max);
public sealed record ValueTarget(double Value, double Tolerance);

public sealed record JerseyRules(IReadOnlyList<JerseyRule> Rules);
public sealed record JerseyRule(Position Position, IReadOnlyList<NumberRange> Numbers);
public sealed record NumberRange(int From, int To);

public sealed record NameList(string Source, IReadOnlyList<WeightedName> Names);
public sealed record WeightedName(string Name, int Weight);
public sealed record HometownList(string Source, IReadOnlyList<Hometown> Places);
public sealed record Hometown(string Name, string State, int Population);
```

### 3.3 `ISeedData` grows

```csharp
public interface ISeedData
{
    TeamSet TeamSet { get; }
    CollegeList Colleges { get; }
    PositionData Positions { get; }
    GenerationSettings Generation { get; }
    JerseyRules JerseyRules { get; }
    NameList FirstNames { get; }
    NameList LastNames { get; }
    HometownList Hometowns { get; }
    IReadOnlyList<string> FamousPeople { get; }
    ContractData Contracts { get; }     // F-12 §4.9, WP3's last slice
}
```

One port for all built-in data, so the generators still read only `ISeedData`.

### 3.4 The loader (`EmbeddedSeedData`)

- **Embed** the eight files with logical names, as WP2 did: `players/positions.json`, `players/generation.json`, `players/jersey-rules.json`, `names/first.json`, `names/last.json`, `places/hometowns.json`, `blocklists/famous-people.json`, `contracts/contracts.json`.
- **One `Lazy<T>` per file**, read with the existing `Read<T>` and `JsonOptions`. Nothing changes in the strict parsing; enum dictionary keys are covered by the same converter (a test proves a misspelled key fails).
- **Log each load** with one new event: **1202**, Information, `Loaded {Resource} in {ElapsedMs} ms`. Teams and colleges keep 1200 and 1201.
- The name files are the largest (a few hundred KB), and they load only when a league is generated.

## 4. The tests (Claude writes them)

In `tests/Infrastructure.Tests/Seed/`, against the shipped files. Each lists every offender, as in WP2.

| Test | Rule |
|---|---|
| Every file loads | Seven files parse; counts are sane |
| Every position is described once | All 16 `Position`s, no duplicates; every one has all 11 measurables with `sd > 0` |
| Roster targets add up | Averages sum to 53; `floor ≤ min ≤ average ≤ max`; `starters ≤ floor`; the floors fit in 53 and the maxima reach it |
| Ages are possible | `21 ≤ min < mean < max` |
| OVR weights sum to 1 | Per position, every weight > 0 |
| Archetypes are well formed | At least one per position; shares sum to 1; unique names; skill means within 0–100 |
| Every attribute is used (D-050) | Each of the 42 appears in some OVR weights, archetype, `skills.common` or physical rating map |
| Generation settings are complete | Every `PlayerTier` has settings; Speed, Acceleration, Agility and Strength each have a map, with `best` and `worst` different; all 11 grades, in descending order, ending at 0; development curve and college-tier shares sum to 1; potential growth ages ascend and reach 99 |
| Jersey numbers exist | Every position has a rule; numbers within 0–99; each position has at least its roster `max` numbers available |
| Names and places are usable | No empty or duplicate names; weights and populations > 0; two-letter states; at least 1,000 first names, 5,000 last names and 1,000 places |
| Famous people are blocked | The list is non-empty and holds known names (Jerry Rice, Tom Brady) |
| A misspelled enum key fails | `"RoutRunning"` in an archetype is a `JsonException` |
| The contract data is consistent | F-12 §4.10's R1–R10 (`ContractDataTests`): the ladder ascends, the rookie rounds descend and cover the minimums, every position has a maximum, the curve pays a Starter well above the minimum, and a roster at the curve's expected values lands within 0.8–1.3 × cap |

## 5. Done when

- [ ] The three enums (and `ContractType`), the records, `ISeedData`'s eight properties, the eight embedded files and event 1202. _Scaffolded by Claude 2026-10-21; your review is the tick._
- [ ] Every §4 test passes (50, including `ContractDataTests`, each proven on a planted problem); `./build.ps1` is green.
- [ ] **Ask for a review**, then commit with the message I give you.

## 6. Order of work

1. **Now:** I prepare `positions.json`, `generation.json`, `jersey-rules.json` and `famous-people.json` (no downloads). You can build §3 against them.
2. **Downloads:** for the name and place lists, I ask before each download, with its file name, source and size: the SSA national baby-name file, the Census 2010 surname file, and the Census Gazetteer places file. Their sources go in THIRD_PARTY_NOTICES.
3. **After F-12's M1 spec** (§8, question 3): `contracts.json`, its records and its tests, as WP3's last slice. _Done 2026-10-21, the same day the spec was revised on your answers (D-080)._

## 7. Where the data stands

**Ready (2026-10-21):** `positions.json`, `generation.json`, `jersey-rules.json` and `famous-people.json`. Every §4 rule passes against them in a reference run.
- **Measurables:** public NFL combine averages by position. **OVR weights and archetypes:** hand-set, so SimLab can replace the weights in M2.
- **A rough reference simulation of §2.2's model** already lands near F-03's targets after two small tuning passes (tier shifts and spreads; tight ends and linebackers +5 and +3, kickers, punters and long snappers −2). Over four seeds: Starter OVR 75.4–75.6 (target 74), Backup 64.1–64.5 (64), Fringe 56.2–56.7 (56), 14–24 players at 90+ (15–25), mean age 26.7 (26.5 ± 1). WP4's distribution tests do the real tuning.
- **The jersey rule** is the 2026 NFL rulebook's (Rule 5, Section 1): QB 0–19; RB, FB, TE, WR 0–49 and 80–89; offensive line 50–79; defensive line 50–79 and 90–99; linebackers 0–59 and 90–99; defensive backs 0–49; kickers and punters 0–49 and 90–99. The rulebook doesn't name two of our positions. **EDGE** players are listed as linemen or linebackers, so they get both (0–79 and 90–99). **Long snappers** are listed as tight ends or linebackers and nearly all wear the 40s, so they get 40–49.
- **`famous-people.json`:** 1,063 names, compiled from memory: Hall of Famers, stars, coaches and broadcasters of roughly the last 25 seasons, every president, and very famous athletes and entertainers. Common-name stars ("Josh Allen", "Chris Johnson", "Mike Williams") are in it on purpose.

- **`first.json` and `last.json` (2026-10-21):** 2,000 first names (SSA, male births 1986–2005, 94.6% of eligible boys' births; female-majority names like Ashley and Madison and placeholders like "Baby" dropped, which also drops a few unisex names such as Peyton) and 5,000 surnames (Census 2010, 60% of Americans; "MCDONALD" → "McDonald"). Sized by the owner (2026-10-21) after a reference run: about 2.4 repeated surnames per 53-man roster, close to real rosters; 1,000 surnames would give 4.6. Sources in THIRD_PARTY_NOTICES.
- **`hometowns.json` (2026-10-21):** 3,489 places of 10,000 people or more from the Census population estimates (the Gazetteer, the first plan, no longer carries population): incorporated places, New England towns (their local governments) and Hawaii's counties (it has no incorporated places), with the government types stripped ("Nashville-Davidson metropolitan government (balance)" → Nashville). Every state and DC.

- **`contracts.json` (2026-10-21):** the numbers of [F-12 §4.3–4.7](../features/F-12_CONTRACTS.md), revised on the owner's answers (D-080): a $300M cap, the 2026 minimum ladder, a rookie scale by round, one APY maximum per position with `curve` 3.5 and a ±20% wiggle, lengths and shares by contract tier. A roster at the curve's expected values prices at 0.98 × cap (R10).

**Next:** nothing in WP3. The owner reviews the §3 code and commits; WP4 generates from it.

## 8. Questions for you (answered 2026-10-21)

All three as recommended ([D-079](../DECISIONS.md#d-079-wp3-player-data-traits-in-generationjson-measurable-priors-deferred-f-12-before-contracts)): *"agree with all three, go ahead"*.
1. ~~Fold `traits.json` into `generation.json`?~~ → **Yes.** F-03 §4.7 updated.
2. ~~Defer the measurable "priors"?~~ → **Yes.** F-03 §4.3 updated.
3. ~~F-12's M1 spec next?~~ → **Yes.** `contracts.json` follows it as WP3's last slice.
