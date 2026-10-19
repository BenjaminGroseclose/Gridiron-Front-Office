# Host Benchmark: MAUI Blazor Hybrid vs. WPF + BlazorWebView

> **Status:** Measured · 2026-10-13, real shell re-measured 2026-10-16 (Spike S0c, [§ below](#real-shell-spike-s0c-2026-10-16)) · Supports [D-021](../DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed), [D-068](../DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar)
>
> Owner: *"Can you estimate the performance improvement / benefits with this decision?"* Rather than estimate, we measured.

---

## TL;DR

| Area | Winner | Size of the difference |
|---|---|---|
| **Dev loop (build after editing a `.razor` file)** | **WPF** | **2.3× faster** (1.1 s vs. 2.4 s) |
| **Clean build** | **WPF** | **4.7× faster** (2.2 s vs. 10.4 s) |
| **Release publish** | **WPF** | **2× faster** like-for-like (18 s vs. 37 s), 12× without ReadyToRun (3 s) |
| **Shipped size** | **WPF** | **14–28% smaller** (200 / 168 MB vs. 233 MB), 30% fewer files (444 vs. 630) |
| **Tooling** | **WPF** | No MAUI workload, no Windows App SDK layer |
| **Warm startup to first Blazor render** | **MAUI** | ~0.2 s faster (0.79 s vs. ~1.0 s). The real GFO shell: ~1.5 s to a themed screen ([below](#real-shell-spike-s0c-2026-10-16)). |
| **Memory** | Tie | ~480 MB vs. ~505 MB total working set (WPF ~5% lower). Private bytes are about equal. |
| **UI rendering at runtime** | Tie | Same WebView2 and the same Blazor renderer, so they're identical by construction |

**Bottom line:** the switch to WPF is about **developer productivity, packaging and simplicity**, not runtime speed. At runtime the two are effectively equal. MAUI reaches first paint about 0.2 s sooner on a warm start, which is imperceptible next to opening a save file.

---

## Method

- **Machine:** AMD Ryzen 7 5800X (8 cores), 64 GB RAM, NVMe SSD, Windows 11 Pro (build 26200), .NET SDK 10.0.100, MAUI workload 10.0.1.
- **Apps:** the stock `maui-blazor` template (restricted to the **Windows target only**, which is MAUI's best case; unpackaged; Windows App SDK self-contained) vs. a WPF app hosting `BlazorWebView` (`Microsoft.AspNetCore.Components.WebView.Wpf` 10.0.1) with **the same Razor components, CSS and `index.html`** copied from the MAUI template.
- **Builds:** Debug, 3 runs each, alternating apps. *Clean* = delete `bin`/`obj` and build (restore from a warm NuGet cache). *No-op* = build again with no changes. *Razor edit* = append a comment to `Home.razor`, then build.
- **Publish:** `dotnet publish -c Release -r win-x64 --self-contained`.
- **Startup:** published Release apps, launched 6 times each. A probe in `MainLayout.OnAfterRenderAsync(firstRender)` records the **time from process start to the first completed Blazor render**, then the memory after a 2 s settle (app process plus the WebView2 child processes started by that run).
- Scripts and raw output were in the session scratchpad (not committed). The real `GridironFrontOffice.Desktop` was re-measured in M0 (Spike S0c, below).

---

## Results

### Build and publish

| Metric (mean of 3) | MAUI | WPF | WPF advantage |
|---|---|---|---|
| Clean build (Debug) | 10.39 s | 2.21 s | **4.7×** |
| No-op build | 2.14 s | 0.91 s | **2.4×** |
| Build after a `.razor` edit | 2.44 s | 1.08 s | **2.3×** |
| Release publish, self-contained | 36.6 s (ReadyToRun **on** by default) | 3.0 s (R2R off) · 18.4 s (R2R on) | **2.0×** like-for-like |
| Publish size / files | 233.3 MB / 630 | 167.7 MB / 444 (R2R off) · 199.8 MB / 444 (R2R on) | **−14% to −28%**, −30% files |

### Startup and memory (Release, warm runs)

| Metric | MAUI (R2R) | WPF (no R2R) | WPF (R2R) |
|---|---|---|---|
| Process start → first Blazor render | **785 ms** (768–793) | 1,074 ms (1,065–1,087) | 1,003 ms (994–1,018) |
| First-ever launch (creates the WebView2 profile, single sample) | 1,988 ms | 1,529 ms | 1,597 ms |
| App process working set | ~179 MB | ~156 MB | ~152 MB |
| App process private bytes | ~136 MB | ~145 MB | ~150 MB |
| WebView2 processes working set | ~325 MB | ~325 MB | ~325 MB |
| **Total working set** | **~504 MB** | **~481 MB** | **~477 MB** |

**Reading the startup numbers:** MAUI's Release publish enables **ReadyToRun** by default (we checked `PublishReadyToRun=true`), and WPF's doesn't. Turning R2R on for WPF closes part of the gap (1,074 → 1,003 ms). The remaining ~0.2 s is probably the WPF WebView2 *composition* control initialization (see the gotcha below), but that's unverified. **Action for M0:** ship `GridironFrontOffice.Desktop` with `<PublishReadyToRun>true</PublishReadyToRun>`, and re-measure on the real app.

---

## Gotchas found while measuring (worth knowing for M0)

1. **WPF `BlazorWebView` on .NET 10 needs a Windows SDK target framework.** With `net10.0-windows`, the app **crashed on startup**: `FileNotFoundException: Microsoft.Windows.SDK.NET` from `WebView2CompositionControl.TryInitializeD3DImage`. The .NET 10 WPF `BlazorWebView` uses WebView2's composition control, which needs the Windows SDK projection. **Fix:** `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`. This is already reflected in [TESTING_AND_QUALITY.md](TESTING_AND_QUALITY.md).
2. **MAUI's Windows App SDK build breaks on long paths.** In a deep folder, the MAUI build failed with `MSB3030: Could not copy … MsixContent\*.winmd` because the paths exceeded 260 characters. Not a problem in a short repo path, but it's an example of the extra layer MAUI brings.
3. **The composition control has an upside:** WPF elements can overlay the WebView (no "airspace" problem). This helps the native title bar option (a) in [UI_DESIGN_SYSTEM.md §7](UI_DESIGN_SYSTEM.md#7-app-shell-and-navigation).

---

## Real shell: Spike S0c (2026-10-16)

The actual `GridironFrontOffice.Desktop` was measured with the S0c shell: the HTML app bar as the title bar ([D-068](../DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar)), Fluent UI v5 with the Stadium theme, and the bundled fonts.

**Setup:**
- **Machine and runtime:** the same machine; SDK 10.0.402, WebView2 runtime 155.0.4283.45.
- **Build:** `dotnet publish -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true`.
- **Probe:** marks from process start to each step (an env-var-gated spike probe). Each run ended once the theme was applied.

| Measurement | Result |
|---|---|
| Publish time / size / files | 40 s / **247 MB** / 333 files. Larger than the benchmark's 200 MB because the app also ships Fluent UI and its icons (~22 MB), EF Core, SQLite and the GFO assemblies. |
| **Warm start → themed first screen** | **median 1.51 s** over 8 runs (1.39–2.09 s; typical 1.38–1.56 s) |
| Warm start → first Blazor render | median 1.47 s (1.35–2.03 s) |
| First-ever launch (fresh WebView2 profile) | 1.52 s first render, **1.57 s** themed |
| Option (a) WPF caption strip vs. (b) app bar as title bar | No difference (themed 1.39–1.57 s over 4 runs of (a)) |

**Where a warm start goes** (typical run):

| Step | Ends at | Takes |
|---|---|---|
| .NET + WPF to `App.OnStartup` | ~140 ms | ~140 ms |
| `MainWindow.InitializeComponent` (loads the BlazorWebView and WebView2 assemblies) | ~415 ms | ~260 ms |
| WebView2 environment + browser process (starts ~490 ms) | ~1,080 ms | **~590 ms** |
| Host page DOM loaded | ~1,175 ms | ~90 ms |
| Blazor boot → first render | ~1,345 ms | ~175 ms |
| Stadium theme applied, page revealed | ~1,380 ms | ~35 ms |

The WPF window's first frame lands at about 875 ms, already in Ground, while WebView2 is still starting.

**Verdict:** well inside M0's "launch → main menu in under 3 s". WebView2's browser-process start is the largest step. `BlazorWebView` has no supported way to start it earlier, so it's left alone until a budget demands it.

**Gotchas found on the real shell** (all now in the docs):
1. **DPI:** without an app manifest, WPF is only *system* DPI aware (`awareness=1`), and Windows would bitmap-stretch the window on a monitor with a different scale. `app.manifest` declares PerMonitorV2 (`awareness=2` after).
2. **First paint:** WebView2's default background is **white**: about 220 ms of it on every start. A frame of Fluent's default theme followed, which is light on a light Windows. The fix is three layers ([UI_DESIGN_SYSTEM.md §3.3](UI_DESIGN_SYSTEM.md#33-source-of-truth-and-persistence)): native Ground backgrounds, a static boot class on the host page, and the theme service revealing the page.
3. **Script race:** a script registered with `AddScriptToExecuteOnDocumentCreatedAsync` in `BlazorWebViewInitialized` didn't run before Blazor's first navigation. Anything the first paint depends on is static in `index.html` or set natively.
4. **Persisted color scheme:** WebView2 persists `CoreWebView2Profile.PreferredColorScheme` in the profile across launches. Leave it at Auto.
5. **Maximize overhang:** a maximized `WindowChrome` window hangs 8 px past the work area at 100% scale (`WindowResizeBorderThickness` says 4). Pad the content by the measured overhang.


## M0 exit measurement (2026-10-19)

The real app after WP9's host half: the title bar, window placement and single instance, measured with the log's startup events instead of a spike probe. `HostLog.FirstThemedScreen` (event 1016) fires on the page's first theme message, when the theme service has applied and lifted the boot hide. It stays in every log, so a startup regression shows up in any session.

- **Build:** `./build.ps1 -Full` (Release, win-x64, self-contained, ReadyToRun): **248 MB**. WebView2 runtime 155.0.4283.62.
- **Runs:** 7 launches through `Start-Process`, the first right after the publish (cold files), then 6 warm.

| Measurement | Page loaded | **First themed screen** |
|---|---|---|
| First launch of the fresh publish | 1,711 ms | **1,998 ms** |
| Warm (6 runs) | 1,200–1,273 ms | **1,397–1,508 ms, median 1,463 ms** |

**Verdict:** inside the 3 s budget, and the same as S0c's 1.51 s, so WP9's host code (monitor enumeration, placement, the hit-test hook, the host script) costs nothing measurable. The screen is still the M0 Home page; re-measure when the main menu replaces it. A true cold start (after a reboot) wasn't measured.
---

## Non-performance benefits (unmeasured but real)

| Benefit | Why it matters for a solo developer |
|---|---|
| **No MAUI workload** | A fresh machine or CI runner needs only the .NET SDK. No `dotnet workload restore`, no workload version drift. |
| **No Windows App SDK / WinUI layer** | One less runtime to bundle, version and debug (see gotcha 2) |
| **A mature, stable host** | WPF changes slowly. `BlazorWebView` for WPF ships with each .NET release. |
| **A faster inner loop all day** | About 1.4 s saved per edit-build cycle × hundreds of cycles per week |
| **Simpler publish** | A plain self-contained `dotnet publish` plus Velopack. No MSIX or WinAppSDK self-contained flags. |

**What we give up:** a built-in macOS path. If macOS ever matters, add a second thin host (MAUI or Photino). The UI lives in RCLs, so that stays about a day of work ([Q-06](../OPEN_QUESTIONS.md#q-06-macos-ever)).
