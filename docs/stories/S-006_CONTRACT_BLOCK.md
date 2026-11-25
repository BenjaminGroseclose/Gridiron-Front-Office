# S-006 · A player's contract, season by season

> **Status:** Draft · 2026-11-19
>
> **Kind:** Story · **Feature:** F-12 · **Milestone:** M1 · **Size:** S · **Needs:** S-005

## Outcome

The Player page's contract block shows F-12's one-line summary, then a table of this season and every season left on the deal.

## Acceptance criteria

1. `ContractText.Line` reads F-12's sentence from a known contract: "3 years left · $12.4M APY · cap hit this season $14.1M · $9.0M guaranteed remaining".
2. The table has columns for season, base, bonus (prorated), guaranteed and cap hit, with the money formatted by `Money.Format`. It starts at this season. Past seasons aren't shown (D-102 #3).
3. A free agent has no contract block.
4. Every contracted player in a seed-42 save has a contract year for this season. `GetPlayerAsync` relies on it (`Single(c => c.Season == current)`), so a later change that breaks it fails here, not on the page.
5. `/_gallery/player` shows the contract block for a veteran, a rookie and a free agent. Checked in the pane.

## Surface

- `UI/Football/ContractText.cs`: `Line(ContractSummaryDto)`, and the table's rows from `Seasons` for this season on.
- Test ID: `contract`.

## Notes

- [F-12 §8](../features/F-12_CONTRACTS.md#8-ui); [D-102](../DECISIONS.md#d-102-the-roster-opens-on-your-team-and-picks-any-team-or-all-of-them-and-a-position-the-top-bar-names-your-team-the-contract-table-runs-from-this-season-every-team-name-links-to-its-page) #3.
- The [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The contract block"), §7 (the contract-year gotcha).
- The read model keeps past seasons, so showing them later only changes the table ([IDEAS.md](../IDEAS.md)).

## Work

- [ ] Claude, at Ready: the tests and the shell
- [ ] You: `ContractText` and the block, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the block in `/_gallery/player`; checked in a visible pane in Dark and Light
- [ ] Docs: F-12 §8, the contract block as built
- [ ] You: `./build.ps1` green, then one commit: `(feat) *players* : show a player's contract from this season on (S-006)`

## Built

—
