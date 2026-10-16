# Third-Party Notices

> The license inventory required by [D-011](docs/DECISIONS.md#d-011-release-posture-personal-first-steam-ready): every package, font, icon set and asset GFO uses, with its license, **checked when it's added**. Commercial-license packages (MediatR, AutoMapper, FluentAssertions v8+, …) are never added.
>
> **How licenses were checked:** the `<license>` expression in each restored package's `.nuspec`, or the source repository's LICENSE file where the package declares none. Versions match `Directory.Packages.props`.
>
> Last updated: 2026-10-15 (M0 WP1, project scaffolding).

## Shipped with the app

| Component | Version | License | Used by | Notes |
|---|---|---|---|---|
| Microsoft.AspNetCore.Components.WebView.Wpf | 10.0.110 | MIT | Desktop | WPF host for Blazor (D-021) |
| Microsoft.AspNetCore.Components.Web | 10.0.13 | MIT | UI, UI.Components | |
| Microsoft.FluentUI.AspNetCore.Components | 5.0.1 | MIT | UI, UI.Components | Fluent UI Blazor v5 (D-025) |
| Microsoft.FluentUI.AspNetCore.Components.Icons | 5.0.1 | MIT | UI.Components | Fluent System Icons (proposed in D-031, confirmed by Spike S0b) |
| Microsoft.EntityFrameworkCore | 10.0.13 | MIT | Application, Infrastructure | D-024 |
| Microsoft.EntityFrameworkCore.Sqlite (+ Microsoft.Data.Sqlite.Core) | 10.0.13 | MIT | Infrastructure | |
| SQLitePCLRaw.bundle_e_sqlite3 (transitive) | 3.0.2 | Apache-2.0 | Infrastructure | Bundles the native SQLite library (SQLite itself is public domain) |
| Serilog | 4.4.0 | Apache-2.0 | Infrastructure | D-007 |
| Serilog.Extensions.Logging | 10.0.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.Async | 2.1.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.Debug | 3.0.0 | Apache-2.0 | Infrastructure | Writes only in Debug builds |

## Build and test only (never shipped)

| Component | Version | License | Used by | Notes |
|---|---|---|---|---|
| Microsoft.EntityFrameworkCore.Design | 10.0.13 | MIT | Infrastructure (design time) | `dotnet ef` migrations. `PrivateAssets=all`. |
| dotnet-ef (local tool) | 10.0.13 | MIT | Developer machine | Pinned in `.config/dotnet-tools.json` |
| Microsoft.CodeAnalysis.BannedApiAnalyzers | 5.6.0 | MIT | All projects | Banned-API rules (RS0030) |
| Microsoft.VisualStudio.Threading.Analyzers | 18.12.1 | MIT | All projects | `async void` and blocking-wait rules |
| xunit.v3 | 4.0.2 | Apache-2.0 | Tests | Runs on Microsoft.Testing.Platform |
| Shouldly | 4.3.0 | BSD-3-Clause | Tests | |
| bunit | 2.11.3 | MIT | UI tests | |
| NetArchTest.Rules | 1.3.2 | MIT | Architecture tests | The package declares no license. MIT per its [repository LICENSE](https://github.com/BenMorris/NetArchTest/blob/master/LICENSE). |
| Mono.Cecil (transitive) | 0.11.3 | MIT | Architecture tests | Via NetArchTest |

## Fonts, icons and other assets

| Asset | Version | License | Location | Notes |
|---|---|---|---|---|
| **Archivo** (variable: width 62–125%, weight 100–900), Latin subset | Fontsource 5.3.0 | SIL OFL 1.1 | `src/UI.Components/wwwroot/fonts/archivo-latin-wdth-normal.woff2` | UI and display face ([VISUAL_DESIGN.md §4](docs/VISUAL_DESIGN.md#4-typography)). © The Archivo Project Authors (Omnibus-Type). License text alongside: `Archivo-OFL.txt`. |
| **JetBrains Mono** (variable: weight), Latin subset | Fontsource 5.3.0 | SIL OFL 1.1 | `src/UI.Components/wwwroot/fonts/jetbrains-mono-latin-wght-normal.woff2` | Debug overlay and log viewer only. © The JetBrains Mono Project Authors. License text alongside: `JetBrainsMono-OFL.txt`. |
| Fluent System Icons (via Microsoft.FluentUI.AspNetCore.Components.Icons) | 5.0.1 | MIT | NuGet package | Proposed in D-031, confirmed in Spike S0b |

Fonts were downloaded 2026-10-15 from `cdn.jsdelivr.net/npm/@fontsource-variable/…@5.3.0`, then checked (WOFF2 signature, exact byte size, license text) before they were added. The OFL requires its license text to travel with the fonts, which is why the `.txt` files sit next to them.

## Declared but not yet referenced

`Verify.XunitV3` 33.3.3 and `Microsoft.Data.Sqlite` 10.0.13 have versions pinned in `Directory.Packages.props` for later work packages. Their licenses get checked and recorded here when a project first references them.
