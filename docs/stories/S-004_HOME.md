# S-004 · Home shows your team

> **Kind:** Story · **Feature:** F-02 · **Milestone:** M1 · **Size:** S · **Needs:** S-003

## Outcome

Home becomes a small dashboard for your team: its band, where the season stands, your best players and your cap space. It's the stub that M3 turns into the real dashboard.

## Acceptance criteria

1. Your team's band (`TeamHero`), in your team's colors.
2. The line "2026 · Training Camp · Aug 1, 2026" (the date in the current culture), with "No games played yet" under it.
3. Your five best players by OVR, each with their name, `PositionPill` and `RatingChip`.
4. A Space tile with Committed beside it, both equal to your team's summary.
5. S-001's redirects still hold: no league goes to the main menu, and no team goes to the picker.

## Surface

- `UI/Home/HomePage.razor` (`/home`, from S-001).
- Test IDs: `season-line`, `top-players`, `cap-space`.

## Notes

- The [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("Home", "The shell follows the store; pages don't").
- Home reads the header and your roster itself, on the thread pool. It doesn't wait on `GameStateStore`, which lags the pick.
- It reuses S-003's parts, so it costs little.
- The names become `PlayerLink`s with S-007.

## Work

- [ ] Claude, at Ready: the tests
- [ ] You: the page, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the page checked in a visible pane in Dark and Light with several team colors
- [ ] Docs: UI_DESIGN_SYSTEM §7 (what Home holds in M1)
- [ ] You: `./build.ps1` green, then one commit: `(feat) *home* : show your team, the season and your best players on home (S-004)`

## Built

—
