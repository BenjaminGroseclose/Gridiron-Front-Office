# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Gridiron Front Office (GFO) is a single-player pro football GM simulation for Windows desktop. It was **restarted from scratch on 2026-10-13. Ignore all git history before that.** **M0 is in progress:** the solution skeleton (projects, references, guardrails, architecture tests, `build.ps1`) is scaffolded, and the work packages in `docs/milestones/M0_FOUNDATIONS.md` are being built in order. `docs/` is the source of truth. Start with `docs/README.md` (reading order), `docs/DECISIONS.md` (what's Accepted vs. Proposed) and `docs/ROADMAP.md` (the current milestone and its exit criteria).

## Commands

```powershell
./build.ps1                       # restore → build (warnings as errors) → fast tests (not Category=Slow), incl. architecture rules
./build.ps1 -Full                 # + slow tests and a Release ReadyToRun publish of the desktop app → ./artifacts/
dotnet test --project tests/Architecture.Tests --filter-class "*PackageRules*"   # one test class (also: --filter-method "*Name*")
dotnet run --project src/DevHost                                                  # browser host on http://localhost:5180
dotnet run --project src/Desktop -- --diag-test=crash                             # Debug: prove a failure path (page|js|task|crash|ui)
dotnet run --project tools/SimLab -- calibrate --seasons 200 --seed 42           # SimLab commands arrive in M2
```
Run `./build.ps1` before every commit. CI doesn't exist yet. It's deferred to F-30 and will just call `build.ps1`.

## Stack (all Accepted; see DECISIONS)

.NET 10 LTS · **WPF + `BlazorWebView`** host (not MAUI) · Blazor UI in Razor Class Libraries · **Fluent UI Blazor v5** · **EF Core 10 + SQLite** (one `.gfo` file per league) · Serilog to `%LOCALAPPDATA%\GridironFrontOffice\logs\gfo-YYYYMMDD.log` · xUnit v3, bUnit, Shouldly, Verify, `FakeTimeProvider`, NetArchTest.

## Architecture: the rules that span many files

**Projects** (`docs/ARCHITECTURE.md` §4): folders use short names, and projects and namespaces use full names (`src/Core/GridironFrontOffice.Core.csproj`). Dependencies flow `Desktop/DevHost/SimLab → UI → UI.Components` and `UI → Application → Simulation → Core`, with `Infrastructure → Application`. These are enforced by `tests/Architecture.Tests`:
- `Core`: pure domain + rules, no dependencies. `Simulation`: depends only on Core. Pure and deterministic.
- `Application`: use cases organized in **feature folders** (`Draft/`, `FreeAgency/`, …), ports, AI GMs (pure: snapshot in → intents out), the phase machine, the live event runner. Its only data dependency is the `Microsoft.EntityFrameworkCore` core package (via `ILeagueDb`).
- `Infrastructure`: `LeagueDbContext`, migrations, `SaveWriter`/`SaveReader`, `SaveManager`, snapshots, logging bootstrap.
- `UI.Components`: app-only, football-agnostic layer over Fluent. `UI`: screens + football composites. Neither references Infrastructure, EF or WPF.
- `Desktop`: a thin WPF composition root. `DevHost`: a browser host for UI work and the `/_gallery` route.

**Data access** (read `docs/architecture/DATA_ACCESS_GUIDE.md` before touching data):
- Writes go **only** through `ISaveWriter.ExecuteAsync(op, (uow, ct) => …)`. One command = one `DbContext` = one connection = one transaction. `SaveWriter` is the **only** code that begins, commits or rolls back.
- Reads go through `ISaveReader.QueryAsync` and **project to DTOs**. Entities never reach the UI.
- **Never** `AddDbContext`/`AddDbContextFactory`, inject a context or connection, call `BeginTransaction`/`TransactionScope`, `new SqliteConnection` outside Infrastructure, or call a command from inside a command (it throws). Share logic as operations that take `IUnitOfWork`.
- Business-rule failures return `Result.Fail(DomainError…)`, never exceptions. Never catch persistence exceptions inside a command.
- Bulk inserts use `uow.CreateCommand(sql)` (same transaction). Long operations ("Play Week") are **many short commands** recorded in the step journal, never one giant transaction. Simulation and AI thinking run *outside* write transactions.
- Real-time commands (draft picks, offers) carry an expected `Version` that's checked inside the transaction.
- Money is `long` dollars (never `decimal`/`double`). In-game dates are `DateOnly`. Saves are WAL mode. Snapshots and exports use `VACUUM INTO`, never a file copy.

