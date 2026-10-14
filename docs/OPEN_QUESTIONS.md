# Open Questions & Challenges

> **Status:** Living document · 2026-10-13
>
> Each question has a **recommendation** and a **decide-by** milestone. When one is answered, record it in [DECISIONS.md](DECISIONS.md), replace the body here with a pointer, and update every affected doc.

---

## Part 1: Challenges (pushback on the plan)

These aren't questions so much as places where I think the plan needs pressure-testing.

### C-1. Scope is the real enemy
A complete football GM sim with contracts, scouting, a real-time draft, trades, AI GMs, a play-by-play engine, a Film Room, owners, coaching staffs and narrative is a **multi-year solo project**. Unfinished is the default outcome for projects like this, and the previous attempt is evidence. The roadmap's defense is **"close the loop, then deepen"**, with the MLP at M6 and an honest Gate 1. **Recommendation:** treat anything not on the current milestone as off-limits, and keep a `docs/IDEAS.md` parking lot so ideas don't leak into the current work.

### C-2. The real-time draft only works if the decision is hard
If true ratings are visible, the clock is just waiting. **Fog of war isn't optional polish. It's what makes your hallmark idea work** (see Q-01). The second risk is **tedium**: rounds 4–7 at full speed can drag, which is why AI acceleration exists. Build **Spike S1** (a throwaway Draft Room prototype) right after M1 to *feel* it before committing.

### C-3. "No pause" conflicts with accessibility and real life
Doorbells, kids, a slower reader, a motor impairment. A strict real-time clock will frustrate some players. The accommodations are **clock presets** (Relaxed), a **pick queue**, auto-pick from your Big Board, and a fair crash/close policy (see Q-07, Q-08, Q-10).

> **Owner response (2026-10-13):** *"I wonder if we can make the 'No Pause' feature a setting for that given save."*
> **Answer: yes, it's cheap, and I recommend it with guardrails.** Proposed as [D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable): a **clock mode** chosen at league creation, either **Live** (the default: accelerate, never pause) or **Pausable**. It's **locked after creation** so pausing doesn't become a crutch for hard picks, **Ironman requires Live**, and a LIVE badge appears on the save card. Pause reuses the resume path that live events already need, so the engineering cost is small. The one remaining challenge is to keep Live the default and the identity. If most players pick Pausable, the hallmark becomes optional.
>
> **Resolved (2026-10-13):** the owner chose to lock it ("I like making it a lock option"). [D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable) is accepted.

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

