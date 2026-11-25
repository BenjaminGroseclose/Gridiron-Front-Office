# S-008 · A team's roster in a grid

> **Status:** Draft · 2026-11-19
>
> **Kind:** Story · **Feature:** F-03, F-04 · **Milestone:** M1 · **Size:** M · **Needs:** S-007

## Outcome

The Team page's roster becomes `RosterGrid`, the first real use of `DataGrid<T>`. It has a pinned name column, sorting and M0's keyboard map, and every name opens its peek.

## Acceptance criteria

1. The columns and their widths:
   - Name: pinned, a `PlayerLink`, 220 px.
   - Pos: `PositionPill`, 88 px.
   - #: 64 px.
   - Age: 64 px.
   - OVR: `RatingChip`, 76 px.
   - Pot: `PotentialChip`, 76 px.
   - Archetype: 160 px.
   - Cap hit: `Money.Format`, 112 px.
   - Team: the crest and the abbreviation, 96 px. Only when `Teams` is given.
2. The sorts:
   - Name, by last name and then first name.
   - OVR, by the estimate.
   - Pot, by the grade.
   - Cap hit, with free agents' "—" last.
   - Team, by the abbreviation.
3. A free agent's row has "FA" in Team and "—" in # and Cap hit.
4. Clicks and keys:
   - Enter or a double-click on a row opens the peek.
   - A name's click opens the peek once, and doesn't select the row.
   - A `TeamLink` in the Team column doesn't select the row.
5. The Team page shows its 53 players in `RosterGrid`, replacing S-003's plain list.
6. The gallery's DataGrid page runs on `RosterGrid`, with 2,000 sample rows and the Team column. That's the budget's reference for S-010. Checked in the pane.

## Surface

- `UI/Football/RosterGrid.razor`, with these parameters:
  - `Rows` (`IEnumerable<PlayerRowDto>`)
  - `Label`
  - `Height`
  - `Teams` (`IReadOnlyDictionary<TeamID, TeamIdentityDto>?`, null for one team's roster)
- `TeamLink` stops propagation in a grid cell, as `PlayerLink` does.

## Notes

- The rules for the grid are in the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2: the rows "The Roster grid", "Free agents" and "The team of a row". §3.2 has the column sketch.
- `DataGrid<T>`'s rules are in [UI_DESIGN_SYSTEM.md §5.3](../architecture/UI_DESIGN_SYSTEM.md#53-datagridt-a-gfo-wrapper-over-fluentdatagrid):
  - every column has `MinWidth` = `Width`;
  - `SortBy` is defined ascending;
  - `TItem` is set when binding callbacks;
  - a pill column is its widest pill plus 36 px.
- Sort by `.Estimate` and `.Value`. `PerceivedRating` and typed IDs don't compare in memory.
- Check that Fluent's cell focus and `gfo-grid.js`'s Enter don't open the sheet twice (guide §7).

## Work

- [ ] Claude, at Ready: the tests and the shell
- [ ] You: `RosterGrid` and the Team page's roster, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the DataGrid gallery page on `RosterGrid`; the grid checked in a visible pane in Dark and Light, Comfortable and Compact
- [ ] Docs: F-03 §8 (the grid as built); UI_DESIGN_SYSTEM §5.4 (`RosterGrid`)
- [ ] You: `./build.ps1` green, then one commit: `(feat) *players* : show a team's roster in the sortable grid (S-008)`

## Built

—
