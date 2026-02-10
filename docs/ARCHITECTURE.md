# Gridiron Front Office - Application Architecture

## Overview

This document describes the layered architecture of the Gridiron Front Office Pro Football simulation game. The application is built using .NET 10 with a clean architecture approach that separates concerns across multiple projects, enforced through project references.

**Target Platform**: Windows Desktop only (using .NET MAUI Blazor as the desktop host)

## Project Structure

```
GridironFrontOffice/
├── GridironFrontOffice.sln
├── src/
│   ├── Domain/                          (Class Library - net10.0)
│   │   └── GridironFrontOffice.Domain.csproj
│   ├── Simulation/                      (Class Library - net10.0)
│   │   └── GridironFrontOffice.Simulation.csproj
│   ├── Application/                     (Class Library - net10.0)
│   │   └── GridironFrontOffice.Application.csproj
│   ├── Persistence/                     (Class Library - net10.0)
│   │   └── GridironFrontOffice.Persistence.csproj
│   ├── UI/                              (Razor Class Library - net10.0)
│   │   └── GridironFrontOffice.UI.csproj
│   └── Desktop/                         (MAUI Blazor App - net10.0-windows)
│       └── GridironFrontOffice.Desktop.csproj
└── docs/
    ├── ARCHITECTURE.md                  (This file)
    └── ...
```

## Layer Descriptions

### 1. Domain Layer (`GridironFrontOffice.Domain`)

**Purpose**: Encapsulates all core business entities, value objects, and domain services with no external dependencies.

**Responsibilities**:
- Define domain entities and value objects that encapsulate core business concepts
- Encode business rules and invariants specific to the domain logic
- Provide domain service interfaces that coordinate complex business operations
- Ensure all entities maintain valid state through invariant checking

**Dependencies**: None (pure .NET)

**Key Files**:
- `Entities/` - domain entity definitions
- `ValueObjects/` - value object definitions
- `Services/` - domain service interfaces and implementations

### 2. Simulation Layer (`GridironFrontOffice.Simulation`)

**Purpose**: Implements the game engine, AI logic, and deterministic play-by-play simulation.

**Responsibilities**:
- Simulate individual games (play-by-play logic)
- Simulate seasons (week-by-week schedules, standings updates)
- AI play-calling and team strategy decisions
- Deterministic random number generation (seeded RNG)
- Stats aggregation (player stats, team stats, league-wide metrics)

**Dependencies**: `Domain`

**Key Files**:
- `Engine/GameSimulator.cs` - main game simulation logic
- `Engine/PlayEngine.cs` - play-by-play execution
- `Engine/SeasonSimulator.cs` - season simulation orchestration
- `AI/PlayCaller.cs` - AI decision-making for play calls
- `Random/DeterministicRng.cs` - seeded random number generation

**Design Principles**:
- **Determinism**: All randomness goes through seeded RNG; same seed = same result
- **Performance**: Optimized for 100+ simulated games per second
- **Modularity**: AI logic can be swapped for different strategies

### 3. Application Layer (`GridironFrontOffice.Application`)

**Purpose**: Orchestrates use cases and coordinates between layers.

**Responsibilities**:
- Implement use cases: `SimulateGame`, `SimulateSeason`, `ExecuteTrade`, `SignFreeAgent`, `AdvanceDraft`, `ProcessInjuries`
- Handle background job execution (long-running simulations)
- Publish domain events for UI updates and side effects
- Manage application services and workflows
- Coordinate persistence and caching

**Dependencies**: `Domain`, `Simulation`

**Key Files**:
- `UseCases/` - application use case implementations
- `Services/` - application-level services
- `Interfaces/` - repository and service interfaces
- `Events/` - domain event definitions

**Typical Use Case Flow**:
```csharp
public class SimulateGameUseCase
{
    public async Task<GameResult> Execute(GameRequest request)
    {
        // Validate request
        // Load league state
        // Run simulation
        // Aggregate statistics
        // Publish domain events
        // Return result
    }
}
```

### 4. Persistence Layer (`GridironFrontOffice.Persistence`)

**Purpose**: Handles all data storage and retrieval using SQLite.

**Responsibilities**:
- Implement repository pattern for all domain entities
- Manage SQLite database schema and migrations
- Handle transactions and data consistency
- Batch writes for performance
- Support save/load functionality for leagues and seasons

**Dependencies**: `Domain`

**Key Files**:
- `Repositories/` - repository implementations (`IGameRepository`, `IPlayerRepository`, etc.)
- `DbContext/` - SQLite context
- `Migrations/` - versioned schema migrations
- `Config/` - connection strings and database configuration

**Database Design**:
- Tables correspond to domain entities with normalized relationships
- Complex data structures stored as JSON for flexibility and versioning
- Supports aggregated data storage with optional detailed transaction logging
- Indexes optimized for common query patterns
- Migrations versioned and backwards-compatible

**Key Design Decisions**:
- Default: Store aggregated results only (optimized for performance and storage)
- Optional: Enable detailed transaction logging for analysis and debugging
- Versioning: Schema migrations support incremental updates without data loss

### 5. UI Layer (`GridironFrontOffice.UI`)

**Purpose**: Defines reusable Blazor components and pages.

**Responsibilities**:
- Build Razor components for all UI sections
- Manage component state and interactions
- Bind to application services
- Display league, team, player, and game information
- Handle user input for trades, drafts, free agency, etc.

**Dependencies**: `Application`

