# Product Requirements Document: League Setup - Franchise Selection

## 1. Executive Summary

- **Problem Statement**: Users starting a new simulation need to select a team, but currently lack the context (cap health, roster needs) required to make an informed strategic decision.
- **Proposed Solution**: A data-driven "Franchise Selection" interface that pairs visual team selection with a deep-dive "Scouting Report" dashboard, highlighting financial health and roster deficiencies.
- **Success Criteria**:
  - Users can identify a team's top 3 positional needs within 5 seconds of selection.
  - "Cap Space" data is legible and color-coded to prevent accidental selection of salary-cap-hell teams.
  - UI renders the 32-team grid and scouting card with <200ms latency.

## 2. User Experience & Functionality

### User Personas
- **The Rebuilder**: Specifically looks for teams with bad rosters and high draft capital to rebuild from scratch.
- **The Contender**: Wants a "win-now" team and needs to ensure the roster is already elite (85+ OVR).

### User Stories

**Story 1: Team Selection & Inspection**
`As a user, I want to click on a team logo so that I can immediately see their financial health and roster needs.`
- **Acceptance Criteria**:
  - Clicking a team in the "Selection Matrix" updates the "Scouting Report" panel instantly.
  - Selected team displays a `Gridiron Blue (#2563EB)` border.
  - "Scouting Report" persists the last selected team even if the user navigates between Division tabs.

**Story 2: Financial Assessment**
`As a user, I want to see Salary Cap data formatted clearly so I can judge the difficulty of the job.`
- **Acceptance Criteria**:
  - Cap Space and Dead Money figures use **Monospace** font for alignment.
  - Values < $5M are displayed in `Warning Gold (#FFB703)`.
  - Negative values (Cap Overages) are displayed in `Flag Red (#C1121F)`.

**Story 3: Random Selection**
`As a user, I want a "Random Team" button so I can challenge myself with an unknown scenario.`
- **Acceptance Criteria**:
  - Button is located in the header of the Selection Matrix.
  - Clicking triggers a pseudo-random selection of one of the 32 teams.
  - The UI automatically scrolls/tabs to the selected team and highlights it.

### Non-Goals
- **Roster Browsing**: Users cannot view the full 53-man roster at this stage. Only aggregated stats (OVR, Top Needs) are shown to keep the setup flow fast.
- **Uniform Selection**: Visual customization of jerseys is out of scope for v1.0.

## 3. Technical Specifications

### Architecture Overview
- **Component**: `GridironFrontOffice.UI/Pages/NewGame/SetupFranchiseSelection.razor`.
- **State Management**: The component receives the list of teams from `SetupWizard.razor` (Parent) via a cascading parameter or state container.
- **Layer Interaction**:
  - **UI Layer**: Handles the display and user selection events.
  - **Application Layer**: Provides a `GetLeagueContextQuery` that returns `List<TeamSelectionDto>`.
  - **Persistence Layer**: Retrieves aggregated data (Sum of Cap Hits, Average Ratings) via optimized SQLite queries.

### Data Structures (DTOs)
The `TeamSelectionDto` should be lightweight to ensure performance:
```csharp
public record TeamSelectionDto(
    int TeamId,
    string City,
    string Name,
    string LogoPath,
    int OverallRating,
    int OffenseRating,
    int DefenseRating,
    decimal CapSpace,
    decimal DeadMoney,
    List<string> TopNeeds // e.g., ["QB", "LT", "CB"]
);