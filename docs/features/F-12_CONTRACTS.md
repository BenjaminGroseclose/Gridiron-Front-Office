# F-12: Contracts & salary cap

> **Status:** Proposed (drafted by Claude 2026-10-21 for owner review; revised the same day on the owner's feedback, [D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle); the **M1 slice only**; the cap fit and the pay curve revised by [D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application) on 2026-10-22) · **Milestone:** M1 (structure, cap-hit formula, generated contracts), M4 (actions, rules, the cap sheet), M9 (v2) · **Owner:** Ben · **Last updated:** 2026-10-22
>
> Decisions this slice implements: [D-040](../DECISIONS.md#d-040-salary-cap-realism-tiers) (realism tiers), [D-056](../DECISIONS.md#d-056-the-offseason-top-51-cap-rule-is-in-amends-d-040) (league-rules default), [D-074](../DECISIONS.md#d-074-contracts-are-real-from-m1-contract-actions-arrive-in-m4) (real contracts from M1), [D-077](../DECISIONS.md#d-077-data-files-carry-no-version-numbers-they-change-only-additively) (additive data files), [D-078](../DECISIONS.md#d-078-m1-builds-for-todays-consumers-fifteen-simplifications) (build for today's consumers), [D-079](../DECISIONS.md#d-079-wp3-player-data-traits-in-generationjson-measurable-priors-deferred-f-12-before-contracts) (this spec before `contracts.json`), [D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle) (the owner's answers to this spec's questions), [D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application) (trades fit the cap; the curve is 2.5). The M4 MVP (actions, dead money, tags, extensions, Top 51, the cap sheet) and the M9 v2 are listed in [FEATURES.md](../FEATURES.md#-f-12-contracts--salary-cap) and get their sections when M4 is planned.
>
> **Real references** are the 2026 NFL league year, the last one before this was written: the cap ([NFL.com](https://www.nfl.com/news/nfl-announces-2026-salary-cap-set-at-301-2-million-per-team)), the minimum-salary ladder ([Yahoo Sports](https://sports.yahoo.com/articles/nfl-veteran-minimum-salary-2026-070038653.html)), the rookie scale ([DAZN](https://www.dazn.com/en-US/news/football/2026-nfl-draft-rookie-salaries-pay-scale-first-round/buqiyhho5tvi1tk3d2gjd4ce1), [Yahoo Sports](https://sports.yahoo.com/articles/nfl-draft-salary-pick-much-054002783.html)), the top of the market by position ([CBS Sports](https://www.cbssports.com/nfl/news/highest-paid-players-at-every-nfl-position-2026/), [Yahoo Sports](https://sports.yahoo.com/articles/nfls-highest-paid-players-position-170446357.html)), and the CBA's season definitions ([NFL Operations](https://operations.nfl.com/inside-football-ops/nfl-operations/nfl-free-agency/types-of-free-agents), [Over The Cap's CBA text](https://overthecap.com/collective-bargaining-agreement/article/8/section/1)). Where a number is GFO's own choice rather than the league's, §4 says so.

---

## 1. Summary

A contract is the GM's core puzzle: talent against money, across years. In M1 nobody signs, cuts or extends anyone, but every player already has a **real contract**: years, a base salary per season, a signing bonus prorated over at most five seasons, guaranteed money, and each season's cap hit from the real formula. Every team starts under a first-season cap, young players carry rookie-scale deals, and the Player and Team pages show it all ([D-074](../DECISIONS.md#d-074-contracts-are-real-from-m1-contract-actions-arrive-in-m4)). This slice defines **the structure**, **the cap-hit arithmetic**, **the numbers** (`data/contracts/contracts.json`) and **the generation rules** WP4 follows, so that M4's actions land on data that already looks and adds up like a cap table. Serves pillars **1 (meaningful decisions)** and **3 (believable and explainable)**.

## 2. Goals / non-goals

**Goals (M1)**
- The contract structure in the save: `Contracts` and `ContractYears`, with each season's cap hit computed, never stored by hand (§5).
- The cap-hit formula as pure Core functions. `CapMath` already exists (WP1); this spec cites it and adds nothing to it (§4.2).
- A first-season cap, a minimum-salary ladder, a rookie scale by round, a veteran pay curve by position, contract lengths by age and tier, and bonus and guarantee shares, all as data (§4.3–4.7, §4.9).
- Generation rules that make every team's total land under the cap at creation, with players mid-contract (§4.8).
- Read models for the Player page (the contract) and the Team page (the cap totals) (§5, §8).

**Non-goals (M1)**
- Every action and rule: signing, release and dead money, the franchise tag, extensions, the rookie scale and minimums *as rules*, Top 51, the cap sheet and validation: **M4**. v2's 5th-year option, restructures and June 1 designations: **M9** ([D-040](../DECISIONS.md#d-040-salary-cap-realism-tiers)).
- Cap growth and rollover between seasons: M4. M1 has one season's cap.
- The valuation model that says what a player *should* earn (F-13, M4). M1's pay curve is first-pass data, tuned by WP4's distribution tests, exactly like F-03's OVR weights.
- **Practice squads:** not in 1.0 at all ([D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle)). Rosters are the 53; injury replacements come from the free-agent pool.
- Incentives, roster bonuses, void years, offsets: not in 1.0.

## 3. Player stories

- As the GM, I open a player and see his contract the way a real cap table shows it: years left, this season's cap hit, the signing bonus still prorating, what's guaranteed.
- As the GM, I open my team and see where I stand against the cap before I've made a single move, and which contracts weigh the most.
- As the GM, I expect a 23-year-old second-round pick to be cheap and a 29-year-old starting quarterback to be expensive, and the team's total to be a plausible 90-odd percent of the cap.
- As the GM, I expect the same receiver to be worth about the same money whichever team he plays for: pay follows the player, not the depth chart he happens to sit on.

## 4. Rules and formulas

### 4.1 Terms

| Term | Definition | Source |
|---|---|---|
| **League year, season** | One cap year = one season. M1's league starts at Training Camp of its first season ([WP5](../milestones/M1_THE_LEAGUE.md#wp5--schema-and-createleague-m)); the cap table is that season's. | — |
| **Cap hit** | What a contract counts against the cap in a season: base salary plus that season's share of the signing bonus (§4.2). | NFL accounting |
| **Accrued season** | A season with the player on full-pay status for **6 or more** regular-season games. Drives free agency status (3 accrued → restricted, 4 → unrestricted; fewer than 4 is exclusive-rights in GFO, D-040). | [NFL Operations](https://operations.nfl.com/inside-football-ops/nfl-operations/nfl-free-agency/types-of-free-agents) |
| **Credited season** | A season with **3 or more** games on full-pay status. Drives pay: the minimum-salary ladder. | [Over The Cap, CBA Art. 8 / 26](https://overthecap.com/collective-bargaining-agreement/article/8/section/1) |
| **Experience** (`Players.Experience`, F-03 §4.8) | **One number** (D-080): the seasons since the player's entry season, serving as both counts. The two definitions above apply from M3, when the sim produces games. | — |
| **Entry season** | For a drafted player, his draft season (F-03 §4.8's draft info). For an undrafted player, the season he first signed. | — |
| **Contract tier** | Starter or Backup **by league-wide rank at the position** (§4.7), not by a team's depth chart. F-03's generation tier (the top `roster.starters` on each *team*) shapes skills; it isn't what pay keys on. | D-080 |

### 4.2 The cap-hit formula (built in WP1)

`Core/Contracts/CapMath.cs` is the whole arithmetic, and this spec adds nothing to it:

- `MaxContractYears = 7`, `MaxProratedYears = 5`.
- `ProratedBonus(signingBonus, years, yearIndex)`: the bonus is spread evenly over the first `min(years, 5)` seasons; the division's remainder goes in the first season, so the shares add up to the bonus exactly; seasons 6 and 7 carry none.
- `CapHit(baseSalary, signingBonus, years, yearIndex) = baseSalary + ProratedBonus(…)`.

`yearIndex` is counted from the **signed season**, so a contract signed two seasons ago is in `yearIndex = 2` this season. Money is `long` dollars everywhere.

### 4.3 The cap and the utilization band

| Value | GFO (M1) | Real reference |
|---|---|---|
| First-season cap | **$300,000,000** (owner, D-080) | $301,200,000 for 2026 ([NFL.com](https://www.nfl.com/news/nfl-announces-2026-salary-cap-set-at-301-2-million-per-team)). A round figure for a fictional league; growth and variance come in M4. |
| Utilization at creation | each team's total between **85% and 97%** of the cap | Real teams enter a season within a few percent of the cap, rollover aside. GFO's own band; §4.8 says how a team is brought into it. |

**What counts** at creation: the current season's cap hit of every player on the 53. Unsigned free agents have no contract and count nothing. (With no practice squad in 1.0, there's nothing else to count; Top 51 in M4 only ever loosens this check.)

### 4.4 Minimum salaries, by credited seasons

The 2026 ladder, used as is ([Yahoo Sports](https://sports.yahoo.com/articles/nfl-veteran-minimum-salary-2026-070038653.html)):

| Credited seasons | Minimum base salary |
|---|---|
| 0 | $885,000 |
| 1 | $1,005,000 |
| 2 | $1,075,000 |
| 3 | $1,145,000 |
| 4–6 | $1,215,000 |
| 7+ | $1,300,000 |

The file holds the steps (`creditedSeasons` → `salary`); a player's minimum is the last step at or below his credited seasons. **Every base salary in every contract is at least the minimum for the seasons the player will have in that season** (experience plus the season's offset from the current one).

### 4.5 The rookie scale (drafted players)

Real rookie contracts are four years, slotted by pick, mostly guaranteed at the top ([DAZN](https://www.dazn.com/en-US/news/football/2026-nfl-draft-rookie-salaries-pay-scale-first-round/buqiyhho5tvi1tk3d2gjd4ce1), [Yahoo Sports](https://sports.yahoo.com/articles/nfl-draft-salary-pick-much-054002783.html): pick 1 $54.57M, pick 32 $16.17M in 2026). GFO keeps the shape with **one row per round** and a straight line between the round's first and last pick (the real first round bends steeply at the top; a straight line pays picks 2–10 less than reality, accepted for M1 by the owner, D-080):

| Round | First pick, 4-year total | Last pick | Signing bonus share | Guarantee share | Real anchor |
|---|---|---|---|---|---|
| 1 | $54,400,000 | $16,100,000 | 0.60 | 1.00 | picks 1 and 32, 2026 (bonus ≈ 66% at the top, 51% at 32; first-rounders are fully guaranteed) |
| 2 | $11,000,000 | $6,500,000 | 0.30 | 0.75 | picks 33 and 64, projected from 2025 (+10%); the bonus runs from ~45% at 33 to ~28% at 64, one share per round |
| 3 | $6,300,000 | $5,300,000 | 0.18 | 0.45 | picks 65 and ~100 |
| 4 | $4,900,000 | $4,700,000 | 0.10 | 0 | |
| 5 | $4,600,000 | $4,500,000 | 0.08 | 0 | |
| 6 | $4,500,000 | $4,400,000 | 0.05 | 0 | mostly four minimum salaries plus a small bonus |
| 7 | $4,400,000 | $4,300,000 | 0.03 | 0 | |

Rules:
- **Four years**, signed in the draft season. The total for pick *p* of *n* in the round is the straight line from `firstPick` to `lastPick`; the bonus is `bonusShare × total`.
- **Base salaries:** the four minimums for seasons 0–3 of the player's career, plus the rest of the money (`total − bonus − Σ minimums`) split by `baseWeights` **[0.10, 0.20, 0.30, 0.40]**, so bases rise each year as real ones do. The file must satisfy `total × (1 − bonusShare) ≥ Σ minimums(0..3)` for every pick (test rule R5), or a late pick's bases would fall under the minimum.
- **Guarantees:** the bonus, plus base salaries guaranteed from the first season on until `guaranteeShare × total` is reached; a share of 0 means the bonus only.
- A player **drafted within the last four seasons** (experience 0–3) is on his rookie deal at creation, `elapsed = experience` (§4.8). Older drafted players are veterans.

### 4.6 Undrafted rookies and minimum deals

- An **undrafted player with experience 0–2** is on his undrafted deal: **three years**, type `Minimum`, each base the minimum for his seasons, a flat signing bonus of **$20,000** (real UDFA bonuses run from nothing to a few hundred thousand), nothing else guaranteed.
- A **veteran on a minimum deal** (§4.7 says who): **one year**, type `Minimum`, base = his minimum, no bonus, nothing guaranteed.

### 4.7 Veteran deals: pay by quality, league-wide

A veteran is anyone on the 53 not covered by §4.5–4.6. His pay follows **where he ranks among every player at his position in the league**, so the same player earns about the same wherever he is (the owner's point, D-080: a receiver who is the fifth-best on one team is the third-best on another, and the depth chart must not change his price).

**The rank.** `t` = the player's rank at his position across all 32 rosters, as a percentile from 0 (the worst) to 1 (the best), by true OVR. (The generator has true ratings; the fog of war is a display matter, F-03 §4.9.)

**The contract tier.** The top `32 × roster.starters` players at the position (from `positions.json`) are **Starters**, the rest **Backups**. The tier picks the length and share bands below; it never changes the money curve.

**The pay curve.** Each position has one band in the file: a `max` APY in dollars; the `min` is always the player's minimum salary (§4.4), so there's nothing to set (D-080: "no need to have a min"). Then

`apy = min + (max − min) × t^curve`, with **`curve` = 2.5** for every position (one number in the file; 3.5 in the first draft, lowered by D-081),

and the **wiggle** the owner asked for: `apy × (1 + w)`, `w ~ N(0, wiggle.sd)` clamped to `± wiggle.max` (**0.08** and **0.20**), so a deal can land up to 20% outside the shape of its band and the bands read as rules of thumb, not walls. The power keeps the top of a band rare: with 2.5, a position's median player earns about 18% of the way from his minimum to the max, a player at the 90th percentile about 77%, and only the top few percent come near the max.

A veteran whose `apy` lands within 5% of his minimum gets the §4.6 minimum deal instead (type `Minimum`).

First-pass maxima, in $M, around 70% of the 2026 top of the market ([CBS Sports](https://www.cbssports.com/nfl/news/highest-paid-players-at-every-nfl-position-2026/), [Yahoo Sports](https://sports.yahoo.com/articles/nfls-highest-paid-players-position-170446357.html): QB $63M, EDGE $50M, WR $42M, CB $31M, OT $30M, DT $32M, RB $21M, LB $21M, S $21M, TE $19M, K $7M), because the record deal at each position is one player, not a band:

| QB | RB | FB | WR | TE | OT | OG | C | EDGE | DT | LB | CB | S | K | P | LS |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 45 | 13 | 3 | 30 | 13 | 22 | 17 | 12 | 35 | 22 | 15 | 22 | 15 | 5.5 | 3.2 | 1.5 |

With these maxima and `curve` 2.5, a roster of 53 at the curve's expected values costs about 1.21 × cap (the R10 check below). Rookie and minimum deals fill about 45% of a generated roster, which brings the league's mean to about 92% of the cap (D-081's reference run), so the trades in §4.8 stay few.

**Length**, by tier and age (the file's `lengths`; `min`–`max` drawn uniformly):

| Tier | ≤ 26 | 27–29 | 30–32 | 33+ |
|---|---|---|---|---|
| Starter | 4–5 | 3–4 | 2–3 | 1–2 |
| Backup | 2–3 | 1–2 | 1 | 1 |

**Total value** = `apy × years`. **Signing bonus** = `total × s`, `s` drawn uniformly from the tier's `bonusShare` band: Starter **0.25–0.40**, Backup **0.00–0.15**. **Guarantees** = `total × g`, `g` from the tier's `guaranteeShare` band: Starter **0.45–0.65**, Backup **0.10–0.40**; the bonus counts first, then base salaries from the first season on until the amount is reached (the reference deals guarantee early seasons, never late ones). Guaranteed money is generated and shown in M1 (owner, D-080); nothing acts on it until M4's releases. **Base salaries** = `total − bonus`, split across the years with weights `1, 1+e, 1+2e, …` (`baseEscalation` **e = 0.08**), then each raised to its season's minimum if it fell under it (the excess comes out of the last season).

The shares are GFO's first pass. The references are anecdotal (a top-of-market deal around 75% guaranteed with a bonus near 17% of total; mid-tier deals near 30% fully guaranteed), so the bands are set to produce cap tables that *look* right, and WP4's tests, then F-13 in M4, tune them.

### 4.8 Mid-contract at creation, and staying under the cap

1. **Elapsed seasons.** A veteran deal was signed `elapsed` seasons ago, `elapsed` uniform in `0 … min(years − 1, experience)`, so a contract never predates the player's entry and always has a season left. A rookie deal has `elapsed = experience`. The signed season is `currentSeason − elapsed`, and this season's cap hit is `CapHit(base[elapsed], bonus, years, elapsed)`.
2. **Every ContractYear is written**, past seasons included (D-074: proration and history span earlier seasons).
3. **The team total** = Σ current-season cap hits over the 53 (§4.3).
4. **Into the band, by trades** ([D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application)). No contract is changed to fit a team. While any team's total is outside `utilization.min`–`max` × cap, the team with the highest total and the team with the lowest swap two players: **both on Veteran contracts, at the same position and in the same generation tier** (F-03 §4.10, so each team keeps a starter where it had one), the higher cap hit going to the lower team, choosing the pair whose difference in cap hits comes closest to half the gap between the two teams. The difference must be smaller than the whole gap, or the trade would only swap which team is high. The players keep their contracts. It runs once, at league creation (M4's rules take over afterwards); a reference run needed about 25 trades per league. A team no trade can bring into the band stays outside it and is logged (§11); the tests require that it never happens.
5. **Determinism:** every draw comes from the player's generation seed (`Hash(leagueSeed, "gen", season, slot)`, F-03 §4.10) through the contract purpose, so the same seed gives the same cap table. The league-wide rank is computed once, after every player exists and before any contract is drawn. The trades draw nothing; ties go to the lower team index, then the lower slot.

### 4.9 The data file: `data/contracts/contracts.json`

_Built 2026-10-21 (WP3's last slice), with `ContractData`, `ContractType`, `ISeedData.Contracts` and `ContractDataTests`._ Like WP3's files (guide §2): camelCase fields, enum values and enum dictionary keys as the C# names, no version field, additive only (D-077). Money in dollars as integers.

```json
{
  "cap": 300000000,
  "utilization": { "min": 0.85, "max": 0.97 },
  "minimumSalaries": [
    { "creditedSeasons": 0, "salary": 885000 }, { "creditedSeasons": 1, "salary": 1005000 },
    { "creditedSeasons": 2, "salary": 1075000 }, { "creditedSeasons": 3, "salary": 1145000 },
    { "creditedSeasons": 4, "salary": 1215000 }, { "creditedSeasons": 7, "salary": 1300000 }
  ],
  "rookieScale": {
    "years": 4,
    "baseWeights": [0.10, 0.20, 0.30, 0.40],
    "rounds": [
      { "round": 1, "firstPick": 54400000, "lastPick": 16100000, "bonusShare": 0.60, "guaranteeShare": 1.00 },
      { "round": 2, "firstPick": 11000000, "lastPick": 6500000, "bonusShare": 0.30, "guaranteeShare": 0.75 },
      "…",
      { "round": 7, "firstPick": 4400000, "lastPick": 4300000, "bonusShare": 0.03, "guaranteeShare": 0 }
    ]
  },
  "undrafted": { "years": 3, "bonus": 20000 },
  "veterans": {
    "curve": 2.5,
    "wiggle": { "sd": 0.08, "max": 0.20 },
    "maxApy": { "QB": 45000000, "RB": 13000000, "FB": 3000000, "WR": 30000000, "TE": 13000000, "OT": 22000000, "OG": 17000000, "C": 12000000, "EDGE": 35000000, "DT": 22000000, "LB": 15000000, "CB": 22000000, "S": 15000000, "K": 5500000, "P": 3200000, "LS": 1500000 },
    "lengths": {
      "Starter": [ { "throughAge": 26, "min": 4, "max": 5 }, { "throughAge": 29, "min": 3, "max": 4 }, { "throughAge": 32, "min": 2, "max": 3 }, { "throughAge": 99, "min": 1, "max": 2 } ],
      "Backup":  [ { "throughAge": 26, "min": 2, "max": 3 }, { "throughAge": 29, "min": 1, "max": 2 }, { "throughAge": 99, "min": 1, "max": 1 } ]
    },
    "bonusShare":     { "Starter": { "min": 0.25, "max": 0.40 }, "Backup": { "min": 0.00, "max": 0.15 } },
    "guaranteeShare": { "Starter": { "min": 0.45, "max": 0.65 }, "Backup": { "min": 0.10, "max": 0.40 } },
    "baseEscalation": 0.08
  }
}
```

**Records** (`Application/Contracts/ContractData.cs`, the WP3 pattern; `ISeedData` gains `ContractData Contracts`):

```csharp
public sealed record ContractData(long Cap, Utilization Utilization, IReadOnlyList<MinimumSalary> MinimumSalaries,
    RookieScale RookieScale, UndraftedDeal Undrafted, VeteranDeals Veterans);
public sealed record Utilization(double Min, double Max);
public sealed record MinimumSalary(int CreditedSeasons, long Salary);
public sealed record RookieScale(int Years, IReadOnlyList<double> BaseWeights, IReadOnlyList<RookieRound> Rounds);
public sealed record RookieRound(int Round, long FirstPick, long LastPick, double BonusShare, double GuaranteeShare);
public sealed record UndraftedDeal(int Years, long Bonus);
public sealed record VeteranDeals(double Curve, Wiggle Wiggle, IReadOnlyDictionary<Position, long> MaxApy,
    IReadOnlyDictionary<PlayerTier, IReadOnlyList<LengthBand>> Lengths,
    IReadOnlyDictionary<PlayerTier, Share> BonusShare, IReadOnlyDictionary<PlayerTier, Share> GuaranteeShare, double BaseEscalation);
/// <summary>A multiplicative nudge on a drawn APY: N(0, Sd), clamped to ±Max.</summary>
public sealed record Wiggle(double Sd, double Max);
public sealed record LengthBand(int ThroughAge, int Min, int Max);
public sealed record Share(double Min, double Max);
```

And in Core, next to `CapMath`: `public enum ContractType { Rookie, Veteran, Minimum }` (M4 adds `Tag` and `Extension`; saves store the name).

### 4.10 Test rules for the file (`tests/Infrastructure.Tests/Seed/ContractDataTests`, Claude writes them)

Each lists every offender, as the WP3 tests do.

| # | Rule |
|---|---|
| R1 | The file loads; `cap > 0`; `0 < utilization.min < utilization.max ≤ 1` |
| R2 | Minimum salaries start at 0 credited seasons and ascend in both columns; every step > 0 |
| R3 | Rookie scale: `years` within 1–`CapMath.MaxContractYears`; `baseWeights` has `years` entries that sum to 1; rounds 1–7 each exactly once; `firstPick ≥ lastPick` within a round and `lastPick(r) ≥ firstPick(r + 1)` between rounds; every share within 0–1, non-increasing by round |
| R4 | Every rookie pick's bases cover the minimums: `lastPick × (1 − bonusShare) ≥ Σ minimum(0..years − 1)` for every round |
| R5 | Undrafted: `years` within 1–7, `bonus ≥ 0` |
| R6 | Every `Position` has a `maxApy` ≥ `minimum(7)`; `curve ≥ 1`; `0 ≤ wiggle.sd ≤ wiggle.max ≤ 0.5` |
| R7 | Lengths: Starter and Backup each have bands with `throughAge` ascending to 99, `1 ≤ min ≤ max ≤ CapMath.MaxContractYears` |
| R8 | Bonus and guarantee shares: a band per Starter and Backup, `0 ≤ min ≤ max ≤ 1`; `baseEscalation ≥ 0` |
| R9 | The curve pays Starters more than Backups: for every position **with backups** (`average > starters`), `apy` at the lowest Starter rank (`t = 1 − starters / average`) is at least 1.5 × the minimum, so a Starter is never a minimum deal by construction. One-deep positions (K, P, LS) are exempt: their worst player in the league is meant to earn the minimum (§10) |
| R10 | **The maxima are the right size:** a 53-man roster at the curve's expected values (`minimum(3) + (max − minimum(3)) / (curve + 1)`, the mean of `t^curve` over a uniform percentile), with `positions.json`'s `roster.average` players per position, costs between **0.8 and 1.3 × cap**, so §4.8's trades stay few |

## 5. Data model

Per [PERSISTENCE.md](../architecture/PERSISTENCE.md) and the [WP5 migration](../milestones/M1_THE_LEAGUE.md#wp5--schema-and-createleague-m). M1 writes only columns that something reads (D-078 #12); the rest arrive with M4.

| Table | M1 columns | Notes |
|---|---|---|
| `Contracts` | `ContractID`, `PlayerID`, `TeamID`, `Type` (`ContractType` name), `SignedSeason`, `Years`, `TotalValue`, `SigningBonus`, `GuaranteedTotal`, `IsActive` | One active contract per player on a team; free agents have none. PERSISTENCE.md lists `SignedOn` (a date): M1 stores the **season**, since the calendar's dates arrive with the phases in M3. |
| `ContractYears` | `ContractID`, `Season`, `BaseSalary`, `ProratedBonus`, `GuaranteedAmount`, `CapHit` | One row per contract season, past seasons included. `CapHit` is stored for the read models and must equal `CapMath.CapHit` (a test). `RosterBonus` and `IsVoid` wait for their readers (M4, v2). |

**Proration is read, never recomputed.** After generation, anything that needs a season's proration sums `ContractYears.ProratedBonus`. That includes M4's dead money and the read models' "still prorating". Nothing calls `CapMath.ProratedBonus` with `Contracts.Years`. The reason is M9's 5th-year option ([D-040](../DECISIONS.md#d-040-salary-cap-realism-tiers)): it adds a fifth season to a first-rounder's four-year deal, and the signing bonus stays spread over the original four, so `Years` no longer gives the proration. The `CapHit = CapMath.CapHit` test above holds in M1, where every contract still has its signed length. M9 amends it.

**Read models (WP6, D-070):** `ContractSummaryDto` (type, years and the season it ends, APY, this season's cap hit, signing bonus and what's still prorating, guaranteed total and what's left) for the Player page; `TeamCapDto` (cap, committed, space, utilization, the top five cap hits) for the Team page.

## 6. Application services

| Service | Commands | Queries | Domain events |
|---|---|---|---|
| `Players/PlayerGenerator` (WP4) | none: generation happens inside `CreateLeague` | — | `LeagueGenerated` carries the cap totals |
| `Contracts/ContractGenerator` (WP4, pure) | — | — | — |
| `Contracts/ContractQueries` (WP6) | — | `ContractSummaryDto`, `TeamCapDto` | — |

`ContractGenerator` is a pure function from (player, his league-wide rank at the position, draft info, `ContractData`, seed) to a contract with its years; the cap trades (§4.8 step 4) are a pure pass over the league's rosters. Both run outside the write transaction, like the rest of generation.

## 7. AI counterpart

None in M1: there are no actions. Every team, the user's included, gets its contracts from the same rules and the same curve, so the user inherits a cap table exactly as plausible as the AI's (no information cheating, D-042). M4's AI uses the same `CapMath` and validation as the user.

## 8. UI

- **Player page (WP7):** a contract block: "3 years left · $12.4M APY · cap hit this season $14.1M · $9.0M guaranteed remaining", and a small per-season table (base, bonus share, cap hit). Money through the existing `$12.4M` formatting; tabular figures.
- **Team page (WP7):** cap, committed, space and utilization as `Stat` tiles, the top five cap hits as a short list.
- No contract builder, no cap sheet, no cut preview: M4.

## 9. Calendar and phase integration

None in M1. The cap year equals the season; the league starts at Training Camp with the cap table of its first season. M3's phases and M4's new-league-year step (growth, rollover, Top 51) attach here.

## 10. Edge cases

- A veteran whose APY lands at his minimum becomes a `Minimum` deal (§4.7), so the generator never writes a "veteran" contract that is really a minimum.
- A 35-year-old Starter gets a 1–2 year deal (§4.7) and never a signing bonus prorated past his years.
- The minimum floor can push a short, cheap contract's total above its drawn APY × years; the total is then the sum of what was written, not the draw.
- A kicker, punter or long snapper is one of 32 at his position, so his rank spans the whole curve: the worst kicker earns the minimum, the best about the max. That is the real shape of those markets.
- A team that no same-position trade can bring into the band (§4.8) stays outside it and is logged (§11); over 20 seeds the tests require that it never happens. If it does, the data is tuned (the maxima or the curve), not the rule.
- Two players tied on OVR at a position take adjacent ranks in seed order, so the rank is still deterministic.

## 11. Logging

Within WP4's `LeagueGenerated` range (1300–1329, numbers from the [WP4 guide](../milestones/M1_WP4_GUIDE.md)): one Information event per league with the number of trades and the lowest, highest and mean utilization (1301); one **Warning** per team still outside the band after the trades (1302).

## 12. Performance

About 1,700 contracts and 6,500 `ContractYears` rows per league, written in the one bulk insert `CreateLeague` already plans. Generation is arithmetic; it's inside the < 5 s new-league budget and not timed separately (D-078 #4).

## 13. Test plan

- **Core:** `CapMath` is covered (WP1). `ContractType` needs nothing.
- **Data (`ContractDataTests`, Claude):** R1–R10 above, each listing every offender; plus "a misspelled enum key fails", as for the WP3 files.
- **Application (WP4):** for every drafted player with experience 0–3 the contract is `Rookie`, four years, signed in the draft season, total on the round's line; undrafted with experience 0–2 are three-year `Minimum` deals; every base ≥ the season's minimum; Σ(bases) + bonus = total; guarantees ≤ total; every `CapHit` = `CapMath.CapHit`; `elapsed ≤ experience` and the contract has a season left; **pay follows rank:** across the league, APY is non-decreasing in `t` at every position before wiggle, and the same player's APY would be the same on any team; **every team between `utilization.min` and `max`**, over 20 seeds, after trades that only ever swap Veteran contracts at the same position and tier; the golden league's cap totals (Verify).
- **UI (WP7, bUnit):** the Player page's contract block and the Team page's tiles read the DTOs.
- **Autoplay:** none in M1.

## 14. Open questions

All six questions of the first draft were answered by the owner on 2026-10-21: see [D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle). None open.

## 15. Decisions made while specifying

- [D-080](../DECISIONS.md#d-080-f-12s-m1-slice-no-practice-squad-in-10-pay-by-league-wide-quality-with-wiggle): no practice squad in 1.0; the $300M cap; guarantees generated and shown in M1; pay by league-wide rank with one max per position and a wiggle; linear rookie rounds; experience as one number.
- [D-081](../DECISIONS.md#d-081-wp4-generation-trades-fit-the-cap-ranked-draft-classes-player-rules-in-application) (WP4, 2026-10-22): teams reach the band by trading veterans, not by scaling contracts (scaling would have changed most teams' deals, many by more than 20%); the curve drops to 2.5 so the league's mean sits inside the band.
