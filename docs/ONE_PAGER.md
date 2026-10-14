# One-Pager: Gridiron Front Office

> **Status:** Draft v1 · 2026-10-13 · Working title (see [Q-22](OPEN_QUESTIONS.md#q-22-name--trademark-check))

## The pitch

**Gridiron Front Office is a modern pro football franchise simulator where you are the general manager.**

You don't play the games. You build the team that plays them: you scout, draft, sign, trade, develop players, manage the salary cap and set the weekly game plan. Then you watch it play out and live with the results.

When the stakes peak, the clock doesn't stop. If you have two minutes on the clock in the draft, you have two *real* minutes. Nothing pauses, and you can't go back.

---

## The opportunity

Football GM sims mostly fall into two camps:

| | Deep but dated | Modern but shallow |
|---|---|---|
| **Examples** | Front Office Football, Draft Day Sports: Pro Football | Football GM (browser), console franchise modes |
| **Strengths** | Rich contracts, scouting, believable statistical sims | Approachable, good-looking, quick sessions |
| **Weaknesses** | Interfaces from another era, steep learning curve, menus instead of moments | Thin front-office systems, little uncertainty, the GM role takes a back seat to playing games |

Nobody owns **deep, modern, and tense**: a GM sim with real front-office depth that looks and feels like premium 2026 software, with moments where your heart rate actually goes up.

> These descriptions of competitors are impressions, not a market study. Check them before writing any store-page copy.

---

## Who it's for

**Primary: the Armchair GM.** An adult football fan who follows the draft, free agency and cap news as closely as the games. Plays long dynasties of 10+ seasons. Wants decisions with consequences and outcomes that feel earned. Comfortable with dense information if it's well organized.

**Secondary: the lapsed sim player.** Has loved deep management sims (Football Manager, OOTP, FOF) but bounced off dated interfaces or no longer has the time. Wants depth delivered efficiently.

**Not for:** people who want to control players on the field, arcade or action fans, or daily-fantasy lineup players.

---

## Design pillars

Every feature has to serve at least one pillar. When two pillars conflict, the one listed first wins.

| # | Pillar | What it means | What it rules out |
|---|---|---|---|
| 1 | **Front office first** | Every system serves the GM fantasy. The user's levers are personnel, money, information and the game plan. | Play-calling, controlling players, a "coach mode" that competes with the GM role |
| 2 | **Pressure, not paperwork** | The key moments happen in real time. Decisions carry weight because they can't be undone. Routine work is fast and automatable. | A pause button during live events, reloading to redo a draft, tedious busywork |
| 3 | **Believable and explainable** | The sim is statistically grounded and calibrated to real-world distributions, and every result can be explained. | Physics, black-box outcomes, rubber-banding, AI that cheats |
| 4 | **Modern and elegant** | The app feels like 2026 software: fast, keyboard-friendly, dark-first, purposeful motion, dense but calm. | Default browser styling, modal mazes, spreadsheet walls with no hierarchy |
| 5 | **Your league, your story** | Long dynasties build history, records, rivalries and narratives worth remembering. | Disposable seasons, leagues that drift into nonsense over decades |

---

## Core loops

### Weekly loop (in season, about 5–15 minutes per week)
1. Read the **Front Office Briefing**: injuries, news, inbox items, the upcoming opponent.
2. Adjust the **depth chart** and set the **game plan** against this opponent's tendencies.
3. **Play the week.** Watch your game live in the Game Center or sim it instantly.
4. Review **results**: box score, Film Room report, standings, around the league.
5. Make **roster moves**: waivers, signings, trades (until the deadline).
6. **Advance.**

### Annual loop (offseason, about 60–120 minutes)
1. **Season review**: awards, owner evaluation, retirements.
2. **Re-sign, tag or let go** of expiring players. Clear cap space.
3. **Scouting season**: combine, pro days, interviews. Build your Big Board.
4. **Free agency**: competitive bidding against 31 AI front offices.
5. **The draft, live**: 7 rounds, a real clock, trade calls, no pause. About 20–40 minutes.
6. **Training camp**: development focus, camp battles, cut down to 53.

### Career loop (multi-season)
Build a dynasty, keep the cap healthy for years, satisfy (or outlast) your owner, and leave a legacy in the record books and the Hall of Fame.

---

## What makes it stand out

| # | Idea | Status |
|---|---|---|
| 1 | **The real-time draft.** The clock never stops (in the default Live mode; a Pausable mode can be chosen per save). AI picks come fast, your clock runs in real time, and trade calls arrive with expiring offers. | Core (owner's idea) |
| 2 | **Modern, elegant UI.** A team-tinted theme, command palette, side-sheet "peeks", broadcast-style moments. | Core (owner's idea) |
| 3 | **Scouting fog of war.** You never see true ratings, only your scouts' estimates, which are uncertain and sometimes biased. | **Proposed, strongly recommended as core** |
| 4 | **Live Events.** The draft's real-time engine reused for the Free Agency Frenzy and Trade Deadline Day. | Proposed |
| 5 | **Film Room.** Every result is explainable: key matchups, win probability swings, efficiency analytics. | Proposed |
| 6 | **The Owner's Box.** Owner personalities, mandates, job security. You can be fired and continue your career elsewhere. | Proposed |

Full design: [STANDOUT_FEATURES.md](STANDOUT_FEATURES.md).

---

## Scope

### In scope for 1.0
- Single-player, one user-controlled franchise, in a fictional 32-team league (2 conferences × 4 divisions).
- Full annual cycle: preseason → 17-game regular season → 14-team playoffs → offseason.
- Rosters (53 active + practice squad + IR), depth charts, weekly game plans.
- Contracts and salary cap (core rules), re-signing, free agency, waivers, trades.
- Scouting with fog of war, and the real-time draft.
- Player development, aging and retirement, plus injuries.
- 31 AI front offices that follow the same rules as the user.
- Stats, records, awards, history, and the Hall of Fame.
- A statistical play-by-play sim with a live Game Center and Film Room.
- Autosave with snapshots, and an optional Ironman mode.

### Explicitly out of scope for 1.0 (and why)

| Out | Why |
|---|---|
| Play-calling or controlling games | Breaks pillar 1. The user's role is GM + game plan ([D-009](DECISIONS.md#d-009-game-day-role-gm--game-plan)). |
| 2D/3D animated games | Huge cost, and it isn't the fantasy. A drive chart plus play-by-play is enough. |
| Real NFL teams, players or logos | Licensing. The game is built Steam-ready ([D-011](DECISIONS.md#d-011-release-posture-personal-first-steam-ready)). |
| Multiplayer or online leagues | A single-writer local SQLite design. It would be a different product. |
| A business sim (ticket prices, concessions, stadium building) | Classic scope trap that doesn't serve the GM fantasy ([Q-15](OPEN_QUESTIONS.md#q-15-how-deep-is-the-financial-simulation)). |
| A college football mode | A separate game. |
| Mobile | Desktop-dense UI. Windows first. |
| Configurable league size or season length | Breaks the schedule formula and multiplies test cases ([Q-13](OPEN_QUESTIONS.md#q-13-league-customization)). |

### Maybe later (post-1.0)
Commissioner/edit mode, modding and team packs, macOS, multiple user-controlled teams, halftime adjustments, a "RedZone" multi-game view, Steam Workshop.

---

## Platform & business

- **Platform:** Windows 10/11 x64 desktop, fully offline, single-player. No account. No telemetry by default.
- **Technology:** .NET 10 (LTS) with Blazor Hybrid (a WPF + BlazorWebView host, so modern HTML/CSS instead of XAML), SQLite (one file per save) via EF Core, the Fluent UI Blazor v5 component library with GFO's own football components, and Serilog file logging. See [ARCHITECTURE.md](ARCHITECTURE.md).
- **Release posture:** personal project first, built **Steam-ready**: fictional data only, permissively licensed assets and dependencies, an installer and updater, and versioned save migrations. Actual Steam integration waits until the "is it fun?" gates in [ROADMAP.md](ROADMAP.md).

---

## Success criteria

### Product: is it good?
- The author voluntarily plays "one more season" after the game stops being a dev task.
- The draft is the most memorable part of the year. Playtesters describe it as tense, not tedious.
- After 20+ seasons, the league still feels alive: varied champions, believable stat leaders, a functioning cap economy.
- Every loss can be explained in the Film Room without reading code.

### Technical: is it solid?
- 30 consecutive autoplayed seasons with no crash, no data corruption and no degenerate league state (see the league-health gates in [TESTING_AND_QUALITY.md](architecture/TESTING_AND_QUALITY.md)).
- League-wide stats land inside the calibration bands (see [SIMULATION_ENGINE.md](architecture/SIMULATION_ENGINE.md)).
- Performance budgets are met (see [ARCHITECTURE.md §9](ARCHITECTURE.md#9-performance-budgets)), including a full week simmed in under 3 s.
- A hard kill at any moment loses at most the last in-progress action.

---

## Biggest risks

| Risk | Why it's dangerous | Mitigation |
|---|---|---|
| **Scope** | A full football GM sim is a multi-year solo effort, and unfinished is the default outcome. | Close the annual loop first, then deepen. A clear Minimum Lovable Product. Something playable at every milestone, with the first playable build at M3. |
| **AI GM quality** | Exploitable AI ruins every GM sim: fleeced trades, cap hell, dumb drafts. | The AI uses the same rules and commands as the user. A shared valuation model. 30-season autoplay health checks start at M5. |
| **Sim believability** | If the stats look wrong, nothing else matters. | Build the engine headless first (M2). Calibrate it with SimLab before building UI on top. Tuning lives in a data file. |
| **Talent drift** | Over decades, leagues inflate into all-stars or deflate into nothing. | Balance generation and progression together, guarded by automated equilibrium tests. |
| **The draft isn't fun** | The hallmark feature fails. | A throwaway "Draft Room feel" prototype early (Spike S1). Fog of war gives the pick real stakes. |
| **The UI looks "generic Fluent"** | Adopting Fluent UI Blazor saves months, but a stock look undercuts "modern and distinctive" | Team-tinted brand theming, GFO's own football composites and broadcast moments, and a visual design doc that configures Fluent deliberately. The `FluentDataGrid` is validated early (Spike S0b), with a fallback grid behind the `DataGrid<T>` wrapper. |
| **Solo motivation** | Long stretches without visible progress. | Every milestone ends with something you can play or watch. |

---

## Minimum Lovable Product

**MLP = the end of M6.** Generate a league, play full seasons, manage contracts and the cap, re-sign and sign free agents, scout through fog of war, run the real-time draft, and repeat for 10+ seasons with a healthy league.

Everything after M6 deepens a game that is already fun. If it isn't fun at M6, stop and fix the core before adding anything else.
