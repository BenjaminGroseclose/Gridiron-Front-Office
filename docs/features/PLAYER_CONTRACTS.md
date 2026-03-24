# Player Contracts System — Product Requirements Document

## Executive Summary

**Problem Statement**:
Teams need a realistic, granular contract system to manage salary cap constraints and force year-to-year roster decisions. Without meaningful contracts, the salary cap becomes a meaningless number; with them, every player signing becomes a strategic commitment with long-term consequences.

**Proposed Solution**:
Implement a multi-faceted contract system supporting fixed-salary, tiered, performance-bonus, and option-based contracts. Players and teams will negotiate through a structured two-way proposal system where both sides submit counter-offers until consensus is reached or negotiation breaks down.

**Success Criteria**:

- Teams actively manage salary cap constraints and feel real tension when signing star players
- Contract negotiations are non-trivial and create meaningful strategic choices
- Players with proven performance and desirable archetypes command higher contract values
- Salary cap parity prevents permanent dominance (same as current league-wide design)
- System produces realistic contract distributions matching NFL comparative analysis

---

## 1. User Experience & Functionality

### 1.1 User Personas

| Persona     | Role                     | Primary Concern                                                |
| ----------- | ------------------------ | -------------------------------------------------------------- |
| **GM/User** | Team decision-maker      | Balancing immediate roster needs with long-term cap health     |
| **AI GM**   | Computer-controlled team | Offering/accepting contracts to build competitive rosters      |
| **Player**  | Negotiation counterparty | Securing favorable terms based on performance and market value |

### 1.2 User Stories

#### Story 1: Initial League Setup — Contract Assignment

```
As a League Creator / GM,
I want to assign initial contracts to all rostered players
so that the league starts with defined salary obligations and a balanced cap scenario.

Acceptance Criteria:
- All 53-man rosters have contracts assigned before first game
- Contracts reflect player age, archetype
- Team salary caps are populated and enforceable
- System prevents duplicate contract assignment
- Initial assignment must complete in < 5 seconds for a 32-team, 1,696-player league
```

#### Story 2: Player Contract Negotiation (Renewal / Extension)

```
As a GM,
I want to negotiate a contract extension with my star QB
so that I can lock in talent at a reasonable rate before they hit free agency.

Acceptance Criteria:
- I can initiate negotiation with any rostered player
- Contract proposals include: salary, term length, bonus structure, option clauses
- Negotiation show both my proposal and Player counter-offer
- I can accept, reject, or modify and re-propose
- Negotiation tracks proposal history for reference
- If negotiation fails, player may request a trade or free agency
```

#### Story 3: Free Agency Contract Offers

```
As a GM,
I want to make competitive offers to free agents
so that I can improve my roster during free agency windows.

Acceptance Criteria:
- I can view available free agents with market value estimates
- I submit contract proposals with all terms pre-defined
- AI/Player evaluates offer and responds with counter-proposal
- System blocks offers that exceed my remaining cap space
- Offer history is maintained for audit/reference
```

#### Story 4: Salary Cap Enforcement

```
As a League / GM,
I want the salary cap system to enforce hard limits
so that teams cannot circumvent cap discipline through creative accounting.

Acceptance Criteria:
- No contract can be signed if resulting cap hit exceeds team cap room
- Dead cap from terminated contracts counts against cap
- Multi-year contracts spread cap hits correctly across years
- Trade deadline ensures cap compliance when players change teams
- System reports cap health (remaining space, projected commitments)
```

#### Story 5: Contract Termination & Dead Cap

```
As a GM,
I want the ability to cut/release players and understand the cap implications
so that I can make roster adjustments with visible consequences.

Acceptance Criteria:
- Cutting a player removes salary but creates dead cap
- Dead cap stays on the cap for remaining contract years (or contractual terms)
- System calculates true net cap savings vs. dead cap cost
- GM can compare: "Keep player for $15M cap hit" vs "Cut player, $8M dead cap"
- Dead cap reports project future years' cap health
```

