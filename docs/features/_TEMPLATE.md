# F-xx: <Feature Name>

> **Status:** Draft | Proposed | Accepted | Built · **Milestone:** Mx · **Owner:** Ben · **Last updated:** YYYY-MM-DD
>
> Copy this file to `docs/features/F-xx_SHORT_NAME.md` right before the feature's milestone starts. Delete sections that don't apply. Keep it honest and short where you can.

---

## 1. Summary
One paragraph: what this is and why the player cares. Link the pillar(s) it serves ([ONE_PAGER.md](../ONE_PAGER.md#design-pillars)).

## 2. Goals / non-goals
**Goals**
- …

**Non-goals (explicitly out for this milestone)**
- …

## 3. Player stories
- As the GM, I want … so that …
- As the GM, I want … so that …

## 4. Rules and formulas
The authoritative rules. Prefer tables and pseudo-code. Every rule here should map to a unit test.

| Rule | Detail | Test name |
|---|---|---|
| … | … | `Feature_Rule_Scenario` |

## 5. Data model
- New or changed tables and columns (link to [PERSISTENCE.md](../architecture/PERSISTENCE.md) conventions)
- Migration notes (data backfill? deterministic seed?)
- Read models (DTOs) the UI consumes

## 6. Application services
| Service | Commands (mutations) | Queries (read models) | Domain events raised |
|---|---|---|---|
| … | … | … | … |

## 7. AI counterpart
How the 31 AI teams use this feature. Inputs (snapshot), decision logic, outputs (intents). Personality differences. **Exploit risks and guards.**

## 8. UI
- Screens and routes
- Wireframe (ASCII or image link)
- Components needed (existing vs. new in `GridironFrontOffice.UI.Components` / `GridironFrontOffice.UI`)
- Empty, loading and error states
- Keyboard shortcuts
- "Broadcast moments", if any

## 9. Calendar and phase integration
Which phases and stops this runs in, blocking issues it adds, step journal steps.

## 10. Edge cases
- …

## 11. Logging
Information-level lifecycle events, Warning conditions, and the event ID range used.

## 12. Performance
Budgets for this feature's operations, and how they're measured.

## 13. Test plan
- Unit (Core rules)
- Application (SQLite temp file)
- Simulation or SimLab (statistical gates)
- UI (bUnit)
- Autoplay impact (league health metrics touched)

## 14. Open questions
- …

## 15. Decisions made while specifying
Link any new `D-xxx` entries in [DECISIONS.md](../DECISIONS.md).
