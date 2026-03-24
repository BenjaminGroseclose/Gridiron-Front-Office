# Player Contract Negotiation Process

## Overview

This document details the step-by-step process for how players and teams negotiate contracts in Gridiron Front Office. It includes workflow diagrams, decision trees, and practical examples to guide implementation and player understanding.

---

## Part 1: Negotiation Lifecycle

### 1.1 High-Level Negotiation Phases

```
PHASE 1: INITIATION
  ├─ GM Triggers
  │  ├─ Extension Proposal (existing rostered player)
  │  ├─ Free Agent Offer (unrestricted player)
  │  └─ Re-negotiation (mutual override)
  │
  └─ System Pre-Checks
     ├─ Player availability (not already in active negotiation)
     ├─ Team salary cap room (minimum offer < available cap)
     └─ Legal status (not retired, not injured, etc.)

PHASE 2: GM PROPOSAL SUBMISSION
  ├─ GM inputs contract terms:
  │  ├─ Term length (1–7 years typically)
  │  ├─ Salary schedule (fixed, tiered, back/front-loaded)
  │  ├─ Signing bonus (optional)
  │  ├─ Performance bonuses (optional)
  │  └─ Option clause (optional)
  │
  └─ System validates:
     ├─ Contract fits in cap room
     ├─ Proposal is "reasonable" (not absurdly low)
     └─ Contract structure is valid

PHASE 3: PLAYER EVALUATION & COUNTER
  ├─ System scores GM offer vs. market value
  │  ├─ Salary comparison (60% weight)
  │  ├─ Term length preference (20% weight)
  │  ├─ Bonus structures (10% weight)
  │  └─ Option favorability (10% weight)
  │
  ├─ Player decision logic:
  │  ├─ If score >= 85: ACCEPT ✓
  │  ├─ If 70–84: COUNTER-PROPOSE (minor changes)
  │  ├─ If 50–69: COUNTER-PROPOSE (major changes)
  │  └─ If < 50: REJECT ✗ (no counter; end negotiation)
  │
  └─ If countering:
     └─ Generate counter-proposal (5–15% variation from GM offer)

PHASE 4: GM RESPONSE
  ├─ GM options:
  │  ├─ Accept counter-proposal → CONTRACT SIGNED ✓
  │  ├─ Modify and re-propose → Back to PHASE 3
  │  └─ Reject and withdraw → NEGOTIATION ENDS ✗
  │
  └─ Constraints:
     ├─ Max 5–6 proposal rounds total
     ├─ After round 5: Player may issue "final offer"
     └─ After round 6: Automatic rejection

PHASE 5: CONTRACT EXECUTION
  ├─ Signing bonus deposited immediately
  ├─ Salary accrues per year
  ├─ Contract marked "Active" in system
  ├─ Notification: Team roster updated
  └─ Available cap room recalculated
```

---

## Part 2: Player Evaluation Scoring System

### 2.1 Offer Scoring Logic (Detailed)

When a GM submits a proposal, the system scores it across four dimensions. Here's how each works:

#### **Dimension 1: Salary vs. Market Value (60% weight)**

```
Calculate:
  Player_Market_Value = [market value from § 3.1 of PLAYER_CONTRACTS.md]
  Proposed_AAV = Proposal_Total_Value / Proposal_Term_Length
  Salary_Ratio = Proposed_AAV / Player_Market_Value

Score Mapping for Salary_Ratio:
  >= 1.05 (5% above market):   100 points
  1.00 – 1.04 (at market):      85 points
  0.95 – 0.99 (2–5% below):     70 points
  0.90 – 0.94 (6–10% below):    50 points
  0.85 – 0.89 (11–15% below):   30 points
  < 0.85 (15%+ below):          10 points

Example:
  Star QB, Market Value: $38M AAV
  GM Proposes: 3-year, $39M total = $13M AAV
  Salary_Ratio = $13M / $38M = 0.34
  Score: 10 points (deeply below market)
```

