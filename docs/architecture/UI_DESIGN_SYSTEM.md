# UI Architecture & Design System

> **Status:** Proposed · 2026-10-13 · Related: [D-003](../DECISIONS.md#d-003-ui-lives-in-razor-class-libraries-the-host-stays-thin), [D-008](../DECISIONS.md#d-008-ui-components-and-assets-permissive-licenses-only), [D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5), [STANDOUT_FEATURES.md §2](../STANDOUT_FEATURES.md#2-modern-elegant-ui)
>
> **Foundation: [Fluent UI Blazor v5](https://www.fluentui-blazor.net)** (Microsoft, MIT, v5.0.0 stable since 2026-09-25), chosen by the owner ([D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5)). GFO builds football composites, a few gap components and a thin theming layer on top.
>
> **Visual values (palette, fonts, radii, density, icons) come from [VISUAL_DESIGN.md](../VISUAL_DESIGN.md)**, the **Stadium** direction with Dark as the default mode ([D-031](../DECISIONS.md#d-031-visual-direction-stadium-light--dark-dark-by-default)). This doc covers the mechanics. When the two disagree on a value, VISUAL_DESIGN.md wins.
>
> **Built in M0 WP8 (2026-10-19):** the theme service, tokens, `Icon`, dialogs and toasts, `DataGrid<T>`, the small components, the app shell and Settings → Appearance, with a gallery page for each in the DevHost. The "As built" notes in each section record where the code differs from the design below.
>
> **API names were verified against Fluent UI Blazor 5.0.1 in Spike S0b** ([D-067](../DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)). Where Fluent needed help (the grid, contrast, Match Windows), the fix is described in the section concerned.

---

## 1. Layers, and one rule

| Layer | Contains | Knows about football? |
|---|---|---|
| **Fluent UI Blazor v5** (NuGet) | Inputs, buttons, overlays, navigation, DataGrid, design tokens, theme service | No |
| **`GridironFrontOffice.UI.Components`** | The GFO theme layer (`GfoThemeService`, extension tokens), selective wrappers (`DataGrid<T>`, dialogs, toasts, `Icon`), gap components Fluent lacks (command palette, charts, KPI tiles, countdown ring) | **No.** App-only ([D-027](../DECISIONS.md#d-027-ui-components-are-for-this-app-only)), kept generic for clean dependencies. |
| **`GridironFrontOffice.UI`** | App shell, pages, and football composites: `PlayerCard`, `RatingChip`, `DepthChartBoard`, `DraftRoom`, `CapSheet`, `TradeBuilder`, `GameCenter`, `Scorebug`, `DriveChart`, `TeamMark` | Yes |

**The rule:** pages never write ad-hoc styles or raw colors. A new look comes from **Fluent tokens**, a **GFO extension token**, or a component. That's how the app stays consistent as it grows.

---

## 2. Design principles

1. **Data first, chrome last.** Content gets the pixels. Borders, shadows and color have to justify themselves.
2. **Calm by default, dramatic on purpose.** 95% of the UI is quiet. Big moments (the pick is in, a game final, a blockbuster trade) get broadcast-style treatment.
3. **Keyboard-first, mouse-friendly.** Everything is reachable by keyboard. `Ctrl+K` goes anywhere.
4. **Peek, then commit.** Side sheets for quick looks, full pages for deep work, modals only for decisions.
5. **Explain every number.** A tooltip, a glossary link or a breakdown on hover.
6. **Instant feedback.** Respond in under 100 ms, use skeletons past 300 ms, show inline progress for long operations. Never freeze the window.
7. **Accessible by construction.** Contrast, focus, motion and color-blind safety are built into the components so pages get them for free (Fluent's components are built for accessibility).

### 2.1 Data-dense and modern: how we blend them

The owner's direction ([C-7](../OPEN_QUESTIONS.md#c-7-modern-and-elegant-vs-data-dense)): **dense *and* modern, not one or the other.** "Modern" comes from hierarchy, rhythm and restraint, not from whitespace.

| Technique | What it looks like |
|---|---|
| **Typographic hierarchy inside cells** | Primary value at full contrast. Secondary info (team, age, contract years) in a smaller, muted line or column. Not everything is equally loud. |
| **Tabular numerals, right-aligned numbers** | Columns of numbers align perfectly and scan fast. |
| **Compact rows, generous column gutters** | Rows are tight (28–32 px), but there's horizontal breathing room between columns. Density without crowding. |
| **No gridlines, subtle row separation** | Hairline row dividers or none. A hover/selection highlight instead of zebra stripes and borders everywhere. |
| **Visual encodings beside the numbers** | Rating chips with color bands, inline bars (cap %, snap share), sparklines (last 5 games), so patterns are visible before the numbers are read |
| **Column groups and sticky context** | "Passing / Rushing / Receiving" group headers. A pinned name column and a sticky header, so you never lose your place. |
| **Progressive disclosure** | Summary row → expand for the detail. Hover cards for secondary stats. Peek sheets for the full player. |
| **Purposeful color** | Neutral by default. Color only *means* something (ratings, deltas, status), so it stands out when it appears. |
| **Saved views and density toggle** | Users choose what's dense. Compact mode for power users, comfortable for browsing. |
| **One focal point per screen** | Every screen has one clear "hero" (the clock, the score, the cap number). The dense data supports it. |

---

## 3. Theming with Fluent UI Blazor v5

### 3.1 How configurable is the theme? (the owner's question)

**Short answer: very configurable for *look* (colors, light/dark, typography, corner radius, spacing, density, team tinting at runtime). Deliberately limited for *structure* (component internals).** You can make it unmistakably *your* app, but its shapes and behaviors will stay in the Fluent 2 family (the Windows 11 / Teams look). That's the trade-off of adopting a full library, and it fits the "modern desktop app" goal well.

| Level | What you can change | How | Configurability |
|---|---|---|---|
| **Brand color** | One hex color generates a **16-step brand ramp** (the Fluent Theme Designer's algorithm), or is used **exactly** (`IsExact`). Fine-tune with **hue torsion** and **vibrancy**. GFO uses the exact color and computes its own accent text colors (§3.2). | `IThemeService.CreateCustomThemeAsync(new ThemeSettings { Color, Mode, IsExact, HueTorsion, Vibrancy })`, edit the returned `Theme`, then `SetThemeAsync(theme)`. (`SetThemeAsync(ThemeSettings)` and a `data-theme-color` root attribute also exist. GFO doesn't use them.) | ⭐⭐⭐ High, **changeable at runtime** |
| **Mode** | Light, Dark, System. Built-in Teams variants. | `ThemeSettings.Mode` (`ThemeMode.Light` / `Dark` / `System`), `SwitchThemeAsync()`, `SetThemeAsync(ThemeColorVariant.Teams)`. GFO only ever passes Light or Dark (§3.2). | ⭐⭐⭐ High, runtime |
| **Design tokens** | Fluent 2 tokens for neutral, brand and status colors, typography (families, sizes, weights, line heights), corner radii, spacing, stroke widths, shadows, motion durations and curves | Set them on the typed `Theme` before applying it (`Theme.Fonts.Family`, `Theme.Borders.Radius`, `Theme.Colors.Neutral` / `.Brand` / `.Compound`), globally or **per element** with `SetThemeToElementAsync(element, theme)` | ⭐⭐⭐ High for anything token-driven |
| **Component options** | Appearance, size and shape variants, plus `Class`/`Style` on components | Component parameters | ⭐⭐ Medium |
| **Component internals** | Internal markup and parts | Mostly off-limits. v5 renders through Fluent Web Components 3 (encapsulated), and v5 removed or changed v4's internal CSS classes. | ⭐ Low, by design |

**Practical meaning for GFO**
- ✅ **Team-tinted accent (from [STANDOUT §2](../STANDOUT_FEATURES.md#2-modern-elegant-ui)):** set the brand color from the user's team when a league opens. GFO uses the *exact* team color for fills and computes the contrast-corrected accent and accent text itself (§3.2). Fluent's generated ramp doesn't keep the team color, and its exact mode doesn't correct text contrast ([D-067](../DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)).
- ✅ **Dark-first plus a light theme:** one setting.
- ✅ **Fonts:** override the font-family tokens once (Archivo + JetBrains Mono, per [VISUAL_DESIGN.md §4](../VISUAL_DESIGN.md#4-typography)).
- ✅ **Dense data screens:** spacing tokens plus the DataGrid's `ItemSize`, which sets the exact row height (28–46 px verified in S0b, §5.3).
- ⚠️ **"Broadcast moments" and football visuals** (scorebug, draft clock, crests, drive chart) are **our own components**. They use Fluent tokens for color and type but are styled freely.
- ❌ Don't restyle Fluent component internals with deep CSS selectors. It breaks on upgrades (v5 already changed internal classes from v4). **The one sanctioned exception** is `DataGrid<T>`'s scoped stylesheet, which carries the two layout fixes the grid can't work without, plus the selected-row paint (§5.3).

### 3.2 Team tinting

```csharp
// GridironFrontOffice.UI.Components/Theming/GfoThemeService.cs: the ONLY place that talks to Fluent's IThemeService
public sealed class GfoThemeService(IThemeService fluent, IAppSettings settings, ISystemTheme system)
{
    private const string NeutralBrand = "#0F6CBD";   // Fluent's default brand blue (VISUAL_DESIGN.md §3.3, Accent: Neutral)

    public async Task ApplyAsync(TeamColors? userTeam)
    {
        var dark = settings.Theme switch                    // default ThemeChoice.Dark (D-031)
        {
            ThemeChoice.Light        => false,
            ThemeChoice.MatchWindows => system.IsDark,     // GFO resolves System itself (D-067)
            _                        => true,
        };
        var primary = settings.Accent == AccentMode.Team && userTeam is not null ? userTeam.Primary : NeutralBrand;

        // Mode AND color together (v5 issue #5351), never ThemeMode.System, and the exact team color.
        var theme = await fluent.CreateCustomThemeAsync(new ThemeSettings
        {
            Color = primary, Mode = dark ? ThemeMode.Dark : ThemeMode.Light, IsExact = true,
        });
        StadiumTokens.Apply(theme, dark);                   // fonts, radii, surfaces (VISUAL_DESIGN.md §3–5, §9)
        AccentTokens.Apply(theme, primary, dark);           // on-accent, Accent ≥ 3:1, Accent text ≥ 4.5:1 (§3.3)
        await fluent.SetThemeAsync(theme);
    }
}
```
- **Why `IsExact` plus GFO's own accent colors:** without `IsExact`, a primary button shows a ramp step (`#903B19` for `#D9541E`), not the team color. With it, Fluent uses the raw color for links, brand text and checked controls as well, which vanish on dark surfaces when the team color is dark. `AccentTokens` therefore sets:
  - `Colors.Neutral.ForegroundOnBrand` to the on-accent color;
  - `Colors.Brand.Foreground*` and `ForegroundLink*` to Accent text;
  - `Colors.Brand.Stroke*` and `Colors.Compound.BrandForeground*` / `BrandStroke*` to Accent.

  Spike S0b verified these on 7 team colors in Dark and Light.
- **Why never `ThemeMode.System`:** given System, Fluent's CSS swaps in its *default* theme when Windows changes mode, dropping the team color and the Stadium tokens.
- **Fluent's own system-theme listener is switched off.** Fluent's script re-applies its default theme on *every* `prefers-color-scheme` change, unless its `localStorage` cache holds an explicit mode, which GFO doesn't use. It does this even under an explicit Dark or Light choice. The host page sets `window.__FluentUIBlazorSystemThemeChangeListener = true` before Fluent loads, the guard Fluent checks before adding that listener.
- **GFO's listener owns every switch.** `ISystemTheme` is a small JS module that watches `prefers-color-scheme` and re-runs `ApplyAsync` on every change: with the new Windows mode under Match Windows, and with the chosen mode otherwise. The re-apply is cheap, and it's a safety net in case a Fluent upgrade drops the guard.
- **Verified in the WPF host** (S0c, [D-068](../DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar)): real WebView2 change events arrived within about 20 ms, and the theme followed with no Fluent-default frames in between.
- **Settings → Appearance:** Dark (**default**) / Light / Match Windows ([VISUAL_DESIGN.md §2](../VISUAL_DESIGN.md#2-modes)).
- Called at **app start** (neutral brand) and on **league open** (team brand). Settings change → call it again.
- **Per-element theming** (`SetThemeToElementAsync`) can tint a region in *another* team's colors, such as the opponent's half of the Game Center or the "pick is in" card in the picking team's colors, without touching the global theme.
- Setting: **Accent: Team / Neutral** (some team colors are loud).
- **As built (M0 WP8):**
  - **`GfoThemeService.ApplyAsync(GfoAppearance)`.** `UI.Components` can't see Application's settings, so the service takes its own small value: `GfoAppearance(Mode, AccentColor, Density)`, where a null `AccentColor` means the neutral blue. In the UI layer, `AppearanceMapping.From(settings.Appearance, teamPrimary)` builds it, and `AppearanceSync` (rendered once by `MainLayout`) applies it after the first render and again on every `ISettingsStore.Changed`. Until M1 there's no team, so Accent: Team shows the neutral blue.
  - **`GfoThemeState`** (`Current`, and the `Applied` event) carries the resolved mode, the four accent colors and the row height for the density. `DataGrid<T>` listens to it. The desktop host doesn't: the service is scoped to the page, so the host follows the `data-theme` attribute instead (§7, as built in WP9).
  - **The page side is `gfo-theme.js`:** it reports Windows light/dark changes back to the service (`OnWindowsThemeChanged`), and after each apply sets `data-theme` and `data-density` on `<html>` (which `gfo-tokens.css` reads) and removes `gfo-booting`.
  - **The state variants are mapped too.** Fluent's hover, pressed and selected slots for Background 2–5, the card background, Foreground 1–3 and Stroke 1 get Stadium values, one surface step up from their base. Otherwise some controls showed Fluent's own default grays on hover.
  - **Contrast:** the gallery's Theme page shows each derived color's ratio live. On-accent is white or near-black, whichever contrasts more, so a mid-tone team color can land at about 4.4:1 on its fill (the worst case of that rule); every F-02 primary tested is above 4.5:1.

### 3.3 Source of truth and persistence
- Fluent caches `ThemeSettings` in `localStorage` (key `fluentui-blazor:theme-settings`, inside the WebView2 profile), but **only on the `SetThemeAsync(ThemeSettings)` path**. Applying a typed `Theme` neither writes nor clears the entry, and `GetThemeSettingsAsync` just reads the cache back, so it goes stale (verified in S0b).
- **GFO's source of truth is `settings.json`** (theme, accent mode) plus the open save (team colors). `GfoThemeService` calls `ClearStoredThemeSettingsAsync()` at startup and never reads the cache, so a stale entry can't fight `settings.json`.
- **First paint is Stadium-colored from the first frame** (S0c, [D-068](../DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar)). Until `ApplyAsync` runs after the first render, Fluent's *default* theme would show. Measured without the layers below: about 220 ms of white (WebView2's default background), then a frame of Fluent's default theme, which is light on a light Windows. Three layers prevent it:
  1. **Native, from `settings.json`:** the WPF window's background, and WebView2's `DefaultBackgroundColor` (set in `BlazorWebViewInitializing`), are both the saved mode's Ground.
  2. **Static host page:** `<html class="gfo-booting">`, with boot-only rules that keep the page transparent (so the native color shows) and `#app` hidden. The class is static on purpose: a script registered with `AddScriptToExecuteOnDocumentCreatedAsync` can lose the race with Blazor's first navigation.
  3. **The theme service removes `gfo-booting`** after its first `ApplyAsync`, so the themed page appears in one step.
- **Leave WebView2's `PreferredColorScheme` at Auto.** It's persisted in the WebView2 profile, so setting it would outlive the session.
- **As built (M0 WP9):** the desktop host decides the first-paint mode before DI (`WindowsTheme.StartsDark`): Dark or Light from `settings.json`, and for Match Windows the Windows app mode from the registry (`AppsUseLightTheme`; missing counts as light). After that the page owns the mode, and the host follows it (§7).

### 3.4 GFO extension tokens (what Fluent doesn't have)
Use **Fluent's tokens directly** for surfaces, text, borders, brand, status, type, radius, spacing, shadows and motion. Don't build a parallel `--gfo-surface-*` system. Add only what a football GM needs:

```css
/* GridironFrontOffice.UI.Components/wwwroot/gfo-tokens.css (Stadium values: VISUAL_DESIGN.md §3–5) */
:root {                                   /* Dark is the default mode */
  /* Rating scale: ALWAYS paired with the number */
  --gfo-rating-elite: #3DD6C6;   /* 90+   */
  --gfo-rating-good:  #8ADB8F;   /* 80–89 */
  --gfo-rating-avg:   #E8D46A;   /* 70–79 */
  --gfo-rating-below: #F4A65F;   /* 60–69 */
  --gfo-rating-poor:  #F07B7B;   /* < 60  */
  --gfo-urgent:       #C62828;   /* draft clock ≤ 10 s, expiring offers, LIVE */

  /* Team colors for non-brand uses (opponent, crests, ticker). Set per element/region. */
  --gfo-team-primary:   currentColor;
  --gfo-team-secondary: currentColor;

  /* Display face: Archivo, condensed */
  --gfo-font-display: 'Archivo', system-ui, sans-serif;
  --gfo-display-stretch: 72%;

  /* Geometry */
  --gfo-radius-s: 8px;  --gfo-radius-m: 14px;  --gfo-radius-l: 20px;  --gfo-radius-pill: 999px;

  /* Data density: mirrors the density setting for GFO's own lists. DataGrid<T> sets ItemSize from the setting in C#. */
  --gfo-grid-row-height: 46px;
  /* Broadcast moments */
  --gfo-dur-moment: 800ms;
}
:root[data-theme="light"] {
  --gfo-rating-elite: #0E766C; --gfo-rating-good: #2E7334; --gfo-rating-avg: #7A5F00;
  --gfo-rating-below: #9A4A00; --gfo-rating-poor: #B42318;
}
:root[data-density="compact"] { --gfo-grid-row-height: 36px; }
```
Rule: if a value exists as a Fluent token, **reference it** (`var(--…)`) instead of duplicating it.

**As built (M0 WP8):** `gfo-tokens.css` also has:
- **status tokens** (`--gfo-status-positive`, `-warning`, `-negative`, `-inactive`), `--gfo-on-urgent`, and `--gfo-chip-tint` (20% Dark, 14% Light) for pill chips;
- **accent aliases** that only read Fluent's brand tokens: `--gfo-accent-fill`, `--gfo-on-accent`, `--gfo-accent`, `--gfo-accent-text`, plus the derived `--gfo-accent-soft` and an opaque `--gfo-selection`;
- the radii as references to Fluent's radius tokens, and `--gfo-cell-padding-x` beside the row height;
- light overrides under `:root[data-theme="light"]`, set by the theme service;
- a `reset` layer (the body font and Ground, and no focus ring for the programmatic focus `FocusOnNavigate` gives a page's `h1`) and the utilities `gfo-visually-hidden`, `gfo-truncate`, `gfo-num`, `gfo-display`.

### 3.5 Typography
| Role | Implementation | Font ([VISUAL_DESIGN.md §4](../VISUAL_DESIGN.md#4-typography)) |
|---|---|---|
| UI text | Override Fluent's base font-family token | **Archivo** (variable), 400–700 |
| Numbers in tables | `font-variant-numeric: tabular-nums` on numeric cells | Archivo |
| Display (scorebug, clock, heroes) | `--gfo-font-display` + `font-stretch: var(--gfo-display-stretch)` | **Archivo condensed** (72% width), 800 |
| Mono (debug overlay) | Fluent's monospace token | **JetBrains Mono** |

Fonts are bundled locally as `woff2` (the app is offline). Both are SIL OFL.

### 3.6 Space, radius, elevation, motion, density
- **Fluent tokens** for spacing, radius, shadows (elevation), and motion durations and curves.
- **Density:** the setting drives a global `data-density` attribute (for `--gfo-grid-row-height` in GFO's own lists) and `DataGrid<T>`'s `ItemSize`, which is the grid's exact row height. `RowSize` is ignored when the grid is virtualized (S0b).
- `@media (prefers-reduced-motion: reduce)` (plus an in-app override) disables "moment" animations in our components and collapses durations to 0.

---

## 4. Styling approach

- **Fluent components:** styled through **tokens and parameters only**. The one exception is `DataGrid<T>`'s scoped stylesheet: two layout fixes and the selected-row paint (§5.3).
- **Our components** (composites, charts, gap components): **plain modern CSS** with **Blazor CSS isolation** (`Component.razor.css`), consuming Fluent tokens and GFO extension tokens. WebView2 is evergreen Chromium, so native nesting, container queries, `:has()`, `color-mix()`, OKLCH and `@layer` all work without polyfills.
- **Global CSS** is ordered with cascade layers: `@layer reset, fluent-overrides, gfo-tokens, components, utilities;`.
- **A tiny utility set** (`gfo-visually-hidden`, `gfo-truncate`, `gfo-num`). No utility framework.
- **JavaScript is a last resort:** small ES modules for the client-side clock animation, drag-and-drop helpers and diagnostics. Fluent's own JS ships with the library.
- **Pages scroll inside their frame.** Fluent's stylesheet sets `body { overflow: hidden; height: 100% }`, so the app shell's content area (and the gallery) is the scroll container, never the body.
- **A scroll frame inside a flex or grid layout** takes `contain: inline-size` (as `DataGrid<T>`'s frame does), or its content's width leaks up and the whole page scrolls sideways instead of the frame.

---

## 5. Components: what Fluent provides vs. what we build

### 5.1 From Fluent UI Blazor v5 (use directly in pages)
| Category | Used for | Notes |
|---|---|---|
| **Buttons and inputs**: button, text and number inputs, select/dropdown, combobox/autocomplete, checkbox, switch, radio group, slider | Forms, filters, game plan sliders, contract builder | Use Fluent directly. No wrappers. |
| **Overlays**: dialog, drawer/panel, tooltip, popover, menu, toast/message bar | Confirmations, **peek sheets**, hover cards, context menus, notifications | Dialogs and toasts go through thin GFO services (§6). Verified in S0b; see the overlay API names below. |
| **Navigation**: nav, tabs, breadcrumb | App shell, sub-pages | — |
| **Display**: badge, avatar, progress, spinner, card, divider, skeleton/placeholder | Status, loading states, KPI containers | `FluentSkeleton` exists in v5 |
| **DataGrid** (`FluentDataGrid`) | Every table screen | Wrapped by GFO's `DataGrid<T>` (§5.3) |
| **Icons** (the Fluent System Icons package, MIT) | UI icons | Confirmed in [VISUAL_DESIGN.md §7](../VISUAL_DESIGN.md#7-iconography) (D-067): Regular weight, Filled for active states. `<FluentIcon Value="@(new Icons.Regular.Size20.Home())" />` (the parameter is `Value`, not `Icon`). GFO's `Icon` component maps names so a swap is cheap. The package adds about 22 MB of per-style DLLs. |

**Overlay API names (verified in S0b).** v5 has no `FluentDrawer` component and no `IToastService`.

| Overlay | How |
|---|---|
| Drawer (peek sheet) | `IDialogService.ShowDrawerAsync<TComponent>(options => …)` with `options.Header.Title`, `options.Header.CloseAction.Visible = true` (the ✕), `options.Alignment = DialogAlignment.End`, `options.Width`, `options.Parameters` (`Dictionary<string, object?>`). The component's markup goes **inside `<FluentDialogBody>`**, which renders the header, the padding and the scrolling body. Escape and the ✕ close it. |
| Confirm dialog | `IDialogService.ShowConfirmationAsync(message, title, primaryButton, secondaryButton)` (in that order). It returns a `DialogResult`, and `Cancelled` is true for the secondary button or Escape. `ShowMessageBoxAsync(MessageBoxOptions)` takes the same values by name. |
| Toast | `INotificationService.ShowToastAsync(options => { options.Intent = ToastIntent.Success; options.Title = …; options.Message = …; })`. Shown bottom-right with an icon and a dismiss button. |

### 5.2 Built by GFO
| Component | Pri | Why we build it |
|---|---|---|
| `CommandPalette` (`Ctrl+K`) | P1 (stub in M1) | Composed from a Fluent dialog + input + list. The command registry is ours. |
| `Stat` (KPI tile), `Kbd` (shortcut hint), `EmptyState`, `KeyValueList`, `Timeline` | P0/P1 | Small, GFO-specific presentation |
| `MoneyField` | P1 | Wraps a Fluent number input with `$12.5M` parsing and formatting |
| `SegmentedControl` | P0 | v5 has no equivalent (verified), so build it from `FluentToggleButton`s. **As built:** a radio group for assistive technology (the chosen option is `aria-checked` and the only one in the Tab order), and focus follows the choice through `gfo-dom.js`, because Fluent's button `Element` can't be focused from .NET. |
| `CountdownRing` | P1 | Trade phone offers, the mini draft clock |
| **Charts:** `Sparkline`, `BarChart`, `LineChart`, `RadarChart`, `WinProbabilityChart` | P1 | Pure SVG, themed with Fluent tokens, accessible (`<title>` + a data-table fallback). Fluent UI Blazor isn't a charting library. |
| **Football composites** (in `GridironFrontOffice.UI`) | — | See §5.4 |

### 5.3 `DataGrid<T>`: a GFO wrapper over `FluentDataGrid`
GM games live in tables, so this wrapper gets extra investment. It adds GFO behavior on top of Fluent's grid: density, saved views, standard cell renderers and the keyboard map.

**Acceptance checklist: verified in Spike S0b** with 2,000 rows × 25 columns. **Verdict: Fluent passes with the GFO actions below, so the fallback isn't needed** ([D-067](../DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)).

| Capability | Fluent v5 (S0b result) | GFO action |
|---|---|---|
| Virtualized rows | ✅ `Virtualize="true"` + `ItemSize`: 23 rows in the DOM for 2,000. Scrolling to row 2,000 works. | Put the grid in an `overflow: auto` container with a fixed height. **Never give the grid `height: 100%`** (it breaks the sticky header). **CSS fix 1:** make the spacer rows span every column. Otherwise rows stop rendering once the grid is scrolled right past column 1. |
| **Pinned first column** | ✅ `Pin="DataGridColumnPin.Start"` with an explicit `Width`. Pinned cells are opaque. | **CSS fix 2:** size the table to its columns (`width: max-content`). Otherwise the pin lets go after a long horizontal scroll. Paint pinned cells like their row (Fluent uses Surface 2) with a 1 px divider. |
| Sticky header **while virtualized** | ✅ `GenerateHeader="DataGridGeneratedHeaderType.Sticky"` holds at every scroll position, together with the pin and virtualization | — |
| Sorting | ✅ `SortBy` (`GridSort<T>`), `SortMode` (`Multiple` works), `SortByColumnAsync`, `OnSortChanged`. 23–52 ms from click to screen on 2,000 rows. | Define every `SortBy` **ascending** (Fluent reverses it for "descending") |
| Column headers | ⚠️ The default header is a column-options button plus a sort button with a 75 px minimum, which overflows 64–72 px stat columns. `HeaderCellAsButtonWithMenu` is a single button but still too wide, and it makes sorting two clicks. | **GFO compact header** via `HeaderCellItemTemplate` + `SortByColumnAsync`. One click sorts (numbers high to low first) and a second flips it. Set `MinWidth` = `Width` (Fluent defaults to 100 px). |
| Reorderable columns | ✅ `ReorderableColumns` (not exercised) | — |
| Column resizing | ✅ `ResizableColumns` + `ResizeType` (adds the options button) | Optional. Decided in WP8, since the compact header owns any options UI. |
| Column show/hide + **saved views per screen** | ❓ No built-in persistence | Wrapper persists column state to `settings.json` |
| Compact density / row size | ✅ **Row height = `ItemSize` exactly** (28, 32, 36 and 44 px tested). `RowSize` is ignored when virtualized. | Wrapper sets `ItemSize` from the density setting (46 / 36 px) |
| Keyboard navigation | ⚠️ Cells are focusable (rows are `display: contents`). **Arrow keys only**, and Down scrolls on into rows virtualization hasn't rendered yet. No PageUp/PageDown/Home/End. Per-column `DisableCellFocus`. | Add PageUp/PageDown, Home/End, Ctrl+Home/End, Enter = open, Space = select |
| Custom Razor cell templates | ✅ `TemplateColumn`. Rating pills fit at 28 px rows. | Standard renderers: rating chip, money, delta, sparkline, position pill, team mark |
| Hierarchical rows / master–detail | ✅ `HasRowDetails` (not exercised) | Box score drill-downs, contract year breakdowns |
| Column groups (Passing / Rushing) | ❓ Not exercised | If missing, build a group header row in the wrapper |

**The two CSS fixes** (and the selected-row paint, below) are the only sanctioned reach into Fluent's markup. They live in `DataGrid<T>`'s scoped stylesheet, and the gallery page exercises them so an upgrade that breaks them shows up immediately:
```css
.gfo-grid .fluent-data-grid tr[aria-hidden="true"] { grid-column: 1 / -1; }   /* Virtualize spacers span every column */
.gfo-grid .fluent-data-grid { width: max-content; min-width: 100%; }             /* sticky pinned cells need the full-width box */
```

**As built (M0 WP8):**
- **Columns are subclasses, not wrappers.** Fluent's grid (like QuickGrid) only collects columns rendered directly in its content; a GFO component rendering a Fluent column one level down left every row empty. So `DataGridColumn<TItem, TValue>` derives from `PropertyColumn` and `DataGridTemplateColumn<TItem>` from `TemplateColumn`, and each applies GFO's rules in `OnParametersSet`:
  - `MinWidth` = `Width`, and a missing `Width` throws;
  - `Numeric` right-aligns, adds `gfo-num` and sorts high to low on the first click;
  - `Pinned` pins the column to the start;
  - the compact header.

  They keep Fluent's parameter names (`Title`, `Property`, `Width`, `Format`, `SortBy`).
- **The grid:** `Items` (any `IEnumerable`, rows in natural order), `Label` (the table's accessible name), `ItemKey`, `Height` of the scrolling frame (default `60vh`), `Virtualize` (default on), `SelectedItem` / `SelectedItemChanged`, `OnItemActivated` (Enter or a double-click), `EmptyContent`. Set `TItem` explicitly when you bind the callbacks: Razor can't infer it through them.
- **`ItemSize` follows the density** from `GfoThemeState` (46 or 36 px). The grid re-renders on a theme change, and virtualization re-measures as it scrolls.
- **The keyboard map lives in `gfo-grid.js`:**
  - PageUp/PageDown move a frame's worth of rows;
  - Home/End go to the first or last cell of the row, Ctrl+Home/End to the first or last row, scrolling virtualized rows into existence first;
  - Enter (open) and Space (select) are passed to .NET.

  Fluent's cells stop key events inside Blazor's own event dispatch, so a Blazor `@onkeydown` on the frame never sees them; a DOM listener does. Fluent's rows are `display: contents`, so heights are measured on cells.
- **Selection:** a click or Space selects; the selected row gets `gfo-row-selected`, painted with the opaque `--gfo-selection` so a pinned cell shows it too. This is the third scoped rule.
- **No rows:** `EmptyContent` fills the whole frame instead of the table, because Fluent's empty row only spans the columns' total width.
- **The frame** has `contain: inline-size`, so it scrolls sideways itself in any flex or grid layout.

**Fallback (kept for upgrades):** if a later Fluent release fails a P0 item (virtualization + pinned column + sticky header together, or compact rows), keep Fluent for everything else and swap the wrapper's internals to another grid (QuickGrid or Radzen; see [UI_COMPONENT_OPTIONS.md](UI_COMPONENT_OPTIONS.md)). **Screens don't change**, because they use `DataGrid<T>`.

### 5.4 Football composites (in `GridironFrontOffice.UI`)
`TeamMark` (generated SVG crest), `PositionPill`, **`RatingChip`/`RatingBar` (0–99 with an uncertainty range, or a plain value when exact; potential as a letter grade; required by fog of war, [D-032](../DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted) and [D-034](../DECISIONS.md#d-034-ratings-display-099-with-ranges-letter-grades-for-potential))**, `PlayerCard`, `PlayerPeekSheet` (in a Fluent drawer), `ContractSummary`, `CapBar`, `CapSheet`, `DepthChartBoard` (drag-and-drop plus keyboard reordering), `GamePlanPanel` (Fluent sliders), `Scorebug`, `DriveChart` (SVG field strip), `PlayByPlayFeed`, `StandingsTable`, `DraftClock`, `DraftTicker`, `BigBoard`, `TradeBuilder`, `TradePhoneCard`, `NewsCard`, `InboxItem`.

---

## 6. Component conventions

### 6.1 Wrap selectively, not everything
The earlier plan said "screens only use GFO component names" ([D-008](../DECISIONS.md#d-008-ui-components-and-assets-permissive-licenses-only)). With a full library adopted, wrapping *every* Fluent component would be pure boilerplate and would hide Fluent features. **Amended rule ([D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5)):**

| Use Fluent directly | Wrap behind a GFO component or service |
|---|---|
| Buttons, inputs, selects, checkboxes, switches, sliders, tabs, badges, cards, tooltips, menus, nav | **DataGrid** (`DataGrid<T>`), **theme** (`GfoThemeService`), **dialogs** (`IGfoDialogs`), **toasts** (`IGfoToasts`), **icons** (`Icon`) |
| *Why:* stable, simple and widely used, so wrapping adds nothing | *Why:* central to GFO behavior, likely to need customization, or worth insulating from vendor changes |

### 6.2 Conventions for GFO's own components
| Convention | Rule |
|---|---|
| Naming | PascalCase, no prefix (`<DataGrid>`, `<Stat>`, `<RatingChip>`). Fluent's are all `Fluent*`, so there are no collisions. CSS class prefix `gfo-`. |
| Parameters | `ChildContent` for content, `Value`/`ValueChanged` for `@bind-Value`, `Class` + `AdditionalAttributes` passthrough on the root element |
| Events | `EventCallback`/`EventCallback<T>`, never `Action` |
| State | Components are dumb. Data loading lives in screen stores. |
| Styling | Fluent tokens and GFO extension tokens only. No raw hex. |
| Accessibility | Each component documents its ARIA role and keyboard map. bUnit tests cover both. |
| Rendering | `@key` in lists. `ShouldRender` for high-frequency components (the clock, grid rows). |
| Docs | Every GFO component has a **gallery page** in `GridironFrontOffice.DevHost` (`/_gallery/...`), shown in light and dark and with team tint on and off |

**As built (M0 WP8): the overlay services.**
- `IGfoDialogs.ConfirmAsync(title, message, confirm, cancel)` returns true for the confirm button; Escape is no.
- `PeekAsync<TComponent>(title, parameters, width)` opens the drawer on the right and completes when it closes.
- `IGfoToasts` has `SuccessAsync`, `InfoAsync`, `WarningAsync` and `ErrorAsync(title, message)`.
- Fluent's `IDialogService` and `INotificationService` have internal members, so they can't be faked: their tests render Fluent's real `FluentProviders` in bUnit, which also proves the argument order.

### 6.3 Setup notes
- Register Fluent UI services in **both** hosts (`GridironFrontOffice.Desktop` and `GridironFrontOffice.DevHost`). The DevHost is where UI work happens day to day.
- Add the Fluent provider/root setup required by v5 to the shared root component in `GridironFrontOffice.UI`, so both hosts get it.
- Pin the Fluent UI Blazor version in `Directory.Packages.props`. Upgrade deliberately (read the release notes; v5.x is new).

---

## 7. App shell and navigation

```
┌───────────────────────────────────────────────────────────────────────────────┐
│ [▲ crest] Wolves · 2027 Week 7 · 4–2 · Cap $12.4M      ⌘K Search   🔔  [ Play Week 7 ▶ ] │  ← custom title/top bar
├────────────┬──────────────────────────────────────────────────────────────────┤
│ Home       │                                                                  │
│ Inbox  (3) │                                                                  │
│ ───────── │                        Routed page content                       │
│ Roster     │                                                                  │
│ Depth Chart│                                                                  │
│ Game Plan  │                                                                  │
│ ───────── │                                                                  │
│ Schedule   │                                                                  │
│ Standings  │                                                                  │
│ Stats      │                                                                  │
│ ───────── │                                                                  │
│ Scouting   │                                                                  │
│ Draft      │                                                                  │
│ Free Agency│                                                                  │
│ Trades     │                                                                  │
│ Finances   │                                                                  │
│ ───────── │                                                                  │
│ League     │                                                                  │
│ History    │                                                                  │
│ Settings   │                                                                  │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

- **The side navigation** uses Fluent's nav component, with badge counts.
- **The app bar is the title bar** ([D-068](../DECISIONS.md#d-068-host-shell-the-app-bar-is-the-title-bar), chosen in Spike S0c over a slim WPF caption strip and the native title bar). There's one 40 px bar. Its right end holds **WPF caption buttons** (46 × 40 px, Segoe Fluent Icons), drawn over the web content, which the WebView2 composition control allows. Everything a native title bar does still works: drag, double-click to maximize, the system menu, Aero Snap, Snap Layouts, resize edges. How:
  - **Window:** `WindowChrome` with `CaptionHeight` 0, `ResizeBorderThickness` from the system, and `GlassFrameThickness` 0,0,0,1 (keeps the DWM shadow and rounded corners). DWM attributes set immersive dark mode, rounded corners and a Border-colored edge.
  - **Drag regions:** the app bar carries `data-drag-region`. A small JS module (`ResizeObserver`) reports the bar's rectangle and its interactive holes (buttons, inputs, `[data-no-drag]`) to the host in CSS px, which equal DIPs at zoom 1. The bar reserves 138 px on the right for the caption buttons.
  - **Hit testing:** a `WM_NCHITTEST` hook, added after `WindowChrome`'s so it runs first, answers in this order:
    1. `HTMAXBUTTON` over the WPF maximize button, which makes Windows 11 show Snap Layouts. Because Windows then owns that button, the hook also handles `WM_NCMOUSEMOVE`/`WM_NCMOUSELEAVE` for hover and `WM_NCLBUTTONUP` for the click.
    2. Nothing over the minimize and close buttons, so WPF handles them (`WindowChrome.IsHitTestVisibleInChrome`).
    3. `HTCAPTION` over the bar's empty space.
    4. Nothing in the top resize band, so `WindowChrome` answers `HTTOP`.
  - **Maximized:** pad the content by the window's *measured* overhang past the monitor's work area (8 px at 100%; `WindowResizeBorderThickness` alone is only 4).
  - **Theme:** the caption buttons follow the theme's mode, so the theme service tells the host when the mode changes.
- **As built (M0 WP9):**
  - **Code:** `src/Desktop/Windowing/` holds `TitleBar` (the chrome, the hook, the DWM attributes, the overhang), `PageMessages` (the page's messages), `WindowPlacementRules` and `Monitors`. `src/Desktop/wwwroot/gfo-host.js` is the page side; only the desktop host page loads it.
  - **One channel, no Blazor interop.** gfo-host.js posts JSON strings through WebView2's own `chrome.webview.postMessage`, and the window reads them in `WebMessageReceived`. BlazorWebView shares the channel but ignores strings without its `__bwv:` prefix. That keeps the whole title bar in the host: the UI layer only marks `data-drag-region` and `data-no-drag`.
    - `{"gfo":"drag","regions":[{"bar":[x,y,w,h],"holes":[[x,y,w,h],…]}]}`: every `[data-drag-region]` and its interactive descendants (buttons, links, inputs, ARIA widgets, Fluent's input elements, `[data-no-drag]`). It's measured at most once per frame after any DOM change, resize, transition end or font load, and sent only when something moved. Any page can have a drag region, not just the shell.
    - `{"gfo":"theme","mode":"dark"|"light"}`: whenever `GfoThemeService` sets `data-theme`. The theme service is scoped to the page, so the host can't subscribe to it; the attribute is the contract instead. On a change the window updates the caption button brushes, the DWM dark-mode flag and border color, its own background and WebView2's background.
  - **Page zoom is off** (`IsZoomControlEnabled = false`), because CSS px must equal DIPs for the hit test. Density is the app's own setting. Release builds also turn off the browser accelerator keys (F5 reload, print, find, DevTools); editing keys still work.
  - **The caption buttons aren't `IsHitTestVisibleInChrome`.** With `CaptionHeight` 0 they don't need it, and it would make WindowChrome answer `HTCLIENT` over them even in the top resize band. They're 39 px tall, so the app bar's bottom border (its 40th pixel) runs under them, and their glyphs dim while the window is inactive, as native ones do. Close turns Windows' own red on hover. The maximize button's click needs a press that started on it.
  - **Measured on the real window** (`WM_NCHITTEST` probes): `HTCAPTION` over the bar's empty space and the wordmark, `HTMAXBUTTON` over maximize, `HTCLIENT` over minimize, close, the nav and the page, `HTTOP`/`HTLEFT` on the edges. Maximized, the content is inset by the measured 8 px, and the top-right pixel closes the window.
- **As built (M0 WP8):**
  - `AppShell` has the slots `Brand` (the wordmark until a league is open), `Context` (the season line), `Progress` (announced politely), `Actions`, `PrimaryAction`, `Navigation` and the page.
  - `ShellOptions(AppBarIsTitleBar)` decides the 138 px caption gap: the desktop host turns it on in WP9; the DevHost leaves it off.
  - `MainNav` is `FluentNav` with Fluent's Small density (28 px items, so the full league nav fits a 720 px window) on Surface 1, set through its `BackgroundColor` parameters. The active item keeps Fluent's accent indicator and semibold text, and gets Accent soft through the `ActiveClass` parameter, which adds GFO's own class rather than touching Fluent's internals.
  - Each item is a `NavEntry`: a Regular icon at rest and a Filled one when active.
- **The primary action button** is context-aware: "Play Week 7", "Advance to Free Agency", "Enter the Draft Room". It's always in the same place, and `Ctrl+Enter` triggers it.
- **Routing:** Blazor `Router` with deep-linkable entity routes (`/player/{id}`, `/team/{id}`, `/game/{id}`, `/draft`, …). Mouse back and forward buttons (XButton1/2) and `Alt+←/→` are wired to history.
- **Peek sheets:** `PeekService.OpenPlayer(id)` opens the `PlayerPeekSheet` (a Fluent drawer) over any page. "Open full profile" navigates.
- **Minimum window** 1280×720. Designed for 1920×1080 and scales cleanly to 1440p/4K. Container queries reflow panels.

---

## 8. Keyboard map (initial)

| Shortcut | Action |
|---|---|
| `Ctrl+K` | Command palette |
| `Ctrl+Enter` | Primary action (Play Week / Advance / Send in the pick*) |
| `G` then `R` / `D` / `S` / `F` / `T` | Go to Roster / Depth Chart / Standings / Free Agency / Trades |
| `/` | Focus the grid quick filter |
| `Esc` | Close the top overlay |
| `Alt+←` / `Alt+→` | Back / Forward |
| `?` | Shortcut cheat sheet |

\* In the Draft Room, sending in the pick requires the confirm dialog (`Enter` twice) to prevent accidents.

---

## 9. UI state management

- **No global Flux library.** Three simple tiers:
  1. **`GameStateStore`** (singleton per league session): current date and phase, user team summary, cap summary, inbox count, live event status. It listens to domain events and refreshes itself.
  2. **Screen stores / view models** (one per page): load read models through Application query services, expose state plus `Changed` events, and dispose with the page.
  3. **Component-local state** for UI-only concerns.
- After commands, rely on **domain events → store refresh**, not manual cross-page refresh calls ([DATA_ACCESS_GUIDE.md §5.7](DATA_ACCESS_GUIDE.md#57-calling-commands-from-the-ui)).
- Long operations render **inline progress** in the top bar ("Simulating Week 7 · 9/16 games") instead of blocking modals.

---

## 10. Broadcast moments

Reserved, choreographed animations in GFO's own components (with a reduced-motion fallback):

| Moment | Treatment |
|---|---|
| **The pick is in** | A dim overlay, the team crest wipe, the player name in the display face, the position and school, a short pulse in the picking team's colors (~1.5 s, skippable) |
| **Your pick on the clock: final 10 s** | The clock turns warning, then negative. A subtle tick sound (if sound is on). |
| **Game final** | The scorebug expands to a final card with the top performers |
| **Blockbuster trade or signing** | A ticker "breaking" style card |
| **Championship** | Confetti-free, elegant: a full-bleed team color field with the trophy line |

---

## 11. Sound (all off by default)

Minimal sounds: a click for confirm, a draft clock tick in the final 10 s, "the pick is in", an incoming offer, the game-final horn. **All off by default**, with volume and mute in Settings. No music in 1.0. All original or CC0 assets ([D-047](../DECISIONS.md#d-047-sound-all-off-by-default-no-music)). The non-audio "you're on the clock" cue is a taskbar flash ([LIVE_EVENTS.md §3](LIVE_EVENTS.md#3-time-handling)).

---

## 12. Testing the UI
- **bUnit** for GFO components: rendering, parameters, keyboard behavior, ARIA attributes. (Fluent's own components are tested upstream. We test our wrappers' behavior.)
- **Gallery** pages double as manual visual QA, in light/dark and with team tint on and off.
- **Playwright through `GridironFrontOffice.DevHost`** (optional, from M3): smoke flows such as creating a league, playing a week and opening a player.
- Details: [TESTING_AND_QUALITY.md](TESTING_AND_QUALITY.md).
