# S-003 · Every team has a page

> **Status:** Draft · 2026-11-19
>
> **Kind:** Story · **Feature:** F-02, F-12 · **Milestone:** M1 · **Size:** M · **Needs:** S-001

## Outcome

`/team/{id}` shows any team in its own colors: its band and stadium, how strong it is, its cap, its biggest contracts and its roster.

## Acceptance criteria

1. The band is painted in the team's own primary color, with the text color `AccentColors.OnAccent` picks for it. A rival's page is in the rival's colors, not yours.
2. The band holds the crest (64 px, decorative), "City Nickname" as the page's `h1`, and the stadium's name, capacity and roof. The ghost monogram is `aria-hidden`.
3. The heading isn't a link (D-102 #4).
4. The roster strength appears as three chips, OVR, OFF and DEF, as the team picker shows them.
5. There are four tiles: Cap, Committed, Space, and Utilization (committed ÷ cap, one decimal). Each equals the team's summary.
6. The top five cap hits (name, position, cap hit) are the team's five largest, in order.
7. The roster lists the team's 53 active players. A plain list is enough; S-008 replaces it with the grid.
8. `/team/99` shows an empty state, "There's no team 99 in this league", with a link to the League page.
9. Going from one team's page to another loads the second. A load that the next id overtook is dropped.
10. With no league open, the page goes to the main menu.
11. `TeamHero` renders the same under de-DE.
12. `/_gallery/team-hero` shows all 32 bands in both modes. Each band's text holds 4.5:1 or better on its fill, and the ghost reads as texture, not text. Checked in the pane.

## Surface

- `UI/Football/TeamHero.razor`: parameters `Team` (`TeamIdentityDto`), `Title`, `Kicker`, `Subtitle`, `HeadingID` and `ChildContent`. It writes the inline custom properties `--gfo-team-fill` and `--gfo-team-on-fill` on `.gfo-team-hero`, and the ghost is `.gfo-team-hero-ghost`.
- `UI/Teams/TeamPage.razor`: `@page "/team/{ID:int}"`.
- Test IDs: `roster-strength`, `cap-tiles` (one `Stat` per tile, found by its label), `top-cap-hits`, `roster`, `team-missing`.

## Notes

- [F-02 §8](../features/F-02_LEAGUE_AND_TEAMS.md#8-ui) and [F-12 §8](../features/F-12_CONTRACTS.md#8-ui) (the cap tiles); [VISUAL_DESIGN.md §6](../VISUAL_DESIGN.md#6-signature-elements) (the band and the ghost monogram).
- The [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 rows "A team's own colors", "The ghost monogram", "The Team page" and "An unknown id".
- §3.1 has two sketches: `TeamHero`'s markup, and the load pattern every page with an id follows (`OnParametersSetAsync`, `Task.Run`, drop a stale load). The load pattern settles the race behind criterion 9.
- The top five come from `GetRosterAsync`'s rows sorted by `CapHit`. No new query is needed.
- Team colors stay out of the stylesheet. It only reads the two variables, so it holds no raw hex.

## Work

- [ ] Claude, at Ready: the tests and the shells
- [ ] You: `TeamHero` and the page, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; `/_gallery/team-hero`; the page checked in a visible pane in Dark and Light, on your team and several rivals
- [ ] Docs: F-02 §8 and F-12 §8 as built; UI_DESIGN_SYSTEM §5.4 (`TeamHero`, `TeamLink`)
- [ ] You: `./build.ps1` green, then one commit: `(feat) *teams* : give every team a page in its own colors (S-003)`

## Built

—
