# Third-Party Notices

> The license inventory required by [D-011](docs/DECISIONS.md#d-011-release-posture-personal-first-steam-ready): every package, font, icon set and asset GFO uses, with its license, **checked when it's added**. Commercial-license packages (MediatR, AutoMapper, FluentAssertions v8+, …) are never added.
>
> **How licenses were checked:** the `<license>` expression in each restored package's `.nuspec`, or the source repository's LICENSE file where the package declares none. Versions match `Directory.Packages.props`. At the M0 exit, the shipped list was also checked against every package in the Release publish (`GridironFrontOffice.Desktop.deps.json`), transitive ones included.
>
> Last updated: 2026-10-21 (M1 WP3: the name and hometown lists).

## Shipped with the app

| Component | Version | License | Used by | Notes |
|---|---|---|---|---|
| Microsoft.AspNetCore.Components.WebView.Wpf | 10.0.110 | MIT | Desktop | WPF host for Blazor (D-021) |
| Microsoft.AspNetCore.Components.Web | 10.0.13 | MIT | UI, UI.Components | |
| Microsoft.AspNetCore.Components (+ .Forms, .WebView 10.0.0, Authorization, Metadata), Microsoft.JSInterop (transitive) | 10.0.13 | MIT | UI, Desktop | Blazor itself |
| Microsoft.Extensions.* (transitive: DependencyInjection, Logging, Options, Configuration, FileProviders, Caching, Http, Validation, …) | 10.0.x | MIT | All | |
| Microsoft.Web.WebView2 (transitive) | 1.0.3179.45 | BSD-3-Clause (Microsoft's `LICENSE.txt`) | Desktop | The WebView2 SDK (Core, Wpf, WinForms DLLs and `WebView2Loader.dll`). The WebView2 *runtime* is Windows' own and isn't shipped. **Binary redistribution must reproduce the license text**, so the notices file that ships with the app (M11 packaging) includes it. |
| Microsoft.Windows.SDK.NET projection (+ WinRT.Runtime) | 10.0.19041.57 | Windows SDK license (files on its REDIST.TXT list may be distributed); WinRT.Runtime is C#/WinRT, MIT | Desktop | Comes with the Windows SDK target framework the host requires (HOST_BENCHMARK.md, gotcha 1); every .NET app on that framework ships it. Confirm both files against REDIST.TXT before the Steam release (M11). |
| Microsoft.FluentUI.AspNetCore.Components | 5.0.1 | MIT | UI, UI.Components | Fluent UI Blazor v5 (D-025) |
| Microsoft.FluentUI.AspNetCore.Components.Icons | 5.0.1 | MIT | UI.Components | Fluent System Icons (proposed in D-031, confirmed by Spike S0b) |
| Microsoft.EntityFrameworkCore | 10.0.13 | MIT | Application, Infrastructure | D-024 |
| Microsoft.EntityFrameworkCore.Sqlite (+ .Sqlite.Core, .Relational, .Abstractions, Microsoft.Data.Sqlite.Core) | 10.0.13 | MIT | Infrastructure | |
| SQLitePCLRaw.bundle_e_sqlite3 (+ .core, .lib.e_sqlite3, .provider.e_sqlite3; transitive) | 2.1.13 | Apache-2.0 | Infrastructure | Bundles the native SQLite library, `e_sqlite3.dll` (SQLite itself is public domain). Corrected at the M0 exit: WP1 had recorded 3.0.2. |
| Serilog | 4.4.0 | Apache-2.0 | Infrastructure | D-007 |
| Serilog.Extensions.Logging | 10.0.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.Async | 2.1.0 | Apache-2.0 | Infrastructure | |
| Serilog.Sinks.Debug | 3.0.0 | Apache-2.0 | Infrastructure | Writes only in Debug builds |
| .NET runtime and Windows Desktop runtime | 10.0.13 | MIT | Desktop | Shipped by the self-contained publish, with Microsoft's own third-party notices |

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

## Data shipped with the app

| Data | Version | License | Location | Notes |
|---|---|---|---|---|
| Baby names (U.S. Social Security Administration) | National data, downloaded 2026-10-21 (`names.zip`, 7,860,026 bytes) | Public domain (a U.S. government work, 17 U.S.C. §105) | `data/names/first.json` | Male births 1986–2005: the 2,000 most common names given to boys at least as often as to girls, placeholders removed, weighted by births. Read from `ssa.gov/oact/babynames/names.zip` in the browser (the server refuses scripted downloads); only the name, sex and count columns were read. |
| 2010 Census surnames (U.S. Census Bureau) | `Names_2010Census.csv`, downloaded 2026-10-21 (`names.zip`, 12,874,389 bytes) | Public domain (17 U.S.C. §105) | `data/names/last.json` | The 5,000 most common surnames, weighted by count. From `www2.census.gov/topics/genealogy/2010surnames/names.zip`; only the name and count columns were read. |
| City and town population estimates (U.S. Census Bureau) | Vintage 2025 (`sub-est2025.csv`, 7,464,556 bytes), downloaded 2026-10-21 | Public domain (17 U.S.C. §105) | `data/places/hometowns.json` | 3,489 places of 10,000 people or more: incorporated places, New England towns and Hawaii's counties (Hawaii has no incorporated places), weighted by population. From `www2.census.gov/programs-surveys/popest/datasets/2020-2025/cities/totals/`; only the level, name, state and population columns were read. |

The famous-people blocklist (`data/blocklists/famous-people.json`) also ships: it's a list of names compiled by hand, which are facts, so it carries no license.

## Data used only by tests (never shipped)

| Data | Version | License | Location | Notes |
|---|---|---|---|---|
| IPEDS institutional directory (U.S. Department of Education, NCES) | HD2024 (2024–25), file dated 2025-09-21 | Public domain (a U.S. government work, 17 U.S.C. §105) | `data/blocklists/real-colleges.json` | Institution names and aliases, plus their sports-style short forms, merged with a hand-compiled list. The fictional colleges are tested against it (D-035). Downloaded 2026-10-20 from `nces.ed.gov/ipeds/datacenter/data/HD2024.zip` (1,088,372 bytes). Only the name and alias columns were read; the zip isn't in the repository. |

## Fonts, icons and other assets

| Asset | Version | License | Location | Notes |
|---|---|---|---|---|
| **Archivo** (variable: width 62–125%, weight 100–900), Latin subset | Fontsource 5.3.0 | SIL OFL 1.1 | `src/UI.Components/wwwroot/fonts/archivo-latin-wdth-normal.woff2` | UI and display face ([VISUAL_DESIGN.md §4](docs/VISUAL_DESIGN.md#4-typography)). © The Archivo Project Authors (Omnibus-Type). License text alongside: `Archivo-OFL.txt`. |
| **JetBrains Mono** (variable: weight), Latin subset | Fontsource 5.3.0 | SIL OFL 1.1 | `src/UI.Components/wwwroot/fonts/jetbrains-mono-latin-wght-normal.woff2` | Debug overlay and log viewer only. © The JetBrains Mono Project Authors. License text alongside: `JetBrainsMono-OFL.txt`. |
| Fluent System Icons (via Microsoft.FluentUI.AspNetCore.Components.Icons) | 5.0.1 | MIT | NuGet package | Proposed in D-031, confirmed in Spike S0b |
| Segoe Fluent Icons, falling back to Segoe MDL2 Assets | Windows | Windows system fonts | Not shipped | The WPF caption buttons' glyphs (D-068), as native title bars draw them. Used from Windows, never bundled. |

Fonts were downloaded 2026-10-15 from `cdn.jsdelivr.net/npm/@fontsource-variable/…@5.3.0`, then checked (WOFF2 signature, exact byte size, license text) before they were added. The OFL requires its license text to travel with the fonts, which is why the `.txt` files sit next to them.

## Declared but not yet referenced

`Microsoft.Data.Sqlite` 10.0.13 has its version pinned in `Directory.Packages.props` for a later work package. Its license gets checked and recorded here when a project first references it. `Verify.XunitV3` was pinned too, until its license was checked: its binaries carry a fee for users with US$10,000 or more of annual revenue, so it was dropped for hand-rolled snapshots (D-085).
