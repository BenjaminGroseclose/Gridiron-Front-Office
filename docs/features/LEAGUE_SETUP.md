# Product Requirements Document: Initial League Creation & Coach Profile Setup

## 1. Executive Summary

**Problem Statement**: New users lack a structured onboarding flow to set up their first league, define their coaching identity, and configure foundational league settings. Without clear guidance, users waste time on configuration and may create inconsistent or suboptimal league profiles.

**Proposed Solution**: A multi-step "League Setup Wizard" that guides users through (1) coach profile creation, (2) league configuration with customizable settings (roster size, practice squad, injury rules, salary cap), and (3) team selection. Each step captures essential data via form inputs, dropdowns, and toggles, validating inputs before progression and persisting state.

**Success Criteria**:
- League setup completion rate ≥ 85% on first attempt.
- Average setup time ≤ 5 minutes (excluding league overview browsing).
- All required fields validated before proceeding to simulation.
- Setup data persists correctly across application sessions.
- Users can return to any wizard step to edit choices before final confirmation.

---

## 2. User Experience & Functionality

### User Personas

- **The New Player**: First-time user, unfamiliar with football league mechanics. Needs sensible defaults and clear explanations for all settings.
- **The Veteran Simmer**: Experienced user seeking precise control over league configuration. Wants quick setup with minimal handholding.
- **The Casual Manager**: Wants to get into the game quickly without worrying about fine-tuning league rules.

### User Stories

**Story 1: Coach Profile Creation**
```
As a new user, I want to create a coach profile so that I can establish my identity in the simulation and track my coaching record.
```

- **Acceptance Criteria**:
  - Coach name field accepts 2-50 characters (letters, numbers, spaces, hyphens).
  - Experience level dropdown offers: Rookie (0 seasons), Intermediate (1-5 seasons), Veteran (6-10 seasons), Legend (10+ seasons).
  - Optional profile picture upload (supports PNG, JPG, max 5MB).
  - Auto-generated coach ID displayed for reference.
  - Form validates before proceeding to next step.
  - Coach profile data saved to persistent storage immediately after creation.

**Story 2: League Configuration**
```
As a user, I want to configure league-wide settings so that I can customize the simulation rules to match my preferred style of play.
```

- **Acceptance Criteria**:
  - League name field accepts 2-100 characters.
  - Roster size dropdown: 46, 50, 53, 55 (NCAA, Default NFL, Full NFL, Expanded).
  - Practice squad size dropdown: 0, 10, 14, 16 (None, Standard, NFL-2024, Custom).
  - Salary cap input: preset options (Standard $255M, Conservative $200M, Generous $300M) or custom numeric entry.
  - Injury toggle enabled by default; users can disable for "superhuman" mode.
  - Hard cap toggle: when enabled, teams cannot exceed salary cap; when disabled, allows cap overages with penalties.
  - Salary cap floor input: optional minimum spending requirement (default 90% of cap).
  - Starting year selector: 2024-2034, with fallback to next NFL season if invalid.
  - Settings summary card displays all selections before confirmation.

**Story 3: Team Research & Selection**
```
As a user, I want to research all 32 teams before selecting my franchise so that I can make an informed strategic decision based on financial health and roster needs.
```

- **Acceptance Criteria**:
  - 32-team grid displays all NFL teams grouped by conference.
  - Clicking a team updates a persistent scouting report showing: Overall Rating, Offensive Rating, Defensive Rating, Cap Space, Dead Money, Top 3 Positional Needs.
  - Team filter by division or conference available.
  - Random team button selects and displays a random team's scouting report.
  - "Next" or "Continue to Selection" button navigates to formal team selection page.
  - Research selections do not lock in the team; user can browse without commitment.

**Story 4: Wizard Navigation & State Management**
```
As a user, I want to navigate freely between wizard steps and edit my choices before finalizing so that I can correct mistakes without restarting.
```

- **Acceptance Criteria**:
  - Step indicator shows current progress (e.g., "Step 2 of 4: League Configuration").
  - "Back" button navigates to previous step; "Next" button proceeds to next step.
  - All entered data persists when navigating between steps.
  - "Skip" or "Use Defaults" option available for optional settings (e.g., profile picture).
  - Final confirmation screen summarizes all choices with edit links.
  - User can only proceed from each step after required fields are valid.

**Story 5: Form Validation & Error Handling**
```
As a user, I want clear validation messages so that I understand what went wrong and how to fix it.
```

