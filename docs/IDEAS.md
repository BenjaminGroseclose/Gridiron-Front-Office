# Ideas Parking Lot

> **Status:** Living document · created 2026-10-14
>
> Ideas that are **not** on the roadmap. Writing one down here is how it stays out of the current milestone ([C-1](OPEN_QUESTIONS.md#c-1-scope-is-the-real-enemy)). Nothing here gets built without first moving into [FEATURES.md](FEATURES.md) or [ROADMAP.md](ROADMAP.md), normally at a gate or when a milestone's spec is written.
>
> **Add an idea:** one row, with where it came from and the signal that would make it worth revisiting. **Promote an idea:** move it into a feature or milestone and delete the row.

| Idea | Source | Why it's parked | Revisit when |
|---|---|---|---|
| **Randomize team identities** (generated names, colors, crests per league) | [D-033](DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames) | Needs a nickname generator and color-harmony logic for a rarely used feature. User import (M11) covers customization. | After M11, if replay variety is requested |
| **Preseason games** (3 simulated games, starters on limited snaps) | [D-038](DECISIONS.md#d-038-no-preseason-games-in-10) | No longer feeds fog of war. Costs snap-limit rules, separate stats and extra calendar weeks. | If camp feels hollow at Gate 1 |
| **Difficulty presets** (trade/negotiation strictness toward the user, scouting accuracy, owner patience) | [D-042](DECISIONS.md#d-042-no-difficulty-presets-in-10) | Team choice is the dial, and one AI code path is easier to make fleece-proof. Hooks (`UserDealMargin`, `UserScoutingAccuracy`) already exist. | Gate 2, if playtesters find the AI too tough |
| **Leadership trait and locker-room chemistry** | [D-043](DECISIONS.md#d-043-limited-personality-model-only-traits-with-a-consumer) | Opaque, hard to explain in the Film Room, easy to feel random. Nothing would read Leadership in 1.0. | Post-1.0, only with a clear explanation model |
| **Pro scouting assignments** (spend a scout to tighten a specific pro's range) | [D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted) | Other teams' players are slightly fuzzy by a fixed amount. Keeping it simple. | If trades or free agency feel like guessing at Gate 1 |
| **Pro perception that sharpens with snaps played** | [D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted) | "Too complex" (owner). | Only together with pro scouting above |
| **RFA tenders and offer sheets, the transition tag** | [D-040](DECISIONS.md#d-040-salary-cap-realism-tiers) | Lots of rules for a small slice of players. Exclusive rights cover the basic case. | Post-1.0 cap depth pass |
| **Void years, incentives (LTBE/NLTBE), offsets, Top 51, compensatory picks** | [D-040](DECISIONS.md#d-040-salary-cap-realism-tiers) | Realism with little GM decision value per hour of work. | Post-1.0 |