**Determinism:** all randomness comes from the in-house PRNG with **derived seeds** (`Hash(leagueSeed, purpose, id)`). `System.Random`, `DateTime.Now/UtcNow`, `Thread.Sleep`, `.Result`/`.Wait()` and `async void` are banned in Core, Simulation and Application. Time comes from `TimeProvider` (live events) or the league date.

**Two clocks:** the league calendar (`DateOnly` + `Phase`) moves only on Advance. Wall-clock `TimeProvider` is used only by live events (the draft). Live events honor the save's **clock mode** (Live = accelerate, never pause; Pausable). Restore, duplicate and export are disabled during live events. **Ironman** requires Live and disables restore and duplicate (D-026).

**UI** (`docs/architecture/UI_DESIGN_SYSTEM.md`): use Fluent v5 primitives directly in screens. Use GFO wrappers only for `DataGrid<T>`, `GfoThemeService`, `IGfoDialogs`, `IGfoToasts` and `Icon`. Style with Fluent tokens + `--gfo-*` extension tokens only: no raw hex, no deep selectors into Fluent internals (the one exception is `DataGrid<T>`'s two scoped layout fixes, D-067). Theme changes go through `GfoThemeService`, which builds a custom `Theme` from the **exact** team color + an explicit Dark/Light mode (never `ThemeMode.System`; mode and color always together, v5 issue #5351), computes the accent text colors itself, and re-applies on every Windows light/dark switch (Fluent's own listener is disabled in the host page, D-068). Blazor handlers return `Task`. Refresh UI from domain events, not manual cross-page calls. **Visual values** (Stadium direction; Dark default + Light + Match Windows; Archivo with a condensed display width; 8/14/20 px radii and pills; team-color hero bands) come from `docs/VISUAL_DESIGN.md`. Check new screens against its §10 QA checklist in both modes and with several team colors.

## Gotchas already found