- **Acceptance Criteria**:
  - Invalid inputs display inline error messages (e.g., "Coach name must be 2-50 characters").
  - Numeric fields only accept valid numbers within range.
  - Required fields are marked with a red asterisk (*).
  - Submit button is disabled until all required fields are valid.
  - Success message displayed when league is created successfully.

### Non-Goals

- **Advanced League Customization**: Rule tweaks (e.g., playoff format, draft order) are out of scope for v1.0. League defaults follow NFL standards.
- **Multi-League Support**: Users cannot manage multiple simultaneous leagues in this release. Only one active league per profile.
- **Coach Customization Beyond Profile**: Jersey customization, headset colors, and coaching playbooks are deferred to v1.1.
- **Import/Export**: League configuration cannot be imported from external files or exported for sharing in v1.0.
- **Historical League Cloning**: Users cannot clone a previous league's settings; they must reconfigure each time.

---

## 3. Technical Specifications

### Architecture Overview

**Component Structure**:
- `GridironFrontOffice.UI/Pages/NewGame/LeagueSetupWizard.razor` (Container component managing wizard state and navigation)
- `GridironFrontOffice.UI/Components/Setup/CoachProfileForm.razor` (Step 1)
- `GridironFrontOffice.UI/Components/Setup/LeagueConfigForm.razor` (Step 2)
- `GridironFrontOffice.UI/Components/Setup/LeagueOverview.razor` (Step 3)
- `GridironFrontOffice.UI/Components/Setup/ConfirmationSummary.razor` (Step 4)

**Data Flow**:
1. User inputs → Form validation (client-side)
2. Form inputs → Wizard state container (cascading parameters)
3. Wizard state → Persistence layer (on "Create League" confirmation)
4. Persistence layer → Database (SQL INSERT)

### Data Structures (DTOs)

**LeagueSetupDto** (Aggregates all user inputs):
```csharp
public record LeagueSetupDto(
    // Coach Profile
    int CoachId,
    string CoachName,
    string CoachExperience,      // "Rookie" | "Intermediate" | "Veteran" | "Legend"
    string? ProfilePictureUrl,
    
    // League Configuration
    string LeagueName,
    int RosterSize,              // 46, 50, 53, 55
    int PracticeSquadSize,       // 0, 10, 14, 16
    decimal SalaryCap,           // Dollar amount
    bool InjuriesEnabled,        // true | false
    bool HardCapEnabled,         // true | false
    decimal SalaryCapFloor,      // Percentage (e.g., 0.90 = 90%)
    int StartingYear,            // 2024-2034
    
    // Team Selection (set after research)
    int? SelectedTeamId,
    DateTimeOffset CreatedAt
);

public record CoachProfileDto(
    int CoachId,
    string Name,
    string Experience,
    string? ProfilePictureUrl
);

public record LeagueConfigDto(
    string LeagueName,
    int RosterSize,
    int PracticeSquadSize,
    decimal SalaryCap,
    bool InjuriesEnabled,
    bool HardCapEnabled,
    decimal SalaryCapFloor,
    int StartingYear
);

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
    List<string> TopNeeds
);
```

### Integration Points

**Application Layer** (Queries & Commands):
- `CreateLeagueCommand`: Accepts `LeagueSetupDto`, returns `LeagueId`.
- `GetTeamsForSelectionQuery`: Returns `List<TeamSelectionDto>` for research phase.
- `GetLeagueByIdQuery`: Retrieves existing league config for editing (v1.1).

**Persistence Layer**:
- `LeagueRepository.CreateAsync(LeagueSetupDto)`: Inserts new league and coach profile.
- `TeamRepository.GetAllWithStatsAsync()`: Retrieves all teams with aggregated cap/rating stats.
- `CoachRepository.CreateAsync(CoachProfileDto)`: Inserts coach profile.

**UI Validation**:
- Client-side HTML5 validation for required fields.
- Blazor form binding with `EditForm` and `DataAnnotationsValidator`.
- Custom validators for coach name format and numeric ranges.

### Preset Configuration Values

**Roster Sizes** (dropdown):
- 46 (NCAA Practice Squad)
- 50 (Development Squad)
- 53 (Full NFL Roster) ← Default
- 55 (Expanded Roster)

**Practice Squad Sizes** (dropdown):
- 0 (No Practice Squad)
- 10 (Standard)
- 14 (NFL Current) ← Default
- 16 (Expanded)

