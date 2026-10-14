# Host Benchmark: MAUI Blazor Hybrid vs. WPF + BlazorWebView

> **Status:** Measured · 2026-10-13 · Supports [D-021](../DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed)
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
| **Warm startup to first Blazor render** | **MAUI** | ~0.2 s faster (0.79 s vs. ~1.0 s) |
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
- Scripts and raw output were in the session scratchpad (not committed). Rerun in M0 on the real `GridironFrontOffice.Desktop` if you want numbers for the actual app.

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

## Non-performance benefits (unmeasured but real)

| Benefit | Why it matters for a solo developer |
|---|---|
| **No MAUI workload** | A fresh machine or CI runner needs only the .NET SDK. No `dotnet workload restore`, no workload version drift. |
| **No Windows App SDK / WinUI layer** | One less runtime to bundle, version and debug (see gotcha 2) |
| **A mature, stable host** | WPF changes slowly. `BlazorWebView` for WPF ships with each .NET release. |
| **A faster inner loop all day** | About 1.4 s saved per edit-build cycle × hundreds of cycles per week |
| **Simpler publish** | A plain self-contained `dotnet publish` plus Velopack. No MSIX or WinAppSDK self-contained flags. |

**What we give up:** a built-in macOS path. If macOS ever matters, add a second thin host (MAUI or Photino). The UI lives in RCLs, so that stays about a day of work ([Q-06](../OPEN_QUESTIONS.md#q-06-macos-ever)).
