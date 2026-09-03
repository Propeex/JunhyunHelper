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
latest audited implementation checkpoint before this document update: 5118a913811f837f9960ccc54d654f76b0a5559d
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
- Removed the old tactical/category/equipment-superiority/local-planner semantics from the authoritative live raid decision route.
- Added ephemeral `[JsonIgnore]` `FarmingGuideItemState.RaidAcquired` provenance and explicit raid-acquired inventory counting without modifying Scanner or persisted presets.
- Added complete-state `FarmingGuideOptimizationScore` with exactly two dimensions: satisfied FIR quantity and retained Flea value.
- Added deterministic from-scratch `FarmingGuideGlobalPackingPlanner` with fixed-placement preservation, owned/nested surfaces, cycle rejection, cross-surface final validation, and explicit `BudgetExceeded` handling.
- Built the live global candidate pool across stored items, top-level equipment, Rig, Backpack, Secure Container, nested storage and incoming Scanner items.
- Routed the active raid decision through the v1.17 global optimizer and preserved `RaidAcquired` through accepted-state sanitization.
- Enforced weight as a strict final-state constraint and added fail-closed handling for unknown destructive Flea comparisons.
- Added v1.17 WPF Product UI/runtime smoke coverage for FIR priority, quota capping, no tactical food privilege, incoming-container capacity, locks, equipment participation, consecutive scans, overweight rejection and unknown-price fail-closed behavior.
- Verified PR #288 head `5118a913811f837f9960ccc54d654f76b0a5559d`: CI, Shutdown Race CI and Documentation Consistency all succeeded.
- Confirmed generic global owner-graph validation already rejects self-containment and indirect nested-container cycles.
- Confirmed raid value display is based on explicit `RaidAcquired` provenance rather than baseline-count inference.

## Current step

Finish the Tarkov system-legality and fact-proof audit of the current global optimizer. The audit has identified concrete implementation gaps that do not require new product semantics:

- complete assembled equipment currently contributes only the root item's weight to global final-weight validation;
- missing item weight can still collapse to `0 kg` in legacy helpers used by the global path;
- missing root dimensions can still collapse to `1x1` for global packing;
- complete-state candidate facts/ranking currently omit attachment/armor-plate Flea value even though snapshot inventory counting includes those retained items;
- `ConflictingSlotIds` is assembly-slot legality and is not yet enforced by `FarmingGuideAssemblyPolicy`;
- upstream `ItemPropertiesHeadwear` is not yet accepted by the shared head equipment-slot compatibility rule;
- a global solve may require an existing stored item to move/rotate inside the same visible storage area, while the current v1.17 instruction projection suppresses that physical delta.

These are being corrected fail-closed and mechanically, without adding new farming priorities, Scanner inference, data sources or user confirmation flows.

## Remaining

- make complete-state Flea/FIR fact collection and retained-set scoring include modeled attachment/armor-plate descendants;
- make v1.17 current/final weight proof assembly-aware, include fixed out-of-pool Melee/Dogtag state, and fail closed on unknown weight;
- fail closed on unknown root geometry before global destructive advice;
- enforce `ConflictingSlotIds` with exact assembly slot context and add Headwear slot compatibility;
- make current → final instructions surface required same-area grid/rotation changes;
- complete stack/quantity and locked-ancestry regression audit without inventing automatic split/merge behavior;
- audit published smoke/source contracts for stale v1.16.x farming expectations;
- add deterministic regression coverage for every corrected legality/fact boundary;
- add the durable developer-authority guard to `AGENTS.md` before completion;
- validate Windows Release build, full deterministic tests, win-x64 publish, actual published EXE Product UI/runtime smoke, graceful shutdown and package integrity;
- synchronize authoritative project docs and final Draft PR description/state;
- only then merge/release.