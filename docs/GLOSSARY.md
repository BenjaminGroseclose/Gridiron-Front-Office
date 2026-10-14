# Glossary

> Football, salary cap and project terms used across the docs. This file also seeds the in-app glossary tooltips (F-27).

## Football and roster

| Term | Meaning |
|---|---|
| **53-man roster** | The active roster limit during the regular season. Offseason limit: 90. |
| **Practice squad (PS)** | Up to 16 players who practice but don't play. Other teams can sign them away ("poaching"). |
| **IR (injured reserve)** | A list for injured players. They don't count against the 53, but must stay out a minimum number of games. |
| **Depth chart** | The ordered list of players at each position and slot, which determines who plays. |
| **Personnel package** | Offensive grouping by the number of RBs and TEs (11 = 1 RB + 1 TE + 3 WR, 12 = 1 RB + 2 TE, 21, 22, 10…). Defensive packages: Base, Nickel (5 DBs), Dime (6 DBs). |
| **Game plan** | The user's weekly strategy settings (balance, aggression, blitz rate, coverage, focus). The user's main lever on game day. |
| **Bye week** | The one week each team doesn't play during the regular season. |
| **Seed** (playoffs) | Playoff ranking within a conference (1–7). The #1 seed gets a first-round bye. |
| **Tiebreakers** | An ordered set of rules for ranking teams with equal records (head-to-head, division record, …). |
| **Waivers** | When a player is released, other teams can claim him in priority order (reverse standings). |
| **UDFA** | Undrafted free agent: a prospect who goes unpicked and can sign with any team after the draft. |
| **Combine** | A pre-draft event where prospects are measured and tested (40-yard dash, bench, vertical, …). |
| **Pro day** | A prospect's school-hosted workout for scouts. |
| **Big Board** | A team's own ranked list of draft prospects. |
| **BPA** | "Best player available": drafting the highest-rated prospect regardless of need. |
| **Trade up / down** | Trading picks to move earlier (up) or later (down) in the draft. |
| **Draft value chart** | A curve assigning trade value to each pick number. |
| **OVR** | Overall rating: a position-weighted summary of a player's attributes. |
| **Potential** | A player's hidden ceiling. Always uncertain in GFO. |

## Contracts and salary cap

| Term | Meaning |
|---|---|
| **Salary cap** | The maximum total cap hits a team may carry in a league year. |
| **Cap hit** | How much a contract counts against the cap in a given season (base salary + prorated bonus + other bonuses). |
| **APY** | Average per year: total contract value ÷ years. The standard way to compare contracts. |
| **Signing bonus** | Money paid up front, but **prorated** evenly over up to 5 years of the contract for cap purposes. |
| **Proration** | Spreading a signing bonus's cap charge across contract years. |
| **Guaranteed money** | Salary owed even if the player is released. |
| **Dead money** | Cap charges for players no longer on the team (remaining proration and guarantees accelerated on release). |
| **Cap rollover** | Unused cap space carried forward into the next league year. |
| **Franchise tag** | A one-year contract at a high, position-based salary that keeps a player off the open market. One per team per year. |
| **Extension** | A new contract signed before the current one expires. |
| **Restructure** | (v2) Converting base salary into a signing bonus to lower the current cap hit, at the cost of future cap hits. |
| **June 1 designation** | (v2) A release timing rule that splits dead money across two league years. |
| **Rookie wage scale** | Fixed, slotted contract values for drafted rookies, based on pick number. |
| **5th-year option** | (v2) A team option to extend a first-round pick's rookie deal by one year. |
| **Top 51 rule** | From the new league year until the regular season, only a team's 51 highest cap hits (plus dead money) count against the cap, so 90-man offseason rosters fit ([D-056](DECISIONS.md#d-056-the-offseason-top-51-cap-rule-is-in-amends-d-040)). |
| **UFA / ERFA** | Unrestricted and exclusive-rights free agents, by accrued seasons. In GFO, an expiring player with fewer than 4 accrued seasons is exclusive-rights: his team keeps him with a one-year qualifying offer. There's no RFA tier in 1.0 ([D-040](DECISIONS.md#d-040-salary-cap-realism-tiers)). |
| **League year** | The contract and cap year, which begins when free agency opens in March. |

