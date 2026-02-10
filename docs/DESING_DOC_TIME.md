# Game Design: Hybrid Time System (Macro vs. Micro)

**Project:** Gridiron Front Office Simulator  
**Core Concept:** Variable Time Scale / Hybrid Real-Time  
**Goal:** To balance the deep, analytical pacing of a management sim with the high-stakes pressure of critical NFL deadlines.

---

## 1. The Core Loop: "Calm Tycoon" vs. "Panic Room"
The game operates on two distinct time scales. The transition between these modes creates the emotional rhythm of the game.

1.  **Macro Mode (The Standard):** Traditional turn-based or daily advancement. Used for 90% of the season (regular weeks, offseason lulls). Low stress, high information processing.
2.  **Micro Mode (The Event):** Pausable Real-Time. Used for specific, high-intensity events (Draft, Trade Deadline, Gameday). High stress, rapid decision-making.

---

## 2. Macro Mode: "The Tycoon"
* **Time Scale:** Daily / Weekly.
* **Control Scheme:** "Next Day" / "Simulate Week" buttons.
* **Player Experience:** Calculating, analytical, relaxed.
* **Active Phases:**
    * Regular Season Tuesdays–Saturdays (Practice week).
    * Early Offseason (Cap management, staff hiring).
    * Pre-Season training camps.

### Gameplay Focus
* **Roster Management:** Setting depth charts, signing low-level free agents.
* **Scouting Logistics:** Assigning regions and scouts without time pressure.
* **Financials:** Adjusting ticket prices, stadium upgrades, contract restructuring.

---

## 3. Micro Mode: "The War Room"
* **Time Scale:** Seconds/Minutes (Real-Time Clock).
* **Control Scheme:** Play/Pause, Speed Controls (1x, 2x, 5x), Ticker Tape.
* **Player Experience:** Reactive, stressful, adrenaline-fueled.
* **UI Changes:** * "Next Day" button is replaced by a digital clock (e.g., `11:45 AM`).
    * Music tempo increases.
    * Visual "Red Alert" or distinct color palette shift to indicate live status.

### Trigger Events

#### A. The Trade Deadline (Final 12 Hours)
* **The Trigger:** The game automatically stops at 8:00 AM on Deadline Day.
* **The Mechanic:** * Offers expire in real-time (e.g., "This offer is on the table for 1 hour").
    * AI GMs become hyper-active, spamming offers and counter-offers via a "Live Feed."
    * **The Risk:** If the clock hits 4:00 PM EST while you are reviewing a contract, the trade window slams shut. You miss the deal.

#### B. The Draft (3 Days in April)
* **The Trigger:** The Commissioner walks to the podium.
* **The Mechanic:** * **Draft Clock:** Each team has a strict time limit (e.g., 10 minutes scaled to 60 seconds of real-time).
    * **Live Trading:** You must negotiate trade-ups/trade-downs while the clock is ticking.
    * **Fog of War:** Picks are announced live. You don't know who is taken until the card is read.

#### C. Game Day (The Sideline)
* **The Trigger:** Kickoff (Sunday 1:00 PM).
* **The Mechanic:** * The match engine runs in real-time (accelerated).
    * **Intervention:** The player can "Call Timeout" (Pause) to adjust tactics, sub injured players, or change play-calling aggression.
    * **Passive vs. Active:** The player can watch the whole game or just "Sim to End," but watching allows for live tactical adjustments that a straight sim does not.

---

## 4. Technical Implementation Notes

### State Management
The Game Loop needs a `TimeState` flag:
```typescript
enum TimeState {
  TURN_BASED, // Standard daily processing
  REAL_TIME   // Tick-based processing
}

// In Real-Time mode, the engine processes 'ticks' instead of 'days'
// 1 Tick = 1 Minute of game world time