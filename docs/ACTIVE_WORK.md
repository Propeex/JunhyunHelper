# ACTIVE WORK

Status: **ACTIVE**

## Current task

Clean v1.17.0 Farming Guide rulebook implementation restarted from stable v1.16.4.

## Goal

Implement the user-confirmed Farming Guide model from stable `main` without inheriting unauthorized product semantics from abandoned PR #287:

- active Farming Guide raid Scanner input is treated as FIR by Farming Guide itself;
- Scanner does not classify FIR or add a FIR confirmation flow;
- maximize remaining required FIR Quest/Hideout quantity first;
- then maximize total retained average-Flea value of the complete final state;
- respect the configured weight rule as the only user-configurable farming constraint;
- solve each scan as a complete unlocked-item optimization problem while preserving verified Tarkov legality and explicit user-fixed state;
- make internal implementation/performance improvements only when confirmed product meaning remains unchanged.

## Base / working state

```text
base main: 379c6ab4ab02431c6bb74b537e899e94f45ee987
public stable: v1.16.4
working branch: feature/v1.17.0-farming-guide-restart-2026-09-03
previous PR #287: CLOSED / ABANDONED / MUST NOT BE RESUMED
Draft PR: #288
latest implementation checkpoint before this document update: a3ea3191da076fbc12f79ff0b9be5b6f83d02cec
```

## Confirmed scope

Canonical product decision:

`docs/DECISION_V1.17.0_FARMING_GUIDE_RULEBOOK.md`

Confirmed rules:

1. During an active Farming Guide raid, every newly Scanner-identified incoming item is treated by Farming Guide as FIR.
2. Scanner does not classify FIR from an icon/checkmark/color/text and does not ask for separate FIR confirmation.
3. Farming objective is lexicographic:
   - maximize currently needed FIR Quest/Hideout units, capped by remaining quantity;
   - then maximize complete final retained average-Flea value.
4. The user's configured weight rule is the only user-configurable farming constraint; weight is not an item priority.
5. Item category gives no tactical privilege.
6. Every scan is a complete unlocked-item optimization problem, not a local insertion problem.
7. User-fixed items and cells are constraints; locks do not add value.
8. Existing verified Tarkov placement/container/equipment/stack mechanics remain system legality rules.
9. Internal optimization/performance authority may not be used to invent a new product decision criterion, automatic inference, observation authority, user interaction, cross-feature behavior, or visible failure semantic.

## Restart rule

Do not copy implementation from abandoned PR #287 as authority.

Code from the abandoned branch may only be consulted later as a non-authoritative implementation reference after the stable-main design is independently derived and only if it matches this confirmed rulebook. No Scanner FIR observation code is to be reused.

## Completed

- Closed PR #287 unmerged and marked it abandoned.
- Created clean branch from stable `main@379c6ab4ab02431c6bb74b537e899e94f45ee987` and opened Draft PR #288.
- Recorded the clean canonical v1.17 rulebook.
- Audited stable live raid path and identified conflicting old product semantics: tactical food/drink/current-weapon-ammo protection, armor/headset superiority, local insertion/displacement, lighter-item and smaller-footprint priority.
- Simplified `FarmingGuideLootPriorityPolicy` to FIR need then total Flea value only; weight/footprint remain system facts, not priority tiers.
- Added ephemeral `[JsonIgnore]` `FarmingGuideItemState.RaidAcquired` provenance so active-raid Scanner acquisitions can be represented without modifying Scanner or persisted presets.
- Added explicit raid-acquired inventory counting; v1.17 can distinguish a new scanned copy from an identical raid-start item even when net item-id count is unchanged.
- Added complete-state `FarmingGuideOptimizationScore` with exactly two dimensions: satisfied FIR quantity and retained Flea value.
- Added a new deterministic from-scratch `FarmingGuideGlobalPackingPlanner` that packs an already-selected retained root set globally, preserves exact fixed placements, supports owned/nested surfaces, rejects owner cycles, supports cross-surface final validation, and distinguishes `BudgetExceeded` from proven `NoSolution`.
- Added deterministic unit tests for the objective and global packing engine.
- First clean CI generation proved Desktop Release compilation succeeds. Core tests exposed two stale footprint-priority expectations; these were corrected to the confirmed no-footprint-priority contract.
- First Documentation Consistency failure was only a missing required `## Goal` checkpoint section; this document update corrects it.
- Shutdown Race CI for the first clean generation succeeded.

## Current step

Finish the stable-main system-mechanics audit, then build a new v1.17 Desktop candidate-pool projection that reuses verified placement/filter/lock/weight mechanics without reusing old tactical/local farming policy. Route the live raid decision to that new complete-state path only after deterministic integration tests are in place.

## Remaining

- confirm fresh CI is green after stale test/documentation corrections;
- finish auditing stable placement surfaces/options, lock closure, equipment/carrier legality, quantity and weight helpers;
- implement complete root candidate pool for stored items, top-level equipment/carriers and incoming Scanner item;
- ensure incoming root is `RaidAcquired = true` only inside active Farming Guide raid state;
- preserve ephemeral provenance after accepted snapshot sanitization without persisting it;
- implement retained-set optimization using `FarmingGuideOptimizationPolicy` + `FarmingGuideGlobalPackingPlanner`;
- remove old tactical/category/equipment-superiority/local-planner semantics from the authoritative v1.17 live route;
- preserve exact locked item placement and reserved-cell constraints;
- derive user instruction from current state → chosen final state diff using existing confirmed action vocabulary;
- preserve quantity/stack and weight behavior within the confirmed rules; add only system mechanics required for correctness, not new preferences;
- add deterministic regression coverage and source-contract tests;
- add a durable developer-authority guard to `AGENTS.md` before completion;
- validate Windows Release build, tests, published EXE Product UI/runtime smoke, graceful shutdown and package integrity;
- synchronize authoritative project docs;
- only then merge/release.