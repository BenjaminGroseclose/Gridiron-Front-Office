# M1 · WP8 builder's guide: the New League wizard v0 and team tint

> **Status:** **Ready to build** ([F-01's M1 spec](../features/F-01_LEAGUE_CREATION.md) accepted 2026-11-05 as [D-096](../DECISIONS.md#d-096-f-01s-m1-slice-accepted); this guide written 2026-11-04, §10 answered the same day as [D-095](../DECISIONS.md#d-095-accent-states-move-away-from-their-text-the-picker-can-start-over-a-team-is-selected-then-taken-the-desktop-end-to-end-is-a-ui-flow-plus-a-check)) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP8](M1_THE_LEAGUE.md#wp8--new-league-wizard-v0-and-team-tint-m) · **Size:** M
>
> WP8 is the first time you can start a real league and play as a team. M0's name-only New League page becomes a two-step wizard. Step one takes a name and an optional seed, then **Generate** runs WP5's `CreateLeague` with progress. Step two shows all 32 teams as cards (crest, roster strength, cap space) from WP6's summaries, drawn with WP7's components. A click marks a card, and **Take over** runs WP5's `PickUserTeamAsync` and opens the league at its home. From then on the app wears the team's primary color through M0's `GfoThemeService`, and the load screen and Continue name each league's team, season and phase. Nothing in Core or the schema changes. When WP8 is done, the M1 plan's item 1 works end to end, and the exit's "the app wears the user team's colors in both modes, with readable text" holds for all 32 teams.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | This guide; [F-01's M1 spec](../features/F-01_LEAGUE_CREATION.md), owed since WP0 (accepted, [D-096](../DECISIONS.md#d-096-f-01s-m1-slice-accepted)); and the contrast of all 32 team colors on Fluent's button states in both modes, which found 7 teams whose hovered, pressed and selected buttons can't be read (§8). |
| **You build** (§3) | Three slices: the wizard; team tint, with the button states fixed; the load screen and Continue. |
| **Claude builds the gallery** | The team colors in the gallery's toolbar, `/_gallery/team-picker` (the 32 cards) and `/_gallery/save-cards` (every card state, with and without a team), and the button states on the Theme page (§3.4). |
| **Claude writes the tests** (§4) | Per slice, as each one lands, each proven to fail on a planted problem. |
| **Claude checks, after each slice** | The wizard end to end in a visible browser pane, in Dark and Light with several team colors ([VISUAL_DESIGN.md §10](../VISUAL_DESIGN.md#10-qa-checklist-for-every-new-screen)), then in the desktop app through WebView2's DevTools port (D-095 #4), with screenshots for you. Then the close-out docs (§5). |

**Read first:**
- [F-01's M1 spec](../features/F-01_LEAGUE_CREATION.md) (new: the rules, the screens and the edge cases) and the plan's [WP8](M1_THE_LEAGUE.md#wp8--new-league-wizard-v0-and-team-tint-m);
- the [WP5 guide](M1_WP5_GUIDE.md) §2 and §4.2–§4.3 (`CreateLeague`, its progress and cancel, the pick, the `created` snapshot);
- WP6's read models: `LeagueHeaderDto` and `TeamSummaryDto` in `Application/Leagues/LeagueReadModels.cs`;
- WP7's components: `TeamMark`, `RatingChip` and `Money.Format` ([UI_DESIGN_SYSTEM.md §5.4](../architecture/UI_DESIGN_SYSTEM.md#54-football-composites-in-gridironfrontofficeui));
- [UI_DESIGN_SYSTEM.md](../architecture/UI_DESIGN_SYSTEM.md) §3.2 (team tinting) and §7 (the app shell);
- [VISUAL_DESIGN.md §3.3](../VISUAL_DESIGN.md#33-team-accent-the-users-franchise-colors) (the derived accent colors);
- [PERSISTENCE.md §9](../architecture/PERSISTENCE.md#9-save-catalog-load-screen) (the load screen's cards).

**AI counterpart:** none. Every team is generated the same way, and the picker shows ratings as an outsider sees them ([D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) #1), the view any rival GM has.

## 1. What you'll build

```
src/Application/
├─ ApplicationServices.cs              + LeagueGenerator, CreateLeague, LeagueCommands (their first consumer, WP5 §2)
├─ Leagues/LeagueReadModels.cs         LeagueHeaderDto + Seed (the picker shows it)
├─ Leagues/LeagueQueries.cs            GetHeaderAsync reads the seed
├─ Saves/SaveRules.cs                  + CheckLeagueName, moved out of SaveManager (§3.1)
└─ Saves/ISaveCatalog.cs               SaveCard + Team, Season, Phase (§3.3)
src/Infrastructure/Saves/
├─ SaveManager.cs                      CreateAsync calls SaveRules.CheckLeagueName
└─ SaveCatalog.cs                      + the league's position and team, for a Ready card (§3.3)
src/UI.Components/Theming/
├─ AccentColors.cs                     + StateFills: hover, pressed and selected, away from the text (§3.2)
└─ StadiumTheme.cs                     sets Fluent's three brand state slots from them
src/UI/
├─ _Imports.razor                      + @using …UI.Football, …Application.Leagues (WP7's files bring their own)
├─ Menu/NewLeaguePage.razor (+ .css)   step one: the name, the seed, Generate with progress (§3.1)
├─ Menu/SeedText.cs                    the seed field: empty is random, digits are the seed
├─ Menu/TeamPickerPage.razor (+ .css)  /pick-team: the 32 cards by division, the marked card, Take over, Start over
├─ Menu/TeamPickCard.razor (+ .css)    one team: crest, name, division, strength, cap space; marked or not
├─ Menu/SaveActions.cs                 + LandingAsync, CreateLeagueAsync, PickAsync, StartOverAsync; − CreateAsync (M0's)
├─ Menu/SaveCardView.razor (+ .css)    + the crest and the league line (§3.3)
├─ Menu/SaveCardText.cs                + League: "New York Excelsiors · 2026 · Training Camp"
├─ Menu/MainMenu.razor (+ .css)        Continue names the team, with its crest
├─ Football/Phases.cs                  Phase.Text(): "Training Camp", "Re-sign & Tag Window", …
├─ League/LeagueHome.razor             a league without a team goes to the picker
├─ Shell/MenuLayout.razor              the menu shell, for the two wizard pages (§2)
└─ Shell/AppearanceSync.razor          + the user team's primary, from GameStateStore (§3.2)
src/DevHost/Components/ (Claude)
├─ GalleryLayout.razor                 + the 32 team colors beside the sample tints
└─ Gallery/                            TeamPickerPage, SaveCardsPage; ThemePage + the button states
tests/ (Claude)                        Application.Tests, Infrastructure.Tests, UI.Components.Tests, UI.Tests (§4)
```

Names are suggestions: keep them unless you have a reason not to. Everything new in UI is in folders that already exist. The `.cs` files (`SaveActions`, `SeedText`, `SaveCardText`) take their own `using`s, since `_Imports.razor` only reaches Razor files.

## 2. Decisions this guide makes

D-095 settled §10's four questions. These are the smaller choices, so the code, the tests and WP9 agree:

| Choice | Rule | Why |
|---|---|---|
| **Two routes, one per step** | `/new` takes the name and seed; `/pick-team` is the picker. The picker serves any open league without a user team, not only one just generated | An unpicked league must reopen to the picker after a close, a crash or restoring its `created` snapshot (WP5 §2). One route for all of them |
| **Where a league lands** | `SaveActions.LandingAsync()`: the picker while the open league's header has no user team, its home once it does. `OpenAsync` (Continue and Load) uses it. `LeagueHome` sends a league without a team to the picker, and the picker sends a league with one home | One rule, in the class that already turns save results into navigation. No second pick ever shows (WP5 §2) |
| **The menu shell for both steps** | `/new` and `/pick-team` declare `@layout MenuLayout`: the app bar as the title bar, no league context, no navigation. `MainLayout` stays as it is | `MainLayout` switches to the league shell on `SaveOpened`, which `CreateLeague` raises before the wizard navigates: without this, step one would flash inside the league shell. And the league's navigation has nothing to show before there's a team |
| **The name is checked before generating** | `SaveRules.CheckLeagueName(name)` returns the trimmed name or `SaveErrors.InvalidName`, with M0's two messages. `SaveManager.CreateAsync` calls it, and so does step one before it generates | Generating first would cost half a second to learn the name was blank. One rule, so the field and the save say the same thing |
| **The seed** | A `FluentTextInput` with `InputMode="TextInputMode.Numeric"` and the placeholder "Random". Empty draws 1–999,999,999 (`SeedText.Random`). Typed, digits only, any `ulong`. Anything else: "A seed is a whole number, up to 18446744073709551615." under the field | A random seed short enough to read out. A number input can't hold a `ulong` (JavaScript numbers lose digits past 2^53), so the field is text |
| **The year** | `TimeProvider.GetLocalNow().Year`, injected into step one | D-073. `TimeProvider` is already registered (Infrastructure), and the UI may read the clock |
| **Progress** | An indeterminate `FluentProgressBar` and the step's name ("Generating the league", "Saving it") in a `role="status"` line, from `IProgress<LeagueCreationStep>`. The fields and buttons wait while it runs | WP5 reports two steps. Under a second in Debug, so a percentage would be invented |
| **No Cancel button** | The token stays unused in v0 | Generation can't be interrupted (it's pure) and the whole create takes under a second (WP5 §2). M5's simulated prior season, at about 15 s, brings a real cancel |
| **The picker reads once** | `GetHeaderAsync()` then `GetTeamSummariesAsync()`, each on the thread pool (`Task.Run`), grouped by `(ConferenceName, DivisionName)` in the summaries' order, which is the team set's: 8 groups of 4 | SQLite's async calls complete synchronously (WP6 §8), so without `Task.Run` the read holds the render thread and the progress bar never paints. The summaries are already in TeamID order |
| **The card is a toggle button** | A native `<button type="button" aria-pressed>` styled as a card with tokens only: the crest at 48 px (`Decorative`), "City Nickname", "Conference · Division", three labelled `RatingChip`s (OVR, OFF, DEF) and "Cap space" with `Money.Format`. Its text is its accessible name. Disabled while a pick runs | The whole card is the target. A native button brings its role, its place in the Tab order, and Enter and Space, with no code. `aria-pressed` says which card is marked |
| **Select, then take over** (D-095 #3) | A click marks one card, and a second card moves the mark. The footer's primary button reads "Take over the New York Excelsiors" once a card is marked, and "Take over" disabled before. It runs `PickUserTeamAsync`, then goes home. There's no dialog | Nothing is final until Take over, and the button names what it does |
| **The marked card's look** | A 2 px `--gfo-accent` outline inside the card's radius, on `--gfo-selection`, the opaque row selection. Before the pick the accent is neutral, so the mark is the neutral blue | The marks VISUAL_DESIGN §3.3 gives selection. Opaque, so the chips keep their 4.5:1 (WP7 §7) |
| **The footer stays in view** | `position: sticky; bottom: 0` on Surface 1 with a top Border, inside the page's scroll: Take over, Start over and Close league | Eight divisions run past one screen, and the button for the marked card must stay reachable |
| **The seed on the picker** | `LeagueHeaderDto` gains `Seed` (`ulong`), read by `GetHeaderAsync`. The picker shows "Saturday League · seed 482913377 · 2026" | The seed is worth sharing at the moment you decide whether to keep the league. Starting over reads the name and seed from the same header |
| **Team tint** | `AppearanceSync` re-applies when Settings → Appearance or `GameStateStore.Header?.UserTeam?.Identity.PrimaryColor` changes, through `AppearanceMapping.From(settings, teamPrimary)`, which already takes the color | The store already reloads on `SaveOpened`, `UserTeamPicked` and `SaveClosed` (WP6). Before the pick and at the menus there's no user team, so the accent is neutral |
| **The button states** (D-095 #1) | `AccentColors.StateFills(fill)`, set on Fluent's `Brand.BackgroundHover`, `BackgroundPressed` and `BackgroundSelected` by `StadiumTheme`: away from the text by 10%, 20% and 15% | Fluent's ramp always darkens, which buries the near-black text of 7 teams (§8) |
| **The load screen reads the league only at this app's schema** | `SaveCatalog` reads the season, the phase and the user's team only for a `Ready` card. Other cards keep M0's columns | Another version's tables may differ, which is why the card's columns are read with raw SQL (PERSISTENCE §9). Until 1.0 every openable save is `Ready` (D-090) |
| **Phase names** | `Phases.Text(this Phase)` in `UI/Football`, with the names of CALENDAR_AND_PHASES §2 ("Training Camp" … "Offseason Program") | The enum's names aren't display text (`ResignWindow`), as with `PotentialGrades.Text` |
| **No new log events** | 1304 (created, with the seed and the time) and 1305 (picked) already cover it | WP5 built both for this caller |
| **The tests run on a real save** | The wizard's UI tests use `RealApp` and the real `CreateLeague` (D-093) | `CreateLeague` and `LeagueCommands` are concrete classes, and D-093 rules out test-only interfaces. About half a second a league |

## 3. The slices

### 3.1 Slice 1: the wizard

**Registration** (`Application/ApplicationServices.cs`). All three depend only on singletons:

```csharp
		services.AddSingleton<LeagueGenerator>();
		services.AddSingleton<CreateLeague>();
		services.AddSingleton<LeagueCommands>();
```

Update the summary's last sentence, which says they join in WP8.

**The name rule** (`Application/Saves/SaveRules.cs`), moved from `SaveManager.CreateAsync`:

```csharp
    /// <summary>
    /// A league's name, trimmed, or why it's refused. SaveManager applies it, and New League checks it before it
    /// generates, so both say the same thing.
    /// </summary>
    public static Result<string> CheckLeagueName(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return Result.Fail<string>(SaveErrors.InvalidName("A league needs a name."));
        }

        return trimmed.Length > MaxLeagueNameLength
            ? Result.Fail<string>(SaveErrors.InvalidName($"League names are at most {MaxLeagueNameLength} characters."))
            : Result.Ok(trimmed);
    }
```

In `SaveManager.CreateAsync`, the two `if`s become a call to it, and `name` is its value.

**The seed on the header** (`Application/Leagues`): `LeagueHeaderDto` gains `ulong Seed` after `LeagueName`, and `GetHeaderAsync` reads `SaveInfo.Seed` with the rest.

**The seed field** (`UI/Menu/SeedText.cs`):

```csharp
/// <summary>New League's seed field (F-01 §4): empty is a random seed, digits are the seed, anything else is refused.</summary>
public static class SeedText
{
    /// <summary>The largest random seed: nine digits, short enough to read out.</summary>
    public const ulong RandomMax = 999_999_999;

    public const string Refused = "A seed is a whole number, up to 18446744073709551615.";

    /// <summary>The typed seed, <paramref name="random"/>'s when the field is empty, or null when it isn't a whole number.</summary>
    public static ulong? Read(string? text, Func<ulong> random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return random();
        }

        return trimmed.All(char.IsAsciiDigit)
            && ulong.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var seed) ? seed : null;
    }

    /// <summary>A random seed from 1 to <see cref="RandomMax"/>. The UI's own randomness: generation never sees it.</summary>
    public static ulong Random() => (ulong)System.Random.Shared.NextInt64(1, (long)RandomMax + 1);
}
```

**`SaveActions`** gains the wizard's actions. Its primary constructor adds `LeagueQueries queries, CreateLeague create, LeagueCommands commands`. M0's `CreateAsync(string)` goes, with the page that called it. The new constant isn't called `NewLeague`, which would hide the record of that name inside the class.

```csharp
    /// <summary>The team picker: where an open league without a user team lands (F-01 §4).</summary>
    public const string TeamPicker = "/pick-team";

    /// <summary>New League's first step.</summary>
    public const string Wizard = "/new";

    /// <summary>Where the open league lands: the picker until it has a user team, then its home.</summary>
    public async Task<string> LandingAsync()
    {
        var header = await Task.Run(() => queries.GetHeaderAsync());
        return header.UserTeam is null ? TeamPicker : LeagueHome;
    }

    /// <summary>Generates the league and opens its picker, or returns why it was refused.</summary>
    public async Task<DomainError?> CreateLeagueAsync(NewLeague league, IProgress<LeagueCreationStep> progress)
    {
        var created = await create.RunAsync(league, progress);
        if (created.IsFailure)
        {
            return created.Error;
        }

        navigation.NavigateTo(TeamPicker);
        return null;
    }

    /// <summary>
    /// Makes the marked team the user's and opens the league's home (D-095 #3: the picker's Take over is the
    /// confirmation). True when picked.
    /// </summary>
    public async Task<bool> PickAsync(TeamIdentityDto team)
    {
        ArgumentNullException.ThrowIfNull(team);
        var picked = await commands.PickUserTeamAsync(team.TeamID);
        if (picked.IsFailure)
        {
            await toasts.ErrorAsync($"Couldn't pick the {team.City} {team.Nickname}", picked.Error!.Message);
            return false;
        }

        navigation.NavigateTo(LeagueHome);
        return true;
    }

    /// <summary>
    /// Asks first, then sends the open, unpicked league to the Recycle Bin and goes back to the first step with its
    /// name and seed filled in (D-095 #2). False when it stays.
    /// </summary>
    public async Task<bool> StartOverAsync()
    {
        if (saves.Current is not { } current)
        {
            navigation.NavigateTo(Wizard);
            return true;
        }

        var header = await Task.Run(() => queries.GetHeaderAsync());
        if (!await dialogs.ConfirmAsync("Start over?", $"{header.LeagueName} goes to the Recycle Bin, and you're back at the first step with its name and seed.",
                confirm: "Start over", cancel: "Keep this league"))
        {
            return false;
        }

        var deleted = await saves.DeleteAsync(current.FilePath);
        if (deleted.IsFailure)
        {
            await toasts.ErrorAsync("Couldn't start over", deleted.Error!.Message);
            return false;
        }

        navigation.NavigateTo(navigation.GetUriWithQueryParameters(Wizard, new Dictionary<string, object?>
        {
            ["name"] = header.LeagueName,
            ["seed"] = header.Seed.ToString(CultureInfo.InvariantCulture),
        }));
        return true;
    }
```

`OpenAsync`'s last line becomes `navigation.NavigateTo(await LandingAsync());`.

**Step one** (`UI/Menu/NewLeaguePage.razor`) keeps M0's frame, field and Enter handling, and gains the seed, the progress and the query parameters:

```razor
@page "/new"
@layout MenuLayout
@inject ISaveManager Saves
@inject SaveActions Actions
@inject NavigationManager Navigation
@inject TimeProvider Time

<PageTitle>New league · Gridiron Front Office</PageTitle>

<MenuFrame LabelledBy="new-title">
    <h1 id="new-title" class="gfo-new-title gfo-display">New league</h1>

    <FluentTextInput Label="League name" @bind-Value="_name" MaxLength="SaveRules.MaxLeagueNameLength" Autofocus="true"
                     Required="true" Disabled="_busy" Message="@_nameError" MessageState="@State(_nameError)"
                     ChangeAfterKeyPress="SubmitKeys" OnChangeAfterKeyPress="OnNameEnterAsync" data-testid="league-name" />
    <FluentTextInput Label="Seed" @bind-Value="_seed" Placeholder="Random" InputMode="TextInputMode.Numeric"
                     Disabled="_busy" Message="@(_seedError ?? "Leave it empty for a random league. The same seed makes the same league.")"
                     MessageState="@State(_seedError)" ChangeAfterKeyPress="SubmitKeys" OnChangeAfterKeyPress="OnSeedEnterAsync"
                     data-testid="league-seed" />

    @if (_step is { } step)
    {
        <FluentProgressBar />
        <p class="gfo-new-step" role="status" data-testid="step">@(step == LeagueCreationStep.Generating ? "Generating the league" : "Saving it")</p>
    }

    <div class="gfo-new-actions">
        <FluentButton Disabled="_busy" OnClick="@(() => Navigation.NavigateTo(SaveActions.MainMenu))" data-testid="back">Back</FluentButton>
        <FluentButton Appearance="ButtonAppearance.Primary" Loading="_busy" OnClick="GenerateAsync" data-testid="generate">Generate</FluentButton>
    </div>
</MenuFrame>

@code {
    private static readonly KeyPress[] SubmitKeys = [KeyPress.For(KeyCode.Enter)];

    [SupplyParameterFromQuery(Name = "name")] public string? StartName { get; set; }
    [SupplyParameterFromQuery(Name = "seed")] public string? StartSeed { get; set; }

    private string? _name = "";
    private string? _seed = "";
    private string? _nameError;
    private string? _seedError;
    private LeagueCreationStep? _step;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        if (Saves.Current is not null)
        {
            Navigation.NavigateTo(await Actions.LandingAsync(), replace: true);
            return;
        }

        (_name, _seed) = (StartName ?? "", StartSeed ?? "");   // Start over brings them back (D-095 #2)
    }

    private static MessageState? State(string? error) => error is null ? null : MessageState.Error;

    private Task OnNameEnterAsync(FluentKeyPressEventArgs e) { _name = e.Value; return GenerateAsync(); }

    private Task OnSeedEnterAsync(FluentKeyPressEventArgs e) { _seed = e.Value; return GenerateAsync(); }

    private async Task GenerateAsync()
    {
        if (_busy)
        {
            return;
        }

        var name = SaveRules.CheckLeagueName(_name);
        var seed = SeedText.Read(_seed, SeedText.Random);
        _nameError = name.IsFailure ? name.Error!.Message : null;
        _seedError = seed is null ? SeedText.Refused : null;
        if (name.IsFailure || seed is null)
        {
            return;
        }

        _busy = true;
        // Created here, so each report is posted back to the renderer's context
        var progress = new Progress<LeagueCreationStep>(s => { if (_busy) { _step = s; StateHasChanged(); } });
        var refused = await Actions.CreateLeagueAsync(new NewLeague(name.Value, seed.Value, Time.GetLocalNow().Year), progress);
        if (refused is not null)
        {
            (_busy, _step, _nameError) = (false, null, refused.Message);   // the only refusal is the name's
        }
    }
}
```

On success the page is gone (the picker replaced it), so `_busy` stays set.

**`MenuLayout`** (`UI/Shell/MenuLayout.razor`) is `MainLayout`'s no-league branch on its own:

```razor
@inherits LayoutComponentBase
@* The menu shell (M1 WP8 §2) for the New League steps: the app bar as the title bar, with no league context or
   navigation, even once the league is open. MainLayout switches to the league shell on SaveOpened, which arrives
   before the wizard moves on. *@
<AppearanceSync />
<AppShell Brand="NoBrand" Bleed="true">
    <PageErrorBoundary>
        @Body
    </PageErrorBoundary>
</AppShell>

@code {
    private static readonly RenderFragment NoBrand = _ => { };
}
```

**The picker** (`UI/Menu/TeamPickerPage.razor`):

```razor
@page "/pick-team"
@layout MenuLayout
@inject ISaveManager Saves
@inject LeagueQueries Queries
@inject SaveActions Actions
@inject NavigationManager Navigation

<PageTitle>Pick your team · Gridiron Front Office</PageTitle>

<main class="gfo-picker" aria-labelledby="picker-title">
    <header class="gfo-picker-header">
        <h1 id="picker-title" class="gfo-display">Pick your team</h1>
        @if (_header is { } header)
        {
            <p class="gfo-num" data-testid="picker-league">@header.LeagueName · seed @header.Seed · @header.Season</p>
        }
        <p>You're taking over a team whose GM was just let go. A stronger roster is an easier start.</p>
    </header>

    @if (_teams is null)
    {
        <FluentProgressBar />
    }
    else if (_teams.Count == 0)
    {
        <EmptyState Title="This save has no league in it" Icon="IconName.Warning" data-testid="picker-empty">
            It was made before the league wizard. Close it, then delete it from the load screen.
        </EmptyState>
    }
    else
    {
        @foreach (var division in _teams.GroupBy(t => (t.ConferenceName, t.DivisionName)))
        {
            <section class="gfo-picker-division" aria-label="@($"{division.Key.ConferenceName} {division.Key.DivisionName}")">
                <h2>@division.Key.ConferenceName · @division.Key.DivisionName</h2>
                <div class="gfo-picker-grid">
                    @foreach (var team in division)
                    {
                        <TeamPickCard Team="team" Selected="@(_selected?.Identity.TeamID == team.Identity.TeamID)"
                                      Disabled="_busy" OnSelect="Select" />
                    }
                </div>
            </section>
        }
    }

    @* Sticky at the bottom of the page's scroll (§2), so Take over stays in reach of any card *@
    <footer class="gfo-picker-actions">
        <FluentButton Disabled="_busy" OnClick="StartOverAsync" data-testid="start-over">Start over</FluentButton>
        <FluentButton Disabled="_busy" OnClick="Actions.CloseAsync" data-testid="close-league">Close league</FluentButton>
        <FluentButton Appearance="ButtonAppearance.Primary" Disabled="@(_selected is null || _busy)" Loading="_busy"
                      OnClick="TakeOverAsync" data-testid="take-over">
            @(_selected is { } marked ? $"Take over the {marked.Identity.City} {marked.Identity.Nickname}" : "Take over")
        </FluentButton>
    </footer>
</main>

@code {
    private LeagueHeaderDto? _header;
    private IReadOnlyList<TeamSummaryDto>? _teams;
    private TeamSummaryDto? _selected;   // the marked card (D-095 #3); nothing is picked until Take over
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        if (Saves.Current is null)
        {
            Navigation.NavigateTo(SaveActions.MainMenu, replace: true);
            return;
        }

        // On the thread pool: SQLite's async calls complete synchronously, and the bar must paint (§2)
        _header = await Task.Run(() => Queries.GetHeaderAsync());
        if (_header.UserTeam is not null)
        {
            Navigation.NavigateTo(SaveActions.LeagueHome, replace: true);   // no second pick
            return;
        }

        _teams = await Task.Run(() => Queries.GetTeamSummariesAsync());
    }

    private Task Select(TeamSummaryDto team)
    {
        _selected = team;   // a second card moves the mark
        return Task.CompletedTask;
    }

    private async Task TakeOverAsync()
    {
        if (_selected is not { } team)
        {
            return;
        }

        _busy = true;
        _busy = await Actions.PickAsync(team.Identity);   // true: the page is going away
    }

    private async Task StartOverAsync()
    {
        _busy = true;
        _busy = await Actions.StartOverAsync();
    }
}
```

**One card** (`UI/Menu/TeamPickCard.razor`). Style it with tokens only: Surface 1, Radius M, a 1 px Border, Surface 2 on hover, Fluent's focus ring on `:focus-visible`, and the marked look (§2) on `[aria-pressed="true"]`.

```razor
@* UI/Menu/TeamPickCard.razor: one team on the picker (F-01 §8). The whole card is a toggle button: a click marks it
   (D-095 #3), and its text is its name. *@
<button type="button" class="gfo-pick-card" aria-pressed="@(Selected ? "true" : "false")" disabled="@Disabled"
        @onclick="() => OnSelect.InvokeAsync(Team)" data-testid="pick-card">
    <TeamMark Team="Team.Identity" Size="48" Decorative="true" />
    <span class="gfo-pick-name">@Team.Identity.City @Team.Identity.Nickname</span>
    <span class="gfo-pick-division">@Team.ConferenceName · @Team.DivisionName</span>
    <span class="gfo-pick-strength">
        <span>OVR <RatingChip Value="Team.RosterStrength.Overall" /></span>
        <span>OFF <RatingChip Value="Team.RosterStrength.Offense" /></span>
        <span>DEF <RatingChip Value="Team.RosterStrength.Defense" /></span>
    </span>
    <span class="gfo-pick-cap">Cap space <span class="gfo-num">@Money.Format(Team.CapSpace)</span></span>
</button>

@code {
    [Parameter, EditorRequired] public TeamSummaryDto Team { get; set; } = default!;

    /// <summary>The marked card: the one Take over would pick.</summary>
    [Parameter] public bool Selected { get; set; }

    /// <summary>A pick is running: every card waits.</summary>
    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback<TeamSummaryDto> OnSelect { get; set; }
}
```

**`LeagueHome`** keeps its redirect to the menu, and adds the other one: once `GameStateStore.Header` has loaded with no user team, `Navigation.NavigateTo(SaveActions.TeamPicker, replace: true)`. WP9 rebuilds Home at `/home` (the plan's route fix); the rule moves with it.

### 3.2 Slice 2: team tint

**The button states** (`UI.Components/Theming/AccentColors.cs`), D-095 #1's one rule for every team:

```csharp
    /// <summary>
    /// The accent fill's hover, pressed and selected shades (M1 WP8 §8): moved away from the on-accent text, toward
    /// black under white text and toward white under near-black text, by 10%, 20% and 15%. So each keeps the text at
    /// least as readable as the fill does. Fluent's own ramp always darkens, which buries near-black text.
    /// </summary>
    public static (string Hover, string Pressed, string Selected) StateFills(string fill)
    {
        var away = OnAccent(fill) == White ? "#000000" : White;
        return (Mix(fill, away, 0.10), Mix(fill, away, 0.20), Mix(fill, away, 0.15));
    }
```

`StadiumTheme.Apply` sets them beside the other brand slots:

```csharp
        var (hover, pressed, selected) = AccentColors.StateFills(accent.AccentFill);
        brand.BackgroundHover = hover;
        brand.BackgroundPressed = pressed;
        brand.BackgroundSelected = selected;
```

**`AppearanceSync`** follows the store as well as the settings:

```razor
@implements IDisposable
@inject ISettingsStore Settings
@inject GfoThemeService Theme
@inject GameStateStore State
@* Applies Settings → Appearance and the user team's primary once the page is up, and again whenever either changes
   (UI_DESIGN_SYSTEM.md §3.2). Without a user team (the menus, the picker) the accent is neutral. Renders nothing. *@

@code {
    private (AppearanceSettings Appearance, string? Primary)? _applied;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Settings.Changed += OnSettingsChanged;
            State.Changed += OnStateChanged;
            await ApplyAsync();
        }
    }

    // Saves and loads complete off the UI thread, so the re-apply is marshalled back onto it.
    private void OnSettingsChanged(object? sender, AppSettings settings) => _ = InvokeAsync(ApplyAsync);

    private void OnStateChanged() => _ = InvokeAsync(ApplyAsync);

    // Only a change to Appearance or to the team's color re-applies: the window placement and developer flags don't.
    private Task ApplyAsync()
    {
        var wanted = (Settings.Current.Appearance, State.Header?.UserTeam?.Identity.PrimaryColor);
        if (wanted == _applied)
        {
            return Task.CompletedTask;
        }

        _applied = wanted;
        return Theme.ApplyAsync(AppearanceMapping.From(wanted.Appearance, wanted.PrimaryColor));
    }

    public void Dispose()
    {
        Settings.Changed -= OnSettingsChanged;
        State.Changed -= OnStateChanged;
    }
}
```

`AppearanceMapping`'s `teamPrimary` comment ("until M1, always") becomes "null when no league is open or before the pick".

### 3.3 Slice 3: the load screen and Continue

**`SaveCard`** (`Application/Saves/ISaveCatalog.cs`) gains three properties, and its summary loses "Until M1":

```csharp
    /// <summary>The league's user team, for a Ready card whose league has one (M1, F-01 §4).</summary>
    public TeamIdentityDto? Team { get; init; }

    /// <summary>The league's season, for a Ready card with a league in it.</summary>
    public int? Season { get; init; }

    /// <summary>The league's phase, for a Ready card with a league in it.</summary>
    public Phase? Phase { get; init; }
```

**`SaveCatalog`** reads them after the card's columns, for a `Ready` card only:

```csharp
    // The league's position and the user's team (M1 WP8). Read only at this app's schema: another version's tables
    // may differ. Teams' count tells a league from a save made by M0's New League page, which has none.
    private const string LeagueQuery =
        "SELECT s.CurrentSeason, s.CurrentPhase, (SELECT COUNT(*) FROM Teams), t.TeamID, t.Abbreviation, t.City, t.Nickname, " +
        "t.PrimaryColor, t.SecondaryColor, t.AccentColor, t.CrestShape, t.CrestMonogram, t.CrestStripe " +
        "FROM SaveInfo s LEFT JOIN Teams t ON t.TeamID = s.UserTeamID LIMIT 1;";
```

Build the `TeamIdentityDto` when `t.TeamID` isn't null, and leave all three null when the count is 0. Parse the enum columns with `Enum.TryParse`, and treat a name it doesn't know as no league (§7).

**The words** (`UI/Menu/SaveCardText.cs`):

```csharp
    /// <summary>"New York Excelsiors · 2026 · Training Camp", "No team picked yet", or null for a card with no league to show.</summary>
    public static string? League(SaveCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card.Season is not { } season || card.Phase is not { } phase)
        {
            return null;
        }

        return card.Team is { } team ? $"{team.City} {team.Nickname} · {season} · {phase.Text()}" : "No team picked yet";
    }
```

**`SaveCardView`** puts `<TeamMark Team="team" Size="40" Decorative="true" />` before the heading when `Card.Team` is set, and the `League` line between the heading and the details. **`MainMenu`**'s Continue line becomes "Saturday League · New York Excelsiors · last played Nov 4, 2026", with a 20 px decorative crest in front.

**Phase names** (`UI/Football/Phases.cs`):

```csharp
/// <summary>The phases as the screens name them (CALENDAR_AND_PHASES.md §2).</summary>
public static class Phases
{
    public static string Text(this Phase phase) => phase switch
    {
        Phase.TrainingCamp => "Training Camp",
        Phase.RegularSeason => "Regular Season",
        Phase.Playoffs => "Playoffs",
        Phase.SeasonWrap => "Season Wrap",
        Phase.ResignWindow => "Re-sign & Tag Window",
        Phase.Combine => "Combine",
        Phase.FreeAgency => "Free Agency",
        Phase.PreDraft => "Pre-Draft",
        Phase.Draft => "Draft",
        Phase.OffseasonProgram => "Offseason Program",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
    };
}
```

### 3.4 The gallery (Claude)

- **The toolbar** gets the 32 team colors next to the three sample tints, so any gallery page can be checked in any team's colors.
- **`/_gallery/team-picker`:** the 32 `TeamPickCard`s grouped by division, built from the shipped data with fixed sample strengths and cap space (as the Teams page builds identities), including the 7 dark-text teams.
- **`/_gallery/save-cards`:** a `SaveCardView` in each state: a picked league, an unpicked one, an upgrade, a newer and an older build, and an unreadable file.
- **The Theme page** gains the three button states, each with its contrast against the on-accent text.

## 4. The tests (Claude writes them)

Per slice, each proven to fail on a planted problem.

**Slice 1**
- **`SaveRulesTests`** (Application.Tests): a blank or all-space name, 60 and 61 characters, trimming; `SaveManager`'s messages unchanged.
- **`SeedTextTests`** (UI.Tests): empty → the random one; "42", " 42 ", "007" → 42, 42, 7; 0 and `ulong.MaxValue`; refused: "-1", "4.2", "1e9", "12a", one past `ulong.MaxValue`; `Random` stays within 1–999,999,999.
- **`NewLeagueWizardTests`** (UI.Tests, `RealApp`):
  - a refused name or seed is explained under its field, and no save exists;
  - Generate shows its steps, creates the league with the typed seed and this year (a `FakeTimeProvider`), and lands on the picker;
  - Start over's name and seed come back in the fields.
- **`TeamPickerTests`** (UI.Tests, `RealApp`):
  - 32 cards in team-set order, in 8 labelled divisions;
  - a card's name, division, three chips and cap space;
  - the header's league, seed and year;
  - nothing is marked at first, and Take over waits;
  - a click marks one card (`aria-pressed`) and names it on Take over, and a second click moves the mark;
  - marking alone picks nothing (the header still has no user team);
  - Take over sets the marked team as the user's and goes home;
  - Start over asks, deletes the league (the fake Recycle Bin) and goes to `/new` with the name and seed;
  - a league with a team goes home, and no league goes to the menu;
  - a save with no teams shows the empty state.
- **`MenuScreenTests`:**
  - Continue and Load land an unpicked league on the picker and a picked one at home;
  - `LeagueHome` sends an unpicked league to the picker;
  - M0's two New League cases are replaced by the wizard's.

**Slice 2**
- **`AccentColorsTests`** (UI.Components.Tests):
  - for the neutral blue and every shipped primary (read from `data/league/teams.json`), the on-accent text holds 4.5:1 on the fill and all three states;
  - each state moves away from its text: lighter under near-black, darker under white;
  - hover, selected and pressed step in that order.
- **`AppearanceSyncTests`** (UI.Tests, `RealApp`):
  - neutral at open;
  - the team's primary after `UserTeamPicked`, in both modes;
  - neutral again on close;
  - Accent: Neutral keeps it neutral after the pick;
  - an unrelated settings save doesn't re-apply.

**Slice 3**
- **`SaveCatalogTests`** (Infrastructure.Tests):
  - a seed-42 save's card has its season and Training Camp, with no team before the pick and its team after;
  - a save made by the empty overload has none of the three;
  - so do a newer and an older-build save.
- **`SaveCardText`:** the league line's three forms. **`SaveCardView` and `MainMenu`:** the crest and the team on a card and under Continue, and no crest without a team.
- **`PhasesTests`:** every phase has a distinct name.

## 5. Done when

**Slice 1: the wizard**
- [ ] `AddGfoApplication` registers the three; `SaveRules.CheckLeagueName`, used by `SaveManager`; `LeagueHeaderDto.Seed`.
- [ ] `SeedText`, `NewLeaguePage`, `MenuLayout`, `TeamPickerPage`, `TeamPickCard`, `SaveActions`' four actions, and `LeagueHome`'s redirect.
- [ ] Claude: `/_gallery/team-picker` and the team colors in the toolbar.
- [ ] Claude's slice 1 tests pass, each proven to fail on a planted problem (§4).

**Slice 2: team tint**
- [ ] `AccentColors.StateFills` (D-095 #1) and `StadiumTheme`'s three slots; `AppearanceSync` on the store.
- [ ] Claude: the Theme page's button states.
- [ ] Claude's slice 2 tests pass, each proven to fail on a planted problem.

**Slice 3: the load screen and Continue**
- [ ] `SaveCard`'s three properties, `SaveCatalog`'s league query, `SaveCardText.League`, `SaveCardView`, `MainMenu` and `Phases`.
- [ ] Claude: `/_gallery/save-cards`.
- [ ] Claude's slice 3 tests pass, each proven to fail on a planted problem.

**Close-out (Claude)**
- [ ] Checked in a visible pane: create → pick → Home, Start over, Continue to an unpicked league, and the load screen, in Dark and Light with the 7 dark-text teams and a few others. Every button state holds 4.5:1 (§8's measurement, repeated on the built code), and the marked card's text and chips hold theirs on `--gfo-selection`. Then the same flow in the desktop app (D-095 #4).
- [ ] `./build.ps1` green.
- [ ] Reviewed against this guide, F-01, F-02 §8, PERSISTENCE §9, UI_DESIGN_SYSTEM §3.2 and §7, VISUAL_DESIGN §3.3 and §10, and D-095 (your §10 answers).
- [ ] Docs: F-01 as built; the plan's WP8 results; PERSISTENCE §9 (the card's league line); UI_DESIGN_SYSTEM §3.2 (the button states) and §7 (the two wizard routes, `MenuLayout`); VISUAL_DESIGN §3.3 (the state rule); CLAUDE.md's gotchas.
- [ ] Committed by you: one commit per slice, each with its tests (D-076), then the docs.

## 6. Order of work

1. **Slice 1.** The registration and `CheckLeagueName` first (pure, and the tests need them), then `SeedText`, `SaveActions`, the two pages and the card. Restart the DevHost watcher after adding the new files: it only knows files that existed when it started.
2. **Slice 2.** `StateFills` and `StadiumTheme` (pure, tested on all 32 colors), then `AppearanceSync`.
3. **Slice 3.** `SaveCard` and `SaveCatalog`, then the card's words and markup.
4. **Close-out:** Claude's checks and the docs, then your commits.

Delete your dev saves made by M0's New League page since WP5: they have the current schema and no teams, so they open to the picker's empty state (§7).

## 7. Gotchas

- **`SaveOpened` arrives before the wizard moves on.** `CreateLeague` returns after `SaveManager` has raised it, and `MainLayout` switches to the league shell on it. That's why both wizard pages use `MenuLayout`: in `MainLayout`, step one would flash inside the league shell before the picker replaces it.
- **Read on the thread pool in a page.** SQLite's async calls complete synchronously (WP6 §8), so `await Queries.GetTeamSummariesAsync()` in `OnInitializedAsync` holds the render thread until it's done, and the progress bar never shows. Wrap each read in `Task.Run`, as `GameStateStore` does.
- **`Progress<T>` posts to the context it was created on.** Create it in the handler, on the renderer's context, and its callback can call `StateHasChanged`. A report can land after `RunAsync` has returned, so ignore reports once the page has moved on (the `_busy` check).
- **A seed is a `ulong`, so the field is text.** `FluentNumberInput` goes through JavaScript numbers, which lose digits past 2^53. `ulong.TryParse` with `NumberStyles.None` refuses signs, decimals, exponents and spaces inside, after the trim.
- **`Enum.Parse` throws `ArgumentException`,** which `SaveCatalog`'s exception filter doesn't list: one save with an unknown phase name would break the whole load screen. Use `Enum.TryParse`.
- **`System.Random` is fine in UI and banned below it.** The banned-API tiers cover Core, Simulation and Application, and the random seed is the UI's choice. Generation only ever sees the number.
- **A pick has no once-only rule** (WP5 §2): the picker is what makes it once-only, by sending a league that has a team home. Don't add a second way to reach it.
- **`AppearanceSync` renders in both layouts,** so each layout switch builds a new one, which applies on its first render. With the tuple check, that's one apply per switch, as M0's was.
- **Fluent's typed theme has the state slots** (`Theme.Colors.Brand.BackgroundHover`, `BackgroundPressed`, `BackgroundSelected`, checked in the 5.0.1 package). `StadiumTheme` sets them like the brand slots it already sets. Set them after `CreateCustomThemeAsync`, never with CSS: no deep selectors into Fluent (UI_DESIGN_SYSTEM §4).
- **Your M0-era dev saves have no teams.** Made by the name-only page after WP5's schema, they're `Ready` and open to the picker's empty state. Delete them from the load screen.
- **The DevHost locks its DLLs,** and the watcher only knows files that existed when it started: restart it after slice 1's new files, and stop it before `./build.ps1`.

## 8. What Claude checked (2026-11-04)

| Question | Answer |
|---|---|
| Does everything the wizard calls exist? | Yes: `CreateLeague.RunAsync(NewLeague, IProgress<LeagueCreationStep>?, CancellationToken)`, `LeagueCommands.PickUserTeamAsync(TeamID)`, `LeagueQueries.GetHeaderAsync()` and `GetTeamSummariesAsync()`, and `ISaveManager.DeleteAsync`. Missing: their registration (WP5 left it for WP8), the seed on the header, and the team on a save card |
| How do the 32 team colors read on Fluent's button states? | Measured in the DevHost, both modes, from the computed tokens. **25 teams** take white text, and it holds 5.32:1 or better on the fill and on every state (lowest: DEN). **7 teams** take near-black text: CHI, ATL, CHA, LAC, LV, TPA and PHX. Their fill is fine (4.55–9.44:1), but Fluent darkens every state, so the text falls to 2.61–3.55:1 when hovered, 2.14–2.38 when selected and 1.48–1.60 when pressed. A `SegmentedControl`'s chosen option is the selected state: Settings' Theme, Accent and Density choices would read at about 2.2:1 for those 7 |
| Does moving the states away from the text fix it? | Yes. Lightened by 10%, 15% and 20% under near-black text, the 7 teams' states read at 5.33–10.91:1, above each fill's own contrast (computed from the same formula). Darkening under white text only raises its contrast, so every team's states hold at least what its fill does: 4.55:1 or better |
| Do the accent's text and marks hold? | Yes, by construction: `AccentColors.Corrected` mixes until 3:1 (marks) or 4.5:1 (text) on Surface 1, for any color (M0, verified on seven colors in Spike S0b). Not re-measured for the 32 |
| Do the Fluent names exist in 5.0.1? | Yes, from the package's own documentation: `FluentProgressBar` is indeterminate when `Value` is null; `FluentTextInput` has `Placeholder`, `InputMode` (`TextInputMode.Numeric`) and `Disabled`; `FluentButton` has `Loading`; the typed theme has the three brand state slots |

## 9. Not in WP8

- **The rule steps:** each arrives with the milestone that gives it meaning (F-01 §2).
- **The inherited situation and team needs** on the cards (M5).
- **League data import** (M11).
- **Cancel while generating:** with M5's 15-second prior season.
- **The record** on save cards (M3).
- **WP9's work:** the top bar's crest and team name (VISUAL_DESIGN §6), Home at `/home` with the route fix, and the league's navigation.
- **The flash on Continue and Load.** `MainLayout` shows the league shell around the main menu for an instant before navigating, as in M0. Each arrives with its first consumer (D-078).

## 10. Questions (answered 2026-11-04)

Answered one by one ([D-095](../DECISIONS.md#d-095-accent-states-move-away-from-their-text-the-picker-can-start-over-a-team-is-selected-then-taken-the-desktop-end-to-end-is-a-ui-flow-plus-a-check)), three as recommended:
1. ~~The button states for the 7 teams with near-black text?~~ → **One rule for all 32:** each state moves away from its text, by 10% (hover), 15% (selected) and 20% (pressed).
2. ~~Trying another seed from the picker?~~ → **Start over:** after a confirmation, the unpicked league goes to the Recycle Bin and step one comes back with its name and seed.
3. ~~Picking a team?~~ → **Select, then confirm** (your choice over the recommended dialog): a click marks a card, and Take over picks it.
4. ~~The plan's end-to-end test in the desktop app?~~ → **A UI flow plus Claude's check** in the desktop app after each slice.
