# M1 · WP7 builder's guide: football components

> **Status:** ✅ **Done 2026-11-04** (written the same day; both slices built, tested and green, §9 answered as [D-094](../DECISIONS.md#d-094-the-crest-gets-a-rim-and-a-halo-and-its-monogram-falls-back-below-31-potential-grades-take-the-rating-colors-the-bar-is-a-marker), awaiting your commits, §5) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP7](M1_THE_LEAGUE.md#wp7--football-components-m) · **Size:** M
>
> WP7 builds the pieces every football screen is drawn from: `TeamMark` (the parametric crest), `PositionPill`, `RatingChip`, `RatingBar`, `PotentialChip` (the potential grade) and `Money.Format` (`$12.4M`). Each one takes a value straight from WP6's read models, so WP8's team picker and WP9's screens bind them without any glue. Ratings show a plain value: σ is 0 until M6, which adds the range display inside these components without changing their callers ([D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) #13). Two gallery pages show them in both modes and with the three sample tints. Nothing in the app changes yet: WP8 and WP9 are their first callers. When WP7 is done, `/_gallery/teams` shows all 32 crests at 16, 32 and 96 px, and `/_gallery/ratings` every tier, grade, position and money format.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | This guide; the crest geometry, drawn and rendered for all 32 teams in both modes (§8); the contrast numbers for the shipped team colors (§8). |
| **You build** (§3) | Two slices of components: ratings, positions and money; `TeamMark`. |
| **Claude builds the gallery** ✅ | `/_gallery/ratings`, `/_gallery/teams` and the DataGrid page on the real components (2026-11-04). From here on, every new component's gallery page is Claude's (your words, 2026-11-04: "Can you create the gallery and going forward do that for me"). |
| **Claude writes the tests** (§4) | Per slice, as each one lands, each proven to fail on a planted problem. |
| **Claude checks, after each slice** | The gallery page in a visible browser pane, in Dark and Light and with the three tints ([VISUAL_DESIGN.md §10](../VISUAL_DESIGN.md#10-qa-checklist-for-every-new-screen)), with screenshots for you. Then the close-out docs (§5). |

**Read first:**
- the plan's [WP7](M1_THE_LEAGUE.md#wp7--football-components-m);
- [VISUAL_DESIGN.md](../VISUAL_DESIGN.md) §3.1 (Surface 3), §3.4 (the rating colors and chips), §4 (Micro type, the display face), §6 (the rating chip, the bars, the crest) and §10 (the QA checklist);
- [UI_DESIGN_SYSTEM.md](../architecture/UI_DESIGN_SYSTEM.md) §3.4 (the extension tokens), §4 (styling), §5.4 (the football composites) and §6.2 (component conventions);
- [F-02](../features/F-02_LEAGUE_AND_TEAMS.md) §4.4 (the crest), §8, §10 and §13; [F-03](../features/F-03_PLAYERS.md) §4.5, §4.6 and §8; [F-12](../features/F-12_CONTRACTS.md) §8;
- WP6's read models: `PerceivedRating` in `Application/Players/Perception.cs`, `PotentialRange` and `PlayerRowDto` in `PlayerReadModels.cs`, `TeamIdentityDto` in `Application/Leagues/LeagueReadModels.cs`.

**AI counterpart:** none. These components only draw read models; the AI never sees a screen.

## 1. What you'll build

```
src/UI.Components/
├─ Formatting/Money.cs                Money.Format: "$12.4M", "$850K", "−$3.2M" (§3.1)
└─ wwwroot/gfo-tokens.css             + --gfo-quiet-fill (Surface 3), .gfo-tier-* and .gfo-tier-chip (§3.1)
src/UI/
├─ _Imports.razor                     + @using …UI.Football, …UI.Components.Formatting, …Application.Players,
│                                       …Application.Leagues, …Core.Players
└─ Football/
   ├─ RatingTiers.cs                  RatingTier, RatingTiers (Of, OfGrade, Token), PotentialGrades.Text (§3.1)
   ├─ RatingChip.razor                a 0–99 rating as a tier-colored pill
   ├─ PotentialChip.razor             the potential grade as a pill
   ├─ RatingBar.razor (+ .razor.css)  the number, a 6 px track and the accent marker; role="meter"
   ├─ PositionPill.razor (+ .razor.css)   the position's abbreviation on Surface 3
   ├─ CrestGeometry.cs                the five shapes, the four stripes and the monogram's size, in a 100 × 100 box (§3.2)
   └─ TeamMark.razor (+ .razor.css)   the crest: field, stripe, rim and monogram (§3.2)
src/DevHost/Components/ (Claude)
├─ _Imports.razor                     + the same usings, and …Core.IDs and …Core.Leagues for the Teams page
├─ GalleryLayout.razor                + Teams and Ratings in Pages
└─ Gallery/
   ├─ RatingsPage.razor (+ .razor.css)    /_gallery/ratings: every tier, grade, position and money format (slice 1)
   ├─ DataGridPage.razor (+ .razor.css)   the stand-in chip and money become the real components (slice 1)
   ├─ GallerySample.cs                GalleryPlayer gets a Position, a PerceivedRating and a PotentialRange; − Money, − Tier
   └─ TeamsPage.razor (+ .razor.css)  /_gallery/teams: the 32 crests by division at 16, 32 and 96 px, and on their own color (slice 2)
tests/ (Claude)                       UI.Components.Tests/Formatting, UI.Tests/Football
```

Names are suggestions: keep them unless you have a reason not to. Folder `Football/` gives the namespace `GridironFrontOffice.UI.Football` with no `@namespace` line, the way `Shell/` does.

## 2. Decisions this guide makes

D-094 settled §9's four questions. These are the smaller choices, so the code, the tests, WP8 and WP9 agree:

| Choice | Rule | Why |
|---|---|---|
| **Where things live** | The football components in `UI/Football`. `Money` in `UI.Components/Formatting` | UI.Components stays football-agnostic ([D-027](../DECISIONS.md#d-027-ui-components-are-for-this-app-only), `LayeringTests`). Money isn't football, and M4's `MoneyField` (UI_DESIGN_SYSTEM §5.2) needs the same format |
| **Components take read-model values** | `RatingChip` and `RatingBar` take a `PerceivedRating`, `PotentialChip` a `PotentialRange`, `PositionPill` a `Position`, each as `Value` (like `Stat`). `TeamMark` takes the whole `TeamIdentityDto` as `Team` | Screens bind WP6's fields as they are (`row.Overall`, `row.Potential`, `summary.RosterStrength.Overall`). M6 changes the components, not their callers |
| **A plain value until M6** | The chip and the bar show `OverallCalculator.Display(Value.Estimate)` and ignore `Sigma`. The potential chip shows `Grade` and ignores `Low` and `High`. M6 adds the range (`78  74–83`) and the spread ("likely B to A−") | D-078 #13, F-03 §8 |
| **Tier the shown number** | `RatingTiers.Of(shown)` on the displayed integer: Elite ≥ 90, Good 80–89, Average 70–79, Below 60–69, Poor < 60 | 89.5 shows as 90, so it must be teal. A chip whose color and number disagree is worse than either alone |
| **One mapping from tier to color** | gfo-tokens.css's utilities layer gains `.gfo-tier-elite` … `.gfo-tier-poor`, each setting `--gfo-tier` to its `--gfo-rating-*` token, and `.gfo-tier-chip`, the tinted pill. `RatingChip` and `PotentialChip` are markup only; `RatingBar` colors its number with the same classes | Scoped CSS doesn't cross components. The rating tokens and the `.gfo-icon-*` role classes already live there. The DataGrid page's stand-in `.chip` moves here |
| **Surface 3 gets a role name** | `--gfo-quiet-fill: var(--colorNeutralBackground1Pressed)`, a Fluent slot `StadiumTheme` always sets to Surface 3 | VISUAL_DESIGN §3.1 gives Surface 3 to position pills and tracks, and Fluent has no Surface 3 token. One role alias, like `--gfo-selection`, not a `--gfo-surface-*` system (UI_DESIGN_SYSTEM §3.4) |
| **Position pill** | The enum's name (it is the abbreviation: `EDGE`, `OG`, `LS`), in Micro type (11 px, 700, +0.04em), Muted on Surface 3. No color by side of the ball, no tooltip | VISUAL_DESIGN §3.1 and §4. Muted on Surface 3 is 7.7:1 in Dark and 8.7:1 in Light (§8) |
| **The bar** | The number in its tier color, then a 6 px Surface 3 track with a 3 × 10 px accent marker at the shown value (§9 Q4). `role="meter"`, 0–99, with an optional `Label` as its accessible name | VISUAL_DESIGN §6's attribute bar, without the range band until M6 |
| **The crest** | Field = primary; stripe = secondary, clipped to the field; a rim in the secondary; the monogram in the accent, or the on-color when the accent fails 3:1 on the primary, with a halo in the primary (§9 Q1, Q2). One drawing at every size. The geometry is `CrestGeometry`'s (§3.2) | F-02 §4.4, plus what the preview showed (§8) |
| **No contrast log** | The fallback is a drawing rule and logs nothing. F-02 §11's warning goes (§9 Q2) | It fires for 15 of the 32 shipped teams, and on every render: a rule, not a data error |
| **Accessible names** | `TeamMark`: `role="img"` with `aria-label` "City Nickname" ("New York Excelsiors"); `Decorative` makes it `aria-hidden` when the name sits beside it. `RatingBar`: a meter. The chips and the pill: their text. None takes focus or keys | F-02 §13 and UI_DESIGN_SYSTEM §6.2. Screen readers are out for 1.0 ([D-071](../DECISIONS.md#d-071-screen-readers-arent-supported-in-10)), but bUnit pins the roles, so nothing has to be retrofitted |
| **Money** | `Money.Format(long dollars)`: from $999,500, millions with one decimal (`$12.4M`, `$9.0M`, `$1,234.5M`); from $1,000, whole thousands (`$850K`); below, whole dollars (`$0`). Rounded half up on the magnitude, a real minus (U+2212) for negatives, the invariant culture. A function, not a component | F-12 §8's `$12.4M` and `$9.0M`, the plan's `$850K`. `Stat` takes a formatted string, so a component wouldn't fit the cap tiles. Nothing in a league reaches $1B (the cap is $300M), so there's no `B` |
| **SVG numbers are integers** | Every number in `TeamMark`'s markup is an `int` or a string constant | A `double` attribute can come out with a culture's decimal comma (`42,6`), which breaks the SVG on a German Windows. A test renders under de-DE (§4) |
| **The gallery reads the shipped data** | `/_gallery/teams` maps `ISeedData.TeamSet`'s teams to `TeamIdentityDto`s itself (`TeamID` = index + 1, as `LeagueWriter` numbers them). No league has to be open | WP6 §4.2 keeps `ToIdentity` private to `LeagueQueries`; ten lines in a DevHost page don't justify an Application helper |
| **No UI snapshot** | bUnit checks structure, classes and attributes; the gallery is the visual check | F-02 §13, D-078 |
| **Not in WP7** | The ghost monogram (WP9's hero band: the monogram as text, VISUAL_DESIGN §6); `ContractSummary`, `CapBar`, `PlayerCard` and the peek sheet (WP9); the team cards (WP8, built from these pieces); the range display (M6); `MoneyField` (M4); imported logos (M11, as `<img>`, F-02 §4.4) | Each arrives with its first consumer (D-078) |

## 3. The slices

### 3.1 Slice 1: ratings, positions and money

```csharp
// UI.Components/Formatting/Money.cs
namespace GridironFrontOffice.UI.Components.Formatting;

public static class Money
{
    private const string Minus = "−";

    /// <summary>
    /// Dollars as screens show them (F-12 §8): "$12.4M" from $999,500, "$850K" from $1,000, "$0" below, "−$3.2M" when
    /// negative. Rounded half up on the magnitude, in integers, the same in every culture.
    /// </summary>
    public static string Format(long dollars)
    {
        var sign = dollars < 0 ? Minus : "";
        var size = Math.Abs(dollars);

        if (size >= 999_500)
        {
            var tenths = (size + 50_000) / 100_000;
            return string.Create(CultureInfo.InvariantCulture, $"{sign}${tenths / 10:#,0}.{tenths % 10}M");
        }

        return size >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{sign}${(size + 500) / 1_000}K")
            : string.Create(CultureInfo.InvariantCulture, $"{sign}${size}");
    }
}
```

| Dollars | Shows | | Dollars | Shows |
|---|---|---|---|---|
| 0 | `$0` | | 1,049,999 | `$1.0M` |
| 999 | `$999` | | 1,050,000 | `$1.1M` |
| 1,000 | `$1K` | | 9,000,000 | `$9.0M` |
| 850,000 | `$850K` | | 12,450,000 | `$12.5M` |
| 999,499 | `$999K` | | 300,000,000 | `$300.0M` |
| 999,500 | `$1.0M` | | −3,200,000 | `−$3.2M` |

```css
/* UI.Components/wwwroot/gfo-tokens.css, in :root (gfo-tokens layer) */
--gfo-quiet-fill: var(--colorNeutralBackground1Pressed);   /* Surface 3: position pills, rating tracks, quiet chips */

/* … and in the utilities layer: one tier class sets --gfo-tier; chips, bars and grades read it (VISUAL_DESIGN.md §3.4). */
.gfo-tier-elite { --gfo-tier: var(--gfo-rating-elite); }
.gfo-tier-good  { --gfo-tier: var(--gfo-rating-good); }
.gfo-tier-avg   { --gfo-tier: var(--gfo-rating-avg); }
.gfo-tier-below { --gfo-tier: var(--gfo-rating-below); }
.gfo-tier-poor  { --gfo-tier: var(--gfo-rating-poor); }

/* The rating pill: the tier color as text on a 20% (Dark) / 14% (Light) tint of itself. */
.gfo-tier-chip {
  display: inline-block;
  min-width: 2.2em;
  padding: 1px 8px;
  border-radius: var(--gfo-radius-pill);
  background: color-mix(in srgb, var(--gfo-tier) var(--gfo-chip-tint), transparent);
  color: var(--gfo-tier);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  text-align: center;
}
```

```csharp
// UI/Football/RatingTiers.cs
namespace GridironFrontOffice.UI.Football;

/// <summary>The five rating colors (VISUAL_DESIGN.md §3.4), worst to best.</summary>
public enum RatingTier { Poor, Below, Average, Good, Elite }

public static class RatingTiers
{
    /// <summary>The tier of a shown 0–99 rating. Tier the number the screen shows, never the raw estimate.</summary>
    public static RatingTier Of(int shown) => shown switch
    {
        >= 90 => RatingTier.Elite,
        >= 80 => RatingTier.Good,
        >= 70 => RatingTier.Average,
        >= 60 => RatingTier.Below,
        _ => RatingTier.Poor,
    };

    /// <summary>A potential grade on the rating scale, by its letter (§9 Q3): A elite, B good, C average, D below, F poor.</summary>
    public static RatingTier OfGrade(PotentialGrade grade) => grade switch
    {
        >= PotentialGrade.AMinus => RatingTier.Elite,
        >= PotentialGrade.BMinus => RatingTier.Good,
        >= PotentialGrade.CMinus => RatingTier.Average,
        PotentialGrade.D => RatingTier.Below,
        _ => RatingTier.Poor,
    };

    /// <summary>The class suffix in gfo-tokens.css: gfo-tier-elite … gfo-tier-poor.</summary>
    public static string Token(this RatingTier tier) => tier switch
    {
        RatingTier.Elite => "elite",
        RatingTier.Good => "good",
        RatingTier.Average => "avg",
        RatingTier.Below => "below",
        _ => "poor",
    };
}

public static class PotentialGrades
{
    /// <summary>The display map PotentialGrade's summary promises: "A+", "A", "A−" … "D", "F", with a real minus (U+2212).</summary>
    public static string Text(this PotentialGrade grade) => grade switch
    {
        PotentialGrade.APlus => "A+",
        PotentialGrade.A => "A",
        PotentialGrade.AMinus => "A−",
        // … B+, B, B−, C+, C, C−
        PotentialGrade.D => "D",
        _ => "F",
    };
}
```

`Of(int)` and `OfGrade(…)` have different names on purpose: a literal `0` converts to any enum, so two `Of` overloads would read ambiguously.

```razor
@* UI/Football/RatingChip.razor: a 0–99 rating as a pill (VISUAL_DESIGN.md §3.4, §6). A plain value while σ = 0;
   M6 adds the scout range after it. *@
<span class="gfo-tier-chip gfo-tier-@_tier.Token() @Class" @attributes="AdditionalAttributes">@_shown</span>

@code {
    [Parameter, EditorRequired] public PerceivedRating Value { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private int _shown;
    private RatingTier _tier;

    protected override void OnParametersSet()
    {
        _shown = OverallCalculator.Display(Value.Estimate);   // Sigma is shown from M6 (D-078 #13)
        _tier = RatingTiers.Of(_shown);
    }
}
```

`PotentialChip` is the same span with `Value.Grade.Text()` and `RatingTiers.OfGrade(Value.Grade)`. `PositionPill` is `<span class="gfo-position-pill @Class" @attributes="AdditionalAttributes">@Value</span>`, with its scoped CSS:

```css
/* UI/Football/PositionPill.razor.css */
.gfo-position-pill {
  display: inline-block;
  padding: 2px 8px;
  border-radius: var(--gfo-radius-pill);
  background: var(--gfo-quiet-fill);
  color: var(--colorNeutralForeground2);   /* Muted */
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
  line-height: 1.3;
}
```

```razor
@* UI/Football/RatingBar.razor: an attribute's number and where it sits on 0–99 (VISUAL_DESIGN.md §6). M6 adds the
   range band (Accent soft) under the marker. *@
<span class="gfo-rating-bar @Class" role="meter" aria-valuemin="0" aria-valuemax="99" aria-valuenow="@_shown"
      aria-label="@Label" @attributes="AdditionalAttributes">
    <span class="value gfo-num gfo-tier-@_tier.Token()">@_shown</span>
    <span class="track" aria-hidden="true"><span class="marker" style="--at: @_shown"></span></span>
</span>

@code {
    [Parameter, EditorRequired] public PerceivedRating Value { get; set; }

    /// <summary>The attribute's name, for assistive technology; the page shows it beside the bar.</summary>
    [Parameter] public string? Label { get; set; }
    // Class, AdditionalAttributes and OnParametersSet as in RatingChip
}
```

```css
/* UI/Football/RatingBar.razor.css */
.gfo-rating-bar { display: flex; align-items: center; gap: 10px; }
.value { min-width: 2ch; text-align: right; font-weight: 700; color: var(--gfo-tier); }
.track { position: relative; flex: 1; height: 6px; border-radius: var(--gfo-radius-pill); background: var(--gfo-quiet-fill); }
.marker {
  position: absolute; top: 50%; left: calc(var(--at) * 1%);
  width: 3px; height: 10px; border-radius: 2px;
  background: var(--gfo-accent);
  transform: translate(-50%, -50%);
}
```

**`/_gallery/ratings`** (`RatingsPage.razor`, built by Claude), one `GallerySection` each:
- **`RatingChip`, every tier and its edges:** 0, 45, 59, 59.5 (shows 60), 60, 69, 70, 79, 80, 89, 89.5 (shows 90), 90, 99, 100 (shows 99), each as `new PerceivedRating(value, 0)`. Check: the number and the color agree at every edge, and the text holds 4.5:1 in both modes.
- **`RatingBar`:** the same values with labels, at a fixed width. Check: the marker sits at the value in the accent, and the number takes its tier's color.
- **`PotentialChip`:** all 11 grades, best first, as `new PotentialRange(grade, grade, grade)`.
- **`PositionPill`:** all 16 positions, from `Enum.GetValues<Position>()`.
- **Money:** the table above, amount beside `Money.Format(amount)`, in tabular figures.

**The DataGrid page** (Claude) gets the real pieces: `GalleryPlayer.Position` becomes a `Position` (drop the gallery's `"IOL"`), `Overall` a `PerceivedRating`, `Potential` a `PotentialRange`. The Pos, OVR and POT columns become template columns with `PositionPill`, `RatingChip` and `PotentialChip`, and Cap hit calls `Money.Format`. The fake `OverallLow–OverallHigh` range span stays: it previews M6. `GallerySample.Money` and `Tier`, and the page's `.chip` rule, go. Sort OVR by `p => p.Overall.Estimate` (a `PerceivedRating` doesn't compare, WP6 §8) and POT by `p => p.Potential.Grade`. The Theme page's chip row stays as it is: it shows the raw tokens.

### 3.2 Slice 2: `TeamMark`

```csharp
// UI/Football/CrestGeometry.cs
/// <summary>
/// The crest in a 100 × 100 box (F-02 §4.4). Every number is an int, so no culture can put a comma into the SVG.
/// Drawn and checked in Claude's preview (guide §8); tune it by eye on /_gallery/teams.
/// </summary>
public static class CrestGeometry
{
    /// <summary>The field: the shape's outline, also its clip path and its rim.</summary>
    public static string Field(CrestShape shape) => shape switch
    {
        CrestShape.Shield => "M50 4 L90 13 V46 C90 70 73 86 50 96 C27 86 10 70 10 46 V13 Z",
        CrestShape.Roundel => "M50 4 A46 46 0 1 1 50 96 A46 46 0 1 1 50 4 Z",
        CrestShape.Hexagon => "M50 4 L90 27 V73 L50 96 L10 73 V27 Z",
        CrestShape.Diamond => "M50 3 L97 50 L50 97 L3 50 Z",
        CrestShape.Banner => "M12 4 H88 V94 L50 78 L12 94 Z",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    /// <summary>The stripe, drawn past the edges and clipped to the field; null for None.</summary>
    public static string? Stripe(CrestStripe stripe) => stripe switch
    {
        CrestStripe.None => null,
        CrestStripe.Bar => "M42 0 H58 V100 H42 Z",
        CrestStripe.TwinBars => "M28 0 H37 V100 H28 Z M63 0 H72 V100 H63 Z",
        CrestStripe.Chevron => "M0 60 L50 84 L100 60 V73 L50 97 L0 73 Z",
        CrestStripe.Sash => "M0 -12 L112 100 L100 112 L-12 0 Z",
        _ => throw new ArgumentOutOfRangeException(nameof(stripe), stripe, null),
    };

    /// <summary>The monogram's font size: 52, 42 or 31 for one, two or three letters, and about 82% of that in a diamond.</summary>
    public static int MonogramSize(CrestShape shape, int letters) => (shape, Math.Clamp(letters, 1, 3)) switch
    {
        (CrestShape.Diamond, 1) => 43,
        (CrestShape.Diamond, 2) => 34,
        (CrestShape.Diamond, _) => 25,
        (_, 1) => 52,
        (_, 2) => 42,
        _ => 31,
    };

    /// <summary>The monogram's vertical center: a shield's and a banner's sit above the middle of the box.</summary>
    public static int MonogramY(CrestShape shape) => shape switch
    {
        CrestShape.Shield => 47,
        CrestShape.Banner => 44,
        _ => 50,
    };
}
```

```razor
@* UI/Football/TeamMark.razor: the team crest (F-02 §4.4), inline SVG from the identity, no image requests. The colors
   are the team's own data, so they go in as presentation attributes; nothing here is a theme token. *@
<svg class="gfo-team-mark @Class" width="@Size" height="@Size" viewBox="0 0 100 100" focusable="false"
     role="@(Decorative ? null : "img")" aria-label="@(Decorative ? null : _name)" aria-hidden="@(Decorative ? "true" : null)"
     data-shape="@Team.CrestShape" data-stripe="@Team.CrestStripe" @attributes="AdditionalAttributes">
    <defs><clipPath id="@_clipID"><path d="@_field" /></clipPath></defs>
    <g clip-path="url(#@_clipID)">
        <path class="field" d="@_field" fill="@Team.PrimaryColor" />
        @if (_stripe is not null)
        {
            <path class="stripe" d="@_stripe" fill="@Team.SecondaryColor" />
        }
        @* Stroked at 8 on the outline and clipped to it, so 4 show, inside the edge. *@
        <path class="rim" d="@_field" fill="none" stroke="@Team.SecondaryColor" stroke-width="8" />
    </g>
    @* The halo is the field's color under each letter (paint-order), so the monogram reads over its stripe. *@
    <text class="monogram gfo-display" x="50" y="@_centerY" font-size="@_fontSize" text-anchor="middle"
          dominant-baseline="central" fill="@_monogram" stroke="@Team.PrimaryColor" stroke-width="7"
          stroke-linejoin="round" paint-order="stroke">@Team.CrestMonogram</text>
</svg>

@code {
    /// <summary>The accent stays while it holds 3:1 on the field (a glyph, VISUAL_DESIGN.md §10; §9 Q2).</summary>
    internal const double MonogramContrast = 3.0;

    // Each crest owns its clip path: 32 on one page must never share an id (TitleBackground's marker, the same way).
    private readonly string _clipID = $"gfo-crest-{Guid.NewGuid():N}"[..20];

    [Parameter, EditorRequired] public TeamIdentityDto Team { get; set; } = default!;

    /// <summary>Width and height in px: 16–20 in grids, 32–48 on cards (F-02 §4.4).</summary>
    [Parameter] public int Size { get; set; } = 32;

    /// <summary>Hidden from assistive technology, for a crest beside its team's name.</summary>
    [Parameter] public bool Decorative { get; set; }
    // Class and AdditionalAttributes as in RatingChip; _field, _stripe, _fontSize, _centerY, _monogram and _name are
    // private fields

    protected override void OnParametersSet()
    {
        _field = CrestGeometry.Field(Team.CrestShape);
        _stripe = CrestGeometry.Stripe(Team.CrestStripe);
        _fontSize = CrestGeometry.MonogramSize(Team.CrestShape, Team.CrestMonogram.Length);
        _centerY = CrestGeometry.MonogramY(Team.CrestShape);
        _monogram = AccentColors.Contrast(Team.AccentColor, Team.PrimaryColor) >= MonogramContrast
            ? Team.AccentColor
            : AccentColors.OnAccent(Team.PrimaryColor);   // white or #0B0D12, as on an accent fill
        _name = $"{Team.City} {Team.Nickname}";
    }
}
```

`TeamMark.razor.css` holds one rule, `.gfo-team-mark { flex: none; vertical-align: middle; }`, so a crest neither shrinks in a flex row nor sits on the text baseline. The math runs in `OnParametersSet`, once per change, not on every render: the League page draws 32 crests (F-02 §12's < 16 ms is read, not asserted).

**`/_gallery/teams`** (`TeamsPage.razor`, built by Claude, `@inject ISeedData Seed`): one `GallerySection` per division, conference by conference in the team set's order, so a look-alike within a division shows (F-02 §4.3, §10). Each team's row: the crest at 96, 32 and 16 px; "City Nickname"; the accent's ratio on the primary, and "on-color" when it falls back; and the crest at 48 px on a band of its own primary (`style="--gfo-team-primary: …"` on the band, `background: var(--gfo-team-primary)` in the page's CSS), the way WP9's hero band shows it. Map each `TeamDefinition` with:

```csharp
private static TeamIdentityDto Identity(TeamDefinition t, int index) => new(new TeamID(index + 1), t.Abbr, t.City,
    t.Nickname, t.Colors.Primary, t.Colors.Secondary, t.Colors.Accent, t.Crest.Shape, t.Crest.Monogram, t.Crest.Stripe);
```

Check, in both modes and with each tint: every crest's field, stripe, rim and monogram; a monogram readable at 32 and 96 px and each crest recognizable at 16; no two in a division share a shape and a stripe; and on its own color, the rim holds the edge.

## 4. The tests (Claude writes them)

Per slice, each proven to fail on a planted problem. bUnit runs no CSS, so the tests check markup, classes and attributes; the look is the gallery's. ✅ marks a test that's written and passing (2026-11-04): 151 in all, 23 in UI.Components.Tests and 128 in UI.Tests.

| Slice | Test | Checks |
|---|---|---|
| **1** | ✅ `MoneyTests` (UI.Components.Tests/Formatting, 23) | §3.1's table, both sides of each rounding edge (999 / 1,000, 1,499 / 1,500, 999,499 / 999,500, 1,049,999 / 1,050,000, 12,449,999 / 12,450,000, 999,949,999 / 999,950,000), thousands separators past $999.9M, negatives with U+2212 (never `-`) rounding like their magnitude, and the same text under de-DE |
| | ✅ `RatingTiersTests` (UI.Tests/Football, 28) | `Of` at 0, 59, 60, 69, 70, 79, 80, 89, 90 and 99; `OfGrade` and `Text` for all 11 grades (U+2212 in the minuses, no two texts alike); `Token` for all five; and gfo-tokens.css defines `.gfo-tier-*` and `--gfo-rating-*` for every token, `.gfo-tier-chip` and `--gfo-quiet-fill` |
| | ✅ `RatingChipTests` (`ChipTests.cs`, 11) | The shown number is `OverallCalculator.Display`'s (78.4 → 78, 59.5 → 60, 89.49 → 89, 89.5 → 90 and Elite, 99.6 → 99, −0.4 → 0), with exactly one tier class; a sigma changes nothing until M6; one span, `Class` and extra attributes passed through |
| | ✅ `PotentialChipTests` (`ChipTests.cs`, 12) | Each grade's text and tier class; the spread (`Low`, `High`) waits for M6 |
| | ✅ `RatingBarTests` (8) | `role="meter"` with 0, 99 and the shown value; the label as `aria-label`, none without one; the number, the marker's `--at` and `aria-valuenow` all the shown value at the edges (0, 59.5, 74, 89.5, 100); the track hidden from assistive technology |
| | ✅ `PositionPillTests` (`ChipTests.cs`, 18) | All 16 positions show their abbreviation (FB and `EDGE` included), and a new position needs a row |
| **2** | ✅ `CrestGeometryTests` (6) | Five distinct closed fields; None is null and the other four stripes distinct; the monogram shrinks with each letter, 0 and 4 letters clamp, a diamond's is the smallest; the shield's and banner's centers above the middle; unknown values throw. No path data is pinned |
| | ✅ `TeamMarkTests` (45) | Each of the 25 shape × stripe pairs: the field and the clip path share the shape's outline, one stripe (none for None), the rim; primary, secondary and accent on the right parts; the monogram's text, size and center; `role="img"` with "New York Excelsiors", or `aria-hidden` and no name when `Decorative`; `Size` sets width and height; two crests own different clip ids, each used by its own group; the fallback on shipped teams on both sides of 3:1 (GB 1.06 and SF 2.98 → white, TPA 2.06 → `#0B0D12`, PHX 3.32 and DAL 14.50 keep their accents); all 32 shipped teams (through `RealApp.Data`) render with their own colors and names; no attribute holds a comma under de-DE |
| **All** | Every existing test | Still green, the architecture rules included (`Money` keeps UI.Components free of Core) |

**The plants (2026-11-04): all 22 caught,** each in a scratch copy, one at a time, with the tests green again after each. Money (4): rounded down (6 of 23 fail), a hyphen for the minus (5), millions only from $1,000,000 (1), the current culture (1). Tiers and chips (8): Elite from above 90; B+ and B crossed; D colored Poor; Average's class as `gfo-tier-average` (the gfo-tokens.css check fails too); the chip tiering the raw estimate (4 of 11); the pill missing FB (the bug the Ratings page found); the meter and the marker at the raw estimate (4 and 3 of 8). Geometry (2): a diamond's monogram as big as a shield's; the length not clamped. `TeamMark` (8): one shared clip id; the fallback at 4.5:1 (6 of 45) or never (4); no halo (25); the rim in the primary (26); the name reversed; a decorative crest keeping its role; and a fractional `double` font size, which the de-DE test caught because Blazor wrote it with a decimal comma.

## 5. Done when

**Slice 1: ratings, positions and money**
- [x] `Money.Format`; `--gfo-quiet-fill`, `.gfo-tier-*` and `.gfo-tier-chip` in gfo-tokens.css.
- [x] `RatingTier`, `RatingTiers` and `PotentialGrades`; `RatingChip`, `PotentialChip`, `RatingBar` and `PositionPill` (its map, which missed FB, replaced by `@Value` at your request, 2026-11-04).
- [x] Claude, 2026-11-04: `/_gallery/ratings`, the DevHost's usings, and the DataGrid page on the real pieces, with `GallerySample.Money`, `Tier` and the `.chip` rule gone.
- [x] Claude's slice 1 tests pass (100: Money, the tiers, the chips, the bar and the pill), each proven to fail on a planted problem (§4).
- [x] The Ratings and DataGrid pages checked in a visible pane, both modes, every tint, Comfortable and Compact (Claude, 2026-11-04): every chip, bar number and pill holds 4.51:1 or better on Surface 1 (§8). Two DataGrid columns clipped and Claude widened them (Pos 72 → 88 px for EDGE, POT 64 → 76 px for A+).
- [x] The chip's tint mixed toward Surface 1, not `transparent`, in gfo-tokens.css (Claude, at your request, 2026-11-04): on a selected row the chips had fallen to 3.17:1 (§7, §8).

**Slice 2: `TeamMark`**
- [x] `CrestGeometry` and `TeamMark` (+ its one CSS rule).
- [x] Claude, 2026-11-04: `/_gallery/teams`, linked from `GalleryLayout.Pages`, with Ratings.
- [x] Claude's slice 2 tests pass (51: the geometry and `TeamMark`, the 32 shipped teams included), each proven to fail on a planted problem (§4).
- [x] The Teams page checked in a visible pane (Claude, 2026-11-04): Dark with the neutral and navy tints, Light with the orange and gold ones, Comfortable and Compact; all 128 crests render, each with its own clip id.

**Close-out (Claude)**
- [x] Every §4 test passes, each proven to fail on a planted problem; `./build.ps1` green (2026-11-04: no warnings, 900 tests). Green again after the chip fix and the docs (no warnings, 900 tests).
- [x] Reviewed by Claude against this guide, F-02 §4.4, F-03 §8, F-12 §8, VISUAL_DESIGN §3–§6 and §10, and D-094 (2026-11-04). Two small things remain in `TeamMark.razor`, both yours: eight lines (the `Class` parameter and the private fields) indent with tabs in a file, like the repository's other Razor files, indented with spaces; and `MonogramContrast`'s comment points at "§9 Q2", now D-094 #2.
- [x] Docs (Claude, 2026-11-04): the plan's WP7 results; UI_DESIGN_SYSTEM §3.4 (the alias and the utilities) and §5.4 (as built: the six pieces, their parameters and roles); VISUAL_DESIGN §6's crest row (the rim, the halo, the 3:1 fallback) and §3.1 (`--gfo-quiet-fill`); F-02 §4.4, §11 (the warning dropped) and §13; F-03 §8 (`PotentialChip` and the grade colors); F-12 §8 (`Money.Format`) and §13 (the contract block and the cap tiles are WP9's, not WP7's); CLAUDE.md's gotchas (§7's first four).
- [ ] Committed by you: one commit per slice, each with its tests (D-076), then the docs.

## 6. Order of work

1. **Slice 1.** The tokens and utilities first (everything else reads them), then `Money` and `RatingTiers` (pure), then the four components. Claude builds the Ratings page and the DataGrid page on them. Restart the DevHost watcher after new files arrive: it only knows files that existed when it started.
2. **Slice 2.** `CrestGeometry`, then `TeamMark`; Claude builds the Teams page. Tune the geometry there by eye, if you want to; the tests don't pin path data, only structure.
3. **Close-out:** the docs, then your commits.

## 7. Gotchas

- **`var()` never resolves in SVG presentation attributes** (`TitleBackground`'s note). `TeamMark`'s colors are literal hex from the read model, so attributes work. Anything token-colored in an SVG goes through its scoped stylesheet.
- **Keep every SVG number an `int`.** A `double` in an attribute is written with the culture's decimal comma (a plant proved it, §4). `CrestGeometry`'s sizes and centers are ints, and `RatingBar`'s `--at` is the shown integer.
- **Each `TeamMark` owns its clip path.** Two crests sharing an id clip each other's stripe. `url(#@_clipID)` works in Razor, as `TitleBackground`'s `marker-end` does.
- **Scoped CSS doesn't cross components,** so the tier colors and the pill live in gfo-tokens.css's utilities (`.gfo-tier-*`, `.gfo-tier-chip`). A tier class on an element inside a Fluent component still works, since it's global.
- **Tier the shown number,** never the estimate: `RatingTiers.Of(OverallCalculator.Display(x))`. 89.5 is a 90.
- **`PerceivedRating` doesn't compare** (WP6 §8): sort a grid column by `.Estimate`, a grade by `.Grade` (an enum, which does).
- **The halo needs `paint-order="stroke"`.** Without it the stroke is painted over the letters and eats half of each one.
- **The rim is half clipped:** the outline stroked at 8 inside the clip shows 4. Don't move it outside the clipped group, or it spills past the shape.
- **A see-through chip takes its row's color.** The chips' 4.5:1 holds on Surface 1 (VISUAL_DESIGN §3.4: 4.51–6.96:1), but with the tint mixed toward `transparent`, a selected row's `--gfo-selection` showed through and the text dropped to 3.17:1 (§8). The tint now mixes toward `var(--colorNeutralBackground1)`, so the chip reads the same on any row. Keep anything tinted that carries text opaque.
- **`[EditorRequired]` is a build error in markup only.** A missing `Value` in Razor is RZ2012, an error under warnings-as-errors; bUnit's parameter builder doesn't check it, so a test that forgets one renders a 0.
- **Verify in a visible pane** (CLAUDE.md): the DataGrid page virtualizes, and a hidden pane stalls it.
- **`Position`'s names are its abbreviations** (WP1: `EDGE` upper case), so the pill needs no display map. `PotentialGrade`'s aren't (`BPlus`): use `Text()`.

## 8. What Claude checked (2026-11-04)

| Question | Answer |
|---|---|
| How many shipped accents hold up on their own primary? | 17 of 32 at 3:1, 10 of 32 at 4.5:1 (WCAG contrast, `AccentColors`' formula). Below 3:1, so on the on-color: GB 1.06, KC 1.21, CIN 1.22, DEN 1.40, NYS 1.98, WAS 2.02, SEA 2.03, TPA 2.06, JAX 2.15, LAC 2.34, BUF 2.47, LAN 2.53, NSH 2.68, CHA 2.76, SF 2.98. The nearest above is PHX at 3.32. The on-color is `#0B0D12` for TPA, LAC and CHA and white for the other 12, and it holds 4.5:1 on every primary (F-02's `Text_on_every_primary_is_readable`) |
| Does the drawing hold up? | Yes. All 32, at 96, 32 and 16 px, on Surface 1 in Dark and Light, rendered in headless Edge (Chromium, the engine WebView2 uses) with the bundled Archivo at 72% width and 800, from §3.2's geometry. Monograms read from 32 px; at 16 px a crest reads by shape and color, and grids put the abbreviation beside it (F-02 §10) |
| What do the rim and the halo buy? | Without the halo, a monogram on its own stripe vanishes: JAX's white J on its pale bar, NYS's S, LAN's N, PHI's P, TPA's dark T on its green bar. Without the rim, dark crests (JAX, NYX) lose their edge on Dark's Surface 1, and TPA's gold on Light's white. With both, all eight read in both modes |
| Is the pill's text readable? | Muted on Surface 3: 7.70:1 in Dark, 8.66:1 in Light |
| Do the tests have the data they need? | Yes: `RealApp.Data` gives UI.Tests the shipped `ISeedData` (D-093), and `EmbeddedSeedData` itself stays internal |
| Do the pieces hold up in the gallery? | On Surface 1, yes: in both modes with all four tints, the chips hold 4.51–7.00:1, the bar's numbers 5.49–11.46:1 and the pills 7.70 (Dark) and 8.66:1 (Light), measured in the DevHost from the computed colors |
| Do the chips hold up on a selected row? | They didn't: with the tint mixed toward `transparent`, the selection showed through. The worst cases were 3.17:1 (Dark, gold, Poor) and 3.80:1 (Light, navy, Elite), and 21 of the 40 tier and look pairs fell below 4.5. Mixed toward `var(--colorNeutralBackground1)` (gfo-tokens.css since 2026-11-04), every tier on a selected row and on a plain one, in all eight looks, is 4.51–6.94:1 (80 of 80, measured from the file) |

The renders went to you with this guide; the Teams page replaces them once it's built.

## 9. Questions (answered 2026-11-04)

All four as recommended ([D-094](../DECISIONS.md#d-094-the-crest-gets-a-rim-and-a-halo-and-its-monogram-falls-back-below-31-potential-grades-take-the-rating-colors-the-bar-is-a-marker), your words: "Yes, all four"):
1. ~~The crest's rim and halo?~~ → **Both:** a 4-unit rim in the secondary and the monogram outlined in the primary.
2. ~~When does the monogram give up its accent?~~ → **Below 3:1 on the primary:** the on-color for 15 of the 32, and F-02 §11's warning goes.
3. ~~Potential grade colors?~~ → **By letter on the rating scale,** the same chip as a rating.
4. ~~`RatingBar` while σ = 0?~~ → **VISUAL_DESIGN §6's bar:** the number, the track and the marker, no fill.

The questions as they were asked:

1. **The crest's rim and halo** (F-02 §4.4 names three layers: field, stripe, monogram):
   - **A. Both, as previewed (recommended):** a 4-unit rim in the secondary inside the edge, and the monogram outlined in the primary. Every shipped crest reads in both modes.
   - B. The halo only: monograms read over their stripes, but dark crests lose their edge on Dark's surfaces.
   - C. Neither, F-02 as written: five monograms vanish into their own stripes (JAX, NYS, LAN, PHI, TPA).
2. **When does the monogram give up its accent?**
   - **A. Below 3:1 on the primary (recommended):** the on-color for 15 of the 32 shipped teams. A glyph's bar is 3:1 (VISUAL_DESIGN §10), and F-02 §11's warning goes: a rule that fires for half the league isn't a data error.
   - B. Below 4.5:1: the on-color for 22 of 32. Safer for the 16 px marks, and fewer accents seen.
   - C. Never: change the 15 failing accents in `teams.json` so every crest keeps its own, with a guardrail test. Claude proposes the colors; it reopens the set you approved in D-064.
3. **Potential grade colors:**
   - **A. By letter on the rating scale (recommended):** A grades teal (Elite), B green, C yellow, D orange, F red, the same chip as a rating. The letter is always shown, so color is never the only signal.
   - B. A neutral pill (Muted on Surface 3, like a position): color stays for ratings alone, and a potential reads as quieter than a rating.
4. **`RatingBar` while σ = 0:**
   - **A. VISUAL_DESIGN §6's bar (recommended):** the number in its tier color, a 6 px Surface 3 track and a 3 × 10 px accent marker at the value. M6 adds the range band under the marker.
   - B. Also fill the track up to the value, in the tier color: 42 bars on the Player page read as a profile at a glance, and the fill makes way for the range band in M6.
