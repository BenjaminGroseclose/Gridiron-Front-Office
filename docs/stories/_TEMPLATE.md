# S-xxx · <The outcome, in a few words>

> **Status:** Draft · YYYY-MM-DD
>
> **Kind:** Story | Enabler | Bug · **Feature:** F-xx · **Milestone:** Mx · **Size:** S | M · **Needs:** S-xxx | —
>
> Copy this file to `docs/stories/S-xxx_SHORT_NAME.md`. IDs run in the order cards are written and are never reused. The status is one word (Draft, Ready, Building, Checking, Done) and the date the card got there. It's on this first line and on the milestone plan's board, and both change in the same edit ([D-104](../DECISIONS.md#d-104-a-cards-status-is-on-the-card-and-on-the-board-one-word-and-a-date)). Anything more than the word (what's blocking, what's left) goes in the card's Work list. An L is two cards. Keep it short: the rules live in the feature spec, the choices in DECISIONS.

## Outcome

One or two sentences: what's true for the player once this is done (for an enabler, what's true for the code or the numbers). "As the GM, I want … so that …" only if it helps.

## Acceptance criteria

Numbered. Each one becomes a test (an enabler's can be a measurement). At Ready, Claude appends the test's name to each line (`→ TeamPageTests.Unknown_id_shows_the_empty_state`), so the card and the tests stay one list. Something a test can't hold (a layout at a width, a look in both modes) says "checked in the pane".

1. …

## Surface

The names the tests depend on, pinned before the build: routes, components and their parameters, `data-testid`s, the CSS classes a test reads, new members on DTOs or queries. At Ready, Claude writes these as empty shells (a route and parameters, no markup) beside the failing tests, so the solution compiles. Renaming one later is a change to this card first.

- …

## Notes

Links into the spec, DECISIONS and earlier cards. A code sketch only when it settles a choice (a race, a pattern, a name).

## Work

- [ ] Claude, at Ready: the tests and the shells
- [ ] You: the code, until the tests pass
- [ ] Claude: each test proven to fail on a planted bug; the screen checked in a visible pane in Dark and Light with several team colors ([VISUAL_DESIGN.md §10](../VISUAL_DESIGN.md#10-qa-checklist-for-every-new-screen)); the gallery; the docs
- [ ] Docs: …
- [ ] You: `./build.ps1` green, then one commit: `(type) *module* : summary (S-xxx)`

## Built

Filled at Done: what changed from the card and why, the numbers measured, the plants and what caught them.