**Salary Cap Presets**:
- Conservative: $200M
- Standard: $255M ← Default
- Generous: $300M
- Custom: User-entered value

**Years Available**: 2024, 2025, 2026, 2027, 2028, 2029, 2030, 2031, 2032, 2033, 2034

### Client-Side Validation Rules

| Field | Type | Min | Max | Required |
|-------|------|-----|-----|----------|
| Coach Name | Text | 2 | 50 | Yes |
| League Name | Text | 2 | 100 | Yes |
| Roster Size | Select | - | - | Yes |
| Practice Squad | Select | - | - | Yes |
| Salary Cap | Decimal | 100M | 500M | Yes |
| Starting Year | Select | 2024 | 2034 | Yes |
| Salary Cap Floor | Decimal | 0% | 100% | No (default 90%) |

### Error Handling & Edge Cases

- **Duplicate League Names**: Warn user but allow (different coach profile). Global uniqueness not enforced in v1.0.
- **Missing Profile Picture**: Optional; use generic coach avatar placeholder.
- **Invalid Year Selection**: If user selects past year, default to current season.
- **Salary Cap <= Salary Cap Floor**: Show validation error "Cap floor cannot exceed total cap."

---

## 4. Risks & Roadmap

### Phased Rollout

**MVP (v1.0)**: Basic league setup with required fields
- Coach name + experience level
- League name + roster/practice squad/salary cap/injuries
- Team selection
- Data persists to database

**v1.1**: Enhanced configuration
- Coach profile picture upload
- Custom salary cap floor editing
- Ability to edit league settings post-creation
- Hard cap enforcement in simulation

**v1.2**: Advanced features
- League template system (e.g., "Rebuilds Only", "Win-Now Challenge")
- Multi-league support
- Import/export league configurations
- Preset coaching philosophies (e.g., "Balanced", "Offensive", "Defensive")

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|-----------|
| Form validation latency on large selections | Low | Medium | Client-side validation only; no server calls during form entry. |
| Data loss on browser crash mid-setup | Medium | High | Auto-save to session storage every 10 seconds. Restore on reload. |
| Salary cap calculation errors | Medium | High | Unit test all cap calculations. Use `decimal` type for money. |
| Team data inconsistency (stale ratings) | Low | Medium | Cache team stats with 1-hour TTL. Display "Last updated" timestamp. |
| Mobile responsiveness on wizard | Medium | Medium | Test on tablets (9-13 inch) and phones. Stack form fields vertically. |

### Data Persistence Strategy

- **Auto-Save**: Wizard state saved to browser session storage every 10 seconds.
- **Manual Save**: Only on "Create League" confirmation. Writes to SQL database.
- **Recovery**: If user closes wizard mid-setup, session is discarded (no incomplete leagues in DB).
- **State Validation**: On app load, verify league state integrity. If corrupt, rollback to last valid save.

### Performance Targets

- **Form Rendering**: <300ms (Blazor component init + layout).
- **Team Grid Display**: <500ms (load all 32 teams with stats).
- **Scouting Report Update**: <200ms (clicking team to show report).
- **League Creation**: <2 seconds (insert coach + league + seed initial data).

---

## 5. User Interface Mockup (Text Description)

### Step 1: Coach Profile
```
┌─────────────────────────────────────┐
│  League Setup Wizard  [Step 1 of 4] │
├─────────────────────────────────────┤
│  Coach Profile                      │
│                                     │
│  Coach Name*              [___________] │
│  ℹ️ 2-50 characters                  │
│                                     │
│  Experience*              [Dropdown] │
│  • Rookie                           │
│  • Intermediate                     │
│  • Veteran                          │
│  • Legend                           │
│                                     │
│  Profile Picture (Optional)         │
│  [Choose File] or Drag & Drop       │
│                                     │
│  ┌────────────────┐ ┌────────────┐  │
│  │  ← Back        │ │  Next →    │  │
│  └────────────────┘ └────────────┘  │
└─────────────────────────────────────┘
```