## Analytics

| Term | Meaning |
|---|---|
| **EP (expected points)** | The average points a team can expect from a given down, distance and field position. |
| **EPA** | Expected points added by a play: EP after − EP before. |
| **WP (win probability)** | The chance of winning from the current game state. |
| **WPA** | Win probability added by a play. Used to find swing plays. |
| **Success rate** | The share of plays that gain "enough" yards for the situation. |
| **Explosive play** | A run of 10+ yards or a pass of 20+ yards (typical definitions). |
| **Pressure rate** | The share of dropbacks where the QB is hurried, hit or sacked. |
| **Deciding matchup** | GFO's per-play record of which player won the rep that determined the outcome. |

## Project terms

| Term | Meaning |
|---|---|
| **GFO** | Gridiron Front Office (the working title). |
| **Save / league file** | One SQLite database (`.gfo`) per league. It's always live, with autosave. |
| **Snapshot** | An automatic point-in-time copy of a save, taken at phase boundaries. Restorable via the Time Machine. |
| **Time Machine** | The screen listing snapshots for deliberate restores. |
| **Ironman** | A league setting, locked at creation, that requires the Live clock mode and disables restores and duplicates. The only exception is automatic recovery from a corrupted file. |
| **League date / phase / stop** | The in-game calendar date, the coarse league-year state, and the next point the Advance button moves to. |
| **Advance** | The primary action that moves the league calendar to the next stop. |
| **Blocking issue** | A problem that must be fixed (error) or acknowledged (warning) before advancing. |
| **Live event** | A phase step that runs in real wall-clock time (the draft; later FA Frenzy and Trade Deadline Day). |
| **Clock mode** | A per-save setting chosen at league creation. **Live** (default): live events can be accelerated, never paused. **Pausable**: live events can be paused. |
| **On the clock** | The state of being the team that must make the current draft pick. |
| **Pick queue** | Pre-set "take X if available" choices, applied when you're on the clock. |
| **Auto-pick** | The pick made automatically from your Big Board when your clock reaches 0:00. |
| **Trade phone** | Draft Room panel showing incoming AI trade offers with countdowns. |
| **Standing / special assignment** | How scouts are directed. A *standing* assignment (a region or a position group) sharpens every covered prospect automatically, down to a floor. A *special* assignment (one prospect) goes past that floor ([D-058](DECISIONS.md#d-058-scouting-department-standing--special-assignments-no-biases-in-10)). |
| **Fog of war** | Hidden true ratings. Teams see *perceived* ratings with uncertainty (σ). |
| **Perceived rating / σ** | A team's estimate of a rating, and the uncertainty around it. |
| **Valuation model** | The single shared service that answers "what is this player worth?" for contracts, trades, the draft and the AI. |
| **SimLab** | A console tool for calibration, league autoplay, EP/WP table generation and benchmarks. |
| **Calibration bands** | Target ranges for league-wide stats that the engine must hit. |
| **Autoplay** | Running the league with AI controlling all 32 teams, for health testing. |
| **League health** | Metrics that show the league stays sane over decades (cap, talent distribution, parity). |
| **MLP** | Minimum Lovable Product: the end of M6. |
| **DevHost** | The browser-based development host for the UI. |
| **Gallery** | The DevHost page showing every design system component and state. |
| **Single-writer queue** | The rule that all save mutations run one at a time through `ISaveWriter`. |
| **Unit of work** | The object a command receives from `SaveWriter`. It's bound to one connection and one transaction, and it's the only way feature code touches the database. |
| **Transaction owner** | `SaveWriter`, the only code allowed to begin, commit or roll back a transaction. |
| **Torture tests** | Tests that prove transactions hold up under exceptions, process kills, concurrent reads and races. They run on every build from M0. |
| **Step journal** | A persisted record of completed steps that makes long advances resumable after a crash. |