| Decide before | Questions |
|---|---|
| **M0** | *None open.* Q-04, Q-05, Q-24, Q-25, Q-28, Q-29 and Q-30 are resolved. |
| **M1** | [Q-01](#q-01-adopt-scouting-fog-of-war-as-a-core-system), [Q-02](#q-02-default-team-set-curated-or-generated), [Q-03](#q-03-ratings-display-scale), [Q-18](#q-18-real-college-names) |
| **M3** | [Q-13](#q-13-league-customization), [Q-14](#q-14-single-vs-multiple-user-controlled-teams), [Q-17](#q-17-injury-frequency), [Q-20](#q-20-preseason-games), [Q-26](#q-26-play-by-play-retention) |
| **M4** | [Q-12](#q-12-salary-cap-realism-depth) |
| **M5** | [Q-15](#q-15-how-deep-is-the-financial-simulation), [Q-16](#q-16-difficulty-what-changes), [Q-19](#q-19-player-personality-and-locker-room-depth) |
| **M6** | [Q-07](#q-07-draft-clock-presets), [Q-08](#q-08-app-closed-while-on-the-clock), [Q-10](#q-10-accessibility-vs-no-pause) |
| **M8+** | [Q-11](#q-11-live-events-beyond-the-draft), [Q-21](#q-21-sound-and-music), [Q-27](#q-27-halftime-adjustments) |
| **M11** | [Q-22](#q-22-name--trademark-check), [Q-23](#q-23-steam-deck) |
| **Before any macOS work** | [Q-06](#q-06-macos-ever) |

---

### Q-01: Adopt scouting fog of war as a core system?
- **Decide by:** M1 (it affects every rating display)
- **Options:** (a) Yes, core: hidden true ratings, perceived estimates with ranges everywhere. (b) Draft-only: hide prospect ratings, show exact ratings for NFL players. (c) No: show true ratings.
- **Recommendation: (a).** It makes the draft a skill test, creates a natural trade and FA market, and gives scouting meaning. (b) is a weaker compromise: it's cheaper, but trades and FA lose the "teams disagree" dynamic. Whatever you choose, build `RatingChip` with range support in M1. It costs nothing if σ = 0.

### Q-02: Default team set: curated or generated?
- **Decide by:** M1
- **Options:** (a) A hand-crafted default set of 32 fictional franchises (names, colors, crest style, cities). (b) Fully generated per league. (c) Both: a curated default plus a "randomize" option.
- **Recommendation: (c).** Hand-crafted identities feel like a real league and create memorable rivalries. Random generation adds replay variety. Writing 32 good team identities is a fun, bounded creative task, and a good evening's work.

### Q-03: Ratings display scale
- **Decide by:** M1
- **Options:** (a) 0–99 numbers (Madden-style). (b) Letter grades (A+…F). (c) The 20–80 scouting scale (authentic, but niche). (d) Stars (FOF-style potential).
- **Recommendation:** numbers **0–99** with **color bands** and **uncertainty ranges** for attributes and OVR. **Letter grades** for scouting summaries and potential ("Ceiling: B+"). Internally, everything is 0–100 floats.

### Q-04: What does "sharable UI components" mean?
- **Resolved (2026-10-13):** *"Shared components are for this app only"* → [D-027](DECISIONS.md#d-027-ui-components-are-for-this-app-only). The project stays separate for dependency hygiene, not for reuse.

### Q-05: Project naming
- **Resolved (2026-10-13):** `GridironFrontOffice` → [D-028](DECISIONS.md#d-028-project-naming-gridironfrontoffice). Full names for projects and namespaces, short folder names, and the short `Gfo`/`gfo-`/`.gfo` forms are kept for types, CSS and files.

### Q-06: macOS, ever?
- **Decide by:** before any macOS work (no longer blocks M0)
- **Context:** with the host moving to WPF ([D-021](DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed)), macOS would mean adding a **second thin host** (MAUI Blazor Hybrid or Photino.Blazor). The RCL architecture keeps that small.
- **Recommendation:** "maybe later". Avoid Windows-only APIs outside `GridironFrontOffice.Desktop` (platform features sit behind `IPlatformServices`). Note that some modern CSS (anchor positioning) arrived later in Safari/WKWebView, so keep a small JS fallback for positioning.

### Q-07: Draft clock presets
- **Decide by:** M6 (informed by Spike S1)
- **Proposal:** Relaxed (4:00 / 3:00 / 2:00), **Standard (2:00 / 1:30 / 1:00)**, Pressure (1:00 / 0:45 / 0:30) for Round 1 / Round 2 / Rounds 3–7. Chosen at league creation and **locked**.
- **Questions for you:** Is your "2 minutes" example the Round 1 length, or every round? Should the preset be changeable between seasons (not mid-draft)? *Recommendation:* changeable only in the offseason before the draft, and never in Ironman.
- **Relation to clock mode ([D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable)):** presets (how long) and clock mode (whether you can pause) are separate settings. A Pausable save still uses a preset.

### Q-08: App closed while on the clock
- **Decide by:** M6
- **Options:** (a) Resume with the remaining time saved at the last heartbeat. (b) Count the time while closed, which usually means an auto-pick. (c) Treat the close as an immediate auto-pick.
- **Recommendation: (a).** A crash should never cost a pick. The "exploit" (closing the app to think) is weak because you can't see the board while it's closed. Plus a 3-second "get ready" countdown on resume.

### Q-09: Ironman mode and restoring before the draft
- **Resolved (2026-10-13):** *"yes, Ironman requires the Live clock mode. It's a one-line rule and keeps 'Ironman' meaning maximum stakes."* → [D-026](DECISIONS.md#d-026-ironman-mode-requires-live-clock-mode). It's a toggle at creation, locked, with no chosen-snapshot restores or duplicates and automatic corruption-only recovery. Estimated at about 2 days in M5. Rollback detection is deferred until Steam.

### Q-10: Accessibility vs. "no pause"
- **Decide by:** M6
- **Update (2026-10-13):** largely answered by the per-save **clock mode** ([D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable)). Players who need to pause create a Pausable save.
- **Answered:** the clock mode is **locked at creation** (owner, 2026-10-13: "I like making it a lock option.").
- **Answered:** Ironman requires Live ([D-026](DECISIONS.md#d-026-ironman-mode-requires-live-clock-mode)).
- **Still open:** is a longer "Accessibility" preset (e.g., 8:00 / 6:00 / 4:00) still worth having, now that Pausable exists? *Recommendation:* no. Pausable covers it.

### Q-11: Live Events beyond the draft
- **Decide by:** M8 (built in M9)
- **Recommendation:** adopt **FA Frenzy** and **Trade Deadline Day** ([STANDOUT_FEATURES.md §B](STANDOUT_FEATURES.md#b-live-events-the-draft-engine-reused)), each **on by default and toggleable at league creation** (locked afterward). The draft is always live.

### Q-12: Salary cap realism depth
- **Decide by:** M4
- **Recommendation:** a tiered approach.
  - **v1 (M4):** base salary, signing bonus proration (≤ 5 years), guarantees, dead money, cap growth, rollover, minimum salaries, the rookie scale, the franchise tag, extensions.
  - **v2 (M9):** restructures, June 1 designations, the 5th-year option, RFA tenders, the transition tag.
  - **Maybe never:** void years, incentives (LTBE/NLTBE), offset language, the Top 51 rule, cash-spending floors.
- **Question:** which v2 items matter most to *you* as a player?

### Q-13: League customization
- **Decide by:** M3
- **Recommendation:** fixed structure for 1.0 ([D-014](DECISIONS.md#d-014-fixed-32-team-league-structure-for-10)). Expose only "soft" settings: salary cap amount and growth, injury frequency, and the draft clock preset.

### Q-14: Single vs. multiple user-controlled teams
- **Decide by:** M3
- **Recommendation:** a single team for 1.0. Multiple user teams complicate the "on the clock" model (you'd be on the clock for several teams), the inbox and fairness. Revisit with commissioner mode (F-28).

### Q-15: How deep is the financial simulation?
- **Decide by:** M5
- **Options:** (a) None: only the salary cap. (b) An owner-funded **staff budget** (scouting, medical, coaching). (c) A full business sim (tickets, concessions, stadium, revenue).
- **Recommendation: (b)** in M10. (c) is a classic scope trap that pulls away from the GM fantasy.

### Q-16: Difficulty: what changes?
- **Decide by:** M5
- **Recommendation:** presets (Rookie / Pro / All-Pro / Legend) that tune **AI trade strictness, negotiation strictness, scouting accuracy, owner patience and AI GM competence**. **Never** AI information cheating or sim rubber-banding (pillar 3).

### Q-17: Injury frequency
- **Decide by:** M3
- **Recommendation:** a realistic default plus a league setting (Low / Realistic / High), fixed at creation. Season-ending injuries to stars are realistic but feel bad. The setting lets players choose.

### Q-18: Real college names
- **Decide by:** M1
- **Context:** real university names (without logos, mascots or colors) are common in indie sports sims, but they're still real institutions. Fully fictional colleges are the most conservative choice for Steam.
- **Recommendation:** make the college list a **data file**. Ship a fictional or "generic" list by default (e.g., "Ridge State", "Coastal Tech") so the default build is clean, and allow a real-names list as a personal mod later. *(This isn't legal advice. Revisit at Gate 2.)*

### Q-19: Player personality and locker room depth
- **Decide by:** M5
- **Recommendation:** a limited personality model in 1.0. Traits affect **negotiation, development and retirement** only. Skip locker room chemistry and morale-driven performance swings: they're opaque, hard to explain in the Film Room, and easy to make feel random.

### Q-20: Preseason games
- **Decide by:** M3
- **Recommendation:** simulate 3 preseason games (instant, starters play limited snaps). They feed fog of war (performance signals for camp battles) and give rookies a first look. There's a "skip" option.

### Q-21: Sound and music
- **Decide by:** M8
- **Recommendation:** minimal UI sounds (confirm click, draft clock tick in the final 10 s, "the pick is in", game final), off by default. No music in 1.0. Assets must be original or CC0.

### Q-22: Name / trademark check
- **Decide by:** M11 (before any public page)
- **Context:** "Gridiron Front Office" is similar in wording to the established title *Front Office Football*. That may be fine, but it should be checked.
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
- **Decide by:** M3
- **Recommendation:** keep full play-by-play for the **last 3 seasons** (a setting: 1 / 3 / 5 / all). Keep box scores, stats and records forever.

### Q-27: Halftime adjustments
- **Decide by:** after M8
- **Context:** a natural extension of "GM + game plan" that stays GM-level: one plan tweak at halftime of your own game.
- **Recommendation:** post-1.0, but keep the engine's `Simulate(…, stopAt: Halftime)` / `Resume(…)` API possible from M2 so it stays cheap.

### Q-28: What went wrong with SQLite transactions before?
- **Answered (2026-10-13):** *"I think I implemented it wrong, let's be sure to detail and make it clear how to utilize it in this app."* → **[DATA_ACCESS_GUIDE.md](architecture/DATA_ACCESS_GUIDE.md)**: the mental model, the seven likely past mistakes (each shown wrong and then fixed), the full `SaveWriter`/`UnitOfWork` implementation, usage patterns, an error-message troubleshooting table, analyzer enforcement, torture tests and a PR checklist.

### Q-29: Component library strategy
- **Resolved (2026-10-13):** Fluent UI Blazor v5 → [D-025](DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5).

### Q-30: Data access library
- **Resolved (2026-10-13):** EF Core with the unit-of-work rules → [D-024](DECISIONS.md#d-024-data-access-ef-core-with-the-gfo-unit-of-work). Spike S0a becomes an *EF Core proof* (torture tests + bulk timing) rather than a library bake-off.
