# Decision Log

> A lightweight ADR log. Each entry records **what** was decided, **why**, and **what we gave up**.
>
> **Status values:** `Accepted` (owner decided or confirmed) · `Proposed` (recommended, awaiting confirmation) · `Superseded by D-xxx`
>
> To change a decision, add a new entry that supersedes it. Don't rewrite history.

| ID | Decision | Status |
|---|---|---|
| D-001 | Fresh start, prior history ignored | Accepted |
| D-002 | .NET 10 LTS + Blazor Hybrid, Windows-first | Accepted (Blazor/HTML/CSS) / Proposed (version) |
| D-003 | UI in Razor Class Libraries, thin host, DevHost | Proposed |
| D-004 | Autosave + snapshots save model | **Accepted** |
| D-005 | Saves in LocalAppData, not Documents | **Accepted** |
| D-006 | EF Core + raw ADO for bulk paths | Superseded by D-024 |
| D-007 | Serilog to rolling `.log` files | Accepted (requirement) / Proposed (library) |
| D-008 | UI components and assets: permissive licenses only | Accepted (licenses). Wrapping rule amended by D-025. |
| D-009 | Game day role: GM + game plan | **Accepted** |
| D-010 | Draft clock: AI fast, user clock real | **Accepted** (amended by D-022) |
| D-011 | Personal first, Steam-ready | **Accepted** |
| D-012 | Deterministic sim with in-house PRNG | Proposed |
| D-013 | Statistical play-by-play sim, no physics | Accepted |
| D-014 | Fixed 32-team structure for 1.0 | **Accepted** (via D-036) |
| D-015 | Integer keys + strongly typed IDs | Proposed |
| D-016 | Single-writer command queue | Proposed |
| D-017 | Fictional league and players only | Accepted (follows from D-011) |
| D-018 | Testing stack | Proposed |
| D-019 | Unpackaged + Velopack, Steam later | Proposed |
| D-020 | Feature folders, no mediator/mapper libraries | Proposed |
| D-021 | Host: WPF + BlazorWebView (MAUI removed) | **Accepted** |
| D-022 | Clock mode is a per-save setting (Live / Pausable), locked at creation | **Accepted** |
| D-023 | Fonts, icons and theme deferred to a visual design document | **Accepted** |
| D-024 | Data access: EF Core with the GFO unit of work | **Accepted** |
| D-025 | UI component library: Fluent UI Blazor v5 | **Accepted** |
| D-026 | Ironman mode (requires Live clock mode) | **Accepted** (rule) / Proposed (implementation levels) |
| D-027 | UI components are for this app only | **Accepted** |
| D-028 | Project naming: `GridironFrontOffice.*` | **Accepted** |
| D-029 | `CLAUDE.md` at the repo root for AI-assisted development | **Accepted** |
| D-030 | Source hosting and CI deferred to F-30 | **Accepted** |
| D-031 | Visual direction: Stadium (light + dark, dark by default) | **Accepted** |
| D-032 | Fog of war: your roster exact, other pros slightly fuzzy, prospects scouted | **Accepted** |
| D-033 | Default teams: 32 curated franchises, real cities, original nicknames | **Accepted** |
| D-034 | Ratings display: 0–99 with ranges, letter grades for potential | **Accepted** |
| D-035 | Fictional colleges; teams and colleges as data files, user import in M11 | **Accepted** |
| D-036 | League settings at creation are gameplay-only; one user team | **Accepted** |
| D-037 | Injury frequency setting, locked at creation | **Accepted** |
| D-038 | No preseason games in 1.0 | **Accepted** |
| D-039 | Play-by-play is kept; measure before pruning | **Accepted** |
| D-040 | Salary cap realism tiers | **Accepted** |
| D-041 | Finances: owner-funded staff budget in M10 only | **Accepted** |
| D-042 | No difficulty presets in 1.0 | **Accepted** |
| D-043 | Limited personality model; only traits with a consumer | **Accepted** |
| D-044 | Draft clock presets: tiered, changeable before the draft | **Accepted** |
| D-045 | Live event close or crash: resume where it left off | **Accepted** |
| D-046 | Live events: FA Frenzy committed, Deadline Day conditional | **Accepted** |
| D-047 | Sound: all off by default, no music | **Accepted** |
| D-048 | Halftime adjustments post-1.0, engine split kept possible | **Accepted** |

---

## D-001: Fresh start
- **Status:** Accepted (owner, 2026-10-13)
- **Context:** An earlier attempt exists in git history (MAUI Blazor, Application/Desktop projects, league wizard, schedule work).
- **Decision:** Start over. Earlier commits are ignored for design purposes. Nothing is carried over by default. Old code can be consulted later case by case, but it doesn't constrain the new design.
- **Consequences:** A clean architecture and docs-first approach. Some re-implementation of things that existed (league wizard, schedule).

