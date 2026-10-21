# Visual Design: "Stadium"

> **Status:** Accepted (direction + modes) · 2026-10-14 · Decision: [D-031](DECISIONS.md#d-031-visual-direction-stadium-light--dark-dark-by-default) · Fulfils [D-023](DECISIONS.md#d-023-fonts-icons-and-theme-deferred-to-a-visual-design-document)
>
> **Explorations:** the [GFO Visual Directions canvas](https://claude.ai/artifact/A3WxdQTV6LYSDV2TWaR1ba) (private to the owner). Direction **D · Stadium** was chosen over A · Fluent Native, B · Broadcast and C · Analyst Terminal. The canvas boards are the visual reference for every value below.
>
> **How this doc is used:** [UI_DESIGN_SYSTEM.md](architecture/UI_DESIGN_SYSTEM.md) defines the *mechanics* (Fluent UI Blazor v5, tokens, wrappers). This doc defines the *values*: what GFO looks like.

---

## 1. The direction in one paragraph

**Stadium is bright, athletic and rounded.** Big condensed headlines, generous rounded panels and pill-shaped chips, team-color hero bands, and dense tables that still breathe. It's built in two modes the player chooses between, **Dark (the default)** and Light, and the team they manage tints the accent throughout the app.

| Keep | Avoid |
|---|---|
| Rounded geometry: 8 / 14 / 20 px corners, pills for chips, filters and position tags | Sharp rectangles, hairline-only "terminal" styling |
| Condensed, heavy display type for titles, clocks and big numbers | Decorative serifs, all-caps everywhere |
| Team color as **big, confident bands** in a few places | Team color sprinkled on every element |
| Neutral ground, color only where it *means* something | Gradient washes, glows, colored card edges |

---

## 2. Modes

| Setting (Settings → Appearance) | Behavior |
|---|---|
| **Dark** (default on first launch) | Slate dark palette (§3.1) |
| **Light** | Stadium light palette (§3.2) |
| **Match Windows** | Follows the Windows app mode setting. `GfoThemeService` watches it and hands Fluent Dark or Light, never `ThemeMode.System` (D-067). |

- The choice is stored in `settings.json` (app-wide, not per save) and applied by `GfoThemeService` at startup and on league open ([UI_DESIGN_SYSTEM.md §3.2](architecture/UI_DESIGN_SYSTEM.md#32-team-tinting)).
- **Alternative dark palettes** explored on the canvas: *team-tinted* (the background blends 10–16% toward the team color) and *true black* (#000 base). Neither is adopted. Either could become an extra appearance option later without touching components, because they're token swaps.

---

## 3. Color

### 3.1 Dark palette ("slate", default)

| Token role | Hex | Use |
|---|---|---|
| Ground | `#0F1318` | App background |
| Surface 1 | `#171C23` | Panels, cards, nav, table body |
| Surface 2 | `#1E242D` | Table headers, inputs, secondary buttons, stat tiles |
| Surface 3 | `#262E38` | Position pills, tracks, quiet chips |
| Border | `#2C3540` | Row dividers, panel outlines |
| Border strong | `#44505F` | Input and secondary button outlines, header underline |
| Text | `#F1F4F7` | Primary text |
| Muted | `#BAC3CE` | Secondary text, labels |
| Faint | `#8F9AA8` | Tertiary text: ranges, jersey numbers, timestamps (≥ 4.5:1 on Surface 1) |

### 3.2 Light palette

| Token role | Hex |
|---|---|
| Ground | `#EDF0F4` |
| Surface 1 | `#FFFFFF` |
| Surface 2 | `#F6F8FA` |
| Surface 3 | `#E9EDF2` |
| Border | `#DDE2E8` |
| Border strong | `#B9C1CC` |
| Text | `#0D1117` |
| Muted | `#39424E` |
| Faint | `#5B6573` |

### 3.3 Team accent (the user's franchise colors)

The accent comes from the user team's **primary color** and is contrast-corrected per mode. In production, `GfoThemeService` applies the primary **exactly** (Fluent's `IsExact`), computes the derived colors below itself, and sets them on Fluent's brand tokens ([UI_DESIGN_SYSTEM.md §3.2](architecture/UI_DESIGN_SYSTEM.md#32-team-tinting)). Fluent's generated ramp doesn't keep the team color, and its exact mode doesn't correct text contrast. Spike S0b verified these rules on seven team colors in both modes ([D-067](DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)).

| Derived color | Rule | Used for |
|---|---|---|
| **Accent fill** | The team primary, unchanged | Primary buttons, hero bands, the scorebug block, the draft clock band, notification badge |
| **On-accent** | White or `#0B0D12`, whichever contrasts more with the fill | Text and icons on accent fill |
| **Accent** | Primary lightened (dark mode) or darkened (light mode) until **≥ 3:1** on Surface 1 | Icons, sparklines, selection marks, rating range markers, focus rings |
| **Accent text** | The same, until **≥ 4.5:1** | Accent-colored labels and links |
| **Accent soft** | Accent at 20% alpha (dark) / 12% (light) | Active nav item, selected filter pills, badges, scout-range bands |
| **Selection** | Surface 1 mixed 20% (dark) / 10% (light) toward the accent, opaque | Selected table rows (must be opaque for pinned columns) |

**Setting:** Accent → **Team** (default) / **Neutral** (`#0F6CBD`, Fluent's default brand blue), for team colors a player finds too loud.

### 3.4 Ratings, status and urgency

Rating chips are **pills**: background = the rating color at 20% alpha (dark) / 14% (light), text = the rating color. The **number is always shown**, so color is never the only signal.

| Tier | Range | Dark | Light |
|---|---|---|---|
| Elite | 90+ | `#3DD6C6` | `#0E766C` |
| Good | 80–89 | `#8ADB8F` | `#2E7334` |
| Average | 70–79 | `#E8D46A` | `#7A5F00` |
| Below | 60–69 | `#F4A65F` | `#9A4A00` |
| Poor | < 60 | `#F07B7B` | `#B42318` |

| Status | Dark | Light |
|---|---|---|
| Healthy / positive | `#6CCB7E` | `#1E7A34` |
| Questionable / warning | `#E8B44C` | `#8A5A00` |
| Out / negative | `#F07B7B` | `#B42318` |
| Injured reserve / inactive | `#A3A3A3` | `#5F6670` |
| **Urgent** (draft clock ≤ 10 s, offer ≤ 8 s, LIVE tag) | `#C62828` fill + white text | same |

> **Color-blind check (done in M0 WP8, 2026-10-19).** Machado (2009) simulation at full severity, adjacent tiers compared as CIEDE2000 ΔE (about 2 is barely noticeable, 5+ is clear):
>
> | Mode | Normal | Protanopia | Deuteranopia | Chip text contrast |
> |---|---|---|---|---|
> | Dark | 18–24 | closest Good/Average **6.9** | closest Average/Below **6.1** | 4.63–6.96:1 |
> | Light | 15–26 | closest Average/Below **3.4** | Average/Below **1.1**, Below/Poor **1.0** | 4.51–5.20:1 |
>
> **Dark holds up.** The steps are subtle but distinct for both deficiencies. **Light doesn't.** For deuteranopia, Average, Below and Poor read as one color, and the light tiers differ by only about 1 L* in lightness, so lightness is no safety net there; only the always-visible number separates them. The chip text meets 4.5:1 in both modes. **Deferred ([D-075](DECISIONS.md#d-075-color-blind-tuning-is-reviewed-before-a-public-release)):** no re-tuning during development; re-tuning the light tiers is a required item of the M11 accessibility audit, before any public or paid release.

---

## 4. Typography

| Role | Font | Settings |
|---|---|---|
| **UI and data** | **Archivo** (variable: width 62–125, weight 100–900) | Width 100%, weights 400–700, `font-variant-numeric: tabular-nums` on all numbers |
| **Display** (page titles, the draft clock, big numbers, scorebug) | **Archivo**, condensed | `font-stretch: 72%`, weight 800, tracking −0.01em |
| **Mono** (debug overlay, log viewer only) | **JetBrains Mono** | — |

- **Licenses:** Archivo (Omnibus-Type) and JetBrains Mono are both **SIL Open Font License 1.1**, free to bundle in a commercial or Steam build. Bundle them locally as `woff2` (the app is offline). Never load them from Google Fonts at runtime.
- One family for both UI and display keeps the app cohesive. The width axis gives the "athletic" condensed look without a second typeface.

**Type scale** (desktop, base 14 px)

| Token | Size / line height | Weight | Use |
|---|---|---|---|
| Display XL | 128 px / 1.0, condensed | 800 | Draft clock |
| Display L | 52–56 px / 1.0, condensed | 800 | Page hero titles ("Roster") |
| Display M | 44–48 px / 1.0, condensed | 800 | "The pick is in" player name |
| Title | 24–30 px / 1.1, condensed | 800 | Peek sheet names, selected prospect |
| Body L | 15 px / 1.4 | 400–700 | Team name in the top bar |
| Body | 14 px / 1.4 | 400–600 | Default text, table cells |
| Small | 12.5–13 px / 1.4 | 400–600 | Secondary lines, filter chips |
| Label | 12 px / 1.3 | 600 | Section and field labels (sentence case) |
| Micro | 11 px | 700, +0.04em | Position pills, badges |

---

## 5. Shape, space and elevation

| Token | Value | Applies to |
|---|---|---|
| Radius S | **8 px** | Stat tiles, small chips, list items, swatches |
| Radius M | **14 px** | Buttons, inputs, panels, tables, cards |
| Radius L | **20 px** | Hero bands, dialogs, overlays |
| Radius pill | **999 px** | Rating chips, filter pills, position tags, segmented controls, badges |
| Space | 4 px grid: 4 · 8 · 12 · 16 · 20 · 24 · 28 · 32 | Panel padding 20. Page padding 20–24. Hero padding 28 × 30. |
| Borders | 1 px Border / Border strong | Panels, inputs, row dividers. No double borders. |
| Elevation (dark) | Surface steps (Ground → S1 → S2 → S3), no shadows | — |
| Elevation (light) | Soft shadows: `0 1px 2px rgba(13,17,23,.06), 0 4px 12px rgba(13,17,23,.06)` on overlays and dialogs only | Panels stay flat with borders |

**Density** (Settings → Appearance → Density)

| | Comfortable (default) | Compact |
|---|---|---|
| Table row | 46 px | 36 px |
| Draft prospect row | 54 px | 42 px |
| Cell padding | 0 × 14 px | 0 × 10 px |

---

## 6. Signature elements

| Element | Spec |
|---|---|
| **Hero band** (page headers: Roster, Team, Draft Room clock band) | Accent fill, Radius L, padding 28 × 30, Display L title in on-accent. The team monogram is a "ghost" at 220 px in on-accent at 12% opacity, bottom-right, decorative. Segmented tabs sit on a black 22% tray, with the selected tab in an on-accent fill and team-color text. |
| **Top bar** | Surface 1, crest + team name + "season · week · record", cap space pill, search with a `Ctrl K` hint, notifications, primary action button (accent fill). |
| **Navigation** | Surface 1 column, 34 px items. The active item gets an Accent soft background, a 3 px accent indicator pill on the left, and bold text. A count badge uses Accent soft + Accent text. **As built (M0 WP8):** Fluent's nav offers 28 or 40 px items; GFO uses 28 px (Small), so the full league nav fits a 720 px window. Semibold text plus Accent soft. **Fixed after WP9 review:** Fluent sizes its indicator for 40 px items (100% − 20 px tall, 16 px outside the item), which at 28 px left it 8 px tall, off-center and in the gutter against the window edge. It's now restyled through GFO's classes to this spec: a 3 × 16 px accent pill 6 px inside the active item's left end, centered. The column has 8 px gutters, the icons sit 16 px into the pill, and the app bar's brand starts on the same 24 px line. |
| **Rating chip + range** | Pill chip (§3.4) followed by the scout range in Faint, e.g. `78  74–83`. Attribute bars use a 6 px Surface 3 track, an Accent soft band for the range, and a 3 × 10 px accent marker for the estimate. |
| **Draft clock** | Display XL condensed digits in on-accent on the hero band. At ≤ 10 s, the clock sits on an Urgent red pill. The client animates the countdown ([LIVE_EVENTS.md §3](architecture/LIVE_EVENTS.md#3-time-handling)). |
| **"The pick is in"** | Scrim `rgba(4,7,12,.8)`. Dialog with Radius L: an accent-fill header band with the title in condensed uppercase, then the player name in Display M. |
| **Scorebug** | Two team blocks (each in its own primary fill and on-color) with a condensed abbreviation and score, then a Surface 3 status block (quarter and clock). Radius M outer. |
| **Crest** | A parametric `TeamMark`: shape (Shield, Roundel, Hexagon, Diamond, Banner) in the team primary, a stripe (None, Single, Double, Chevron, Sash) in the secondary, and the monogram in the accent (or on-color) in Archivo condensed 800. Defined in [F-02 §4.4](features/F-02_LEAGUE_AND_TEAMS.md#44-crest-system-teammark). |

---

## 7. Iconography

- **Fluent System Icons** (Microsoft, MIT), which ship with Fluent UI Blazor's icons package. **Regular** weight for UI, **Filled** for the active nav item and selected states. Their rounded terminals match Stadium's geometry.
- Through GFO's `Icon` wrapper ([UI_DESIGN_SYSTEM.md §6.1](architecture/UI_DESIGN_SYSTEM.md#61-wrap-selectively-not-everything)), so the set can still be swapped.
- Sizes: 16 px (inline), 18 px (nav, toolbar), 20 px (buttons), 24 px (empty states).
- **Color:** an icon draws in the text color around it. `Icon`'s `Color` picks a role instead, each a token: Accent (§3.3, a mark at ≥ 3:1), Muted or Faint (§3.1), or a status color (§3.4: Positive, Warning, Negative). Color is never the only signal, so a status-colored icon sits next to text that says the same thing. Icons handed to Fluent parameters (`IconStart`) get the same roles through `IconCatalog.Create(…, color:)`.
- **Status:** **confirmed** by the owner after Spike S0b showed them next to Archivo ([D-067](DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)).

---

## 8. Motion

| Token | Value | Use |
|---|---|---|
| Fast | 120 ms, ease-out `cubic-bezier(.2,.8,.2,1)` | Hover, press, toggles |
| Base | 200 ms | Panels, sheets, tab changes |
| Slow | 320 ms | Dialogs, page transitions |
| Moment | 800 ms–1.5 s | "The pick is in", game final (skippable) |

`prefers-reduced-motion` and the in-app setting collapse all of these to 0 and replace moments with a static highlight.

---

## 9. Mapping to Fluent UI Blazor v5

> **Verified in Spike S0b on Fluent UI Blazor 5.0.1** ([D-067](DECISIONS.md#d-067-spike-s0b-fluent-ui-v5-fits-datagridt-keeps-fluentdatagrid)). Everything below is set on the typed `Theme` from `CreateCustomThemeAsync` before `SetThemeAsync(theme)` ([UI_DESIGN_SYSTEM.md §3.2](architecture/UI_DESIGN_SYSTEM.md#32-team-tinting)).

| GFO value | Fluent v5 mechanism |
|---|---|
| Mode: Dark default / Light / Match Windows | `ThemeSettings.Mode` = `Dark` / `Light`, always set together with the color (v5 issue #5351). **Match Windows is resolved by GFO**, which watches `prefers-color-scheme`. Fluent never sees `System`, because on an OS flip it would swap in its default theme. |
| Team accent | `ThemeSettings.Color` = team primary (or `#0F6CBD` for Neutral) with `IsExact = true`. GFO's §3.3 colors go to `Colors.Neutral.ForegroundOnBrand` (on-accent), `Colors.Brand.Foreground*` / `ForegroundLink*` (Accent text), and `Colors.Brand.Stroke*` + `Colors.Compound.BrandForeground*` / `BrandStroke*` (Accent). |
| Ground and surfaces (§3.1–3.2) | `Colors.Neutral.Background1` = Surface 1 (its `Hover` = Surface 2, `Pressed` = Surface 3), `Background2` = Surface 2, `Background3`–`5` = Ground, `CardBackground` = Surface 1. As built (WP8), every Background, card, Foreground and Stroke 1 state variant is set too, one surface step up from its base. `Foreground1`–`3` = Text / Muted / Faint. `Stroke1` = Border strong, and `Stroke2`, `Stroke3` and `StrokeSubtle` = Border. **Fluent paints `body` with Background1, so the app shell paints the page Ground itself.** |
| Archivo, JetBrains Mono | `Fonts.Family.Base` and `.Numeric` = Archivo, `.Monospace` = JetBrains Mono (bundled `woff2`, `gfo-fonts.css`). `--gfo-font-display` = Archivo, with `font-stretch: 72%` applied by GFO components. |
| Radius S / M / L / pill | `Borders.Radius.Small` = 8 px, `.Medium` = 14 px (buttons, inputs), `.Large` and `.XLarge` = 20 px, `.Circular` = 999 px |
| Ratings, status, urgent, density, moment | GFO extension tokens (`--gfo-rating-*`, `--gfo-status-*`, `--gfo-urgent`, `--gfo-grid-row-height`, `--gfo-dur-moment`) in `gfo-tokens.css`. Grid density is `DataGrid<T>`'s `ItemSize`, which is the exact row height (§5). |

---

## 10. QA checklist for every new screen

- [ ] Looks right in **Dark and Light**, and with **at least three team colors** (try dark navy, bright orange and a pale secondary-heavy team)
- [ ] Text contrast ≥ 4.5:1 (≥ 3:1 for 24 px+ and UI glyphs) in both modes
- [ ] Numbers use tabular figures. Ratings show the number next to the color.
- [ ] Rounded geometry per §5. No sharp-cornered panels or buttons.
- [ ] Team color used as **fills in a few big places**, not scattered
- [ ] Compact density still readable at 1080p
- [ ] Reduced motion respected
