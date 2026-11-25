# S-002 · Browse the league by conference and division

> **Status:** Checking · 2026-11-24
>
> **Kind:** Story · **Feature:** F-02 · **Milestone:** M1 · **Size:** S · **Needs:** S-001

## Outcome

`/league` shows the whole league at a glance: both conferences, each with its four division cards, and every team in them a link to its page.

## Acceptance criteria

1. Two conferences, each with four divisions of four teams, named and ordered as the team set has them (`GetStructureAsync`). → `LeaguePageTests.Two_conferences_of_four_divisions_of_four_teams_in_the_team_sets_order`
2. Each team row is its crest (24 px, decorative) and a `TeamLink` reading "City Nickname" that goes to `/team/{id}`. → `LeaguePageTests.Each_team_is_its_crest_and_name_as_a_link_to_its_page`
3. The page's `h1` reads "League". → `LeaguePageTests.The_heading_is_League`
4. With no league open, the page goes to the main menu. → `LeaguePageTests.Without_an_open_league_it_goes_to_the_main_menu`
5. Under 1,100 px the two conferences stack in one column. Checked in the pane.

**At Ready (2026-11-19), 4 tests fail, each for this card's reason:** `LeaguePage` is a shell (`@page "/league"` only), so the three that open seed 42 time out waiting for its eight division cards, and the fourth stays on `/league`. `./build.ps1` compiles clean and fails only these four.

How the tests read the page (`tests/UI.Tests/League/LeaguePageTests.cs`):
- They open seed 42 with team 5 picked and wait (10 s) until eight `[data-testid=division]` cards have rendered: the read runs on the thread pool.
- A conference's or a division's text *starts with* its name, so the name comes first in its element, before its cards or teams. Both conferences have an East, a North, a South and a West, so each division is checked inside its own conference.
- The order is checked against `GetStructureAsync` on the same save: the conferences, then each division's links by `href`.
- A link's name is its own text nodes ("City Nickname"); the crest's monogram is SVG text inside it and doesn't count. The crest is `svg.gfo-team-mark` inside the link (`TeamLink`'s `CrestSize="24"`, as the Surface says), not a `TeamMark` beside it as the WP9 guide sketched.
- The page has exactly one `h1`.

## Surface

- `UI/League/LeaguePage.razor`: `@page "/league"`.
- `[data-testid=conference]` on each conference column, holding its name; `[data-testid=division]` on each division card, holding its name.
- Each team is a `TeamLink` (`a.gfo-team-link`) with `CrestSize="24"`.

## Notes

- [F-02 §8](../features/F-02_LEAGUE_AND_TEAMS.md#8-ui); the [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("The League page", "Every page has an `h1`"); [D-100](../DECISIONS.md#d-100-screens-are-built-from-fluents-layout-and-text-components-scoped-css-covers-only-what-they-cant).
- One read of `GetStructureAsync`, on the thread pool. A CSS grid holds the two columns, each a `FluentStack` of four flat, full-width `FluentCard`s, one under another (the owner's mockup, 2026-11-24).
- The record joins each row in M3.

## Work

- [x] Claude, at Ready: the tests and the shell
- [x] You, then Claude at your request (2026-11-24): the page, until the tests pass. The four `LeaguePageTests` pass.
- [x] Claude: each test proven to fail on a planted bug; the page checked in a visible pane in Dark and Light with several team colors, at 1,280 px and under 1,100 px
- [x] Docs: F-02 §8, the League page as built
- [ ] You: `./build.ps1` green, then one commit: `(feat) *league* : browse the league by conference and division (S-002)`. The crest blocker is cleared (2026-11-25): the owner keeps the top bar's 28 px crest, so S-001 #4 and then its test now say 28, and all 284 UI tests pass. Stop `watch: DevHost` before `./build.ps1`: it locks the DevHost's DLLs.

## Built

**Page (2026-11-24):** laid out as the owner's mockup.
- "League" in Display L.
- The two conferences in a CSS grid (24 px gap), one column under 1,100 px.
- Each conference is a `FluentStack` (16 px): its name in condensed display type (26 px, 800) with a divider filling the rest of the line, then its four flat, full-width division cards, one under another (not two by two).
- Each card: the division name (`FluentText` `h3`, 12 px semibold, Muted) 12 px above four `TeamLink`s with 24 px crests, 12 px apart.
- The read runs once, on the thread pool.

**Two traps found:**
- Fluent's stylesheet sets every horizontal `fluent-divider` to `width: 100%`. Beside the name, that squeezed the name until it wrapped. The rule is now `flex: 1 1 0; width: auto` through `::deep` on a GFO class.
- Fluent adopts an unlayered `h1, …, h6 { font-weight: var(--fontWeightMedium) }`, which beats `.gfo-display`'s 800 in `@layer utilities`. Every display heading in the app (Home, Load, Settings, the main menu) renders at 500. This page sets 800 in its scoped CSS; the app-wide fix belongs in `gfo-tokens.css`.

**Tests, each proven on a planted bug:** a swap build with `CustomAfterMicrosoftCommonTargets`, the repository untouched. Each plant failed only its own test, and the other three passed:

| Plant | Test that failed |
| --- | --- |
| The conference name after its cards | #1 |
| `TeamLink` without `CrestSize` | #2 |
| The heading "Leagues" | #3 |
| No redirect without a league | #4 (`failed (canceled)`, the 10 s wait) |

**Build:** every project except the DevHost (locked by the owner's running `watch: DevHost`) builds with `--no-incremental -warnaserror`. Fast tests: 1,050 of 1,051 pass. The one failure was S-001's top bar crest, from the owner's `CrestSize="28"`; S-001 #4 now says 28, and the UI tests pass, 284 of 284 (2026-11-25).

**Screens:**
- **Setup:** the DevHost in a visible Chrome window, on the owner's "Start Check" save (Minneapolis Millers, #343A40).
- **Covered:** Dark and Light, team accent and Neutral (#0F6CBD), at 1,280 px and 1,050 px.
- **At 1,280 px:** two 487 px columns.
- **At 1,050 px:** one 767 px column, Heritage then Frontier.
- **Throughout:**
  - No sideways scroll and no link wraps.
  - The gap from name to line is 16 px.
  - Cards: shadow `none`, 20 px padding, 14 px radius.
  - All 32 crests read on both modes' cards.
- **Keyboard:** Tab from the heading lands on New York Excelsiors with `:focus-visible`. The ring is Chrome's default outline, not VISUAL_DESIGN's accent focus ring. That's `TeamLink`'s, so it affects every page; not changed here.