## D-002: Platform: .NET 10 LTS + Blazor Hybrid, Windows-first
- **Status:** Blazor Hybrid (HTML/CSS UI instead of XAML) accepted by the owner. .NET 10 version proposed. The host framework is covered by D-021.
- **Context:** The owner has shipped projects successfully with MAUI Blazor Hybrid and wants modern HTML/CSS for the UI rather than XAML. The game is a data-dense desktop app.
- **Decision:** .NET 10 (LTS, supported until Nov 2028). The UI is Blazor rendered in a native window through **BlazorWebView** (WebView2 on Windows), targeting Windows 10/11 x64 first. Stay on .NET 10 through 1.0 unless a blocking need appears. Don't chase .NET 11 (STS).
- **Consequences:** + Full modern HTML/CSS, Razor components, the .NET ecosystem. − A WebView memory footprint (~150–300 MB).
- **Alternatives:** see [ARCHITECTURE.md §11](ARCHITECTURE.md#11-alternatives-considered-host-framework).

## D-003: UI lives in Razor Class Libraries; the host stays thin
- **Status:** Proposed
- **Decision:** All UI lives in `GridironFrontOffice.UI.Components` (generic design system) and `GridironFrontOffice.UI` (game screens). `GridironFrontOffice.Desktop` only composes services and hosts the WebView. A `GridironFrontOffice.DevHost` (Blazor Web App) hosts the same UI in a browser for fast iteration, the component gallery and Playwright tests. Platform features sit behind `IPlatformServices`.
- **Consequences:** + Switching hosts takes about a day. + Browser hot reload and DevTools. + Testable with bUnit. − One extra project to maintain. − Small behavior differences between Interactive Server (DevHost) and Hybrid, such as JS interop latency and scoped lifetimes.

## D-004: Save model: autosave + snapshots
- **Status:** **Accepted** (owner, 2026-10-13)
- **Context:** If players can reload freely, the no-pause draft can be save-scummed and the pressure disappears.
- **Decision:** One SQLite file per league (`.gfo`) is the live database. Every command commits immediately, and there's no Save button. Automatic snapshots (`VACUUM INTO`) at phase boundaries can be restored deliberately through a Time Machine screen. **Restore, duplicate and export are disabled during live events.** Snapshots are taken *before* the draft starts, never mid-draft.
- **Consequences:** + Crash-safe and simple to reason about. + The pressure is preserved. − Restoring the pre-draft snapshot still lets a player redo a whole draft (accepted; Ironman mode closes that gap, [D-026](#d-026-ironman-mode-requires-live-clock-mode)). − Snapshot disk usage (handled by a budget and pruning).
- **Alternatives rejected:** strict Ironman only (too harsh if a bug corrupts a league), traditional save/load (defeats the identity).

## D-005: Saves live in LocalAppData, not Documents
- **Status:** **Accepted** (owner, 2026-10-13: "Sounds good to me.")
- **Context:** OneDrive's Known Folder Move redirects `Documents` to a synced folder on many PCs. A live SQLite database with WAL side files in a sync folder risks lock conflicts and corrupt synced copies.
- **Decision:** Saves, snapshots, logs and settings live under `%LOCALAPPDATA%\GridironFrontOffice\`. Users share and back up saves through **Export/Import** (a consistent single-file copy) and an **Open saves folder** button.
- **Consequences:** + Safe from sync tools. − Less discoverable than `Documents\My Games` (mitigated by in-app buttons). Steam Cloud, if added, syncs snapshots and exports, not the live file.

## D-006: EF Core for the model, raw ADO for bulk paths
- **Status:** **Superseded by [D-024](#d-024-data-access-ef-core-with-the-gfo-unit-of-work)** (the owner chose EF Core with the unit-of-work rules after reviewing the options).
- **Original proposal (kept for reference):**
- **Decision:** EF Core 10 (SQLite provider) for the domain model, read-model projections and migrations. Raw `SqliteCommand` prepared statements for high-volume inserts (player game stats, play logs, scouting reports). `ExecuteUpdate`/`ExecuteDelete` for set-based changes. No `decimal` (money is `long` dollars), and `DateOnly` for in-game dates.
- **Consequences:** + Productivity where it matters, speed where it matters. − Two data access styles to keep consistent (both sit behind `ISaveWriter`).
- **Alternatives:** Dapper everywhere (more SQL to hand-maintain, and no migrations story), EF only (too slow for weekly bulk writes).

## D-007: Logging: Serilog to rolling .log files
- **Status:** Requirement accepted (owner: "log to a .log file"). Library proposed.
- **Decision:** `ILogger<T>` in all code. Serilog configured only in the composition root. An async rolling file sink at `%LOCALAPPDATA%\GridironFrontOffice\logs\gfo-YYYYMMDD.log` (daily, plus a new file at 10 MB, 14 retained). A separate opt-in sim trace log. Global crash capture, including JS errors from the WebView. See [LOGGING.md](architecture/LOGGING.md).
- **Alternatives:** a custom MEL file provider (reinventing rolling and retention), NLog (equally fine; Serilog chosen for its ecosystem and familiarity).

## D-008: UI components and assets: permissive licenses only
- **Status:** License rule accepted. In-house vs. third-party was resolved by [D-025](#d-025-ui-component-library-fluent-ui-blazor-v5) (Fluent UI Blazor v5). The second standing rule below is **amended by D-025** to selective wrapping.
- **Decision (standing):** Any component library, font, icon set or other bundled asset must be usable in a commercial Steam release at no cost and without revenue caps we might plausibly hit. MIT, Apache, ISC and SIL OFL are preferred. Re-check and record the license at the moment of adoption.
- **Decision (standing):** Screens depend on GFO's own component names (`<DataGrid>`, `<Button>`) even if a vendor library sits underneath, so the vendor can be swapped later.
- **Original proposal (superseded by D-025):** fully in-house components with no third-party library. Fonts and icons were proposed here, but that's now deferred (D-023).

## D-009: Game day role: GM + game plan
- **Status:** **Accepted** (owner, 2026-10-13)
- **Decision:** On game day the user sets the depth chart and a weekly **game plan** (tendencies, focus, aggression), then watches a live replay or sims instantly. **No play-calling.** The plan's effect is bounded (about +2–5% win probability against an equal opponent).
- **Consequences:** + Keeps the GM fantasy and a manageable engine scope. + Gives weekly engagement. − No "coach mode" for players who want it (out of scope). The engine API keeps a halftime split possible for future halftime adjustments.

## D-010: Draft clock: AI picks fast, user's clock real
- **Status:** **Accepted** (owner, 2026-10-13)
- **Decision:** When the user is on the clock, real wall-clock time runs at 1× and never pauses. AI picks resolve after short deliberation delays (about 3–15 s), which the user can accelerate (2×/4×). **The rule: time can be accelerated, never paused.** A full draft takes about 20–40 minutes.
- **Consequences:** + The pressure lands where it matters, and the draft fits in a session. − Requires a real-time engine with race-safe commands ([LIVE_EVENTS.md](architecture/LIVE_EVENTS.md)).
- **Details resolved:** clock presets → [D-044](#d-044-draft-clock-presets-tiered-changeable-before-the-draft), close/crash policy → [D-045](#d-045-live-event-close-or-crash-resume-where-it-left-off).
- **Amended by D-022:** "never paused" applies to saves in **Live** clock mode (the default). Saves created in **Pausable** mode can pause live events.

## D-011: Release posture: personal first, Steam-ready
- **Status:** **Accepted** (owner, 2026-10-13: "Personal for now, but if I enjoy it, Steam someday. It should be built ready for Steam, with the likelihood it never reaches it.")
- **Decision:** Build for personal play first, and pay only the *cheap* Steam-readiness costs now:
  - fictional league, teams and players only (D-017);
  - permissively licensed dependencies and assets, with a license check before adding any package (avoid MediatR, AutoMapper, FluentAssertions v8+ and other libraries that moved to commercial licenses);
  - versioned save migrations from the first public build;
  - an installable, updatable build (D-019);
  - no telemetry. Crash reporting, if ever added, is opt-in.
- **Deferred until Gate 2:** Steamworks integration, achievements, cloud saves, store assets, trademark clearance.

## D-012: Deterministic simulation with an in-house PRNG
- **Status:** Proposed
- **Decision:** All randomness in Core, Simulation and Application comes from an in-house PRNG (xoshiro256** or PCG32) with **derived seeds** per purpose (game, progression, generation). `System.Random` and wall-clock time are banned in those projects (enforced by an analyzer).
- **Consequences:** + Reproducible bugs, golden tests, resumable advances (re-simulating gives identical results). − A small amount of discipline (hash-based seed derivation, no unordered iteration in the sim).

## D-013: Statistical play-by-play simulation, no physics
- **Status:** Accepted (owner: "game simulation engine (not a physics engine)")
- **Decision:** A matchup-based statistical play-by-play engine with full game state, calibrated against real-world league distributions, recording a **deciding matchup** per play for explainability. See [SIMULATION_ENGINE.md](architecture/SIMULATION_ENGINE.md).

## D-014: Fixed 32-team league structure for 1.0
- **Status:** **Accepted** (owner, 2026-10-14, by choosing "Fixed + no cap knobs" for Q-13; see [D-036](#d-036-league-settings-at-creation-are-gameplay-only-one-user-team))
- **Decision:** 32 teams, 2 conferences, 4 divisions of 4, 17 games across 18 weeks, 7 playoff seeds per conference, 53-man rosters + a 16-player practice squad. Not configurable in 1.0.
- **Why:** configurability breaks the schedule formula and tiebreakers and multiplies test cases. See [Q-13](OPEN_QUESTIONS.md#q-13-league-customization).

## D-015: Integer keys with strongly typed IDs
- **Status:** Proposed
- **Decision:** `INTEGER PRIMARY KEY` (rowid) tables with C# `readonly record struct PlayerId(int Value)` style IDs, mapped through EF value converters. The only GUID is `SaveInfo.SaveId`, which identifies a league across exports and imports.
- **Consequences:** + Compact, fast joins, and no mixing up `PlayerId` with `TeamId` at compile time. − Converter boilerplate (minimal with a source generator or a base convention).

## D-016: Single-writer command queue
- **Status:** Proposed
- **Decision:** Every mutation to a save goes through `ISaveWriter.ExecuteAsync` (one at a time, each in its own transaction). Domain events are published only after commit. Reads run concurrently (WAL). Commands that can race (live events) carry version tokens.
- **Consequences:** + No write contention, no double picks, simple reasoning. − Very long commands block other writes (mitigated by step-journaled advances).

## D-017: Fictional league and players only
- **Status:** Accepted (follows from D-011)
- **Decision:** No real league, team, player or logo names. Generated names are checked against a blocklist of famous real people. Real **city** names are used with original nicknames ([D-033](#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)). Colleges are fictional by default, and users can import their own list in M11 ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). Personal-use real-roster imports are a post-1.0 modding question (F-28), not shipped content.

## D-018: Testing stack
- **Status:** Proposed
- **Decision:** xUnit v3, bUnit, **Shouldly** (or the AwesomeAssertions fork, Apache 2.0, *not* FluentAssertions v8+, which requires a commercial license), Verify (snapshot tests), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`), NetArchTest (architecture rules), BenchmarkDotNet, and optionally Playwright for .NET (E2E through DevHost). Tests use real SQLite temp files, not the EF in-memory provider.

## D-019: Distribution: unpackaged + Velopack, Steam later
- **Status:** Proposed
- **Decision:** Ship as a **self-contained, unpackaged** desktop app (with the WPF host from D-021, a plain `dotnet publish` with no MSIX and no Windows App SDK). Use **Velopack** (MIT) for the installer, delta updates and prerequisite bootstrapping (WebView2). Steam builds disable Velopack (Steam handles updates). MSIX is avoided because of its file-system virtualization quirks and sideloading friction.
- **Open:** code signing (a cost decision, made at M11).

## D-020: Feature folders, no mediator or mapper libraries
- **Status:** Proposed
- **Decision:** `GridironFrontOffice.Application` is organized by feature (`Draft/`, `FreeAgency/`, …) with plain command and query services. No MediatR or AutoMapper (both moved to commercial licensing in 2025, and neither is needed at this scale). Mapping is hand-written projections. In-process domain events use a tiny in-house dispatcher.

## D-021: Host: WPF + BlazorWebView (MAUI removed)
- **Status:** **Accepted** (owner, 2026-10-13: "Sounds good, let's remove MAUI and go with the lighter weight WPF."). Earlier: "If MAUI is too heavyweight then I am fine removing it. I left it in so that we can leverage MAUI Blazor (modern HTML, CSS, etc.) as compared to XAML."
- **Evidence (measured, [HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md)):** clean build 4.7× faster, edit→build 2.3× faster, publish 2× faster like-for-like, 14–28% smaller install, no MAUI workload. Runtime is roughly equal: MAUI reaches first render about 0.2 s sooner on warm starts, and memory is about the same. **The win is developer productivity and simplicity, not runtime speed.**
- **Requirements found while measuring:** `GridironFrontOffice.Desktop` targets **`net10.0-windows10.0.19041.0`** (the .NET 10 WPF `BlazorWebView` uses WebView2's composition control and crashes at startup with plain `net10.0-windows`), and publishes with **`PublishReadyToRun=true`**.
- **Context:** The HTML/CSS benefit comes from **Blazor + BlazorWebView**, not from MAUI. Microsoft ships the same `BlazorWebView` control for **WPF** (`Microsoft.AspNetCore.Components.WebView.Wpf`, published for .NET 10). It renders the same Razor components with the same WebView2 (Chromium) engine. On Windows, MAUI adds a large workload, the Windows App SDK/WinUI packaging layer, slower builds and more version churn, and in return mainly offers a macOS path.
- **Decision:** `GridironFrontOffice.Desktop` is a **WPF** app hosting `BlazorWebView`. Everything else (RCLs, DevHost, services) is unchanged, because the host was already designed to be thin.
- **Consequences:**
  - \+ No MAUI workload. Faster builds, simpler CI and publishing (self-contained, no Windows App SDK). A mature, stable host.
  - \+ Identical UI technology: Razor, HTML, modern CSS, the same WebView2.
  - − Windows only. If macOS becomes a goal ([Q-06](OPEN_QUESTIONS.md#q-06-macos-ever)), add a second thin host (MAUI or Photino.Blazor). The RCL architecture keeps that to roughly a day of work.
  - − A custom title bar needs a little WPF work (`WindowChrome` with a native caption strip, or a WebView2 non-client region with `app-region: drag`; to be validated in M0).
- **Alternatives:** keep MAUI (heavier, macOS path), Photino.Blazor (lightweight and cross-platform, smaller ecosystem), WinForms + BlazorWebView (equivalent to WPF, but WPF has better window chrome control).

## D-022: Clock mode is a per-save setting (Live / Pausable)
- **Status:** **Accepted, locked at creation** (owner's idea, 2026-10-13: "I wonder if we can make the 'No Pause' feature a setting for that given save." Then: "I like making it a lock option."). "Ironman requires Live" was confirmed in [D-026](#d-026-ironman-mode-requires-live-clock-mode).
- **Decision:** At league creation, the user picks a **clock mode** that applies to every live event in that save (the draft, and later FA Frenzy and Deadline Day):
  - **Live** (default, the intended experience): time can be accelerated, never paused.
  - **Pausable:** a Pause button freezes the whole live event (your clock, AI deliberation, offer timers) until resumed. The clock presets still apply.
- **Guardrails:**
  - **Locked after creation** (decided). Otherwise "turn on pause for one hard pick" becomes the norm.
  - **Ironman requires Live** (decided, D-026). Future achievements require Live.
  - The save card shows a **LIVE** badge so the mode is visible.
  - The New League wizard explains the trade-off in one sentence and recommends Live.
- **Consequences:** + Accessibility and real-life interruptions are handled without diluting the default identity. + Cheap to build, since pause reuses the live event resume path ([LIVE_EVENTS.md](architecture/LIVE_EVENTS.md)). − Two modes to test. − Marketing copy must say "the clock never stops *in Live mode*".

## D-023: Fonts, icons and theme deferred to a visual design document
- **Status:** **Accepted** (owner, 2026-10-13: "Font, Icons, and overall theme will be decided later on another document."). **Fulfilled 2026-10-14** by [VISUAL_DESIGN.md](VISUAL_DESIGN.md) (see D-031).
- **Decision:** Font families, the icon set, the color palette and the overall theme are chosen in a future visual design document. Until then, the docs specify only **requirements** (e.g., tabular numerals, a condensed display face for scoreboards, permissive licenses, bundled locally for offline use) and **token names** (the contract components code against), not values. With D-025, the visual design doc's job becomes **configuring Fluent's theme** (brand color behavior, fonts, radius, density) plus GFO's extension tokens.

## D-024: Data access: EF Core with the GFO unit of work
- **Status:** **Accepted** (owner, 2026-10-13: "I think we will go with EF Core + the 2 rule [the §2 unit-of-work rules]. That is what I had in mind prior to working on this." On past problems: "I think I implemented it wrong, let's be sure to detail and make it clear how to utilize it in this app.")
- **Decision:**
  - **EF Core 10** with **Microsoft.Data.Sqlite**. EF Core migrations, applied when a save is opened.
  - **`SaveWriter` is the only transaction owner.** One command = one `DbContext` = one connection = one transaction. Feature code receives an `IUnitOfWork`.
  - **No `AddDbContext`/`AddDbContextFactory`.** Contexts are created per command or query from the open `LeagueSession`.
  - `GridironFrontOffice.Application` references **`Microsoft.EntityFrameworkCore` (core package, no provider)** and works through `ILeagueDb`/`ILeagueReadDb`. The SQLite provider, configurations, migrations and transaction control stay in Infrastructure. *This amends the earlier "Application references nothing data-related" rule, so commands can use LINQ directly without a repository layer.*
  - Raw SQL only through `uow.CreateCommand` (same connection and transaction), for hot paths like weekly stat inserts.
  - Enforced by banned-API analyzers, architecture tests, a nested-command guard, and **transaction torture tests on every build from M0**.
- **The how-to:** [DATA_ACCESS_GUIDE.md](architecture/DATA_ACCESS_GUIDE.md). Alternatives considered: [DATA_ACCESS_OPTIONS.md](architecture/DATA_ACCESS_OPTIONS.md).

## D-025: UI component library: Fluent UI Blazor v5
- **Status:** **Accepted** (owner, 2026-10-13: "I have always liked Fluent UI so let's go with that option.")
- **Decision:** Adopt **Microsoft Fluent UI Blazor v5** (MIT; v5.0.0 stable since 2026-09-25; built on Fluent UI Web Components 3 / Fluent 2) as the component foundation (strategy C in [UI_COMPONENT_OPTIONS.md](architecture/UI_COMPONENT_OPTIONS.md)). **Start on v5, not v4.** v4 now gets only essential security fixes, and v4 → v5 is a migration.
- **Theming:** brand color → a generated 16-step ramp, set at runtime through `IThemeService`/`ThemeSettings` (enables the team-tinted accent), plus light/dark/system modes and design-token overrides. Details: [UI_DESIGN_SYSTEM.md §3](architecture/UI_DESIGN_SYSTEM.md#3-theming-with-fluent-ui-blazor-v5).
- **Amends D-008's wrapping rule:** use Fluent primitives **directly** in screens. Wrap only the central or volatile pieces: `DataGrid<T>`, `GfoThemeService`, dialog and toast services, and `Icon`.
- **Risks and mitigations:** v5 is new (expect 5.0.x fixes; pin the version). Verify that the `FluentDataGrid` pinned column + virtualization + sticky header work together (a v4 issue existed). **Spike S0b** validates both. The fallback is to keep Fluent and swap only the grid internals behind `DataGrid<T>`.

## D-026: Ironman mode (requires Live clock mode)
- **Status:** The rule is **Accepted** (owner, 2026-10-13: "yes, Ironman requires the Live clock mode. It's a one-line rule and keeps 'Ironman' meaning maximum stakes."). The implementation levels below are **Proposed**.
- **Decision (accepted):**
  - Ironman is a league-creation toggle, **off by default** and **locked after creation** (`SaveInfo.IsIronman`). No command exists to change it.
  - **Ironman requires the Live clock mode** ([D-022](#d-022-clock-mode-is-a-per-save-setting-live--pausable)). The wizard disables the toggle when Pausable is selected, and the Application layer rejects the combination as defense in depth.
  - An Ironman save **can't be restored to a chosen snapshot or duplicated**. Export is allowed (for backup), and an imported copy keeps the flag.
  - The save card shows an **IRONMAN** badge. If Steam ever happens, achievements unlock only in Ironman.
- **Implementation (proposed), estimated at about 2 days in M5 once the wizard and Time Machine exist:**
  - **Level 1, honor system:** guards in `SaveManager.RestoreAsync`/`DuplicateAsync` (not just hidden buttons), the wizard rule, the badge, and tests.
  - **Corruption-only recovery:** keep taking snapshots in Ironman. Allow one kind of restore: **automatic**, to the **most recent good snapshot**, and only when the live file fails `quick_check`/`integrity_check` on open or a schema migration fails. The player never picks a point in time.
  - **Groundwork in M0:** a `SaveInfo.Generation` counter that `SaveWriter` increments on every commit. It costs almost nothing now and makes Level 2 possible later without a migration.
- **Deferred:** **Level 2 (rollback detection).** The app records the highest `Generation` seen per `SaveId` in `LocalAppData`. A reopened save with a lower generation was rolled back by a file copy, so it loses Ironman status. Build it only if Steam achievements happen (about 1–2 days). It detects rollbacks but can't prevent them.
- **Rejected:** cryptographic signing of save state. The key would ship inside the app, so it stops nobody.
- **Known limitation (accepted):** copying `.gfo` files in Explorer can't be prevented. Ironman protects the *meaning* of the badge for honest players, not the file from a determined one.

## D-027: UI components are for this app only
- **Status:** **Accepted** (owner, 2026-10-13: "Shared components are for this app only"). Resolves Q-04.
- **Decision:** `GridironFrontOffice.UI.Components` is **not** a reusable library. No NuGet packaging, versioning, public API or neutral naming is needed.
- **Kept anyway:** the separate project and its "no football knowledge, no Application references" rule, for **dependency hygiene**. Generic UI (theme, wrappers, charts) can't accidentally couple to game services, and its bUnit tests stay fast and isolated.
- **Alternative (not taken):** merging it into `GridironFrontOffice.UI` would mean one fewer project, but the rule would then be enforceable only by namespace. Reconsider if the project ends up tiny.

## D-028: Project naming: `GridironFrontOffice.*`
- **Status:** **Accepted** (owner, 2026-10-13). Resolves Q-05. (The earlier proposal was `Gfo.*`.)
- **Decision:**
  - Projects, assemblies and root namespaces: **`GridironFrontOffice.<Name>`** (e.g., `GridironFrontOffice.Core`, `GridironFrontOffice.Infrastructure`, `GridironFrontOffice.Core.Tests`).
  - **Folders use the short name**: `src/Core/GridironFrontOffice.Core.csproj`, `tests/Core.Tests/GridironFrontOffice.Core.Tests.csproj`. Paths stay readable and well under the 260-character limit (which bit MAUI's build during the host benchmark).
  - **Unchanged short forms:** type prefixes (`GfoThemeService`, `IGfoDialogs`, `AddGfoInfrastructure()`), the CSS prefix `gfo-` and tokens `--gfo-*`, the save extension `.gfo`, log files `gfo-YYYYMMDD.log`, and "GFO" as the abbreviation in prose.
  - Logging categories follow namespaces (e.g., `GridironFrontOffice.Simulation.Trace`).

## D-029: `CLAUDE.md` at the repo root for AI-assisted development
- **Status:** **Accepted** (owner, 2026-10-13). Resolves Q-24.
- **Decision:** a concise root [`CLAUDE.md`](../CLAUDE.md) encodes the non-negotiable rules (architecture boundaries, the data access rules, UI rules, determinism, workflow and Definition of Done) and **points into `docs/` instead of duplicating it**. It's updated in the same change whenever a decision changes a rule.
- **Not now:** `.github/copilot-instructions.md`. Add it (pointing at `CLAUDE.md`) if Copilot is used alongside.

## D-030: Source hosting and CI deferred to F-30
- **Status:** **Accepted** (owner, 2026-10-13: "Let's define this as a feat / task for later."). Resolves Q-25.
- **Decision:** remote hosting, CI, nightly runs and issue tracking become the engineering task **[F-30](FEATURES.md#f-30-source-hosting--ci)**: recommended before M5, required before M11. Until then, **`build.ps1`** at the repo root runs the same gates locally (build with warnings as errors + fast tests; `-Full` adds calibration, autoplay and benchmarks). The future CI just calls the script.
- **Consequence:** discipline replaces automation for a while. Run `./build.ps1` before every commit and `./build.ps1 -Full` before closing a milestone.

## D-031: Visual direction: Stadium (light + dark, dark by default)
- **Status:** **Accepted** (owner, 2026-10-14). On the visual directions canvas: "I like the curve buttons / components for Stadium Light but I want to see it in dark mode", then "Can we go with stadium light / dark based off their selection? Default to dark".
- **Decision:** GFO uses the **Stadium** direction ([VISUAL_DESIGN.md](VISUAL_DESIGN.md)): rounded geometry (8 / 14 / 20 px corners and pills), Archivo with a condensed width for display, team-color hero bands, and pill rating chips. The **player chooses** Dark, Light or Match Windows in Settings, and **Dark is the default**.
- **Dark palette:** **slate** (neutral). The team-tinted and true-black variants were explored on the canvas and kept as possible future appearance options; they're token swaps, so components don't change.
- **Fonts:** Archivo (UI + display) and JetBrains Mono (debug only), both SIL OFL and bundled locally. **Icons:** Fluent System Icons (MIT), *proposed*, to confirm in the M0 gallery.
- **Not chosen:** A · Fluent Native (too plain for the "modern, distinctive" pillar), B · Broadcast (loud for everyday screens), C · Analyst Terminal (too austere). Explorations: the [GFO Visual Directions canvas](https://claude.ai/artifact/A3WxdQTV6LYSDV2TWaR1ba).

## D-032: Fog of war: your roster exact, other pros slightly fuzzy, prospects scouted
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-01. The owner chose "core, own roster exact": *"In general I think you should know your own roster's current rating, other roster should slightly fuzzy but since game film is open to the public, and only players should require a scout action to review / remove the fog of war. This is a core concept so be sure to clearly break it down to make sure we are aligned."* Then, on whether other pros sharpen over time: *"Yes, prospects only, however I don't want to have it sharpen based off starts, that is too complex. Keep it simple in that other teams are slightly fuzzy and never sharpen. Prospects start very fuzzy and sharpen based off scouts."* Potential and personality visibility were chosen from the recommended options in the same session.
- **Decision:** a team's view of a player's **current ratings** depends only on who the player is to that team:

| Who you're looking at | Current ratings | Does it sharpen? |
|---|---|---|
| **Your roster** (active, practice squad, IR), including players **the moment** they're drafted, signed, claimed or traded for | **Exact** | n/a |
| **Other teams' players and free agents** | **Slightly fuzzy**: a small, fixed uncertainty (target about ±3 OVR shown, e.g. `78 75–81`; tuned in the F-17 spec). Game film is public, so every team sees pros about equally well. | **Never** |
| **College prospects** (and undrafted players until they sign) | **Very fuzzy** until scouted | **Only through scouting actions:** area scouts, the combine (physical ratings near-exact), pro days, interviews |

  - **Potential** stays hidden for everyone, your roster included. It's shown as a letter grade with a confidence band ([D-034](#d-034-ratings-display-099-with-ranges-letter-grades-for-potential)). On your roster it **narrows as you watch the player develop**, but it's never exact.
  - **Personality traits** ([D-043](#d-043-limited-personality-model-only-traits-with-a-consumer)): your roster shows them as coarse descriptors (High / Average / Low, never numbers) from the moment of acquisition. Prospects reveal them only through limited interviews. Other teams' players show only **reputation tags** earned from visible behavior (e.g., "Held out in 2029").
  - **Stable, not flickery:** each team's error on a pro is derived from `Hash(leagueSeed, "perception", teamId, playerId, season)` and redrawn once per league year. It's never re-rolled per view, and pro fuzz needs no storage.
  - **The sim always uses true ratings.** Stats are the deliberate leak: a "78" who keeps getting beaten will show it.
  - **AI symmetry:** every AI team knows its own roster exactly, sees everyone else with the same fixed fuzz (with its own independent error), and scouts prospects with its own staff. Teams disagree, and that disagreement creates the market.
  - **No pro scouting assignments.** That sub-system is removed from F-17.
  - Difficulty doesn't touch any of this ([D-042](#d-042-no-difficulty-presets-in-10)).
- **Consequences:**
  - \+ Simple to build and explain. + A **reveal moment** on every acquisition, so draft night tells you right away whether you read current ability correctly. + Scope cuts: no pro scouting, no snap-based sharpening.
  - − **"Acquire, peek, cut"** reveals exact ratings. For pros the gain is small (±3). For undrafted rookies it's bigger, which is accepted as realistic (rookie camp tryouts) and bounded by the 90-man offseason limit.
  - − **Winner's curse:** in free agency, the team that overestimates a player most wins the bidding. The valuation model (F-13) must discount uncertain estimates, or AI teams systematically overpay. The M7 exploit suite also measures trade arbitrage (selling your own players that the AI overrates).
  - − A 10-year veteran is exactly as uncertain as a 2nd-year player. Accepted for simplicity.
- **Alternatives rejected:** your roster converging to the truth over a season (closes the peek exploit, but needs more machinery), pros sharpening with snaps played ("too complex"), draft-only fog, no fog.

## D-033: Default teams: 32 curated franchises, real cities, original nicknames
- **Status:** **Accepted** (owner, 2026-10-14, chose "Real cities, original names"). Resolves Q-02.
- **Decision:** the default league is 32 **hand-written** franchises in **real metro areas** (a mix of current pro football markets and plausible new ones) with **original nicknames**, colors, generated SVG crests and fictional stadium names. Every nickname is checked against a blocklist of real pro football team names, past and present (NFL, AFL, AAFC, WFL, USFL, XFL, UFL, CFL and arena leagues). The set lives in a versioned data file ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). The 32 identities are drafted in the F-02 spec for owner review.
- **Parked:** a "randomize identities" option ([IDEAS.md](IDEAS.md)). It needs a nickname generator and color-harmony logic for a rarely used feature.
- **Consequences:** + A real-league feel and regional rivalries. − Real city names (not trademarks, but the M11 trademark check, [Q-22](OPEN_QUESTIONS.md#q-22-name--trademark-check), covers the shipped set too; not legal advice).

## D-034: Ratings display: 0–99 with ranges, letter grades for potential
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-03.
- **Decision:** attributes and OVR are shown as **0–99 integers** with color bands and, where uncertain, a range (`78  74–83`). Exact values show no range (your roster, combine-measured physicals). **Potential and scouting summaries use letter grades** ("Ceiling: B+", "Starter by Year 2 · B") and are never shown as a number, because a number implies false precision. Internally, ratings are 0–100 floats. Color is never the only signal (WCAG; see [VISUAL_DESIGN.md](VISUAL_DESIGN.md)).

## D-035: Fictional colleges; teams and colleges as data files, user import in M11
- **Status:** **Accepted** (owner, 2026-10-14: *"Use Fictional, but I would like similar to teams a way to import a list to customize it"*, then chose "Schema in M1, import in M11"). Resolves Q-18.
- **Decision:**
  - **Fictional colleges by default:** about 130 major and 100 small programs (e.g., "Ridge State", "Coastal Tech"), each tagged with a **region** and **tier** for scouting, and checked against real institution names.
  - **Teams ([D-033](#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)) and colleges are versioned JSON data files with a published schema from M1.** The defaults are just the built-in files, and the generators never hard-code lists.
  - **User import ships in M11**: a "League data" step in the New League wizard that loads a custom team set and/or college list, with clear validation errors. This moves team packs from F-28 (post-1.0) into 1.0. In-game editing stays in F-28.
  - Imported content is the user's own. Nothing real ships with the game.
- **Consequences:** + A personal real-names list is possible without shipping one. − A schema to version from M1 (small).

## D-036: League settings at creation are gameplay-only; one user team
- **Status:** **Accepted** (owner, 2026-10-14, chose "Fixed + no cap knobs"). Resolves Q-13 and confirms [D-014](#d-014-fixed-32-team-league-structure-for-10). Also resolves Q-14: a single user-controlled team was proposed in the same session as already implied by the one-pager's 1.0 scope, and the owner raised no objection.
- **Decision:**
  - The league structure is fixed (D-014).
  - Settings chosen at creation: **clock mode** ([D-022](#d-022-clock-mode-is-a-per-save-setting-live--pausable)), **Ironman** ([D-026](#d-026-ironman-mode-requires-live-clock-mode)), **draft clock preset** ([D-044](#d-044-draft-clock-presets-tiered-changeable-before-the-draft)), **live event toggles** ([D-046](#d-046-live-events-fa-frenzy-committed-deadline-day-conditional)) and **injury frequency** ([D-037](#d-037-injury-frequency-setting-locked-at-creation)). There's no difficulty setting ([D-042](#d-042-no-difficulty-presets-in-10)).
  - **The cap amount and cap growth are not exposed.** They're tuning values in `LeagueRules`. Editing them belongs to commissioner mode (F-28).
  - **One user-controlled team** in 1.0. Multiple teams are revisited with F-28.
- **Why:** the valuation model is calibrated as a percentage of the cap, so a cap-amount setting is cosmetic and a cap-growth setting can break the 30-season health gate. Every knob multiplies the configurations that health checks have to cover.

## D-037: Injury frequency setting, locked at creation
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-17.
- **Decision:** **Low / Realistic (default) / High**, chosen at league creation and **locked** so it can't be dialed down before the playoffs. The 30-season health gate runs on Realistic. Low and High each get a short (5-season) autoplay smoke run in `build.ps1 -Full`, so they can't silently break the league.

## D-038: No preseason games in 1.0
- **Status:** **Accepted** (owner, 2026-10-14, chose "None in 1.0"). Resolves Q-20.
- **Decision:** Training Camp is annual progression at camp open, development focus, cut-down to 53, waivers and the practice squad. **There are no preseason games**, preseason stats or preseason schedule weeks. Parked in [IDEAS.md](IDEAS.md).
- **Why:** their main justification was feeding fog of war, which no longer applies under [D-032](#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted) (your roster is exact, other pros never sharpen, prospects are already drafted). What's left is flavor, at the cost of snap-limit rules, separate stats, about 48 extra games and more calendar weeks.

## D-039: Play-by-play is kept; measure before pruning
- **Status:** **Accepted** (owner, 2026-10-14, chose "Keep all, measure first"). Resolves Q-26.
- **Decision:** all play logs are kept, with **no pruning job and no setting** at first. Real sizes are measured in M3 and in the M5 10-season autoplay. **Trigger:** if play logs exceed 25% of the 10-season save budget (about 37 MB of 150 MB), switch to the fallback: league-wide play-by-play for the last 3 seasons, plus **every one of the user's team's games forever**, with season aggregates and record-worthy plays extracted before pruning. Box scores, stats, records and history are kept forever regardless.
- **Why:** [PERSISTENCE.md §6](architecture/PERSISTENCE.md#6-growth-estimates-and-retention) estimates about 1.5 MB of play logs per season. Pruning would be complexity for an unmeasured problem.

## D-040: Salary cap realism tiers
- **Status:** **Accepted** (owner, 2026-10-14, selected the 5th-year option, restructures and June 1 designations for v2). Resolves Q-12.
- **Decision:**
  - **v1 (M4):** base salary, signing bonus proration (≤ 5 years), guarantees, dead money, the cap per league year with growth, rollover, minimum salaries, the rookie wage scale, the franchise tag, extensions.
  - **v2 (M9):** the **5th-year option**, **restructures**, **June 1 designations**.
  - **Not in 1.0:** RFA tenders and offer sheets, the transition tag, void years, incentives (LTBE/NLTBE), offset language, the Top 51 rule, cash-spending floors, compensatory picks.
  - **In place of RFA** (to be confirmed in the F-12/F-15 specs): a player whose contract expires with **fewer than 4 accrued seasons** is an **exclusive-rights** free agent. His team keeps him by making a one-year qualifying offer at the minimum salary for his experience. If it doesn't, he becomes an unrestricted free agent.

## D-041: Finances: owner-funded staff budget in M10 only
- **Status:** **Accepted** (owner, 2026-10-14, chose "Staff budget in M10"). Resolves Q-15.
- **Decision:** the only money besides the salary cap is an **owner-funded staff budget** (scouting, medical, coaching) arriving with F-23 in **M10**. Until then (M6–M9), **every team has equal scouting capacity** (the same weekly scout points), and scouting departments differ only in scout quality. No revenue, tickets, concessions or stadium.
- **Fixes an inconsistency:** F-17 listed "a scouting budget" in M6, before any owner budget existed.

## D-042: No difficulty presets in 1.0
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-16. The owner asked: *"Not sure if difficult makes sense in this context, can you walk me through how this could playout. Not like the simulation should get easier or hard?"* After the walkthrough (the only honest levers are how strictly the league deals with you: trade and contract strictness, your scouting accuracy, owner patience), the owner chose "No presets in 1.0".
- **Decision:**
  - **No difficulty setting.** The AI judges the user's trades and contract talks with exactly the same logic it applies to other AI teams. The simulation never changes.
  - **Challenge comes from team choice** (the wizard shows roster strength and cap space, so a 3–14 team with no cap room is hard mode), Ironman, the clock preset and injury frequency.
  - **Hooks kept, not exposed:** internal tuning values `UserDealMargin` (an acceptance offset in trades and negotiation) and `UserScoutingAccuracy` (a prospect σ multiplier), both neutral by default, so a setting would take about a day to add later.
  - **Revisit at Gate 2** with playtester feedback.
- **Consequences:** + One code path for AI evaluation, so the M7 exploit suite tests what players actually face. + Every deal meets the same standard (pillar 3). − New players can't turn the game down a notch. Team choice is the dial.

## D-043: Limited personality model; only traits with a consumer
- **Status:** **Accepted** (owner, 2026-10-14: *"Limited, trim unused traits but keep it open to add more as I see fit"*). Resolves Q-19.
- **Decision:**
  - Personality affects **negotiation, development and retirement only**. No locker-room chemistry, no morale-driven performance swings.
  - **1.0 traits, each with a consumer:** Work ethic and Coachability (development; Coachability matters once coaching arrives in M10), Greed and Loyalty (negotiation), Competitiveness (free agency contender priority, retirement), Ego (role demands).
  - **Leadership is parked** with locker-room chemistry ([IDEAS.md](IDEAS.md)).
  - **Extensible:** traits are defined in data along with the systems that read them. Adding one later means data plus a migration that **backfills existing players deterministically** from derived seeds.
  - Visibility follows [D-032](#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted).
- **Note:** Potential, development curve, peak age offset and injury proneness are hidden *ratings*, not personality. They're unaffected.

## D-044: Draft clock presets: tiered, changeable before the draft
- **Status:** **Accepted** (owner, 2026-10-14, chose "Changeable before the draft" and, for Q-10, "No, Pausable covers it"). Resolves Q-07 and Q-10.
- **Decision:**
  - Presets per round (Round 1 / Round 2 / Rounds 3–7): **Relaxed** 4:00 / 3:00 / 2:00, **Standard** (default) 2:00 / 1:30 / 1:00, **Pressure** 1:00 / 0:45 / 0:30. **The times are tentative until Spike S1** tunes them.
  - Chosen at creation. **Changeable in the offseason until the draft starts**, never during it, and **never in an Ironman league**. A longer clock lengthens your clock without removing the pressure, so this isn't the pause exploit that the locked clock mode (D-022) prevents.
  - Independent of clock mode. A Pausable save still uses a preset.
  - **No separate accessibility preset.** Pausable mode plus Relaxed cover it.

## D-045: Live event close or crash: resume where it left off
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-08. The owner first asked: *"Can we restart the draft? Feel like a unlikely situation unless you want to push back."* After the pushback (restarting turns Alt-F4 into a "redo my draft" button, breaks [D-004](#d-004-save-model-autosave--snapshots) and Ironman, and needs a rollback in the middle of a live event, while resuming is already required by Pausable mode), the owner chose "Resume where it left off".
- **Decision:** committed picks and trades stand. On reopen, the event resumes at the same step with the user's remaining clock restored from the 1-second heartbeat, after a 3-second "get ready" countdown that isn't charged to the user. Applies to every live event ([LIVE_EVENTS.md §5](architecture/LIVE_EVENTS.md#5-persistence-and-resume)).
- **Accepted loophole:** in Live mode, quitting acts as a pause. You can't see the board while the app is closed, it's single-player, and every alternative punishes crashes.

## D-046: Live events: FA Frenzy committed, Deadline Day conditional
- **Status:** **Accepted** (owner, 2026-10-14, chose "Frenzy yes, Deadline conditional"). Resolves Q-11.
- **Decision:** the draft is always live. **FA Frenzy is committed** for M9. **Trade Deadline Day is conditional:** it's built only if FA Frenzy proves the pattern is fun a second time (decided in the middle of M9). Each is on by default, **toggleable at league creation and locked afterward**.

## D-047: Sound: all off by default, no music
- **Status:** **Accepted** (owner, 2026-10-14, chose "All sounds off by default" over the recommendation to turn live event cues on by default). Resolves Q-21.
- **Decision:** a minimal sound set (confirm click, final-10-seconds tick, "the pick is in", incoming offer, game final), **all off by default**, with volume and mute in Settings. **No music in 1.0.** Original or CC0 assets only.
- **Non-audio cue (proposed):** when the window isn't focused, the Windows taskbar button flashes when the user goes on the clock or a live event offer arrives, so a user who alt-tabbed during accelerated AI picks still notices.

## D-048: Halftime adjustments post-1.0, engine split kept possible
- **Status:** **Accepted** (proposed in the 2026-10-14 session; the owner raised no objection). Resolves Q-27.
- **Decision:** halftime adjustments are **post-1.0**. From M2, the engine API keeps `Simulate(input, stopAt: Halftime)` and `Resume(state, newPlan)` possible (no UI), so the option stays cheap ([SIMULATION_ENGINE.md](architecture/SIMULATION_ENGINE.md)).
