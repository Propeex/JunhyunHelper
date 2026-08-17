# EFT 1.1 trader task-pool prerequisite audit — 2026-08-17

## Status

**AUDIT COMPLETE / CURRENT-VERSION COMPATIBILITY ADOPTED**

This document records the evidence and product boundary for EFT 1.1 `globalVariable` Quest availability. DEC-044 remains the generic rule: preserve exact read-side requirements and prefer exact observed profile values. The post-v0.1.8 usability pass adds a narrower current-version compatibility because treating all 162 usages as unrelated unknown facts produces a clearly over-broad `확인 필요` result.

## Current live structure

A fresh 2026-08-17 regular-mode audit found:

- 517 tasks total,
- 162 quests using `globalVariable`,
- exactly 27 unique variable IDs,
- all comparisons are `>=` small integer thresholds,
- every ID is trader-local to one of Prapor / Therapist / Skier / Peacekeeper / Mechanic / Ragman / Jaeger,
- the variables form LL1→LL4 staged groups; Ragman currently has three staged groups,
- the global-variable Quest itself has no ordinary task/trader prerequisite that could replace the variable gate.

The same 162 / 27 shape had already been observed across regular / PvE / PvP Season in the earlier audit.

## Direct LL seed cross-check

The live feed was audited again for non-global Quest batches gated directly by the same trader's loyalty level. The exact current counts are:

| Trader | LL2 seeds | LL3 seeds | LL4 seeds |
|---|---:|---:|---:|
| Prapor | 4 | 5 | 4 |
| Therapist | 4 | 4 | 3 |
| Skier | 3 | 3 | 4 |
| Peacekeeper | 4 | 5 | 3 |
| Mechanic | 4 | 5 | 5 |
| Ragman | 4 | 5 | 4 |
| Jaeger | 6 | 8 | 5 |

These batches line up with the staged variables and their first thresholds. This gives a current-version model of:

```text
reach trader LL
  -> direct LL seed batch becomes available
  -> completed seed / newly unlocked pool tasks advance the pool counter
  -> variable threshold unlocks the next pool batch
```

The public task feed still does not publish the generic server-side write rule. Therefore this is **not** promoted to a generic inference algorithm.

## Exact audited rules

### Prapor

- `6a20540cf1b67a977cc5a088` — LL1, 8 quests, thresholds {1,3,5}
- `6a2688488bba18e0b0187a04` — LL2, 6 quests, thresholds {3,5}, seed count 4
- `6a32651a811905ed0cac0973` — LL3, 6 quests, thresholds {1,3}, seed count 5
- `6a326525789ae12ecb0b2807` — LL4, 5 quests, thresholds {1,2}, seed count 4

### Therapist

- `6a4e4ab3ecd1145894d00990` — LL1, 6 quests, thresholds {1,2,4}
- `6a4e4aed3ded7a18126603f6` — LL2, 6 quests, thresholds {1,2,4}, seed count 4
- `6a4e4b28629dc64c4001967c` — LL3, 5 quests, thresholds {1,3}, seed count 4
- `6a56925b1c30ba5a77c7c518` — LL4, 1 quest, threshold {1}, seed count 3

### Skier

- `6a59f3ba06c8949abad30871` — LL1, 8 quests, thresholds {1,2,3}
- `6a5a111de1f417ac80a163e5` — LL2, 9 quests, thresholds {1,3,4}, seed count 3
- `6a5a115181116e807b55f258` — LL3, 6 quests, thresholds {1,3}, seed count 3
- `6a5a1192efde11cc7105b18f` — LL4, 2 quests, threshold {1}, seed count 4

### Peacekeeper

- `6a5ba40fe5c4eaef5610f232` — LL1, 6 quests, thresholds {1,3}
- `6a5ba450a7851e16ce0bde44` — LL2, 9 quests, thresholds {1,3,5}, seed count 4
- `6a5ba48b8cfd0bddb3d4d2e1` — LL3, 4 quests, thresholds {2,4}, seed count 5
- `6a5ba4c57cbb93b629051591` — LL4, 7 quests, thresholds {1,3}, seed count 3

### Mechanic

- `6a3171c927ca9591bf4db1c4` — LL1, 6 quests, thresholds {1,3}
- `6a3c0fefbea2d2ad581c090b` — LL2, 10 quests, thresholds {1,3,5}, seed count 4
- `6a3cf95c6b35530c4a4f532e` — LL3, 12 quests, thresholds {1,3,5}, seed count 5
- `6a3d1c0990e9ffe15463e961` — LL4, 2 quests, threshold {1}, seed count 5