### 1.3 Non-Goals

- **Dynamic market adjustments** based on league-wide salary trends (fixed position-based tiers for MVP 1)
- **Sponsorship or endorsement revenue** (stay focused on team salary management)
- **Grievance/arbitration system** for player disputes
- **Contract history/legacy tracking** across entire league (store current contracts only)
- **Salary floor enforcement** (only hard cap, not minimum spending requirement)

---

## 2. Contract System Architecture

### 2.1 Contract Types

#### Type 1: Fixed Salary Contract

**Structure**: Single annual salary + optional signing bonus, flat across all years.

```
Example: 3-year / $12M ($4M AAV)
  Year 1: $4M salary (+ $2M signing bonus at signing)
  Year 2: $4M salary
  Year 3: $4M salary
```

**Use Cases**:

- Veteran role players
- Budget-constrained signings
- Short-term depth pieces

---

#### Type 2: Tiered Salary Contract

**Structure**: Salary increases each year (often paired with a signing bonus).

```
Example: 4-year / $20M ($5M AAV) with escalation
  Year 1: $3M salary (+ $1M signing bonus)
  Year 2: $4M salary
  Year 3: $5M salary
  Year 4: $8M salary (back-loaded)
```

**Cap Impact**: Full cap hit for each year, regardless of whether salary is paid.

**Use Cases**:

- Franchise stars entering peak years
- Young players on development curve
- Extension deals that incentivize performance

---

#### Type 3: Performance-Based Bonuses

**Structure**: Base salary + conditional bonus triggers (must be earned).

```
Example: 2-year / $4M base + up to $2M in bonuses
  Year 1: $2M salary + $0.5M (for 10+ sacks OR 80+ tackles OR similar)
  Year 2: $2M salary + $0.5M (same trigger)
```

**Negotiation Integration**: Teams propose bonus structures; Players evaluate if they match expected performance.

**Use Cases**:

- High-effort positions (DE, LB, S)
- Unproven talent (conditional growth contracts)
- Controlled risk for teams and players

---

#### Type 4: Team Options

**Structure**: Deadline after which team can terminate contract and eat dead cap OR execute automatic renewal.

```
Example: 3-year / $15M with team option:
  Year 1: $5M salary + $3M signing bonus
  Year 2: $5M salary
  Year 3: $5M salary (TEAM OPTION — due by June 1 of Year 3)
    - If exercised: continue Year 3
    - If declined: player may hit free agency; team eats remaining guaranteed money
```

**Cap Mechanics**: Option years count against cap until formally declined.

**Use Cases**:

- Veterans with declining ratings
- Bridge deals with escape clauses
- Reducing long-term risk

---

#### Type 5: Player Options

**Structure**: Similar to team options, but **Player decides** whether to accept or demand renegotiation.

```
Example: 4-year / $16M with player option in Year 3:
  Years 1-2: Fixed terms
  Year 3: PLAYER OPTION — player can:
    - Accept Year 3 + Year 4 as written, OR
    - Demand renegotiation (triggers negotiation), OR
    - Declare free agency
```

**Use Cases**:

- Star players with leverage
- Retention deals designed to show organizational faith
- (Less common than team options in MVP 1; focus on team options first)

---

### 2.2 Core Contract Fields

