# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Gridiron Front Office (GFO) is a single-player pro football GM simulation for Windows desktop. It was **restarted from scratch on 2026-10-13. Ignore all git history before that.** **M0 is complete** (tagged `m0-complete`, 2026-10-20; `docs/milestones/M0_FOUNDATIONS.md` records what was built). **M1 · The League Exists is next.** Design changes from the Claude Design review land throughout development. `docs/` is the source of truth. Start with `docs/README.md` (reading order), `docs/DECISIONS.md` (what's Accepted vs. Proposed) and `docs/ROADMAP.md` (the current milestone and its exit criteria).

## Commands

```powershell
./build.ps1                       # restore → build (warnings as errors) → fast tests (not Category=Slow), incl. architecture rules
./build.ps1 -Full                 # + slow tests and a Release ReadyToRun publish of the desktop app → ./artifacts/
dotnet test --project tests/Architecture.Tests --filter-class "*PackageRules*"   # one test class (also: --filter-method "*Name*")
dotnet watch --project src/DevHost                                                # browser host with hot reload on http://localhost:5180 (VS Code: task "watch: DevHost")
dotnet run --project src/Desktop -- --diag-test=crash                             # Debug: prove a failure path (page|js|task|crash|ui)
dotnet run --project tools/SimLab -- calibrate --seasons 200 --seed 42           # SimLab commands arrive in M2
dotnet ef migrations add <Name> --project src/Infrastructure --output-dir Data/Migrations   # a schema change (dotnet tool restore first)
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
- Reads go through `ISaveReader.QueryAsync` and **project to DTOs**: `sealed record`s named `…Dto`, next to their query service (D-070). Entities never reach the UI.
- **Never** `AddDbContext`/`AddDbContextFactory`, inject a context or connection, call `BeginTransaction`/`TransactionScope`, `new SqliteConnection` outside Infrastructure, or call a command from inside a command (it throws). Share logic as operations that take `IUnitOfWork`.
- Business-rule failures return `Result.Fail(DomainError…)`, never exceptions. Never catch persistence exceptions inside a command.
- Bulk inserts use `uow.CreateCommand(sql)` (same transaction). Long operations ("Play Week") are **many short commands** recorded in the step journal, never one giant transaction. Simulation and AI thinking run *outside* write transactions.
- Real-time commands (draft picks, offers) carry an expected `Version` that's checked inside the transaction.
- Money is `long` dollars (never `decimal`/`double`). In-game dates are `DateOnly`. Saves are WAL mode. Snapshots and exports use `VACUUM INTO`, never a file copy.

**Determinism:** all randomness comes from the in-house PRNG with **derived seeds** (`Hash(leagueSeed, purpose, id)`). `System.Random`, `DateTime.Now/UtcNow`, `Thread.Sleep`, `.Result`/`.Wait()` and `async void` are banned in Core, Simulation and Application. Time comes from `TimeProvider` (live events) or the league date.

**Two clocks:** the league calendar (`DateOnly` + `Phase`) moves only on Advance. Wall-clock `TimeProvider` is used only by live events (the draft). Live events honor the save's **clock mode** (Live = accelerate, never pause; Pausable). Restore, duplicate and export are disabled during live events. **Ironman** requires Live and disables restore and duplicate (D-026).

**UI** (`docs/architecture/UI_DESIGN_SYSTEM.md`): use Fluent v5 primitives directly in screens. Use GFO wrappers only for `DataGrid<T>`, `GfoThemeService`, `IGfoDialogs`, `IGfoToasts` and `Icon`. Style with Fluent tokens + `--gfo-*` extension tokens only: no raw hex, no deep selectors into Fluent internals (the one exception is `DataGrid<T>`'s scoped stylesheet: the two layout fixes from D-067 and the selected-row paint). Theme changes go through `GfoThemeService`, which builds a custom `Theme` from the **exact** team color + an explicit Dark/Light mode (never `ThemeMode.System`; mode and color always together, v5 issue #5351), computes the accent text colors itself, and re-applies on every Windows light/dark switch (Fluent's own listener is disabled in the host page, D-068). Blazor handlers return `Task`. Refresh UI from domain events, not manual cross-page calls. **Visual values** (Stadium direction; Dark default + Light + Match Windows; Archivo with a condensed display width; 8/14/20 px radii and pills; team-color hero bands) come from `docs/VISUAL_DESIGN.md`. Check new screens against its §10 QA checklist in both modes and with several team colors.

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
- **Persistence (WP6, DATA_ACCESS_GUIDE.md §3):**
  - The schema version **is** the number of EF migrations, so adding one bumps it.
  - A save with an unknown migration is "from a newer app" and is refused.
  - Domain-event subscribers run on the thread that finished the command (`ConfigureAwait(false)`), not the UI thread, so marshal with `InvokeAsync`.
  - The torture tests (`tests/Infrastructure.Tests/Data`) must stay green on every build. The Crasher kill test launches a separate process.
- **Save lifecycle (WP7, PERSISTENCE.md §2, §9, §10):**
  - Open save files read-only only through `SqliteFile.ReadOnly`. A plain read-only connection to a closed WAL save leaves `-wal`/`-shm` files behind, and SQLite can't remove them from a read-only connection.
  - `VACUUM INTO` writes a rollback-journal file, so a snapshot that becomes the live save is switched back to WAL.
  - Every host registers `IPlatformServices` (the Crasher too); `AddGfoInfrastructure` doesn't. Tests use `FakePlatformServices`, so a build never touches the real Recycle Bin.
  - Restore and delete are `SaveManager` operations, and their rules (`SaveRules`) are checked there, not just in the UI.
- **Design system (WP8, UI_DESIGN_SYSTEM.md):**
  - `UI.Components` can't see Application, so `GfoThemeService.ApplyAsync` takes a `GfoAppearance`; `AppearanceSync` maps the settings onto it and re-applies on every change.
  - Fluent's grid only collects columns rendered directly in its content: GFO's `DataGridColumn`/`DataGridTemplateColumn` subclass Fluent's columns. Set `TItem` explicitly on `DataGrid` when binding its callbacks.
  - Fluent sets `body { overflow: hidden }`: pages scroll inside the shell's content area. A scroll frame in a flex or grid layout needs `contain: inline-size`.
  - Fluent's cells swallow key events inside Blazor's dispatch, and Fluent's `Element` references can't be focused from .NET: grid keys and focus moves go through small modules (`gfo-grid.js`, `gfo-dom.js`).
  - A folder `_Imports.razor` with `@layout X` also applies to `X` if it's in that folder, and the router then loops silently. Keep layouts outside the folder.
  - `::deep` needs an element of the component's own to anchor on. Style Fluent elements only through classes GFO adds via parameters (`Class`, `ActiveClass`).
  - Fluent's `IDialogService`/`INotificationService` can't be faked (internal members): test the overlay wrappers against real `FluentProviders` in bUnit.
  - The running DevHost locks its DLLs: stop it before building. Two ways to run it, never both at once (one port, one output folder): the `watch: DevHost` task (`dotnet watch`, also what the Claude preview uses) applies saved `.razor`, `.razor.css` and `.cs` changes to the running app and refreshes the browser, and restarts it for an edit hot reload can't apply; F5 "DevHost (browser)" is the debugger, where C# Dev Kit applies `.cs` and `.razor` changes but not CSS. A debugger can't be attached to the watcher's app: the runtime then refuses the watcher's code patches. The watcher only knows files that existed when it started: after adding a `.cs` or `.razor` file, restart it.
- **Desktop window (WP9, UI_DESIGN_SYSTEM.md §7):**
  - The page talks to the window through WebView2's own `chrome.webview.postMessage` (JSON strings from `gfo-host.js`, read by `PageMessages`), not Blazor interop. Pages mark drag areas with `data-drag-region`, and custom controls inside them with `data-no-drag`.
  - Page zoom stays off: the hit test assumes CSS px = DIPs.
  - Don't mark the caption buttons `IsHitTestVisibleInChrome`: WindowChrome then answers `HTCLIENT` over them even in the top resize band.
  - A window restored maximized is positioned while hidden, then shown with `ShowWindow(SW_SHOWMAXIMIZED)`. `WindowState = Maximized` in the constructor creates it maximized, and Restore then goes to Windows' default spot.
  - The app is single-instance: a second launch only brings the running window forward, so close it before running a new build.
  - The composition control is invisible to UI Automation (only an `Image`), so screen readers aren't supported in 1.0 (D-071). To script the running page, launch with `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222` and use the DevTools protocol. The page's origin is `https://0.0.0.1/`.
