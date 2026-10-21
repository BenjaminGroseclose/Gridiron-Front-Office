# M1 · The League Exists: the Plan

> **Status:** **Accepted** (owner answered §12 on 2026-10-20) · 2026-10-20 · Milestone definition: [ROADMAP.md §M1](../ROADMAP.md#m1--the-league-exists) · Branch: `main` (D-076) · Version: `0.1.x` ([D-072](../DECISIONS.md#d-072-versions-are-0milestonebuild-until-10))
>
> This is the detailed plan for M1. It turns the roadmap's scope into **eleven ordered work packages**, names what each one builds and how it's checked, and ends with the exit checklist and the questions only the owner can answer. **M1 is the first football:** a generated, believable league you can browse, with no games yet (those are M2 and M3).
>
> **Specs for M1:** [F-02 League & teams](../features/F-02_LEAGUE_AND_TEAMS.md) and [F-03 Players](../features/F-03_PLAYERS.md) are accepted. F-01's M1 slice (the New League wizard v0) and F-12's M1 slice (the contract structure and generation, D-074) get short specs in WP0.
>
> **Who builds it:** the owner writes the code. Claude writes a builder's guide per work package (files, signatures, the tests to pass, reference values), prepares the data files, and reviews each package before it's committed (§11). Start here: [M1_WP1_GUIDE.md](M1_WP1_GUIDE.md). Everything else is specified by [PERSISTENCE.md](../architecture/PERSISTENCE.md), [DATA_ACCESS_GUIDE.md](../architecture/DATA_ACCESS_GUIDE.md), [UI_DESIGN_SYSTEM.md](../architecture/UI_DESIGN_SYSTEM.md) and [VISUAL_DESIGN.md](../VISUAL_DESIGN.md).

---

## 1. What you'll have at the end of M1

1. **New League** asks for a name and a seed, generates a whole league in under 5 seconds with progress, then lets you pick your team from all 32 (crest, roster strength, cap space).
2. **A believable league:** 32 fictional teams in real metros with original names, colors, generated crests and stadiums; about 2,300 players with measurables, 42 ratings, hidden potential and personality, names, ages, colleges and hometowns, all from data files.
3. **Real contracts** (D-074): every player is mid-contract, with years, base salaries, a prorated signing bonus, guarantees and real cap hits, and every team sits under the first-season cap.
4. **The same seed always makes the same league** (a golden test), and a different seed makes a different, equally plausible one (distribution tests).
5. **Screens to browse it all:** a Home stub, the League page (both conferences), a Team page with its hero band and roster, the Roster grid (2,000 rows, sticky, sortable, keyboard), the Player page, and a peek sheet from every player name.
6. **The app wears your team's colors** once the league is open, readable in both modes.
7. **Ratings are fog-of-war ready:** every rating travels with an estimate _and_ an uncertainty. In M1 the uncertainty is always zero; M6 adds the range display without touching read models (D-078).
8. **Ctrl+K jumps** to any player or team by name.

## 2. Decisions M1 builds on

| Decision                                        | What it means for M1                                                                                      |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| **D-012** (xoshiro256\*\*, accepted 2026-10-20) | Every random draw in generation comes from it, with derived seeds                                         |
| **D-073**                                       | The first season is the real-world year at creation; seed + year → the same league                        |
| **D-074**                                       | Real contract data in M1; contract actions in M4                                                          |
| **D-075**                                       | No color-blind tuning until the pre-release audit                                                         |
| D-014                                           | Fixed 2 × 4 × 4 structure, 53 + 16 rosters                                                                |
| D-015                                           | Integer keys with strongly typed IDs: the first real ones (`TeamID`, `PlayerID`, …) arrive here           |
| D-032, D-034                                    | Ratings shown 0–99 with ranges; potential as a letter grade; `IPerception` from day one (σ = 0)           |
| D-033, D-035, D-064                             | The curated 32 teams, fictional colleges, both as data files with published schemas                       |
| D-043, D-050, D-065                             | Six personality traits, 42 attributes each with a sim consumer, 80% ranges, roster mix as league averages |
| D-049                                           | M1–M4 leagues start at Training Camp (the offseason start arrives in M5)                                  |
| D-070                                           | Read models are `sealed record …Dto` next to their query services                                         |
| D-077                                           | Data files carry no version number and only grow: a field added later is optional                         |
| D-078                                           | Fifteen simplifications: each piece arrives with its first consumer (see the WPs)                         |
| D-071                                           | No screen-reader work; keyboard, contrast and color-blind safety stay                                     |
| D-072                                           | This milestone is `0.1.x`                                                                                 |

## 3. Work packages at a glance

| WP   | Name                               | Size | Needs    | Done when                                                                                               |
| ---- | ---------------------------------- | ---- | -------- | ------------------------------------------------------------------------------------------------------- |
| WP0  | Decisions and docs prep            | S    | —        | §12 answered ✅; F-01 and F-12 M1 specs written                                                         |
| WP1  | Core: IDs, positions and the PRNG  | M    | WP0      | ✅ Done 2026-10-20                                                                                      |
| WP2  | Team and college data              | M    | WP1      | ✅ Done 2026-10-21                                                                                       |
| WP3  | Player data                        | M–L  | WP1      | Positions, OVR weights, archetypes, distributions, traits, jersey rules, names, hometowns, blocklists   |
| WP4  | The player model and generator     | L    | WP2, WP3 | Distribution, golden, blocklist and contract tests green over many seeds                                |
| WP5  | Schema and `CreateLeague`          | M    | WP4      | A generated league is written in one command in < 5 s, with progress                                    |
| WP6  | Perception and read models         | M    | WP5      | `IPerception` (σ = 0), the DTOs and query services, `GameStateStore` v0                                 |
| WP7  | Football components                | M    | WP6      | `TeamMark`, `PositionPill`, `RatingChip`/`RatingBar`, the grade chip, money, in the gallery, both modes |
| WP8  | New League wizard v0 and team tint | M    | WP5–WP7  | Name + seed → generate → pick a team → the app in your colors                                           |
| WP9  | The screens                        | L    | WP6, WP7 | Home, League, Team, Roster (DataGrid v1), Player, the peek sheet                                        |
| WP10 | Command palette v0 and the M1 exit | S–M  | WP9      | Ctrl+K works; the exit checklist (§10) passes; tag `m1-complete`                                        |

## 4. The work packages in detail

### WP0 · Decisions and docs prep (S)

**Goal:** settle what only you can decide, and write the one missing spec.

- ✅ Your answers to §12 (2026-10-20): D-012 accepted, D-073, D-074, D-075, the route change, the data downloads, saved views in M3.
- **F-12's M1 spec** (`docs/features/F-12_CONTRACTS.md`, the M1 slice; Claude drafts, you review): the contract structure, the cap-hit formula, the first-season cap, salary bands by position and tier, contract length by age and tier, bonus and guarantee shares, rookie-style deals, and how a team's total is kept under the cap. Checked against real references.
- **F-01's M1 spec** (`docs/features/F-01_LEAGUE_CREATION.md`, from the template; Claude drafts, you review): the wizard steps, what the team picker shows, progress, cancel, the error cases, and what the load screen's cards gain (crest, team, season, phase).
- **Route fix from M0:** F-02 puts the League page (all teams) at `/league`, which M0 used for the league home. The home becomes `/home`, the League page `/league` (UI_DESIGN_SYSTEM.md §7, `SaveActions.LeagueHome`).

### WP1 · Core: IDs, positions and the PRNG (M) ✅ Done 2026-10-20

**Goal:** the deterministic building blocks every later package uses. **Builder's guide:** [M1_WP1_GUIDE.md](M1_WP1_GUIDE.md).

- **PRNG** (Core, D-012): xoshiro256\*\* seeded through SplitMix64, with `Normal`, `Range` and weighted pick. **Derived seeds:** a stable 64-bit hash of `(leagueSeed, purpose, ids…)` written in-house (never `string.GetHashCode`, which changes between runs). A golden test pins the first values for fixed seeds, so an accidental algorithm change can't pass.
- **Typed IDs:** `TeamID`, `PlayerID`, `ConferenceID`, `DivisionID`, `StadiumID`, `CollegeID`, `ContractID`, on the M0 `IStronglyTypedID` convention with `UseAutoincrement()`.
- **The cap-hit formula (D-074):** a pure Core function in `long` dollars: the year's base salary plus the signing bonus prorated evenly over the first min(years, 5) seasons.
- **Enums and value types:** `Position` (16), `PlayerStatus`, `CrestShape`, `CrestStripe`, `Roof`, `Surface`, `ClimateZone`, `Attribute` (42), potential grades.
- **Tests:** the PRNG's distributions (mean and SD within tolerance), seed independence across purposes, the golden sequence.

**Results (2026-10-20):** the PRNG, seed derivation, typed IDs, enums and the cap-hit formula, with 42 Core tests; details and the review in [M1_WP1_GUIDE.md §6](M1_WP1_GUIDE.md#6-done-when). Changed from this plan: the typed IDs live in one file (`EntityIDs.cs`, one line each, owner's call), `Position.EDGE` is upper case so every name is its abbreviation, and the crest stripes are `Bar`/`TwinBars` (F-02 updated). Later, D-078 removes `StadiumID` (in WP5) and the correlated multivariate draws.

### WP2 · Team and college data (M) ✅ Done 2026-10-21

**Goal:** the league's identity, as data, with every guardrail proven. **Builder's guide:** [M1_WP2_GUIDE.md](M1_WP2_GUIDE.md).

- `data/league/teams.json` from F-02 Appendix A, and its public schema `teams.schema.json`. The data is copied into the save at creation, so a save stays self-contained. Data files carry no version number and only grow (D-077).
- `data/league/colleges.json`: **230 fictional** colleges (D-035), each with a **region** (`Northeast, Southeast, Midwest, Southwest, West`, for scouting coverage) and a **tier** (`Power` 50, `Major` 80, `Small` 100, for prospect quality), plus its schema.
- **Blocklists** (`data/blocklists/`, **test-only**, never in the app): pro football team names across every league F-02 §4.3 lists, current same-metro major-league names, real stadium names, each metro's real NFL colors for the ΔE check, and real college names (the IPEDS directory plus a hand-compiled list).
- **`ISeedData`** (an Application port, Infrastructure implementation): loads the two embedded files and parses them strictly. Tests check the shipped data's structure; a reusable validator arrives with M11's import (D-078).
- **Color checks** (a test helper, not production code): WCAG contrast and CIEDE2000, used only by the guardrail tests. Imported colors need no check at import time, because the UI's `AccentColors` already corrects contrast for any color at runtime.
- **Tests (F-02 §4.3, §13):** the 2 × 4 × 4 structure, unique abbreviations, shared metros split across conferences, the blocklists, no echo of the metro's NFL colors (both colors within ΔE 12), primaries ≥ ΔE 20 apart within a division, every primary's on-color at ≥ 4.5:1, distinct crests within a division, no real college names. _The data is ready and approved (2026-10-20, recorded under D-064): four names, one stadium and eight primaries changed to pass, listed in F-02 Appendix A._
- **Logging:** events 1200 (team set loaded) and 1201 (college list loaded).

**Results (2026-10-21):** the definition records and `ISeedData` (Application), `EmbeddedSeedData` and `SeedLog` (Infrastructure), and 31 tests: the loader (8), the color checks (8), the team guardrails (12) and the college guardrails (3). Each guardrail was proven to fail on a planted problem. Details and the review in [M1_WP2_GUIDE.md §6](M1_WP2_GUIDE.md#6-done-when). Changed from this plan: Claude writes the tests from here on (§11).

### WP3 · Player data (M–L)

**Goal:** every number the generator needs, as data with first-pass values. **Builder's guide:** [M1_WP3_GUIDE.md](M1_WP3_GUIDE.md).

- `data/players/positions.json`: roster averages, ranges and floors (F-03 §4.1); hand-set OVR weights (recalibrated by SimLab in M2); archetype profiles; age and measurable distributions by position.
- `traits.json` (the six D-043 traits), `generation.json` (the tier targets, F-03 §4.10), `jersey-rules.json` (the current NFL numbering rule, **checked against the real rule** when written).
- **Names and places** (approved 2026-10-20): `data/names/first.json`, `last.json` and `data/places/hometowns.json`, frequency-weighted from public-domain U.S. government data (SSA baby names, the Census surname list, the Census places gazetteer). Each download is named, with its source and size, for approval before it happens; the sources go in THIRD_PARTY_NOTICES.
- `data/contracts/contracts.json` (F-12's M1 slice): the first-season cap, minimum salaries by experience, salary bands by position and tier, length, bonus and guarantee shares, rookie-style deals by draft round. _Waits for F-12's M1 spec (WP0), which isn't written yet; it's WP3's last slice._
- `data/blocklists/famous-people.json` (F-03 §4.11): Hall of Fame members, notable players of the last ~25 seasons, and a short list of other very famous people.
- **Tests:** every file parses and validates; weights sum to 1; every attribute appears in at least one position's weights or archetypes.

### WP4 · The player model and generator (L)

**Goal:** a generator that makes believable, deterministic players.

- **Core (pure):** `OverallCalculator` (OVR by position from the weights, onto the 0–99 curve), the potential grade bands, archetype labels from the attribute profile.
- **Application (`Players/PlayerGenerator`):** position → body → athletic testing → physical ratings (with the ±3 "football speed vs. track speed" noise) → archetype → skills (the archetype's means, a shared quality draw by tier, and noise) → hidden ratings → personality → bio. Each player's seed is `Hash(leagueSeed, "gen", season, slot)`.
- **Contracts (D-074):** a contract per player from the bands: mid-contract at creation (signed in an earlier season, so proration started then), rookie-style deals for players drafted in the last four seasons, minimum deals for fringe players, and each team's total kept under the first-season cap.
- **League generation:** 32 rosters of 53 plus 16 on each practice squad, with the league-wide roster mix averaging to the targets and every floor kept; about 150 unsigned free agents; unique jersey numbers per team; names re-rolled on a blocklist hit or a same-team duplicate.
- **Tests (F-03 §13):** the OVR calculator per position, grade bands, measurable → physical mapping (monotonic, within the noise), archetypes; **distributions** over 20 seeds (tier means and SDs, ~15–25 players at 90+, mean age 26.5 ± 1, roster mix within ±0.3 of each average, floors kept); **golden** `GenerateLeague(seed: 42)` summary (Verify); the **blocklist re-roll** (a unit test); **contracts** (every team under the cap, cap hits from the formula, salaries shaped by position within tolerance). Generating ~2,300 players targets < 2 s (read from the log, not asserted).
- **Logging:** `LeagueGenerated` (1300–1329).

### WP5 · Schema and `CreateLeague` (M)

**Goal:** a generated league written safely into the save.

- **Migration:** `Conferences`, `Divisions`, `Teams` (the stadium as columns), `Colleges`, `Players` (wide: measurables, 42 attributes, hidden ratings, six personality traits), `Contracts` + `ContractYears` (D-074: type, signed season, total value, signing bonus, guarantees, and per year the base salary, prorated bonus, guaranteed amount and cap hit), and `SaveInfo`'s league position (`UserTeamID`, `CurrentSeason`, `CurrentPhase`, `CurrentDate`, `Seed`). `StadiumID` leaves `EntityIDs.cs` (D-078).
- **`CreateLeague`:** generation runs **outside** the write transaction; then one command bulk-inserts everything through `uow.CreateCommand` (EF change tracking costs ~100 µs per entity; this is ~2,300 players). Either the whole league exists or none of it does. Progress is reported per step; the league starts at Training Camp.
- **Typed-ID keys need opting in to `AUTOINCREMENT`** (found 2026-10-20): EF treats a key with a value converter as _not generated_, so `TypedIDs.UseAutoincrementForGeneratedKeys` skips it unless the entity says `.ValueGeneratedOnAdd()` (M0's convention test does). Either mark every typed-ID key generated in one place (preferred: extend the convention so no entity can forget), or have the bulk insert assign IDs itself; rows added later (contracts in M4, prospects in M5) still need `AUTOINCREMENT`.
- **The user's team:** picking a team after generation is a second, small command (`SaveInfo.UserTeamID`, the only place the user's team is stored).
- **Tests:** the bulk insert round-trips every column; a failure mid-write leaves no partial league; the torture tests stay green. Generate + write is timed in the log; the < 5 s budget is checked at the exit (§10).

### WP6 · Perception and read models (M)

**Goal:** what screens read, through the fog-of-war boundary.

- **`IPerception`** (Application): `Perceive(viewer, player)` → estimates with sigmas. σ = 0 for everyone until M6. The sim never calls it.
- **Read models** (D-070): `TeamIdentityDto`, `TeamSummaryDto`, `LeagueStructureDto`, `PlayerRowDto`, `PlayerDetailDto`, each rating an estimate + sigma, potential a grade + spread.
- **Query services:** `LeagueStructureQueries` (identities cached per session), `PlayerQueries` (`GetRoster`, `GetPlayer`, `SearchPlayers`), always with the user's team as the viewer.
- **`GameStateStore` v0** (UI): the user team summary, season, phase and date, loaded when the save opens. Refreshing from domain events arrives with the moving calendar (M3, D-078).
- **Tests:** own roster exact, σ = 0 everywhere.

### WP7 · Football components (M)

**Goal:** the pieces every football screen is built from.

- **`TeamMark`:** the parametric SVG crest (5 shapes × 5 stripes, monogram in condensed Archivo 800), readable at 16 px, with an accessible name and the on-color fallback when the accent fails contrast.
- **`PositionPill`, `RatingChip`, `RatingBar`:** a plain value (σ = 0 until M6, which adds the range display, D-078); the potential grade chip.
- **`Money`:** compact dollar display ($24.5M, $850K) for grids and contract panels.
- **Gallery:** `/_gallery/teams` (all 32 crests at 16, 32 and 96 px, both modes) and a ratings page (every tier).
- **Tests:** bUnit for every shape and stripe, the contrast fallback, accessible names, the chips.

### WP8 · New League wizard v0 and team tint (M)

**Goal:** start a real league.

- **The wizard** replaces M0's name-only page: a name and an optional seed (random by default) → **Generate**, with progress → **pick a team** from 32 cards (crest, name, roster strength, cap space) → open the league at Home.
- **Team tint:** opening a league re-themes the app from the team's primary through `GfoThemeService` (the M0 plumbing; `AppearanceMapping` gets the team color), contrast-adjusted, in both modes.
- **The load screen and Continue** gain the crest, team, season and phase (PERSISTENCE.md §9).
- **AI counterpart:** none; every team is generated the same way.
- **Tests:** bUnit for each wizard step and the error cases; an end-to-end create → pick → home in the desktop app.

### WP9 · The screens (L)

**Goal:** browse every team and player.

- **Home** (`/home`): a stub dashboard (your team's identity, record placeholder, top players).
- **League** (`/league`): both conferences, four division cards each, every team a link (F-02 §8).
- **Team** (`/team/{id}`): the hero band in the team color with the ghost monogram, crest, stadium, its roster, and its cap totals (cap, committed, space).
- **Roster** (DataGrid v1): virtualized, sticky header, pinned name column, sorting, the M0 keyboard map, Enter opens the peek. Name, position, age, OVR chip, potential grade, archetype, cap hit.
- **Player** (`/player/{id}`): the header, attributes grouped as in F-03 §4.2 with `RatingBar`, measurables, bio, personality descriptors, and the contract panel (each year's base, bonus, guarantee and cap hit).
- **`PlayerPeekSheet`** and `PeekService.OpenPlayer(id)`: **every player name is a link that opens it**; "Open full profile" navigates.
- **The nav** grows: Home, Roster, League (team pages from links), Settings.
- **Tests:** bUnit per screen; the 2,000-row grid sorts and scrolls under budget (Spike S0b's numbers as the bar).

### WP10 · Command palette v0 and the M1 exit (S–M)

**Goal:** jump anywhere, then prove the milestone.

- **Command palette v0** (`Ctrl+K`): a Fluent dialog with a search box and results; players and teams by name; Enter navigates. The command registry comes later (P1).
- **Run the exit checklist** (§10), record the generation time, update the docs, tag **`m1-complete`**, set `VersionPrefix` to `0.2.0`.

## 5. What the repository gains in M1

```
data/
├─ league/      teams.json, teams.schema.json, colleges.json, colleges.schema.json
├─ players/     positions.json, traits.json, generation.json, jersey-rules.json
├─ names/       first.json, last.json
├─ places/      hometowns.json
└─ blocklists/  football-teams.json, major-league-teams.json, stadiums.json, metro-nfl-colors.json, real-colleges.json,
                famous-people.json (all test-only except famous-people, which the name generator needs)
src/
├─ Core/        IDs/ (TeamID, PlayerID, …), Random/ (PRNG, seed derivation), Players/ (Position, Attribute, OverallCalculator, grades, archetypes)
├─ Application/ Leagues/ (CreateLeague, LeagueStructureQueries, ISeedData), Players/ (PlayerGenerator, IPerception, PlayerQueries)
├─ Infrastructure/ Data/ (the new entities and migration), Seed/ (EmbeddedSeedData, SeedLog)
├─ UI/          Football/ (TeamMark, PositionPill, RatingChip, RatingBar), League/ (Home, League, Team, Roster, Player, PlayerPeekSheet),
│               Menu/ (the wizard), Shell/ (GameStateStore, CommandPalette)
└─ DevHost/     Gallery/ (teams, ratings)
```

## 6. Packages

No new packages are planned. JSON is read with System.Text.Json and validated in code; ΔE and contrast are small formulas written in-house, in the tests; the PRNG and the seed hash are in-house by design (D-012). If one becomes necessary, its license is checked and recorded in THIRD_PARTY_NOTICES.md before it's added.

## 7. Docs that change during M1

- **New:** F-01's M1 spec, this plan's results as each WP lands.
- **Updated:** DECISIONS (D-012 accepted, plus any decision from §12), PERSISTENCE §5 (the as-built schema), UI_DESIGN_SYSTEM (the new components and screens, routes), VISUAL_DESIGN (crests, rating colors), LOGGING (events 1200–1219, 1300–1329), THIRD_PARTY_NOTICES (the data sources and their licenses), ROADMAP and CLAUDE.md at the end.

## 8. Not in M1

- **Games and the sim** (M2), the schedule, standings and the calendar advancing (M3).
- **The depth chart** (F-04's editing, M3); the roster here is a view.
- **Contract actions and cap rules** (M4): signing, releases and dead money, tags, extensions, Top 51, the cap sheet. M1's contracts are real data you can read, not yet change (D-074).
- **Fog-of-war data** (σ > 0, M6), scouting, draft classes (M5).
- **Saved views** in the grid (M3, with the stats screens), user import of team sets and colleges (M11).
- **Color-blind re-tuning** of the rating colors (the pre-release audit, D-075).
- **Spike S1** (Draft Room feel) runs after M1, on M1's generated prospects; it isn't part of this milestone.

## 9. Risks and fallbacks

| Risk                                          | Fallback                                                                                          |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| The generator misses its distribution targets | Tune the data (that's why targets live in files); widen tolerances only with a written reason     |
| A guardrail test rejects a team name or color | Swap it in review, as F-02 expects; record the change in F-02                                     |
| League creation over 5 s                      | The bulk path is already measured in Spike S0a; generation is pure and can be profiled on its own |
| The 2,000-row grid misses its budget          | Spike S0b passed with 2,000 × 25; fall back to fewer default columns                              |
| Name or place data licensing                  | Only public-domain U.S. government sources; verified and recorded before the files are added      |

## 10. M1 exit checklist

From [ROADMAP.md §M1](../ROADMAP.md#m1--the-league-exists) and the specs' test plans:

- [ ] Same seed → identical league (golden test). A different seed → a different, equally plausible league.
- [ ] Generating a new league takes < 5 s, with progress (Release build).
- [ ] Every attribute has a planned consumer in the attribute → resolver matrix (D-050); the enforcing test lands with the engine in M2.
- [ ] Rating distributions per position match their targets (a test), and the roster mix averages hold with every floor kept.
- [ ] Every F-02 guardrail test passes against the shipped team data.
- [ ] Every player has a real contract, every team is under the first-season cap, and cap hits match the formula.
- [ ] A blocklisted name is always re-rolled (a unit test).
- [ ] Sorting and scrolling a 2,000-player grid stays under budget.
- [ ] Every player name is a link that opens a peek sheet.
- [ ] The app wears the user team's colors in both modes, with readable text.
- [ ] `./build.ps1` is green; THIRD_PARTY_NOTICES lists every data source.
- [ ] Tag `m1-complete`, set `VersionPrefix` to `0.2.0`.

## 11. How M1 lands

On `main` (D-076): a commit per work package (or per meaningful slice of a large one), `./build.ps1` before every commit, and the `m1-complete` tag when §10 passes.

**The owner writes the production code** (2026-10-20), **and Claude writes the tests** (2026-10-21). For each work package:

1. **Claude writes its builder's guide** (`M1_WPn_GUIDE.md`): what to build and where, the signatures, the tests that define "done", reference values, and the gotchas from earlier milestones.
2. **Claude prepares the package's data files** (team and college JSON, blocklists, the downloaded name and place lists after approval), so the code has real inputs.
3. **You build the production code.** Claude writes the tests the guide specifies, and proves each one can fail.
4. **Ask for a review:** Claude runs `./build.ps1`, reads the diff against the guide and the specs, and reports what to fix before you commit.

## 12. Questions for the owner (answered 2026-10-20)

1. ~~The PRNG~~ → **xoshiro256\*\*** ([D-012](../DECISIONS.md#d-012-deterministic-simulation-with-an-in-house-prng), accepted).
2. ~~The first season's year~~ → **the real-world year at creation**, 2026 today ([D-073](../DECISIONS.md#d-073-a-leagues-first-season-is-the-real-world-year-its-created-in)).
3. ~~The route change~~ → **yes**: the league home moves to `/home`, and `/league` is the League page.
4. ~~Name and place data~~ → **yes**, public-domain U.S. government lists trimmed into JSON files; each download is approved before it happens.
5. ~~M1's contracts~~ → **real contract data now, the actions in M4** ([D-074](../DECISIONS.md#d-074-contracts-are-real-from-m1-contract-actions-arrive-in-m4)).
6. ~~Saved views~~ → **M3**, with the stats screens.
7. ~~Light-mode rating colors~~ → **not now**; reviewed before any public or paid release ([D-075](../DECISIONS.md#d-075-color-blind-tuning-is-reviewed-before-a-public-release)).