```
Contract Entity (Domain Layer)
├── ContractID : int
├── PlayerID : int (foreign key to Player)
├── TeamID : int (foreign key to Team)
├── ContractType : enum (Fixed, Tiered, PerformanceBonus, TeamOption, PlayerOption)
├── StartYear : int (league year contract begins)
├── EndYear : int (last year of contract)
├── TermLengthYears : int (derived: EndYear - StartYear + 1)
├── SigningBonus : decimal (total signing bonus, paid on execution)
├── TotalContractValue : decimal (sum of all salary + bonuses across all years)
├── AverageAnnualValue (AAV) : decimal (TotalContractValue / TermLengthYears)
├── GuaranteedMoney : decimal (money guaranteed regardless of cut/injury)
├── YearlyBreakdown : List<ContractYear> (see below)
├── OptionDetails : OptionClause? (if contract includes options)
├── NegotiationNotes : string? (audit trail / memo)
├── SignedDate : DateTime
├── Status : enum (Active, Expired, Terminated, Void)
└── ModifiedDate : DateTime

ContractYear (Value Object)
├── Year : int (absolute league year)
├── BaseSalary : decimal
├── BonusEarnings : decimal (performance or condition-based)
├── CapHit : decimal (what counts against salary cap this year)
├── IsGuaranteed : bool (true if guaranteed regardless of cut)

OptionClause (Value Object)
├── OptionType : enum (TeamOption, PlayerOption)
├── OptionYear : int (absolute league year of option decision)
├── DeadlineDate : DateTime (e.g., June 1 of option year)
├── CostToDeclining : decimal (dead cap if declined)
└── AutomaticIfNotDeclared : bool (fail-safe: what happens if forgotten?)
```

---

## 3. Contract Valuation & Negotiation System

### 3.1 Player Market Value Assessment

The system must determine **fair market value** for each player to ground negotiations realistically.

#### Market Value Inputs:

1. **Position** (QB > edge rusher > WR > CB > etc. in cap dollars)
2. **Age Curve** (peak earning years 5–10 of experience)
3. **Overall Rating** (current simulation performance value 0–99)
4. **Archetype Quality** (elite archetypes command premium)
5. **Contract History** (proven consistency matters)
6. **Remaining Useful Life** (age decay curve)

#### Market Value Formula (Pseudocode):

```
BASE_VALUE = Position_Weight * League_Average_Salary
RATING_MULTIPLIER = (Player Overall / 80) ^ 1.5  // Diminishing returns for stars
AGE_CURVE_MULTIPLIER = ApplyAgeDecayFactors(Experience, Position)
ARCHETYPE_BONUS = Archetype_Tier_Adjustment (e.g., "Elite QB" = +15%)

MarketValue = BASE_VALUE * RATING_MULTIPLIER * AGE_CURVE_MULTIPLIER * ARCHETYPE_BONUS

// Cap market value to realistic bounds
MarketValue = Clamp(MarketValue, Min_Position_Salary, Max_Position_Salary)
```

#### Market Value Tiers (Example — QB):

```
Overall Rating     Example Market Value (AAV) Term Preference
     99              $45M+                       Long (franchise star)
  90-98              $30-42M                     Medium-Long (core player)
  80-89              $20-28M                     Medium (solid starter)
  70-79              $12-18M                     Short-Medium (vet depth)
  < 70               $6-11M                      Short (backup/role player)
```

### 3.2 Negotiation Flow: Two-Way Proposal System

#### Phase 1: Initiation

- **GM initiates** by proposing contract terms:
    - Term length (1–7 years, typically)
    - Annual salary structure (fixed, tiered, back-loaded, front-loaded)
    - Signing bonus
    - Performance bonuses (if included)
    - Option clause (if included)
- **System validates**: Proposal does not exceed team cap room
- **Player evaluates**: Proposal is scored against market value + team prestige factor

#### Phase 2: Player Counter-Proposal

The system (representing the player/agent) scores the GM's offer and responds:

```
Offer_Score = Sum(
    Salary_vs_Market_Value (60% weight),
    Term_vs_Preference (20% weight),
    Bonus_Structures (10% weight),
    Option_Favorability (10% weight)
)

If Offer_Score >= 85:
  Player accepts ✓ (contract signed)
Else If Offer_Score >= 70:
  Player counter-proposes (adjust salary up 5–10%, modify structure)
Else If Offer_Score >= 50:
  Player counter-proposes (adjust salary up 10–15%, request longer term)
Else:
  Player rejects ✗ (suggests "unrealistic" + refuses further negotiation)
```