#### **Dimension 2: Term Length Preference (20% weight)**

Players prefer contract lengths that balance security with future upside.

```
Player Preference Formula:
  Younger players (1–5 years exp):    Prefer longer terms (4–5 years)
  Established (6–12 years exp):       Prefer medium terms (3 years)
  Veterans (13+ years exp):           Prefer shorter terms (1–2 years)

Term Score Calculation:
  Proposed_Term_vs_Preference:
    ±0 years difference:               100 points (ideal)
    ±1 year difference:                 85 points (acceptable)
    ±2 year difference:                 65 points (less ideal)
    ±3+ year difference:                40 points (not preferred)

Example:
  Young WR (3 years exp) prefers 4-year deals
  GM proposes: 3-year deal
  Score: 85 points (1 year shorter than preference)
```

#### **Dimension 3: Bonus Structures (10% weight)**

Front-loaded bonuses favor players (immediate cash); back-loaded favor teams (flexibility).

```
Bonus Preference Rules:

If Player is YOUNG (< 7 years):
  Prefer: Signing bonus + modest salary growth
  Score: 100 if signing bonus >= 10% of contract value
  Score:  70 if signing bonus 5–10%
  Score:  40 if signing bonus < 5%

If Player is ESTABLISHED (7–12 years):
  Prefer: Balanced signing + steady salary
  Score: 100 if signing bonus 8–15% & salary stable
  Score:  70 if signing bonus 5–8%
  Score:  40 if salary drops year-to-year

If Player is VETERAN (13+ years):
  Prefer: High signing bonus + back-loaded (cash now!)
  Score: 100 if signing bonus >= 15% of value
  Score:  70 if signing bonus 10–15%
  Score:  40 if signing bonus < 10%

Performance Bonus Handling:
  (Optional in MVP 1, focus on salary structure first)
  Generally slightly unfavorable to players (conditional earn)
  Reduce bonus structure score by 10–15% if heavy performance ties
```

#### **Dimension 4: Option Favorability (10% weight)**

Options change the power dynamic. Team options favor GMs (exit clause); player options favor players.

```
Option Scoring:

NO OPTION in Contract:
  Score: 80 points (neutral; most contracts have no option)

TEAM OPTION (team can decline):
  Player view: Unfavorable
  Score: 30 points (team can cut you; reduces security)
  Mitigation: Can boost score to 60 if option year is 3+ years out

PLAYER OPTION (player can decline):
  Player view: Favorable
  Score: 100 points (full control over future)

Example:
  QB contract with TEAM OPTION in Year 3
  Option Favorability Score: 30 points
  (Player prefers no option or player option)
```

### 2.2 Aggregate Scoring Formula

```
Offer_Score =
  (Salary_Score × 0.60) +
  (Term_Score × 0.20) +
  (Bonus_Score × 0.10) +
  (Option_Score × 0.10)

Score Range: 0–100

Example Calculation:
  Salary Score: 70 points × 0.60 = 42
  Term Score:   85 points × 0.20 = 17
  Bonus Score:  90 points × 0.10 = 9
  Option Score: 80 points × 0.10 = 8
  ────────────────────────────────────
  Total Offer Score: 76 points

Interpretation:
  76 is in the 70–84 range → Player counters (minor changes)
```

---

## Part 3: Decision Logic by Score Band

### 3.1 Player Response Matrix

```
Offer Score Range   Player Response         System Action
──────────────────────────────────────────────────────────────
85–100              ACCEPT ✓                Contract signed immediately
                                            (no counter-proposal)

70–84               COUNTER-PROPOSE        Generate counter with 5–10%
                    (negotiate)            salary adjustment + tweaks

50–69               COUNTER-PROPOSE        Generate counter with 10–15%
                    (firm counter)         salary adjustment + major changes

< 50                REJECT ✗                End negotiation
                    (walk away)            Player requests trade or
                                           waits for free agency
```

### 3.2 Player Counter-Proposal Logic (70–84 Band)

