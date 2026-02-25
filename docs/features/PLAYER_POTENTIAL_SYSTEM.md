# Player Potential and Peak Years PRD

## 1. Executive Summary

- Problem Statement: Player generation lacks a clear, consistent potential and age-based development curve, which reduces realism and long-term roster differentiation.
- Proposed Solution: Introduce a player potential model that shapes peak ratings by archetype mean and position-specific peak years, then applies age-based development and decline.
- Success Criteria:
  - Age-to-OVR curve falls within defined target bands for each position (TBD values).
  - League-wide roster value spread increases by >= 15% over baseline after 3 seasons.
  - Playtest feedback reports improved progression realism (>= 4/5 average rating).

## 2. User Experience and Functionality

- User Personas:
  - Franchise player who wants believable progression and regression over seasons.
  - League builder who wants teams to diversify over time.
  - Simulation tester who validates output distributions.

- User Stories:
  - As a franchise player, I want younger players to have room to grow so that scouting and long-term planning matter.
  - As a league builder, I want veterans to decline after peak years so that roster churn feels authentic.
  - As a simulation tester, I want rating distributions to align with target curves so that balance is stable across seasons.

- Acceptance Criteria:
  - Each generated player has a BasePotential value in the range 0 to 100.
  - Each position has defined peak age start and end values (TBD per position).
  - Peak Player Rating is derived from archetype attribute mean adjusted by BasePotential.
  - Players below peak age have development room proportional to BasePotential.
  - Players above peak age have a decline rate tied to position and age.
  - Overall ratings show no discontinuities when crossing peak age boundaries.
  - League-wide rating distributions are reproducible with a fixed seed (TBD seed control design).

- Non-Goals:
  - Training, coaching, and dynamic attribute growth during season.
  - Injury-driven long-term development effects.
  - UI changes or new screens for player potential.

## 3. AI System Requirements

- Not applicable.

## 4. Technical Specifications

- Architecture Overview:
  - Player generation produces BasePotential and PeakYears data per position.
  - Archetype attribute means and standard deviations drive peak ratings.
  - Age-based development applies a curve up to peak years and decline after peak years.
  - Final per-attribute ratings are computed and persisted with the player.

- Initial League Creation (Critical):
  - Define league targets before generation:
    - Position counts per team (use roster ranges).
    - Age bands per position (rookie, prime, vet) with target percentages.
    - OVR tiers per position (elite, starter+, starter, depth, replacement) with target percentages.
    - League-wide OVR mean and std dev, with position offsets.
  - Two-stage generation:
    - Stage A: For each team, fill positions to target counts and assign a target tier per slot.
    - Stage B: Create players by sampling age and potential, compute peak OVR from archetype mean, and apply age curve to get current OVR.
  - Deterministic randomness: Seed by league ID, team ID, position, and slot index.
  - Post-gen validation: Audit distributions and adjust if outside target bands.

- Age Curve and Randomness (Initial Creation):
  - Peak OVR is derived from archetype mean adjusted by BasePotential.
  - Current OVR is Peak OVR multiplied by a position-specific age curve.
  - Age curve is smooth and piecewise:
    - Pre-peak: ramps from a lower bound to 1.0 at peak start.
    - Peak: near-flat plateau between peak start and peak end.
    - Decline: gradual decrease after peak end.
  - Allow outliers without breaking distributions:
    - Apply a small talent noise to Peak OVR (TBD std dev).
    - Apply a smaller age noise to Current OVR (TBD std dev).
  - Guardrails:
    - Clamp Current OVR to valid bounds.
    - No discontinuities at peak start or peak end.
    - Position-level mean and std dev remain within target bands.

- Integration Points:
  - Player generation: Apply potential and peak-years logic during player creation.
  - Domain model: Extend Player with BasePotential and optional PeakAgeStart/PeakAgeEnd fields.
  - Rating helper: Ensure OverallRating uses final attribute values.
  - Persistence: Save new fields to the player entity.

- Security and Privacy:
  - No new external data sources.
  - No PII beyond existing player names and colleges.

## 5. Risks and Roadmap

- Phased Rollout:
  - MVP: BasePotential + peak years + decline only.
  - v1.1: Position-specific peak and decline curves (non-linear).
  - v2.0: Training and experience effects.

- Technical Risks:
  - Tuning risk: Poorly chosen curves can compress ratings or skew positions.
  - Stability risk: Randomness without seed control can hinder reproducibility.
  - Compatibility risk: Changes may require migration of existing player records.

## Open Questions and TBDs

- Target OVR bands by age and position.
- Position-specific peak age ranges and decline rates.
- Whether BasePotential is stored as 0-100 integer or 0-1 scalar.
- Seed strategy for deterministic generation in tests.
