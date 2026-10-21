# F-03: Players: Model & Generation

> **Status:** Accepted (owner review 2026-10-15, [D-065](../DECISIONS.md#d-065-f-03-player-model-accepted-42-attributes-80-ranges-roster-mix-as-averages)) · **Milestone:** M1 (model + generation), M5 (yearly prospect classes), M6 (fog-of-war data) · **Owner:** Ben · **Last updated:** 2026-10-15
>
> Decisions this spec implements: [D-032](../DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted) (fog of war), [D-034](../DECISIONS.md#d-034-ratings-display-099-with-ranges-letter-grades-for-potential) (display scale), [D-035](../DECISIONS.md#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11) (data files), [D-043](../DECISIONS.md#d-043-limited-personality-model-only-traits-with-a-consumer) (personality), [D-049](../DECISIONS.md#d-049-a-new-league-starts-at-the-offseason-the-previous-gm-was-fired) (start point), [D-050](../DECISIONS.md#d-050-every-player-attribute-must-be-read-by-the-sim) (every attribute has a consumer).

---

## 1. Summary

Players are the game's raw material: believable, distinct people whose ratings mean something on Sunday and whose careers you remember. This spec defines the **player model** (positions, attributes, measurables, hidden ratings, personality, bio), the **overall (OVR) and potential** formulas, **how a team perceives a player** (the fog-of-war boundary), and the **generator** that builds a league's players and, from M5, each year's draft class. Serves pillars **3 (believable and explainable)** and **5 (your league, your story)**.

## 2. Goals / non-goals

**Goals (M1)**
- The final attribute list, every attribute read by the sim (§4.2).
- Measurables generated first, physical ratings derived from them (§4.3).
- OVR by position from data-driven weights. Potential as a hidden peak-OVR target shown as a letter grade (§4.5–4.6).
- Personality traits per D-043, shown as coarse descriptors (§4.7).
- The **perception boundary**: every player read model is computed *for a viewing team* (§4.9). In M1 it returns exact values (σ = 0) for everyone.
- A deterministic generator that hits per-position distribution targets (§4.10), with names checked against a blocklist (§4.11).

**Non-goals**
- Fog-of-war *data* (σ > 0): M6 (F-17).
- Progression, aging, retirement: M5 (F-20). Expected aging curves come from F-13 in M4.
- Yearly draft classes: M5. The generator API exists in M1, but it's first used in M5.
- Contract actions and cap rules: M4 (F-12). M1 generates real contract data for every player ([D-074](../DECISIONS.md#d-074-contracts-are-real-from-m1-contract-actions-arrive-in-m4)).
- Position changes, jersey number management UI (later).

## 3. Player stories
- As the GM, I want every number on a player page to matter in games, so I can trust what I'm reading.
- As the GM, I want players to look and test like real athletes (a 4.38 40-yard dash means fast), so the combine is meaningful.
- As the GM, I want my own players' current ratings shown exactly and other teams' players shown with an honest range, so I know what I know.
- As the GM, I want a player's potential to stay a judgment call (a letter grade), never a number I can min-max.

## 4. Rules and formulas

### 4.1 Positions

| Position | League average (of 53) | Typical range per team | Floor | Depth chart roles (derived packages, [D-052](../DECISIONS.md#d-052-depth-chart-position-depth-drives-every-package-with-optional-overrides)) |
|---|---|---|---|---|
| QB | 3 | 2–4 | 2 | QB |
| RB | 3 | 2–4 | 2 | RB, KR/PR candidates |
| FB | 1 | 0–2 | 0 | FB (21/22 personnel), lead blocker. Teams without one use a TE or RB. |
| WR | 6 | 5–7 | 4 | WR1–WR3 by package, slot |
| TE | 3 | 2–4 | 2 | TE1–TE2 (12/22 personnel), goal line |
| OT | 4 | 3–5 | 3 | LT, RT |
| OG | 4 | 3–5 | 3 | LG, RG |
| C | 2 | 1–3 | 1 | C (a guard can fill in, since blocking ratings carry over) |
| EDGE | 5 | 4–6 | 3 | DE (4-3), OLB (3-4) |
| DT | 4 | 3–5 | 3 | DT, NT |
| LB | 5 | 4–6 | 3 | MLB, WLB, SLB |
| CB | 6 | 5–7 | 4 | CB1–CB2, nickel and dime slots |
| S | 4 | 3–5 | 3 | FS, SS |
| K | 1 | 1 | 1 | K |
| P | 1 | 1 | 1 | P |
| LS | 1 | 1 | 1 | LS |

- **These are league averages, not rules** (owner, 2026-10-15: *"don't make them strict some team might have more or less but that should be the average"*). Generation draws each team's mix within the typical ranges so the **league-wide average** hits the targets and every roster totals exactly 53. Practice squads (16) vary the same way.
- **Floors** are what generation and the AI always keep so every package can be fielded. The only *hard* rule is the F-05 blocker that every required depth chart slot is filled ([CALENDAR_AND_PHASES.md §4](../architecture/CALENDAR_AND_PHASES.md#4-blocking-issues-and-warnings)).
- **The AI** (F-21) treats the averages as soft targets, shifted by its needs, and later by GM personality (M7) and scheme (M10). Two AI teams won't carry identical rosters.
- **Test:** `Generation_RosterMix_LeagueAverageWithinTolerance` (league means within ±0.3 per position over 20 seeds) and `Generation_RosterMix_RespectsFloors`.

Scheme roles (3-4 OLB, nickel CB, FS/SS) are depth chart roles, not positions.

### 4.2 Attributes and their consumers (D-050)

**42 attributes**, internal 0–100 floats. The stage references point to [SIMULATION_ENGINE.md §4](../architecture/SIMULATION_ENGINE.md#4-play-resolution) (Pass 1–6, Run 1–4).

| Group | Attribute | Read by | Matters most for |
|---|---|---|---|
| Physical | **Speed** | Pass 2 coverage separation, Pass 6 after the catch, Run 3 big play, pursuit, return and coverage units | WR, RB, CB, S, LB |
| | **Acceleration** | Run 2 yards before contact, Pass 1 rush get-off, Pass 2 closing on the ball | RB, EDGE, CB |
| | **Agility** | Pass 2 change of direction in man coverage, elusiveness checks in Pass 6 and Run 3 | WR, CB, RB, S |
| | **Strength** | Pass 1 anchor vs. power rush, Run 1 block vs. shed, Run 2 ball-carrier power | OL, DL, RB, TE |
| | **Stamina** | Fatigue model (engine §4.6) | All (DL and RB most) |
| | **Toughness** | Injury hazard (engine §4.5) | All |
| Mental | **Awareness** | Pass 3 finding the open receiver, Pass 2 zone coverage, Run 1 run fits, pursuit angles | QB, LB, S, CB |
| | **Discipline** | Penalty hazard (engine §4.4) | All |
| Passing | **Arm Strength** | Pass 4 deep throws | QB |
| | **Short / Medium / Deep Accuracy** | Pass 4 ball placement by depth | QB |
| | **Throw on the Run** | Pass 4 throws under pressure or on a scramble | QB |
| | **Pocket Presence** | Pass 3 time in the pocket, sack avoidance | QB |
| | **Decision Making** | Pass 3 throwaway vs. force, interception risk | QB |
| Ball carrying | **Ball Security** | Run 4 and Pass 6 fumble chance | RB, WR, TE, QB |
| | **Vision** | Run 2 finding the cutback | RB |
| | **Elusiveness** | Pass 6 after the catch, Run 3 big play, returns | RB, WR, returners |
| | **Break Tackle** | Run 2 yards after contact, Pass 6 broken tackles | RB, TE, WR |
| Receiving | **Hands** | Pass 5 drops | WR, TE, RB |
| | **Route Running** | Pass 2 separation | WR, TE |
| | **Release** | Pass 2 beating press coverage | WR, TE |
| | **Contested Catch** | Pass 5 contested catches (absorbs the old *Jumping*) | WR, TE |
| Blocking | **Pass Block** | Pass 1 protection | OL, TE, RB |
| | **Run Block** | Run 1 point of attack | OL, TE |
| | **Lead Block** | Run 1 lead and pull blocks | FB, TE, OG |
| Defense | **Pass Rush (Power)**, **Pass Rush (Finesse)** | Pass 1 protection | EDGE, DT, blitzing LB |
| | **Block Shedding** | Run 1 point of attack | DL, LB |
| | **Tackling** | Contact resolution in Run 2 and Pass 6 | All defenders |
| | **Pursuit** | Run 3 big play, Pass 6 after the catch | LB, S, DL |
| | **Hit Power** | Forced fumbles in Run 4 and Pass 6 (resolves the old "strip skill") | LB, S, DL |
| | **Man Coverage**, **Zone Coverage** | Pass 2 coverage, by the defense's call | CB, S, LB |
| | **Press** | Pass 2 jam vs. release | CB |
| | **Ball Skills** | Pass 5 interceptions and deflections (absorbs the old *Jumping*) | CB, S |
| Special teams | **Kick Power**, **Kick Accuracy** | Kickoffs, field goals, PATs | K |
| | **Punt Power**, **Punt Accuracy** | Punts | P |
| | **Return** | Kick and punt returns | Returners |
| | **Snap Accuracy** | Bad-snap chance | LS |

**Changes from the starting list in FEATURES.md:** *Jumping* is folded into Contested Catch and Ball Skills (the vertical and broad jump still feed their generation, §4.3). *Play Recognition* is folded into Awareness. *Agility* and *Strength* are wired in as shown. "Strip skill" in the run pipeline becomes Hit Power. **Test (from M2):** `Attributes_EveryAttribute_HasTuningConsumer` fails if any attribute isn't referenced in `SimTuning.json`.

### 4.3 Measurables first, physical ratings derived

Generation order for every player: **position → body (height, weight) → athletic testing → physical ratings**.

| Measurable | Drives | Notes |
|---|---|---|
| 40-yard dash | **Speed** | Position-specific distributions (e.g., CB mean 4.48, OT 5.25) |
| 10-yard split | **Acceleration** | Correlated with the 40 |
| 3-cone, short shuttle | **Agility** | |
| Bench press reps | **Strength** | |
| Vertical, broad jump | Priors for Contested Catch, Ball Skills, Acceleration | Not ratings themselves. Deferred: stored, not read in M1 (D-079) |
| Arm length | Priors for Pass Block, Press, Pass Rush | Deferred (D-079) |
| Hand size | Priors for Hands, Ball Security (QBs: fumbles) | Deferred (D-079) |
| Height, weight | Priors across positions (e.g., Break Tackle, Strength) | Deferred (D-079) |

**M1 ([D-079](../DECISIONS.md#d-079-wp3-player-data-traits-in-generationjson-measurable-priors-deferred-f-12-before-contracts)):** only the first four rows become ratings, each through a straight line between a best and a worst value in `data/players/generation.json`. The priors return as a field added there if M2's SimLab shows players feel samey.

- `Physical = f(measurable) + hiddenNoise`, with `hiddenNoise ~ N(0, 3)` rating points ("football speed vs. track speed"). That's why the combine makes physical ratings **near-exact** (±3) rather than exact. It falls out of the model with no special case.
- Stamina and Toughness don't come from measurables.
- Measurables are stored. For prospects, the combine (or a pro day) **reveals** them exactly (F-17). For your own players they're always known.

### 4.4 Skill attributes
- Each position has a few **archetype profiles** in data (§4.8) that shape which skills are high or low. Generation samples an archetype, then draws each skill as the archetype's mean for it, plus one **shared quality draw** for the player conditioned on the target **tier** (starter, backup, fringe, §4.10), plus independent noise. The shared draw makes a player's skills rise and fall together; the archetype gives the pattern (a route runner's Route Running and Release are high together). No correlation matrices ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).
- Off-position skills get low baselines (a CB's Pass Block barely matters, and the sim rarely asks for it).

### 4.5 Overall (OVR)
- `OVR(position) = scale(Σ wᵢ · attributeᵢ)`. Weights per position live in `data/players/positions.json` (they sum to 1), and `scale` maps the weighted sum onto the 0–99 display curve so the tier targets hold (§4.10).
- **Weights are calibrated to the sim in M2:** SimLab regresses each position's on-field contribution on its attributes, and the weights follow that regression. OVR is an honest summary of what the engine rewards (pillar 3), not a guess. *Starting weights are hand-set for M1.*
- **Archetype label:** recomputed from the attribute profile (not stored at generation), so it stays true after development. Examples: QB Pocket Passer / Scrambler, WR Deep Threat / Possession / Slot, EDGE Speed Rusher / Power Rusher, CB Man / Zone / Slot.
- Display: 0–99 integers with color bands ([D-034](../DECISIONS.md#d-034-ratings-display-099-with-ranges-letter-grades-for-potential), [VISUAL_DESIGN.md](../VISUAL_DESIGN.md)).

### 4.6 Hidden ratings and potential

| Hidden rating | Meaning | Consumer |
|---|---|---|
| **Potential** | A **peak-OVR target** (float), at least current OVR for young players | F-20 progression, valuation (perceived), display as a grade |
| **Development curve** | Early / Normal / Late bloomer, plus a bust-risk probability | F-20 |
| **Peak age offset** | Shifts the position's expected peak age (F-13's aging curves) | F-20, F-13 |
| **Injury proneness** | Multiplier on the injury hazard | Sim (§4.5 of the engine), F-11 |

**Potential is shown only as a letter grade** (never a number), from bands in data. Starting bands (tuned with the distribution tests):

| Grade | A+ | A | A− | B+ | B | B− | C+ | C | C− | D | F |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Peak OVR | ≥ 92 | 88–91 | 85–87 | 82–84 | 78–81 | 75–77 | 72–74 | 68–71 | 64–67 | 58–63 | < 58 |

With uncertainty, a grade shows its likely spread ("Ceiling: B+, likely B to A−"). Potential stays uncertain for **every** team, your own roster included. On your roster it narrows with each season you watch the player develop ([D-032](../DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted)).

### 4.7 Personality (D-043)

| Trait | Consumer |
|---|---|
| Work ethic | Development (F-20) |
| Coachability | Development with coaching (F-20, M10) |
| Greed | Contract asks (F-14) |
| Loyalty | Hometown and own-team discount (F-14) |
| Competitiveness | Free agency contender priority, retirement (F-15 M9, F-20) |
| Ego | Role demands (F-14/F-15 M9) |

- Values are hidden 0–100 floats. **Shown as coarse descriptors:** Low (< 35), Average, High (> 65). Visibility follows D-032: your roster from acquisition, prospects through interviews, others only through reputation tags.
- Traits are six columns on `Players`, like the attributes ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)). Their values come from one distribution in `data/players/generation.json`, which also holds the Low/High thresholds ([D-079](../DECISIONS.md#d-079-wp3-player-data-traits-in-generationjson-measurable-priors-deferred-f-12-before-contracts)). Adding one later is a column and a deterministic backfill migration.

### 4.8 Bio

| Field | Rule |
|---|---|
| **Name** | First and last names drawn from frequency-weighted lists (candidate sources: SSA baby-name data and the Census surname list, both U.S. government public domain; license verified at adoption). No demographic modeling. The full name is checked against the blocklist (§4.11). No two active players on the same team share a full name. |
| **Birthdate, age** | `DateOnly`. Ages by position from data (e.g., RB mean 25, QB 28, K/P 30). Rookies are 21–23. |
| **College** | From `colleges.json` ([D-035](../DECISIONS.md#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). The college's **tier** shapes the prospect quality distribution, and its **region** drives scouting coverage ([D-058](../DECISIONS.md#d-058-scouting-department-standing--special-assignments-no-biases-in-10)). |
| **Hometown** | A real U.S. place, weighted by population (public data). |
| **Experience** | Accrued seasons (the definition lives in F-12). |
| **Jersey number** | Per the current NFL numbering rules by position, encoded in `data/players/jersey-rules.json` and **verified against the real rule when the data file is written** ([D-056](../DECISIONS.md#d-056-the-offseason-top-51-cap-rule-is-in-amends-d-040), league-rules default). Unique within a team. |
| **Draft info** | Season, round, pick and team. Undrafted players are flagged. |

### 4.9 Perception: the fog-of-war boundary

Every rating a screen shows passes through one Application service:

```csharp
public interface IPerception
{
    // The viewer is always a team: the user's team for screens, each AI team for its own decisions.
    PerceivedPlayer Perceive(TeamID viewer, PlayerSnapshot player);
}

public readonly record struct PerceivedRating(float Estimate, float Sigma);  // Sigma = 0 means exact
```

- **Rules (D-032):** the viewer's own players → exact current ratings. Other teams' players and free agents → a fixed fuzz. Prospects → the viewer's scouting state (F-17).
- **Error model (numbers owned by F-17):** `error = σ_player · z_player + σ_attr · z_attr`. A shared player-level term (a team misjudges the *whole* player) plus a small attribute-level term. The shared term dominates, so the OVR range behaves like the "about ±3" in D-032 instead of averaging away. All `z` values come from `Hash(leagueSeed, "perception", viewer, player, season)`, redrawn once per league year.
- **Displayed range** = an **80% interval** (estimate ± 1.28σ), so the truth falls outside the range about 1 time in 5. That's what makes surprises and busts honest.
- **OVR range** = the OVR formula over perceived attributes, with σ propagated through the weights.
- **M1–M5:** `IPerception` returns σ = 0 for everything, but **every read model already carries an estimate *and* a sigma**, so M6 changes data and adds the range display without touching read models ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).
- The sim never calls `IPerception`. It always uses true ratings.

### 4.10 Generation targets (initial values, tuned by tests)

| Target | Initial value |
|---|---|
| Players per team at creation (M1–M4, Training Camp start) | 53 active + 16 practice squad |
| Unsigned free agents at creation | ~150 (injury replacements) |
| Starter OVR | Mean ~74, SD ~7, by position |
| Backup OVR | Mean ~64, SD ~6 |
| Fringe / practice squad OVR | Mean ~56, SD ~5 |
| 90+ OVR players league-wide | ~15–25 (the baseline the M5 health gate holds within ±25%) |
| Mean age, league-wide | 26.5 (the health band is 25.5–27.5) |
| **Yearly draft class (M5)** | ~300 prospects, each with college production stats generated from true ratings + noise + college tier |

- **From M5 (offseason start, D-049):** generate rosters as of the *previous* season's opening day (53 + 16), simulate that season, then run Season Wrap, which produces the inherited situation at the Re-sign window.
- **Determinism:** each player's seed is `Hash(leagueSeed, "gen", season, slotIndex)`. Same seed, same league (a golden test).

### 4.11 The name blocklist
- `data/blocklists/famous-people.json`: normalized full names. **Sources to compile:** Pro Football Hall of Fame members, notable NFL players of the last ~25 seasons (e.g., Pro Bowl selections), plus a short list of other very famous people (presidents, globally famous athletes and entertainers). Names are facts. Compiling the list is a one-time effort of a few hours, kept in the spec's scope.
- **Rule:** a generated full name that matches (case- and accent-insensitive) is re-rolled. **Test:** a unit test feeds a blocklisted name first and checks that it's re-rolled ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).

## 5. Data model

- `Players`: `PlayerID`, `TeamID?`, `Status` (Prospect, Active, PracticeSquad, InjuredReserve, FreeAgent, Retired), `Position`, names, `BirthDate`, `CollegeID`, `Hometown`, `ExperienceSeasons`, draft info, `JerseyNumber`; **measurables** (wide columns); **42 true attribute columns** (wide; floats); **hidden ratings** (`Potential`, `DevCurve`, `BustRisk`, `PeakAgeOffset`, `InjuryProneness`); **personality** (six trait columns, §4.7).
- `RatingHistory (PlayerID, Season, Ovr, key attributes)`: one row per season, from M5 with player development (F-20, [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).
- `Colleges (CollegeID, Name, Region, Tier)`: copied from the data file at creation.
- **Data files:** `data/players/positions.json` (roster targets, OVR weights, archetypes, age distributions, measurable distributions), `jersey-rules.json`, `generation.json` (tiers, the athletic maps, hidden ratings, personality and the test targets), `data/names/first.json`, `last.json`, `data/places/hometowns.json`, `data/league/colleges.json`, `data/blocklists/famous-people.json`.
- **Read models** (all built through `IPerception` for the user's team): `PlayerRowDto` (ID, name, position, age, team, OVR estimate + sigma, potential grade + spread, archetype), `PlayerDetailDto` (+ every attribute estimate + sigma, measurables, bio, personality descriptors where visible, contract summary).

## 6. Application services

| Service | Commands | Queries | Domain events |
|---|---|---|---|
| `Players/PlayerGenerator` | Used inside `CreateLeague` (and the yearly draft class step from M5) | — | `PlayersGenerated` |
| `Players/PlayerQueries` | — | `GetRoster(teamID)`, `GetPlayer(playerID)`, `SearchPlayers(filter)`, all for the user's team as the viewer | — |
| `Players/Perception` (`IPerception`) | — | Used by every query above and by the AI with its own team as the viewer | — |
| `Players/OverallCalculator` (Core, pure) | — | OVR, archetype label and the potential grade from ratings | — |

## 7. AI counterpart
- The AI reads players **through the same `IPerception`**, with its own team as the viewer: exact for its own roster, fuzzy for everyone else. That's the whole fog-of-war guarantee in one place.
- Generation is neutral: AI teams' rosters come from the same targets as the user's.

## 8. UI
- **Roster** (DataGrid v1): name, position, age, OVR chip, potential grade, archetype, contract. Sticky name column. Every name opens the `PlayerPeekSheet`.
- **Player page:** a header (photo-less silhouette, name, position, number, team colors), an attributes panel grouped as in §4.2 with `RatingBar` (a range from M6), measurables, bio, personality descriptors (or "Unknown"), and the contract.
- **`RatingChip` / `RatingBar`:** a plain value while σ = 0 (M1–M5). From M6, an estimate plus its 80% range when σ > 0, and the potential grade chip shows its spread ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).
- Keyboard: the grid has arrow-key navigation, and Enter opens the peek sheet.

## 9. Calendar and phase integration
- League creation: generation (M1).
- From M5: the **yearly draft class** is generated at the start of each regular season, so scouting has the season to work ([D-058](../DECISIONS.md#d-058-scouting-department-standing--special-assignments-no-biases-in-10)).

## 10. Edge cases
- **Two-way or position-flexible players:** one position each in 1.0. Position changes come later.
- **K/P/LS:** OVR formulas use only their special teams attributes, plus Toughness and Discipline at low weight.
- **Undrafted players** keep their prospect perception until they sign (D-059).
- **Potential below current OVR** (declining veterans): the grade reflects the peak still reachable, which can be "already reached" and is shown as the current level.

## 11. Logging
- Information: `LeagueGenerated {Seed} {Players} {ElapsedMs}`, `DraftClassGenerated {Season} {Count}`.
- Event IDs: 1300–1329.

## 12. Performance
- Generating ~2,300 players (rosters, practice squads, free agents) < 2 s, inside the < 5 s league budget.
- `IPerception` for a 2,000-row grid < 20 ms (pure math, cached per league year).
- These are targets read from the timed log, not test assertions; the < 5 s league budget is measured at the M1 exit ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).

## 13. Test plan
- **Unit (Core):** the OVR calculator per position, potential grade bands, the measurable → physical mapping (monotonic, near-exact within the noise), archetype labeling.
- **Golden:** `GenerateLeague(seed: 42)` summary snapshot (Verify).
- **Distribution:** per-position tier means and SDs within tolerance over 20 seeds. Count of 90+ players. Age mean.
- **Blocklist:** a blocklisted name is re-rolled (a unit test, [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)).
- **Perception:** own roster exact and σ = 0 before M6. The 80% interval math and determinism per (viewer, player, season) arrive with σ > 0 in M6.
- **Consumer test (M2):** every attribute is referenced by `SimTuning.json`.
- **Autoplay (M5):** generation + progression hold the health bands for 30 seasons.

## 14. Open questions (for owner review)
*All resolved in the 2026-10-15 review ([D-065](../DECISIONS.md#d-065-f-03-player-model-accepted-42-attributes-80-ranges-roster-mix-as-averages)):*
1. ~~The attribute list~~ → the 42 attributes as specified.
2. ~~The display interval~~ → 80% (estimate ± 1.28σ), retunable in data after playtesting.
3. ~~Roster composition~~ → the targets are league averages with per-team variation and floors (§4.1), not rules.

## 15. Decisions made while specifying
- Jumping and Play Recognition **folded**, and Agility and Strength **wired in**, as D-050 allows. [SIMULATION_ENGINE.md §4](../architecture/SIMULATION_ENGINE.md#4-play-resolution) is updated to match.
- OVR weights are **derived from the sim** (M2 regression), not hand-set permanently.
- The perception error model is a **shared player-level term plus a small attribute-level term**, and ranges are **80% intervals**. Numbers are owned by F-17.
- [D-065](../DECISIONS.md#d-065-f-03-player-model-accepted-42-attributes-80-ranges-roster-mix-as-averages): the owner accepted the 42 attributes and the 80% interval, and made the roster targets league averages rather than rules.
