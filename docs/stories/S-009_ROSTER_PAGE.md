# S-009 · Browse any roster by team and position

> **Status:** Draft · 2026-11-19
>
> **Kind:** Story · **Feature:** F-03, F-04 · **Milestone:** M1 · **Size:** M · **Needs:** S-008

## Outcome

`/roster` opens on your team. Two pickers change it: **Team** (All teams, then the 32) and **Position** (All positions, then the 16). The choice is in the route, so Back from a player's page returns to the same list.

## Acceptance criteria

1. With no query, the page shows your team's 53 active players under its band, titled "Roster".
2. The Team picker lists All teams, then the 32 as "City Nickname" in the team set's order. The Position picker lists All positions, then the 16 in `Position`'s order.
3. `?team=all` shows every player who isn't retired, free agents included: 1,846 for seed 42. The Team column shows, and the band is neutral.
4. `?team=7` shows that team's 53 under its band.
5. `?position=QB` shows only quarterbacks, for one team and for all teams.
6. Changing a picker lands on `/roster?team=…&position=…` with both choices, replacing the history entry.
7. Going from one team to another shows the second.
8. An unknown team (`?team=99`) falls back to yours, and an unknown position (`?position=42`) falls back to all.
9. Before the pick, the Roster opens on All teams (D-102 #1).

## Surface

- `UI/Players/RosterPage.razor`: `@page "/roster"`, with `[SupplyParameterFromQuery]` `team` (an ID, `all`, or nothing for yours) and `position` (an abbreviation, or nothing for all).
- Test IDs: `team-picker`, `position-picker`.
- A route builder beside `Routes.WithTeam`. Its name is settled at Ready, and the tests read the URL, not the builder.

## Notes

- [D-102](../DECISIONS.md#d-102-the-roster-opens-on-your-team-and-picks-any-team-or-all-of-them-and-a-position-the-top-bar-names-your-team-the-contract-table-runs-from-this-season-every-team-name-links-to-its-page) #1 has the rule, the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The Roster page") the details, and §3.2 the query sketch.
- One team reads `GetRosterAsync`, and All teams reads `GetLeaguePlayersAsync`. The position filters the loaded rows in memory, so only a new team reads again.
- Parse a position with `Enum.TryParse` **and** `Enum.IsDefined`, or `?position=42` passes.
- `FluentSelect` is new to GFO: Spike S0b didn't cover it. Check its binding and both modes, and record what's non-obvious.

## Work

- [ ] Claude, at Ready: the tests and the shell
- [ ] You: the page and its pickers, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the page checked in a visible pane in Dark and Light, through both pickers, then Back from a player's page
- [ ] Docs: F-03 §8 (the Roster as built); UI_DESIGN_SYSTEM §5.1 (`FluentSelect`); CLAUDE.md's gotchas
- [ ] You: `./build.ps1` green, then one commit: `(feat) *players* : browse any roster by team and position (S-009)`

## Built

—
