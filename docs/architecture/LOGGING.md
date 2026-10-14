# Logging & Diagnostics

> **Status:** Proposed · 2026-10-13 · Related: [D-007](../DECISIONS.md#d-007-logging-serilog-to-rolling-log-files)

---

## 1. Goals

1. **Always-on file logging** to `.log` files that a non-technical user can find and send.
2. **Diagnose any crash** from the log alone: what was happening, in which league and phase, on which build.
3. **Near-zero overhead** on the UI thread and in the simulation hot loop.
4. **Opt-in deep tracing** for sim calibration and AI debugging, without bloating normal logs.
5. **Local only.** Logs never leave the machine unless the user sends them.

---

## 2. Stack

| Piece | Choice |
|---|---|
| API used in code | `Microsoft.Extensions.Logging.ILogger<T>` everywhere. **Serilog types appear only in the composition root.** |
| Provider | `Serilog.Extensions.Logging` (`builder.Logging.AddSerilog(dispose: true)`) |
| Sinks | `Serilog.Sinks.File`, wrapped in `Serilog.Sinks.Async`. `Serilog.Sinks.Debug` in Debug builds only. |
| Hot-path logging | `[LoggerMessage]` source-generated methods (no boxing, no template parsing at runtime) |

---

## 3. Files

| Log | Path | Rolling | Retention |
|---|---|---|---|
| **Main log** | `%LOCALAPPDATA%\GridironFrontOffice\logs\gfo-YYYYMMDD.log` | Daily, plus a new file at 10 MB (`_001`, `_002`, …) | 14 files |
| **Sim trace** (opt-in) | `...\logs\sim-trace-YYYYMMDD.log` | Daily, plus a new file at 50 MB | 3 files |
| **Live event log** | Part of the main log (`SourceContext = GridironFrontOffice.Application.LiveEvents.*`) | — | — |

**Output template**
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}
```
`{Properties:j}` prints scope and enricher properties (SessionId, SaveId, Season, Phase) as JSON on the second line. Only non-empty property sets are printed, via a custom formatter if needed.

---

## 4. Bootstrap configuration (sketch)

```csharp
// GridironFrontOffice.Desktop / App.xaml.cs → OnStartup (composition root, WPF host)
var paths = AppPaths.ForCurrentUser();          // %LOCALAPPDATA%\GridironFrontOffice
var settings = SettingsStore.Load(paths);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(settings.Developer.LogLevel ?? LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
        settings.Developer.LogSql ? LogEventLevel.Information : LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With(new AppContextEnricher(appVersion, sessionId))   // AppVersion, SessionId
    .WriteTo.Async(a => a.File(
        path: Path.Combine(paths.Logs, "gfo-.log"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 14,
        shared: false,
        outputTemplate: Template))
    .WriteTo.Logger(trace => trace
        .Filter.ByIncludingOnly(Matching.FromSource("GridironFrontOffice.Simulation.Trace"))
        .MinimumLevel.Verbose()
        .WriteTo.Async(a => a.File(Path.Combine(paths.Logs, "sim-trace-.log"), /* … */)),
        restrictedToMinimumLevel: settings.Developer.SimTrace ? LogEventLevel.Verbose : LogEventLevel.Fatal + 1)
#if DEBUG
    .WriteTo.Debug()
#endif
    .CreateLogger();

var services = new ServiceCollection();
services.AddLogging(logging => logging.ClearProviders().AddSerilog(Log.Logger, dispose: true));
services.AddWpfBlazorWebView();
// services.AddGfoInfrastructure(paths).AddGfoApplication().AddGfoUi();
```

> The `Fatal + 1` trick is shorthand for "sink disabled". In practice, use a `LoggingLevelSwitch` so the Developer settings screen can toggle the sim trace **at runtime** without restarting.

On shutdown: `Log.CloseAndFlush()` (from WPF `Application.Exit` and from `AppDomain.ProcessExit`).

---

## 5. Level policy

| Level | Use for | Examples |
|---|---|---|
| **Verbose** | Sim internals (trace log only) | Per-play resolution inputs and outputs, AI candidate scores |
| **Debug** | Developer detail (Debug builds or opt-in) | Query timings, cache hits, AI decision summaries |
| **Information** | Lifecycle and user-visible milestones | App start, save opened or closed, phase advanced, week simmed (with duration), **every draft pick and trade with key inputs**, snapshot taken |
| **Warning** | Unexpected but recovered | Slow operation over budget, snapshot pruned for space, AI produced an invalid intent (rejected), import validation warning |
| **Error** | A failed operation, with the app still running | Command threw, file I/O failure, migration failure (rolled back) |
| **Fatal** | Crash | Unhandled exception, the process is going down |

**Rule:** a healthy 1-hour session at Information level produces **under 1 MB** of log.

---

## 6. Context enrichment

| Property | Source | Lifetime |
|---|---|---|
| `AppVersion`, `SessionId` (GUID per app run) | Enricher | Process |
| `SaveId`, `LeagueName` | `LogContext.PushProperty` when a league session opens | League session |
| `Season`, `Phase`, `Week` | Pushed by the phase machine around each advance | Operation |
| `Operation`, `OperationId` | `ISaveWriter.ExecuteAsync` scope | Command |
| `ThreadId` | Only at Debug level | — |

---

## 7. Startup banner

Logged once at Information level on every launch:

```
==== Gridiron Front Office 0.3.0+a1b2c3d (Release) ====
OS: Windows 11 Pro 10.0.26200 x64 | .NET 10.0.x | WebView2 1xx.0.xxxx.xx
Culture: en-US | Screen: 2560x1440 @ 125% | Process: 64-bit
Paths: data=%LOCALAPPDATA%\GridironFrontOffice
Settings: theme=dark density=compact logLevel=Information simTrace=off
```

User-specific path segments are written with environment tokens (`%LOCALAPPDATA%`) instead of the expanded path, so the Windows user name doesn't end up in shared logs.

---

## 8. Capturing failures everywhere

| Source | Hook | Logged as |
|---|---|---|
| .NET unhandled exceptions | `AppDomain.CurrentDomain.UnhandledException` | Fatal, then the crash dialog |
| Unobserved task exceptions | `TaskScheduler.UnobservedTaskException` (+ `SetObserved()`) | Error |
| WPF UI thread | `Application.DispatcherUnhandledException` (log, show the crash dialog, decide whether to set `Handled`) | Fatal or Error |
| Blazor component exceptions | `ErrorBoundary` on each page, with a custom `OnError` that logs | Error (the page shows a recovery panel) |
| Blazor framework logs | Flow through `ILogger` automatically (`Microsoft.AspNetCore.Components.*`) | As emitted, Warning+ in Release |
| **JavaScript in the WebView** | A small `gfo-diagnostics.js`: `window.onerror` and `unhandledrejection` → `DotNet.invokeMethodAsync('GridironFrontOffice.UI', 'LogClientError', …)` | Error with `SourceContext = GridironFrontOffice.UI.Js` |
| EF Core | `Microsoft.EntityFrameworkCore.*` categories | Warning+ (SQL text only when the dev setting is on) |
| Commands | `ISaveWriter` logs the operation name, elapsed ms, and failure | Information / Error |
| Slow operations | `using var _ = logger.TimeOperation("SimWeek", budgetMs: 3000)` → Warning if over budget | Information / Warning |

**Crash dialog** (shown when possible): "Something went wrong. Your league is safe up to your last action." Buttons: **Copy diagnostics**, **Open logs folder**, **Restart**.

---

## 9. Sim trace logging

- **Category:** `GridironFrontOffice.Simulation.Trace`. **Off by default.**
- Enabled from **Settings → Developer → Simulation trace** (runtime toggle), or by SimLab with `--trace`.
- Content: per play, the situation, the play call and why, matchup inputs, sampled values, outcome and deciding matchup.
- Hot-path rule: every trace call sits behind `if (_trace.IsEnabled(LogLevel.Trace))` or uses `[LoggerMessage]`. A disabled trace must cost under 1% of engine time (benchmarked).

---

## 10. In-app diagnostics

- **Settings → Diagnostics**
  - *Open logs folder*
  - *Copy diagnostics*: builds a `.zip` in `diagnostics\` containing the last 3 main logs, `settings.json` (sanitized) and the current save's `SaveInfo` row (**not** the save itself), then shows it in Explorer.
  - *Log level* (Information / Debug) and *Simulation trace* toggles.
- **Developer overlay** (Debug builds, `Ctrl+Shift+D`): a live tail of recent log events in a drawer, plus the last 20 command timings.

---

## 11. Conventions for code

```csharp
// Message templates, never interpolation:
_logger.LogInformation("Draft pick {Overall} by {TeamAbbr}: {PlayerId} {Position} (auto={WasAuto}, clockLeftMs={ClockLeftMs})",
    pick.Overall, team.Abbr, player.Id, player.Position, wasAuto, clockLeftMs);

// Hot paths: source-generated
internal static partial class SimLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Trace,
        Message = "Play {PlayNo}: {Call} {Down}&{Distance} at {YardLine} -> {Outcome} ({Yards} yds) decided by {Matchup}")]
    public static partial void PlayResolved(ILogger logger, int playNo, string call, int down,
        int distance, int yardLine, string outcome, int yards, string matchup);
}
```

**Event ID ranges:** 1000–1999 App/Host · 2000–2999 Persistence · 3000–3999 Simulation · 4000–4999 Calendar/Season · 5000–5999 Live Events/Draft · 6000–6999 AI · 7000–7999 Contracts/FA/Trades · 8000–8999 UI.

**Never log:** full file paths containing the user name (use tokens), entire entity graphs, or anything at Information level inside per-play loops.