#### Phase 3: GM Response to Counter

- **GM can**:
    - Accept counter-proposal ✓
    - Reject ✗ and withdraw negotiation
    - Modify counter and re-propose (up to max # of rounds, e.g., 5 total proposals)
- **Negotiation ends** when:
    - Both sides agree (contract signed)
    - Player walks away (declares free agency intent or requests trade)
    - GM walks away (focus on other priorities)

#### System Safeguards:

- **Cap enforcement**: GM cannot propose beyond available cap room
- **Sanity check**: Counter-proposals adjust by 5–15% max (no wild swings)
- **Timeout**: After 5+ proposal rounds, player may demand "take it or leave it"
- **Persistence**: Contract negotiations can span multiple game days/UI sessions

### 3.3 Example Negotiation Walkthrough

```
=== GM INITIATES ===
GM proposes to RB "James Johnson" (Overall 85, Market Value ~$6M AAV):
  Term: 2 years
  Year 1: $5M salary + $1M signing bonus
  Year 2: $6.5M salary
  Total: $11.5M / $5.75M AAV
  Status: PROPOSED

=== PLAYER COUNTER ===
System evaluates: $5.75M AAV vs. $6M market = 96% of market
Counter-proposal from Player:
  Term: 3 years (wants job security)
  Year 1: $5.5M salary + $1.5M signing bonus
  Year 2: $6M salary
  Year 3: $6.5M salary
  Total: $18.5M / $6.17M AAV
  Status: PLAYER COUNTER

=== GM RESPONSE ===
GM accepts term 3, but pushes back on bonus:
  Term: 3 years
  Year 1: $5.5M salary + $0.5M signing bonus
  Year 2: $6M salary
  Year 3: $6.5M salary
  Total: $18M / $6M AAV
  Status: COUNTER-PROPOSAL 2

=== PLAYER RESPONSE ===
System evaluates: 3-year job security ✓, $6M AAV matches market ✓
Player accepts.
  Contract Status: SIGNED ✓
  Dead cap protection: Years 1-3 fully guaranteed ($18M GTD)
```

---

## 4. Salary Cap Management

### 4.1 Hard Cap Rules

**League Salary Cap**: Fixed annual cap per team (e.g., $200M for 2025 season).

**Cap Hit Calculation**:

```
Annual_Cap_Hit = Year_Salary + (Signing_Bonus / Years_Until_Fully_Amortized)
  + Prorated_Dead_Cap + Performance_Bonus_Accruals
```

**Example**:

```
QB Contract: 4-year / $20M, $4M signing bonus
Year 1: $4M salary + ($4M signing bonus / 4 years) = $5M cap hit

If QB is cut in Year 2:
  Remaining signing bonus amortization: $2M (Year 2 + 3)
  Dead cap in Year 2: $2M (new entry on cap)
  New QB minimum cap hit: $2M dead cap + new QB salary
```

### 4.2 Cap Room Calculation

```
Available_Cap_Room = League_Cap_Total - Sum(All_Current_Contract_Cap_Hits)
                    - Reserve_for_Signings (typically 10-15% buffer)
```

**Warning Thresholds**:

- **Green** (> 15M available): Plenty of room for mid-tier signings
- **Yellow** (5–15M available): Limited room; can sign role players
- **Red** (< 5M available): Heavy constraints; only minimum-salary options

### 4.3 Trade Deadline Cap Compliance

When a player is traded:

- **Removing team**: Releases all future salary obligations (clean cap escape)
- **Acquiring team**: Assumes remaining salary + any prorated signing bonus
- **System check**: Trade is blocked if acquiring team would exceed cap

Example:

```
RB "Smith" traded mid-Year 2:
  Remaining contract: Year 2 @ $4M, Year 3 @ $4.5M

Original Team (removing):
  Cap relief: $4M (Year 2) + $4.5M (Year 3) = $8.5M freed

Acquiring Team (adding):
  Cap hit: $4M (Year 2) + $4.5M (Year 3)
  Must have room; else trade is blocked
```

### 4.4 Free Cap Space Tracking

UI/Reports display:

- **Current Year Cap Usage**: Visualized as a progress bar (allocated vs. total)
- **Future Year Commitments**: Table showing Years 2–5 projected cap hits
- **Dead Cap**: Highlighted line item (money committed but not playing)
- **Remaining Room**: Bold, actionable number for signing decisions

---

## 5. Initial Contract Assignment (League Setup)

### 5.1 Algorithm for Auto-Assigning Contracts

When a new league is created, all 1,696 players (32 teams × 53 players) must receive starting contracts.

**Goal**: Produce realistic, balanced contracts that match player archetype/performance.

#### Assignment Logic:

```
For each Player P in League:
  1. Determine Player Market Value (see § 3.1)
  2. Determine Contract Structure Preference:
     - Stars (Overall >= 85): 4–5 year tiered contracts
     - Starters (80–84): 3–4 year tiered or fixed
     - Role players (70–79): 2–3 year fixed
     - Depth/Backups (< 70): 1–2 year fixed

  3. Assign specific salary breakdown:
     - Front-load for older players (encourage retention early)
     - Back-load for younger players (incentivize growth)
     - Spread signing bonuses across tenure

  4. Include guaranteed money:
     - Starters: 50–75% of contract guaranteed
     - Role players: 25–50% guaranteed
     - Backups: 0–25% guaranteed

  5. Assign random signings date (off-season, varies slightly per player)

  6. Log in database with Status: "Active"
```

#### Cap Balance Check:

After assigning all players, system verifies:

```
For each Team T:
  Total_Cap_Hit = Sum(All_Contract_Cap_Hits for Team T)
  If Total_Cap_Hit > League_Cap:
    // Re-adjust oldest/lowest-rated players downward slightly
    // Iterate until compliant
  Remaining_Cap_Room[T] = League_Cap - Total_Cap_Hit
  Verify: All teams have ~5–15% cap room for signings (realistic buffer)
```

**Objective**: No team starts over cap; all teams have realistic negotiating room.

---

## 6. Technical Specifications

### 6.1 Domain Layer Changes

**New Entities** (`GridironFrontOffice.Domain`):

```
Domain/
├── Contract.cs                      (main contract entity)
├── ContractYear.cs                  (yearly breakdown, value object)
├── OptionClause.cs                  (option structure, value object)
├── Enums/
│   ├── ContractType.cs              (Fixed, Tiered, etc.)
│   ├── ContractStatus.cs            (Active, Expired, Terminated)
│   └── OptionType.cs                (TeamOption, PlayerOption)
└── Helpers/
    └── ContractNameHelper.cs        (human-readable contract descriptions)
```

**Modified Entities**:

- `Team.cs`: Add `SalaryCap: decimal`, `DeadCapTracker: List<DeadCapEntry>`
- `Player.cs`: Add `ContractID: int?` (foreign key to active contract)

### 6.2 Persistence Layer Changes

**New Repositories** (`GridironFrontOffice.Persistence`):

```
Repositories/
├── ContractRepository.cs            (CRUD + queries)
└── SalaryCapRepository.cs           (cap calculations, reporting)

Interfaces/
├── IContractRepository.cs
└── ISalaryCapRepository.cs
```

**Database Context Changes** (`GridironFrontOfficeDbContext.cs`):

- Add `DbSet<Contract>`, `DbSet<ContractYear>`, `DbSet<DeadCapEntry>`
- Define relationships (Contract → Player, Contract → Team, Contract → OptionClause)
- Add unique constraint: One active contract per player per time period

### 6.3 Application Layer Changes

**New Services** (`GridironFrontOffice.Application`):

```
├── ContractNegotiationService.cs    (handles proposal/counter flow)
├── SalaryCapService.cs              (cap enforcement, room calculations)
├── ContractTerminationService.cs    (cut/release + dead cap logic)
├── InitialContractService.cs        (league setup auto-assignment)
└── Interfaces/
    ├── IContractNegotiationService.cs
    ├── ISalaryCapService.cs
    ├── IContractTerminationService.cs
    └── IInitialContractService.cs
```

**Service Responsibilities**:

| Service                       | Key Methods                                                                                                |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `IContractNegotiationService` | `InitiateNegotiation()`, `SubmitProposal()`, `EvaluateCounterProposal()`, `AcceptOffer()`, `RejectOffer()` |
| `ISalaryCapService`           | `GetAvailableCapRoom()`, `ValidateContractAgainstCap()`, `CalculateCapHit()`, `ProjectFutureYearCaps()`    |
| `IContractTerminationService` | `CutPlayer()`, `ReleasePlayer()`, `CalculateDeadCap()`, `TradePlayer()`                                    |
| `IInitialContractService`     | `auto AssignContracts()`, `GenerateContractForPlayer()`                                                    |

### 6.4 Simulation Layer Integration

**Existing**: `GameManager.cs`, `SeedDataService.cs`

**Changes**:

- `SeedDataService`: Call `IInitialContractService.AssignContracts()` after player generation
- On season/year advancement: Age all contracts, check for expiring contracts, notify GMs

### 6.5 UI Layer (Razor Components)

**New Pages/Components** (`GridironFrontOffice.UI / .Desktop`):

```
Pages/
├── ContractManagement.razor         (main contract hub)
├── ContractNegotiation.razor        (proposal/counter flow)
├── SalaryCapDashboard.razor         (cap health + future projections)
├── PlayerContracts.razor            (roster view with contract details)

Components/
├── ContractCard.razor               (displays contract summary)
├── ProposalForm.razor               (terms input for GM)
├── CounterProposalDisplay.razor     (shows player's counter)
├── CapRoomIndicator.razor           (visual cap room gauge)
```

---

## 7. Risks & Mitigation

| Risk                                                               | Likelihood | Impact                                | Mitigation                                                                                                                         |
| ------------------------------------------------------------------ | ---------- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| **Negotiation deadlock** (no convergence after many rounds)        | Medium     | Players left in limbo, GMs frustrated | Hard timeout after 5 proposals; player declares free agency or GM walks away                                                       |
| **Market value misalignment** (formula produces unrealistic tiers) | Medium     | Contracts not believable              | Calibrate against NFL comparables; A/B test with community; iterate formula                                                        |
| **Performance bonus exploitation** (teams abuse bonus structure)   | Low        | Cap circumvention concern             | Cap counting rules enforce full cap hit regardless of earned bonus; reword as "upside cap hit" not alternative                     |
| **Dead cap spirals** (AI teams accumulate massive dead cap)        | Medium     | AI teams become uncompetitive         | Monitor dead cap % per team; adjust AI negotiation bias to avoid over-signing long-term deals                                      |
| **UI complexity** (contract negotiation screen overwhelming)       | High       | Players don't engage with system      | Simplify UI to "Salary", "Term", "Bonus", "Option?" only; hide details in tooltip                                                  |
| **Database migration** (large schema change to existing saves)     | High       | Existing leagues unplayable           | Plan migration carefully; add null defaults for `Contract` fields; grandfather legacy players with auto-generated 1-year contracts |

---

## 8. Phased Rollout

### **MVP 1 (Contracts Foundation)** — _[CURRENT PHASE]_

- ✓ Domain entities: `Contract`, `ContractYear`, `OptionClause`
- ✓ Fixed and Tiered salary contracts only (no bonuses yet)
- ✓ Hard salary cap enforcement + cap room tracking
- ✓ Simple negotiation: GM proposes, Player accepts/rejects (no counter-proposals)
- ✓ Auto-assign contracts on league creation
- ✓ UI: List player contracts, view cap room, propose new contracts

**Scope**: Get contracts in place and cap enforcement working; negotiate baseline system.

### **MVP 1.1 (Contract Negotiations)**

- Performance-based bonuses (earn/condition-based)
- Two-way proposal system (player counter-proposes)
- Contract termination + dead cap tracking
- Trade deadline cap compliance
- Enhanced UI: Negotiation flow with proposal history

**Scope**: Make negotiation non-trivial and add depth to contract mechanics.

### **MVP 1.2 (Contract Depth)**

- Team options and player options
- Renegotiation triggers (age milestones, performance thresholds)
- Contract history/legacy tracking (optional; lower priority)
- Advanced cap reports (dead cap projection, 5-year cap trajectory)

**Scope**: Extend contract systems to replicate real NFL options/leverage mechanics.

---

## 9. Success Metrics & Acceptance Tests

### League-Level Metrics

| Metric                           | Target                                    | Measurement                            |
| -------------------------------- | ----------------------------------------- | -------------------------------------- |
| **Average Team Cap Utilization** | 85–95%                                    | Post-league-generation audit           |
| **Contract Disputes/Deadlocks**  | < 5% of negotiations                      | Log negation outcomes                  |
| **Dead Cap as % of Total Cap**   | 5–12% league-wide average                 | Annual cap report                      |
| **Contract Negotiation Time**    | < 30 seconds per negotiation              | UI/UX stopwatch during UI testing      |
| **AI Contract Competitiveness**  | AI teams sign ~4–6 players per off-season | Replay several seasons, count signings |

### Player-Level Metrics

| Metric                                  | Target                               | Measurement                                     |
| --------------------------------------- | ------------------------------------ | ----------------------------------------------- |
| **Top Tier Players** (Overall >= 85)    | Earn 30–40%+ of league salary        | Aggregate top 100 players' AAV vs. league total |
| **Backup/Depth Players** (Overall < 70) | Earn < 10% of league salary          | Aggregate bottom 200 players                    |
| **Contract Realism Check**              | Elite QBs $35M+, Top WRs $18M+, etc. | Manual spot-check against NFL comps             |

### System Performance

| Metric                                          | Target      | Measurement              |
| ----------------------------------------------- | ----------- | ------------------------ |
| **Initial Contract Assignment (1,696 players)** | < 5 seconds | Benchmark on dev machine |
| **Cap Validation on Trade/Signing**             | < 100ms     | Unit tests + benchmarks  |
| **Negotiation Round-trip Response**             | < 1 second  | UI responsiveness test   |

---

## Appendix A: Contract Examples

### Example 1: Star QB (Year 1)

```
Player: "Tom Elite" (Overall 92, Age 8 years)
Market Value: ~$38M AAV

Contract Offered:
  Type: Tiered Salary
  Term: 4 years
  Start Year: 2025 | End Year: 2028
  Signing Bonus: $12M
  Year 1: $8M salary + $3M signing bonus = $11M cap hit
  Year 2: $10M salary = $10M cap hit
  Year 3: $12M salary = $12M cap hit
  Year 4: $14M salary = $14M cap hit
  Total Value: $54M / $13.5M AAV
  Guaranteed: $20M (includes full Year 1 + Year 2 salary)

Negotiation Flow:
  GM proposes above
  → Player counter: "I want $38M AAV minimum, 4 years = $152M total"
  → GM revises: "3 years, $42M total / $14M AAV" (bump Year 1)
  → Player accepts ✓

Result: Star QB locked up, team has cap strain in Years 3–4
```

### Example 2: Young WR (Year 1)

```
Player: "Rookie Upside" (Overall arche 73, Age 1 year)
Market Value: ~$4M AAV

Contract Offered:
  Type: Tiered Salary (dev-focused)
  Term: 3 years
  Signing Bonus: $0.5M
  Year 1: $3M salary + $0.5M signing bonus = $3.5M cap hit
  Year 2: $4M salary = $4M cap hit
  Year 3: $5M salary = $5M cap hit
  Total Value: $12.5M / $4.17M AAV
  Guaranteed: $3.5M (Year 1 + signing bonus)

Negotiation:
  GM proposes above
  → Player counter: "I want 4 years, more guaranteed money" (young player wants security)
  → GM: "Can't extend to 4 years (cap constraints). Add Year 1 salary guarantee."
  → Agree: Year 2 salary now also guaranteed ($7.5M total guaranteed)
  → Signed ✓

Result: Team has development flexibility; young WR gets security
```

### Example 3: Veteran Edge Rusher (Post-Peak)

```
Player: "Grizzled Vet" (Overall 74, Age 12 years)
Market Value: ~$7M AAV

Contract Offered:
  Type: Fixed Salary + Team Option
  Term: 2 years + 1 option
  Year 1: $6M salary = $6M cap hit
  Year 2: $8M salary = $8M cap hit
  Year 3 (TEAM OPTION): $8.5M salary
    - If exercised: $8.5M cap hit
    - If declined by June 1: Player enters free agency, team eats $2M dead cap

Total Value (if all exercised): $22.5M / $7.5M AAV
  Guaranteed: $14M (Years 1–2 only; Year 3 not guaranteed)

Negotiation:
  GM proposes above
  → Player counter: "Too short term. I want 3 guaranteed years."
  → GM: "Can't guarantee Year 3, but I'll increase Year 2 salary to $9M + team option becomes automatic unless declined."
  → Player accepts (Year 3 becomes "likely")

Result: Vet feels secure through Year 2; team has out if he doesn't perform
```

---

## Appendix B: NFL Comparative Salary Data (Reference)

Below is approximate 2024 NFL salary cap context for calibration:

```
League Salary Cap: $248.5M per team
Average Starter Salary: ~$4.5M
Average Role Player:     ~$1.5M
Top-10 QB Salary:        $50M+ AAV
Top-10 EDGE:             $22M+ AAV
Top-10 WR:               $18M+ AAV
Top-10 CB:               $16M+ AAV
Average Backup:          $0.75M
Average Depth:           $0.5M or league minimum
```

**Target for Gridiron Front Office**:

- Scaled proportionally to whatever league cap you choose (e.g., $200M = ~80% of NFL)
- Same distribution shape (star heavy, depth light)
- Realism check: Quick visual scanning of top earners should match archetypes/ratings

---

## Appendix C: Glossary

| Term                    | Definition                                                                               |
| ----------------------- | ---------------------------------------------------------------------------------------- |
| **AAV**                 | Average Annual Value; total contract value ÷ term length years                           |
| **Cap Hit**             | Amount of contract that counts against annual salary cap in a given year                 |
| **Dead Cap**            | Salary cap space still committed due to a player cut/trade remainder of guaranteed money |
| **Fully Guaranteed**    | Contract salary/bonuses that must be paid regardless of cut or injury                    |
| **GTD**                 | Guaranteed Money abbreviation                                                            |
| **Market Value**        | Estimated realistic salary for a player based on performance, position, age              |
| **Option Clause**       | Right (team or player) to end or extend contract on a specific date                      |
| **Prorated**            | Spread across multiple years (e.g., signing bonus amortized over 4 years)                |
| **Signing Bonus**       | Lump sum paid upon contract execution, typically amortized against cap                   |
| **Tag / Franchise Tag** | (Not in MVP 1) One-year contract offer designed to keep player vs. free agency           |
| **Tiered Contract**     | Salary increases year-to-year; different salary per year                                 |
| **Trade Deadline**      | Last day teams can acquire/trade players before season end                               |

---

## Document History

| Version | Date       | Author | Notes                            |
| ------- | ---------- | ------ | -------------------------------- |
| 1.0     | 2026-03-24 | —      | Initial PRD; MVP 1 scope defined |
