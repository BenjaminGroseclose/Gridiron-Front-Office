# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Gridiron Front Office (GFO) is a single-player pro football GM simulation for Windows desktop. It was **restarted from scratch on 2026-10-13. Ignore all git history before that.** The repo is currently **docs only (pre-M0)**. `docs/` is the source of truth. Start with `docs/README.md` (reading order), `docs/DECISIONS.md` (what's Accepted vs. Proposed) and `docs/ROADMAP.md` (the current milestone and its exit criteria).

## Commands (planned in M0; they don't exist until M0 scaffolding lands)

```powershell
./build.ps1                       # restore → build (warnings as errors) → fast tests (Category!=Slow), incl. transaction torture tests
./build.ps1 -Full                 # + slow gates: SimLab calibration, 30-season autoplay, benchmarks → ./artifacts/
dotnet test tests/Core.Tests --filter "FullyQualifiedName~ContractMath"   # a single test class or method
dotnet run --project tools/SimLab -- calibrate --seasons 200 --seed 42     # also: autoplay | tables | bench | abtest
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

**UI** (`docs/architecture/UI_DESIGN_SYSTEM.md`): use Fluent v5 primitives directly in screens. Use GFO wrappers only for `DataGrid<T>`, `GfoThemeService`, `IGfoDialogs`, `IGfoToasts` and `Icon`. Style with Fluent tokens + `--gfo-*` extension tokens only: no raw hex, no deep selectors into Fluent internals. Theme changes go through `GfoThemeService`, which always passes the **full `ThemeSettings`** (mode + color; v5 issue #5351). Blazor handlers return `Task`. Refresh UI from domain events, not manual cross-page calls. **Visual values** (Stadium direction; Dark default + Light + Match Windows; Archivo with a condensed display width; 8/14/20 px radii and pills; team-color hero bands) come from `docs/VISUAL_DESIGN.md`. Check new screens against its §10 QA checklist in both modes and with several team colors.

## Gotchas already found

- `Desktop` **must** target `net10.0-windows10.0.19041.0`. The .NET 10 WPF `BlazorWebView` uses WebView2's composition control and crashes at startup on plain `net10.0-windows`. Publish with `PublishReadyToRun=true` (`docs/architecture/HOST_BENCHMARK.md`).
- Fluent UI Blazor v5 is new (5.0.0, 2026-09-25). API names marked "(v5)" in the docs are unverified until Spike S0b. Check them before relying on them.
- The sim calibration bands in `SIMULATION_ENGINE.md` §9.2 are from memory. Verify them against a real reference before tuning.

## Workflow

- **Docs first.** Write a feature spec (`docs/features/_TEMPLATE.md` → `F-xx_NAME.md`) right before building a feature. Keep code and docs in agreement within the same change.
- **IDs:** features `F-xx`, milestones `M-x`, decisions `D-xxx`, questions `Q-xx`. When a question is answered: add a `D-xxx` (quote the owner's exact words), replace the question body with a pointer, update every affected doc, and update **this file** if a rule changed.
- Stay inside the current milestone's scope (`docs/ROADMAP.md`). Park new ideas instead of building them.
- Every feature ships with its **AI counterpart** (AI teams use the same commands and validation as the user; no information cheating).
- **Steam-ready licensing:** check a package's license before adding it. No MediatR, AutoMapper, FluentAssertions v8+ or other commercial-license packages. Fictional teams, players and names only.
- Definition of Done: `docs/architecture/TESTING_AND_QUALITY.md` §8.5. The data-change checklist: `DATA_ACCESS_GUIDE.md` §10.