When score is 70–84, player wants better terms but is interested in negotiating.

```
Counter Strategy:
  1. Identify weakness in original offer
     - If salary < market: Bump AAV by 5–10%
     - If term too short: Add 1–2 years
     - If bonus too low: Add signing bonus or upfront cash

  2. Generate counter-proposal
     Formula: Adjust ONE major element + keep others similar

  3. Translate back to contract terms
     Example:
       Original: 3-year, $15M ($5M AAV) + $1M bonus
       Counter:  3-year, $16.5M ($5.5M AAV) + $1.5M bonus
       (10% salary bump, partial bonus increase)

  4. Ensure counter is "reasonable" (not outlandish)
     - Counter AAV <= 1.15 × Player Market Value (never jump 20%+)
     - Counter term within ±2 years of original
     - Counter cap hit acceptable to team (system checks)
```

### 3.3 Player Counter-Proposal Logic (50–69 Band)

When score is 50–69, player is frustrated and wants significant improvement.

```
Counter Strategy:
  1. Identify major weakness
     - Does salary fall short of market? (most common)
     - Is term way too short / too long?
     - Are bonuses/guarantees insufficient?

  2. Generate firmer counter
     Formula: Adjust by 10–15% on primary weakness

  3. Example:
       Original:  4-year, $20M ($5M AAV) | Market: $6M AAV
       Counter:   3-year, $19.5M ($6.5M AAV)
       Rationale: Reduce term (player wants flexibility)
                 + bump AAV closer to market ($6.5M vs. requested $6M)

  4. Boundary checks:
     - Counter AAV <= 1.20 × Player Market Value
     - If counter still < market, acknowledge gap in notes
     - System allows up to 5 total proposal rounds before timeout
```

---

## Part 4: Negotiation Flow Diagrams

### 4.1 Simple Negotiation (Quick Accept)

```
             ┌─────────────────────────────┐
             │  GM initiates negotiation   │
             │  (Extension or FA offer)    │
             └────────────┬────────────────┘
                          │
                          ▼
             ┌─────────────────────────────┐
             │  System validates cap room  │
             │  & offer reasonableness     │
             └────────────┬────────────────┘
                          │
                          ▼
             ┌─────────────────────────────┐
             │  Player evaluates offer     │
             │  Offer Score = 87 points    │
             └────────────┬────────────────┘
                          │
                          ▼
             ┌─────────────────────────────┐
             │  Score >= 85?               │
             │  YES ✓                      │
             └────────────┬────────────────┘
                          │
                          ▼
             ┌─────────────────────────────┐
             │  CONTRACT SIGNED ✓          │
             │  • Salary cap adjusted      │
             │  • Bonus paid immediately  │
             │  • Roster notified          │
             └─────────────────────────────┘

Time elapsed: ~5–10 seconds
```

### 4.2 Complex Negotiation (Multiple Rounds)

```
             ┌──────────────────────────────────┐
             │  GM Proposal 1                   │
             │  (e.g., 3-yr / $15M)             │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  System evaluates score = 72     │
             │  (70–84 band: negotiate)         │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  Player Counter 1                │
             │  (e.g., 3-yr / $16.5M)           │
             │  +5–10% salary bump              │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  GM sees counter & options:      │
             │  [A] Accept this counter    ✓    │
             │  [B] Modify & re-propose        │
             │  [C] Reject & walk away     ✗    │
             └────────────┬─────────────────────┘
                          │
                    Case [B]: Modify
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  GM Proposal 2                   │
             │  (e.g., 4-yr / $18M)             │
             │  (extend term, modest bump)      │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  System evaluates score = 82     │
             │  (70–84 band: still negotiating) │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  Player Counter 2                │
             │  (e.g., 4-yr / $19M)             │
             │  (accepts term, smaller bump)    │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  GM evaluates:                   │
             │  [A] Accept Counter 2       ✓    │
             │  [B] One more proposal          │
             │  [C] Walk away              ✗    │
             └────────────┬─────────────────────┘
                          │
                    Case [A]: Accept
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  CONTRACT SIGNED ✓               │
             │  4 years / $19M AAV              │
             │  Settlement: ~2 rounds           │
             └──────────────────────────────────┘

Time elapsed: ~30–60 seconds (UI interactions)
Proposal rounds: 2
```

