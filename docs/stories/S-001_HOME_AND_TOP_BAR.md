# S-001 · Home at `/home`, and the top bar names your team

> **Status:** Done · 2026-11-19
>
> **Kind:** Story · **Feature:** F-01, F-02 · **Milestone:** M1 · **Size:** S · **Needs:** —

## Outcome

After the pick you land on Home at `/home`, the nav reads Home, Roster, League and Settings, and the top bar shows your team's crest and name with "League · season · phase" beside it. This finishes the routes and the top bar, which are half-built, so `main` builds green again.

## Acceptance criteria

1. ✅ Every landing that went to `/league` goes to `/home`: Start on the summary, Continue, Load, the menu with a league open, and the summary's and the picker's landings once there's a team. → `FinalizeTests.Start_picks_the_team_and_opens_home`, `FinalizeTests.A_league_with_a_team_goes_home`, `TeamPickerTests.A_league_with_a_team_goes_home`, `MainMenuTests.With_a_league_open_the_menu_goes_to_its_home`, `MenuLandingTests.Continue_lands_a_league_on_the_picker_until_it_has_a_team`, `MenuLandingTests.Load_lands_a_league_on_the_picker_until_it_has_a_team`
2. ✅ `/home` with no league open goes to the main menu, and with a league but no team, to the picker (`LeagueHome`'s two redirects, now Home's). With a team it stays, showing `LeagueHome`'s content until S-004. → `HomePageTests.Without_an_open_league_it_goes_to_the_main_menu`, `HomePageTests.A_league_without_a_team_goes_to_the_picker`, `HomePageTests.It_names_the_league_and_says_what_comes_next`
3. ✅ The nav is Home, Roster, League and Settings, in that order, with the current one active. → `MainNavTests.Home_Roster_League_and_Settings_with_the_current_one_active`
4. ✅ After the pick, the brand is your team's crest (20 px, decorative) and "City Nickname", a link to `/team/{id}`, with no wordmark. → `MainLayoutTopBarTests.A_pick_puts_your_teams_crest_and_name_in_the_brand_as_a_link_to_its_page`
5. ✅ Before the pick, the brand is empty, and the context reads "Seed 42 · 2026 · Training Camp" once the header loads. → `MainLayoutTopBarTests.Before_the_pick_the_brand_is_empty_and_the_context_names_the_league_season_and_phase`
6. ✅ Until the header loads, the context is the league's name, in the same `league-name` element. → `MainLayoutTests.With_a_league_open_the_shell_names_it_and_shows_the_navigation`, `MainLayoutTests.The_shell_follows_a_league_opened_from_any_screen`
7. ✅ A layout made after the store loaded starts with your team. → `MainLayoutTopBarTests.A_layout_made_after_the_store_loaded_starts_with_your_team`
8. ✅ Closing the league takes your team out of the top bar. → `MainLayoutTopBarTests.Closing_the_league_takes_your_team_out_of_the_top_bar`
9. ✅ A layout that goes away stops following the store. → `MainLayoutTopBarTests.A_layout_that_goes_away_stops_following_the_store`

Tests 4–9 were already in `main` (36ebd25). 1–3 moved at Ready (2026-11-17): from `/league` to `/home`, `LeagueHomeTests` into `Home/HomePageTests.cs`, and the nav to four entries.

**At Ready, 11 tests fail, each for this card's reason:**
- The three `HomePageTests`: `HomePage` is a shell.
- Both `MainLayoutTests` above: the `league-name` element is missing before the header loads.
- Three `MainLayoutTopBarTests` (4, 5, 7): the context reads "Seed 42 2026 TrainingCamp".
- `FinalizeTests.Back_returns_to_the_picker_with_the_team_marked_and_picks_nothing` and `TeamPickerTests.Next_opens_the_summary_for_the_marked_team`: `Routes.WithTeam` is a shell that throws.
- `MainMenuTests.Continue_names_its_team_with_the_crest`: see the Notes.

Criteria 1, 3, 8 and 9 already pass on the tree. They're proven on a plant at Checking like the rest.

**On 2026-11-19, every criterion is proven** (✅: its tests pass on the tree and fail on a planted bug; see Built). `./build.ps1` is green: 1,047 fast tests.

## Surface

- `Routes`: `Home` (`/home`), `League`, `Roster`, `MainMenu`, `TeamPicker`, `Wizard`, `Settings`, `Finalize`, `Team(TeamID)`, and `WithTeam(route, TeamID)`, which moves here from `SaveActions`.
- `UI/Home/HomePage.razor`: `@page "/home"`.
- The top bar: `.gfo-appbar-brand`, `[data-testid=user-team]` around the `TeamLink`, and `[data-testid=league-name]` around the whole context.

## Notes

