# ACTIVE WORK

Status: **ACTIVE**

## Current task

Farming Guide raid-advisor rule simplification, complete-state optimization, and corrected FIR raid-session semantics.

## Goal

Align Farming Guide with the user-confirmed product model:

- separate **system rules** from **farming rules**;
- system rules enforce Tarkov-valid inventory mechanics, explicit user locks, data/proof safety and transaction consistency, but do not become farming preferences;
- farming priority is lexicographic: (1) currently needed FIR quest/hideout units, then (2) maximum retained economic value;
- FIR priority is category-agnostic and capped by each item's remaining required quantity;
- **while a Farming Guide raid is active, every newly Scanner-identified incoming item is modeled as FIR; Scanner itself does not inspect or classify an FIR marker**;
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
branch checkpoint before this document update: b0d332d9e3d244e1444161acaf5c2c9fadaa43cc
current work: active; do not merge/release until corrected semantics receive fresh full CI and final documentation/validation
```

## Confirmed scope

1. Keep the user-facing farming rulebook minimal and fixed:
   - currently needed FIR quest/hideout units first, capped by remaining need;
   - then maximize complete final retained average-Flea value;
   - respect weight as the only user-configurable farming constraint.
2. During an active Farming Guide raid session, treat every newly Scanner-identified incoming item as FIR. This is a Farming Guide raid-session rule. Do **not** add Scanner-side FIR icon/checkmark/color/OCR classification and do not add a separate FIR confirmation interaction.
3. Treat category-specific tactical retention/upgrade logic as superseded: food, drink, ammunition, magazines, medicine, armor/headset performance and similar categories receive no automatic privilege unless the exact item is FIR-needed.
4. Treat all Tarkov-valid placement behavior, explicit locks, quantity/provenance/value/weight facts, transaction revision consistency, solver proof boundaries and uncertainty reporting as system rules rather than additional farming priorities.
5. Replace local insertion/victim reasoning with a complete candidate-pool model: preserve explicit locks, release all other movable items conceptually, add the scanned FIR item, solve the best complete legal state, then derive the user instruction from the state difference.
6. Preserve fail-closed safety without conflating uncertainty with a proven discard: missing facts, incomplete solver domain or exhausted proof budget must produce an indeterminate/no-advice result rather than `버리기`.
7. Reuse already verified Tarkov placement/filter/nesting mechanics where valid, but do not weaken the new rulebook to fit historical local-planner assumptions.
8. Add deterministic regression coverage for rule boundaries, global-optimum cases, locks, weight, nested/new containers, top-level equipment/carriers, FIR quantity/provenance and indeterminate safety.
9. Keep PR #287 Draft until implementation, tests, Release/published EXE smoke, packaging and documentation consistency are all complete.

## Confirmed rule authority

`docs/DECISION_V1.17.0_FARMING_GUIDE_GLOBAL_OPTIMIZATION.md` is the canonical decision for this work and supersedes conflicting older raid-loot heuristics and the abandoned Scanner FIR visual-classification attempt.

Formal farming objective for every legal candidate final state `S`:

```text
Primary(S)   = sum over item ids min(FIR-qualified retained units, remaining FIR need)
Secondary(S) = total retained average Flea value
maximize (Primary(S), Secondary(S))
```

subject to Tarkov-valid placement, explicit user locks and the configured weight rule.

## Completed

- Recovered repository authority and current v1.16.4 state.
- Recorded the canonical v1.17 system-rule/farming-rule decision and removed historical tactical-category priorities from the authoritative live path.
- Added explicit non-committing `Indeterminate` advice; incomplete/unknown/budget-exhausted proofs do not become pending transactions and do not mutate raid inventory state.
- Simplified Farming Guide priority to FIR need then total retained economic value; item weight/footprint/value-density are not farming priority tiers.
- Implemented the complete-state lexicographic optimization score and deterministic regression tests.
- Implemented a deterministic all-selected-items global packing proof that rebuilds unlocked placement from scratch and fails closed on proof-budget exhaustion.
- Unified ordinary stored items, nested containers, top-level equipment/carriers and the incoming item into one root candidate pool.
- Selected existing or newly scanned containers can contribute their internal storage grids during the same solve; removed containers contribute no capacity and their descendants must be legally repacked.
- Implemented exact bounded stack-quantity optimization under the weight constraint, including partial retained quantities when mathematically required.
- Added source-backed stack maximum and discard-limit ingestion. `discardLimit` is preserved as data but is not yet used as a legality rule because the current helper does not have sufficient trusted raid-count provenance to enforce it correctly.
- Added global-state-difference instruction presentation so retained/moved/equipped/discarded roots are reported from the optimized final state rather than legacy local-victim semantics.
- Preserved explicit item/equipment/carrier/cell locks as system constraints throughout global packing.
- Kept explicit modeled FIR provenance so quantity-capped FIR scoring remains exact and identical-item replacement cannot be confused with baseline ownership.
- Renamed the stack-quantity optimizer's historical FIR-bearing `RaidAcquired` terminology to explicit `FirQualified` / `fixedFirQualifiedUnits`; the actual `FarmingGuideItemState.RaidAcquired` field remains an acquisition-history fact.
- Corrected the product FIR boundary after user review:
  - every newly Scanner-identified incoming item during an active Farming Guide raid is created with `RaidAcquired = true` and `FirStatus = FoundInRaid`;
  - Scanner no longer supplies FIR status;
  - the live global solver no longer waits for `scanned.FirStatus` or FIR-marker proof;
  - previously accepted modeled raid items preserve their explicit FIR state through repacking/normalization.
- Fully removed the unintended Scanner FIR visual-classification implementation:
  - restored `ScannerModels.cs` to remove Scanner FIR fields;
  - restored `ScannerCoordinator.FarmingGuide.cs` to catalog/presentation snapshot behavior;
  - restored `ScannerLab38WindowsVision.cs` so capture/OCR has no FIR detector call;
  - deleted `ScannerFirMarkerDetector.cs`;
  - deleted `ScannerRuntimeService.FirObservationV1170.cs`;
  - deleted the FIR marker detector tests.
- Replaced the old source-contract test with a guard that requires active-raid incoming roots to be `FoundInRaid` and forbids Scanner-side FIR marker/classification code.
- Updated the canonical v1.17 decision with the explicit active-raid scan FIR rule and Scanner non-responsibility.

## Rule-level findings that must remain explicit

1. **FIR is quantity-based, not a permanent item-category flag.** Only the remaining required units receive first priority; surplus copies revert to ordinary economic value.
2. **Active-raid Scanner incoming items are FIR by product contract.** The product does not spend capture/OCR complexity re-proving FIR from screen pixels after the Farming Guide raid has already established the context.
3. **Raid-start baseline items do not gain FIR from starting a raid.** The automatic FIR assignment applies to newly scanned incoming loot, not pre-existing modeled ownership.
4. **Category is irrelevant.** Food/ammo/medicine/etc. can be first-priority when currently FIR-needed and ordinary loot otherwise.
5. **Scanner does not own FIR semantics.** No FIR icon/checkmark/color/text detector or manual FIR confirmation should be introduced unless the user explicitly changes this product rule.
6. **Tetris/slot density is not a third priority.** It matters only because it changes which legal complete states fit; the objective remains final retained total value.
7. **Locks remove optimizer choices; they do not add item value.** Item/slot/carrier/cell lock representations are system state implementing the user's explicit fixed-item/fixed-cell intent.
8. **Unknown facts are not zero.** Destructive advice fails closed when needed/value/legality facts required for proof are unavailable. This does not apply to FIR of a new active-raid scan because that FIR status is already defined by the product rule.
9. **A bounded heuristic may not be presented as the optimum.** If the solver cannot prove a destructive result within its deterministic budget, it must not issue a known-unproven discard/rearrangement.
10. **Indeterminate is not discard.** `Discard` requires a proven farming decision; uncertainty leaves modeled inventory unchanged.
11. **Current layout is not a farming preference.** Current placement/fewer moves may only be a deterministic tie/stability choice after FIR satisfaction and total retained value are equal.

## Product-semantics safety lesson / future guard

The abandoned Scanner FIR detector was a product-semantic change, not an internal optimization. Future work must obey the repository's existing intent-alignment rule strictly:

- internal performance, architecture, testing and maintenance may be improved autonomously only while confirmed product behavior is preserved;
- adding a new observation requirement, user interaction, classification rule, data-authority rule, or failure behavior is a product decision when it changes what the program believes or tells the user;
- when that meaning is not already confirmed in product/decision documents, do not invent it to solve an implementation gap; obtain user confirmation first;
- tests should guard confirmed product boundaries so future refactors cannot silently recreate discarded semantics.

## Remaining implementation / validation gaps

These are why PR #287 remains Draft:

1. **Fresh corrected-semantics CI.** Run/observe all branch checks after removing Scanner FIR classification and assigning FIR at the Farming Guide raid-session boundary.
2. **Final source/rule audit.** Confirm no tactical/category/value-density priority has leaked back in, no obsolete local solver is used in the live route, no Scanner FIR detector/classifier remains, and no uncertainty is converted to discard.
3. **Release-quality documentation.** Update `PRODUCT.md`, `DECISIONS.md`, `CURRENT_STATE.md`, `STATE.md`, machine-readable project state/release notes as appropriate after final validation.
4. **Final release validation.** Require green Windows Release/published EXE Product UI/runtime smoke, graceful shutdown, packaging, documentation consistency, exact-main state and public release asset integrity before calling v1.17.0 complete.

## Current step

Observe fresh CI for the corrected active-raid-scan-is-FIR implementation. If green, perform the final source/rule audit and authoritative-document update. Do not reintroduce any Scanner-side FIR observation path.

## Remaining

- confirm corrected branch CI is fully green;
- perform final source/rule audit for tactical/category/value-density regressions, Scanner FIR-classification leakage, unknown-to-discard leakage, and accidental use of the obsolete single-item solver path;
- update final project/product/state/release documentation;
- only then mark PR ready, merge, exact-main verify and release/document as appropriate.