### 4.3 Failed Negotiation (Walked Away)

```
             ┌──────────────────────────────────┐
             │  GM Proposal 1                   │
             │  (e.g., 2-yr / $8M)              │
             │  [Below market for star player]  │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  System evaluates score = 35     │
             │  (< 50: player rejects)          │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  Player Response: REJECT ✗       │
             │  Message: "Offer is unrealistic" │
             │           "Not interested in     │
             │            negotiating at this   │
             │            salary level."        │
             └────────────┬─────────────────────┘
                          │
                          ▼
             ┌──────────────────────────────────┐
             │  NEGOTIATION ENDS                │
             │  Player status options:          │
             │  • Request trade                 │
             │  • Wait for free agency          │
             │  • Remain on roster (unhappy)    │
             └──────────────────────────────────┘

Time elapsed: ~5 seconds
Outcome: Failed; team must rebuild relationship or
         move player in trade
```

---

## Part 5: Special Negotiation Scenarios

### 5.1 Re-Negotiation (Mid-Contract Adjustment)

**Trigger**: Player and team mutually agree to adjust existing active contract.

```
Scenario:
  Star player (Overall 85) has breakout season early in contract.
  Team wants to extend success; Player wants pay bump for new expectations.

Process:
  1. Either party initiates re-negotiation
  2. System presents current contract terms as baseline
  3. Both sides propose new structure:
     - Extend contract length: Add 1–2 years
     - Increase salary: Bump AAV by 10–20%
     - Modify structure: Make more back-loaded to reward continuation

  4. Same scoring/counter logic as extension negotiation
  5. If accepted: Void old contract, replace with new one
     (dead cap treated as "renegotiation credit" for cap purposes)

Cap Implication:
  Old contract dead cap = $2M
  New contract AAV bump = $3M
  Net cap increase = $1M (new AAV increase) not $3M (system credits old dead cap)
```

### 5.2 Trade Deadline Negotiation

**Trigger**: Player is traded to new team mid-season.

```
Flow:
  1. GM1 trades Player X to GM2 (cap compliance checked)
  2. GM2 inherits Player X's remaining contract
  3. Option: GM2 can immediately trigger renegotiation
     (Player may be upset about trade; higher offer needed to retain)

  4. If renegotiation happens:
     - Player starts Score lower (0–10% penalty for trade disruption)
     - GM2 must offer 3–8% salary bump to match market
     - Typical outcome: Quick re-negotiation + accept

  Example:
    Player traded W3 of season.
    Original contract: Remaining $5M this year + next 2 years ($5M each)
    GM2 offers: "$5.5M this year, $5.5M next 2 years" (small bump)
    Player evaluates as "acceptable given trade disruption"
    Negotiation settles in 1–2 rounds

Cap Mechanics:
  GM1 loses: RB salary from this point onward, frees up cap
  GM2 gains: RB salary this point onward, uses cap
  If renegotiation bump: GM2 must have cap room for increase
```

### 5.3 Expiring Contract (Final Year)

**Trigger**: Contract enters final year of term.

```
Flow:
  1. As contract approaches final year:
     - System notifies GM: "Player X contract expires after this season"
     - GM has these options:
       a) Let expire (player hits free agency)
       b) Extend with new contract (negotiate new terms)
       c) Trade player (avoid losing for nothing)

  2. If GM initiates extension:
     - Player likely to be amenable (security for Final Year player)
     - Offer Score boosted by 10–20% (player values known commodity)
     - Typically settles quickly at market rate

  Example:
    S "Safety Sam" in final year of 3-yr deal.
    Original contract AAV: $4M
    Market Value (now): $5M (due to age + performance improvement)

    GM offers: 2-year, $10M ($5M AAV)
    Player Score: 90 (market rate + stability boost)
    Player accepts quickly ✓

  If GM doesn't extend:
    Player hits free agency after season
    Now free to negotiate with 32 teams
    May leave for higher bidder (team loses player)
```

