# Open Questions & Challenges

> **Status:** Living document · last updated 2026-10-19
>
> Each question has a **recommendation** and a **decide-by** milestone. When one is answered, record it in [DECISIONS.md](DECISIONS.md), replace the body here with a pointer, and update every affected doc.
>
> **2026-10-14:** every question that could be decided before M0 was worked through in one session (D-032 … D-048). Only **Q-06, Q-22 and Q-23** remain open. They need M11 research or a reason to do macOS work.

---

## Part 1: Challenges (pushback on the plan)

These aren't questions so much as places where I think the plan needs pressure-testing.

### C-1. Scope is the real enemy
A complete football GM sim with contracts, scouting, a real-time draft, trades, AI GMs, a play-by-play engine, a Film Room, owners, coaching staffs and narrative is a **multi-year solo project**. Unfinished is the default outcome for projects like this, and the previous attempt is evidence. The roadmap's defense is **"close the loop, then deepen"**, with the MLP at M6 and an honest Gate 1. **Recommendation:** treat anything not on the current milestone as off-limits, and keep a `docs/IDEAS.md` parking lot so ideas don't leak into the current work.

> **Done (2026-10-14):** [IDEAS.md](IDEAS.md) exists, seeded with everything parked while answering the open questions (randomized team identities, preseason games, difficulty presets, Leadership and locker-room chemistry, RFA tenders, …).

### C-2. The real-time draft only works if the decision is hard
If true ratings are visible, the clock is just waiting. **Fog of war isn't optional polish. It's what makes your hallmark idea work.** The second risk is **tedium**: rounds 4–7 at full speed can drag, which is why AI acceleration exists. Build **Spike S1** (a throwaway Draft Room prototype) right after M1 to *feel* it before committing.

> **Resolved (2026-10-14):** fog of war is core ([D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted)). Prospects are very fuzzy and sharpen only through scouting, which is exactly where the draft clock needs it. Spike S1 is still the test for tedium.

### C-3. "No pause" conflicts with accessibility and real life
Doorbells, kids, a slower reader, a motor impairment. A strict real-time clock will frustrate some players. The accommodations are **clock presets** (Relaxed), a **pick queue**, auto-pick from your Big Board, and a fair crash/close policy.

