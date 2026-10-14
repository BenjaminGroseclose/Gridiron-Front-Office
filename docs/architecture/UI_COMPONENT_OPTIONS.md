# UI Component Library Options

> **Status:** **Decided: Fluent UI Blazor v5, strategy C** ([D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5)) · 2026-10-13. Kept as the record of what was considered, and as the fallback list if the Fluent DataGrid fails Spike S0b.
>
> **Update since this was written:** Fluent UI Blazor **v5.0.0 went stable on 2026-09-25**. Its DataGrid gained pinned columns, reorderable columns, hierarchical rows and master/detail. v4 now gets only essential security fixes. See [UI_DESIGN_SYSTEM.md §3](UI_DESIGN_SYSTEM.md#3-theming-with-fluent-ui-blazor-v5) for theming.
>
> Owner feedback: *"I would be open to using an already existing DataGrid / component library. Can you provide a few options that I could review online?"*
>
> Fonts, icons and the overall theme are decided in [VISUAL_DESIGN.md](../VISUAL_DESIGN.md) (Stadium).

---

## 1. Three strategies

| Strategy | What you build | What you adopt | Cost | Risk |
|---|---|---|---|---|
| **A. Fully in-house** (the original plan) | Everything | Nothing | Highest (the DataGrid alone is weeks) | Lowest license or look risk |
| **B. Hybrid** | Design tokens, app shell, buttons, cards, dialogs, football composites | **A DataGrid** (the most expensive component), themed to match | Medium | Two styling systems to reconcile, but in one well-contained component |
| **C. Adopt a full library** | Football composites + theme overrides | Nearly all generic components | Lowest | The app looks like *that library* unless you invest in theming. You're tied to its release cadence. |

> **My lean is B.** The DataGrid is where in-house cost explodes. Buttons, cards and dialogs are cheap to build and are where the GFO look lives. Strategy C is reasonable too if a library's look already feels right to you (Fluent UI is the strongest candidate for that).

---

## 2. Hard requirements (filters)

1. **License:** compatible with a future commercial Steam release with no revenue cap you might plausibly hit. MIT or Apache preferred. A "community license" is acceptable only if you understand its limits.
2. **Works in Blazor Hybrid** (BlazorWebView). All the options below do.
3. **DataGrid:** row virtualization with 2,000+ rows, a sticky header, a **frozen first column**, multi-column sort, **Razor cell templates** (rating chips, team marks), compact density, keyboard navigation, column show/hide, a dark theme.
4. **Themeable via CSS variables**, so it can match GFO's tokens.

---

## 3. Options to review

### Open source (MIT): no cost, no revenue limits

| Library | DataGrid highlights | Look and theming | Watch-outs | Review |
|---|---|---|---|---|
| **Radzen Blazor Components** | Virtualization, simple/advanced/Excel-style filtering, **frozen columns (left/right)**, resize, reorder, grouping, density options, saving and loading grid settings. Large library (100+ components, including charts). | Free themes: Material, Standard, Default, Dark, Humanistic, Software (+ dark variants). **Material 3 and Fluent themes are premium** (paid tier). | The default themes read "enterprise admin", so expect CSS work for a GFO look. | [DataGrid demos](https://blazor.radzen.com/datagrid) · [Frozen columns](https://blazor.radzen.com/datagrid-frozen-columns) · [Density](https://blazor.radzen.com/datagrid-density) · [Themes](https://www.radzen.com/blazor-themes-and-ui-components) · [GitHub](https://github.com/radzenhq/radzen-blazor) |
| **MudBlazor** | `MudDataGrid`: sorting, filtering, grouping, virtualization, editing, column resizing, hierarchical rows. Very large community. | Material Design, themeable through `MudTheme` | A strongly Material look (ripples, elevation) is harder to make feel un-Material. **Verify frozen columns + virtualization together** in the demos. | [DataGrid docs](https://mudblazor.com/components/datagrid) · [GitHub](https://github.com/MudBlazor/MudBlazor) |
| **Fluent UI Blazor** (Microsoft) | `FluentDataGrid` is built on QuickGrid: virtualization, sorting, resizable columns, keyboard navigation | Fluent 2 (the Windows 11 / Teams look): modern, calm, desktop-native. Theming with design tokens. | **v5 is a major rewrite** and was at release candidate as of April 2026. Starting on v4 means a migration, and starting on v5 means some churn. A reported issue with the sticky header + virtualization ([#4102](https://github.com/microsoft/fluentui-blazor/issues/4102)). | [Docs & demos](https://www.fluentui-blazor.net) · [GitHub](https://github.com/microsoft/fluentui-blazor) |
| **QuickGrid** (Microsoft, part of ASP.NET Core) | Minimal and fast: virtualization, sorting, paging | **Unstyled.** You own 100% of the look, which makes it ideal for strategy A or B. | No built-in filter UI, column resize/reorder or frozen columns (a frozen first column is doable with CSS `position: sticky`). Third-party extensions exist but are unvetted. | [Docs](https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid) · [Samples](https://aspnet.github.io/quickgridsamples/) |

### Commercial or limited free license: the most powerful grids

| Library | License | Notes | Review |
|---|---|---|---|
| **Syncfusion Blazor** | Commercial. **Free Community License** if annual revenue is under $1M, with 5 or fewer developers, 10 or fewer employees, and under $3M of outside funding. | Arguably the most complete grid (frozen rows and columns, row and column virtualization, Excel-like filtering). Very large suite. You're bound to vendor terms if you ever pass the thresholds. | [DataGrid demos](https://blazor.syncfusion.com/demos/datagrid/overview) · [Community license](https://www.syncfusion.com/products/communitylicense) |
| **Telerik UI for Blazor** | Commercial, paid per developer per year | Excellent grid, polished themes | [Grid demos](https://demos.telerik.com/blazor-ui/grid/overview) |
| **DevExpress Blazor** | Commercial, paid per developer per year | Excellent grid | [Demos](https://demos.devexpress.com/blazor/) |

### JavaScript grid (via interop)

| Library | License | Notes | Review |
|---|---|---|---|
| **AG Grid** | Community edition is **MIT**. Enterprise is commercial. | The strongest raw data grid (pinned columns, row and column virtualization, a CSS-variable theming API). **No official Blazor component.** Community wrappers expose a subset. Cell content can't be Razor components, so football visuals (rating chips, team marks) need JS/HTML renderers. | [ag-grid.com](https://www.ag-grid.com/) |

### Excluded
| Library | Why |
|---|---|
| **Blazorise** | Dual-licensed. Current listings show the free Community tier as **non-commercial only**, so a Steam release would need paid per-developer licenses. Fine if you're willing to pay, but it fails the "no-cost, Steam-ready" filter. |

> Licensing changes often (several .NET libraries changed theirs in 2025). **Re-check the license page of whichever library you pick at the moment you adopt it**, and record the version and license in [DECISIONS.md](../DECISIONS.md).

---

## 4. How to evaluate (about 30–60 minutes per library)

Open each library's online demos and score them 1–5 on:

| # | Check | Why it matters for GFO |
|---|---|---|
| 1 | Virtualized grid + **frozen first column** + sticky header. Scroll both directions. Does it stay aligned and smooth? | Every roster and stats screen |
| 2 | **Compact density** at 1080p. How many rows fit, and is it still readable? | Data-dense, modern ([C-7](../OPEN_QUESTIONS.md#c-7-modern-and-elegant-vs-data-dense)) |
| 3 | **Dark theme.** Does it look modern or "admin panel"? | Pillar 4 |
| 4 | **Custom cell template** (a Razor component in a cell) | Rating chips with ranges, team marks |
| 5 | **Keyboard:** arrows, Enter, Tab order | Keyboard-first UX |
| 6 | Multi-sort, column hide/show, **save and restore column layout** | Saved views per screen |
| 7 | **Theming effort:** inspect class names and CSS variables. How hard would matching GFO's tokens be? | Consistency |
| 8 | **Docs and activity:** recent releases, open issues on the grid | Long-term solo maintenance |

**Suggested shortlist to try first:** Radzen DataGrid, Fluent UI Blazor DataGrid and QuickGrid. Add MudBlazor if you like Material, and Syncfusion if you want to see the ceiling of what a grid can do.

---

## 5. What changes depending on the choice

| Choice | Impact on [UI_DESIGN_SYSTEM.md](UI_DESIGN_SYSTEM.md) |
|---|---|
| **A** (in-house) | No change. The DataGrid stays a P0 build in M1. |
| **B** (hybrid) | `GridironFrontOffice.UI.Components` wraps the chosen grid in a `DataGrid<T>` facade (so screens don't depend on the vendor API directly) and maps GFO tokens to the grid's CSS variables. Everything else stays in-house. |
| **C** (full library) | `GridironFrontOffice.UI.Components` becomes a thin layer: a theme (token → library variables), a few wrappers, and football composites. The component inventory shrinks to what the library lacks. |

In all three cases, keep **screens depending on GFO's own component names** (`<DataGrid>`, `<Button>`), not the vendor's. That makes a later switch much cheaper.
