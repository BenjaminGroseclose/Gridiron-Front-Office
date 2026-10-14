# UI Architecture & Design System

> **Status:** Proposed · 2026-10-13 · Related: [D-003](../DECISIONS.md#d-003-ui-lives-in-razor-class-libraries-the-host-stays-thin), [D-008](../DECISIONS.md#d-008-ui-components-and-assets-permissive-licenses-only), [D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5), [STANDOUT_FEATURES.md §2](../STANDOUT_FEATURES.md#2-modern-elegant-ui)
>
> **Foundation: [Fluent UI Blazor v5](https://www.fluentui-blazor.net)** (Microsoft, MIT, v5.0.0 stable since 2026-09-25), chosen by the owner ([D-025](../DECISIONS.md#d-025-ui-component-library-fluent-ui-blazor-v5)). GFO builds football composites, a few gap components and a thin theming layer on top.
>
> **Visual values (palette, fonts, radii, density, icons) come from [VISUAL_DESIGN.md](../VISUAL_DESIGN.md)**, the **Stadium** direction with Dark as the default mode ([D-031](../DECISIONS.md#d-031-visual-direction-stadium-light--dark-dark-by-default)). This doc covers the mechanics. When the two disagree on a value, VISUAL_DESIGN.md wins.
>
> **API names marked "(v5)" should be confirmed against the v5 docs during Spike S0b.** v5 is a fresh major release, and some details are from release notes rather than the API reference.

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

| Level | What you can change | How (v5) | Configurability |
|---|---|---|---|
| **Brand color** | One hex color generates a **16-step brand ramp** (the same algorithm as the Fluent Theme Designer, built to pass contrast checks). Optionally use the *exact* color. Fine-tune with **hue torsion** and **vibrancy**. | `IThemeService.SetThemeAsync(new ThemeSettings { Color, Mode, HueTorsion, Vibrancy })`, or `data-theme-color` on the root element | ⭐⭐⭐ High, **changeable at runtime** |
| **Mode** | Light, Dark, System. Built-in Teams light and dark themes. | `ThemeSettings.Mode`, `SetDarkThemeAsync()`, `SwitchThemeAsync()`, `SetTeamsLightThemeAsync()` | ⭐⭐⭐ High, runtime |
| **Design tokens** | Fluent 2 tokens for neutral, brand and status colors, typography (families, sizes, weights, line heights), corner radii, spacing, stroke widths, shadows, motion durations and curves | Override the CSS custom properties globally, or **per element** (v5 can apply a theme to one element subtree) | ⭐⭐⭐ High for anything token-driven |
| **Component options** | Appearance, size and shape variants, plus `Class`/`Style` on components | Component parameters | ⭐⭐ Medium |
| **Component internals** | Internal markup and parts | Mostly off-limits. v5 renders through Fluent Web Components 3 (encapsulated), and v5 removed or changed v4's internal CSS classes. | ⭐ Low, by design |

**Practical meaning for GFO**
- ✅ **Team-tinted accent (from [STANDOUT §2](../STANDOUT_FEATURES.md#2-modern-elegant-ui)) is built in:** set the brand color from the user's team when a league opens. Fluent generates an accessible ramp, so we don't need to write our own contrast correction.
- ✅ **Dark-first plus a light theme:** one setting.
- ✅ **Fonts:** override the font-family tokens once (Archivo + JetBrains Mono, per [VISUAL_DESIGN.md §4](../VISUAL_DESIGN.md#4-typography)).
- ✅ **Dense data screens:** spacing and row-height tokens plus the DataGrid's options. Verify the compact row height in Spike S0b.
- ⚠️ **"Broadcast moments" and football visuals** (scorebug, draft clock, crests, drive chart) are **our own components**. They use Fluent tokens for color and type but are styled freely.
- ❌ Don't restyle Fluent component internals with deep CSS selectors. It breaks on upgrades (v5 already changed internal classes from v4).

### 3.2 Team tinting

```csharp
// GridironFrontOffice.UI.Components/Theming/GfoThemeService.cs: the ONLY place that talks to Fluent's IThemeService
public sealed class GfoThemeService(IThemeService fluent, IAppSettings settings)
{
    private const string NeutralBrand = "#0F6CBD";   // Fluent's default brand blue (VISUAL_DESIGN.md §3.3, Accent: Neutral)

    public Task ApplyAsync(TeamColors? userTeam) =>
        // Always pass mode AND color together. Known v5 issue #5351: SetThemeAsync(color) alone resets the mode to System.
        fluent.SetThemeAsync(new ThemeSettings
        {
            Color = settings.Accent == AccentMode.Team && userTeam is not null ? userTeam.Primary : NeutralBrand,
            Mode  = settings.Theme switch                    // default ThemeChoice.Dark (D-031)
            {
                ThemeChoice.Light        => ThemeMode.Light,
                ThemeChoice.MatchWindows => ThemeMode.System,
                _                        => ThemeMode.Dark,
            },
        });
}
```
- **Settings → Appearance:** Dark (**default**) / Light / Match Windows ([VISUAL_DESIGN.md §2](../VISUAL_DESIGN.md#2-modes)).
- Called at **app start** (neutral brand) and on **league open** (team brand). Settings change → call it again.
- **Per-element theming** (v5) can tint a region in *another* team's colors, such as the opponent's half of the Game Center or the "pick is in" card in the picking team's colors, without touching the global theme.
- Setting: **Accent: Team / Neutral** (some team colors are loud).

### 3.3 Source of truth and persistence
- Fluent v5 caches theme settings in `localStorage` (inside the WebView2 profile) and restores them automatically.
- **GFO's source of truth is `settings.json`** (theme, accent mode) plus the open save (team colors). `GfoThemeService.ApplyAsync` runs at startup and on league open, so Fluent's cache is only an optimization, never the authority. That avoids two sources of truth disagreeing.

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

  /* Data density */
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
- **Density:** a global `data-density` attribute drives `--gfo-grid-row-height` and the DataGrid's row size option.
- `@media (prefers-reduced-motion: reduce)` (plus an in-app override) disables "moment" animations in our components and collapses durations to 0.

---

## 4. Styling approach

- **Fluent components:** styled through **tokens and parameters only**.
- **Our components** (composites, charts, gap components): **plain modern CSS** with **Blazor CSS isolation** (`Component.razor.css`), consuming Fluent tokens and GFO extension tokens. WebView2 is evergreen Chromium, so native nesting, container queries, `:has()`, `color-mix()`, OKLCH and `@layer` all work without polyfills.
- **Global CSS** is ordered with cascade layers: `@layer reset, fluent-overrides, gfo-tokens, components, utilities;`.
- **A tiny utility set** (`gfo-visually-hidden`, `gfo-truncate`, `gfo-num`). No utility framework.
- **JavaScript is a last resort:** small ES modules for the client-side clock animation, drag-and-drop helpers and diagnostics. Fluent's own JS ships with the library.

---

## 5. Components: what Fluent provides vs. what we build

### 5.1 From Fluent UI Blazor v5 (use directly in pages)
| Category | Used for | Notes |
|---|---|---|
| **Buttons and inputs**: button, text and number inputs, select/dropdown, combobox/autocomplete, checkbox, switch, radio group, slider | Forms, filters, game plan sliders, contract builder | Use Fluent directly. No wrappers. |
| **Overlays**: dialog, drawer/panel, tooltip, popover, menu, toast/message bar | Confirmations, **peek sheets**, hover cards, context menus, notifications | Dialogs and toasts go through thin GFO services (§6) |
| **Navigation**: nav, tabs, breadcrumb | App shell, sub-pages | — |
| **Display**: badge, avatar, progress, spinner, card, divider, skeleton/placeholder | Status, loading states, KPI containers | Confirm skeleton availability in v5. Build one if it's missing. |
| **DataGrid** (`FluentDataGrid`) | Every table screen | Wrapped by GFO's `DataGrid<T>` (§5.3) |
| **Icons** (the Fluent System Icons package, MIT) | UI icons | Proposed in [VISUAL_DESIGN.md §7](../VISUAL_DESIGN.md#7-iconography-proposed): Regular weight, Filled for active states. GFO's `Icon` component maps names so a swap is cheap. |

### 5.2 Built by GFO
| Component | Pri | Why we build it |
|---|---|---|
| `CommandPalette` (`Ctrl+K`) | P1 (stub in M1) | Composed from a Fluent dialog + input + list. The command registry is ours. |
| `Stat` (KPI tile), `Kbd` (shortcut hint), `EmptyState`, `KeyValueList`, `Timeline` | P0/P1 | Small, GFO-specific presentation |
| `MoneyField` | P1 | Wraps a Fluent number input with `$12.5M` parsing and formatting |
| `SegmentedControl` | P0 | If v5 has no equivalent, build it from toggle buttons |
| `CountdownRing` | P1 | Trade phone offers, the mini draft clock |
| **Charts:** `Sparkline`, `BarChart`, `LineChart`, `RadarChart`, `WinProbabilityChart` | P1 | Pure SVG, themed with Fluent tokens, accessible (`<title>` + a data-table fallback). Fluent UI Blazor isn't a charting library. |
| **Football composites** (in `GridironFrontOffice.UI`) | — | See §5.4 |

### 5.3 `DataGrid<T>`: a GFO wrapper over `FluentDataGrid`
GM games live in tables, so this wrapper gets extra investment. It adds GFO behavior on top of Fluent's grid: density, saved views, standard cell renderers and the keyboard map.

**Acceptance checklist** (verify in Spike S0b with 2,000 rows):

| Capability | Fluent v5 status | GFO action |
|---|---|---|
| Virtualized rows | ✅ Supported | Use it. Requires a fixed row height. |
| **Pinned first column** | ✅ `Pin` on columns (left/right, needs an explicit width) (v5) | Pin the name column |
| Sticky header **while virtualized** | ⚠️ There was a v4 issue ([#4102](https://github.com/microsoft/fluentui-blazor/issues/4102)) | **Test explicitly** |
| Sorting | ✅ Supported (plus an `OnSortChanged` event, v5) | Multi-sort: verify, or implement in the wrapper |
| Reorderable columns | ✅ Supported (v5) | — |
| Column resizing | ❓ Verify in v5 | — |
| Column show/hide + **saved views per screen** | ❓ No built-in persistence assumed | Wrapper persists column state to `settings.json` |
| Compact density / row size | ❓ Verify the option | Wrapper sets the row height from `--gfo-grid-row-height` |
| Keyboard navigation | ✅ Cell focus supported (with a `DisableCellFocus` option, v5) | Add Enter = open, Space = select |
| Custom Razor cell templates | ✅ Template columns | Standard renderers: rating chip, money, delta, sparkline, position pill, team mark |
| Hierarchical rows / master–detail | ✅ (v5) | Box score drill-downs, contract year breakdowns |
| Column groups (Passing / Rushing) | ❓ Verify | If missing, build a group header row in the wrapper |

**Fallback:** if `FluentDataGrid` fails a P0 item (virtualization + pinned column + sticky header together, or compact rows), keep Fluent for everything else and swap the wrapper's internals to another grid (QuickGrid or Radzen; see [UI_COMPONENT_OPTIONS.md](UI_COMPONENT_OPTIONS.md)). **Screens don't change**, because they use `DataGrid<T>`.

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
- **Custom title bar** ([D-021](../DECISIONS.md#d-021-host-wpf--blazorwebview-maui-removed)), with two options to validate in M0:
  - **(a)** `WindowChrome` with a slim native caption strip styled to match. It keeps Windows Snap Layouts. (The .NET 10 WPF `BlazorWebView` uses WebView2's composition control, so WPF elements can overlay the web content. See [HOST_BENCHMARK.md](HOST_BENCHMARK.md).)
  - **(b)** A borderless window where the whole top bar is HTML, using WebView2 non-client regions (`app-region: drag`). More seamless, but no native caption-button behavior.
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