- `Desktop` **must** target `net10.0-windows10.0.19041.0`. The .NET 10 WPF `BlazorWebView` uses WebView2's composition control and crashes at startup on plain `net10.0-windows`. Publish with `PublishReadyToRun=true` (`docs/architecture/HOST_BENCHMARK.md`).
- **Fluent UI Blazor v5** (5.0.1 pinned) was verified in Spike S0b (D-067). The non-obvious API names:
  - drawers are `IDialogService.ShowDrawerAsync<T>`, with the content inside `<FluentDialogBody>`;
  - toasts use `INotificationService` (there's no `IToastService` or `FluentDrawer`);
  - `ShowConfirmationAsync(message, title, primaryButton, secondaryButton)` takes its arguments in that order;
  - `FluentIcon` takes `Value`, not `Icon`.
- **`FluentDataGrid` rules** (UI_DESIGN_SYSTEM §5.3):
  - row height is `ItemSize` (`RowSize` is ignored when virtualized);
  - never put `height: 100%` on the grid (it breaks the sticky header);
  - set `MinWidth` = `Width` (Fluent defaults to 100 px);
  - define `SortBy` ascending.
- **Verify UI in a visible browser pane.** In a hidden pane `IntersectionObserver` stalls, so virtualized grids look broken.
- **Desktop host (Spike S0c, D-068):**
  - `app.manifest` must keep PerMonitorV2: without it WPF is only system DPI aware, which looks blurry on mixed-scale monitors.
  - The first paint depends on static state: `<html class="gfo-booting">` in `index.html`, plus native Ground backgrounds. A script registered with `AddScriptToExecuteOnDocumentCreatedAsync` can run *after* Blazor's first navigation.
  - WebView2 persists `PreferredColorScheme` in its profile, so never set it.
  - The app bar is the title bar: put interactive elements inside it as real buttons and inputs, or mark them `[data-no-drag]`, so they aren't drag area.
- **Tests run on Microsoft.Testing.Platform** (`global.json` → `"test": { "runner": … }`). The .NET 10 SDK refuses the old VSTest bridge for xUnit v3, so don't add `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`. Filters are xUnit v3 options (`--filter-class`, `--filter-method`, `--filter-trait`, `--filter-not-trait "Category=Slow"`), not VSTest `--filter` expressions. A test project with zero tests exits with code 8, which `build.ps1` accepts.
- **Guardrails live in the build:** banned APIs are three tiers in `build/BannedSymbols.*.txt` (transactions everywhere, purity rules for Core/Simulation/Application, raw SQLite outside Infrastructure), wired by `Directory.Build.props`. EF Core is referenced by Application with `PrivateAssets="compile"`, so UI code that touches EF types doesn't compile. Package versions live only in `Directory.Packages.props`.
- **Logging (WP5, LOGGING.md):**
  - Log through `ILogger<T>` with `[LoggerMessage]` methods. The analyzers require it (CA1848) and want computed arguments in a local first (CA1873).
  - Serilog types stay in Infrastructure and the hosts (architecture test).
  - Logs: the real app writes to `%LOCALAPPDATA%\GridironFrontOffice\logs`, the DevHost to `GridironFrontOffice-Dev\logs`.
  - The DevHost **doesn't prerender**: a disposed prerender scope makes Fluent v5 fault background tasks, and the desktop WebView never prerenders anyway.
- **EF Core + SQLite (Spike S0a):** EF's SQLite convention doesn't give typed-ID keys `AUTOINCREMENT`, so `LeagueDbContext` must apply `UseAutoincrement()` to them (EF 10 ignores the raw `Sqlite:Autoincrement` annotation). EF change tracking costs about 100 µs per inserted entity, so league generation and draft classes use the raw `uow.CreateCommand` bulk path. Migration files are marked as generated code in `.editorconfig`.
- The sim calibration bands in `SIMULATION_ENGINE.md` §9.2 are from memory. Verify them against a real reference before tuning.

## Workflow

- **Docs first.** Write a feature spec (`docs/features/_TEMPLATE.md` → `F-xx_NAME.md`) right before building a feature. Keep code and docs in agreement within the same change.
- **Git (D-066):** commit messages are `(type) *module* : summary` (types `feat`, `bugfix`, `fix`, `refactor`, `perf`, `test`, `doc`, `style`, `chore`, `revert`), never Conventional Commits. Branches are `type/short-name`. Each milestone is one branch (`feat/m0-foundations`) with commits per work package, merged unsquashed when its exit checklist passes. Spike code is deleted once its results are recorded.
- **IDs:** features `F-xx`, milestones `M-x`, decisions `D-xxx`, questions `Q-xx`. When a question is answered: add a `D-xxx` (quote the owner's exact words), replace the question body with a pointer, update every affected doc, and update **this file** if a rule changed.
- Stay inside the current milestone's scope (`docs/ROADMAP.md`). Park new ideas in `docs/IDEAS.md` instead of building them.
- Every feature ships with its **AI counterpart** (AI teams use the same commands and validation as the user; no information cheating). There are no difficulty presets (D-042): the AI judges the user's offers with exactly the logic it applies to other AI teams.
- **Fog of war (D-032):** a team sees its own roster's current ratings exactly, other teams' players with a fixed derived fuzz that never sharpens, and prospects only through scouting. The sim always uses true ratings. Read models carry an estimate *and* an uncertainty everywhere a rating is shown.
- **Steam-ready licensing:** check a package's license before adding it. No MediatR, AutoMapper, FluentAssertions v8+ or other commercial-license packages. Fictional teams, players and names only (real city names with original nicknames are fine, D-033). Team sets and college lists are data files; generators never hard-code them (D-035).
- Definition of Done: `docs/architecture/TESTING_AND_QUALITY.md` §8.5. The data-change checklist: `DATA_ACCESS_GUIDE.md` §10.
