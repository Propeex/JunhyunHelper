# ACTIVE WORK

Status: **ACTIVE**

## Current task

Farming Guide raid-advisor rule simplification and global inventory optimization.

## Goal

Align the Farming Guide with the user-confirmed product model:

- separate **system rules** from **farming rules**;
- system rules enforce Tarkov-valid inventory mechanics and user locks, but do not become farming preferences;
- farming priority is lexicographic: (1) currently needed FIR quest/hideout items, then (2) maximum retained economic value;
- food, drink, ammunition, magazines and medicine receive no automatic tactical-retention privilege unless the exact item is currently FIR-needed; users protect personally important items with locks;
- weight is the only user-configurable farming constraint;
- every scan conceptually releases all unlocked inventory items into one candidate pool with the incoming item and solves for the best legal final inventory state, instead of locally inserting the incoming item around the current arrangement.

## Base / working state

```text
base main: 379c6ab4ab02431c6bb74b537e899e94f45ee987
public stable: v1.16.4
working branch: feature/v1.17.0-farming-guide-global-optimization-2026-09-03
PR: not opened yet
```

## Confirmed scope

1. Audit existing Farming Guide decision documents, policies, planner semantics and tests for conflicts with the newly confirmed rules.
2. Record the new product decision and supersession relationship.
3. Remove tactical food/drink/current-weapon-ammo retention from automated farming decisions.
4. Simplify loot priority to FIR-needed first, economic retained value second; geometry is a system feasibility concern rather than an independent farming preference.
5. Replace local displacement semantics with a global unlocked-item optimization model while preserving locked items/cells and Tarkov-valid placement/filter/nesting rules.
6. Apply user-configured weight as a hard farming constraint when enabled/defined by the product settings.
7. Add deterministic regression coverage for rule boundaries and global-optimum cases.
8. Run relevant deterministic tests, full suite/Release verification where available, then PR/CI and documentation consistency.

## Completed

- Recovered repository authority and current v1.16.4 state.
- Identified two concrete conflicts in current code:
  - `FarmingGuideRepackingPlanner` is explicitly a local displacement planner rather than a global unlocked-item optimizer.
  - `FarmingGuideTacticalResourcePolicy` encodes automatic food/drink/current-weapon-ammo retention, which the user has now rejected as a farming rule.
- Identified priority drift: current policy uses weight/footprint as item-level tie breakers; new product rule treats weight as the only user farming constraint and geometry as system feasibility.

## Current step

Audit all callers/tests and design the smallest coherent implementation that produces the globally optimal retained legal state without reintroducing tactical heuristics.

## Remaining

- update decision/product/architecture/reference docs;
- implement rule changes and global optimization;
- update/add tests;
- validate build/test behavior;
- open PR, verify CI, merge/release if the implementation is complete and release-ready.
