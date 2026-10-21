# M1 · WP2 builder's guide: team and college data

> **Status:** ✅ **Done 2026-10-21** (§6 has the review) · **For:** the owner, who builds M1 ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP2](M1_THE_LEAGUE.md#wp2--team-and-college-data-m--done-2026-10-21) · **Size:** M · **Written:** 2026-10-20
>
> WP2 turns the league's identity into data: the 32 teams (F-02) and about 230 fictional colleges (D-035) as JSON files, loaded once and guarded by tests that prove no team copies a real one and every color is readable. Generation (WP4) and league creation (WP5) read everything through one port, `ISeedData`; nothing hard-codes a team or a college.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepares** (§2) | The data files: `teams.json` (F-02's appendix, colors adjusted where the guardrails fail, for your review), `colleges.json` (~230 fictional programs), their two public schemas, and the test-only blocklists |
| **You build** (§3–§5) | The definition records, the `ISeedData` port, the embedded loader and the logging. **Claude writes the tests** (owner, 2026-10-21) |

**Read first:** F-02 §4–6 and §10–13, [D-033](../DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames), [D-035](../DECISIONS.md#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11), [D-064](../DECISIONS.md#d-064-f-02-team-set-accepted-shared-city-labels).

## 1. What you'll build

```
data/
├─ league/       teams.json, teams.schema.json, colleges.json, colleges.schema.json               (Claude)
└─ blocklists/   football-teams.json, major-league-teams.json, stadiums.json, metro-nfl-colors.json,
                 real-colleges.json                                                                 (Claude, test-only)
src/
├─ Core/
│  └─ Leagues/                       + CollegeRegion, CollegeTier (next to TeamEnums.cs)
├─ Application/Leagues/
│  ├─ TeamSet.cs                     the definition records (teams, conferences, colleges)
│  └─ ISeedData.cs                   the port: the team set and college list
└─ Infrastructure/Seed/
   ├─ EmbeddedSeedData.cs            reads the embedded files, parses strictly, caches
   └─ SeedLog.cs                     events 1200–1219
tests/
└─ Infrastructure.Tests/Seed/        EmbeddedSeedDataTests, TeamGuardrailTests, CollegeGuardrailTests, ColorChecksTests
                                     (+ the RepoFiles and ColorChecks helpers)
```

## 2. The data formats (Claude writes the files; your code reads them)

Only `teams.json` and `colleges.json` ship inside the app. The blocklists are **test-only**: they hold real team, stadium and college names, so they never go into the binary; the tests read them from the repository.

### 2.1 `data/league/teams.json`

F-02 §5's shape, plus the set's name (logged when it loads; M11's import stores it in the save, D-078). No version field and no `.v1` in the name: the built-in files ship inside the app, and the format only grows (D-077):

```json
{
  "name": "Default",
  "conferences": [
    { "key": "HER", "name": "Heritage Conference",
      "divisions": [ { "key": "HER-E", "name": "East", "teams": ["NYX", "PHI", "WAS", "BAL"] } ] }
  ],
  "teams": [
    { "abbr": "NYX", "city": "New York", "nickname": "Excelsiors", "metro": "new-york",
      "colors": { "primary": "#A6192E", "secondary": "#2B2E34", "accent": "#C9CDD2" },
      "crest": { "shape": "Shield", "monogram": "NY", "stripe": "TwinBars" },
      "stadium": { "name": "Skyline Field", "capacity": 82500, "roof": "Open", "surface": "Grass", "climate": "Cold" } }
  ]
}
```

Enum values are the C# names exactly (`Shield`, `TwinBars`, `Retractable`). `metro` is a lowercase key shared by the two teams in a two-team metro (`new-york`, `los-angeles`) and used to look up the blocklists by city.

### 2.2 `data/league/colleges.json`

```json
{
  "name": "Default",
  "colleges": [
    { "name": "Ridge State", "region": "Midwest", "tier": "Power" },
    { "name": "Coastal Tech", "region": "Southeast", "tier": "Small" }
  ]
}
```

**Regions and tiers are new** (D-035 only says "tagged with a region and tier"); this is the proposal, and the counts are data, so they can change:

| Enum | Members | Used by |
|---|---|---|
| `CollegeRegion` | `Northeast, Southeast, Midwest, Southwest, West` | Scouting coverage: a standing assignment covers a region (D-058, M6) |
| `CollegeTier` | `Power` (~50), `Major` (~80), `Small` (~100) | Prospect quality (F-03 §4.8): D-035's "130 major and 100 small" split into two major tiers so the very best programs stand out |

### 2.3 The blocklists (test-only)

| File | Shape | Guardrail |
|---|---|---|
| `football-teams.json` | `["Chargers", "Express", …]` | Every pro football nickname, past and present, from every league F-02 §4.3 lists |
| `major-league-teams.json` | `{ "new-york": ["Yankees", "Knicks", …], … }` | Current NBA, MLB, NHL, MLS, WNBA and NWSL names, by metro |
| `stadiums.json` | `["Lambeau Field", …]` | Current and former pro stadium names |
| `metro-nfl-colors.json` | `{ "new-york": [ { "team": "…", "colors": ["#…", "#…"] } ], … }` | Each metro's real NFL team colors (two teams in New York and Los Angeles) |
| `real-colleges.json` | `["…", …]` | Real institution names (D-035: "checked against real institution names"): every institution in the IPEDS directory with its short forms ("Kent State University" → "Kent State"), plus a hand-compiled list of nicknames and closed schools |

## 3. The code

### 3.1 The definition records (`Application/Leagues/TeamSet.cs`)

Plain data that mirrors the files. They aren't read models, so no `Dto` suffix (D-070 is for query results).

```csharp
public sealed record TeamSet(string Name,
    IReadOnlyList<ConferenceDefinition> Conferences, IReadOnlyList<TeamDefinition> Teams);
public sealed record ConferenceDefinition(string Key, string Name, IReadOnlyList<DivisionDefinition> Divisions);
public sealed record DivisionDefinition(string Key, string Name, IReadOnlyList<string> Teams);   // abbreviations
public sealed record TeamDefinition(string Abbr, string City, string Nickname, string Metro,
    TeamColors Colors, CrestDefinition Crest, StadiumDefinition Stadium);
public sealed record TeamColors(string Primary, string Secondary, string Accent);
public sealed record CrestDefinition(CrestShape Shape, string Monogram, CrestStripe Stripe);
public sealed record StadiumDefinition(string Name, int Capacity, Roof Roof, Surface Surface, ClimateZone Climate);

public sealed record CollegeList(string Name, IReadOnlyList<CollegeDefinition> Colleges);
public sealed record CollegeDefinition(string Name, CollegeRegion Region, CollegeTier Tier);
```

### 3.2 The port (`Application/Leagues/ISeedData.cs`)

```csharp
/// The built-in league data, loaded once (D-035). Generators read only this; nothing hard-codes a list.
public interface ISeedData
{
    TeamSet TeamSet { get; }
    CollegeList Colleges { get; }
}
```

### 3.3 The color checks (`tests/Infrastructure.Tests/Seed/ColorChecks.cs`, test-only)

A small `internal static` helper next to the guardrail tests, not production code: the guardrail tests on the shipped data are its only users. Imports (M11) don't need it, because the UI's `AccentColors` already corrects contrast for any team color at runtime. It works on `#RRGGBB` strings:

| Method | Formula |
|---|---|
| `double Contrast(string a, string b)` | WCAG 2: luminance per channel `c = v/255`, linear = `c ≤ 0.03928 ? c/12.92 : ((c+0.055)/1.055)^2.4` (the UI's threshold), `L = 0.2126 R + 0.7152 G + 0.0722 B`; contrast `(lighter + 0.05) / (darker + 0.05)` |
| `double OnColorContrast(string fill)` | The best of `Contrast(fill, "#FFFFFF")` and `Contrast(fill, "#0B0D12")`: the on-color the UI picks |
| `(double L, double A, double B) ToLab(string hex)` (private) | sRGB → linear (threshold **0.04045** here) → XYZ (D65: `X = 0.4124564 r + 0.3575761 g + 0.1804375 b`, `Y = 0.2126729 r + 0.7151522 g + 0.0721750 b`, `Z = 0.0193339 r + 0.1191920 g + 0.9503041 b`) → Lab with white `(0.95047, 1.0, 1.08883)` and `f(t) = t > (6/29)³ ? ∛t : t / (3·(6/29)²) + 4/29`; `L = 116 f(Y) − 16`, `a = 500 (f(X) − f(Y))`, `b = 200 (f(Y) − f(Z))` |
| `double DeltaE2000(string a, string b)` | CIEDE2000 on the two Labs; the steps are below |

**CIEDE2000** (Sharma, Wu and Dalal, 2005), angles in degrees:
```
C1 = √(a1²+b1²), C2 = √(a2²+b2²), C̄ = (C1+C2)/2,  G = 0.5·(1 − √(C̄⁷/(C̄⁷+25⁷)))
a1' = (1+G)·a1, a2' = (1+G)·a2,  C1' = √(a1'²+b1²), C2' = √(a2'²+b2²)
h1' = atan2(b1, a1') mod 360 (0 when a1' = b1 = 0), h2' likewise
ΔL' = L2−L1,  ΔC' = C2'−C1'
Δh' = 0 if C1'C2' = 0; else h2'−h1', brought into [−180, 180] by ±360
ΔH' = 2·√(C1'C2')·sin(Δh'/2)
L̄' = (L1+L2)/2,  C̄' = (C1'+C2')/2
h̄' = h1'+h2' if C1'C2' = 0; (h1'+h2')/2 if |h1'−h2'| ≤ 180; else (h1'+h2' ± 360)/2 (+ when the sum < 360)
T  = 1 − 0.17cos(h̄'−30) + 0.24cos(2h̄') + 0.32cos(3h̄'+6) − 0.20cos(4h̄'−63)
Δθ = 30·exp(−((h̄'−275)/25)²),  RC = 2·√(C̄'⁷/(C̄'⁷+25⁷))
SL = 1 + 0.015(L̄'−50)²/√(20+(L̄'−50)²),  SC = 1 + 0.045C̄',  SH = 1 + 0.015C̄'·T,  RT = −sin(2Δθ)·RC
ΔE = √((ΔL'/SL)² + (ΔC'/SC)² + (ΔH'/SH)² + RT·(ΔC'/SC)·(ΔH'/SH))
```
Expose a Lab-to-Lab overload too (`DeltaE2000((L, A, B) x, (L, A, B) y)`): the published test pairs are in Lab.

### 3.4 The loader (`Infrastructure/Seed/EmbeddedSeedData.cs`)

- **Embed the two files** in `GridironFrontOffice.Infrastructure.csproj`, with explicit names so lookups don't depend on folder separators:
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="..\..\data\league\teams.json" LogicalName="league/teams.json" />
    <EmbeddedResource Include="..\..\data\league\colleges.json" LogicalName="league/colleges.json" />
  </ItemGroup>
  ```
- **Read** with `typeof(EmbeddedSeedData).Assembly.GetManifestResourceStream("league/teams.json")`.
- **Parse strictly** with System.Text.Json, so a typo in the data (or later, an import) fails loudly instead of loading as a default:
  ```csharp
  new JsonSerializerOptions
  {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      RespectNullableAnnotations = true,              // a missing non-nullable field is an error
      RespectRequiredConstructorParameters = true,    // so is a missing constructor parameter
      UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,   // and an unknown field ("stripes")
      Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
  };
  ```
- **A field added later** (D-077) is a constructor parameter with a default (`string? Motto = null`): the two `Respect…` options only require parameters without one, so older files still load.
- **Load once:** two `Lazy<T>` fields, the class registered as a singleton (`services.AddSingleton<ISeedData, EmbeddedSeedData>()` in `AddGfoInfrastructure`). Loading takes well under 20 ms (F-02 §12); the log's `ElapsedMs` shows it, and no test asserts it (D-078).

### 3.5 Logging (`Infrastructure/Seed/SeedLog.cs`)

`[LoggerMessage]` methods, as in M0's `HostLog`:
- **1200**, Information: `Team set {Name} loaded: {TeamCount} teams in {ElapsedMs} ms` (F-02 §11's `TeamSetLoaded`).
- **1201**, Information: `College list {Name} loaded: {CollegeCount} colleges in {ElapsedMs} ms`.

Add both to LOGGING.md §8.2.

## 4. The guardrail tests (F-02 §4.3)

They load the shipped data through `EmbeddedSeedData` and read the blocklists from disk. A small `RepoFiles` helper finds the repository root the way Architecture.Tests' `RepoLayout` does (the folder holding `GridironFrontOffice.slnx`).

**Name matching** is exact after normalizing: trimmed, case-insensitive, accents removed (`Normalize(NormalizationForm.FormD)`, then drop non-spacing marks).

The team tests are in `TeamGuardrailTests`, the college tests in `CollegeGuardrailTests`. Names are sentences, like the rest of the repo's tests.

| Test | Rule | Threshold |
|---|---|---|
| `The_league_is_2_conferences_of_4_divisions_of_4_teams_and_every_team_is_in_exactly_one` | 2 × 4 × 4 (D-014); every team in exactly one division, and every division entry a real team | — |
| `Abbreviations_are_2_or_3_capital_letters_and_unique` | League-wide | — |
| `Colors_are_RRGGBB_and_monograms_are_1_to_3_characters` | Every team color, every monogram | — |
| `Teams_sharing_a_metro_play_in_different_conferences` | A metro key used by two teams puts them in different conferences | — |
| `Teams_sharing_a_city_label_differ_in_monogram_and_primary_color` | Their abbreviations already differ (unique league-wide) | primaries ΔE ≥ 20 |
| `No_nickname_is_a_pro_football_name` | No nickname matches `football-teams.json` | — |
| `No_nickname_is_a_major_league_name_in_its_own_metro` | No nickname matches its metro's list in `major-league-teams.json`; a metro missing from the list fails too | — |
| `No_team_echoes_the_colors_of_its_metros_NFL_team` | **An echo is both colors within the threshold of the real two, in either order**; a metro missing from `metro-nfl-colors.json` fails too | ΔE < 12 for both |
| `Primaries_in_a_division_are_clearly_different` | The four primaries in a division, pairwise | ΔE ≥ 20 |
| `Text_on_every_primary_is_readable` | Every primary's best on-color | ≥ 4.5:1 |
| `Crests_in_a_division_differ_in_shape_or_stripe` | No two teams in a division share shape + stripe | — |
| `No_stadium_is_a_real_stadium_name` | No stadium name matches `stadiums.json` | — |
| `College_names_are_unique` | After normalizing | — |
| `No_college_is_a_real_institution` | No college name matches `real-colleges.json` | — |
| `Every_region_has_every_tier_in_about_the_planned_mix` | Every region has colleges of every tier; about 50 / 80 / 100 by tier | ±10 per tier |

F-02's "Sensitive names" guardrail stays a review checklist item, not a test.

**Report every offender, not just the first:** collect the failures and assert the list is empty, so one test run names every team to fix. When the first run flags a name or a color, the fix is in the data (F-02 expects it), never a loosened threshold without a written reason.

## 5. Reference values (`ColorChecksTests`)

Just enough to prove the helper is right before the guardrails trust it. All were computed with an independent implementation.

**CIEDE2000, Lab inputs** (Sharma, Wu and Dalal, 2005; the second pair covers a zero-chroma color):

| Lab 1 | Lab 2 | ΔE₀₀ |
|---|---|---|
| (50, 2.6772, −79.7751) | (50, 0, −82.7485) | 2.0425 |
| (50, 0, 0) | (50, −1, 2) | 2.3669 |
| (50, 2.5, 0) | (73, 25, −18) | 27.1492 |
| (50, 2.5, 0) | (58, 24, 15) | 19.4535 |

**Hex → ΔE₀₀** (the full path through Lab): `#A6192E` vs `#1F3F7A` = 38.0003; `#FFFFFF` vs `#000000` = 100.0000.

**Contrast:** `#A6192E` vs `#FFFFFF` = 7.5003; `#B88A1B` vs `#0B0D12` = 6.1907 (so its on-color is near-black).

Compare doubles with a tolerance (`ShouldBe(expected, tolerance: 0.0001)`).

## 6. Done when

- [x] `ColorChecks` matches §5.
- [x] `EmbeddedSeedData` loads the shipped files: 32 teams and 230 colleges, logged with the time it took. The app registers one shared instance.
- [x] Every guardrail test in §4 passes against the shipped data, and each was **proven to fail**. The first draft's problems were planted back in (Phoenix's contrast, the Aviators, Empire Field, the Kansas City/Green Bay colors, a same-metro nickname, a shared crest, a shared monogram, a real and a duplicate college, a reversed Cowboys echo); each was caught by its own test, then removed.
- [x] Events 1200–1201 logged and listed in LOGGING.md; `./build.ps1` green: 381 tests, 31 of them new.
- [x] **Reviewed** (2026-10-21). What the review found and fixed:
  - **`ISeedData` wasn't registered** in `AddGfoInfrastructure`, so nothing could inject it, and nothing would have noticed until WP4. Registered; `The_app_registers_one_shared_instance` now guards it.
  - **Whitespace:** tabs and missing final newlines in every new production file (`.editorconfig` wants spaces). The build doesn't enforce it; `dotnet format whitespace` fixed it.
- **What was built:** `Core/Leagues/CollegeEnums.cs`; `Application/Leagues/` (`TeamSet.cs`, `ISeedData.cs`); `Infrastructure/Seed/` (`EmbeddedSeedData`, `SeedLog`), the two embedded files and the registration; `tests/Infrastructure.Tests/Seed/` (`EmbeddedSeedDataTests`, `ColorChecksTests`, `TeamGuardrailTests`, `CollegeGuardrailTests`, with the `RepoFiles` and `ColorChecks` helpers).
- **Changed from this guide:** Claude wrote the tests (owner, 2026-10-21) and applied the two review fixes at the owner's request.

## 7. Where the data stands

The data files are ready (2026-10-20), and every §4 guardrail passes against them in a reference run:
- `teams.json`: four nicknames and one stadium were already taken and eight primaries were too close or too dark. F-02's Appendix A lists every change and why.
- `colleges.json`: 230 programs (Power 50, Major 80, Small 100), every region holding every tier, no duplicate names.
- `real-colleges.json`: about 13,900 names. They come from all 6,072 institutions in the U.S. Department of Education's IPEDS directory (HD2024, public domain) with their aliases and sports-style short forms, plus a hand-compiled list for nicknames ("Ole Miss") and closed schools. No fictional college matches one exactly. The one near miss, Ridge State against Florida's Ridge Technical College, stays: it's D-035's own example, no real Ridge State exists, and the test's exact-match rule passes it.
- The guardrail tests confirm all of this (2026-10-21).