> **Owner response (2026-10-13):** *"I wonder if we can make the 'No Pause' feature a setting for that given save."*
> **Answer: yes, it's cheap, and I recommend it with guardrails.** Proposed as [D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable): a **clock mode** chosen at league creation, either **Live** (the default: accelerate, never pause) or **Pausable**. It's **locked after creation** so pausing doesn't become a crutch for hard picks, **Ironman requires Live**, and a LIVE badge appears on the save card. Pause reuses the resume path that live events already need, so the engineering cost is small. The one remaining challenge is to keep Live the default and the identity. If most players pick Pausable, the hallmark becomes optional.
>
> **Resolved (2026-10-13):** the owner chose to lock it ("I like making it a lock option"). [D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable) is accepted.
>
> **Resolved (2026-10-14):** presets are changeable before the draft ([D-044](DECISIONS.md#d-044-draft-clock-presets-tiered-changeable-before-the-draft)), a close or crash resumes where it left off ([D-045](DECISIONS.md#d-045-live-event-close-or-crash-resume-where-it-left-off)), and no separate accessibility preset is needed because Pausable covers it.

### C-4. MAUI is more framework than you need
For a Windows-only game, MAUI mostly hosts a WebView, and it brings heavy tooling and churn.

> **Owner response (2026-10-13):** *"If MAUI is too heavyweight then I am fine removing it. I left it in so that we can leverage MAUI Blazor (modern HTML, CSS, etc.) as compared to XAML."*
> **Answer: remove it.** The HTML/CSS benefit comes from **Blazor + `BlazorWebView`**, which Microsoft also ships for **WPF**, with the same Razor components and the same WebView2 engine. You keep 100% of the modern UI and drop the MAUI workload, the Windows App SDK packaging and the build overhead. Proposed as [D-021](DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed). If macOS ever matters ([Q-06](#q-06-macos-ever)), a MAUI or Photino host can be added next to it, because the host is thin.
>
> **Resolved (2026-10-13):** MAUI removed. [D-021](DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed) is accepted. Measured benefits: [HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md). It's a big dev-loop win, and runtime is roughly equal.

### C-5. Building every component yourself has a real price
A good **DataGrid** (virtualized, sticky columns, multi-sort, saved views, keyboard navigation) is weeks of work. So are accessible popovers, menus and a drag-and-drop depth chart.

> **Owner response (2026-10-13):** *"I would be open to using an already existing DataGrid / component library. Can you provide a few options that I could review online?"*
> **Answer:** see [UI_COMPONENT_OPTIONS.md](architecture/UI_COMPONENT_OPTIONS.md). It has three strategies (in-house, hybrid, full library), a shortlist with demo links (Radzen, Fluent UI Blazor, QuickGrid, MudBlazor, plus commercial options), and an evaluation checklist. **My lean: the hybrid approach.** Adopt a DataGrid, keep the rest in-house. The decision is [Q-29](#q-29-component-library-strategy).
>
> **Resolved (2026-10-13):** *"I have always liked Fluent UI so let's go with that option."* [D-025](DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5): **Fluent UI Blazor v5** as the full library, with theming detailed in [UI_DESIGN_SYSTEM.md §3](architecture/UI_DESIGN_SYSTEM.md#3-theming-with-fluent-ui-blazor-v5).

### C-6. The AI decides whether the game is good
Every GM sim lives or dies on whether the AI can be fleeced and whether the league stays healthy over decades. Most hobby sims bolt the AI on last. **Here the AI ships with each feature**, uses the same rules as the player, and is gated by a 30-season autoplay from M5.

> **Reinforced (2026-10-14):** with no difficulty presets ([D-042](DECISIONS.md#d-042-no-difficulty-presets-in-10)), the AI judges your deals with exactly the logic it uses with other AI teams. There's one code path to make fleece-proof.

### C-7. "Modern and elegant" vs. "data-dense"
GM players *want* big tables. Modern apps *want* whitespace. Pretty-but-sparse screens will frustrate your target player, and spreadsheet walls will betray the pillar. The answer is **hierarchy and density control**: compact mode, sticky columns, peek sheets and a strong type scale. Not fewer numbers.

> **Owner response (2026-10-13):** *"I think there is a way to blend these two options to be data dense with a modern UI."*
> **Agreed, and that's the target.** The concrete techniques for blending them are now in [UI_DESIGN_SYSTEM.md §2.1](architecture/UI_DESIGN_SYSTEM.md#21-data-dense-and-modern-how-we-blend-them). They also feed the DataGrid evaluation (compact density and custom cells are on the checklist).

### C-8. Saves in `Documents` are a trap
A live SQLite database in a OneDrive-synced folder is a corruption risk.

> **Owner response (2026-10-13):** *"Sounds good to me."* **Resolved:** [D-005](DECISIONS.md#d-005-saves-live-in-localappdata-not-documents) is accepted. Saves go in `LocalAppData`, with Export for sharing and backups.

---

## Part 2: Questions

### Decide-by summary

| Decide before | Open questions |
|---|---|
| **M0 … M10** | *None open.* |
| **M11** | [Q-22](#q-22-name--trademark-check), [Q-23](#q-23-steam-deck) |
| **Before any macOS work** | [Q-06](#q-06-macos-ever) |

**Resolved:** Q-04, Q-05, Q-09, Q-24, Q-25, Q-28, Q-29, Q-30 (2026-10-13) and Q-01, Q-02, Q-03, Q-07, Q-08, Q-10 … Q-21, Q-26, Q-27 (2026-10-14).

---

### Q-01: Adopt scouting fog of war as a core system?
- **Resolved (2026-10-14):** yes. Your roster is exact, other teams' players are slightly fuzzy and never sharpen, and prospects are very fuzzy and sharpen only through scouting → [D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted).

### Q-02: Default team set: curated or generated?
- **Resolved (2026-10-14):** 32 curated franchises in real cities with original nicknames. "Randomize" is parked → [D-033](DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames).

### Q-03: Ratings display scale
- **Resolved (2026-10-14):** 0–99 with color bands and ranges, letter grades for potential → [D-034](DECISIONS.md#d-034-ratings-display-099-with-ranges-letter-grades-for-potential).

### Q-04: What does "sharable UI components" mean?
- **Resolved (2026-10-13):** *"Shared components are for this app only"* → [D-027](DECISIONS.md#d-027-ui-components-are-for-this-app-only). The project stays separate for dependency hygiene, not for reuse.

### Q-05: Project naming
- **Resolved (2026-10-13):** `GridironFrontOffice` → [D-028](DECISIONS.md#d-028-project-naming-gridironfrontoffice). Full names for projects and namespaces, short folder names, and the short `Gfo`/`gfo-`/`.gfo` forms are kept for types, CSS and files.

### Q-06: macOS, ever?
- **Decide by:** before any macOS work (no longer blocks M0)
- **Context:** with the host moving to WPF ([D-021](DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed)), macOS would mean adding a **second thin host** (MAUI Blazor Hybrid or Photino.Blazor). The RCL architecture keeps that small.
- **Recommendation:** "maybe later". Avoid Windows-only APIs outside `GridironFrontOffice.Desktop` (platform features sit behind `IPlatformServices`). Note that some modern CSS (anchor positioning) arrived later in Safari/WKWebView, so keep a small JS fallback for positioning.

### Q-07: Draft clock presets
- **Resolved (2026-10-14):** tiered presets (times tentative until Spike S1), changeable in the offseason before the draft, never in Ironman → [D-044](DECISIONS.md#d-044-draft-clock-presets-tiered-changeable-before-the-draft).

### Q-08: App closed while on the clock
- **Resolved (2026-10-14):** resume where it left off, with the remaining time → [D-045](DECISIONS.md#d-045-live-event-close-or-crash-resume-where-it-left-off). (Restarting the draft was considered and rejected.)

### Q-09: Ironman mode and restoring before the draft
- **Resolved (2026-10-13):** *"yes, Ironman requires the Live clock mode. It's a one-line rule and keeps 'Ironman' meaning maximum stakes."* → [D-026](DECISIONS.md#d-026-ironman-mode-requires-live-clock-mode). It's a toggle at creation, locked, with no chosen-snapshot restores or duplicates and automatic corruption-only recovery. Estimated at about 2 days in M5. Rollback detection is deferred until Steam.

### Q-10: Accessibility vs. "no pause"
- **Resolved (2026-10-14):** the per-save clock mode covers it. No separate accessibility preset → [D-044](DECISIONS.md#d-044-draft-clock-presets-tiered-changeable-before-the-draft).

### Q-11: Live Events beyond the draft
- **Resolved (2026-10-14):** FA Frenzy is committed, and Trade Deadline Day is conditional on FA Frenzy proving the pattern → [D-046](DECISIONS.md#d-046-live-events-fa-frenzy-committed-deadline-day-conditional).

### Q-12: Salary cap realism depth
- **Resolved (2026-10-14):** v2 is the 5th-year option, restructures and June 1 designations. No RFA tenders (exclusive rights instead) → [D-040](DECISIONS.md#d-040-salary-cap-realism-tiers).

### Q-13: League customization
- **Resolved (2026-10-14):** fixed structure with gameplay-only creation settings and no cap knobs → [D-036](DECISIONS.md#d-036-league-settings-at-creation-are-gameplay-only-one-user-team).

### Q-14: Single vs. multiple user-controlled teams
- **Resolved (2026-10-14):** a single team for 1.0 → [D-036](DECISIONS.md#d-036-league-settings-at-creation-are-gameplay-only-one-user-team).

### Q-15: How deep is the financial simulation?
- **Resolved (2026-10-14):** an owner-funded staff budget in M10 only, with equal scouting capacity until then → [D-041](DECISIONS.md#d-041-finances-owner-funded-staff-budget-in-m10-only).

### Q-16: Difficulty: what changes?
- **Resolved (2026-10-14):** no difficulty presets in 1.0. Team choice is the dial → [D-042](DECISIONS.md#d-042-no-difficulty-presets-in-10).

### Q-17: Injury frequency
- **Resolved (2026-10-14):** Low / Realistic / High, locked at creation → [D-037](DECISIONS.md#d-037-injury-frequency-setting-locked-at-creation).

### Q-18: Real college names
- **Resolved (2026-10-14):** fictional by default. Teams and colleges are data files, and user import ships in M11 → [D-035](DECISIONS.md#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11).

### Q-19: Player personality and locker room depth
- **Resolved (2026-10-14):** limited, and only traits something reads. Extensible → [D-043](DECISIONS.md#d-043-limited-personality-model-only-traits-with-a-consumer).

### Q-20: Preseason games
- **Resolved (2026-10-14):** none in 1.0 → [D-038](DECISIONS.md#d-038-no-preseason-games-in-10).

### Q-21: Sound and music
- **Resolved (2026-10-14):** all sounds off by default, no music → [D-047](DECISIONS.md#d-047-sound-all-off-by-default-no-music).

### Q-22: Name / trademark check
- **Decide by:** M11 (before any public page)
- **Context:** "Gridiron Front Office" is similar in wording to the established title *Front Office Football*. That may be fine, but it should be checked. The shipped team set (real city names, original nicknames, [D-033](DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)) should be checked at the same time.
- **Recommendation:** do a trademark search before any store page or public repo name. Keep "GFO" as the working codename either way. *(Not legal advice.)*

### Q-23: Steam Deck
- **Decide by:** M11
- **Context:** Steam Deck runs Windows games through Proton. WebView2-based apps have historically had mixed results under Proton/Wine.
- **Recommendation:** research and test before promising Deck support. Native Linux would need a different host.

### Q-24: AI-assisted development conventions
- **Resolved (2026-10-13):** yes → [D-029](DECISIONS.md#d-029-claudemd-at-the-repo-root-for-ai-assisted-development). The root [`CLAUDE.md`](../CLAUDE.md) is written.

### Q-25: Source hosting and CI
- **Resolved (2026-10-13):** *"Let's define this as a feat / task for later."* → [F-30](FEATURES.md#f-30-source-hosting--ci) and [D-030](DECISIONS.md#d-030-source-hosting-and-ci-deferred-to-f-30). The public/private and issue-tracker questions move into F-30. Until then, `build.ps1` runs the gates locally.

### Q-26: Play-by-play retention
- **Resolved (2026-10-14):** keep everything and measure. A pre-agreed fallback applies if the budget is breached → [D-039](DECISIONS.md#d-039-play-by-play-is-kept-measure-before-pruning).

### Q-27: Halftime adjustments
- **Resolved (2026-10-14):** post-1.0, with the engine's halftime split kept possible from M2 → [D-048](DECISIONS.md#d-048-halftime-adjustments-post-10-engine-split-kept-possible).

### Q-28: What went wrong with SQLite transactions before?
- **Answered (2026-10-13):** *"I think I implemented it wrong, let's be sure to detail and make it clear how to utilize it in this app."* → **[DATA_ACCESS_GUIDE.md](architecture/DATA_ACCESS_GUIDE.md)**: the mental model, the seven likely past mistakes (each shown wrong and then fixed), the full `SaveWriter`/`UnitOfWork` implementation, usage patterns, an error-message troubleshooting table, analyzer enforcement, torture tests and a PR checklist.

### Q-29: Component library strategy
- **Resolved (2026-10-13):** Fluent UI Blazor v5 → [D-025](DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5).

### Q-30: Data access library
- **Resolved (2026-10-13):** EF Core with the unit-of-work rules → [D-024](DECISIONS.md#d-024-data-access-ef-core-with-the-gfo-unit-of-work). Spike S0a becomes an *EF Core proof* (torture tests + bulk timing) rather than a library bake-off.

### Q-31: Screen readers can't see the page (the composition control)
- **Decide by:** M1, before the first real screens (it may touch [D-068](DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar))
- **Context (found in M0 WP9):** the .NET 10 WPF `BlazorWebView` hosts WebView2's *composition* control, which renders the page as an image ([HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md)). The window's UI Automation tree holds only an `Image` and the three WPF caption buttons. Narrator and other screen readers, and UIA-based test tools, can't reach any page content, so the ARIA labels the Definition of Done asks for don't reach anyone. Keyboard use, contrast, reduced motion and color-blind safety aren't affected.
- **Options:**
  1. **The HWND-based WebView2 control**, if `BlazorWebView` can still be given one on .NET 10. Its accessibility is native. But WPF can no longer draw over it (airspace), so the caption buttons would move into HTML, and Snap Layouts would rely on WebView2's own non-client regions (`app-region`), which S0c couldn't use under the composition control. This reopens part of D-068.
  2. **Bridge UIA** from the composition control to the hidden browser's automation provider. Possibly heavy, and not something the control offers today.
  3. **Accept it for 1.0**: state that screen readers aren't supported, keep everything else, and revisit with the F-27 audit in M11.
- **Recommendation:** a one-day spike on option 1 (does it load, do Snap Layouts and drag still work, does Narrator read the page?) before M1 builds screens, and fall back to option 3 if it fails. Until then the current host stays.