What was left in the working tree on 2026-11-17:
- **`WithTeam`:** `Finalize.razor:116` and `TeamPickerPage.razor:96` call `Routes.WithTeam`. At Ready it's a shell on `Routes` that throws, so the tree builds. Its body is still `SaveActions.WithTeam`'s (`SaveActions.cs:18`): move it, and delete the old one.
- **Continue's line lost the league's name:** with a team it reads "New York Excelsiors · last played …", and F-01 §4 says "Saturday League · New York Excelsiors · last played …" (`Continue_names_its_team_with_the_crest`). 149c466's line was right: `@target.LeagueName@(target.Team is { } t ? $" · {t.City} {t.Nickname}" : "")`.
- **The context** (`MainLayout.razor:21`): it needs ` · ` between its parts and `Phase.Text()`, and the span has to wrap both branches, or the league's name loses its test ID. This sketch settles it:

  ```razor
  <span class="gfo-league-context" data-testid="league-name">
      @if (_leagueHeader is { } header)
      {
          <text>@header.LeagueName · @header.Season · @header.Phase.Text()</text>
      }
      else
      {
          @league.LeagueName
      }
  </span>
  ```
- **Home:** `HomePage.razor` is empty. It takes `LeagueHome`'s content and both redirects, and `League/LeagueHome.razor` (with its `.css`) goes. Home's real content is S-004.
- **Already fixed in the tree:** `TeamLink` shows "City Nickname", Continue's line without a team falls back to the league's name, and every caller uses `Routes`.
- Until S-002 and S-009 land, the nav's League and Roster lead to the router's not-found page.

Rules: [D-102](../DECISIONS.md#d-102-the-roster-opens-on-your-team-and-picks-any-team-or-all-of-them-and-a-position-the-top-bar-names-your-team-the-contract-table-runs-from-this-season-every-team-name-links-to-its-page) #2, and the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The routes", "The nav", "The top bar names your team").

## Work

- [x] Claude, at Ready: move the landing, redirect and nav tests (criteria 1–3); shells for `HomePage` (`@page "/home"`) and `Routes.WithTeam`
- [x] You: `WithTeam`'s body onto `Routes`, the context, Home at `/home`, Continue's line, and delete `LeagueHome`
- [x] Claude: each moved test proven to fail on a planted bug; the top bar checked in a visible pane in Dark and Light with several team colors
- [x] Docs: UI_DESIGN_SYSTEM §7 (Home is `/home`, the routes, the top bar); M0_FOUNDATIONS' note that Home is `/league`
- [ ] You: `./build.ps1` green, then one commit: `(feat) *shell* : land on home at /home and name your team in the top bar (S-001)`

## Built

Checked 2026-11-19.

**Plants:** 16 bugs, one build each, swapped in through `CustomAfterMicrosoftCommonTargets` (the repository untouched). Every one failed its criterion's tests and no test outside the card failed, except where noted:

| # | Planted bug | Failed |
| --- | --- | --- |
| 1 | `PickAsync` lands on `/league` | `Start_picks_the_team_and_opens_home` |
| 1 | `LandingAsync` sends a picked league to `/league` | Continue's and Load's `…until_it_has_a_team` (the picked case) |
| 1 | The summary sends a picked league to `/league` | `FinalizeTests.A_league_with_a_team_goes_home` |
| 1 | The picker sends a picked league to `/league` | `TeamPickerTests.A_league_with_a_team_goes_home` |
| 1 | The menu with a league open goes to `/league` | `With_a_league_open_the_menu_goes_to_its_home` |
| 2 | Home drops its redirect to the menu | `Without_an_open_league_it_goes_to_the_main_menu` |
| 2 | Home drops its redirect to the picker | `A_league_without_a_team_goes_to_the_picker` |
| 2 | Home's heading loses the league's name | `It_names_the_league_and_says_what_comes_next` |
| 3 | Roster and League swap places | `Home_Roster_League_and_Settings_with_the_current_one_active` |
| 4 | The crest is 24 px | `A_pick_puts_your_teams_crest_and_name_…` |
| 5 | The phase without `Text()` | the three top-bar tests that read the context (4, 5, 7) |
| 5 | A wordmark in the empty brand | `Before_the_pick_…`, `Closing_the_league_…`, and two `MainLayoutTests` that also count the brand |
| 6 | `league-name` only around the loaded header | both `MainLayoutTests` of criterion 6 |
| 7 | The layout ignores what the store already holds | `A_layout_made_after_the_store_loaded_starts_with_your_team` |
| 8 | The header keeps the last team after close | `Closing_the_league_takes_your_team_out_of_the_top_bar` |
| 9 | `Dispose` doesn't unsubscribe from the store | `A_layout_that_goes_away_stops_following_the_store` |

**Screens:** the DevHost in a visible Edge window at 1440 × 900, three new leagues with New York (red), Jacksonville (navy) and Cincinnati (green), each in Dark and Light. In all six the brand is the 20 px crest (`aria-hidden`) and "City Nickname" linking to `/team/{id}`, the context reads "League · 2026 · Training Camp", the nav is Home (active, in the team's tint), Roster, League and Settings, and closing the league empties the bar. Contrast on the app bar: the brand 15.5:1 (Dark) and 18.9:1 (Light), the context 9.6:1 and 10.2:1. `/home` on a league without a team went straight back to the picker.

**Docs:** UI_DESIGN_SYSTEM §7 (as built: `/home`, the nav, `Routes`, the top bar's test IDs; the landing ends at `/home`), M0_FOUNDATIONS' routes note, and the M1 plan's route-fix line.
