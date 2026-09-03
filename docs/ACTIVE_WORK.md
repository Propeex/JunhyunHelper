# ACTIVE WORK

Status: **ACTIVE**

## Current task

Farming Guide raid-advisor rule simplification and global inventory optimization.

## Goal

Align the Farming Guide with the user-confirmed product model:

- separate **system rules** from **farming rules**;
- system rules enforce Tarkov-valid inventory mechanics, user locks, data/proof safety and transaction consistency, but do not become farming preferences;
- farming priority is lexicographic: (1) currently needed FIR quest/hideout units, then (2) maximum retained economic value;
- FIR priority is category-agnostic and capped by each item's remaining required quantity;
- food, drink, ammunition, magazines, medicine, armor/headset performance and other tactical categories receive no automatic retention/upgrade privilege unless the exact item is currently FIR-needed; users protect personally important items with locks;
- weight is the only user-configurable farming constraint;
- every scan conceptually releases all unlocked inventory items into one candidate pool with the incoming item and solves for the best legal final inventory state, instead of locally inserting the incoming item around the current arrangement;
- geometry, filters, rotation, nesting, stack rules, lock preservation and deterministic fail-closed behavior are system mechanics, not farming priorities.

## Base / working state

```text
base main: 379c6ab4ab02431c6bb74b537e899e94f45ee987
public stable: v1.16.4
working branch: feature/v1.17.0-farming-guide-global-optimization-2026-09-03
Draft PR: #287
current reviewed head: ecedcd48baae1cf4e6ac349d8241d7c946da5c7d
```

## Confirmed rule authority

`docs/DECISION_V1.17.0_FARMING_GUIDE_GLOBAL_OPTIMIZATION.md` is the canonical decision for this work and supersedes conflicting older raid-loot heuristics.

Formal farming objective for every legal candidate final state `S`:

```text
Primary(S)   = sum over item ids min(FIR-qualified retained units, remaining FIR need)
Secondary(S) = total retained average Flea value
maximize (Primary(S), Secondary(S))
```

subject to Tarkov-valid placement, explicit user locks and the configured weight rule.

## Completed

- Recovered repository authority and current v1.16.4 state.
- Audited historical Farming Guide rule/planner paths and found the following conflicting old behavior:
  - local displacement / direct-fit-first conceptual model;
  - tactical food/drink/current-weapon-ammunition retention;
  - item-level weight/footprint priority tie-breaks;
  - armor/headset combat-performance upgrade heuristics.
- Recorded the v1.17 canonical system-rule/farming-rule decision, including quantity-capped FIR semantics, complete-state economic objective and bounded-search fail-closed requirement.
- Simplified `FarmingGuideLootPriorityPolicy` to FIR-needed then total Flea value only; weight and footprint are no longer farming priority tiers.
- Added `FarmingGuideOptimizationPolicy` for complete-state lexicographic scoring and deterministic tests covering FIR dominance, remaining-quantity capping, raid-baseline acquisition and retained-value tie resolution.
- Routed the live scan path through v1.17 decision/safety entry points, bypassing tactical armor/headset superiority heuristics and tactical resource retention.
- Added best-first unlocked stored-item subset optimization on top of existing proven storage legality/repacking mechanics.
- Added fail-closed victim handling: items whose required FIR/value facts cannot be proven are not automatically sacrificed.
- First Draft PR CI proved Desktop compilation succeeded; the initial core-test failure was an obsolete test asserting weight as an item-level priority. That test has been corrected to the newly confirmed product contract and CI is rerunning.
- Documentation Consistency is green on the current Draft PR head.

## Rule-level findings that must remain explicit

1. **FIR is quantity-based, not a permanent item-category flag.** Only the remaining required units receive first priority; surplus copies revert to ordinary economic value.
2. **Category is irrelevant.** Food/ammo/medicine/etc. can be first-priority when currently FIR-needed and ordinary loot otherwise.
3. **Tetris/slot density is not a third priority.** It matters only because it changes which legal complete states can fit; the objective remains final retained total value.
4. **Locks remove optimizer choices; they do not add item value.** Item/slot/carrier/cell lock representations are system state implementing the user's explicit fixed-item/fixed-cell intent.
5. **Unknown facts are not zero.** Destructive advice fails closed when needed/value/legality facts required for proof are unavailable.
6. **A bounded heuristic may not be presented as the optimum.** If the solver cannot prove a destructive result within its deterministic budget, it keeps the current state rather than issuing a known-unproven discard/rearrangement.
7. **Current layout is not a farming preference.** Fewer moves/current placement may be used only as a deterministic tie-break after FIR satisfaction and total retained value are exactly equal.

## Remaining implementation gaps found by the global-rule audit

These are why PR #287 remains Draft and must not be merged/released yet:

1. **Populated-container decomposition.** The current v1.17 destructive search safely treats only leaf stored items as removable. A truly global pool must be able to move a container's contents elsewhere and then retain or discard the unlocked container independently, while preserving legal nested surfaces. Current behavior fails closed rather than deleting descendants, but can therefore miss the true optimum.
2. **New-container capacity in the same scan.** Existing transition surfaces are built from currently stored containers. If the newly scanned item itself is a legal storage container, a true global solver must be able to keep it and simultaneously use its newly introduced internal grids for the same final-state optimization.
3. **Top-level equipment/carrier unification.** The current v1.17 path removes combat-performance priority and complete-state-checks equipment replacement candidates, but unlocked equipment/carriers are not yet fully represented in the same global candidate pool as ordinary stored items. The final implementation must optimize them only as legal placement/capacity surfaces and economic items, subject to locks.
4. **FIR provenance edge case.** Current session acquisition accounting is primarily baseline-count based. For complete semantic precision, the model should preserve raid-acquired/FIR provenance when an identical baseline copy is discarded and a new copy is acquired, rather than relying only on net item-id count.
5. **Stack granularity.** Current modeled stack instances are retained/evicted as whole instances. If partial-stack split recommendations are to be part of the physical system model, the global solver must model legal split quantities explicitly rather than pretending whole-stack selection is mathematically complete.
6. **Low-level packing search completeness.** The existing repacking engine is safe and deterministic but historically optimized local displacement and is bounded. It can serve as a legality proof during migration, but a final "global optimum" claim requires an all-selected-items packing proof or an explicit fail-closed proof boundary.

## Current step

Keep the product rulebook fixed while replacing the remaining local/hierarchical implementation assumptions with a complete candidate-pool solver. Do not weaken the rulebook to fit old code.

## Remaining

- finish complete global solver for unlocked stored items, nested containers, newly acquired containers and top-level equipment/carriers;
- make FIR acquisition/provenance semantics explicit enough for quantity-correct optimization;
- decide/implement system-level stack split representation only if required for exact physical optimization, without adding a farming preference;
- add deterministic regression cases for populated containers, incoming-container capacity, equipment/storage trade-offs, locks, weight and unknown-fact fail-closed behavior;
- update `PRODUCT.md`, `DECISIONS.md`, architecture/current-state/state docs when implementation semantics are no longer transitional;
- require green deterministic tests, Windows Release build, published EXE Product UI/runtime smoke, graceful shutdown, packaging and Documentation Consistency;
- only then mark PR ready, merge, exact-main verify and release/document as appropriate.
