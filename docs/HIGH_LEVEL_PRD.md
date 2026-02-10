# Product Requirements Document (PRD): Gridiron Front Office

## 1. Executive Summary

- **Problem Statement**: Current football simulations often lack long-term tension, becoming "solved" and predictable after a few seasons, while often suffering from uninspired user interfaces.
- **Proposed Solution**: A professional, data-driven desktop simulation built with .NET 10 and MAUI Blazor, focusing on "Executive Suite" decision-making and a "Dynasty Decay" system to ensure ongoing challenge.
- **Success Criteria**:
    - **Performance**: Simulate a full league week (16 games) in under 200ms.
    - **Engagement**: Users can complete a full season and offseason cycle in approximately 15 minutes.
    - **Difficulty**: "Dynasty Decay" mechanics (regression/cap spikes) effectively prevent linear, indefinite winning streaks.
    - **UI Standard**: 100% adherence to the "Executive Suite Dark" style guide, achieving a modern, sophisticated dashboard feel.

---

## 2. User Experience & Functionality

- **User Personas**: 
    - **The Architect**: Focuses on long-term franchise building, draft strategy, and salary cap health.
    - **The Analyst**: Power users who rely on data-dense tables and monospace-aligned statistics to find market inefficiencies.
- **User Stories**:
    - **Story**: As a GM, I want to advance through the season so I can see how my roster performs and manage mid-season injuries.
    - **AC**: Simulation results must be deterministic based on a seeded RNG.
    - **Story**: As a GM, I want to manage the 53-man roster so I can stay compliant with the salary cap.
    - **AC**: UI must highlight cap warnings in **Gold (#FFB703)** and violations in **Flag Red (#C1121F)**.
- **Non-Goals**: 
    - **On-field Control**: There is no manual control of players or on-field gameplay.
    - **Multiplayer**: The initial release is strictly a single-player Windows desktop experience.

---

## 3. Technical Specifications

- **Architecture Overview**: A six-layer Clean Architecture (Domain, Simulation, Application, Persistence, UI, and Desktop Host).
- **Integration Points**: 
    - **Persistence**: SQLite database for all entity storage and save files.
    - **UI**: MudBlazor component library for highly interactive, data-dense grids.
- **Simulation Layer**: 
    - Implements deterministic play-by-play logic and AI decision-making.
    - AI teams must prioritize roster longevity and cap health over simple high-rating acquisitions.
- **Security & Privacy**: Standard Windows desktop data handling; all data remains local to the user's machine.

---

## 4. UI Design Standards

- **Core Palette**: Main background uses **Abyss (#0A0E27)** with a **Navy (#1A1F3A)** surface for cards and tables.
- **Typography**: 
    - Bold Sans-serif for headers.
    - Monospace for all statistics and financial figures to ensure perfect column alignment.
- **Visual Hierarchy**: 90% neutral colors (Navy/Slate) with 10% high-impact status colors for critical data.

---

## 5. Risks & Roadmap

- **Phased Rollout**:
    - **v1.0 (MVP)**: Full Season Loop including Draft, Free Agency, and Playoffs.
    - **v1.5 (Depth)**: Multi-year cap projections, trade finders, and contract restructuring.
    - **v2.0 (Analytics)**: Historical league tracking, custom coaching trees, and advanced scouting metrics.
- **Technical Risks**:
    - **Database Performance**: Managing large volumes of historical player stats over multi-decade simulations.
    - **AI Balancing**: Ensuring the AI remains competitive and doesn't fall into predictable patterns that the player can exploit.