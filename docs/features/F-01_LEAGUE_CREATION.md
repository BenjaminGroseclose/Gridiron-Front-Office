# F-01: League creation & save management

> **Status:** Accepted (owner review 2026-11-05, [D-096](../DECISIONS.md#d-096-f-01s-m1-slice-accepted); drafted by Claude 2026-11-04, its four questions answered the same day as [D-095](../DECISIONS.md#d-095-accent-states-move-away-from-their-text-the-picker-can-start-over-a-team-is-selected-then-taken-the-desktop-end-to-end-is-a-ui-flow-plus-a-check); **the M1 slice only**: the New League wizard v0, and what the load screen and Continue show of a league) · **Milestone:** M0 (save management, built), M1 (the wizard v0), M3 (Time Machine, export, import, duplicate), M5 (the offseason start, Ironman), M6 (clock settings), M11 (league data import) · **Owner:** Ben · **Last updated:** 2026-11-05
>
> M0 built save management from [PERSISTENCE.md](../architecture/PERSISTENCE.md) §2, §9 and §10 (create, open, close, the load screen, Continue, delete), and this spec doesn't repeat it. The feature's whole arc is in [FEATURES.md](../FEATURES.md#-f-01-league-creation--save-management). Decisions this slice implements: [D-042](../DECISIONS.md#d-042-no-difficulty-presets-in-10) (team choice is the difficulty), [D-049](../DECISIONS.md#d-049-a-new-league-starts-at-the-offseason-the-previous-gm-was-fired) (M1–M4 leagues open at Training Camp), [D-073](../DECISIONS.md#d-073-a-leagues-first-season-is-the-real-world-year-its-created-in) (the first season is this year), [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) (build for today's consumers), [D-089](../DECISIONS.md#d-089-a-league-and-its-save-are-written-in-one-command-the-cap-lives-in-a-seasons-table) (a league and its save in one command) and [D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) (an outsider's view before the pick). The builder's guide is [M1_WP8_GUIDE.md](../milestones/M1_WP8_GUIDE.md).

---

## 1. Summary

New League v0 takes a name and an optional seed, generates the whole league with progress, then shows all 32 teams with their roster strength and cap space. You pick one and land at your league's home, with the app in your team's colors. Which team you take is the only difficulty setting there is (D-042), so the picker shows what you'd be taking over. The load screen and Continue then name each league's team, season and phase.

## 2. Goals / non-goals

**Goals**
- A league in a few seconds: a name is all that's required.
- The same seed and year always make the same league, whatever its name (D-073), so a seed can be shared.
- A choice made on what matters: every team's roster strength (overall, offense and defense, D-092 #3) and cap space, side by side.
- A league without a team always reopens to the picker: after a crash, a close, or restoring its `created` snapshot.
- The app wears your team's colors from the pick on, with readable text in both modes (M1 §1, item 6).
- The load screen and Continue say which team each save is and where its season stands.

**Non-goals (out for M1)**
- The rule steps (injury frequency in M3, Ironman in M5, the clock settings in M6, live events in M9, "career can end" in M10). Each arrives with its milestone ([D-036](../DECISIONS.md#d-036-league-settings-at-creation-are-gameplay-only-one-user-team)).
- The inherited situation and team needs on the cards (M5, with the simulated prior season), and league data import (M11).
- A cancel button while generating: it takes under a second in Debug (WP5 §2). `CreateLeague` already honors a cancelled token.
- The team's record on the load screen (M3, with games), league templates, more than one user team (F-28), and changing teams after the pick.

## 3. Player stories

- As the GM, I want to start a league with just a name, so I'm looking at teams within seconds.
- As the GM, I want to type a seed a friend shared, so we run the same league.
- As the GM, I want to compare all 32 teams' strength and cap room before I choose, because the team I take is how hard my career will be.
- As the GM, I want each save on the load screen to show its team and where its season is, so I can tell my leagues apart.

## 4. Rules

| Rule | Detail | Test name |
|---|---|---|
| **The name** | Required and trimmed, at most `SaveRules.MaxLeagueNameLength` (60) characters, checked before generating by the rule `SaveManager` applies (`SaveRules.CheckLeagueName`). Two leagues may share a name: each gets its own file | `A_blank_name_is_refused_before_anything_is_generated` |
| **The seed** | Empty means random: a whole number from 1 to 999,999,999, drawn by the UI (short enough to read out). Typed, it's any whole number from 0 to 18,446,744,073,709,551,615 (a `ulong`), digits only, with spaces trimmed. Anything else is refused under the field. The seed is shown on the picker | `A_typed_seed_makes_its_league`, `A_seed_that_isnt_a_whole_number_is_refused` |
| **The season** | This year, from `TimeProvider` (D-073), passed to `CreateLeague` | `The_league_starts_this_year` |
| **Generate** | `CreateLeague.RunAsync` with progress: "Generating the league" then "Saving it". The save and the league are written in one command, and the `created` snapshot is taken before the pick (D-089). The fields and buttons wait while it runs | `Generating_shows_each_step_then_the_picker` |
| **The picker** | All 32 teams in the team set's order, grouped by division (8 groups of 4). Each card: the crest, "City Nickname", the conference and division, roster strength as three rating chips (overall, offense, defense), and cap space. Ratings are an outsider's (D-092 #1) | `The_picker_shows_every_team_by_division` |
| **The pick** | Select, then confirm (D-095 #3): a click marks one card, and a second moves the mark. **Take over the {City Nickname}** runs `LeagueCommands.PickUserTeamAsync`, then opens the league's home. Nothing is picked before Take over, and a league that already has a team never shows the picker again | `Take_over_picks_the_marked_team_and_opens_home` |
| **Starting over** | From the picker, after a confirmation (D-095 #2): the unpicked league goes to the Recycle Bin, as Delete does, and the first step comes back with the name and seed filled in | `Starting_over_deletes_the_unpicked_league` |
| **Where an open league lands** | Its home when it has a user team, the picker when it doesn't: from Continue, from Load, and after the wizard | `A_league_without_a_team_opens_to_the_picker` |
| **The tint** | From the pick on, the accent is the user team's primary (`Settings → Accent: Team`, the default), with every derived color readable (UI_DESIGN_SYSTEM §3.2). Before the pick, and at the menus, it's neutral | `The_app_takes_the_teams_color_once_it_is_picked` |
| **The load screen** | A card for a save this app can open as it is shows the crest, the team ("New York Excelsiors") and "2026 · Training Camp". A league without a team says "No team picked yet". Other cards are unchanged | `A_card_names_its_team_season_and_phase` |
| **Continue** | Names its league and team, with the crest, above the button | `Continue_names_its_team` |

## 5. Data model

- **No schema change.** Everything the wizard needs is already in the save (WP5): `SaveInfo.Seed`, `CurrentSeason`, `CurrentPhase` and `UserTeamID`, and the `Teams` table.
- **`SaveCard`** (Application, `ISaveCatalog`) gains `Team` (`TeamIdentityDto?`), `Season` (`int?`) and `Phase` (`Phase?`). `SaveCatalog` reads them with raw SQL from `SaveInfo` and `Teams`, only for a save at the current schema (`Ready`), since other versions may have other columns (PERSISTENCE §9).
- **Read models used:** `LeagueHeaderDto` (where the league lands), `TeamSummaryDto` (the cards). Both exist (WP6).

## 6. Application services

| Service | Commands (mutations) | Queries (read models) | Domain events raised |
|---|---|---|---|
| `Leagues/CreateLeague` (WP5) | `RunAsync(NewLeague, progress, ct)`: generates, then creates the save with its league | — | `SaveOpened` (by `SaveManager`) |
| `Leagues/LeagueCommands` (WP5) | `PickUserTeamAsync(TeamID)` | — | `UserTeamPicked` |
| `Leagues/LeagueQueries` (WP6) | — | `GetHeaderAsync()`, `GetTeamSummariesAsync()` | — |
| `Saves/ISaveManager` (M0) | `DeleteAsync(path)`, for starting over | — | `SaveDeleted`, `SaveClosed` |
| `Saves/ISaveCatalog` (M0) | — | `ListAsync()`, with the team, season and phase (M1) | — |
| `Saves/SaveRules` (M0) | — | `CheckLeagueName(name)` (M1), moved out of `SaveManager` | — |

WP8 registers `LeagueGenerator`, `CreateLeague` and `LeagueCommands` in `AddGfoApplication`, with their first consumer (WP5 §2).

## 7. AI counterpart

None. Every team is generated the same way, and the 31 others are AI teams from the moment you pick. The picker shows ratings as an outsider sees them (D-092 #1), the same view any rival GM has: from M6 that's an estimate, not the truth.

## 8. UI

- **Routes:** `/new` (step one), `/pick-team` (the picker, for any open league without a user team), `/load` and `/` (M0's, with the team added).
- **Step one** (`/new`, on the menu backdrop):

  ```
  New league
  [ League name                    ]
  [ Seed (optional)                ]   Leave it empty for a random league.
  (progress bar)  Generating the league…
                                 [Back]  [Generate]
  ```

- **The picker** (`/pick-team`, full width, in the menu shell without the league's navigation):

  ```
  Pick your team                              Saturday League · seed 482913377 · 2026
  You're taking over a team whose GM was just let go. Stronger rosters make an easier start.
  Heritage · East
  [crest] New York Excelsiors   [crest] Philadelphia …   [crest] …   [crest] …
          OVR 78  OFF 80  DEF 75
          Cap space $12.4M
  Heritage · North …
  ─────────────────────────────────────────────────────────────── (sticky footer)
                   [Start over]  [Close league]  [Take over the New York Excelsiors]
  ```

- **Components:** `TeamPickCard` (new, `UI/Menu`: a toggle button, marked with the accent's outline on the opaque selection), a `MenuLayout` for both steps (the menu shell, so the league's navigation doesn't show before there's a team), and WP7's `TeamMark`, `RatingChip` and `Money.Format`. `SaveCardView` gains the crest and a line for the team.
- **States:**
  - **Generating:** an indeterminate progress bar with the step's name, and every control waiting.
  - **Refused input:** a bad name or seed is explained under its field.
  - **Failures:** a failed create or pick is a toast, and the page stays.
  - **Loading:** the picker shows a progress bar while the summaries load.
  - **A save with no teams** (one made by M0's New League page): an empty state that says so, with Close league.
- **Keyboard:** Enter in either field generates. The cards are toggle buttons in reading order: Enter or Space marks one, and Tab reaches Take over in the footer. Start over's confirmation takes Enter and Escape.

## 9. Calendar and phase integration

The league opens at Training Camp on `LeagueCalendar.TrainingCampOpens(season)` (D-049, until M5's offseason start). Nothing moves the calendar in M1.

## 10. Edge cases

- **A league closed or crashed before the pick** reopens to the picker. So does restoring its `created` snapshot (M3's Time Machine).
- **A save made by M0's New League page** after WP5 has the current schema and no teams. The picker says so and offers Close league. Delete it from the load screen.
- **Two leagues with the same name** get separate files (`SaveManager`'s unique path).
- **Seed 0** is valid. A typed seed with leading zeros ("007") is the number 7, and the picker shows it as 7.
- **Starting over** from a league that fails to delete (a file another program holds) keeps the league and says why, as Delete does.

## 11. Logging

No new events. `CreateLeague` logs 1304 (the league created, with the seed and the time, where the < 5 s budget is read) and `LeagueCommands` 1305 (the user's team picked). Each command is SaveWriter's 2000.

## 12. Performance

- **Generating and saving** a league: < 5 s in a Release build, with progress. Read from 1304 at the M1 exit, not asserted (D-078 #4). Debug: about 0.4 s to generate and about 0.25 s to write (WP5 §7).
- **The picker's 32 summaries:** one `GetTeamSummariesAsync` read (four statements, WP6). Measured in the DevHost at the WP8 check.

## 13. Test plan

- **Application:** `SaveRules.CheckLeagueName`, with the messages `SaveManager` gave.
- **Infrastructure:** `SaveCatalog` reads the team, season and phase of a seed-42 save, before and after the pick. A save from another schema gets none of them.
- **UI (bUnit, over a real save, D-093):**
  - step one's fields, progress and refusals;
  - the picker's 32 cards and their order;
  - marking a card, moving the mark, and Take over's pick and landing;
  - starting over;
  - the load screen's and Continue's team;
  - the tint following the pick.
- **UI.Components:** every state fill holds its text at 4.5:1 for all 32 shipped primaries and the neutral blue (D-095 #1).
- **The desktop app:** create → pick → Home, run by Claude through WebView2's DevTools port after each slice, with screenshots (D-095 #4). The UI tests run the same flow over a real save.
- **Autoplay:** no impact.

## 14. Open questions

*All four answered on 2026-11-04 ([D-095](../DECISIONS.md#d-095-accent-states-move-away-from-their-text-the-picker-can-start-over-a-team-is-selected-then-taken-the-desktop-end-to-end-is-a-ui-flow-plus-a-check), the [WP8 guide §10](../milestones/M1_WP8_GUIDE.md#10-questions-answered-2026-11-04)):*
1. ~~The button states for teams with dark text on their color~~ → one rule for all 32: each state moves away from its text.
2. ~~Trying another seed from the picker~~ → Start over, which sends the unpicked league to the Recycle Bin.
3. ~~Confirming the pick~~ → select a card, then Take over.
4. ~~The desktop end-to-end~~ → a UI flow plus Claude's check in the desktop app.

## 15. Decisions made while specifying

- [D-095](../DECISIONS.md#d-095-accent-states-move-away-from-their-text-the-picker-can-start-over-a-team-is-selected-then-taken-the-desktop-end-to-end-is-a-ui-flow-plus-a-check): the accent's button states, starting over, select then take over, and the desktop check.
- [D-096](../DECISIONS.md#d-096-f-01s-m1-slice-accepted): this M1 slice accepted as written.