- **Core (M1 WP1):**
  - Never use `string.GetHashCode()` or `HashCode.Combine` for anything that must repeat: .NET randomizes them per process. Seeds come from `Seeds.Derive` (FNV-1a 64 + SplitMix64's `Mix`).
  - Naming analyzers fail the build: CA1720 rejects members named after types (`Single`, `Double`), CA1711 reserves the `Attribute` suffix (`PlayerAttribute` carries a justified suppression), and CA1716 rejects members named after Visual Basic keywords (`Exit`: the platform port's member is `Quit`).
  - Typed IDs are one line each in `Core/IDs/EntityIDs.cs`. A typed-ID key only gets `AUTOINCREMENT` when it's marked generated: EF treats a converted key as not generated (see WP5 in the M1 plan).
- **Built-in data (M1 WP2–WP3):**
  - The built-in team set and college list are embedded in Infrastructure (`league/teams.json`, `league/colleges.json`) and parsed by `EmbeddedSeedData.JsonOptions`, which rejects unknown, missing and null fields. A field added later needs a default value, or every older file fails (D-077).
  - The blocklists (`data/blocklists/`) are test-only except `famous-people.json`, which ships; `RepoFiles` reads the others from the repository. A failing guardrail test means fixing the data (F-02 Appendix A records it), never loosening a threshold.
  - The WP3 files key dictionaries on enums (`"CB"`, `"Forty"`, `"Starter"`): the strict options parse them as the C# names, and a misspelled key is a `JsonException`. The records in `Application/Players/PlayerData.cs` and `Application/Contracts/ContractData.cs` are the files' shapes, not tables: the tables arrive with WP5.
  - The build doesn't enforce whitespace (`.editorconfig`'s spaces and final newline). `dotnet format whitespace --folder --include <paths>` fixes it.
- **EF Core + SQLite (Spike S0a):** EF's SQLite convention doesn't give typed-ID keys `AUTOINCREMENT`, so `LeagueDbContext` must apply `UseAutoincrement()` to them (EF 10 ignores the raw `Sqlite:Autoincrement` annotation). EF change tracking costs about 100 µs per inserted entity, so league generation and draft classes use the raw `uow.CreateCommand` bulk path. Migration files are marked as generated code in `.editorconfig`.
- The sim calibration bands in `SIMULATION_ENGINE.md` §9.2 are from memory. Verify them against a real reference before tuning.

## Workflow

- **Docs first.** Write a feature spec (`docs/features/_TEMPLATE.md` → `F-xx_NAME.md`) right before building a feature. Keep code and docs in agreement within the same change.
- **Git (D-066, D-076):** commit messages are `(type) *module* : summary` (types `feat`, `bugfix`, `fix`, `refactor`, `perf`, `test`, `doc`, `style`, `chore`, `revert`), never Conventional Commits. **Work on `main`**, no branches. A commit per work package (or per meaningful slice), and a tag (`m1-complete`, …) when a milestone's exit checklist passes. Spike code is deleted once its results are recorded.
- **Versions (D-072):** `0.<milestone>.<build>` until 1.0, set only in `Directory.Build.props` (`VersionPrefix`). Raise the middle number when a milestone starts (right after the previous milestone's tag), the last for each build handed out. Save compatibility is the schema version, never the app version.
- **Write `ID`, never `Id` (D-069):** `SaveID`, `PlayerID`, `IStronglyTypedID`, `saveID`, `IDs`, in code, columns, log properties and docs. Only outside names keep `Id` (`EventId`, `MigrationId`); `NamingRulesTests` enforces it.
- **IDs:** features `F-xx`, milestones `M-x`, decisions `D-xxx`, questions `Q-xx`. When a question is answered: add a `D-xxx` (quote the owner's exact words), replace the question body with a pointer, update every affected doc, and update **this file** if a rule changed.
- Stay inside the current milestone's scope (`docs/ROADMAP.md`). Park new ideas in `docs/IDEAS.md` instead of building them.
- Every feature ships with its **AI counterpart** (AI teams use the same commands and validation as the user; no information cheating). There are no difficulty presets (D-042): the AI judges the user's offers with exactly the logic it applies to other AI teams.
- **Fog of war (D-032):** a team sees its own roster's current ratings exactly, other teams' players with a fixed derived fuzz that never sharpens, and prospects only through scouting. The sim always uses true ratings. Read models carry an estimate *and* an uncertainty everywhere a rating is shown.
- **Steam-ready licensing:** check a package's license before adding it. No MediatR, AutoMapper, FluentAssertions v8+ or other commercial-license packages. Fictional teams, players and names only (real city names with original nicknames are fine, D-033). Team sets and college lists are data files; generators never hard-code them (D-035). Data files carry no version number and only grow: a field added later is optional, with a default (D-077).
- Definition of Done: `docs/architecture/TESTING_AND_QUALITY.md` §8.5. The data-change checklist: `DATA_ACCESS_GUIDE.md` §10.
