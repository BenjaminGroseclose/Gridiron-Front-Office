# S-005 · A player's full profile

> **Kind:** Story · **Feature:** F-03 · **Milestone:** M1 · **Size:** M · **Needs:** S-003

## Outcome

`/player/{id}` shows everything F-03 generates for a player: the ratings by group, the measurables, the bio and the personality, under a band in their team's colors. The contract block is S-006.

## Acceptance criteria

1. **The band** is in the player's team colors (neutral for a free agent). It holds:
   - the name, as the page's `h1`;
   - `PositionPill` and the jersey number;
   - a `TeamLink` to the team (none for a free agent);
   - the OVR and potential chips.
2. **Ratings:** the eight groups in F-03 §4.2's order. Each attribute is a `RatingBar` labeled with its name, 42 in all.
3. **`AttributeGroups`** (Core):
   - every attribute has a group;
   - the groups hold 6, 2, 7, 4, 4, 3, 10 and 6 attributes, in the enum's order;
   - `Of` and `Members` agree.
4. **`PlayerText`:** every attribute, group, personality trait and trait level has a name of its own, and each one throws on a value it doesn't know.
5. **Measurables:**
   - height in feet and inches (74 is 6'2");
   - weight in pounds;
   - the timed drills in seconds, with two decimals;
   - bench in reps;
   - vertical, arm and hand in inches, with one decimal;
   - broad jump in feet and inches.

   Decimals follow the current culture, de-DE included.
6. **Bio:**
   - age and birth date, hometown, college, experience;
   - the draft as "2023 · Round 2, pick 41 · Boston", with the drafting team a `TeamLink`, or "Undrafted".
7. **Personality:** each trait's level for your own player. "Unknown" for a rival's player, and before the pick.
8. **An unknown id:** `/player/99999` shows an empty state with a link to the League page.
9. **Switching players:** going from one player to another loads the second, and a stale load is dropped.
10. **No league open:** the page goes to the main menu.
11. **The gallery:** `/_gallery/player` shows the page's blocks from sample DTOs: your player, a rival's, a free agent, a rookie and an undrafted player. Checked in the pane.

## Surface

- `Core/Players/AttributeGroups.cs`: `Of(this PlayerAttribute)` and `Members(AttributeGroup)`.
- `UI/Football/PlayerText.cs`: `Text()` for `PlayerAttribute`, `AttributeGroup`, `PersonalityTrait` and `TraitLevel`, and `Measurable(Measurable, double)`.
- `Routes.Player(PlayerID)`.
- `UI/Players/PlayerPage.razor`: `@page "/player/{ID:int}"`.
- Test IDs: `ratings` (each group a `[data-group]` section), `measurables`, `bio`, `personality`, `player-missing`.

## Notes

- **Spec:** [F-03 §8](../features/F-03_PLAYERS.md#8-ui) with §4.2 (the groups), §4.3 (measurables), §4.7 (personality) and §4.8 (bio).
- **Decisions:** [D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) #2 (whose personality shows).
- **The guide:** [WP9 guide](../milestones/M1_WP9_GUIDE.md#2-decisions-this-guide-makes) §2, the rows "The Player page", "Attribute groups" and "The team of a row".
- **Code to follow:**
  - `AttributeGroups`: §3.3 has the sketch.
  - Load pattern: S-003's.
  - Team colors: they come from `GetTeamIdentitiesAsync`, read once.
- **How it's reached:** by URL only until S-007 makes every name open a peek with "Open full profile".

## Work

- [ ] Claude, at Ready: the tests and the shells
- [ ] You: `AttributeGroups`, `PlayerText` and the page, until the tests pass
- [ ] Claude, after:
  - each test proven to fail on a planted bug;
  - `/_gallery/player`;
  - the page checked in a visible pane in Dark and Light, for your player, a rival's and a free agent.
- [ ] Docs: F-03 §8, the Player page as built
- [ ] You: `./build.ps1` green, then one commit: `(feat) *players* : show a player's full profile (S-005)`

## Built

—