---

## Part 6: AI Negotiation Behavior (Non-Player Teams)

### 6.1 AI Team Offer Strategy

AI teams (other 31 franchises) also negotiate contracts. Their behavior should be moderate and realistic.

```
AI Offer Generation Logic:

For each free agent or extension target:

1. Calculate Player Market Value (same formula as human teams)

2. Determine AI "Budget Allocation":
   a) If AI team is rebuilding: Offer slightly below market (90–95%)
   b) If AI team is contending: Offer at market (100%)
   c) If AI team is star-building: Offer above market (110–115%)

3. Generate contract structure:
   - Young players: 4–5 year tiered contracts
   - Veterans: 2–3 year fixed
   - Mix in signing bonuses (20–30% of value) for stars

4. Avoid AI "Cheating":
   - Never violate cap limits (same as human teams)
   - Don't offer absurdly high/low contracts (realism checks)
   - Accept reasonable counters to avoid deadlock

5. Example:
   AI team pursuing RB "Runner" (Market Value: $4.5M AAV)

   AI Budget: ~$100M cap room, contending
   AI Offer: 3-year, $13.5M total ($4.5M AAV on market)
   Structure: $1M signing bonus, escalating salary

   Runner's counter: "I want 4 years"
   AI accepts upgrade: "Ok, 4 years at $18M ($4.5M AAV)"

   Negotiation settles in 1–2 rounds (AI is reasonable)
```

### 6.2 AI Negotiation Timeout & Walkaway

```
If negotiation drags past 5 rounds:
  - AI team may issue "Final Offer" ultimatum
  - Player has 1 more round to accept or it's off the table
  - If rejected: AI walks away, targets next free agent

Example:
  Round 5 (Stalemate):
    GM proposes: 3-year, $15M
    Player counters: 3-year, $18M

  Round 5.5 (AI deadline):
    AI: "This is my final offer: 3-year, $16M.
         Respond yes/no by end of day."

  Round 6:
    If player rejects: Negotiation ends; AI moves on to next target
```

---

## Part 7: Player Satisfaction & Morale

### 7.1 Contract Satisfaction Metric

After a contract is signed, the system tracks Player Satisfaction:

```
Signed_Salary_vs_Market_Value determines morale:

100%+ of market:      ✓✓ (HAPPY)      +morale, wants to stay
95–100% of market:    ✓  (SATISFIED)  neutral morale
90–94% of market:     ~  (OKAY)       slightly disappointed
85–89% of market:     ✗  (UNHAPPY)    morale penalty
< 85% of market:      ✗✗ (VERY UPSET) trade request likelihood

In-Game Effect:
  • Happy players: Small performance boost (+1 overall rating)
  • Unhappy players: Performance penalty (−1 overall rating)
  • Very upset: May request trade or hold out (future feature)
```

### 7.2 Contract Negotiation as Morale Event

If GM is seen as "unreasonable" during negotiation (very low initial offers, too many rejected counters), other players may lose confidence in the organization.

```
Cautionary Example:
  GM low-balls star QB with insulting offer (50% below market).
  Team morale dips slightly (perception of cheapness).
  Next free agency: Free agents less interested in team.

Positive Example:
  GM offers fair market rate to star player.
  Player accepts; team reputation improved.
  Next free agency: Free agents more likely to sign.
```

---

## Part 8: Practical Scenarios & Walkthroughs

### 8.1 Scenario: Signing a Young Star (Draft Pick)

**Context**:  
You drafted WR in 2024, he's now 2025 free agency eligible (RFA). He had breakout season (Overall 82). Market value: $8M AAV.

**Your Goal**: Lock him in long-term before he gets expensive.

