# S-007 · Every player name opens the peek

> **Kind:** Story · **Feature:** F-03 · **Milestone:** M1 · **Size:** M · **Needs:** S-004, S-006

## Outcome

Every player name in the league's screens opens a peek sheet. The sheet shows enough to judge the player, and "Open full profile" goes to their page. This is one of M1's exit items.

## Acceptance criteria

1. `PlayerLink` is a button styled as a link, with `aria-haspopup="dialog"` and the player's name as its text. A click opens the peek, titled with the name.
2. The sheet shows:
   - position, number, team and age;
   - the OVR and potential chips, with the archetype;
   - height, weight, college and experience;
   - the contract line (`ContractText.Line`), which a free agent doesn't have.
3. "Open full profile" closes the sheet, then lands on `/player/{id}`.
4. The team's name in the sheet closes it too, then lands on `/team/{id}`. This works through `TeamLink`'s `BeforeNavigate`.
5. An unknown player shows a toast, "That player isn't in this league", not an empty sheet.
6. The names on the Team page (the top cap hits and the roster) and on Home (the top five) are `PlayerLink`s.
7. `/_gallery/player` adds the sheet for your player, a rival's and a free agent. Checked in the pane.

## Surface

- `UI/Football/PlayerLink.razor`, with parameters `Player` (`PlayerID`) and `Name`. Its class is `.gfo-player-link`.
- `UI/Players/PeekService.cs` (scoped in `AddGfoUi`), with `OpenPlayerAsync(PlayerID)`.
- `UI/Players/PlayerPeekSheet.razor`, with parameters `Player` (`PlayerDetailDto`) and `Team` (`TeamIdentityDto?`). The test ID `open-profile` is on its button.
- `TeamLink` gains `BeforeNavigate` (`Func<Task>?`). With one, the link prevents its default, awaits the callback, then navigates.

## Notes

- UI_DESIGN_SYSTEM §7 ("peek, then commit").
- The [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2 ("Player names", "The peek") has the rules, and §3.2 sketches `PlayerLink` and `PeekService`.
- §7 of the guide: close the drawer before navigating, or it stays open over the next page.
- How the sheet reaches its `DialogInstance` to close itself (`FluentDialogBody`'s `Instance`) is checked against Fluent 5.0.1 at Ready.
- In a grid, a name's click must not also select the row. That's S-008, where the grid arrives.

## Work

- [ ] Claude, at Ready: the tests and the shells
- [ ] You: `PlayerLink`, `PeekService`, the sheet and `BeforeNavigate`, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the sheet in `/_gallery/player`; checked in a visible pane in Dark and Light, from the Team page and Home
- [ ] Docs: UI_DESIGN_SYSTEM §7 (the peek) and §5.4 (`PlayerLink`, `TeamLink`'s `BeforeNavigate`)
- [ ] You: `./build.ps1` green, then one commit: `(feat) *players* : open a peek sheet from every player name (S-007)`

## Built

—
