# Standout Features

> **Status:** Draft v1 · 2026-10-13
>
> Two owner ideas (the real-time draft and the modern UI) plus four proposed ideas, followed by a shortlist of smaller ones. Each proposed idea includes *why it earns its place*, *what it costs*, and a recommendation.

---

## Summary and recommendation

| # | Feature | Source | Recommendation | Milestone |
|---|---|---|---|---|
| 1 | Real-time draft | Owner | **Core**, the hallmark | M6 |
| 2 | Modern, elegant UI | Owner | **Core**, applies to everything | M0 onward |
| A | Scouting fog of war | Proposed → **Accepted** | **Core.** Your roster exact, other pros slightly fuzzy, prospects scouted ([D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted)). | M6 (display groundwork in M1) |
| B | Live Events (FA Frenzy, Deadline Day) | Proposed → **Accepted** | **FA Frenzy committed; Deadline Day only if Frenzy proves the pattern** ([D-046](DECISIONS.md#d-046-live-events-fa-frenzy-committed-deadline-day-conditional)). | M9 |
| C | Film Room (explainable results) | Proposed | **Adopt.** It makes a non-physics sim trustworthy and feels modern. | M8 |
| D | The Owner's Box (job security and GM career) | Proposed | Adopt in a light form. Gives long-term stakes. | M10 |

> **The challenge:** a real-time draft is only interesting if the decision is hard. If every prospect's true rating is visible, the "right" pick is "sort by OVR, take the top one", and two minutes on the clock is two minutes of waiting. **Fog of war (A) is what makes your idea work.** I recommend treating A as part of feature 1, not as an extra.

---

## 1. The real-time draft: "The clock never stops"

### The rule
> **Time can be accelerated, never paused** (in **Live** clock mode, the default).

### Clock mode (per save)
Chosen at league creation ([D-022](DECISIONS.md#d-022-clock-mode-is-a-per-save-setting-live--pausable), proposed from your idea):

| Mode | Behavior | Who it's for |
|---|---|---|
| **Live** (default) | The rule above. No pause, ever. The save card shows a **LIVE** badge. Required for Ironman. | The intended experience |
| **Pausable** | A Pause button freezes the whole live event (your clock, AI deliberation, offer timers) until you resume | Accessibility, real-life interruptions, players who want to deliberate |

The clock mode is **locked after creation** (decided, so pausing doesn't become the default answer to a hard pick), and the wizard says plainly that Live is how the game is meant to be played.

### In Live mode
- Nothing pauses the clock while it's running: not menus, not dialogs, not alt-tabbing, not minimizing.
- When **you** are on the clock, it runs in real wall-clock time at 1×. Always.
- When an **AI team** is on the clock, its pick resolves after a short, realistic "deliberation" delay. You can speed that up to 2× or 4×, which shortens *their* delays and never touches *your* clock.
- Each committed pick is permanent. Restores and duplicating the league are disabled while the draft is live (see [D-004](DECISIONS.md#d-004-save-model-autosave--snapshots)).

### Clock presets
Chosen at league creation. **Changeable in the offseason until the draft starts**, never during it and never in an Ironman league ([D-044](DECISIONS.md#d-044-draft-clock-presets-tiered-changeable-before-the-draft)). The times below are tentative until Spike S1 tunes them.

| Preset | Round 1 | Round 2 | Rounds 3–7 | Who it's for |
|---|---|---|---|---|
| Relaxed | 4:00 | 3:00 | 2:00 | First-timers, accessibility, players who like to deliberate |
| **Standard** (default) | **2:00** | **1:30** | **1:00** | The intended experience |
| Pressure | 1:00 | 0:45 | 0:30 | Veterans and Ironman runs |

### AI pick timing (at 1×)

| Round | AI deliberation | Notes |
|---|---|---|
| 1 | 8–15 s | Longer if a trade is being negotiated. "The pick is in" reveal. |
| 2 | 5–10 s | |
| 3–7 | 3–6 s | Usually played at 2×/4×. |

**Estimated length on the Standard preset:** about 30 minutes at 1×, about 20 minutes with later rounds at 4×. That fits the 20–40 minute target.

### What you can do while you're on the clock
- **Everything except advancing the calendar.** Browse prospects, read scouting reports, check team needs, look at the cap sheet. The Draft Room is the default view, but you can leave it. A persistent **ON THE CLOCK** banner with a mini clock follows you around the whole app.
- **Make the pick.** Confirm with a single action (the "send it in" moment).
- **Trade.** Propose a trade-down to any team. AI teams answer in 2–6 seconds of simulated "thinking". **Trades don't reset the clock.** If you trade down, the new team inherits the remaining time, as in the real draft.
- **Take incoming calls.** The trade phone shows AI offers as cards with a countdown ring (usually 15–30 s). They expire if you ignore them.

### What you can do while you're *not* on the clock
- Re-order your **Big Board** live as players come off it.
- Set a **pick queue** ("if X is still there, take X; otherwise Y"). This is preparation, not a pause.
- Call the team that's on the clock to **trade up** into the current pick.
- Watch the **ticker**: picks, trades, rumors, "run on cornerbacks" alerts.

### When the clock hits 0:00
1. **Auto-pick** takes the highest available player on *your* Big Board.
2. If your board is empty or exhausted, it takes your scouts' best player available, weighted by team needs.
3. The pick is announced as an **auto-pick**. Media draft grades may poke fun at it. There is no other penalty.

### If the app closes or crashes mid-draft
Every pick is its own committed transaction, and the remaining time on the clock is saved about once per second. When you reopen the league, the draft resumes at the exact pick **with the time that was left**. A crash can't cost you a pick, and closing the app can't redo one: committed picks and trades stand ([D-045](DECISIONS.md#d-045-live-event-close-or-crash-resume-where-it-left-off)).

### Before the draft (the prep that makes the clock matter)
- **Scouting season**: assign scouts, attend the combine and pro days, use your limited interview slots (see [A. Fog of war](#a-scouting-fog-of-war)).
- **Media boards and mock drafts**: a noisy "consensus", useful for predicting what *other* teams will do.
- **Your Big Board**: drag-and-drop tiers with notes ("Tier 2: starters by Year 2").
- **Team needs** dashboard: depth chart holes, expiring contracts, age cliffs.

### After the draft
- **Instant media grades** based on *consensus* perception, not true ratings, so they can be very wrong. That's the fun.
- A **draft class retrospective** in your history: classes are re-graded after 3 and 5 seasons using real performance. This is how you learn whether your scouts are any good.
- **Rookie contracts** are generated automatically from the slotted rookie scale.
- An **undrafted free agent (UDFA) window**: one turn-based round in which you and every AI team make blind offers to the best undrafted players, all resolved at once ([D-059](DECISIONS.md#d-059-the-udfa-window-is-one-turn-based-round-of-blind-offers)).

### Draft Room layout (wireframe)

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│ ◄ Ticker: R1 P14 HAWKS — CB J. Okafor (Ridge State) · TRADE: WOLVES ↔ ORCAS ...      │
├───────────────┬──────────────────────────────────────────────┬───────────────────────┤
│ YOUR BOARD    │   ROUND 1 · PICK 15 · YOU ARE ON THE CLOCK    │ TRADE PHONE           │
│ T1 ▸ Okafor ✕ │            ┌──────────────┐                  │ ┌───────────────────┐ │
│ T1 ▸ Mensah   │            │    1:12      │  ◄ big clock     │ │ ORCAS want #15     │ │
│ T2 ▸ Reyes    │            └──────────────┘                  │ │ Give: #15          │ │
│ T2 ▸ Lindqvist│   [ Prospect grid: filters / pos / tiers ]   │ │ Get: #22 + 2028 R3 │ │
│ ...           │   Name    Pos  Scout OVR   Range   Need      │ │  ◔ 0:18  [Accept]  │ │
│               │   Mensah  EDGE   78       74–83   ●●●        │ └───────────────────┘ │
│ PICK QUEUE    │   Reyes   OT     76       70–84   ●●○        │ YOUR PICKS            │
│ 1. Mensah     │   ...                                         │ R1 #15 · R2 #47 · ... │
│ 2. Reyes      │   [ Selected prospect card: report, combine ] │ TEAM NEEDS            │
│               │                       [ SEND IN THE PICK ▶ ] │ EDGE ●●● OT ●●○ ...   │
└───────────────┴──────────────────────────────────────────────┴───────────────────────┘
```

### Accessibility vs. "no pause"
"No pause" can be an accessibility problem. The accommodations are the **Pausable clock mode** (per save), the **Relaxed** preset (longer clocks), the pick queue and keyboard-first controls. No separate accessibility preset is needed ([D-044](DECISIONS.md#d-044-draft-clock-presets-tiered-changeable-before-the-draft)).

### Technical design
See [architecture/LIVE_EVENTS.md](architecture/LIVE_EVENTS.md): the authoritative clock, single-writer commands, race handling (a pick vs. expiry at 0.05 s), persistence, and resume.

---

## 2. Modern, elegant UI

"Modern" has to be concrete or it means nothing. GFO's definition:

| # | Principle | Concretely |
|---|---|---|
| 1 | **Dark-first, calm surfaces** | Graphite neutrals and one accent color. Light theme supported. |
| 2 | **Your team, your colors** | The accent color comes from your franchise's colors, adjusted for contrast. The whole app re-tints when you take over a team. |
| 3 | **Typography-led hierarchy** | Archivo for the UI, the same family condensed for scoreboards, the clock and big numbers, tabular numerals everywhere ([VISUAL_DESIGN.md](VISUAL_DESIGN.md)). |
| 4 | **Peek, don't navigate** | Clicking any player or team name, anywhere, opens a side sheet. Full pages are for deep work. |
| 5 | **Keyboard-first** | A `Ctrl+K` command palette (jump to any player, team or screen, or run actions), shortcuts on every screen, arrow-key navigation in grids. |
| 6 | **Instant** | Interactions under 100 ms, skeletons instead of spinners, no blocking modals during long operations (progress shows inline). |
| 7 | **Motion with purpose** | 120–320 ms transitions for state changes. Big "broadcast moments" are reserved for big events: the pick is in, a blockbuster signing, a game going final, a championship. |
| 8 | **No dead ends** | Every name is a link, every number has an explanation on hover, every empty state suggests the next action. |
| 9 | **Density control** | Comfortable and Compact density modes, because GM players love dense tables. |
| 10 | **Native-feeling chrome** | A custom title bar with no stock Windows frame, and remembered window size and position. |
| 11 | **Accessible** | WCAG AA contrast, visible focus, reduced-motion support, rating colors that work for colorblind users, and color never as the only signal. |

**Reference points:** Linear, Raycast, Vercel/Geist and Arc for app feel. Apple Sports and The Athletic for sports data. Broadcast scorebugs and draft graphics for the big moments.

Full system: [architecture/UI_DESIGN_SYSTEM.md](architecture/UI_DESIGN_SYSTEM.md).

---

## A. Scouting fog of war

### The idea
**You never see the true ratings of anyone outside your building.** You know your own players' current ability. Everyone else you see through public film (slightly fuzzy) or, for college prospects, through your scouts' eyes (very fuzzy until you invest in information). Every AI team has its own beliefs too, and they disagree with yours. **Decided:** [D-032](DECISIONS.md#d-032-fog-of-war-your-roster-exact-other-pros-slightly-fuzzy-prospects-scouted).

### Why it earns its place
- **It makes the draft a skill test.** Without uncertainty, drafting is sorting. With it, two minutes on the clock means weighing a safe 74–78 against a volatile 68–88.
- **It creates a real market.** Teams value players differently, so trades, free agent bidding and draft "reaches" happen naturally, without scripting.
- **It makes scouting a system with choices**: budget, scout quality and assignments.
- **It tells stories.** Busts, steals and "our scouts were right about him" moments.
- Few modern GM sims do this well, and it is FOF's most loved feature.

### How it works
- Every player has **true ratings** (hidden, used by the sim). Each team sees **current ratings** according to who the player is to them:

| Who you're looking at | What you see | Does it sharpen? |
|---|---|---|
| **Your roster** (including players the moment you draft, sign, claim or trade for them) | **Exact** current ratings: `OVR 78` | n/a |
| **Other teams' players and free agents** | **Slightly fuzzy**, by a fixed amount (about ±3): `OVR 78 (75–81)`. Game film is public. | **Never** |
| **College prospects** | **Very fuzzy** until scouted: `OVR 74 (64–84)` | **Only through scouting** (table below) |

- **Prospect information sources:**

| Information source | What it sharpens | Cost |
|---|---|---|
| Area scouts, **standing assignment** (a region or a position group, in season) | All attributes, automatically, **down to a floor** | Nothing weekly. Where you point each scout ([D-058](DECISIONS.md#d-058-scouting-department-standing--special-assignments-no-biases-in-10)) |
| A scout on a **special assignment** (one specific prospect) | Past the standing floor | That scout's standing coverage slows while he's on it |
| Combine | Physical attributes, almost exactly (measurables) | Free, but only for invited prospects |
| Pro day | Physical attributes for non-invitees | Scout time |
| Interviews (limited, e.g. 30 per class) | Personality: work ethic, coachability, character flags | A scarce slot |
| College production | Noisy skill signal | Free |

- **Potential is always the most uncertain thing.** It's a letter grade ("Ceiling: B+"), never a number, for every player including yours. On your roster it narrows as you watch the player develop, but it's never exact.
- **Personality** shows as coarse descriptors on your roster from acquisition, through interviews for prospects, and only as reputation tags ("Held out in 2029") for everyone else.
- **The reveal moment:** when you draft or acquire someone, his true current ratings appear. Draft night tells you right away whether you read current ability correctly. Potential plays out over years, which is what the 3- and 5-year retrospective grades.
- **Stable estimates:** a team's error on a pro is deterministic and redrawn once per league year, so ranges never flicker between views.
- **Scouts have visible traits**: accuracy and position expertise, so who covers which region or position matters. Every team has the same scouting capacity until the owner's staff budget arrives in M10 ([D-041](DECISIONS.md#d-041-finances-owner-funded-staff-budget-in-m10-only)). **Biases** (a scout consistently wrong in one direction, learnable through attributed reports) are parked for after 1.0 ([IDEAS.md](IDEAS.md)).
- **AI teams don't cheat.** Each knows its own roster exactly, sees other pros with its own independent fuzz, and scouts prospects with its own staff, built the same way as yours.
- **No difficulty scaling** ([D-042](DECISIONS.md#d-042-no-difficulty-presets-in-10)). How well you see prospects depends on your scouts and how you use them.

### Cost and architectural impact
- **Decide before M1.** Every rating display in the app has to support ranges from day one. The `RatingChip` and `RatingBar` components and the read models must carry an estimate *and* an uncertainty. Retrofitting this later means touching every screen.
- Data: `ScoutingReport (TeamId, PlayerId, AttributeGroup, Estimate, Sigma, UpdatedOn)` for **prospects** only, stored for the user's team. AI prospect perception can be stored or derived deterministically from (seed, team, player, scouting investment). **Pro fuzz needs no storage**: it's derived from `Hash(leagueSeed, "perception", teamId, playerId, season)` ([PERSISTENCE.md](architecture/PERSISTENCE.md)).
- The valuation model (F-13) must work on perceived ratings, not true ones, and discount uncertain estimates (otherwise the team that overestimates a free agent most always wins the bidding and overpays).

---

## B. Live Events: the draft engine, reused

### The idea
Build the real-time engine once for the draft, then use it for the other moments where real front offices feel time pressure:

| Event | Real-time window | What happens |
|---|---|---|
| **The draft** (M6) | About 20–40 min | As described above. |
| **Free Agency Frenzy** (M9) | About 10–15 min, covering the first ~72 in-game hours of FA | Top free agents receive offers. Agents set deadlines ("Client decides in 0:45"). Other teams' signings stream in the ticker. You get outbid, or you overpay in a panic. Afterward, FA continues turn-based, day by day. |
| **Trade Deadline Day** (M9, conditional) | About 5–10 min, covering the final in-game day before the deadline | AI teams shop players. Calls come in with expiring offers. Contenders and sellers get desperate. |

### Why it earns its place
- It turns the hallmark into a **pattern**. Three times a year, the game asks you to perform under pressure.
- The marginal cost is low. The clock, actors, expiring offers, ticker and persistence are all built for the draft (M6).
- It drives the **Pressure, not paperwork** pillar without adding busywork.

### Guardrails
- The draft is always live. FA Frenzy and Deadline Day are on by default but can be **toggled at league creation** (locked afterward). **FA Frenzy is committed. Deadline Day is built only if FA Frenzy proves the pattern is fun** ([D-046](DECISIONS.md#d-046-live-events-fa-frenzy-committed-deadline-day-conditional)).
- The same rules apply everywhere: the save's **clock mode** (Live or Pausable) governs every live event. Restore and duplicate are disabled during the event in either mode.

---

## C. Film Room: explainable results

### The idea
**Every game result can be explained.** After each game, the Film Room says *why* it happened in plain language and backs that up with data.

**Post-game report**
- A **win probability chart** with the 5 biggest swing plays (win probability added) linked to the play-by-play.
- **Key matchups won and lost**: e.g. "LT D. Haskins vs. EDGE M. Toure: allowed 3 sacks, 7 pressures on 31 pass-block snaps."
- **Unit grades** (OL, pass rush, coverage, …) and **player grades** (0–100) built from matchup outcomes.
- **"Why we lost"**: three generated sentences drawn from the factors that mattered most.
- **Game plan review**: did your plan work? ("You blitzed 41% of snaps; their QB went 9/12 for 160 yards against the blitz.")

**Season analytics**
- Efficiency per play (EPA-style), success rate, explosive play rate, pressure rate, red zone conversion, and so on, for your team, the league and individual players.

### Why it earns its place
- **Trust.** A statistical sim feels arbitrary when results come without explanations. Explanations make it feel *fair*, which supports pillar 3.
- **Modern feel.** The analytics era is part of how modern fans see football.
- **It connects systems.** Player grades feed fog of war (performance is a signal), contract talks ("my client graded 88 last season") and awards voting.

### Implementation notes
- The engine records the **deciding matchup** and participants for every play (see [SIMULATION_ENGINE.md §7](architecture/SIMULATION_ENGINE.md#7-explainability-the-deciding-matchup)).
- **The Expected Points and Win Probability models are generated by the sim itself.** SimLab runs thousands of seasons and produces lookup tables (by down, distance, field position, score difference and time), which ship as data. Because the models are derived from the engine, they always agree with it. No real-world model has to be bolted on.

---

## D. The Owner's Box: job security and the GM career

### The idea
- Every team has an **owner** with a personality (Patient Builder, Win-Now, Penny-Pincher, Meddler) and **season goals** (e.g. "Make the playoffs", "Develop a franchise QB", "Get under budget").
- A **job security** meter responds to results relative to expectations.
- **End-of-season review.** If you're fired, your career continues. You interview with teams that have openings (usually bad ones) and start a new chapter.
- **GM legacy**: career record, titles, draft hit rate, reputation. Reputation affects how agents and owners treat you.
- A light **staff budget** for scouting and coaching staff (no medical staff, [D-062](DECISIONS.md#d-062-coaching-staff-is-head-coach--two-coordinators-only)).
- **AI GMs face the same owners.** They get fired and replaced too, which is where your openings come from.
- **Whether a career can end** is a locked league setting, off by default ([D-063](DECISIONS.md#d-063-career-can-end-is-a-creation-setting-off-by-default)). You can always choose to retire.

### Why it earns its place
- It gives **long-horizon stakes** beyond "win more".
- It makes rebuilding a strategic negotiation with the owner, not a free pass.
- Getting fired and climbing back is a story players tell.

### Guardrails
- Owners **don't veto** your moves. Meddling shows up as pressure and messages, not blocked actions. Players hate losing control.
- No business sim. See [Q-15](OPEN_QUESTIONS.md#q-15-how-deep-is-the-financial-simulation).

---

## Smaller ideas worth considering

| Idea | Value | Cost | Suggested milestone |
|---|---|---|---|
| **Ironman mode** (league setting, requires Live): no restores, the only mode where future Steam achievements unlock | Maximum stakes, and it fits the "no pause" identity | Low (~2 days) | M5. **Decided** ([D-026](DECISIONS.md#d-026-ironman-mode-requires-live-clock-mode)) |
| **Rival GMs with memory**: AI GMs remember lopsided trades and become wary or vengeful | AI with character, and it discourages exploits | Medium | M7 |
| **Front Office Briefing**: a one-screen weekly digest of what changed and what needs you | Cuts weekly busywork | Low | M3 |
| **Shareable cards**: export a PNG of your draft class, a season recap or a player card | Social sharing, free marketing if it ever ships on Steam | Low–Med | M11 |
| **Draft class retrospective**: classes re-graded after 3 and 5 years | Payoff for fog of war | Low | M6 |
| **Storylines**: multi-week narratives (QB controversy, holdout, rookie breakout, revenge game) | "Your league, your story" | Medium–High | M10 |
| **Halftime adjustments**: one game plan tweak at halftime of your game | More engagement on game day, still GM-level | Medium | Post-1.0 ([D-048](DECISIONS.md#d-048-halftime-adjustments-post-10-engine-split-kept-possible)) |