**Key Files**:
- `Components/Dashboard/` - league overview, KPIs
- `Components/Team/` - team management (roster, depth chart)
- `Components/League/` - standings, schedule, transactions
- `Components/Game/` - game viewer, play-by-play display
- `Components/Draft/` - draft room UI
- `Pages/` - top-level Razor pages
- `wwwroot/` - static assets (CSS, JS, images)

**Component Example**:
```razor
@* Components/ItemGrid.razor *@
@inherits ComponentBase
@inject ApplicationService appService

<div class="grid-container">
    @foreach (var item in Items)
    {
        <div class="item-card" @onclick="() => SelectItem(item)">
            <h4>@item.Name</h4>
            <p>@item.Description</p>
            <p>Status: @item.Status</p>
        </div>
    }
</div>

@code {
    [Parameter]
    public List<DomainEntity> Items { get; set; } = new();
    
    private void SelectItem(DomainEntity item) 
    { 
        // Handle selection logic
    }
}
```

**Reusability Note**: This Razor Class Library can be reused in:
- Future Blazor Server web app
- Future Blazor WASM web app
- Any other .NET host

### 6. Desktop Host (`GridironFrontOffice.Desktop`)

**Purpose**: MAUI Blazor application that serves as the Windows desktop host.

**Responsibilities**:
- Host the Blazor UI in a BlazorWebView control
- Manage application lifecycle and window management
- Set up dependency injection for all layers
- Integrate platform-specific features (file system, native APIs)
- Handle packaging and distribution

**Dependencies**: `UI`, `Application`, `Persistence`

**Key Files**:
- `MauiProgram.cs` - DI configuration and MAUI setup
- `App.xaml.cs` - MAUI app lifecycle
- `MainPage.xaml` - window with BlazorWebView
- `Platforms/Windows/` - Windows-specific code
- `Resources/` - icons, splash screen

**Target**: `net10.0-windows10.0.19041.0` (Windows 11+)

## Dependency Graph

```
Desktop (Thin Shell)
  ↓
  ├→ UI (Razor Components)
  │   ↓
  │   Application (Orchestration)
  │     ↓
  │     ├→ Domain (Entities)
  │     └→ Simulation (Engine)
  │          ↓
  │          Domain
  │
  ├→ Persistence (SQLite)
  │   ↓
  │   Domain
  │
  └→ Application (DI Setup)

** Direction: Top depends on below, no circular dependencies **
```

## Key Design Principles

### 1. Clean Architecture
- **Clear Separation**: Each layer has a single responsibility
- **Testability**: Core logic (Domain, Simulation) is pure C# with no dependencies
- **Framework Independence**: Domain and Simulation don't reference MAUI, Blazor, or EF Core
- **Inversion of Control**: Domain defines interfaces; Application/Desktop inject implementations

### 2. Determinism
- **Seeded RNG**: All randomness in simulation uses a single seeded random number generator
- **Reproducibility**: Same seed + same league state = identical simulation results
- **Seed Recording**: Seeds are stored in save files for perfect replay

### 3. Performance
- **Simulation Worker**: Heavy computations run off the UI thread
- **Batch I/O**: Persist aggregated stats after game/season completion, not per-play
- **In-Memory Aggregates**: Keep computed statistics in memory during simulation
- **Indexed Queries**: Optimize SQLite queries with proper indexes

### 4. Extensibility
- **Plugin Boundaries**: AI modules, playbook formats, rating systems defined as interfaces
- **Migration Path**: Future web version can reuse Domain, Simulation, Persistence, UI layers
- **Optional Modules**: Per-play logging, advanced analytics, custom strategies can be added

## Development Workflow

### Building
```bash
dotnet build                    # Build all projects
dotnet build src/Desktop/...   # Build Desktop only
```

### Running
```bash
dotnet run --project src/Desktop
```

### Testing
```bash
# Add test projects later (xUnit, MSTest, etc.)
dotnet test
```

## Migration Strategy (Future)

If the application needs to support multiple UI platforms:

1. **Keep Domain, Simulation, Persistence unchanged**
2. **Create new UI library or reuse existing `GridironFrontOffice.UI`**
3. **Create new host**:
   - `GridironFrontOffice.Web` - Blazor Server for web
   - `GridironFrontOffice.WebAssembly` - Blazor WebAssembly for SPA
   - `GridironFrontOffice.Mobile` - MAUI for iOS/Android

Each new host would reference the same core layers, ensuring consistent business logic and data.

## Naming Conventions

- **Namespaces**: `GridironFrontOffice.<Layer>` (e.g., `GridironFrontOffice.Simulation`)
- **Interfaces**: `I<Concept>` (e.g., `IGameRepository`, `IPlayCaller`)
- **Entities**: PascalCase (e.g., `Team`, `Player`, `Contract`)
- **ValueObjects**: PascalCase (e.g., `Rating`, `Salary`, `Position`)
- **Services**: `<Concept>Service` (e.g., `GameSimulationService`, `RosterManagementService`)

## Deployment

### Development
- Debug build, local SQLite database, verbose logging

### Release
- Release build, optimized for performance
- Packaged via MAUI for Windows (MSI installer)
- Auto-updates via standard Windows Update mechanisms (optional, future)

## Next Steps

1. **Scaffold Domain Models**: Define `Team`, `Player`, `Game`, `Season` entities
2. **Build Simulation Engine**: Implement deterministic game simulation with seeded RNG
3. **Create Repository Implementations**: SQLite schema and EF Core mappings
4. **Build Application Use Cases**: Orchestrate simulation and persistence
5. **Develop UI Components**: Blazor pages and components for league management
6. **Integrate and Test**: End-to-end workflow validation

---

**Architecture Version**: 1.0  
**Last Updated**: February 1, 2026  
**Target Framework**: .NET 10