### Ragman

- `6a4b339f18db62e03b4f7ded` — LL1, 6 quests, thresholds {1,2}
- `6a4b4e6a30dac4b01af220aa` — LL2, 7 quests, thresholds {1,2,4}, seed count 4
- `6a4b9c9a60b56d421cceea18` — LL3, 3 quests, thresholds {1,2}, seed count 5

### Jaeger

- `6a43a01ccc83aceedd35f09c` — LL1, 8 quests, thresholds {1,3}
- `6a43a095bfef0cd74c298963` — LL2, 4 quests, thresholds {2,5}, seed count 6
- `6a43a13633c97d216dfc85de` — LL3, 7 quests, thresholds {2,4}, seed count 8
- `6a43a16dde81644a7951f31b` — LL4, 3 quests, threshold {1}, seed count 5

## Product compatibility boundary

The current Quest UI may reconstruct a missing value only when **all** of the following remain true:

1. exact variable ID is in the audited 27-ID table,
2. trader ID matches,
3. the pool contains the exact audited number of quests,
4. every pool Quest still has exactly one matching `>=` profile-variable requirement,
5. threshold set is unchanged,
6. pool Quests have no new ordinary task/trader/unsupported availability requirements,
7. for LL2+ the direct same-trader loyalty seed count exactly matches the audit.

If any condition drifts, the compatibility does nothing and the original `Indeterminate` behavior wins.

## Exact profile values always win

If a profile already contains the exact EFT variable value, the compatibility never replaces it. Synthetic current-version values exist only in the temporary profile copy used to evaluate current Quest availability and are not persisted to `user.db`.

This keeps future scanner/import support compatible with the exact model.

## LL2–LL4 behavior

For a validated LL2–LL4 pool:

- current trader LL below the pool stage → current pool value is 0,
- current trader LL at/above the stage → value is reconstructed from completed direct seed quests plus completed quests in that same pool.

Current profile settings already store core-trader LL values, so this model is available without adding a new user input.

Across the current 162 global-variable quests, **114 belong to LL2–LL4 pools** and are eligible for this reconstruction.

## LL1 remains conservative

The 48 LL1 pool quests do not have an equivalent public direct-LL seed batch that proves the initial write rule. The application therefore does **not** synthesize LL1 counter values from the current live feed alone.

A completed LL1 gated quest can provide a conservative lower-bound witness in diagnostic logic, but no missing LL1 current value is persisted or globally guessed.

If a future exact profile `Variables` importer provides these values, normal exact evaluation handles them automatically.

## Needed Items safety boundary

Current Quest presentation and future item cleanup do not have to use the same optimism level.

The audited task-pool reconstruction is applied to current Quest catalog presentation. `FutureNeededItemsPlanner` continues to use the conservative reachability evaluator, where missing profile-variable facts stay `IndeterminatePotential`. Therefore reducing false `확인 필요` rows cannot make a genuinely needed future item incorrectly appear safe to discard.

## Expected unresolved structure after compatibility

Before this pass, raw source-level unresolved causes after dialogue compatibility were:

- `globalVariable`: 162 quests
- availability delay: 13 quests
- total structural union: 175

With validated LL2–LL4 task-pool compatibility and no exact LL1 variable values, the remaining raw unresolved ceiling becomes:

- LL1 task-pool variables: 48 quests
- availability delay: 13 quests
- structural union: 61

This is **not** a promise that the UI will show exactly 61 `확인 필요` entries. Completed / Locked / Unavailable profile states can mask an unresolved condition, and exact imported variable values can resolve additional cases.

## Why this does not become a generic heuristic

The public feed still does not define:

```text
variable X = trader Y LLZ side-task completion count
quest Q completion increments X
```

Therefore the application does not infer future variable mappings by ObjectId order, quest naming, or similarity. New IDs remain unknown until audited or exactly observed.

This preserves the main DEC-044 principle while correcting the v0.1.8 user-visible over-conservatism for the exact current EFT 1.1 dataset.

## Verification

Automated tests lock:

- exact profile variable value precedence,
- audited LL2 reconstruction,
- future LL pool deterministic zero,
- structural drift fail-closed,
- LL1 missing-value conservatism.

The live audit used temporary GitHub Actions on `agent/global-variable-audit-v2-2026-08-17`; the temporary workflow is removed after the evidence is recorded.