```
PROPOSAL 1:
  Term: 5 years (secure, shows commitment)
  Salary: Year 1 $6M, Year 2 $7M, Year 3 $8M, Year 4 $9M, Year 5 $10M
  Signing Bonus: $2M (upfront cash)
  Total: $40M / $8M AAV

  System cap check: ✓ You have $22M cap room

PLAYER EVALUATION:
  Salary Score (40M / 5yrs = $8M AAV vs. market $8M):
    Score = 85 points (at market) × 0.60 = 51

  Term Score (5 years vs. young player's 4-year preference):
    Score = 85 points (ideal for young player) × 0.20 = 17

  Bonus Score (2M signing bonus of 40M total = 5%):
    Score = 40 points (slightly low) × 0.10 = 4

  Option Score (no option):
    Score = 80 points × 0.10 = 8

  TOTAL: 51 + 17 + 4 + 8 = 80 points (COUNTER-PROPOSE range)

PLAYER COUNTER:
  "I'll take the 5-year term, but I want a higher signing bonus."

  Counter: 5 years / $40M, but restructure:
    Signing Bonus: $3.5M (++$1.5M more upfront)
    Year 1–5 Salary: Adjusted down slightly
    Goal: More cash now, same total value

DECISION:
  Option A: Accept counter ✓
    (Slight adjustment; player stays happy & locked up 5 years)

  Option B: Reject & walk
    (Risk: Player hits true free agency, gets offers from 32 teams)

RECOMMENDED: Accept Counter
  Outcome: WR signed 5-year, $40M deal. Roster secured.
           Player Satisfaction: HAPPY (at-market deal + security)
```

### 8.2 Scenario: Veteran Re-Negotiation (Cap Crunch)

**Context**:  
Top RB (Overall 88) has 1 year left on contract at $6M. New star TE signed, new top-10 edge rusher acquired in trade. You're now $8M over cap for next year.

**Your Goal**: Renegotiate RB down, restructure to fit cap.

```
CURRENT CONTRACT:
  1 year remaining: $6M salary, $2M dead cap from signing bonus

PROBLEM:
  Next year cap projections: $213M committed, only $200M cap available
  Need to shed $13M to be compliant

OPTIONS:
  1) Cut RB: Save $6M salary, but $2M dead cap = net $4M savings (not enough)
  2) Trade RB: Save full $6M (but lose talent)
  3) Renegotiate RB: Extend contract, spread salary, reduce next-year hit

NEGOTIATION APPROACH (Option 3):

PROPOSAL 1:
  You approach RB: "I want to keep you, but I need cap relief.
  New structure: Extend from 1 year to 3 years, spread the salary."

  Offer: 3-year extension starting next year
    Year 1: $5M (instead of $6M, save $1M)
    Year 2: $5M
    Year 3: $5M
    New signing bonus: $1M (helps you cap-wise, helps player—cash upfront)
    Total: $16M / $5.33M AAV (DOWN from $6M, hurts player perception)

PLAYER EVALUATION:
  Salary Score ($16M / 3 years = $5.33M AAV vs. market $7.5M for RB of his rating):
    Score = 30 points (15% below market) × 0.60 = 18 (OUCH)

  Term Score (3 years vs. veteran's preference for 2):
    Score = 65 points × 0.20 = 13

  Bonus Score ($1M / $16M = 6.25%):
    Score = 40 points × 0.10 = 4

  Option Score (no option):
    Score = 80 points × 0.10 = 8

  TOTAL: 18 + 13 + 4 + 8 = 43 points (REJECT range)

PLAYER RESPONSE:
  "I'm not taking a pay cut. I've been loyal. Find another way."
  Negotiation ends ✗

YOUR PIVOT:
  (Next option: Trade RB to another team for picks,
   or cut player and accept cap penalty, or keep overages
   and wait for relief via other moves)

LESSON: Can't always renegotiate stars downward.
        Sometimes you must trade or cut high-salary veterans.
```

### 8.3 Scenario: Free Agency Bidding War

