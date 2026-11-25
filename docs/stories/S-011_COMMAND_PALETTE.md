# S-011 · Ctrl+K jumps to any player or team

> **Status:** Draft · 2026-11-19
>
> **Kind:** Story · **Feature:** F-03, F-02 · **Milestone:** M1 · **Size:** M · **Needs:** S-003, S-005

## Outcome

Ctrl+K opens a search box anywhere in an open league. Typing a name finds players and teams, and Enter goes to the one you chose. This is the palette's v0. The command registry comes later (P1).

## Acceptance criteria

These criteria are a draft: the three questions below settle their details before this card is Ready.

1. Ctrl+K anywhere in the league shell opens a Fluent dialog with its search box focused. At the main menu nothing opens.
2. Typing finds players by name through `PlayerQueries.SearchPlayers`, with your team as the viewer. It also finds teams by city, nickname or abbreviation.
3. The arrow keys move through the results. Enter closes the dialog and goes to the result: a player's page (`/player/{id}`) or a team's (`/team/{id}`).
4. Escape closes the dialog. A query that matches nothing shows "No matches".
5. `SearchPlayers` (Application, on a real save) returns what the matching rule says, in the agreed order, and never more than the limit.

## Questions (answer before Ready)

1. **How does a name match?** I recommend that each word of the query starts a word of the name, ignoring case: "ja mi" finds "Jalen Mitchell". The alternative is any substring.
2. **How many results, in what order?** I recommend up to 4 teams, then up to 8 players, with players by OVR. The alternative is one list, by best match.
3. **Where does a player go?** I recommend the Player page, since the plan says "Enter navigates". The alternative is the peek.

## Surface

- `PlayerQueries.SearchPlayers…` (Application): its exact signature is settled with the questions.
- `UI/Shell/CommandPalette.razor`.
- Test IDs: `palette-search`, `palette-result`.
- The Ctrl+K handler lives in a small module beside `gfo-dom.js`. Fluent's elements can't be focused from .NET.

## Notes

- The plan's [WP10](../milestones/M1_THE_LEAGUE.md#wp10--command-palette-v0-and-the-m1-exit-sm).
- UI_DESIGN_SYSTEM §5.2 (`CommandPalette`, "stub in M1") and §8 (`Ctrl+K`).
- `SearchPlayers` arrives here with its first caller, per [D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) #4. Reads log nothing.

## Work

- [ ] You: answer the three questions
- [ ] Claude, at Ready: the tests and the shells
- [ ] You: `SearchPlayers` and the palette, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the palette checked in a visible pane in Dark and Light, and in the desktop app
- [ ] Docs: F-03 §6 (`SearchPlayers` as built); UI_DESIGN_SYSTEM §5.2 (`CommandPalette` as built)
- [ ] You: `./build.ps1` green, then one commit: `(feat) *shell* : jump to any player or team with ctrl+k (S-011)`

## Built

—
