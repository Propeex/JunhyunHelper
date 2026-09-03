# ACTIVE WORK

Status: **ACTIVE**

## Current task

Farming Guide raid-advisor rule simplification, complete-state optimization, and FIR provenance hardening.

## Goal

Align Farming Guide with the user-confirmed product model:

- separate **system rules** from **farming rules**;
- system rules enforce Tarkov-valid inventory mechanics, explicit user locks, data/proof safety and transaction consistency, but do not become farming preferences;
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
last code/test checkpoint before this document update: fd592c08983c61da847cf177cfd3e95347174d91
last fully green FIR implementation checkpoint: 2103c508cb1c4781a4baa1480b1dcecb558a574c
current work: active; do not merge/release until final release-quality validation and authoritative documentation are complete
```

## Confirmed scope

1. Keep the user-facing farming rulebook minimal and fixed:
   - currently needed FIR quest/hideout units first, capped by remaining need;
   - then maximize complete final retained average-Flea value;
   - respect weight as the only user-configurable farming constraint.
2. Treat category-specific tactical retention/upgrade logic as superseded: food, drink, ammunition, magazines, medicine, armor/headset performance and similar categories receive no automatic privilege unless the exact item is FIR-needed.
3. Treat all Tarkov-valid placement behavior, explicit locks, quantity/provenance/value/weight facts, transaction revision consistency, solver proof boundaries and uncertainty reporting as system rules rather than additional farming priorities.
4. Replace local insertion/victim reasoning with a complete candidate-pool model: preserve explicit locks, release all other movable items conceptually, add the scanned item, solve the best complete legal state, then derive the user instruction from the state difference.
5. Preserve fail-closed safety without conflating uncertainty with a proven discard: missing facts, incomplete solver domain or exhausted proof budget must produce an indeterminate/no-advice result rather than `버리기`.
6. Reuse already verified Tarkov placement/filter/nesting mechanics where valid, but do not weaken the new rulebook to fit historical local-planner assumptions.
7. Add deterministic regression coverage for rule boundaries, global-optimum cases, locks, weight, nested/new containers, top-level equipment/carriers, FIR quantity/provenance and indeterminate safety.
8. Keep PR #287 Draft until implementation, tests, Release/published EXE smoke, packaging and documentation consistency are all complete.

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
- Separated active-raid acquisition provenance from Tarkov FIR provenance:
  - `RaidAcquired` records when the modeled raid acquired an instance;
  - `FarmingGuideFirStatus` is independently `Unknown`, `NotFoundInRaid`, or `FoundInRaid`;
  - only explicit `FoundInRaid` units satisfy the primary FIR objective;
  - normalization/repacking preserves both facts;
  - stack quantity optimization receives FIR-qualified units rather than treating all raid-acquired units as FIR.
- Corrected v1.17 FIR-need fallbacks to use `ScannerItemSnapshot.CurrentNeededFir`, not generic `CurrentNeeded`.
- Implemented conservative automatic Scanner FIR observation from the same header-locked inspect-window pixels that prove the scanned item identity:
  - `ScannerFirMarkerDetector` uses scale-relative lower-left geometry, circular-marker evidence and check-stroke evidence rather than a copyrighted bitmap template;
  - neutral gray/white and yellow/gold FIR check variants are accepted as positive evidence;
  - only the exact current `TarkovWindow` live scan may promote `Unknown` to `FoundInRaid`;
  - Display Test and simulated Farming Guide scans remain catalog-only and cannot manufacture FIR provenance;
  - absence or ambiguous detection remains `Unknown`; Scanner does **not** infer `NotFoundInRaid` from a missing marker.
- Propagated live Scanner FIR observation through `ScannerItemSnapshot` into the incoming `FarmingGuideItemState`, and preserved it through the unified global solver.
- Unified quantity-one and stacked incoming items onto the same quantity-complete live solver so FIR/weight/proof semantics do not diverge through an older single-item branch.
- Added deterministic FIR marker regressions covering neutral positive, yellow positive, ring-only negative, check-only negative, out-of-ROI negative and bright-noise negative cases.
- Added source-contract regressions proving live FIR is Tarkov-window-only, positive-only, simulator-safe, and connected to the global solver.
- Renamed the stack-quantity optimizer's historical FIR-bearing `RaidAcquired` terminology to explicit `FirQualified` / `fixedFirQualifiedUnits`; this removes the remaining semantic naming trap while preserving the real `FarmingGuideItemState.RaidAcquired` acquisition fact.
- Validation for FIR implementation checkpoint `2103c508cb1c4781a4baa1480b1dcecb558a574c`:
  - Documentation Consistency: success (`33724798003`);
  - Shutdown Race CI: success (`33724798019`);
  - full Windows CI: success (`33724798037`), including Desktop build, core tests, Windows x64 publish, Product UI/runtime smoke, package verification and artifact upload.
- A fresh CI generation is required after the semantic stack-optimizer rename/checkpoint ending at `fd592c08983c61da847cf177cfd3e95347174d91`.

## Rule-level findings that must remain explicit

1. **FIR is quantity-based, not a permanent item-category flag.** Only the remaining required units receive first priority; surplus copies revert to ordinary economic value.
2. **Category is irrelevant.** Food/ammo/medicine/etc. can be first-priority when currently FIR-needed and ordinary loot otherwise.
3. **Raid-acquired is not FIR.** An item obtained during the modeled raid may still be non-FIR; `RaidAcquired` must never be used as a substitute for Tarkov FIR provenance.
4. **Unknown FIR is not false and not true.** When FIR status can affect the optimum, Unknown must cause fail-closed/no-advice until the status is proven.
5. **Scanner marker absence is not non-FIR proof.** Current automatic observation is deliberately positive-only; false negatives degrade to `Unknown`/no-advice rather than a destructive false conclusion.
6. **Tetris/slot density is not a third priority.** It matters only because it changes which legal complete states fit; the objective remains final retained total value.
7. **Locks remove optimizer choices; they do not add item value.** Item/slot/carrier/cell lock representations are system state implementing the user's explicit fixed-item/fixed-cell intent.
8. **Unknown facts are not zero.** Destructive advice fails closed when needed/value/legality facts required for proof are unavailable.
9. **A bounded heuristic may not be presented as the optimum.** If the solver cannot prove a destructive result within its deterministic budget, it must not issue a known-unproven discard/rearrangement.
10. **Indeterminate is not discard.** `Discard` requires a proven farming decision; uncertainty leaves modeled inventory unchanged.
11. **Current layout is not a farming preference.** Current placement/fewer moves may only be a deterministic tie/stability choice after FIR satisfaction and total retained value are equal.

## Remaining implementation / validation gaps

These are why PR #287 remains Draft:

1. **Fresh post-cleanup CI.** Re-run/observe all branch checks after the stack-optimizer terminology cleanup and source-contract update.
2. **Live FIR ground-truth boundary.** The new detector is covered by deterministic synthetic shape/noise tests and current UI/reference inspection, but the repository does not yet contain a broad real-user Tarkov FIR screenshot corpus across resolutions/UI scales. Because the detector is positive-only, false negatives safely become `Unknown`; false positives remain the risk to watch in real use. Do not weaken the detector to improve recall without ground-truth evidence.
3. **Release-quality documentation.** Update `PRODUCT.md`, `DECISIONS.md`, `CURRENT_STATE.md`, `STATE.md`, machine-readable project state/release notes as appropriate after final validation.
4. **Final release validation.** Require green Windows Release/published EXE Product UI/runtime smoke, graceful shutdown, packaging, documentation consistency, exact-main state and public release asset integrity before calling v1.17.0 complete.

## Current step

Wait for and inspect the fresh CI generation after `fd592c08983c61da847cf177cfd3e95347174d91` plus this checkpoint update. If green, perform the final source/rule audit and authoritative-document update. Preserve the positive-only Scanner FIR boundary: do not turn marker absence into `NotFoundInRaid` without independent trusted evidence.

## Remaining

- confirm the fresh CI generation is fully green after the FIR-qualified terminology cleanup;
- perform final source/rule audit for tactical/category/value-density regressions, `RaidAcquired => FIR` leakage, unknown-to-discard leakage, and accidental use of the obsolete single-item solver path;
- record the real-live FIR detector validation boundary explicitly in authoritative docs;
- update final project/product/state/release documentation;
- only then mark PR ready, merge, exact-main verify and release/document as appropriate.
