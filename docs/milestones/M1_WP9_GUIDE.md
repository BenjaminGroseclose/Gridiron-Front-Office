# M1 · WP9 builder's guide: the screens

> **Status:** **Ready to build** (§10 answered 2026-11-09 as [D-102](../DECISIONS.md#d-102-the-roster-opens-on-your-team-and-picks-any-team-or-all-of-them-and-a-position-the-top-bar-names-your-team-the-contract-table-runs-from-this-season-every-team-name-links-to-its-page); written the same day from [F-02 §8](../features/F-02_LEAGUE_AND_TEAMS.md#8-ui), [F-03 §8](../features/F-03_PLAYERS.md#8-ui) and [F-12 §8](../features/F-12_CONTRACTS.md#8-ui), all accepted, and checked against the code the same day) · **For:** the owner, who builds M1's production code ([M1_THE_LEAGUE.md §11](M1_THE_LEAGUE.md#11-how-m1-lands)) · **Work package:** [WP9](M1_THE_LEAGUE.md#wp9--the-screens-l) · **Size:** L
>
> WP9 is where the league becomes something you can browse. Home moves to `/home` and becomes a stub dashboard for your team. `/league` becomes the League page: both conferences, four division cards each. A Team page shows a team's hero band in its own colors, its stadium, its cap and its roster. The Roster is `DataGrid<T>`'s first real use: your team by default, any team or all of them, at any position. The Player page shows everything F-03 generates: ratings by group, measurables, bio, personality and the contract. Every player name opens a peek sheet, with "Open full profile" one click further, and every team name goes to its Team page. Nothing in the schema changes, and the read models are WP6's. One small map is new in Core (each attribute's group). When WP9 is done, three of M1's exit items hold: the screens, the 2,000-row grid under budget, and a peek sheet from every player name.

---

## 0. Who does what

| | |
|---|---|
| **Claude prepared** ✅ | This guide, and a read of what WP6's read models and M0's grid and drawer already give the screens (§8). Every column the plan names is on `PlayerRowDto`; what's missing is small and listed in §2. |
| **You build** (§3) | Three slices: the league and its teams (routes, nav, top bar, Home, League, Team); players in a grid (the Roster grid and its pickers, `PlayerLink`, the peek sheet); the Player page. |
| **Claude builds the gallery** | `/_gallery/team-hero` (all 32 bands), `/_gallery/player` (the peek sheet and the Player page's blocks, from sample DTOs) and the DataGrid page on `RosterGrid` (§3.4). |
| **Claude writes the tests** (§4) | Per slice, as each one lands, each proven to fail on a planted problem. |
| **Claude checks, after each slice** | Each screen in a visible pane in Dark and Light with several team colors ([VISUAL_DESIGN.md §10](../VISUAL_DESIGN.md#10-qa-checklist-for-every-new-screen)), then in the desktop app through WebView2's DevTools port (D-095 #4), with screenshots. The grid's sort and scroll times are measured there (§2, "The budget"). |

**Read first:**
- the plan's [WP9](M1_THE_LEAGUE.md#wp9--the-screens-l) and the specs' UI sections: [F-02 §8](../features/F-02_LEAGUE_AND_TEAMS.md#8-ui) (League page, Team header), [F-03 §8](../features/F-03_PLAYERS.md#8-ui) (Roster, Player page, the chips) with §4.2 (the attribute groups), §4.3 (measurables), §4.7 (personality) and §4.8 (bio), and [F-12 §8](../features/F-12_CONTRACTS.md#8-ui) (the contract block, the cap tiles);
- WP6's read models: `LeagueQueries` and `PlayerQueries` with their DTOs (`Application/Leagues/LeagueReadModels.cs`, `Application/Players/PlayerReadModels.cs`), and the [WP6 guide](M1_WP6_GUIDE.md) §2;
- `DataGrid<T>` and its rules: [UI_DESIGN_SYSTEM.md §5.3](../architecture/UI_DESIGN_SYSTEM.md#53-datagridt-a-gfo-wrapper-over-fluentdatagrid), and the gallery's `/_gallery/datagrid`;
- WP7's components ([UI_DESIGN_SYSTEM.md §5.4](../architecture/UI_DESIGN_SYSTEM.md#54-football-composites-in-gridironfrontofficeui)): `TeamMark`, `RatingChip`, `PotentialChip`, `RatingBar`, `PositionPill`, `Money.Format`;
- [D-100](../DECISIONS.md#d-100-screens-are-built-from-fluents-layout-and-text-components-scoped-css-covers-only-what-they-cant): every new screen is built from `FluentStack`, `FluentCard`, `FluentText` and `FluentDivider`;
- [VISUAL_DESIGN.md §6](../VISUAL_DESIGN.md#6-signature-elements) (the hero band and its ghost monogram) and §10 (the QA checklist).

**AI counterpart:** none. The screens show what any GM sees: rivals' ratings through `IPerception` (exact in M1, σ = 0), and personality only for your own players ([D-092](../DECISIONS.md#d-092-before-the-pick-an-outsider-looks-personality-shows-for-your-own-roster-roster-strength-is-the-starters-mean-ovr) #2).

## 1. What you'll build

```
src/Core/Players/
└─ AttributeGroups.cs                 each attribute's group (F-03 §4.2), and a group's attributes in the table's order
src/UI/
├─ _Imports.razor                     + @using …UI.Home, …UI.Teams, …UI.Players
├─ UiServices.cs                      + PeekService (scoped)
├─ Menu/SaveActions.cs                LeagueHome "/league" → Home "/home"; + League, Roster, and Team(id) / Player(id) routes
├─ Shell/MainNav.razor                Home, Roster, League, Settings
├─ Shell/MainLayout.razor             the top bar names your team once it's picked (D-102 #2)
├─ Home/HomePage.razor (+ .css)       /home: your team's band, where the season stands, top players, cap space
├─ League/LeaguePage.razor (+ .css)   /league: two conferences, four division cards each (League/LeagueHome.razor goes)
├─ Teams/TeamPage.razor (+ .css)      /team/{id:int}: the band, the stadium, the cap tiles, the top five cap hits, the roster
├─ Players/RosterPage.razor           /roster: a team (yours by default) or all of them, at a position (D-102 #1)
├─ Players/PlayerPage.razor (+ .css)  /player/{id:int}: header, ratings by group, measurables, bio, personality, contract
├─ Players/PlayerPeekSheet.razor      the drawer's content, with Open full profile
├─ Players/PeekService.cs             OpenPlayerAsync(PlayerID): reads the player, then opens the drawer
├─ Football/TeamHero.razor (+ .css)   a team's hero band in its own colors, with the ghost monogram
├─ Football/TeamLink.razor            a team's name: a link to its page, wherever a team is named (D-102 #4)
├─ Football/PlayerLink.razor (+ .css) a player's name: a button that opens the peek
├─ Football/RosterGrid.razor          DataGrid<PlayerRowDto> with the roster's columns
├─ Football/PlayerText.cs             display names: attributes, groups, traits and their levels, measurables with units
└─ Football/ContractText.cs           the contract line, and a season's row
src/DevHost/Components/ (Claude)
├─ GalleryLayout.razor                + Team hero, Player
└─ Gallery/                           TeamHeroPage, PlayerPartsPage (the peek and the page's blocks); DataGridPage on RosterGrid
tests/ (Claude)                       Core.Tests, UI.Tests (§4)
```

Names are suggestions. New folders in UI (`Home/`, `Teams/`, `Players/`) follow the screen they hold, as `Menu/` and `League/` do. Restart the DevHost watcher after adding files: it only knows the files it started with.

## 2. Decisions this guide makes

The specs settle what each screen shows. These are the smaller choices, so the code, the tests and WP10 agree. Four of them are your answers to §10 (D-102).

| Choice | Rule | Why |
|---|---|---|
| **The routes** | `/home` Home, `/league` the League page, `/team/{id:int}`, `/player/{id:int}`, `/roster`. `SaveActions.LeagueHome` becomes `SaveActions.Home` (`"/home"`), with `League` and `Roster` beside it and `Team(TeamID)` / `Player(PlayerID)` building the two others. Home keeps `LeagueHome`'s two redirects (no league → the menu; no team → the picker) | The plan moved Home to `/home` (owner Q3, 2026-10-20) so `/league` can be the League page. One constant per route, so a move is one line |
| **Each page reads its own data** | On the thread pool (`Task.Run`), as WP8's pages do, in `OnParametersSetAsync` for the three pages whose route carries a choice (a team, a player, the Roster's picks). A load that finishes after the route changed is dropped | Blazor reuses the page between `/team/3` and `/team/7`: `OnInitialized` runs once, so the second team would never load. SQLite's async calls complete synchronously (WP6 §8) |
| **The shell follows the store; pages don't** | The top bar reads `GameStateStore.Header`; pages read `LeagueQueries` / `PlayerQueries` themselves | The store lags the pick (WP8 §7). A page that waits on it shows nothing for a moment after Start |
| **An unknown id** | `/team/99` and `/player/99999` show an `EmptyState` ("There's no team 99 in this league") with a link to the League page. No router `NotFound` yet | The queries return null; one sentence and a way out is enough until a deep link can come from outside the app |
| **Every page has an `h1`** | The hero's title is the page's `h1` | `FocusOnNavigate Selector="h1"`: a page without one leaves focus nowhere |
| **The nav** | Home (`IconName.Home`), Roster (`People`), League (`Board`), Settings, in that order. Team and Player pages are reached through links, so no nav item is active on them | F-03 and the plan's nav. A team or player is one of 32 or 1,846: a link, not a nav entry |
| **The top bar names your team** (D-102 #2) | Once a team is picked, the app bar's brand is its crest (20 px) and "City Nickname", a link to its Team page, and its context is "League name · 2026 · Training Camp". Until the store has your team, the brand is empty (no wordmark, owner 2026-11-14), and until the header loads, the context is the league's name | VISUAL_DESIGN §6's top bar, minus the week and record (no games yet) and the cap pill (the Team page has it) |
| **A team's own colors** | `TeamHero` takes a `TeamIdentityDto` and paints with that team's primary, set inline as `--gfo-team-fill`, and the text color `AccentColors.OnAccent` picks for it (white or near-black, 4.55:1 or better for all 32, D-095). Your own team's band equals the theme's accent; a rival's is its own | A rival's page in your colors would be wrong. Team colors arrive as hex from the read model, as `TeamMark`'s do; the stylesheet only references the variable |
| **The ghost monogram** | The team's monogram (`CrestMonogram`) in condensed Archivo 800 at 220 px, on-fill at 12% opacity, bottom-right and clipped by the band, `aria-hidden` | VISUAL_DESIGN §6. It's text, not a second crest |
| **Team names** (D-102 #4) | `TeamLink`: a team's name as a link to its page (an `<a href>`), with its crest beside it where the spec shows one. Every team name in the league's screens is one: the League page, the grids' Team column, the Player page's band and bio (the drafting team too), the peek sheet, Home and the top bar. Not the Team page's own heading, and not the menu screens. In a grid cell its click doesn't also select the row. No team peek | Your answer: *"teams should be clickable everywhere to view the team page."* The app bar's hit test already leaves `a[href]` clickable (`gfo-host.js`) |
| **Player names** | `PlayerLink`: a `<button>` styled as a link (accent text, underline on hover and focus) with `aria-haspopup="dialog"`, which calls `PeekService.OpenPlayerAsync`. In a grid cell its click doesn't also select the row (`@onclick:stopPropagation`) | The exit checklist: every player name opens a peek. A button, not a link: it opens something here rather than going somewhere |
| **The peek** | `PeekService.OpenPlayerAsync(PlayerID)` reads `GetPlayerAsync` first (the drawer needs the name as its title), then `IGfoDialogs.PeekAsync<PlayerPeekSheet>(name, …, "480px")`. An unknown player is a toast. The sheet: position, number, team and age; OVR and potential chips with the archetype; height, weight, college and experience; the contract line; **Open full profile**, which closes the drawer and navigates. The team's name in the sheet closes it first too: `TeamLink` takes an optional `BeforeNavigate` callback, and with one it prevents the link's default, awaits the callback, then navigates | UI_DESIGN_SYSTEM §7 ("peek, then commit"). A sheet left open over the next page would cover it |
| **The Roster grid** | `RosterGrid` over `DataGrid<PlayerRowDto>`: Name (pinned, `PlayerLink`, sorts by last then first name), Pos (`PositionPill`, 88 px), # (64 px), Age (64 px), OVR (`RatingChip`, 76 px, sorts by `Overall.Estimate`), Pot (`PotentialChip`, 76 px, by `Potential.Grade`), Archetype (160 px), Cap hit (`Money.Format`, 112 px, by `CapHit`), and when it shows players of more than one team, Team (crest and abbreviation, 96 px). Enter or a double-click on a row opens the peek (`OnItemActivated`) | F-03 §8's columns. The pill widths are WP7's (widest pill plus the cell padding). `PerceivedRating` and typed IDs don't compare: sort by `.Estimate` and `.Value` (WP6 §8) |
| **The Roster page** (D-102 #1) | Opens on your team. Two `FluentSelect`s above the grid: **Team** (All teams, then the 32 as "City Nickname" in the team set's order) and **Position** (All positions, then the 16 in `Position`'s order, by abbreviation). The choice is the route: `/roster?team=7&position=QB`, with `team=all`; no `team` is yours, no `position` is all. A picker's change navigates with `replace: true`: Back leaves the Roster instead of stepping through every choice, and still returns from a player's page to the last one. One team reads `GetRosterAsync` (its active players); All teams reads `GetLeaguePlayersAsync` (every player who isn't retired, free agents included) and adds the Team column. The position filters the loaded rows in memory; only a new team reads again. An unknown team or position in the route falls back to yours, or to all positions. The hero band is the chosen team's (`TeamHero`, title "Roster"), a neutral one for All teams | Your answer: *"Roster should show your team by default. It should have a picker of team including All teams (same as all player option) as well as position."* The route keeps the list when Back returns from a player's page |
| **Free agents** | No team: the Team cell says "FA", and `#`, Cap hit and the contract show "—" | `PlayerRowDto.TeamID`, `JerseyNumber` and `CapHit` are null for them |
| **The team of a row** | `GetTeamIdentitiesAsync` once per page, as a `Dictionary<TeamID, TeamIdentityDto>`: the Team column, the Player header's colors and the draft line's team come from it | The rows carry only `TeamID`. Identities are read each time, about a millisecond (D-093 #3): no new query |
| **The Team page** | `TeamHero` (crest, "City Nickname", the stadium as "Skyline Field · 82,500 · Open air"); the roster strength as three chips (as the picker shows it); four `Stat` tiles: Cap, Committed, Space and Utilization (committed ÷ cap, one decimal); the top five cap hits (name, position, cap hit), from `GetRosterAsync` sorted by `CapHit`; then the roster in `RosterGrid` | F-02 §8 and F-12 §8. No query gives the top five, and the roster's rows already carry each cap hit |
| **The League page** | Two conference columns, each with its four division cards (`FluentCard`, flat), each card its four teams: crest (24 px) and `TeamLink`, in the team set's order. A CSS grid for the two columns; one column under 1,100 px | F-02 §8. The record joins each row in M3 |
| **Home** | Your team's `TeamHero`; a line "2026 · Training Camp · Aug 1, 2026" with "No games played yet"; your five best players by OVR (`PlayerLink`, `PositionPill`, `RatingChip`); and a Space tile with Committed beside it | The plan's stub. Its parts are the Team page's, so Home costs little and becomes M3's dashboard |
| **Attribute groups** | `AttributeGroups.Of(this PlayerAttribute)` and `AttributeGroups.Members(AttributeGroup)` in Core, beside `AttributeGroup`, in F-03 §4.2's order (6, 2, 7, 4, 4, 3, 10 and 6 attributes) | The grouping is the domain's (`AttributeGroup` and the enum's order are already in Core); the words are the UI's (`PlayerText`) |
| **The Player page** | A band in the player's team colors (neutral for a free agent): name in Display L, `PositionPill`, number, `TeamLink`, OVR and potential chips. Then flat cards: **Ratings** (the eight groups in a CSS grid, each attribute a `RatingBar` with its name as the label); **Measurables** (a `<dl>`: height 6'2", weight 245 lb, 40-yard 4.52 s, and so on, decimals in the current culture); **Bio** (age and birth date, hometown, college, experience, draft: "2023 · Round 2, pick 41 · Boston" or "Undrafted"); **Personality** (each trait's level, or "Unknown" for a rival's player); **Contract** (§2's next row) | F-03 §8, every field of `PlayerDetailDto`. Personality is null for rivals and for an outsider (D-092 #2) |
| **The contract block** (D-102 #3) | `ContractText.Line`: "3 years left · $12.4M APY · cap hit this season $14.1M · $9.0M guaranteed remaining" (F-12 §8), then a table of this season and the ones left: season, base, bonus (prorated), guaranteed, cap hit, money through `Money.Format` | F-12 §8 and your answer: *"Yes future season, TBD on if we can go back and see previous seasons"*. The read model keeps the past seasons (`Seasons`), so showing them later is the table's change alone (IDEAS.md) |
| **The budget** | Measured, not asserted (D-078 #4): Claude times a header click to the next paint, a position change to the next paint, and a scroll through the whole list in the desktop app over the DevTools port, on `/roster?team=all` (1,846 rows for seed 42) and the gallery's 2,000 rows. The bar is ARCHITECTURE.md's < 100 ms for a sort or a filter; S0b measured 45–52 ms with GFO's header | bUnit has no layout, so it can't time a sort to the screen or a scroll. The exit item reads "2,000-player grid", so the gallery keeps its 2,000 |
| **No new log events** | — | Reads log nothing (WP6's 1306 already times the league list) |

## 3. The slices

### 3.1 Slice 1: the league and its teams

**Routes** (`UI/Menu/SaveActions.cs`). `LeagueHome` goes, and every caller moves to `Home` (`SaveActions.cs`, `Finalize.razor`, `TeamPickerPage.razor`, `LoadScreen.razor`, `MainMenu.razor`):

```csharp
	/// <summary>The open league's home (M1 WP9: /league is the League page now).</summary>
	public const string Home = "/home";

	/// <summary>Both conferences and their divisions (F-02 §8).</summary>
	public const string League = "/league";

	/// <summary>A team's roster or every player, at a position (D-102 #1).</summary>
	public const string Roster = "/roster";

	/// <summary>A team's page.</summary>
	public static string Team(TeamID team) => string.Create(CultureInfo.InvariantCulture, $"/team/{team.Value}");

	/// <summary>A player's page.</summary>
	public static string Player(PlayerID player) => string.Create(CultureInfo.InvariantCulture, $"/player/{player.Value}");
```

**The nav** (`UI/Shell/MainNav.razor`):

```razor
<NavEntry Href="@SaveActions.Home" Title="Home" Icon="IconName.Home" Match="NavLinkMatch.All" />
<NavEntry Href="@SaveActions.Roster" Title="Roster" Icon="IconName.People" Match="NavLinkMatch.All" />
<NavEntry Href="@SaveActions.League" Title="League" Icon="IconName.Board" Match="NavLinkMatch.All" />
<NavEntry Href="/settings" Title="Settings" Icon="IconName.Settings" />
```

**The top bar** (`UI/Shell/MainLayout.razor`, D-102 #2). The brand follows the store, like the tint: the crest and name once `Header.UserTeam` is set, as a `TeamLink` to its page, and empty before (`NoBrand`, as at the menu). `MainLayout` subscribes to `GameStateStore.Changed` and marshals with `InvokeAsync`, as it already does for `SaveOpened`.

**A team's band** (`UI/Football/TeamHero.razor`, with its stylesheet):

```razor
@using GridironFrontOffice.Application.Leagues
@* A team's hero band (VISUAL_DESIGN.md §6) in its own colors: the fill is the team's primary, the text whichever of
   white and near-black reads on it (AccentColors.OnAccent), and the ghost monogram sits behind, decorative. The colors
   are the team's data, so they go in as hex through two custom properties, as TeamMark's do. *@
<header class="gfo-team-hero" style="--gfo-team-fill: @Team.PrimaryColor; --gfo-team-on-fill: @_onFill">
    <span class="gfo-team-hero-ghost gfo-display" aria-hidden="true">@Team.CrestMonogram</span>
    <TeamMark Team="Team" Size="64" Decorative="true" />
    <div>
        @if (Kicker is not null)
        {
            <p class="gfo-team-hero-kicker">@Kicker</p>
        }
        <h1 class="gfo-team-hero-title gfo-display" id="@HeadingID">@(Title ?? $"{Team.City} {Team.Nickname}")</h1>
        @if (Subtitle is not null)
        {
            <p class="gfo-team-hero-subtitle">@Subtitle</p>
        }
    </div>
    @ChildContent
</header>

@code {
    private string _onFill = "";

    [Parameter, EditorRequired] public TeamIdentityDto Team { get; set; } = default!;

    /// <summary>The page's heading; "City Nickname" when not given.</summary>
    [Parameter] public string? Title { get; set; }

    [Parameter] public string? Kicker { get; set; }

    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public string? HeadingID { get; set; }

    /// <summary>What the band carries on its right: chips, a stat, a link.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet() => _onFill = AccentColors.OnAccent(AccentColors.Normalize(Team.PrimaryColor));
}
```

The stylesheet is the band's (Radius L, 28 × 30, `background: var(--gfo-team-fill)`, `color: var(--gfo-team-on-fill)`), with the ghost at 220 px, `opacity: 0.12`, absolutely placed bottom-right with `overflow: hidden` on the band. A CSS rule that reads the variables has no raw hex in it.

**The League page** (`UI/League/LeaguePage.razor`) reads `GetStructureAsync` once. A CSS grid holds the two conferences; each conference is a `FluentStack` of four flat `FluentCard`s; each team row is a `FluentStack` with `TeamMark` (24 px, decorative) and `TeamLink`.

**The Team page** (`UI/Teams/TeamPage.razor`). The pattern every page with an id follows:

```razor
@page "/team/{ID:int}"
@using GridironFrontOffice.Application.Leagues
@using GridironFrontOffice.Application.Players
@inject ISaveManager Saves
@inject LeagueQueries Leagues
@inject PlayerQueries Players
@inject NavigationManager Navigation

@code {
    [Parameter] public int ID { get; set; }

    private int _loaded = -1;
    private TeamSummaryDto? _team;
    private IReadOnlyList<PlayerRowDto>? _roster;
    private bool _missing;

    protected override async Task OnParametersSetAsync()
    {
        if (Saves.Current is null)
        {
            Navigation.NavigateTo(SaveActions.MainMenu, replace: true);
            return;
        }

        if (ID == _loaded)
        {
            return;
        }

        (_loaded, _team, _roster, _missing) = (ID, null, null, false);
        var id = new TeamID(ID);
        // On the thread pool (WP8 §7); a load the next team overtook is dropped
        var team = await Task.Run(() => Leagues.GetTeamSummaryAsync(id));
        var roster = team is null ? [] : await Task.Run(() => Players.GetRosterAsync(id));
        if (ID != id.Value)
        {
            return;
        }

        (_team, _roster, _missing) = (team, roster, team is null);
    }
}
```

The page then shows `TeamHero`, the three roster-strength chips, the four `Stat` tiles, the top five cap hits (`_roster.Where(p => p.CapHit is not null).OrderByDescending(p => p.CapHit).Take(5)`) and `RosterGrid` over `_roster` (slice 2 brings the grid; until then a plain list is fine).

**Home** (`UI/Home/HomePage.razor`, from `League/LeagueHome.razor`): its two redirects move with it, then it reads the header and the user's roster on the thread pool and shows §2's four parts.

### 3.2 Slice 2: players in a grid

**The name** (`UI/Football/PlayerLink.razor`):

```razor
@using GridironFrontOffice.Application.Players
@inject PeekService Peek
@* A player's name (M1 exit: every one opens a peek sheet). A button, styled as a link: it opens the sheet here rather
   than going somewhere. Inside a grid its click doesn't also select the row. *@
<button type="button" class="gfo-player-link" aria-haspopup="dialog" @onclick="OpenAsync" @onclick:stopPropagation="true">@Name</button>

@code {
    [Parameter, EditorRequired] public PlayerID Player { get; set; }

    [Parameter, EditorRequired] public string Name { get; set; } = "";

    private Task OpenAsync() => Peek.OpenPlayerAsync(Player);
}
```

**The peek** (`UI/Players/PeekService.cs`, scoped in `AddGfoUi`):

```csharp
/// <summary>
/// Opens a player's peek sheet over any page (UI_DESIGN_SYSTEM.md §7, M1 exit: every player name opens one). The sheet
/// needs the name for its title, so the player is read first. An unknown player is a toast, not an empty sheet.
/// </summary>
public sealed class PeekService(PlayerQueries players, LeagueQueries leagues, IGfoDialogs dialogs, IGfoToasts toasts)
{
	public async Task OpenPlayerAsync(PlayerID player)
	{
		// On the thread pool: SQLite's async calls complete synchronously (WP8 §7)
		var detail = await Task.Run(() => players.GetPlayerAsync(player));
		if (detail is null)
		{
			await toasts.WarningAsync("That player isn't in this league");
			return;
		}

		var team = detail.Row.TeamID is { } id ? (await Task.Run(() => leagues.GetTeamIdentitiesAsync())).Single(t => t.TeamID == id) : null;
		await dialogs.PeekAsync<PlayerPeekSheet>($"{detail.Row.FirstName} {detail.Row.LastName}",
			new Dictionary<string, object?> { [nameof(PlayerPeekSheet.Player)] = detail, [nameof(PlayerPeekSheet.Team)] = team });
	}
}
```

**The sheet** (`UI/Players/PlayerPeekSheet.razor`) is a `FluentDialogBody` holding §2's facts. **Open full profile** closes the drawer, then navigates: Fluent v5's `DialogInstance` has `CloseAsync()`, and `FluentDialogBody` exposes the `Instance` (the exact way the content reaches it is checked when the slice lands, §7).

**The grid** (`UI/Football/RosterGrid.razor`):

```razor
@using GridironFrontOffice.Application.Leagues
@using GridironFrontOffice.Application.Players
@inject PeekService Peek
<DataGrid TItem="PlayerRowDto" Items="Rows" Label="@Label" ItemKey="@(p => p.PlayerID.Value)" Height="@Height"
          OnItemActivated="@(p => Peek.OpenPlayerAsync(p.PlayerID))">
    <DataGridTemplateColumn TItem="PlayerRowDto" Title="Name" Width="220px" Pinned="true" SortBy="ByName">
        <PlayerLink Player="context.PlayerID" Name="@($"{context.FirstName} {context.LastName}")" />
    </DataGridTemplateColumn>
    <DataGridTemplateColumn TItem="PlayerRowDto" Title="Pos" Width="88px" SortBy="ByPosition">
        <PositionPill Value="context.Position" />
    </DataGridTemplateColumn>
    <DataGridColumn TItem="PlayerRowDto" Title="#" Property="@(p => p.JerseyNumber)" Width="64px" Numeric="true" />
    <DataGridColumn TItem="PlayerRowDto" Title="Age" Property="@(p => p.Age)" Width="64px" Numeric="true" />
    <DataGridTemplateColumn TItem="PlayerRowDto" Title="OVR" Width="76px" Numeric="true" SortBy="ByOverall">
        <RatingChip Value="context.Overall" />
    </DataGridTemplateColumn>
    <DataGridTemplateColumn TItem="PlayerRowDto" Title="Pot" Width="76px" SortBy="ByPotential">
        <PotentialChip Value="context.Potential" />
    </DataGridTemplateColumn>
    <DataGridColumn TItem="PlayerRowDto" Title="Archetype" Property="@(p => p.Archetype)" Width="160px" />
    <DataGridTemplateColumn TItem="PlayerRowDto" Title="Cap hit" Width="112px" Numeric="true" SortBy="ByCapHit">
        @(context.CapHit is { } hit ? Money.Format(hit) : "—")
    </DataGridTemplateColumn>
    @if (Teams is not null)
    {
        <DataGridTemplateColumn TItem="PlayerRowDto" Title="Team" Width="96px" SortBy="ByTeam">
            @* crest and abbreviation, or FA *@
        </DataGridTemplateColumn>
    }
</DataGrid>
```

The sort keys are static `GridSort<PlayerRowDto>.ByAscending(…)` fields, as the gallery's are: by `LastName` then `FirstName`, by `Position`, by `Overall.Estimate`, by `Potential.Grade`, by `CapHit`, and by the team's abbreviation through the dictionary. The parameters: `Rows`, `Label`, `Height` and `Teams` (null for one team's roster).

**The Roster page** (`UI/Players/RosterPage.razor`, D-102 #1). The route carries the choice, so the page reads its query in `OnParametersSetAsync`, as the Team page reads its id:

```razor
@page "/roster"

@code {
    /// <summary>A team's ID, "all", or nothing for the user's team.</summary>
    [SupplyParameterFromQuery(Name = "team")] public string? TeamQuery { get; set; }

    /// <summary>A position's abbreviation, or nothing for every position.</summary>
    [SupplyParameterFromQuery(Name = "position")] public string? PositionQuery { get; set; }

    private IReadOnlyList<PlayerRowDto> _rows = [];   // the team's, or everyone's
    private Position? _position;

    private IEnumerable<PlayerRowDto> Shown => _position is { } p ? _rows.Where(r => r.Position == p) : _rows;
}
```

The team is read again only when it changes; the position only filters. Two `FluentSelect`s sit above the grid in a horizontal `FluentStack` (each `Width="auto"`), labeled "Team" and "Position", and each change navigates to `SaveActions.Roster` with both queries (`replace: true`). `SaveActions` gains a builder for it, beside `WithTeam`. A position is parsed with `Enum.TryParse` and `Enum.IsDefined`, as `SaveCatalog` reads names (CLAUDE.md): `?position=42` is no position. `RosterGrid` gets `Shown`, and `Teams` only for All teams.

### 3.3 Slice 3: the Player page

**The groups** (`Core/Players/AttributeGroups.cs`):

```csharp
/// <summary>Each attribute's group, as F-03 §4.2 sorts them (the Player page shows them in this order).</summary>
public static class AttributeGroups
{
	public static AttributeGroup Of(this PlayerAttribute attribute) => attribute switch
	{
		PlayerAttribute.Speed or PlayerAttribute.Acceleration or PlayerAttribute.Agility or PlayerAttribute.Strength
			or PlayerAttribute.Stamina or PlayerAttribute.Toughness => AttributeGroup.Physical,
		PlayerAttribute.Awareness or PlayerAttribute.Discipline => AttributeGroup.Mental,
		// … Passing (7), Ball carrying (4), Receiving (4), Blocking (3), Defense (10), Special teams (6)
		_ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
	};

	/// <summary>A group's attributes in the table's order (the enum's).</summary>
	public static IReadOnlyList<PlayerAttribute> Members(AttributeGroup group) =>
		[.. Enum.GetValues<PlayerAttribute>().Where(a => a.Of() == group)];
}
```

**The words** (`UI/Football/PlayerText.cs`): a `Text()` for each `PlayerAttribute` ("Throw on the run", "Pass rush (power)"), `AttributeGroup` ("Ball carrying", "Special teams"), `PersonalityTrait` ("Work ethic") and `TraitLevel` ("Low", "Average", "High"), each a `switch` that throws on a value it doesn't know, as `Phases.Text` does. And `Measurable(Measurable, double)`: height in feet and inches (74 → 6'2"), weight in pounds, the timed drills in seconds with two decimals, bench in reps, vertical and arm and hand in inches with one decimal, broad jump in feet and inches.

**The contract** (`UI/Football/ContractText.cs`): `Line(ContractSummaryDto)` (F-12 §8's sentence), and the table's rows from `Seasons.Where(s => s.Season >= current)`.

**The page** (`UI/Players/PlayerPage.razor`) follows the Team page's pattern (`/player/{ID:int}`, `OnParametersSetAsync`, dropped stale loads) and shows §2's blocks. Its band is `TeamHero` with the player's name as `Title` and his team (a free agent's band uses the neutral accent).

### 3.4 The gallery (Claude)

- **`/_gallery/team-hero`:** all 32 teams' bands with a sample title, kicker and subtitle, in both modes: each holds its text at 4.5:1 or better on its own fill, and the ghost reads as texture, not text.
- **`/_gallery/player`:** `PlayerPeekSheet` and the Player page's blocks from sample DTOs: your own player (personality shown), a rival's (Unknown), a free agent (no team, no contract), a rookie and an undrafted player.
- **The DataGrid page** moves onto `RosterGrid` with 2,000 sample rows and the Team column: the budget's reference.

## 4. The tests (Claude writes them)

Per slice, each proven to fail on a planted problem. Screens run on real saves through `RealScreenContext` (D-093).

**Slice 1**
- **Routes and shell** (UI.Tests): the four nav entries in order with the current one active; `/league` is the League page and `/home` is Home, with Home's two redirects; the top bar's crest and team name after the pick, a link to your Team page, and an empty brand before; every caller of the old `LeagueHome` lands on `/home` (the landing, Continue, Load and Start tests move with it).
- **`TeamHeroTests`:** the fill and the on-fill color from the team (white or near-black, as `AccentColors.OnAccent` says), the monogram `aria-hidden`, the title an `h1`; rendered under de-DE.
- **`LeaguePageTests`:** 2 conferences × 4 divisions × 4 teams in the team set's order, each team a link to `/team/{id}` with its crest.
- **`TeamLinkTests`:** the link's `href` and text for a team; every team name the slice's screens show is one (the League page, Home, the top bar), and the Team page's own heading isn't.
- **`TeamPageTests`:** the band and stadium of the team in the route; the four tiles equal the team's summary (utilization to one decimal); the top five cap hits are its five largest, in order; an unknown id shows the empty state; going from one team to another loads the second (the reused page).
- **`HomePageTests`:** your team's band, where the season stands, your five best players by OVR, the space tile.

**Slice 2**
- **`RosterGridTests`:** the columns and their widths; each sort key orders by what it says (name by last then first, OVR by estimate, cap hit with free agents' "—" last); a free agent's row; the Team column only for a mixed list, each cell a `TeamLink` (or "FA") whose click doesn't select the row.
- **`PlayerLinkTests` and `PeekServiceTests`:** a name opens the sheet titled with it; a click on a name in a grid opens the sheet and doesn't select the row; Enter on a row opens it too; an unknown player is a toast; the sheet's facts; Open full profile closes the sheet and lands on `/player/{id}`, and the sheet's team name closes it and lands on `/team/{id}`.
- **`RosterPageTests`:** your team by default (53 active players) with its band; the Team picker lists All teams, then the 32 in order, and the Position picker All positions, then the 16; `?team=all` shows every player who isn't retired (1,846 for seed 42, free agents included) with the Team column; a rival's ID shows that team's 53; `?position=QB` shows only quarterbacks, for one team and for all; a picker's change lands on the route with both choices; going from one team to another reads the second; an unknown team or position (`?team=99`, `?position=42`) falls back to yours, or to all.

**Slice 3**
- **`AttributeGroupsTests`** (Core.Tests): every attribute has a group; the groups hold 6, 2, 7, 4, 4, 3, 10 and 6 in the enum's order; `Members` and `Of` agree.
- **`PlayerTextTests`:** every attribute, group, trait and level has a name of its own; each measurable's format at its edges (74 inches is 6'2", 4.5 s shows two decimals), under de-DE too.
- **`ContractTextTests`:** F-12 §8's line from a known contract; the table starts at this season.
- **`PlayerPageTests`:** all 42 attributes under their eight groups; your player's personality and a rival's "Unknown"; the bio's draft line ("Undrafted" too), with the drafting team a link; the band's team a link, and none for a free agent; the contract table from this season on, and no contract block for a free agent; an unknown id; going from one player to another.

**The budget** (Claude, the desktop app, §2): sort and scroll timings recorded in the plan's WP9 results.

## 5. Done when

**Slice 1: the league and its teams**
- [ ] The routes and `SaveActions`' constants, the nav, the top bar, `TeamHero`, `TeamLink`, Home, the League page and the Team page.
- [ ] Claude: `/_gallery/team-hero`.
- [ ] Claude's slice 1 tests pass, each proven to fail on a planted problem.

**Slice 2: players in a grid**
- [ ] `PlayerLink`, `PeekService` and `PlayerPeekSheet`, `RosterGrid`, the Roster page with its two pickers, and the Team page's roster.
- [ ] Claude: the DataGrid page on `RosterGrid`.
- [ ] Claude's slice 2 tests pass, each proven to fail on a planted problem.

**Slice 3: the Player page**
- [ ] `AttributeGroups`, `PlayerText`, `ContractText` and the Player page.
- [ ] Claude: `/_gallery/player`.
- [ ] Claude's slice 3 tests pass, each proven to fail on a planted problem.

**Close-out (Claude)**
- [ ] Checked in a visible pane, Dark and Light, with several team colors: Home → League → a rival's Team page → a name's peek → Open full profile → the Player page → its team's name, and the Roster through both pickers, then Back. Then the same in the desktop app (D-095 #4).
- [ ] The grid's sort and scroll times, measured and recorded (§2).
- [ ] `./build.ps1` green.
- [ ] Reviewed against this guide, F-02 §8, F-03 §8, F-12 §8, UI_DESIGN_SYSTEM §5.3, §5.4 and §7, VISUAL_DESIGN §6 and §10, and D-100.
- [ ] Docs: F-02, F-03 and F-12's UI sections as built; the plan's WP9 results; UI_DESIGN_SYSTEM §7 (Home is `/home`, the routes, the peek, the top bar), §5.4 (the new composites) and §5.1 (`FluentSelect`); M0_FOUNDATIONS' note that Home is `/league`; CLAUDE.md's gotchas.
- [ ] Committed by you: one commit per slice, each with its tests (D-076), then the docs.

## 6. Order of work

1. **Slice 1.** The routes first (every redirect and test follows the constant), then the nav and the top bar, then `TeamHero`, then the League and Team pages, then Home, which reuses them.
2. **Slice 2.** `PlayerLink` and the peek first (the grid's names need them), then `RosterGrid`, then the Roster page and the Team page's roster.
3. **Slice 3.** `AttributeGroups` and `PlayerText` (pure, tested first), then the page.
4. **Close-out:** Claude's checks, the budget, the docs, then your commits.

## 7. Gotchas

- **A page with an id is reused between ids.** `/team/3` to `/team/7` keeps the same component: load in `OnParametersSetAsync`, keyed on the id, and drop a load the next id overtook. A test goes from one team to another to hold this.
- **Read on the thread pool** (WP8 §7): SQLite's async calls complete synchronously, so a read in `OnParametersSetAsync` without `Task.Run` holds the render thread.
- **Every page needs an `h1`:** the router's `FocusOnNavigate` looks for one.
- **Sort by values that compare:** `PerceivedRating` and typed IDs throw "Failed to compare two elements" in an in-memory sort (WP6 §8). Use `.Estimate` and `.Value`.
- **`DataGrid<T>`'s rules** (UI_DESIGN_SYSTEM §5.3): every column has a `Width` (and `MinWidth` = `Width`), `SortBy` ascending, never `height: 100%` on the grid, and `TItem` set when binding its callbacks. A pill column needs its widest pill plus 36 px.
- **A button inside a row** also reaches the row's click, which selects it. `@onclick:stopPropagation` keeps the name's click to itself; check that Fluent's cell focus and `gfo-grid.js`'s Enter don't open the sheet twice.
- **Close the drawer before navigating.** The drawer lives in the layout's providers, so a navigation leaves it open over the next page. The sheet has two ways out, Open full profile and its team's name, and both close it first.
- **`FluentSelect` is new to GFO.** Spike S0b didn't cover it: check its binding (`Items`, `OptionText`, `OptionValue`, the value and its change) and both modes when slice 2 lands, and add what's non-obvious to UI_DESIGN_SYSTEM §5.1 and CLAUDE.md, as S0b's names were.
- **A link inside a row** reaches the row too: `TeamLink` in the Team column stops propagation, as `PlayerLink` does.
- **Team colors stay out of stylesheets.** They're data: inline custom properties from the read model, as `TeamMark`'s are. The stylesheet only reads the variables.
- **Personality is null for a rival** and for an outsider before the pick (D-092 #2): show "Unknown", never an empty block.
- **`GetPlayerAsync` expects a contract year for this season** (`PlayerQueries`: `Single(c => c.Season == current)`). Every M1 contract has one; a test pins it, so a later change that breaks it fails there, not on the page.
- **The store lags the pick** (WP8 §7): the top bar follows the store, but no page waits on it.
- **New files need a watcher restart** (CLAUDE.md), and a `static readonly` field added during a hot reload stays null.

## 8. What Claude checked (2026-11-09)

| Question | Answer |
|---|---|
| Do the read models cover the screens? | Yes. `PlayerRowDto` has every Roster column (name, position, number, age, OVR, potential, archetype, cap hit); `PlayerDetailDto` has the Player page (measurables, 42 perceived attributes, bio, draft, personality, the contract with every season); `TeamSummaryDto` the Team page's cap and stadium; `GetStructureAsync` the League page; `GetRosterAsync` a team's 53 active players, sorted by position then OVR. Missing, and handled in the UI: a team's identity on a row (§2's dictionary), the attribute groups (§3.3) and every display name (`PlayerText`) |
| Does `DataGrid<T>` do what the Roster needs? | Yes: a sticky header, virtualization, a pinned first column (`Pinned`), the sort header, Enter and double-click (`OnItemActivated`), PageUp/PageDown and Home/End (`gfo-grid.js`), and density from the theme. Spike S0b: 23 DOM rows for 2,000, a sort in 45–52 ms with GFO's header |
| Is there a peek to build on? | `IGfoDialogs.PeekAsync<T>(title, parameters, width)` opens Fluent's drawer from the end and completes when it closes. There's no `PeekService` or player sheet yet (the gallery's `GalleryPeek` stands in). Fluent 5.0.1 has `DialogInstance.CloseAsync()`, and `FluentDialogBody` exposes its `Instance` |
| How many players? | Seed 42 has 1,846 players who aren't retired: the every-player list. The league list read takes 70–160 ms in Debug (WP6) |
| What does `/league` hold today? | M0's Home stub (`League/LeagueHome.razor`), with the two redirects WP8 gave it. `SaveActions.LeagueHome` is used in six places, and three test files name the route |

## 9. Not in WP9

- **A team peek:** team names go to the Team page (D-102 #4).
- **Past contract seasons:** open (D-102 #3), parked in [IDEAS.md](../IDEAS.md).
- **The record, the schedule and results:** with games (M2–M3). Home says "No games played yet".
- **The top bar's week, record and cap pill:** with the season (M2+).
- **Searching the grid, and filters beyond team and position:** WP10's command palette jumps to a player by name; other filters come with the screens that need them.
- **The global keyboard map** (UI_DESIGN_SYSTEM §8: G then R, `/`, `?`): P1. The grid's own keys are M0's.
- **Rating ranges:** σ is 0 until M6, which adds the range to `RatingChip` and `RatingBar` without touching the screens (D-078).
- **Personality descriptors by trait** ("Tireless worker"): the level is enough until a screen needs the words.
- **The depth chart, transactions and any editing:** later milestones.

## 10. Questions (answered 2026-11-09)

Answered together as [D-102](../DECISIONS.md#d-102-the-roster-opens-on-your-team-and-picks-any-team-or-all-of-them-and-a-position-the-top-bar-names-your-team-the-contract-table-runs-from-this-season-every-team-name-links-to-its-page), two as recommended and two further:
1. ~~What does the Roster show?~~ → **Your team by default, with a Team picker (All teams, then the 32) and a Position picker** (your answer, beyond the recommended "All players" switch). All teams is every player.
2. ~~Should the top bar name your team now?~~ → **Yes**, as recommended: the crest and "City Nickname", a link to your Team page, with "League · season · phase".
3. ~~The contract table: which seasons?~~ → **This season and the ones left**, as recommended. Past seasons are open, parked in [IDEAS.md](../IDEAS.md).
4. ~~Team names: a link, or a peek too?~~ → **A link to the Team page, everywhere a team is named** (your answer, beyond the recommendation's places). No team peek.
