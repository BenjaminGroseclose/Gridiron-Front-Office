# S-010 · The grid stays fast at 2,000 rows

> **Kind:** Enabler · **Feature:** F-03 · **Milestone:** M1 · **Size:** S · **Needs:** S-009

## Outcome

The M1 exit item "sorting and scrolling a 2,000-player grid stays under budget" is measured in the desktop app and recorded.

## Acceptance criteria

These are measurements, taken in the desktop app over WebView2's DevTools port, on `/roster?team=all` (1,846 rows for seed 42) and on the gallery's 2,000 rows.

1. A header click to the next paint takes under 100 ms.
2. A position change to the next paint takes under 100 ms.
3. A scroll through the whole list shows no blank rows, and its time is recorded.
4. The numbers go in the M1 plan's results, beside Spike S0b's 45–52 ms per sort.

## Surface

None. Nothing new in the code unless the budget is missed.

## Notes

- Why it's measured rather than asserted ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) #4): bUnit has no layout, so it can't time a paint or a scroll.
- The bar: ARCHITECTURE.md's < 100 ms for a sort or a filter.
- The measuring setup: the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The budget"), and D-095 #4's desktop check.
- If the budget is missed, the fallback is fewer default columns ([the plan's §9](../milestones/M1_THE_LEAGUE.md#9-risks-and-fallbacks)). That would be a new card.

## Work

- [ ] Claude: the measurements, recorded
- [ ] Docs: the M1 plan's results (the timings)
- [ ] You: one commit: `(doc) *m1* : record the grid's sort and scroll times (S-010)`

## Built

—
