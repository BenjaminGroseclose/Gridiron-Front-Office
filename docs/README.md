# Gridiron Front Office: Documentation

> **Status:** Pre-production (design) · **Restarted:** 2026-10-13 · **Owner:** Ben Groseclose
>
> Nothing in this folder is final until it is marked **Accepted** in [DECISIONS.md](DECISIONS.md).

Gridiron Front Office (GFO) is a single-player professional football franchise simulation for Windows desktop. You run the whole organization: scouting, the draft, free agency, contracts and the salary cap, player development, and the weekly game plan. What sets it apart is that the important moments happen in real time. **The draft clock never stops.**

---

## Reading order

| # | Document | Answers |
|---|---|---|
| 1 | [ONE_PAGER.md](ONE_PAGER.md) | What are we building, who is it for, and why will anyone care? |
| 2 | [STANDOUT_FEATURES.md](STANDOUT_FEATURES.md) | What makes GFO different: the real-time draft, the modern UI, plus four proposed ideas |
| 3 | [FEATURES.md](FEATURES.md) | The full feature catalog (F-00 … F-29): MVP vs. later, dependencies, AI counterparts |
| 4 | [ROADMAP.md](ROADMAP.md) | The order we build in (M0 … M11) and the exit criteria for each milestone |
| 5 | [VISUAL_DESIGN.md](VISUAL_DESIGN.md) | What GFO looks like: the Stadium direction, dark/light palettes, type, shape, signature elements |
| 5b | [ARCHITECTURE.md](ARCHITECTURE.md) | Technical architecture overview: stack, solution layout, runtime model |
| 6 | [architecture/](architecture/) | Deep dives (listed below) |
| 7 | [DECISIONS.md](DECISIONS.md) | Decision log: what was decided, why, and what we gave up |
| 8 | [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) | What is still open, with recommendations and challenges |
| 8b | [IDEAS.md](IDEAS.md) | The parking lot: ideas deliberately kept off the roadmap, and when to revisit them |
| 9 | [GLOSSARY.md](GLOSSARY.md) | Football, salary cap and project terminology |

### Architecture deep dives

| Document | Scope |
|---|---|
| [architecture/PERSISTENCE.md](architecture/PERSISTENCE.md) | SQLite save files (`.gfo`), autosave, snapshots, migrations, schema overview |
| **[architecture/DATA_ACCESS_GUIDE.md](architecture/DATA_ACCESS_GUIDE.md)** | **Read before touching the database.** EF Core + SQLite in GFO: the unit of work, patterns, past mistakes and their fixes, troubleshooting, torture tests |
| [architecture/DATA_ACCESS_OPTIONS.md](architecture/DATA_ACCESS_OPTIONS.md) | Background: why SQLite transactions fail in .NET apps, and the ORM options considered (EF Core chosen) |
| [architecture/UI_COMPONENT_OPTIONS.md](architecture/UI_COMPONENT_OPTIONS.md) | Background: the component library options considered (Fluent UI Blazor v5 chosen), plus the fallback grid list |
| [architecture/HOST_BENCHMARK.md](architecture/HOST_BENCHMARK.md) | Measured MAUI vs. WPF host comparison: builds, publish size, startup, memory |
| [architecture/LOGGING.md](architecture/LOGGING.md) | Serilog to rolling `.log` files, crash capture, sim trace logs |
| [architecture/UI_DESIGN_SYSTEM.md](architecture/UI_DESIGN_SYSTEM.md) | Fluent UI Blazor v5 foundation, **theming (how configurable it is)**, GFO components and wrappers, app shell, UX rules |
| [architecture/SIMULATION_ENGINE.md](architecture/SIMULATION_ENGINE.md) | Statistical play-by-play engine, determinism, calibration |
| [architecture/CALENDAR_AND_PHASES.md](architecture/CALENDAR_AND_PHASES.md) | League calendar, phase state machine, the Advance flow |
| [architecture/LIVE_EVENTS.md](architecture/LIVE_EVENTS.md) | Real-time engine behind the draft (and later FA Frenzy / Deadline Day) |
| [architecture/TESTING_AND_QUALITY.md](architecture/TESTING_AND_QUALITY.md) | Test strategy, SimLab gates, `build.ps1` (CI later, F-30), code conventions |

### Templates

| Document | Use |
|---|---|
| [features/_TEMPLATE.md](features/_TEMPLATE.md) | Template for a detailed feature spec. Write one right before building a feature, not before. |

---

## Conventions

- **IDs.** Features `F-xx`, milestones `M-x`, decisions `D-xxx`, open questions `Q-xx`. Use the IDs when one doc refers to another.
- **Feature specs.** These live in `docs/features/F-xx_NAME.md`. Write each one just before its milestone starts. A spec written two milestones early goes stale.
- **Answering questions.** When a `Q-xx` is answered, add a `D-xxx` entry to DECISIONS.md, delete the question (leave a one-line pointer), and update every doc it affects in the same change.
- **Docs vs. code.** The docs describe intent. If code and docs disagree, fix one of them in the same pull request.
- **Status labels.** `Draft` → `Proposed` → `Accepted` → `Superseded`.
- **Diagrams** are written in Mermaid so they render on GitHub and in VS Code.
