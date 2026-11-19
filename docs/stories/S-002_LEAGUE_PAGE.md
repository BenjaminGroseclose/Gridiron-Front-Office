# S-002 · Browse the league by conference and division

> **Kind:** Story · **Feature:** F-02 · **Milestone:** M1 · **Size:** S · **Needs:** S-001

## Outcome

`/league` shows the whole league at a glance: both conferences, each with its four division cards, and every team in them a link to its page.

## Acceptance criteria

1. Two conferences, each with four divisions of four teams, named and ordered as the team set has them (`GetStructureAsync`).
2. Each team row is its crest (24 px, decorative) and a `TeamLink` reading "City Nickname" that goes to `/team/{id}`.
3. The page's `h1` reads "League".
4. With no league open, the page goes to the main menu.
5. Under 1,100 px the two conferences stack in one column. Checked in the pane.

## Surface

- `UI/League/LeaguePage.razor`: `@page "/league"`.
- `[data-testid=conference]` on each conference column, holding its name; `[data-testid=division]` on each division card, holding its name.
- Each team is a `TeamLink` (`a.gfo-team-link`) with `CrestSize="24"`.

## Notes

- [F-02 §8](../features/F-02_LEAGUE_AND_TEAMS.md#8-ui); the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The League page", "Every page has an `h1`"); [D-100](../DECISIONS.md#d-100-screens-are-built-from-fluents-layout-and-text-components-scoped-css-covers-only-what-they-cant).
- One read of `GetStructureAsync`, on the thread pool. A CSS grid holds the two columns, each a `FluentStack` of flat `FluentCard`s.
- The record joins each row in M3.

## Work

- [ ] Claude, at Ready: the tests and the shell
- [ ] You: the page, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the page checked in a visible pane in Dark and Light with several team colors, at 1,280 px and under 1,100 px
- [ ] Docs: F-02 §8, the League page as built
- [ ] You: `./build.ps1` green, then one commit: `(feat) *league* : browse the league by conference and division (S-002)`

## Built

—
