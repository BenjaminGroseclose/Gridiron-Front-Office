# Decision Log

> A lightweight ADR log. Each entry records **what** was decided, **why**, and **what we gave up**.
>
> **Status values:** `Accepted` (owner decided or confirmed) · `Proposed` (recommended, awaiting confirmation) · `Superseded by D-xxx`
>
> To change a decision, add a new entry that supersedes it. Don't rewrite history.

| ID | Decision | Status |
|---|---|---|
| D-001 | Fresh start, prior history ignored | Accepted |
| D-002 | .NET 10 LTS + Blazor Hybrid, Windows-first | **Accepted** |
| D-003 | UI in Razor Class Libraries, thin host, DevHost | **Accepted** |
| D-004 | Autosave + snapshots save model | **Accepted** |
| D-005 | Saves in LocalAppData, not Documents | **Accepted** |
| D-006 | EF Core + raw ADO for bulk paths | Superseded by D-024 |
| D-007 | Serilog to rolling `.log` files | **Accepted** |
| D-008 | UI components and assets: permissive licenses only | Accepted (licenses). Wrapping rule amended by D-025. |
| D-009 | Game day role: GM + game plan | **Accepted** |
| D-010 | Draft clock: AI fast, user clock real | **Accepted** (amended by D-022) |
| D-011 | Personal first, Steam-ready | **Accepted** |
| D-012 | Deterministic sim with in-house PRNG (xoshiro256\*\*) | **Accepted** |
| D-013 | Statistical play-by-play sim, no physics | Accepted |
| D-014 | Fixed 32-team structure for 1.0 | **Accepted** (via D-036) |
| D-015 | Integer keys + strongly typed IDs | **Accepted** |
| D-016 | Single-writer command queue | **Accepted** |
| D-017 | Fictional league and players only | Accepted (follows from D-011) |
| D-018 | Testing stack | **Accepted** |
| D-019 | Unpackaged + Velopack, Steam later | Proposed |
| D-020 | Feature folders, no mediator/mapper libraries | **Accepted** |
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
| D-035 | Fictional colleges; teams and colleges as data files, user import in M11 | **Accepted** (versioning amended by D-077) |
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
| D-049 | A new league starts at the offseason: "the previous GM was fired" | **Accepted** |
| D-050 | Every player attribute must be read by the sim | **Accepted** |
| D-051 | Onboarding is help pages only, authored in Markdown | **Accepted** |
| D-052 | Depth chart: position depth drives every package, with optional overrides | **Accepted** |
| D-053 | M3 game day: a minimal text replay | **Accepted** |
| D-054 | Injury status is Available / Out until playing hurt (M10) | **Accepted** |
| D-055 | M3 messaging: Briefing + news feed; the inbox arrives in M7 | **Accepted** |
| D-056 | The offseason "Top 51" cap rule is in (amends D-040) | **Accepted** |
| D-057 | Free agency: blind bids with signals | **Accepted** |
| D-058 | Scouting department: standing + special assignments, no biases in 1.0 | **Accepted** |
| D-059 | The UDFA window is one turn-based round of blind offers | **Accepted** |
| D-060 | A paused live event allows browsing but no actions | **Accepted** |
| D-061 | Trade feedback is qualitative, plus "what would it take?" | **Accepted** |
| D-062 | Coaching staff is head coach + two coordinators only | **Accepted** |
| D-063 | "Career can end" is a creation setting, off by default | **Accepted** |
| D-064 | F-02 team set accepted; shared city labels | **Accepted** |
| D-065 | F-03 player model accepted: 42 attributes, 80% ranges, roster mix as averages | **Accepted** |
| D-066 | Git workflow: GFO commit format, one branch per milestone | **Accepted** (branching amended by D-076) |
| D-067 | Spike S0b: Fluent UI v5 fits; `DataGrid<T>` keeps `FluentDataGrid` | **Accepted** |
| D-068 | Host shell: the app bar is the title bar | **Accepted** |
| D-069 | Write `ID`, never `Id` | **Accepted** |
| D-070 | Read models are records named `…Dto` | **Accepted** |
| D-071 | Screen readers aren't supported in 1.0 | **Accepted** |
| D-072 | Versions are `0.<milestone>.<build>` until 1.0 | **Accepted** |
| D-073 | A league's first season is the real-world year it's created in | **Accepted** |
| D-074 | Contracts are real from M1; contract actions arrive in M4 | **Accepted** |
| D-075 | Color-blind tuning is reviewed before a public release | **Accepted** |
| D-076 | Work happens on `main`; no milestone branches | **Accepted** |
| D-077 | Data files carry no version numbers; they change only additively | **Accepted** |
| D-078 | M1 builds for today's consumers: fifteen simplifications | **Accepted** |
| D-079 | WP3 player data: traits in generation.json, measurable priors deferred, F-12 before contracts | **Accepted** |
| D-080 | F-12's M1 slice: no practice squad in 1.0; pay by league-wide quality, with wiggle | **Accepted** |
| D-081 | WP4 generation: trades fit the cap, ranked draft classes, player rules in Application | **Accepted** |
| D-082 | Team strength from a ladder, carried by the young core | **Accepted** |

---

## D-001: Fresh start
- **Status:** Accepted (owner, 2026-10-13)
- **Context:** An earlier attempt exists in git history (MAUI Blazor, Application/Desktop projects, league wizard, schedule work).
- **Decision:** Start over. Earlier commits are ignored for design purposes. Nothing is carried over by default. Old code can be consulted later case by case, but it doesn't constrain the new design.
- **Consequences:** A clean architecture and docs-first approach. Some re-implementation of things that existed (league wizard, schedule).