**Context**:  
Top FA CB hits market (UFA). Market value: $14M AAV. You want him badly.

```
COMPETING OFFERS IN LEAGUE:

Team A (contending): 4-year, $56M ($14M AAV) + $8M signing bonus
Team B (rebuilding): 3-year, $40M ($13.3M AAV) + $2M bonus
Your Offer:          4-year, $60M ($15M AAV) + $10M signing bonus

PLAYER EVALUATION OF YOUR OFFER:
  Salary Score ($15M AAV vs. market $14M):
    Score = 100 points (5% above market!) × 0.60 = 60

  Term Score (4 years vs. CB's preference for 3):
    Score = 85 points × 0.20 = 17

  Bonus Score ($10M / $60M = 16.7%):
    Score = 90 points × 0.10 = 9

  Option Score (no option):
    Score = 80 points × 0.10 = 8

  TOTAL: 60 + 17 + 9 + 8 = 94 points (ACCEPT!)

RESULT:
  CB signs with you ✓
  Contract: 4-year, $60M / $15M AAV
  Cap hit: Significant ($15M Y1), but you secure top-tier talent
  Other teams' offers rejected

OUTCOME:
  You win bidding war. CB happy (above-market deal).
  Your roster strengthened.
  But cap constrained for next 3 years.
```

---

## Part 9: Implementation Checklist

### For Developers

- [ ] **Domain Models**: Create `Contract`, `ContractYear`, `OptionClause` entities
- [ ] **Market Value Formula**: Implement scoring algorithm (position, age, archetype, rating)
- [ ] **Offer Scoring System**: Build 4-dimension scoring (salary, term, bonus, option)
- [ ] **Counter-Proposal Logic**: Implement counter-generation (adjust by 5–15%)
- [ ] **Cap Validation**: Ensure all offers/trades blocked if over cap
- [ ] **Dead Cap Tracking**: Implement dead cap amortization on cuts/trades
- [ ] **UI Components**: Build proposal form, counter-display, negotiation history
- [ ] **AI Negotiation**: Code AI team offer generation (avoid extreme offers)
- [ ] **Morale Integration**: Link contract satisfaction to player performance ratings

### For Quality Assurance

- [ ] Test acceptance criteria from § 1.2 (PLAYER_CONTRACTS.md)
- [ ] Verify cap enforcement across 32 teams
- [ ] Stress test: 1,000+ simultaneous negotiations (performance)
- [ ] Boundary cases: Very high market stars, very low-rated depth players
- [ ] Trade deadline: Ensure cap compliance when trading players
- [ ] Morale: Spot-check that salary satisfaction feeds into performance

### For Design/UI

- [ ] Sketch contract proposal form (Salary, Term, Bonus, Options inputs)
- [ ] Sketch counter-proposal display (side-by-side comparison of offers)
- [ ] Sketch cap room indicator (visual gauge + numeric display)
- [ ] Sketch negotiation history view (proposal round #, dates, responses)
- [ ] Sketch contract termination flow (warning: "You'll create $X dead cap")

---

## Glossary

| Term                 | Definition                                                                 |
| -------------------- | -------------------------------------------------------------------------- |
| **Counter-Proposal** | Player/agent's response to GM offer with modified terms                    |
| **Dead Cap**         | Guaranteed money still counted against cap after player is cut/traded      |
| **Offer Score**      | System's 0–100 rating of how attractive a contract proposal is to a player |
| **Market Value**     | Estimated realistic AAV based on player performance, position, age         |
| **Proposal Round**   | Single back-and-forth offer/counter cycle                                  |
| **Re-Negotiation**   | Mutual agreement to modify an existing active contract mid-term            |
| **Trade Deadline**   | Last allowed date to trade players (typically mid-season)                  |

---

## Document History

| Version | Date       | Author | Notes                                                   |
| ------- | ---------- | ------ | ------------------------------------------------------- |
| 1.0     | 2026-03-24 | —      | Initial document; detailed negotiation flow + scenarios |
