# F-02: League Structure & Team Identity

> **Status:** Accepted (owner review 2026-10-15, [D-064](../DECISIONS.md#d-064-f-02-team-set-accepted-shared-city-labels)). Names and colors may still change if the §4.3 data tests flag them. · **Milestone:** M1 · **Owner:** Ben · **Last updated:** 2026-10-15
>
> Decisions this spec implements: [D-033](../DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames) (curated teams, NFL footprint, original names), [D-035](../DECISIONS.md#d-035-fictional-colleges-teams-and-colleges-as-data-files-user-import-in-m11) (data files, M11 import), [D-014](../DECISIONS.md#d-014-fixed-32-team-league-structure-for-10) (fixed structure), [D-031](../DECISIONS.md#d-031-visual-direction-stadium-light--dark-dark-by-default) (Stadium visuals).

---

## 1. Summary

The league is 32 fictional franchises in the same metros as the real league, organized into 2 conferences of 4 divisions. Each franchise has a hand-written identity: a nickname rooted in its city, original colors, a generated SVG crest and a stadium. The goal is a league that feels like it has existed for decades, so rivalries and history mean something from the first season. Serves pillars **4 (modern and elegant)** and **5 (your league, your story)** ([ONE_PAGER.md](../ONE_PAGER.md#design-pillars)).

## 2. Goals / non-goals

**Goals (M1)**
- The default team set (Appendix A) in a JSON data file with a published schema.
- Structure: 2 conferences × 4 divisions × 4 teams, aligned by geography.
- The `TeamMark` crest component, generated from per-team parameters.
- Identity guardrails enforced by tests (§4.3).
- Team identity copied into the save at creation (a save is self-contained).
- Screens: League (all teams by conference and division), and the Team page header (hero band, crest, colors).

**Non-goals**
- User import of a custom team set (M11, F-01).
- Team editing, relocation, expansion (F-28 or later).
- Any gameplay effect from stadium roof, surface or climate (post-1.0 weather).
- "Randomize identities" ([IDEAS.md](../IDEAS.md)).

## 3. Player stories
- As the GM, I want every team to have a distinct, believable identity so the league feels real and rivalries feel earned.
- As the GM, I want to recognize a team instantly by crest and colors in grids, standings and the scorebug.
- As the GM, I want my team's colors to tint the app, readable in both light and dark themes.
- As a player who wants real teams, I want to import my own team set later (M11), without the game shipping anything real.

## 4. Rules

### 4.1 Structure

| Rule | Detail | Test |
|---|---|---|
| Fixed shape | 2 conferences × 4 divisions (East, North, South, West) × 4 teams = 32 | `League_Structure_Is32Teams2x4x4` |
| Conference names | **Heritage Conference** and **Frontier Conference** (original; accepted for the base set. An imported set can rename conferences and divisions) | — |
| Geographic alignment | Divisions group nearby metros. Each conference spans the country. | Review |
| Two-team metros split | New York and Los Angeles each have two teams, one per conference | `League_SharedMetros_SplitAcrossConferences` |
| Shared city, distinct identity | Teams that share a city label have different abbreviations, monograms and primary colors | `Teams_SharedCity_DistinctAbbrMonogramColor` |
| Unique abbreviations | 2–3 uppercase letters, unique league-wide | `Teams_Abbreviations_Unique` |
| Division rivalries | Mean the schedule only (6 division games) until storylines in M10 | — |

### 4.2 Team identity fields

| Field | Rule |
|---|---|
| `City` | A real metro label. Two-team metros **share** the label ("New York", "Los Angeles"), like the real league. Nickname and abbreviation tell them apart. |
| `Nickname` | Original, plural, classic and rooted in place (tone chosen by the owner, 2026-10-14). |
| `Abbreviation` | 2–3 letters (§4.1). |
| `Colors` | `Primary`, `Secondary`, `Accent` as hex. The UI derives on-colors and the accent tint ([VISUAL_DESIGN.md](../VISUAL_DESIGN.md)). |
| `Crest` | `Shape`, `Monogram`, `Stripe` (§4.4). |
| `Stadium` | `Name`, `Capacity`, `Roof` (Open / Dome / Retractable), `Surface` (Grass / Turf), `ClimateZone` (Cold / Temperate / Hot). Flavor only in 1.0. |

### 4.3 Guardrails (each one is a test against the data file)

| Guardrail | Detail | Test |
|---|---|---|
| No real pro football names | Every nickname is checked against a blocklist of pro football team names, past and present: NFL, AFL, AAFC, WFL, USFL (all eras), XFL (all eras), UFL (all eras), AAF, CFL, World League / NFL Europe, and arena and indoor leagues | `Teams_Nicknames_NotInFootballBlocklist` |
| No same-metro major-league names | A nickname can't match a **current** NBA, MLB, NHL, MLS, WNBA or NWSL team in the same metro | `Teams_Nicknames_NoSameMetroMajorLeagueName` |
| No echo of the real team's colors | Each metro lists the real NFL team's colors. A team's primary/secondary pair can't sit within a ΔE (CIEDE2000) threshold of that pair | `Teams_Colors_DoNotEchoMetroNflTeam` |
| Distinct within a division | The four primaries in a division are pairwise ≥ ΔE 20 apart | `Teams_Colors_DistinctWithinDivision` |
| Readable | Text on each primary meets WCAG AA in both themes, using the derived on-color | `Teams_Colors_OnColorContrast` |
| Distinct crests | No two teams in a division share the same `Shape` + `Stripe` pair | `Teams_Crests_DistinctWithinDivision` |
| No real stadium names | Stadium names are checked against a list of current and former pro stadium names | `Stadiums_Names_NotInBlocklist` |
| Sensitive names | No nickname uses Confederate, Indigenous or other cultural imagery | Review checklist |

The blocklists live in `data/blocklists/` as plain JSON lists, compiled once (team and stadium names are facts) and extended whenever a collision is found. *The draft names in Appendix A were checked by hand against the leagues above. Running the automated test may still force a swap or two.*

### 4.4 Crest system (`TeamMark`)

A small parametric SVG, so 32 crests look like one family and can be read at 16 px:

| Parameter | Values |
|---|---|
| `Shape` | Shield, Roundel, Hexagon, Diamond, Banner |
| `Monogram` | 1–3 characters, Archivo condensed 800 ([VISUAL_DESIGN.md](../VISUAL_DESIGN.md)) |
| `Stripe` | None, Bar, TwinBars, Chevron, Sash (one stripe, two stripes; the code can't use "Single" and "Double", which are .NET type names) |
| Colors | Field = Primary, stripe = Secondary, monogram = Accent (or the on-color when the accent fails contrast) |

- Rendered by `TeamMark` (in `GridironFrontOffice.UI`) at the sizes used in grids (16–20 px), cards (32–48 px) and hero bands (the large "ghost" monogram).
- **Imported logos (M11)** are rendered as `<img>` (or sanitized) and never inlined, because SVG can carry script ([D-033](../DECISIONS.md#d-033-default-teams-32-curated-franchises-real-cities-original-nicknames)).
- Replaces the placeholder crest in [VISUAL_DESIGN.md](../VISUAL_DESIGN.md).

## 5. Data model

**Data file:** `data/league/teams.json` (embedded resource), with a JSON Schema at `data/league/teams.schema.json`. The schema is **public**, because M11 import validates against it. Neither has a version number: the format only grows, and a field added later is optional ([D-077](../DECISIONS.md#d-077-data-files-carry-no-version-numbers-they-change-only-additively)).

```json
{
  "name": "Default",
  "conferences": [
    { "key": "HER", "name": "Heritage Conference",
      "divisions": [ { "key": "HER-E", "name": "East", "teams": ["NYX", "PHI", "WAS", "BAL"] } ] }
  ],
  "teams": [
    { "abbr": "NYX", "city": "New York", "nickname": "Excelsiors", "metro": "new-york",
      "colors": { "primary": "#A6192E", "secondary": "#2B2E34", "accent": "#C9CDD2" },
      "crest": { "shape": "Shield", "monogram": "NY", "stripe": "TwinBars" },
      "stadium": { "name": "Skyline Field", "capacity": 82500, "roof": "Open", "surface": "Grass", "climate": "Cold" } }
  ]
}
```

**Save tables** (copied from the data file at creation, [PERSISTENCE.md §5.2](../architecture/PERSISTENCE.md#52-league-structure)):
- `Conferences (ConferenceID, Key, Name)`, `Divisions (DivisionID, ConferenceID, Key, Name)`
- `Teams (TeamID, DivisionID, Abbreviation, City, Nickname, Metro, PrimaryColor, SecondaryColor, AccentColor, CrestShape, CrestMonogram, CrestStripe, StadiumID, IsUserTeam)`
- `Stadiums (StadiumID, Name, Capacity, Roof, Surface, ClimateZone)`
- `SaveInfo.TeamSetName` (which data set the league came from: "Default", or an imported set's name in M11)

**Read models:** `TeamIdentityDto` (ID, abbreviation, city, nickname, colors, crest), `TeamSummaryDto` (+ conference, division, record later), `LeagueStructureDto` (conferences → divisions → teams).

## 6. Application services

| Service | Commands | Queries | Domain events |
|---|---|---|---|
| `Leagues/LeagueGenerator` (part of `CreateLeague`) | Copies the team set into the save, inside the creation command | — | `LeagueCreated` |
| `Leagues/LeagueStructureQueries` | — | `GetLeagueStructure()`, `GetTeamIdentity(teamID)`, `GetAllTeamIdentities()` (cached per session; identities never change in 1.0) | — |
| `Leagues/TeamSetLoader` (Infrastructure port `ISeedData`) | — | Loads and validates `teams.json` against the schema (the same validator M11 import uses) | — |

## 7. AI counterpart
None. Structure and identity are static data. (Every team, user or AI, is defined the same way.)

## 8. UI

- **League page** (`/league`): 2 conference columns × 4 division cards, each team row showing crest, city + nickname and (from M3) record. Every team name is a link to its Team page and opens a peek on click.
- **Team page header:** the **hero band** in the team's accent with the ghost monogram ([VISUAL_DESIGN.md](../VISUAL_DESIGN.md)), crest, city and nickname, and the stadium as a subtitle.
- **New League wizard, team selection** (F-01): a grid of all 32 cards (crest, name, and from M5 the inherited situation).
- **Theme tint:** taking over a team re-tints the app through `GfoThemeService` (accent from Primary, contrast-adjusted).
- **Gallery:** a `/_gallery/teams` page renders all 32 crests at 16, 32 and 96 px in both themes. It's the manual QA view for §4.3.
- Keyboard: the League page is a grid of links in reading order, with no custom key handling needed.

## 9. Calendar and phase integration
None. Identity is fixed at creation.

## 10. Edge cases
- **Two teams in one metro:** both use the same city label, as in the real league. Anywhere a city alone would be ambiguous, the UI shows the nickname or the abbreviation: the scorebug and dense grids use abbreviations (NYX/NYS, LAC/LAN), and lists use "City Nickname". Pickers and search never show the city alone.
- **Monogram collisions in a division** (e.g., Boston and Buffalo): use a 2–3 letter monogram (`BUF`). The test only requires distinct shape + stripe, but review should also catch look-alike monograms.
- **Imported sets (M11):** must have exactly 32 teams in a 2 × 4 × 4 structure (D-014). Validation errors name the field and team. Blocklist checks are **not** applied to imported sets (the user's own content), but the schema, contrast fixes and the SVG safety rule are.

## 11. Logging
- Information: `TeamSetLoaded {Name} {TeamCount}` at league creation.
- Warning: a team color failed contrast and fell back to a derived on-color (data issue to fix).
- Event IDs: 1200–1219.

## 12. Performance
- Loading and validating the team set: < 20 ms. Identities are cached per session and served from memory.
- `TeamMark` renders as inline SVG from parameters with no image requests. 32 crests on the League page render in < 16 ms.

## 13. Test plan
- **Unit (Core):** structure invariants (§4.1).
- **Data tests:** every guardrail in §4.3 runs against `teams.json`, plus schema validation.
- **bUnit:** `TeamMark` renders each shape and stripe, falls back to the on-color when the accent fails contrast, and exposes an accessible name ("New York Excelsiors").
- **Golden:** the League page snapshot (Verify) in both themes.
- **Autoplay:** no impact.

## 14. Open questions (for owner review)
*All resolved in the 2026-10-15 review ([D-064](../DECISIONS.md#d-064-f-02-team-set-accepted-shared-city-labels)):*
1. ~~Two-team metro labels~~ → shared "New York" and "Los Angeles", like the real league.
2. ~~Conference names~~ → Heritage and Frontier for the base set (most players are expected to import their own real-league set in M11).
3. ~~Changes to the 32~~ → none for now.

## 15. Decisions made while specifying
- [D-064](../DECISIONS.md#d-064-f-02-team-set-accepted-shared-city-labels): the default team set is accepted, with shared city labels for the two-team metros and Heritage/Frontier conference names.

---

## Appendix A: The default team set

This is `data/league/teams.json` in table form; the file wins if they ever disagree. Every team passes the §4.3 tests. "Avoids" names the real NFL team colors in that metro.

### Heritage Conference

| Div | Abbr | Team | Rooted in | Colors (P / S / A) | Avoids | Crest | Stadium (roof, cap.) |
|---|---|---|---|---|---|---|---|
| East | NYX | **New York Excelsiors** | "Excelsior", the state motto | Crimson `#A6192E` / Charcoal `#2B2E34` / Silver `#C9CDD2` | Royal blue + red, green | Shield · NY · TwinBars | Skyline Field (Open, 82,500) |
| East | PHI | **Philadelphia Continentals** | The Continental Congress and Army | Colonial blue `#1F3F7A` / Buff `#D8C39A` / White `#FFFFFF` | Midnight green, silver, black | Roundel · P · Bar | Founders Field (Open, 68,000) |
| East | WAS | **Washington Statesmen** | The capital | Forest `#1F5B3F` / Marble `#ECE6D6` / Bronze `#9C7A4A` | Burgundy + gold | Banner · W · Chevron | Federal Park (Open, 67,000) |
| East | BAL | **Baltimore Privateers** | The War of 1812 privateers | Cannon grey `#4C4C4D` / Rust `#C2562B` / Sailcloth `#F0E6D2` | Purple, black, gold | Diamond · B · Sash | Harborview Stadium (Open, 70,500) |
| North | CHI | **Chicago Zephyrs** | The Windy City and its famous Zephyr train | Flag blue `#2A8FC4` / Star red `#D7263D` / White `#FFFFFF` | Navy + orange | Hexagon · C · Bar | Lakefront Field (Open, 61,500) |
| North | GB | **Green Bay Voyageurs** | The French voyageurs who reached the bay | Oxblood `#7A1F1F` / Birch `#E9E0CC` / Pine `#2E4A3A` | Green + gold | Shield · GB · Chevron | Bayshore Field (Open, 78,000) |
| North | MIN | **Minneapolis Millers** | The Mill City's flour mills | Mill iron `#343A40` / Flour `#F4F1EA` / Wheat `#D98E04` | Purple + gold | Roundel · M · TwinBars | Mill City Dome (Dome, 66,800) |
| North | KC | **Kansas City Drovers** | The cattle drives and stockyards | Saddle `#6B481D` / Hide tan `#E7D7B5` / Prairie sky `#2C5F8A` | Red + gold | Banner · KC · Sash | Stockyard Stadium (Open, 76,400) |
| South | ATL | **Atlanta Railmen** | Atlanta began as Terminus, a railroad junction | Safety orange `#D9541E` / Steel `#2F3E46` / White `#F2F2F2` | Red + black | Diamond · A · Chevron | Terminus Dome (Retractable, 71,000) |
| South | CHA | **Charlotte Prospectors** | The first U.S. gold rush and the Charlotte Mint | Nugget gold `#B88A1B` / Deep pine `#1E3B2F` / Cream `#F5F0E1` | Black, process blue, silver | Shield · C · Sash | Queen City Stadium (Open, 74,800) |
| South | NSH | **Nashville Longhunters** | Tennessee's 18th-century longhunters | Hunter green `#4E6B3A` / Buckskin `#C9A36B` / Dark brown `#2B1E16` | Navy, light blue, red | Hexagon · N · TwinBars | Cumberland Field (Open, 69,000) |
| South | JAX | **Jacksonville Commodores** | The city's naval stations | Navy `#1B2A4A` / White `#E8EDF2` / Signal red `#B22234` | Teal, gold, black | Roundel · J · Bar | St. Johns Stadium (Open, 67,800) |
| West | LAC | **Los Angeles Condors** | The California condor | Sunset orange `#E07A2F` / Night `#1F2E4A` / Sand `#F4E1C1` | Royal blue + yellow (and the other LA team's powder blue + gold) | Roundel · LA · Sash | Sunset Bowl (Open, 70,000) |
| West | SF | **San Francisco Sea Lions** | The sea lions of the bay | Bay blue `#2E6F8E` / Fog `#B8C4CC` / Ink `#1B1F24` | Red + gold | Shield · SF · Bar | Bayside Stadium (Open, 68,500) |
| West | SEA | **Seattle Steelheads** | Washington's state fish | Steelhead rose `#A33B57` / Silver `#D6DEE3` / Rain slate `#23343F` | Navy, action green, wolf grey | Diamond · S · TwinBars | Sound Field (Open, 68,000) |
| West | LV | **Las Vegas Sidewinders** | The Mojave desert | Turquoise `#1F9E94` / Sand `#E3C9A0` / Charcoal `#2B2B2B` | Silver + black | Hexagon · LV · Chevron | Mojave Dome (Dome, 65,000) |

### Frontier Conference

| Div | Abbr | Team | Rooted in | Colors (P / S / A) | Avoids | Crest | Stadium (roof, cap.) |
|---|---|---|---|---|---|---|---|
| East | NYS | **New York Shipwrights** | The Brooklyn Navy Yard, near their Kings County stadium | Ship oak `#60452C` / Cream `#F3E9D7` / Black `#1A1A1A` | Green, royal blue + red, black + white | Shield · S · Bar | Kings County Field (Open, 72,000) |
| East | BOS | **Boston Ironsides** | USS *Constitution*, "Old Ironsides", berthed in Boston | Hull black `#1C1C1C` / White `#F2F2F2` / Copper `#B87333` | Navy, red, silver | Banner · B · TwinBars | Harbor Point Stadium (Open, 65,900) |
| East | BUF | **Buffalo Canalers** | The Erie Canal's western terminus | Canal green `#3B6E4F` / Towpath tan `#C9B37E` / Ink `#1D2A33` | Royal blue + red | Hexagon · BUF · Chevron | Erie Field (Open, 71,600) |
| East | PIT | **Pittsburgh Forgemen** | The steel mills | Furnace red `#B23A1E` / Iron grey `#5A5F66` / White `#EDEDED` | Black + gold (all three local teams) | Diamond · P · Sash | Confluence Field (Open, 68,400) |
| North | DET | **Detroit Motormen** | The Motor City | Gunmetal `#2F2F36` / Signal amber `#E0A100` / Chrome `#C0C7CE` | Honolulu blue + silver | Hexagon · D · TwinBars | Motor City Dome (Dome, 65,000) |
| North | CLE | **Cleveland Freighters** | The Great Lakes ore freighters | Lake blue `#2C5E7A` / Ore rust `#9E4A2F` / Off-white `#F0EDE6` | Brown + orange | Shield · C · Sash | Lakeshore Stadium (Open, 67,400) |
| North | CIN | **Cincinnati Rivermen** | The Ohio River trade | River green `#2F5D3A` / Buff `#E8D9B5` / Maroon `#7A2E2E` | Orange + black | Roundel · CIN · Bar | Queensgate Field (Open, 65,500) |
| North | IND | **Indianapolis Drivers** | The city's racing heritage | Racing red `#C1121F` / Black `#111111` / White `#FFFFFF` | Royal blue + white | Banner · I · Chevron | Crossroads Dome (Retractable, 67,000) |
| South | MIA | **Miami Tarpons** | Tarpon fishing in Biscayne Bay | Deep sea `#093B70` / Tarpon silver `#A9B8C3` / Coral `#F26B4A` | Aqua + orange | Roundel · M · Sash | Biscayne Stadium (Open, 65,300) |
| South | TPA | **Tampa Captains** | The port and Gulf shipping | Sun gold `#E1AD21` / Palm green `#1F4E3D` / White `#FFFFFF` | Red + pewter | Banner · T · Bar | Bayfront Stadium (Open, 69,200) |
| South | NO | **New Orleans Crescents** | The Crescent City | Mardi Gras purple `#642683` / Green `#2E8B57` / Gold `#E3B505` | Black + old gold | Diamond · NO · Chevron | Crescent Dome (Dome, 73,000) |
| South | HOU | **Houston Riggers** | The Gulf's offshore oil rigs | Crude black `#2A2A2A` / Flame `#E86A10` / White `#F5F5F5` | Steel blue + red (and the old Columbia blue + red) | Shield · H · TwinBars | Bayou Dome (Retractable, 72,200) |
| West | LAN | **Los Angeles Navigators** | The twin ports of Los Angeles and Long Beach | Harbor teal `#136F72` / Cream `#F7F3EA` / Deep harbor `#0B2B30` | Royal blue + yellow, powder blue + gold | Diamond · N · Bar | Harbor Gate Stadium (Open, 62,000) |
| West | PHX | **Phoenix Javelinas** | The Sonoran Desert's javelina | Copper `#B8651D` / Sand `#F0D9B5` / Charcoal `#2B2B2B` | Cardinal red, black, white | Banner · P · TwinBars | Saguaro Dome (Retractable, 63,400) |
| West | DEN | **Denver Bighorns** | The Rocky Mountain bighorn, Colorado's state animal | Mountain sky `#396EA5` / Snow `#EEF2F3` / Alpine green `#2E5E4E` | Orange + navy | Shield · D · Chevron | Summit Field (Open, 76,100) |
| West | DAL | **Dallas Marshals** | The frontier lawmen | Black `#1E1E1E` / Badge gold `#C8A04B` / Cream `#F2EFE6` | Navy, silver, royal blue | Hexagon · DAL · Sash | Trinity Dome (Retractable, 80,000) |

### Changes from the first draft (2026-10-20, M1 WP2)
The first run of the §4.3 checks changed the draft, as D-064 allows:
- **Four names** were already in pro football: Atlanta **Aviators** and New York **Kings** (Columbus Aviators and Louisville Kings, UFL 2026), Houston **Wildcatters** (indoor teams in West Texas and Lafayette) and Phoenix **Scorpions** (Florida Scorpions, indoor). They became the **Railmen**, the **Shipwrights** (`NYK` → `NYS`, monogram `K` → `S`), the **Riggers** and the **Javelinas**. Colors and crests stayed, except the Shipwrights' primary below.
- **One stadium:** Empire Field was the BC Lions' home in 2010–11, so the Excelsiors play at **Skyline Field**.
- **Eight primaries** moved, each the smallest change that passes (ΔE₀₀ is how far it moved):

| Team | Was | Now | Moved | Why |
|---|---|---|---|---|
| PHX | `#B5651D` | `#B8651D` | 0.8 | Contrast 4.48:1, under 4.5 |
| DEN | `#3C6E9F` | `#396EA5` | 0.8 | 19.5 from LAN |
| NO | `#5B2C83` | `#642683` | 2.3 | 18.0 from MIA |
| MIA | `#12355B` | `#093B70` | 3.4 | 16.9 from HOU |
| KC | `#6B4226` | `#6B481D` | 5.9 | 14.7 from GB |
| DET | `#2F3B45` | `#2F2F36` | 6.9 | 13.8 from CLE |
| BAL | `#3D4F5C` | `#4C4C4D` | 8.6 | 12.1 from PHI |
| NYS | `#8A4B32` | `#60452C` | 12.0 | 9.5 from PIT, and too close to NYX across town |

"Captains" and "Navigators" passed the blocklist. Nothing here is legal clearance; that's the M11 trademark check ([Q-22](../OPEN_QUESTIONS.md#q-22-name--trademark-check)).