### Step 2: League Configuration
```
┌──────────────────────────────────────┐
│  League Setup Wizard  [Step 2 of 4]  │
├──────────────────────────────────────┤
│  League Configuration                │
│                                      │
│  League Name*           [___________] │
│                                      │
│  Roster Size*           [Dropdown]   │
│  ℹ️ Max players on active roster      │
│                                      │
│  Practice Squad Size*   [Dropdown]   │
│  ℹ️ Reserve roster capacity            │
│                                      │
│  Salary Cap*            [Dropdown]   │
│  ☑ Conservative ($200M)              │
│  ◉ Standard ($255M)                  │
│  ☑ Generous ($300M)                  │
│  ☑ Custom [___________]              │
│                                      │
│  Enable Injuries?       [Toggle ON]  │
│  Hard Cap Enforcement?  [Toggle OFF] │
│  Salary Cap Floor       [0.90]       │
│  ℹ️ Min spending requirement (% of cap) │
│                                      │
│  Starting Year*         [Dropdown]   │
│  2024 - 2034                         │
│                                      │
│  ┌────────────────┐ ┌────────────┐   │
│  │  ← Back        │ │  Next →    │   │
│  └────────────────┘ └────────────┘   │
└──────────────────────────────────────┘
```

### Step 3: League Overview & Team Research
```
┌──────────────────────────────────────┐
│  League Setup Wizard  [Step 3 of 4]  │
├──────────────────────────────────────┤
│  Select Your Franchise               │
│  [Random Team] [Filter by Division]  │
│                                      │
│  ┌─────┬─────┬─────┬─────┐           │
│  │ AFC │ AFC*│ AFC │ AFC* │           │
│  │East │East │ East│East │           │
│  ├─────┴─────┴─────┴─────┤           │
│  │ ┌──┐ ┌──┐ ┌──┐ ┌──┐   │           │
│  │ │PA│ │NE│ │BUF│ │MIA│  │           │
│  │ └──┘ └──┘ └──┘ └──┘   │           │
│  │ ... [28 more teams]   │           │
│  └─────────────────────────┘           │
│                                      │
│  ┌──── Scouting Report ────┐          │
│  │ New England Patriots    │          │
│  │ Overall: 78 | Off: 76  │          │
│  │ Def: 80                │          │
│  │ Cap Space: $15.2M      │          │
│  │ Dead Money: $8.5M      │          │
│  │ Top Needs: CB, RB, WR  │          │
│  └────────────────────────┘          │
│                                      │
│  ┌────────────────┐ ┌────────────┐   │
│  │  ← Back        │ │  Next →    │   │
│  └────────────────┘ └────────────┘   │
└──────────────────────────────────────┘
```

### Step 4: Confirmation Summary
```
┌──────────────────────────────────────┐
│  League Setup Wizard  [Step 4 of 4]  │
├──────────────────────────────────────┤
│  Review & Confirm                    │
│                                      │
│  Coach Profile            [Edit]     │
│  • Name: John Smith                  │
│  • Experience: Veteran               │
│                                      │
│  League Configuration     [Edit]     │
│  • League: My First League            │
│  • Roster: 53 | Practice Sq: 14       │
│  • Salary Cap: $255M                 │
│  • Injuries: Enabled                 │
│  • Hard Cap: Disabled                │
│                                      │
│  Team Selection           [Edit]     │
│  • New England Patriots               │
│                                      │
│  ┌──────────────────┐  ┌──────────┐  │
│  │  ← Edit Details  │  │  Start!  │  │
│  └──────────────────┘  └──────────┘  │
│                                      │
│  [✓ I've read the rules]              │
└──────────────────────────────────────┘
```

---

## 6. Success Metrics & Analytics

- **Funnel Metrics**:
  - Step 1 Completion: % of users who create a coach profile
  - Step 2 Completion: % who configure league settings
  - Step 3 Completion: % who research and select a team
  - Step 4 Completion: % who confirm and create league

- **Performance Metrics**:
  - Average time per step
  - Form submission errors per step
  - Back-button usage (indicator of confusion?)

- **Engagement Metrics**:
  - Return rate: % of users who start a second league
  - Configuration choices: Most popular roster/cap combinations

---

## 7. Testing Strategy

### Unit Testing
- Coach name validation (2-50 chars, no special chars)
- Salary cap calculations (floor vs. total)
- Year selection (valid range 2024-2034)

### Integration Testing
- Coach profile creation flow (database insert)
- League data persistence across wizard steps
- Team data retrieval and filtering

### UAT (User Acceptance Testing)
- Test on desktop (Chrome, Firefox, Edge) and tablet (iOS, Android)
- Verify form validation messages are clear
- Confirm state recovery after browser crash

### Acceptance Criteria Verification
- [ ] All required fields validated before progression
- [ ] State persists across wizard steps
- [ ] Salary cap logic prevents invalid configurations
- [ ] Team research data displays within SLA (<500ms)
