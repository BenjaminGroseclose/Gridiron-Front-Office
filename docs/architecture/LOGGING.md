# Logging & Diagnostics

> **Status:** Implemented for M0 (WP5, 2026-10-16). The sim trace (§9) and in-app diagnostics (§10) arrive later · Related: [D-007](../DECISIONS.md#d-007-logging-serilog-to-rolling-log-files)

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
| Every other call | `[LoggerMessage]` too. The analyzers require it (CA1848), and a computed argument goes into a local first (CA1873). Shared host messages are in `Infrastructure/Logging/HostLog.cs`; UI messages are in `UI/Diagnostics/UiLog.cs`. |
| Line format | `GfoLogFormatter` (Infrastructure, §3) |

---

## 3. Files

| Log | Path | Rolling | Retention |
|---|---|---|---|
| **Main log** | `%LOCALAPPDATA%\GridironFrontOffice\logs\gfo-YYYYMMDD.log` | Daily, plus a new file at 10 MB (`_001`, `_002`, …) | 14 files |
| **Sim trace** (opt-in) | `...\logs\sim-trace-YYYYMMDD.log` | Daily, plus a new file at 50 MB | 3 files |
| **Live event log** | Part of the main log (`SourceContext = GridironFrontOffice.Application.LiveEvents.*`) | — | — |

**Line format** (`GfoLogFormatter`, a small custom formatter, because Serilog's `{Properties}` token always prints, even `{}`):
```
2026-10-16 09:18:26.191 -04:00 [FTL] [GridironFrontOffice.Desktop.App] Unhandled exception on the UI thread
    {"SaveId":7,"Phase":"Draft"}          ← only when the event carries properties its message didn't use
System.InvalidOperationException: …       ← only when there's an exception, with its full stack trace
```
- The first line is `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}`.
- **`AppVersion` and `SessionId` enrich every event but are printed once, in the startup banner** (§7). That keeps lines short and the log well under the 1 MB/hour rule; any session is easy to find because each one starts with a banner. `EventId` isn't repeated either.
- **Every line passes through `PathTokens`**, so `%LOCALAPPDATA%`, `%APPDATA%`, `%TEMP%` and `%USERPROFILE%` replace the real folders, even inside exception messages. The Windows user name never reaches the file (§11).

---

## 4. Bootstrap configuration

**As built (M0 WP5).** Both composition roots run the same sequence. The shared parts live in `GridironFrontOffice.Infrastructure/Logging/`:
```csharp
// GridironFrontOffice.Desktop / App.xaml.cs → OnStartup. The DevHost's Program.cs does the same with AppPaths.ForDevelopment().
var paths = AppPaths.ForCurrentUser().EnsureCreated();     // the DevHost: %LOCALAPPDATA%\GridironFrontOffice-Dev
_settings = SettingsStore.Load(paths);                      // before DI, so the level comes from settings.json
var app = AppIdentity.From(typeof(App).Assembly, Configuration);   // "0.0.1+ce3e6f6" from the informational version

Log.Logger = GfoLogging.CreateLogger(paths, _settings.Current, app, debugOutput: Debugger.IsAttached);
_log = new SerilogLoggerFactory(Log.Logger).CreateLogger("GridironFrontOffice.Desktop.App");
HostLog.Banner(_log, StartupBanner.Build(app, HostEnvironment.Describe(), paths, _settings.Current));
if (_settings.Created) HostLog.SettingsCreated(_log, paths.SettingsFile);              // first launch
if (_settings.LoadWarning is { } problem) HostLog.SettingsUnreadable(_log, problem);   // logged once the logger exists

GlobalExceptionHooks.Install(_log, _crash.ShowAndExit);     // AppDomain + TaskScheduler (§8)
DispatcherUnhandledException += OnDispatcherUnhandledException;   // the WPF UI thread (§8)

services.AddLogging(logging => logging.ClearProviders().AddSerilog(Log.Logger));
// The DevHost keeps ASP.NET's console logger (dev tooling reads it) and adds Serilog for the file.
```
**The level comes from `settings.json` alone.** `AddSerilog` adds a filter rule just for Serilog, which outranks .NET's default Information minimum and any `"Logging"` levels in the DevHost's `appsettings.json`. Serilog then applies `developer.logLevel`. A test guards this (`LogLevelSettingTests`), because the opposite would mean Debug silently never reached the file.
`GfoLogging.CreateLogger` holds the configuration. The sketch it was built from follows; the sim trace sink isn't configured until the engine exists (M2).

```csharp
// The original sketch (GridironFrontOffice.Desktop / App.xaml.cs → OnStartup)
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

On shutdown: `HostLog.Exiting` (exit code and session length), then `Log.CloseAndFlush()` (from WPF `Application.Exit` and from `AppDomain.ProcessExit`; the DevHost flushes in a `finally`). The crash paths flush before the process ends (§8).

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
| `AppVersion`, `SessionId` (GUID per app run) | Enricher. In the text log they're printed once, in the banner (§3) | Process |
| `SaveId`, `LeagueName` | `LogContext.PushProperty` when a league session opens | League session |
| `Season`, `Phase`, `Week` | Pushed by the phase machine around each advance | Operation |
| `Operation`, `OperationId` | `ISaveWriter.ExecuteAsync` scope | Command |
| `ThreadId` | Only at Debug level | — |

---

## 7. Startup banner

Logged once at Information level on every launch:

```
==== Gridiron Front Office 0.0.1+ce3e6f6 (Debug, Desktop) ====
OS: Windows 11 (10.0.26200) x64 | .NET 10.0.13 | WebView2 155.0.4283.45
Culture: en-US | Screen: 2560x1440 @ 100%, 3 monitors | Process: 64-bit
Paths: data=%LOCALAPPDATA%\GridironFrontOffice
Settings: theme=Dark accent=Team density=Comfortable logLevel=Information logSql=off simTrace=off
Session: 97370dc7-fb0d-47fd-81a9-a6944623c0f4
```
(A real banner from M0. The host supplies its line through `HostDescription`: the DevHost reports `WebView2 n/a` and `Screen: browser`.) Windows 11 still reports version 10.0, so the build number (≥ 22000) names it.

User-specific path segments are written with environment tokens (`%LOCALAPPDATA%`) instead of the expanded path, so the Windows user name doesn't end up in shared logs.

---

## 8. Capturing failures everywhere

| Source | Hook | Logged as |
|---|---|---|
| .NET unhandled exceptions | `AppDomain.CurrentDomain.UnhandledException` | Fatal, then the crash dialog on its own STA thread (Desktop), or flush and exit (DevHost) |
| Unobserved task exceptions | `TaskScheduler.UnobservedTaskException` (+ `SetObserved()`) | Error |
| WPF UI thread | `Application.DispatcherUnhandledException`. **Always fatal:** log, set `Handled`, show the crash dialog, exit. Every command is its own transaction, so nothing is lost by not running on in an unknown state. | Fatal |
| Blazor component exceptions | `PageErrorBoundary` (`UI/Shell`) wraps `@Body` in the layout, so every routed page has one. `OnErrorAsync` logs it; the page shows a recovery panel (**Try again**, **Home**); navigating anywhere clears it. | Error (event 8000) |
| Blazor framework logs | Flow through `ILogger` automatically (`Microsoft.AspNetCore.Components.*`) | As emitted, Warning+ in Release |
| **JavaScript in the WebView** | `UI/wwwroot/js/gfo-diagnostics.js`, a classic script each host page loads **before Blazor**, so boot-time errors count too. It hooks `error` and `unhandledrejection` and holds errors until the `ClientDiagnostics` component (in the layout) attaches, then calls its `[JSInvokable] LogClientError` through a `DotNetObjectReference`. **50 per session**, then one Warning (8002). | Error with `SourceContext = GridironFrontOffice.UI.Js` (8001) |
| EF Core | `Microsoft.EntityFrameworkCore.*` categories | Warning+ (SQL text only when the dev setting is on) |
| Commands | `ISaveWriter` logs the operation name, elapsed ms, and failure | Information / Error |
| Slow operations | `using var _ = logger.TimeOperation("SimWeek", budgetMs: 3000)` → Warning if over budget | Information / Warning |

**Crash dialog** (shown when possible): a small *native WPF* window (`Desktop/Diagnostics/CrashWindow`), because Blazor itself may be what broke.
- **Text:** "Something went wrong. Your league is safe up to your last action." Below it: the exception type and message.
- **Buttons in M0:** **Open logs folder**, **Close**, **Restart**. **Copy diagnostics** arrives in M11 (M0_FOUNDATIONS.md §11, question 4).
- **Order of events:** the exception is logged first. The choice is logged too (event 1013), then the log is flushed and the process exits with code 1. It exits itself, so Windows' own crash report doesn't pile on top.
- **Restart** launches the app again with the same arguments, minus any Debug `--diag-test`. WP9's single-instance guard has to let the new process wait for the old one to exit.

### 8.1 Proving it (Debug builds)
Every hook has a trigger, so the paths are tested end to end rather than assumed:
- **The Home page's diagnostics test panel** (`UI/Diagnostics/DiagnosticsTestPanel`, Debug builds only) has four buttons:
  - **Page error:** the page boundary.
  - **JavaScript error:** the JS bridge.
  - **Unobserved task:** a dropped faulted task, collected on purpose.
  - **Crash the app:** an exception on a background thread. The DevHost logs Fatal and exits; the desktop host shows the crash dialog.
- **Desktop only:**
  - `--diag-test=page|js|task|crash` presses the panel button through WebView2 script, so the paths can be tested without clicking inside the WebView.
  - `--diag-test=ui` (or **Ctrl+Shift+F12**) throws on the WPF UI thread.

M0 checked every path in both hosts. Each one landed in the log with its stack trace, and no log line contained the Windows user name.

### 8.2 Event IDs in use
- **1000–1013, App/Host (`HostLog`):** banner, settings unreadable, settings file created (1006), window shown, WebView2 ready, host page loaded, exiting, the unhandled background-thread, unobserved-task and UI-thread exceptions, and the crash dialog choice.
- **2000–2022, Persistence (`DataLog`):**
  - commands: committed (Debug), rejected, rolled back, slow (> 1 s), rollback failed;
  - saves: created, opened, upgraded, closed, refused (2014), restored (2017), deleted (2018), a restore or delete refused (2021, 2022);
  - snapshots: taken, with its time and size (2015), and pruned (2016);
  - the save list can't read a file (2019, Warning);
  - a failing domain-event subscriber (2020).
- **8000–8003, UI (`UiLog`):** page error, JavaScript error, JavaScript errors suppressed, and the diagnostics script missing.

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

**Never log:** full file paths containing the user name (the formatter tokenizes them anyway, as a safety net), entire entity graphs, or anything at Information level inside per-play loops.