## D-002: Platform: .NET 10 LTS + Blazor Hybrid, Windows-first
- **Status:** **Accepted** (owner, 2026-10-15: *"D-002: .NET 10 WPF is where we landed I thought, MAUI is not need for our Blazor Hybrid"*). Earlier, Blazor Hybrid (HTML/CSS UI instead of XAML) was accepted by the owner. This decision is the **.NET 10 platform + Blazor UI**. The host framework is [D-021](#d-021-host-wpf--blazorwebview-maui-removed): **WPF**, with no MAUI.
- **Implementation note (2026-10-15):** `global.json` pins the **10.0.402 SDK** (`rollForward: latestPatch`, so it stays on the 10.0.4xx band). Runtime 10.0.13. Bump the pin deliberately when moving to a newer feature band.
- **Context:** The owner has shipped projects successfully with MAUI Blazor Hybrid and wants modern HTML/CSS for the UI rather than XAML. The game is a data-dense desktop app.
- **Decision:** .NET 10 (LTS, supported until Nov 2028). The UI is Blazor rendered in a native window through **BlazorWebView** (WebView2 on Windows), targeting Windows 10/11 x64 first. Stay on .NET 10 through 1.0 unless a blocking need appears. Don't chase .NET 11 (STS).
- **Consequences:** + Full modern HTML/CSS, Razor components, the .NET ecosystem. − A WebView memory footprint (~150–300 MB).
- **Alternatives:** see [ARCHITECTURE.md §11](ARCHITECTURE.md#11-alternatives-considered-host-framework).

## D-003: UI lives in Razor Class Libraries; the host stays thin
- **Status:** **Accepted** (owner, 2026-10-15: *"Accepted, UI.Components and UI should own the UI while Desktop only hosts the webview"*). Scaffolded in M0 WP1 and enforced by the architecture tests.
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
- **Status:** **Accepted** (owner, 2026-10-15: *"Accepted format looks good and Serilog is the right choice"*). Earlier, the requirement: "log to a .log file".
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
- **Status:** **Accepted** (owner, 2026-10-20: *"go with `xoshiro256**`"*, answering the M1 plan).
- **Decision:** All randomness in Core, Simulation and Application comes from an in-house PRNG, **xoshiro256\*\*** (seeded through SplitMix64), with **derived seeds** per purpose (game, progression, generation). A seed is derived by an in-house stable hash of `(leagueSeed, purpose, ids…)`: FNV-1a 64 over the bytes, then SplitMix64's finalizer (never `string.GetHashCode`, which changes between runs). `System.Random` and wall-clock time are banned in those projects (enforced by an analyzer). The exact algorithms and their reference values are in [M1_WP1_GUIDE.md](milestones/M1_WP1_GUIDE.md).
- **Consequences:** + Reproducible bugs, golden tests, resumable advances (re-simulating gives identical results). − A small amount of discipline (hash-based seed derivation, no unordered iteration in the sim).

## D-013: Statistical play-by-play simulation, no physics
- **Status:** Accepted (owner: "game simulation engine (not a physics engine)")
- **Decision:** A matchup-based statistical play-by-play engine with full game state, calibrated against real-world league distributions, recording a **deciding matchup** per play for explainability. See [SIMULATION_ENGINE.md](architecture/SIMULATION_ENGINE.md).

## D-014: Fixed 32-team league structure for 1.0
- **Status:** **Accepted** (owner, 2026-10-14, by choosing "Fixed + no cap knobs" for Q-13; see [D-036](#d-036-league-settings-at-creation-are-gameplay-only-one-user-team))
- **Decision:** 32 teams, 2 conferences, 4 divisions of 4, 17 games across 18 weeks, 7 playoff seeds per conference, 53-man rosters + a 16-player practice squad. *(The practice squad left 1.0 in [D-080](#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle); rosters are the 53.)* Not configurable in 1.0.
- **Why:** configurability breaks the schedule formula and tiebreakers and multiplies test cases. See [Q-13](OPEN_QUESTIONS.md#q-13-league-customization).

## D-015: Integer keys with strongly typed IDs
- **Status:** **Accepted** (owner, 2026-10-15: *"Correct we should use Integer IDs"*). The ID convention is built in M0 WP6. The first IDs arrive in M1.
- **Decision:** `INTEGER PRIMARY KEY` (rowid) tables with C# `readonly record struct PlayerID(int Value)` style IDs, mapped through EF value converters. The only GUID is `SaveInfo.SaveID`, which identifies a league across exports and imports.
- **Consequences:** + Compact, fast joins, and no mixing up `PlayerID` with `TeamID` at compile time. − Converter boilerplate (minimal with a source generator or a base convention).

- **Implementation note (Spike S0a, 2026-10-15):** proven with one generic `IDConverter<TID>` registered for every ID type in `ConfigureConventions`. **EF's SQLite convention doesn't add `AUTOINCREMENT` to converted keys**, so `LeagueDbContext` applies `UseAutoincrement()` (the EF Core 10 API; the old raw annotation is ignored) to every generated typed-ID key. Otherwise SQLite may reuse the highest ID after a delete ([DATA_ACCESS_GUIDE.md §3.3](architecture/DATA_ACCESS_GUIDE.md#33-infrastructure-the-context-and-the-session)).

## D-016: Single-writer command queue
- **Status:** **Accepted** (owner, 2026-10-15: *"A single write makes sense to me, should only ever need to save something one at a time no need to thread saving."*). Note: only *writes* are serialized. Reads still run concurrently alongside the writer (SQLite WAL), so screens never wait on a save.
- **Decision:** Every mutation to a save goes through `ISaveWriter.ExecuteAsync` (one at a time, each in its own transaction). Domain events are published only after commit. Reads run concurrently (WAL). Commands that can race (live events) carry version tokens.
- **Consequences:** + No write contention, no double picks, simple reasoning. − Very long commands block other writes (mitigated by step-journaled advances).

## D-017: Fictional league and players only
- **Status:** Accepted (follows from D-011)
- **Decision:** No real league, team, player or logo names. Generated names are checked against a blocklist of famous real people. Real **city** names are used with original nicknames ([D-033](#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)). Colleges are fictional by default, and users can import their own list in M11 ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). Personal-use real-roster imports are a post-1.0 modding question (F-28), not shipped content.

## D-018: Testing stack
- **Status:** **Accepted** (owner, 2026-10-15: *"Whatever you recommend"*). Accepted as recommended, with the open choice resolved: **Shouldly** is the assertion library (already in use; the AwesomeAssertions alternative isn't needed). BenchmarkDotNet arrives with the engine (M2), `FakeTimeProvider` with live events (M6), and Playwright stays optional (M3+).
- **Decision:** xUnit v3, bUnit, **Shouldly** (or the AwesomeAssertions fork, Apache 2.0, *not* FluentAssertions v8+, which requires a commercial license), Verify (snapshot tests), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`), NetArchTest (architecture rules), BenchmarkDotNet, and optionally Playwright for .NET (E2E through DevHost). Tests use real SQLite temp files, not the EF in-memory provider.
- **Implementation note (M0 WP1, 2026-10-15):** tests run on **Microsoft.Testing.Platform**, enabled in `global.json`. The .NET 10 SDK no longer runs xUnit v3 through the VSTest bridge, so `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` aren't used, and filters use xUnit's own options ([TESTING_AND_QUALITY.md §7](architecture/TESTING_AND_QUALITY.md#7-build-script-now-ci-later)).

## D-019: Distribution: unpackaged + Velopack, Steam later
- **Status:** Proposed
- **Decision:** Ship as a **self-contained, unpackaged** desktop app (with the WPF host from D-021, a plain `dotnet publish` with no MSIX and no Windows App SDK). Use **Velopack** (MIT) for the installer, delta updates and prerequisite bootstrapping (WebView2). Steam builds disable Velopack (Steam handles updates). MSIX is avoided because of its file-system virtualization quirks and sideloading friction.
- **Open:** code signing (a cost decision, made at M11).

## D-020: Feature folders, no mediator or mapper libraries
- **Status:** **Accepted** (owner, 2026-10-15: *"I like this idea, feature folders make sense in terms of organization"*).
- **Decision:** `GridironFrontOffice.Application` is organized by feature (`Draft/`, `FreeAgency/`, …) with plain command and query services. No MediatR or AutoMapper (both moved to commercial licensing in 2025, and neither is needed at this scale). Mapping is hand-written projections. In-process domain events use a tiny in-house dispatcher.

## D-021: Host: WPF + BlazorWebView (MAUI removed)
- **Status:** **Accepted** (owner, 2026-10-13: "Sounds good, let's remove MAUI and go with the lighter weight WPF."). Earlier: "If MAUI is too heavyweight then I am fine removing it. I left it in so that we can leverage MAUI Blazor (modern HTML, CSS, etc.) as compared to XAML."
- **Evidence (measured, [HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md)):** clean build 4.7× faster, edit→build 2.3× faster, publish 2× faster like-for-like, 14–28% smaller install, no MAUI workload. Runtime is roughly equal: MAUI reaches first render about 0.2 s sooner on warm starts, and memory is about the same. **The win is developer productivity and simplicity, not runtime speed.**
- **Requirements found while measuring:** `GridironFrontOffice.Desktop` targets **`net10.0-windows10.0.19041.0`** (the .NET 10 WPF `BlazorWebView` uses WebView2's composition control and crashes at startup with plain `net10.0-windows`), and publishes with **`PublishReadyToRun=true`**.
- **Real-shell result (Spike S0c, 2026-10-16):** the actual app reaches a themed first screen in about 1.4–1.5 s warm and 1.57 s on a first-ever launch, well inside M0's 3 s launch budget. It also needs a per-monitor-v2 DPI manifest. Details: [D-068](#d-068-host-shell-the-app-bar-is-the-title-bar), [HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md#real-shell-spike-s0c-2026-10-16).
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
- **Spike S0a result (2026-10-15): confirmed. The linq2db fallback isn't needed.** All 27 checks passed on EF Core 10.0.13:
  - Bulk: a full season (25,650 stat rows + 285 play logs) persists in 665 ms (target < 1 s), and a week in 27 ms.
  - A migration with a backfill on a populated save takes 45 ms.
  - The torture checks pass, and the transaction is IMMEDIATE.
  - The smallest command costs 0.2 ms.
  - Corrections it found, all now in the guide: typed-ID keys need an explicit `UseAutoincrement()`, migration files must be marked as generated code, and league generation belongs on the raw bulk path (EF change tracking costs about 100 µs per entity).
  - Details: [M0_FOUNDATIONS.md WP2](milestones/M0_FOUNDATIONS.md#wp2--spike-s0a-ef-core-proof-sm-throwaway-code--done-2026-10-15).

## D-025: UI component library: Fluent UI Blazor v5
- **Status:** **Accepted** (owner, 2026-10-13: "I have always liked Fluent UI so let's go with that option.")
- **Decision:** Adopt **Microsoft Fluent UI Blazor v5** (MIT; v5.0.0 stable since 2026-09-25; built on Fluent UI Web Components 3 / Fluent 2) as the component foundation (strategy C in [UI_COMPONENT_OPTIONS.md](architecture/UI_COMPONENT_OPTIONS.md)). **Start on v5, not v4.** v4 now gets only essential security fixes, and v4 → v5 is a migration.
- **Theming:** brand color → a generated 16-step ramp, set at runtime through `IThemeService`/`ThemeSettings` (enables the team-tinted accent), plus light/dark/system modes and design-token overrides. Details: [UI_DESIGN_SYSTEM.md §3](architecture/UI_DESIGN_SYSTEM.md#3-theming-with-fluent-ui-blazor-v5).
- **Amends D-008's wrapping rule:** use Fluent primitives **directly** in screens. Wrap only the central or volatile pieces: `DataGrid<T>`, `GfoThemeService`, dialog and toast services, and `Icon`.
- **Risks and mitigations:** v5 is new (expect 5.0.x fixes; pin the version). Verify that the `FluentDataGrid` pinned column + virtualization + sticky header work together (a v4 issue existed). **Spike S0b** validates both. The fallback is to keep Fluent and swap only the grid internals behind `DataGrid<T>`.
- **Spike S0b result (2026-10-15): confirmed. The fallback grid isn't needed.** `FluentDataGrid` passes the P0 checklist with 2,000 rows once `DataGrid<T>` applies four fixes, and the Stadium theme maps onto v5's typed `Theme`. Details: [D-067](#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid).

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
  - **Already built in M0 (WP7):** the Level 1 restore guard. `SaveManager.RestoreAsync` refuses an Ironman league through `SaveRules.CanRestore`, with tests. The duplicate guard, the wizard rule, the badge and the corruption-only recovery remain for later.
- **Deferred:** **Level 2 (rollback detection).** The app records the highest `Generation` seen per `SaveID` in `LocalAppData`. A reopened save with a lower generation was rolled back by a file copy, so it loses Ironman status. Build it only if Steam achievements happen (about 1–2 days). It detects rollbacks but can't prevent them.
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
- **Fonts:** Archivo (UI + display) and JetBrains Mono (debug only), both SIL OFL and bundled locally. **Icons:** Fluent System Icons (MIT), **confirmed** by the owner after Spike S0b showed them next to Archivo ([D-067](#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)).
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
  - **Stable, not flickery:** each team's error on a pro is derived from `Hash(leagueSeed, "perception", teamID, playerID, season)` and redrawn once per league year. It's never re-rolled per view, and pro fuzz needs no storage.
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
- **Decision:** the default league is 32 **hand-written** franchises in **real metro areas** (a mix of current pro football markets and plausible new ones) with **original nicknames**, colors, generated SVG crests and fictional stadium names. Every nickname is checked against a blocklist of real pro football team names, past and present (NFL, AFL, AAFC, WFL, USFL, XFL, UFL, CFL and arena leagues). The set lives in a data file ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). The 32 identities are drafted in the F-02 spec for owner review.
- **Parked:** a "randomize identities" option ([IDEAS.md](IDEAS.md)). It needs a nickname generator and color-harmony logic for a rarely used feature.
- **Consequences:** + A real-league feel and regional rivalries. − Real city names (not trademarks, but the M11 trademark check, [Q-22](OPEN_QUESTIONS.md#q-22-name--trademark-check), covers the shipped set too; not legal advice).
- **Amended (owner, 2026-10-14, feature review):** *"Let mirror the NFL, but again they should be able to upload their own teams. Additionally I assume we cannot use the NFL teams direct due to copyright so we will need to account for this."*
  - **The metros mirror the NFL's footprint**, including two teams each in the New York and Los Angeles areas. This replaces "a mix of current markets and new ones".
  - **Divisions are aligned by geography** (proposed default, reviewed in the F-02 spec), with each two-team metro split across the conferences. Conference and division names are original.
  - **Guardrail:** no team uses the colors of the real team in its metro, and no crest resembles a real logo. Real team names, logos and distinctive color schemes are protected (mainly trademark and trade dress), so none of them ship.
  - Real teams are the user's own business through the M11 import ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)). Imported SVG logos are rendered as `<img>` (or sanitized) and never inlined, because SVG can carry script.

## D-034: Ratings display: 0–99 with ranges, letter grades for potential
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-03.
- **Decision:** attributes and OVR are shown as **0–99 integers** with color bands and, where uncertain, a range (`78  74–83`). Exact values show no range (your roster, combine-measured physicals). **Potential and scouting summaries use letter grades** ("Ceiling: B+", "Starter by Year 2 · B") and are never shown as a number, because a number implies false precision. Internally, ratings are 0–100 floats. Color is never the only signal (WCAG; see [VISUAL_DESIGN.md](VISUAL_DESIGN.md)).

## D-035: Fictional colleges; teams and colleges as data files, user import in M11
- **Status:** **Accepted** (owner, 2026-10-14: *"Use Fictional, but I would like similar to teams a way to import a list to customize it"*, then chose "Schema in M1, import in M11"). Resolves Q-18. The "versioned" part is **amended by [D-077](#d-077-data-files-carry-no-version-numbers-they-change-only-additively)** (no version numbers; formats only grow).
- **Decision:**
  - **Fictional colleges by default:** about 130 major and 100 small programs (e.g., "Ridge State", "Coastal Tech"), each tagged with a **region** and **tier** for scouting, and checked against real institution names.
  - **Teams ([D-033](#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)) and colleges are JSON data files with a published schema from M1.** The defaults are just the built-in files, and the generators never hard-code lists.
  - **User import ships in M11**: a "League data" step in the New League wizard that loads a custom team set and/or college list, with clear validation errors. This moves team packs from F-28 (post-1.0) into 1.0. In-game editing stays in F-28.
  - Imported content is the user's own. Nothing real ships with the game.
- **Consequences:** + A personal real-names list is possible without shipping one. − A schema to maintain from M1 (small).

## D-036: League settings at creation are gameplay-only; one user team
- **Status:** **Accepted** (owner, 2026-10-14, chose "Fixed + no cap knobs"). Resolves Q-13 and confirms [D-014](#d-014-fixed-32-team-league-structure-for-10). Also resolves Q-14: a single user-controlled team was proposed in the same session as already implied by the one-pager's 1.0 scope, and the owner raised no objection.
- **Decision:**
  - The league structure is fixed (D-014).
  - Settings chosen at creation: **clock mode** ([D-022](#d-022-clock-mode-is-a-per-save-setting-live--pausable)), **Ironman** ([D-026](#d-026-ironman-mode-requires-live-clock-mode)), **draft clock preset** ([D-044](#d-044-draft-clock-presets-tiered-changeable-before-the-draft)), **live event toggles** ([D-046](#d-046-live-events-fa-frenzy-committed-deadline-day-conditional)) and **injury frequency** ([D-037](#d-037-injury-frequency-setting-locked-at-creation)). There's no difficulty setting ([D-042](#d-042-no-difficulty-presets-in-10)). *(Added later: "career can end", [D-063](#d-063-career-can-end-is-a-creation-setting-off-by-default).)*
  - **The cap amount and cap growth are not exposed.** They're tuning values in `LeagueRules`. Editing them belongs to commissioner mode (F-28).
  - **One user-controlled team** in 1.0. Multiple teams are revisited with F-28.
- **Why:** the valuation model is calibrated as a percentage of the cap, so a cap-amount setting is cosmetic and a cap-growth setting can break the 30-season health gate. Every knob multiplies the configurations that health checks have to cover.

## D-037: Injury frequency setting, locked at creation
- **Status:** **Accepted** (owner, 2026-10-14). Resolves Q-17.
- **Decision:** **Low / Realistic (default) / High**, chosen at league creation and **locked** so it can't be dialed down before the playoffs. The 30-season health gate runs on Realistic. Low and High each get a short (5-season) autoplay smoke run in `build.ps1 -Full`, so they can't silently break the league.

## D-038: No preseason games in 1.0
- **Status:** **Accepted** (owner, 2026-10-14, chose "None in 1.0"). Resolves Q-20.
- **Decision:** Training Camp is annual progression at camp open, development focus, cut-down to 53, waivers and the practice squad *(no practice squad in 1.0 since [D-080](#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle))*. **There are no preseason games**, preseason stats or preseason schedule weeks. Parked in [IDEAS.md](IDEAS.md).
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
  - **Not in 1.0:** RFA tenders and offer sheets, the transition tag, void years, incentives (LTBE/NLTBE), offset language, cash-spending floors, compensatory picks. *(The Top 51 rule was listed here too. It's back in v1, see [D-056](#d-056-the-offseason-top-51-cap-rule-is-in-amends-d-040).)*
  - **In place of RFA** (to be confirmed in the F-12/F-15 specs): a player whose contract expires with **fewer than 4 accrued seasons** is an **exclusive-rights** free agent. His team keeps him by making a one-year qualifying offer at the minimum salary for his experience. If it doesn't, he becomes an unrestricted free agent.

## D-041: Finances: owner-funded staff budget in M10 only
- **Status:** **Accepted** (owner, 2026-10-14, chose "Staff budget in M10"). Resolves Q-15.
- **Decision:** the only money besides the salary cap is an **owner-funded staff budget** (scouting, medical, coaching) arriving with F-23 in **M10**. Until then (M6–M9), **every team has equal scouting capacity** (the same weekly scout points), and scouting departments differ only in scout quality. No revenue, tickets, concessions or stadium.
- **Fixes an inconsistency:** F-17 listed "a scouting budget" in M6, before any owner budget existed.
- *How scouting capacity is spent (standing and special assignments rather than weekly points) is defined in [D-058](#d-058-scouting-department-standing--special-assignments-no-biases-in-10). With no medical staff ([D-062](#d-062-coaching-staff-is-head-coach--two-coordinators-only)), the staff budget covers scouting and coaching only.*

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

## D-049: A new league starts at the offseason: "the previous GM was fired"
- **Status:** **Accepted** (owner, 2026-10-14: *"What is the most likely starting point for a new GM, imagine the previous GM was fired. I think it is at the start of the off season"*). The two supporting defaults below were proposed in the same review and are confirmed in the F-01 spec.
- **Decision:**
  - From M5, when the offseason exists, a new league starts at the **Re-sign & Tag Window**. You were hired right after the owner's end-of-season review, so the prior season is complete: champion, awards and retirements.
  - **The prior season is simulated, not invented** (proposed): at creation, the real engine plays the previous season headless with every team AI-run. Standings, results, box scores and season stats are kept, but not play-by-play. That history drives the draft order, schedule rotation, awards, contract talks ("my client had 1,200 yards") and the situation you inherit (expiring deals, cap space, needs).
  - **Your first draft isn't blind** (proposed): your team inherits the previous regime's in-season prospect scouting, equal to an average AI team's default assignments. You add the combine, pro days and interviews yourself.
  - The New League wizard frames the takeover ("You've been hired as GM of …") and shows each team's inherited situation in team selection: last record, cap space, expiring contracts, needs.
  - **Before M5** (M1–M4), development builds start at Training Camp, because the offseason doesn't exist yet.
- **Consequences:** + The hallmark (free agency, then the live draft) arrives in the first session. + A strong narrative hook. − Generation does more work (budget: < 15 s with progress, versus < 5 s for a camp start). − You draft for a team you haven't watched play. The simulated prior season and inherited scouting are the mitigation.
- **Supersedes** the "starts at Training Camp" note in [CALENDAR_AND_PHASES.md](architecture/CALENDAR_AND_PHASES.md). A "start at Training Camp" option is parked ([IDEAS.md](IDEAS.md)).

## D-050: Every player attribute must be read by the sim
- **Status:** **Accepted** (owner, 2026-10-14, chose "Yes: cut or fold unused"). The rating counterpart of [D-043](#d-043-limited-personality-model-only-traits-with-a-consumer).
- **Context:** in [SIMULATION_ENGINE.md §4](architecture/SIMULATION_ENGINE.md#4-play-resolution), Agility, Jumping, Strength and Play Recognition never appear as inputs to a play resolver, while the run pipeline reads a "strip skill" that isn't an attribute. An attribute the sim ignores still inflates OVR and misleads the player page.
- **Decision:**
  - **Every attribute is read by at least one play resolver** in `SimTuning`. Appearing in an OVR formula doesn't count.
  - The F-03 spec contains the **attribute → resolver matrix**. Each unused attribute is either folded into another (e.g., Jumping → Contested Catch, Agility → Elusiveness/Route Running) or wired in on purpose (e.g., Play Recognition in run defense and zone coverage). "Strip skill" resolves to an existing attribute.
  - A test fails if any attribute has no consumer in the tuning file.
- **Consequences:** + Every number on a player page means something on Sunday (pillar 3). + Less calibration surface in M2. − A slightly shorter attribute list than other sims.

## D-051: Onboarding is help pages only, authored in Markdown
- **Status:** **Accepted** (owner, 2026-10-14: *"Help pages only, which can work as a markdown file or something similar."*)
- **Decision:**
  - No scripted tutorial, no "Assistant GM" character, no first-time phase cards.
  - **Help pages** are Markdown files shipped in `data/help/` and rendered in the app (a Markdown library such as Markdig, BSD-2, license-checked at adoption). Help content is trusted shipped content.
  - **Glossary tooltips stay** (F-27, ongoing) and read from a runtime glossary data file.
  - In-flow guidance comes from the Advance button's "what's next" and blocking-issue messages (F-05).
- **Consequences:** + A much smaller M11. + Help is easy to write and keep current. − New GM-sim players get less hand-holding. Parked: phase cards and an Assistant GM ([IDEAS.md](IDEAS.md)).

## D-052: Depth chart: position depth drives every package, with optional overrides
- **Status:** **Accepted** (owner, 2026-10-14, chose "Position depth + overrides" in the feature review).
- **Decision:** the user orders players **by position** (QB1, QB2, RB1…, WR1–WR5, CB1–CB4, …) and sets the returners. **Every package is derived automatically** from that order (offense 11/12/21/22/10/goal line, defense base/nickel/dime/goal line, special teams coverage units). **Optional per-package overrides** exist for power users (e.g., a specific TE in goal-line sets). Locks and the auto depth chart still apply. The AI uses the same model.
- **Why:** hand-managing about 13 packages of ordered slots is the kind of busywork pillar 2 rules out.

## D-053: M3 game day: a minimal text replay
- **Status:** **Accepted** (owner, 2026-10-14, chose "Minimal text replay in M3").
- **Decision:** M3 replays the user's game from its event log as a **scorebug + play-by-play feed** at 1× / 2× / 4× / instant, with other games' scores ticking in. The drive chart, win probability chart and the polished Game Center stay in M8.
- **Why:** M3's exit criterion is "you actually want to play the next week". A box score alone is a weak weekly payoff, and a text replay is cheap because the log already exists.

## D-054: Injury status is Available / Out until playing hurt (M10)
- **Status:** **Accepted** (owner, 2026-10-14, chose "Available / Out until M10").
- **Decision:** from M3, injuries are measured in weeks, and a player is **Available** or **Out** (with an expected return). **Questionable and Doubtful arrive in M10** together with playing hurt, when they mean "may play at reduced effectiveness".
- **Why:** Questionable/Doubtful have nothing behind them until playing hurt exists (the same consumer rule as [D-043](#d-043-limited-personality-model-only-traits-with-a-consumer) and [D-050](#d-050-every-player-attribute-must-be-read-by-the-sim)).

## D-055: M3 messaging: Briefing + news feed; the inbox arrives in M7
- **Status:** **Accepted** (owner, 2026-10-14, chose "Briefing + news; inbox in M7").
- **Decision:** M3 ships the **Front Office Briefing** (the weekly digest, including a "needs your attention" section) and the league **news feed**. The standalone **inbox arrives in M7** with the first items that expire (trade offers), followed by contract demands, owner messages and scout reports.
- **Why:** in M3 the inbox's actionable items (injuries, roster limits) duplicate the Advance flow's blocking issues and warnings.

## D-056: The offseason "Top 51" cap rule is in (amends D-040)
- **Status:** **Accepted** (owner, 2026-10-14: *"Whatever the NFL rule is, I think it is the top 51"*). Amends [D-040](#d-040-salary-cap-realism-tiers).
- **Context:** offseason rosters go up to 90. Without the rule, all 90 contracts count against the cap (about 37 extra near-minimum deals, roughly 12% of the cap), squeezing every team every spring and forcing pre-draft cuts.
- **Decision:** **v1 (M4)** includes the NFL's rule: from the start of the new league year until the regular season begins, only the **51 highest cap hits count, plus all dead money**. After final cuts, every contract counts. The fine print (for example, whether bonus proration for players outside the top 51 still counts) is confirmed against a real reference in the F-12 spec. It's a single pure function in Core, and the AI uses the same function.
- **Default for league rules:** when a rules question comes up, follow the real league's rule unless it adds complexity without adding a decision (the owner's pattern here and in [D-033](#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)).

## D-057: Free agency: blind bids with signals
- **Status:** **Accepted** (owner, 2026-10-14, chose "Blind bids with signals").
- **Decision:** in free agency you see **how many teams are seriously interested** in a player and his **current leaning** ("Leaning: Hawks"), never the amounts or terms of competing offers. **AI teams see exactly the same signals** about your offers. The same model drives FA Frenzy (M9).
- **Why:** open bids let you beat the best offer by $1, which kills the tension. Fully blind bidding feels arbitrary when a player signs elsewhere with no warning.

## D-058: Scouting department: standing + special assignments, no biases in 1.0
- **Status:** **Accepted** (owner, 2026-10-14: *"Standing assignment, on region / position. Special assignment to a specific player. For example standing assignment only gets you so far"*, and "No biases in 1.0"). Replaces the weekly point allocation in the earlier F-17 text.
- **Decision:**
  - **Standing assignments:** each area scout covers a **region** or a **position group**, chosen by you and kept until you change it. Every prospect in that coverage sharpens automatically each week, **down to a floor and no further** ("only gets you so far").
  - **Special assignments:** you send a scout to a **specific prospect** to sharpen him past the standing floor. While a scout is on a special assignment, his standing coverage slows, so where your best scouts go is a real trade-off. The F-17 spec sets the numbers (floor, rates, how many special assignments per scout).
  - **No required weekly input.** Progress accrues as the calendar advances.
  - **Scouts differ in accuracy and position expertise**, which you can see, so choosing who covers what matters. **No scout biases in 1.0** (parked with scout-specific reports, [IDEAS.md](IDEAS.md)).
  - **AI teams use the same model.** Their default assignments are also the baseline your team inherits at a new league's offseason start ([D-049](#d-049-a-new-league-starts-at-the-offseason-the-previous-gm-was-fired)). Capacity is equal across teams until the M10 staff budget ([D-041](#d-041-finances-owner-funded-staff-budget-in-m10-only)).
- **Why:** weekly allocation would be a chore for 18 weeks a year, while standing plus special assignments keeps the decision without the busywork. Biases can't be learned without attributed reports, so in 1.0 they'd just be noise.

## D-059: The UDFA window is one turn-based round of blind offers
- **Status:** **Accepted** (owner, 2026-10-14, chose "One turn-based round").
- **Decision:** the live draft event ends with the last pick. Then, as a turn-based step, you and every AI team submit **blind offers** to undrafted players (up to roster space). All offers **resolve at once**, with players choosing by offer and opportunity (where they'd sit on the depth chart). Undrafted players stay fuzzy until signed ([D-032](#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted)).
- **Why:** a real-time scramble against 31 AI teams acting in seconds would be a race the user mostly loses.

## D-060: A paused live event allows browsing but no actions
- **Status:** **Accepted** (owner, 2026-10-14, chose "Browse, no actions"). Resolves the "decision to confirm" in [LIVE_EVENTS.md §5.1](architecture/LIVE_EVENTS.md#51-pause-pausable-clock-mode-only).
- **Decision:** in a Pausable save, while a live event is paused you can browse everything (scouting reports, the Big Board, team needs, the cap sheet), but picks, trades and offers are disabled until you resume.
- **Why:** Pausable is the accessibility mode. Restricting reading while paused would punish the players it exists for.

## D-061: Trade feedback is qualitative, plus "what would it take?"
- **Status:** **Accepted** (owner, 2026-10-14, chose "Qualitative + 'what would it take'").
- **Decision:** while you build a trade, the AI shows a **reaction scale** (Not interested / Far apart / Getting closer / Close / Deal) and answers **"What would it take?"** by naming what it wants. **No value numbers or balance bar** are shown. The draft value chart for *picks* stays visible, since pick value is public knowledge.
- **Why:** visible valuation numbers let you tune every offer to land exactly on the acceptance line, turning trades into arithmetic and making the AI trivially exploitable.

## D-062: Coaching staff is head coach + two coordinators only
- **Status:** **Accepted** (owner, 2026-10-14: *"HC, OC, DC only"*, chosen over the recommendation that also kept a head of medical).
- **Decision:**
  - F-22's staff is the **head coach** (game management, development, special teams), the **offensive coordinator** and the **defensive coordinator** (scheme and play-calling). The carousel and contracts cover these three roles.
  - **No** special teams coordinator, position coaches, strength staff or medical staff.
  - **Consequences elsewhere:** F-11 loses "medical staff quality (shorter recoveries)" (the injury report stays, run by an implicit team doctor), and the M10 owner-funded staff budget ([D-041](#d-041-finances-owner-funded-staff-budget-in-m10-only)) covers **scouting and coaching** only.
- **Parked:** the other staff roles ([IDEAS.md](IDEAS.md)).

## D-063: "Career can end" is a creation setting, off by default
- **Status:** **Accepted** (owner, 2026-10-14: *"Configurable option, locked at start of game"*). The default (off) was proposed in the same session.
- **Decision:**
  - A league-creation setting, **locked** like the others ([D-036](#d-036-league-settings-at-creation-are-gameplay-only-one-user-team)). It arrives with F-23 in M10.
  - **Off (default):** after a firing, your reputation decides which openings want you, but you **always get at least one offer** (the worst job available).
  - **On:** with a bad enough reputation, no team offers you a job and the career ends with the legacy screen.
  - Either way, you can **choose to retire**, which ends the save gracefully with a legacy screen.
  - Independent of Ironman: an Ironman player may turn it on, but Ironman doesn't force it.

## D-064: F-02 team set accepted; shared city labels
- **Status:** **Accepted** (owner, 2026-10-15, reviewing [F-02](features/F-02_LEAGUE_AND_TEAMS.md)): *"Use New York and Los Angeles like the real league"*; on the Heritage and Frontier conference names, *"Yeah those work for the base version I think most users will import real ones"*; on changing any of the 32, *"For now no, those look good to me"*.
- **Decision:**
  - The two-team metros **share their city label**, as in the real league: **New York** Excelsiors (NYX) and **New York** Shipwrights (NYS, drafted as the Kings, NYK), **Los Angeles** Condors (LAC) and **Los Angeles** Navigators (LAN). Abbreviation, nickname, monogram and colors tell them apart, and the UI never shows the city alone where it would be ambiguous.
  - Conference names are **Heritage** and **Frontier** for the base set.
  - The 32 drafted franchises are accepted as-is. They may still change only if the automated blocklist, color or contrast tests flag them ([F-02 §4.3](features/F-02_LEAGUE_AND_TEAMS.md#43-guardrails-each-one-is-a-test-against-the-data-file)).
- **Note:** the owner expects most players to import a real-league set. The team-set schema already covers conference and division names, so an import can rename those too ([D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)).
- *(Added later, 2026-10-20, M1 WP2: the first run of the §4.3 checks changed the set, as this decision allows, and the owner approved the result: "the names and colors are approved". Four nicknames already used in pro football became the Atlanta **Railmen** (was Aviators), the New York **Shipwrights** (was Kings; `NYK` → `NYS`, monogram `S`), the Houston **Riggers** (was Wildcatters) and the Phoenix **Javelinas** (was Scorpions). The Excelsiors' Empire Field became **Skyline Field**, and eight primary colors moved. [F-02 Appendix A](features/F-02_LEAGUE_AND_TEAMS.md#changes-from-the-first-draft-2026-10-20-m1-wp2) lists each change and why.)*

## D-065: F-03 player model accepted: 42 attributes, 80% ranges, roster mix as averages
- **Status:** **Accepted** (owner, 2026-10-15, reviewing [F-03](features/F-03_PLAYERS.md)): on the attribute list, *"I am good with the 42 attributes"*; after the interval was explained, chose "80% interval"; on roster composition, *"Yes but don't make them strict some team might have more or less but that should be the average"*.
- **Decision:**
  - **42 attributes**, each read by the sim ([F-03 §4.2](features/F-03_PLAYERS.md#42-attributes-and-their-consumers-d-050)). Jumping is folded into Contested Catch and Ball Skills, Play Recognition into Awareness.
  - **Uncertain ratings show an 80% interval** (estimate ± 1.28σ): the truth falls outside the shown range about 1 time in 5. It's one value in data, retunable after playtesting.
  - **Per-position roster targets are league averages**, not rules. Each team varies within a typical range, above a floor that keeps every package fieldable ([F-03 §4.1](features/F-03_PLAYERS.md#41-positions)). The only hard rule stays F-05's "required depth chart slots filled".

## D-066: Git workflow: GFO commit format, one branch per milestone
- **Status:** **Accepted**; the branching part is **amended by [D-076](#d-076-work-happens-on-main-no-milestone-branches)** (work happens on `main`).
- **Originally:** **Accepted** (owner, 2026-10-15, answering the M0 plan's questions: chose "Your current format" for commits, "One M0 branch" for landing work, and "Delete it" for spike code).
- **Decision:**
  - **Commit messages** use the owner's existing format, `(type) *module* : summary` (e.g., `(feat) *m0* : scaffold solution, projects and guardrails`), replacing the Conventional Commits rule in TESTING_AND_QUALITY.md §8.4. Types follow the commit-review skill: `feat`, `bugfix`, `fix`, `refactor`, `perf`, `test`, `doc`, `style`, `chore`, `revert`. **Branches** are `type/short-name`, reusing the same types.
  - **One branch per milestone** (`feat/m0-foundations` for M0), with commits per work package, merged into `main` once the exit checklist passes. Merged without squashing, so the work-package history survives.
  - **Spike code is throwaway:** it's deleted once its results are recorded in DECISIONS. Git history keeps it.
- **Where:** [TESTING_AND_QUALITY.md §8.4](architecture/TESTING_AND_QUALITY.md#84-git-workflow-d-066), [M0_FOUNDATIONS.md §11](milestones/M0_FOUNDATIONS.md#11-questions-for-the-owner-answered-2026-10-15).

## D-067: Spike S0b: Fluent UI v5 fits; `DataGrid<T>` keeps `FluentDataGrid`
- **Status:** **Accepted** (owner, 2026-10-15, after reviewing the spike: chose "Confirm them" for the icon set and "Allow it" for the scoped CSS exception). The grid verdict itself follows D-025's fallback rule, since no P0 item failed. Spike run 2026-10-15 in the DevHost on Fluent UI Blazor 5.0.1.
- **Grid verdict: `FluentDataGrid` passes the P0 checklist with 2,000 rows, so the fallback grid isn't needed.** Virtualization (23 rows in the DOM), a pinned first column, a sticky header, 36 px rows, Razor cell templates and arrow-key navigation (which follows virtualization) all work together. A sort takes 23–52 ms from click to screen. It takes four fixes, all owned by `DataGrid<T>` ([UI_DESIGN_SYSTEM.md §5.3](architecture/UI_DESIGN_SYSTEM.md#53-datagridt-a-gfo-wrapper-over-fluentdatagrid)):
  1. **Two scoped CSS rules, the only sanctioned reach into Fluent's markup.**
     - Virtualize's spacer rows sit in column 1 only. Once the grid scrolls right past that column, the rows stop rendering and the grid goes blank. The rule makes the spacers span every column.
     - The table box is only as wide as its container, so the pinned column un-pins after a long horizontal scroll. The rule sizes the table to its columns.
     - Both rules live in `DataGrid<T>`'s scoped stylesheet and nowhere else, and the gallery page exercises them.
  2. **GFO's own compact header** through the supported `HeaderCellItemTemplate` + `SortByColumnAsync`. One click sorts (numbers high to low first), and a second click flips it. Fluent's default header (a column-options button plus a sort button with a 75 px minimum) overflows 64–72 px stat columns. `SortBy` is always defined ascending, because Fluent reverses it for "descending".
  3. **Column `MinWidth` = `Width`** (Fluent defaults to 100 px). The grid sits in an `overflow: auto` container with a fixed height, and is never given `height: 100%`, which breaks the sticky header.
  4. **Row height is `ItemSize`.** With virtualization on, Fluent sets every cell's height to `ItemSize` and ignores `RowSize`. Density is therefore a C# value the wrapper reads from settings (46 px comfortable, 36 px compact).

  Fluent handles only the arrow keys, so the wrapper also adds PageUp/PageDown, Home/End, Ctrl+Home/End, Enter = open and Space = select.
- **Theme: the team color is used exactly, and GFO computes the accent colors.** `GfoThemeService` works in three steps:
  1. It calls `CreateCustomThemeAsync` with `IsExact = true`.
  2. It overrides the Stadium fonts, radii and surfaces, plus the brand foreground tokens.
  3. It applies the result with `SetThemeAsync(Theme)`.

  Without `IsExact`, a primary button shows a ramp step (`#903B19` for `#D9541E`) instead of the team color. With it, Fluent also uses the raw color for links and brand text, which vanish on dark surfaces when the team color is dark. So the service computes [VISUAL_DESIGN.md §3.3](VISUAL_DESIGN.md#33-team-accent-the-users-franchise-colors)'s on-accent, Accent (≥ 3:1) and Accent text (≥ 4.5:1) itself. This was verified on 7 team colors in Dark and Light.
  - **GFO resolves Match Windows itself.** Given `ThemeMode.System`, Fluent's CSS swaps in its *default* theme when Windows changes mode, dropping the team color and the Stadium tokens. The service hands Fluent Dark or Light and re-applies the theme on a `prefers-color-scheme` change.
  - **Completed by Spike S0c ([D-068](#d-068-host-shell-the-app-bar-is-the-title-bar)):** an explicit mode isn't enough on its own. Fluent's script re-applies its default theme on *every* real `prefers-color-scheme` change, unless its own `localStorage` cache holds an explicit mode, and GFO doesn't use that cache. The browser emulation in S0b never fired change events, so this path was invisible there. The host page now turns Fluent's listener off, and GFO handles every change.
  - **Fluent's `localStorage` theme cache isn't used.** Only the `ThemeSettings` path writes it, and `GetThemeSettingsAsync` reads it back, so the cache goes stale under a custom theme. The service clears it at startup, and `settings.json` stays the source of truth.
- **Overlays work:** the drawer (the future peek sheet), the confirm dialog and the toast. The verified API names are in [UI_DESIGN_SYSTEM.md §5.1](architecture/UI_DESIGN_SYSTEM.md#51-from-fluent-ui-blazor-v5-use-directly-in-pages).
- **Icons: Fluent System Icons are confirmed** (D-031's proposed part). They render cleanly next to Archivo: Regular weight for UI, Filled for active states, behind GFO's `Icon` wrapper. The icons package adds about 22 MB of per-style DLLs to the app.
- **Details:** [M0_FOUNDATIONS.md WP3](milestones/M0_FOUNDATIONS.md#wp3--spike-s0b-fluent-ui-v5-fit-check-m-throwaway-code--done-2026-10-15).

## D-068: Host shell: the app bar is the title bar
- **Status:** **Accepted** (owner, 2026-10-16, after Spike S0c: chose "(b) App bar is title bar" over a slim WPF caption strip and the native Windows title bar).
- **Decision:** the window has **no separate title bar**. The HTML app bar (40 px: crest, team, season line, cap pill, search, primary action) is the title bar, and WPF caption buttons (46 × 40 px, Segoe Fluent Icons) sit over its right end. Native behavior is kept: drag, double-click to maximize, the system menu, Aero Snap, Windows 11 **Snap Layouts** on the maximize button, resize edges and corners, the 1280 × 720 minimum. The composition control lets WPF draw over web content. How it works ([UI_DESIGN_SYSTEM.md §7](architecture/UI_DESIGN_SYSTEM.md#7-app-shell-and-navigation)):
  - `WindowChrome` with `CaptionHeight` 0 keeps the resize border, the DWM shadow and rounded corners.
  - The app bar reports its rectangle and its interactive holes (buttons, inputs) to the host through a small JS module (`ResizeObserver`).
  - A `WM_NCHITTEST` hook answers in this order:
    - `HTMAXBUTTON` over the WPF maximize button, which is what makes Windows show Snap Layouts;
    - the WPF minimize and close buttons are left to WPF;
    - `HTCAPTION` over the bar's empty space;
    - the top resize band is left to `WindowChrome`.
  - A maximized window is padded by its *measured* overhang: 8 px at 100% scale, not the 4 px `WindowResizeBorderThickness` suggests.
- **Why:**
  - It saves 32 px of height on dense screens compared with option (a), which stacks two bars (72 px of chrome).
  - It matches the single top bar in the AppShell design.
  - Its cost is now proven and small: about 150 lines of host code plus a 20-line JS module.
  - The native title bar (31 px) was the zero-code baseline, but it gives the least integrated look and still means two bars.
- **Also found by the spike** (now in the docs; numbers in [HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md#real-shell-spike-s0c-2026-10-16)):
  - **Per-monitor-v2 DPI needs an app manifest.** Without one, WPF is only system-aware, and Windows would bitmap-stretch the window on a monitor with a different scale. `src/Desktop/app.manifest` now declares PerMonitorV2.
  - **First paint is Stadium-colored from the first frame**, through three layers:
    1. the WPF window background and WebView2's `DefaultBackgroundColor` are set natively from the saved mode;
    2. the host page boots as `<html class="gfo-booting">`, transparent with `#app` hidden;
    3. the theme service removes the class once Stadium is applied.

    Without them, the window showed about 220 ms of **white**, then a frame of Fluent's default theme, which is light on a light Windows.
  - **Fluent's system-theme listener is switched off** with the guard flag it checks (`window.__FluentUIBlazorSystemThemeChangeListener = true`, set before Fluent loads). Otherwise Fluent resets to its default theme on every Windows light/dark switch, even under an explicit Dark choice. GFO's listener re-applies the theme on each change. Real WebView2 change events were verified: 4 of 4 switches arrived within about 20 ms.
  - **Host-page scripts can lose the race with Blazor.** A script registered with `AddScriptToExecuteOnDocumentCreatedAsync` didn't run before Blazor's first navigation, so first-paint state is static in `index.html` or set natively. WebView2 also **persists** `PreferredColorScheme` in its profile, so the host never sets it.
  - **Startup:**
    - Warm, about 1.4–1.5 s to a themed screen (median 1.51 s over 8 runs; first render 1.47 s); first-ever launch 1.57 s.
    - WebView2's browser-process start (about 0.6 s) is the largest step.
    - The title bar choice makes no difference.
    - The self-contained ReadyToRun publish is 247 MB.
- **Details:** [M0_FOUNDATIONS.md WP4](milestones/M0_FOUNDATIONS.md#wp4--spike-s0c-host-shell-sm-throwaway-code--done-2026-10-16).

## D-069: Write `ID`, never `Id`
- **Status:** **Accepted** (owner, 2026-10-18: *"All `Id` should be written like `ID` this is my preference"*).
- **Decision:** the abbreviation is always `ID` (plural `IDs`), wherever GFO chooses the name:
  - types, members, parameters, locals and namespaces: `SaveID`, `PlayerID`, `IStronglyTypedID`, `TypedIDs`, `ReadSaveID`, `saveID`, `GridironFrontOffice.Core.IDs`;
  - database columns (they come from the property names): `SaveInfo.SaveID`;
  - log properties: `{SaveID}`, `{SessionID}`;
  - file names and docs.

  A lone camelCase local, a route parameter or HTML attribute stays lowercase: `id`, `/player/{id}`, `<div id="app">`.
- **Exceptions:** names that belong to someone else keep their own spelling, because GFO can't rename them:
  - Microsoft logging's `EventId` (`[LoggerMessage(EventId = …)]`, the `EventId` struct with its `Id` property, and the `eventId` parameter);
  - EF Core's `MigrationId` history column;
  - the `Id` attribute in the Windows app manifest schema.
- **Note:** this departs from the .NET naming guideline, which spells the abbreviation `Id`. It's a deliberate house style. SQLite column names are case-insensitive, so respelling the `SaveID` column changed no existing save.
- **Enforced by** an architecture test (`NamingRulesTests`), which scans source and docs for the mixed-case spelling. A new outside name goes on its `OutsideNames` (or `OutsideMembers`) list.

## D-070: Read models are records named `…Dto`
- **Status:** **Accepted** (owner, 2026-10-18: *"Use the DTO suffix"*, choosing between the guide's `RosterRow` and the specs' `TeamSummaryDto`).
- **Decision:** every read model a query service returns is a `sealed record` whose name ends in `Dto`: `RosterRowDto`, `TeamSummaryDto`, `PlayerDetailDto`, `LeagueHeaderDto`. It lives in the feature folder next to the query service that fills it. The casing is `Dto`, as in the F-02 and F-03 specs.
- **Why:** one suffix makes it obvious at a glance what a screen may hold (a read-only copy, never an entity), and it's the form the specs already used.

## D-071: Screen readers aren't supported in 1.0
- **Status:** **Accepted** (owner, 2026-10-19: *"Option 3"*, answering [Q-31](OPEN_QUESTIONS.md#q-31-screen-readers-cant-see-the-page-the-composition-control)).
- **Context:** the .NET 10 WPF `BlazorWebView` hosts WebView2's composition control, which renders the page as an image. Windows' UI Automation sees only that image and the three WPF caption buttons, so Narrator and other screen readers can't reach the page (found in M0 WP9).
- **Decision:** GFO 1.0 doesn't support screen readers, and says so. The host stays as built ([D-068](#d-068-host-shell-the-app-bar-is-the-title-bar)), with no spike on the HWND-based web view and no UIA bridge.
- **Everything else stays:** keyboard access with visible focus, WCAG AA contrast in both themes, color never the only signal, reduced motion, color-blind-safe ratings, the Pausable clock mode. Components keep their ARIA roles and labels: they cost little, tests find elements by them, the DevHost (a real browser) exposes them, and they're ready if a later host can.
- **Steam:** no "Playable without Vision" claim. The store page's accessibility section lists only what GFO really has (F-29).
- **Why:** no law requires a single-player PC game without chat to support screen readers (US CVAA covers only in-game communication; the EU Accessibility Act exempts microenterprises' services and doesn't list games), Steam doesn't require it, and the fix would reopen the title bar for an uncertain result. This is a personal project aiming for Steam, so the effort goes into the game.
- **Revisit:** with the F-27 accessibility audit in M11, or sooner if WebView2's composition control starts exposing the page to UI Automation.

## D-072: Versions are `0.<milestone>.<build>` until 1.0
- **Status:** **Accepted** (owner, 2026-10-19: *"go ahead with 0.<milestone>.<build>"*).
- **Decision:** the app's version is `0.<milestone>.<build>` until the first Steam release, which is `1.0.0`.
  - **Milestone:** the one being built. M0 is `0.0.x`, M1 is `0.1.x`, … M11 is `0.11.x`. It goes up when a milestone starts, right after the previous one is tagged (`m0-complete` was followed by `0.1.0`).
  - **Build:** goes up for each build handed to anyone outside development (a playtester). Development builds don't bump it.
  - **The commit** is appended automatically: the .NET SDK stamps `0.0.1+94b9338`, so a bug report always points at exact code.
- **One source:** `<VersionPrefix>` in `Directory.Build.props`, edited by hand. Everything reads it through `AppIdentity` (Application, built by each host): the log's banner, `SaveInfo`'s app versions, the main menu (`v0.0.1`, short) and Settings → About (`0.0.1+94b9338 (Debug)`, with Copy for bug reports).
- **Save compatibility stays on the schema version** (the number of migrations, PERSISTENCE.md §3), never the app version, which a save only records.
- **Why:** the version says which milestone a build belongs to at a glance. One line edited at a milestone is simpler than a git-tag tool (MinVer, Nerdbank.GitVersioning) for a solo project; revisit with CI (F-30) if builds go out often.

## D-073: A league's first season is the real-world year it's created in
- **Status:** **Accepted** (owner, 2026-10-20: *"Yeah use the current year in real life, 2026 today"*, answering the M1 plan).
- **Decision:** New League takes the current calendar year as the league's first season (a league created on 2026-10-20 starts at Training Camp of the 2026 season). The year is an input to generation, stored in `SaveInfo`, so **the same seed and the same year always give the same league**. Golden tests pin both. From M5 the D-049 simulated prior season is the year before.
- **Why:** it feels current to the player; passing the year in keeps generation deterministic and testable.

## D-074: Contracts are real from M1; contract actions arrive in M4
- **Status:** **Accepted** (owner, 2026-10-20: *"Contract should be and feel real in the first version of the game, the core to managing a team"*, then chose "Real data now, actions in M4").
- **Decision:** M1 generates **real contracts** for every player, in the real `Contracts` and `ContractYears` tables: 1–7 years, a base salary per year, a signing bonus prorated over at most 5 years, guaranteed money, and each year's cap hit from the real formula (pure functions in Core). Players start mid-contract, so proration and history span earlier seasons. Team cap totals stay under a first-season cap from data, and young players carry rookie-style deals. The Player page shows the contract and the Team page the cap totals.
- **Still M4 (F-12):** the actions and rules: signing, releasing with dead money, the franchise tag, extensions, the rookie wage scale as a rule, minimum salaries as a rule, Top 51, the cap sheet and validation.
- **Amends:** the roadmap's "simple contracts (one row per player with a cap hit)" for M1, and F-03's matching non-goal. F-12 gets an M1-slice spec for the structure and generation rules before WP4.

## D-075: Color-blind tuning is reviewed before a public release
- **Status:** **Accepted** (owner, 2026-10-20: *"No concerned about color blind right now, document in the future as something to review if we go public (sell) this."*).
- **Decision:** no color-blind re-tuning during development. M0's finding stays recorded (in Light mode, the Average, Below and Poor rating colors merge for deuteranopia; VISUAL_DESIGN.md §3.4), and the review is a required item of the M11 accessibility audit (F-27) before any public or paid release. Numbers on rating chips stay, so color is never the only signal.

## D-076: Work happens on `main`; no milestone branches
- **Status:** **Accepted** (owner, 2026-10-20: *"From now on lets stay on main, branching does not matter to me in this repo"*).
- **Decision:** commits go straight to `main`. No milestone branches and no merge step; this amends D-066's "one branch per milestone".
- **Unchanged:** the commit format (`(type) *module* : summary`), a commit per work package (or per meaningful slice), `./build.ps1` before every commit, and a tag per milestone (`m1-complete`, …) when its exit checklist passes, which is also when the version's middle number goes up (D-072).
- **History:** M0 used `feat/m0-foundations`, merged into `main` with `m0-complete` on the merge commit; M1's first commits came through PR #2.


## D-077: Data files carry no version numbers; they change only additively
- **Status:** **Accepted** (owner, 2026-10-20, on the data-file versions in the WP2 data: *"once we settle on a schema that will be pretty locked, and if we ever change it should be considered optional"*, then *"go ahead with the change"*). Amends [D-035](#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11)'s "versioned".
- **Decision:**
  - The files in `data/` (the team set, the college list, and the player, name, place and contract data that follow) have **no version**: no `schemaVersion` or `version` field, and no `.v1` in the file name. The built-in files ship inside the app, so they can't fall out of step with it.
  - **Formats only grow.** A field added later is optional, with a default, so an older file (an M11 import) still loads. A newer file read by an older app fails strict parsing with an error that names the unknown field.
  - If a breaking change is ever unavoidable, a `schemaVersion` field is added then, and a file without one means today's format. Nothing is lost by waiting.
  - A save never records a team-set version: its team data is copied into the save at creation. (The set's name joins the save with M11's import, [D-078](#d-078-m1-builds-for-todays-consumers-fifteen-simplifications).)
- **Unchanged:** the **save's** schema version (the number of EF migrations, [PERSISTENCE.md §3](architecture/PERSISTENCE.md)), which is what refuses a save from a newer app; the published JSON schemas (now unversioned); strict parsing.
- **Consequences:** + No version fields, rules or file renames to maintain. − A breaking format change, if one ever comes, pays for its version field and conversion then.

## D-078: M1 builds for today's consumers: fifteen simplifications
- **Status:** **Accepted** (owner, 2026-10-21, after asking for an over-engineering review of M1: *"approve 1-15, keep the command palette in M1"*). Amends [F-02](features/F-02_LEAGUE_AND_TEAMS.md) and [F-03](features/F-03_PLAYERS.md), [D-077](#d-077-data-files-carry-no-version-numbers-they-change-only-additively)'s save note, and the roadmap's M1 scope (the range display moves to M6).
- **Decision:** M1 builds what M1 itself uses; each piece arrives with its first consumer.
  1. **No reusable data validator yet.** Tests check the shipped data (structure, unique abbreviations and college names). `SeedDataRules` and its located errors arrive with M11's import, which shows them to the player.
  2. **Skills = archetype + shared quality + noise**, not a correlated multivariate normal: the archetype sets each skill's mean, one shared draw (by tier) lifts a player's skills together, and independent noise goes on top. No correlation matrices, no Cholesky.
  3. **The name re-roll is unit-tested** instead of a 100-league blocklist sweep.
  4. **Only the budgets a player feels are checked:** a new league in under 5 s (Release, measured at the exit) and the 2,000-row grid. Component targets (loading, generation, perception) are logged, not asserted.
  5. **No UI golden snapshots in M1:** design changes keep landing. Verify stays for the generator's golden test.
  6. **The stadium is columns on `Teams`:** no `Stadiums` table and no `StadiumID`.
  7. **The user's team is stored once,** in `SaveInfo.UserTeamID` (no `Teams.IsUserTeam`).
  8. **`RatingHistory` arrives with player development** (F-20, M5).
  9. **Personality traits are six columns on `Players`,** like the attributes, instead of a narrow `PlayerTraits` table.
  10. **The team picker shows crest, name, roster strength and cap space;** needs come with the AI's needs model (M5).
  11. **No distribution warnings at runtime;** the distribution tests cover the targets.
  12. **No save columns without a reader:** `SaveInfo.TeamSetName` arrives with M11's import, and the save's `Teams` has no `Metro` (the data file keeps it for its tests).
  13. **Rating ranges render from M6.** Every read model still carries an estimate and a sigma through `IPerception` (σ = 0), so M6 adds the range display without touching read models.
  14. **`GameStateStore` v0 loads when the save opens;** refreshing from domain events arrives with the moving calendar (M3).
  15. **The wizard has a Generate button and an optional seed** (random by default), with no regenerate-on-edit.
- **Unchanged:** the command palette v0 stays in M1 (owner's call), and so does the fog-of-war rule that every read model carries an estimate and an uncertainty.
- **Consequences:** + Fewer tables, classes and tests in M1, and nothing built before its consumer. − Each deferred piece is built when its milestone arrives; all are small.

## D-079: WP3 player data: traits in generation.json, measurable priors deferred, F-12 before contracts
- **Status:** **Accepted** (owner, 2026-10-21, answering the three questions in the [WP3 guide](milestones/M1_WP3_GUIDE.md): *"agree with all three, go ahead"*). Amends [F-03](features/F-03_PLAYERS.md) §4.3, §4.7 and §5.
- **Decision:**
  1. **No `traits.json`.** With the six trait columns ([D-078](#d-078-m1-builds-for-todays-consumers-fifteen-simplifications)), the data is one distribution and the Low/High thresholds, which live in `data/players/generation.json`'s `personality`.
  2. **The measurable "priors" are deferred.** All 11 measurables are generated and stored (the combine needs them in M6), but in M1 only four become ratings: the 40 (Speed), the 10-yard split (Acceleration), the 3-cone and shuttle (Agility) and the bench (Strength). Vertical, broad jump, arm length, hand size, height and weight nudge no skill yet. The nudges come back as a field added to `generation.json` if M2's SimLab shows players feel samey.
  3. **F-12's M1 spec comes next,** drafted by Claude for the owner's review. `contracts.json` is WP3's last slice, after it.
- **Consequences:** + One file fewer, and no prior tuning in M1. − Seven measurables carry no rating signal until the nudges return.

## D-080: F-12's M1 slice: no practice squad in 1.0; pay by league-wide quality, with wiggle
- **Status:** **Accepted** (owner, 2026-10-21, reviewing the first draft of [F-12](features/F-12_CONTRACTS.md)). Amends [D-014](#d-014-fixed-32-team-league-structure-for-10) (the 16-player practice squad) and [D-038](#d-038-no-preseason-games-in-10) (Training Camp's practice-squad step); moves the practice-squad part of F-16 out of 1.0.
- **The owner's words:** *"Lets move practice squad to the next major release of this application that is too complex for version 1 and not impactful enough to dig into"*; on the veteran bands: *"I don't want to be too strict on the min / max, I think these are general rules to follow but there should be wiggle room if possible"*; and to the six questions: *"300M is good"*; practice squad: *"remove practice squad for initial release unless you believe they are necessary to the application"*; guarantees: *"Yes keep them in M1"*; bands: *"I think it can hold a only a max, no need to have min for a backup but I think knowing if a player is a backup is a bit vague. For example a WR might be 5th best on team X but 3rd best on team Y. Team Y buy the rule would pay more since it would be rated as a starter"*; rookie rounds: *"Yes linear is fine"*; experience: *"Yes experience can be 1 number"*.
- **Decision:**
  1. **No practice squad in 1.0.** Claude's answer to "unless you believe they are necessary": they aren't. Rosters are the 53; injury replacements (M3) come from the free-agent pool, which stays at about 150; cut-down day and waivers (F-16, M5) stay. The practice squad, elevations and poaching are parked in [IDEAS.md](IDEAS.md) for the next major release. `PlayerStatus` loses `PracticeSquad`, `generation.json` its `practiceSquad` count, and the player counts drop from ~2,300 to ~1,850 per league.
  2. **The first-season cap is $300,000,000.**
  3. **Guaranteed money is generated and shown in M1** (the Player page); nothing acts on it until M4.
  4. **Pay follows the player, not the depth chart.** A veteran's APY comes from his **league-wide rank at his position** through one curve per position with only a **maximum** in the data (the minimum is his minimum salary), plus a **wiggle** (a clamped random nudge of up to ±20%) so the bands are rules of thumb, not walls. "Starter" and "Backup" for contract length and shares are also league-wide (the top `32 × roster.starters` at a position). F-03's per-team generation tier is unchanged for skills.
  5. **Rookie rounds are linear** between a round's first and last pick.
  6. **Experience is one number** in M1, serving as both accrued and credited seasons; the CBA's two definitions apply from M3.
- **Consequences:** + Fewer rules and tables for 1.0, and a pay model no depth chart can distort. − A deeper team's good backup is paid like the Starter he'd be elsewhere, which is the realistic outcome; the cap-fitting pass absorbs it.

## D-081: WP4 generation: trades fit the cap, ranked draft classes, player rules in Application
- **Status:** **Accepted** (owner, 2026-10-22, answering the questions before the [WP4 guide](milestones/M1_WP4_GUIDE.md): *"Trade to fit sounds right, this is during the initial creation of the league only right?"* (yes: once, inside league generation), then "Ranked classes" and "Application/Players", both as recommended). Amends [F-12](features/F-12_CONTRACTS.md) §4.7, §4.8 and §10–13, and [F-03](features/F-03_PLAYERS.md) §4.5, §4.8 and §6.
- **Why:** a reference run of F-12 as drafted (20 seeds, outside the repo) put teams' cap totals at 37–119% of the cap before fitting, mean 78%. Scaling would have changed about 80% of teams' veteran deals, a third of them by more than 20% (up to ×3.4), so the same player would earn very different money by team, against D-080 #4.
- **Decision:**
  1. **Trades fit the cap.** After every contract is drawn, while a team is outside the 85–97% band, the team furthest over and the team furthest under swap two veterans of the same position and depth tier: the pair whose cap hits best close half the gap between them. Pay stays tied to the player, and the cap shapes rosters. It runs once, at league creation; M4's rules take over from there. Replaces F-12 §4.8 step 4's scaling.
  2. **The pay curve drops from 3.5 to 2.5,** so the league's mean sits inside the band: 92% in the reference run, about 25 trades per league, all 640 teams in the band, spread evenly from 85% to 96%.
  3. **Draft history comes from ranked classes.** For each entry season, the class is ranked by OVR plus noise; the top 70% (at most 7 × 32) were drafted, in that order, spread evenly over the picks. A player still on his rookie deal was drafted by his own team; anyone else by a random team. Two numbers in `generation.json`.
  4. **OVR, the potential grade and the archetype label live in `Application/Players`,** beside the data records they read (weights, grade bands, archetypes). Nothing outside Application needs them: the sim reads raw attributes, and the read models, the AI and SimLab go through Application.
- **Consequences:** + No contract depends on its team; draft picks are unique and track quality; no records move to Core. − The trades move good veterans from teams that can't afford them to teams that can, a little parity the real cap also produces. A team no trade can fix is logged, and the tests fail on it.

## D-082: Team strength from a ladder, carried by the young core
- **Status:** **Accepted** (owner, 2026-10-22, walking through the [WP4 guide](milestones/M1_WP4_GUIDE.md): *"In a normal league there are contenders, mid level, fringe, and tanking teams. I know as time goes on this will naturally form. Is there a way to have it exist from day one?"*; then chose "Ladder + young core" and one cap band for every team ("Not now"), adding *"I won't want to over complicate is so if this is making it difficult to manage we can skip"*. It costs three numbers, a shuffle and one line of generation, so it stays.) Amends [F-03](features/F-03_PLAYERS.md) §4.4 and §4.10.
- **Why:** players drawn independently average out. Across 20 reference leagues, the best and worst teams' starters were only about 6 OVR apart before the cap trades ([D-081](#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application)) and 5 after: no contenders, no tanking teams.
- **Decision:**
  1. **A strength ladder per league:** 32 values evenly spaced from −`range` to +`range` (`generation.json`'s `teamStrength.range`, 0.7), shuffled across the teams by the seed. Every league has the same shape: real contenders, a middle and real bottom teams.
  2. **Carried by the young core:** a team's strength shifts its players' shared quality draw (F-03 §4.4) by `strength × weight`, with `youngWeight` (1.6) for players still within rookie-deal experience (`experience < rookieScale.years`) and `veteranWeight` (0.7) for the rest. A strong team mostly drafted well, which is how real contenders afford their edge: those players are on rookie deals. Free agents have no team and no shift. **Pay is untouched** (D-080 #4).
  3. **The cap is unchanged:** one 85–97% band for every team and the same trades. Cap space that follows strength (contenders at the cap, rebuilding teams with room) is parked in [IDEAS.md](IDEAS.md) for free agency (M4).
  4. **The strength isn't saved or shown.** Screens show roster strength computed from the players (WP8's team picker).
- **Reference run:** starters about 9 OVR apart best to worst after the trades (5 without the ladder), 35–44 trades per league, all 20 leagues in band. How much OVR makes a contender is a question of wins, so `range` is tuned when games exist (M2/M3).
- **Consequences:** + Every league has its story on day one, and the team picker is a real choice. − Three more numbers to tune; the cap trades still take back about a third of the spread.
