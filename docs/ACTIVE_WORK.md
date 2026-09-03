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
latest implementation checkpoint before this document update: 418e609b0ca325a0f589e76c1b54fd46a72db413
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

Code from the abandoned branch may only be consulted as a non-authoritative implementation reference after the stable-main design is independently derived and only if it matches this confirmed rulebook. No Scanner FIR observation code is to be reused.

## Completed

- Closed PR #287 unmerged and marked it abandoned.
- Created clean branch from stable `main@379c6ab4ab02431c6bb74b537e899e94f45ee987` and opened Draft PR #288.
- Recorded the canonical confirmed v1.17 rulebook.
- Removed tactical/category/equipment-superiority/local-planner semantics from the authoritative live raid decision route.
- Added ephemeral `[JsonIgnore]` `FarmingGuideItemState.RaidAcquired` provenance; Scanner does not inspect FIR and presets do not persist raid provenance.
- Added complete-state `FarmingGuideOptimizationScore` with exactly two dimensions: satisfied FIR quantity and retained Flea value.
- Added deterministic from-scratch `FarmingGuideGlobalPackingPlanner` with fixed-placement preservation, owned/nested surfaces, cycle rejection, cross-surface validation and explicit budget failure.
- Built the global candidate pool across stored roots, top-level equipment, Rig, Backpack, Secure Container, nested storage and the incoming Scanner item.
- Routed the active raid decision through the v1.17 global optimizer and preserved `RaidAcquired` through accepted-state sanitization.
- Enforced weight as a strict final-state constraint rather than an item priority.
- Made v1.17 root geometry and complete assembly weight proof fail closed on unknown facts instead of treating unknown values as 1x1 or 0 kg.
- Included modeled attachment/armor-plate descendants in complete retained Flea value, FIR fact collection and final/current weight proof.
- Included fixed out-of-pool Melee/Dogtag state in final weight proof.
- Enforced Tarkov assembly `ConflictingSlotIds` in both directions and added `ItemPropertiesHeadwear` compatibility for the head equipment slot.
- Preserved body armor/armored-rig, helmet/headset, item conflict, grid filter, nesting, owner graph and stack legality in final global validation.
- Exposed same-storage-area position/rotation changes as explicit `내부 재배치` instructions instead of silently requiring movement.
- Audited stack quantity end to end: user-entered ammo/currency quantity remains one observed stack instance and scales FIR/value/weight; no automatic split/merge behavior was invented.
- Added deterministic stack quantity tests for acquired and baseline stacks.
- Audited lock ancestry: a fixed nested item or nested fixed cell fixes the necessary stored ancestor chain and root carrier so indirect movement is impossible.
- Added published Product Smoke for nested fixed-item/fixed-cell ancestry propagation.
- Removed the legacy v1.15 local raid-priority smoke from the published gate so obsolete tactical/local-planner expectations are not product authority.
- Preserved dedicated nested storage as a legal final-placement choice and added a v1.17 global-solver smoke proving a compatible incoming key can be placed inside the existing dedicated container without adding a retention priority.
- Confirmed attachment/plate price/FIR facts reuse the existing Scanner catalog/presentation resolver for arbitrary canonical item IDs; no new observation source or inference path was added and missing facts fail closed.
- Added a durable developer product-semantics authority boundary to `AGENTS.md`.
- Verified an earlier legality/fact-proof checkpoint on PR #288 with CI, Shutdown Race CI and Documentation Consistency all successful.
- Latest implementation checkpoint `418e609b0ca325a0f589e76c1b54fd46a72db413` has passed Windows desktop compilation and core deterministic tests; its full publish/runtime/package and shutdown workflows are being completed before release-prep commits.

## Current step

Complete final branch validation, then prepare the v1.17.0 release identity and authoritative release documentation. The implementation/fact/legality audit has no unresolved product-semantic question at this checkpoint.

## Remaining

- require the latest implementation checkpoint to pass win-x64 publish, actual published EXE Product UI/Farming Guide smoke, graceful shutdown, package verification, dedicated Shutdown Race CI and Documentation Consistency;
- bump Desktop/EXE/FIRST_RUN release identity from v1.16.4 to v1.17.0 and add matching release notes/status documentation;
- re-run the complete branch release gate on the final version/documentation HEAD;
- perform final PR #288 changed-file/review audit and synchronize its description/state;
- merge only after the final branch gate is green;
- validate exact `main` with CI, Shutdown Race CI and Documentation Consistency;
- publish stable v1.17.0 only from the exact validated main source and verify tag/release/assets/checksums;
- update `PROJECT_STATE`, `CURRENT_STATE`, `STATE` and release-status facts to the actual public release;
- close `ACTIVE_WORK` only after merge, exact-main validation, release publication and documentation are complete.