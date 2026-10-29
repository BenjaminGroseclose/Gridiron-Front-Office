# Testing, Quality & Engineering Conventions

> **Status:** Proposed · 2026-10-13 · Related: [D-018](../DECISIONS.md#d-018-testing-stack)

---

## 1. Philosophy

- **Test the rules hard, test the UI lightly.** Cap math, tiebreakers, the sim engine and AI valuations are where silent bugs ruin a 20-season league.
- **Determinism is a testing superpower.** Fixed seeds turn "flaky simulation" into reproducible golden tests.
- **Some tests are statistical.** Calibration and league health are tested with tolerances over many simulated seasons. They run in the slow gate (`build.ps1 -Full`, nightly once CI exists), not on every build.
- **Fast feedback:** the fast suite (`build.ps1`) runs in under 2 minutes. Run it before every commit.

---

## 2. Test projects

| Project | Scope | Tools | Runs |
|---|---|---|---|
| `GridironFrontOffice.Core.Tests` | Pure rules: cap hits, proration, dead money, tiebreakers, roster validation, overall formulas, draft order | xUnit v3, Shouldly, property tests (hand-rolled generators or FsCheck) | Every build |
| `GridironFrontOffice.Simulation.Tests` | Engine units (resolvers), determinism, golden games, property tests (monotonicity) | xUnit, JSON snapshots | Every build (fast subset) + slow gate (statistical) |
| `GridironFrontOffice.Application.Tests` | Use cases against a **real SQLite temp file**: signing, trading, the phase machine, journal resume, the live draft with `FakeTimeProvider`, AI decision units, the golden league | xUnit, `FakeTimeProvider`, JSON snapshots | Every build |
| `GridironFrontOffice.Infrastructure.Tests` | **Transaction torture tests** ([DATA_ACCESS_GUIDE.md §9](DATA_ACCESS_GUIDE.md#9-proving-it-the-torture-tests-m0-run-on-every-build), including a kill-the-process test), migrations (fixture saves from each released schema), snapshots, export/import, integrity handling, bulk writers | xUnit | Every build |
| `GridironFrontOffice.UI.Components.Tests` | Components: rendering, parameters, keyboard, ARIA | bUnit | Every build |
| `GridironFrontOffice.UI.Tests` | Screens with fake query services | bUnit | Every build |
| `GridironFrontOffice.Desktop.Tests` | The desktop host's plain logic: window placement, the page's messages, the single-instance guard | xUnit | Every build |
| `GridironFrontOffice.Architecture.Tests` | Dependency rules ([ARCHITECTURE.md §4.1](../ARCHITECTURE.md#41-dependency-rules)), banned APIs | NetArchTest (MIT) | Every build |
| *(optional)* E2E | Smoke flows through `GridironFrontOffice.DevHost` | Playwright for .NET | Slow gate, from M3 |

> **Why a real SQLite file and not the EF in-memory provider?** The in-memory provider doesn't enforce constraints or transactions and doesn't behave like SQLite. A temp file per test (or `Data Source=:memory:` with a kept-open connection) is fast enough and catches real problems.

---

## 3. Determinism and golden tests

- **Snapshots are hand-rolled** ([D-085](../DECISIONS.md#d-085-the-golden-league-is-a-hand-rolled-json-snapshot-verify-is-dropped)): the test serializes a summary to JSON and compares it with `<Test>.<name>.verified.json` next to it, line endings ignored. On a mismatch it writes `<Test>.<name>.received.json` and fails; reading the diff and renaming the received file over the verified one approves the change.
- **Golden league:** `Generate(seed: 42, season: 2026)` → a canonical summary (per team its strength, position counts, Starters' mean OVR and cap total; the league's top cap hits; a few players in full) → a snapshot. Any unintended change to generation fails the test.
- **Golden games:** a set of about 20 canonical `SimGameInput`s → `SimGameResult` summary snapshot. Tuning changes require approving new snapshots on purpose (and bumping `TuningVersion`).
- **Re-run equality:** simulate the same input twice in one process and in parallel → byte-identical event logs.

---

## 4. Statistical gates (slow gate: `build.ps1 -Full`)

| Gate | Command | Pass criteria |
|---|---|---|
| **Calibration** | `simlab calibrate --seasons 200` | All metrics inside the bands in [SIMULATION_ENGINE.md §9.2](SIMULATION_ENGINE.md#92-initial-target-bands) |
| **Game plan effect size** | `simlab abtest gameplan` | Optimal vs. default is +2–5% win probability |
| **League health** | `simlab autoplay --seasons 30 --seeds 5` | See §5 |
| **Performance** | `simlab bench` + BenchmarkDotNet | No budget regression > 20% |

---

## 5. League health metrics (from M5)

A 30-season autoplay (AI controls all 32 teams) must keep these inside bands **every season**:

| Metric | Healthy band (initial guess, tune later) |
|---|---|
| Teams over the cap at the start of the regular season | 0 |
| Average team cap space at the start of the regular season | 2–8% of the cap |
| Roster count at final cuts | Exactly 53 for all teams |
| Talent distribution: mean starter OVR by position | Within ±3 of the season-1 baseline (no inflation or deflation) |
| Number of 90+ OVR players league-wide | Within ±25% of the baseline |
| Average player age | 25.5–27.5 |
| Distinct champions over 30 seasons | ≥ 12 |
| Longest playoff drought / streak | Plausible (no team in or out for 30 straight seasons) |
| Average QB starter OVR | Stable within ±4 |
| FA: % of top-100 FAs signed by week 1 | ≥ 90% |
| Contracts: top APY by position as % of cap | Inside per-position bands (e.g., top QB 15–20%) |
| Draft: R1 picks starting by year 3 | 55–80% |

Failures write a detailed per-season CSV so drift can be diagnosed.

---

## 6. Live event tests
All real-time behavior is tested with `FakeTimeProvider`. See the scenario table in [LIVE_EVENTS.md §9](LIVE_EVENTS.md#9-testing).

---

## 7. Build script now, CI later

Source hosting and CI are a deliberate later task, **[F-30](../FEATURES.md#f-30-source-hosting--ci)** ([D-030](../DECISIONS.md#d-030-source-hosting-and-ci-deferred-to-f-30)). Until then, a single script at the repo root runs the same gates locally, and when CI arrives it just calls this script.

```powershell
# build.ps1 (exists since M0 WP1)
./build.ps1          # restore → build (warnings as errors) → fast tests (not Category=Slow), including the transaction torture tests
./build.ps1 -Full    # + slow tests, Release ReadyToRun publish; later SimLab calibration (M2), E2E (M3), 30-season autoplay (M5) → ./artifacts/
```

**Test runner:** xUnit v3 on **Microsoft.Testing.Platform** (`global.json` → `"test": { "runner": "Microsoft.Testing.Platform" }`). The .NET 10 SDK no longer supports xUnit v3 through the VSTest bridge. Mark slow tests with `[Trait("Category", "Slow")]`. Filter with xUnit's options (`--filter-not-trait "Category=Slow"`, `--filter-class`, `--filter-method`), not VSTest `--filter` expressions. A project with zero tests exits with code 8, which `build.ps1` accepts.

**Habit until F-30:** run `./build.ps1` before every commit, and `./build.ps1 -Full` before closing a milestone, plus at least weekly once the autoplay gate exists (M5).

### 7.1 Target CI pipeline (built in F-30)

```yaml
# Sketch — .github/workflows/ci.yml
on: [pull_request, push]
jobs:
  build-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { global-json-file: global.json }
      - run: ./build.ps1            # same gates as local
# nightly.yml: schedule → ./build.ps1 -Full; uploads ./artifacts (calibration, autoplay, benchmark reports)
```

- The WPF host builds only on Windows runners, with no workloads needed. Test projects don't reference `GridironFrontOffice.Desktop`.
- Calibration and autoplay reports are uploaded as artifacts, which gives you a free history of how the sim evolves.

---

## 8. Code conventions

### 8.1 Build settings (`Directory.Build.props`)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```
(`GridironFrontOffice.Desktop` overrides with `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`, `<UseWPF>true</UseWPF>` and the Razor SDK. **The Windows SDK TFM is required:** with plain `net10.0-windows`, the .NET 10 WPF `BlazorWebView` crashes at startup ([HOST_BENCHMARK.md](HOST_BENCHMARK.md#gotchas-found-while-measuring-worth-knowing-for-m0)). A future macOS host would be a separate project.)

### 8.2 C# style
- File-scoped namespaces, `sealed` by default, `record`/`record struct` for DTOs and value objects, primary constructors for services.
- `Async` suffix. `CancellationToken` is the last parameter and is passed through.
- No `#region`. Prefer small files: one public type per file.
- **Banned APIs** (Microsoft.CodeAnalysis.BannedApiAnalyzers, three tiers in `build/BannedSymbols.*.txt` wired by `Directory.Build.props`: `Transactions` everywhere, `Pure` for Core/Simulation/Application, `RawSqlite` everywhere except Infrastructure) in Core, Simulation and Application: `System.Random`, `DateTime.Now`/`UtcNow` (use `TimeProvider` / the league date), `Console.*`, `Thread.Sleep`, `Task.Result`/`Wait()`.
- **Banned everywhere except `SaveWriter`:** `DbConnection.BeginTransaction*`, `DatabaseFacade.BeginTransaction*`/`UseTransaction*` (if EF Core), `System.Transactions.TransactionScope`. See [DATA_ACCESS_GUIDE.md §8](DATA_ACCESS_GUIDE.md#8-how-the-rules-are-enforced-so-you-cant-do-it-wrong-by-accident).
- **No `AddDbContext`/`AddDbContextFactory`** (architecture test), and **no `async void`** (VSTHRD100 as an error).
- Domain errors are `Result<T>` with typed error codes. Exceptions are reserved for bugs and I/O.
- Money is `long` dollars with helper formatting (`Money.Format(12_500_000) → "$12.5M"`).

### 8.3 Blazor style
- Components get `[Parameter]`s only. Data loading happens in screen stores, not deep components.
- Use `@key` on list items. Never inject `DbContext` into a component (it's architecturally impossible anyway, since UI has no EF reference).
- CSS isolation per component, tokens only, no raw hex.

### 8.4 Git workflow (D-066, D-076)
Decided in [D-066](../DECISIONS.md#d-066-git-workflow-gfo-commit-format-one-branch-per-milestone), with branching replaced by [D-076](../DECISIONS.md#d-076-work-happens-on-main-no-milestone-branches).

- **Commit messages:** `(type) *module* : summary`, for example `(feat) *m0* : scaffold solution, projects and guardrails` or `(doc) *docs* : resolve open questions`. Types (the commit-review skill's list): `feat`, `bugfix`, `fix`, `refactor`, `perf`, `test`, `doc`, `style`, `chore`, `revert`. Branch prefixes reuse the same types.
- **Work on `main`** (D-076): no branches and no merge step. A commit per work package, or per meaningful slice of a large one.
- Run `./build.ps1` before every commit.
- Tag milestones (`m0-complete`, …) as save-compatible checkpoints.

### 8.5 Definition of Done (per feature)
- [ ] The rules are covered by unit tests (Core/Application)
- [ ] The AI counterpart exists and passes autoplay (if the feature has one)
- [ ] The UI uses design system components only, with empty, loading and error states
- [ ] Keyboard accessible, with focus handled correctly and a visible focus ring
- [ ] Accessible in both themes: text contrast meets WCAG AA, color is never the only signal, reduced motion is respected, custom components carry ARIA labels ([VISUAL_DESIGN.md §10](../VISUAL_DESIGN.md) QA checklist). Screen readers aren't supported in the desktop host for 1.0 ([D-071](../DECISIONS.md#d-071-screen-readers-arent-supported-in-10)), but the labels stay: tests find elements by them, and they're ready if a later host can expose them
- [ ] Logging at the right levels for lifecycle events
- [ ] Performance within budget (timed log or benchmark)
- [ ] Docs updated (feature spec, plus DECISIONS if anything was decided)
- [ ] A migration added if the schema changed, plus the fixture save updated at release time
