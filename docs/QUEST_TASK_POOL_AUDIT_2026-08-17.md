# EFT 1.1 trader task-pool prerequisite audit — 2026-08-17

## Status

**AUDIT COMPLETE / CURRENT-VERSION COMPATIBILITY ADOPTED**

This document records the evidence and product boundary for EFT 1.1 `globalVariable` Quest availability. DEC-044 remains the generic rule: preserve exact read-side requirements and prefer exact observed profile values. The current-version compatibility exists because treating all 162 usages as unrelated unknown facts produces a clearly over-broad `확인 필요` result.

## Current live structure

A fresh 2026-08-17 regular-mode audit found:

- 517 tasks total,
- 162 quests using `globalVariable`,
- exactly 27 unique variable IDs,
- all comparisons are `>=` small integer thresholds,
- every ID is trader-local to one of Prapor / Therapist / Skier / Peacekeeper / Mechanic / Ragman / Jaeger,
- the variables form LL1→LL4 staged groups; Ragman currently has three staged groups,
- the global-variable Quest itself has no ordinary task/trader prerequisite that could replace the variable gate.

The same 162 / 27 shape had already been observed across regular / PvE / PvP Season.

## Direct LL seed cross-check

The live feed was audited for non-global Quest batches gated directly by the same trader's loyalty level. The exact current counts are:

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

The current Quest UI may reconstruct a missing value only when the audited current-version structure remains intact:

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

## LL1 conservative boundary and pristine-zero exception

The 48 LL1 pool quests do not have an equivalent public direct-LL seed batch that proves the current counter after progression. The application therefore still does **not** reconstruct a progressed LL1 counter from completed Quest counts alone.

v0.1.10 adds one narrower fact that can be established without inventing the write rule:

- the variable belongs to the exact audited LL1 pool,
- current trader loyalty is LL1,
- the profile has **zero completed Quest for that trader**.

Only in that pristine state is the missing LL1 current value synthesized as **0**. If any Quest for that trader is completed, or trader loyalty is above LL1, this zero inference no longer applies.

A completed LL1 gated Quest can still provide a conservative lower-bound witness when evaluating its threshold, but no progressed LL1 current value is persisted or globally guessed.

If a future exact profile `Variables` importer provides these values, normal exact evaluation handles them automatically and overrides compatibility.

## Needed Items safety boundary

Current Quest presentation and future item cleanup do not use the same optimism level.

The audited task-pool reconstruction is applied to current Quest catalog presentation. `FutureNeededItemsPlanner` continues to use the conservative reachability evaluator, where missing profile-variable facts stay `IndeterminatePotential`. Therefore reducing false `확인 필요` rows cannot make a genuinely needed future item incorrectly appear safe to discard.

## Expected unresolved structure

Before task-pool compatibility, raw source-level unresolved causes after dialogue compatibility were:

- `globalVariable`: 162 quests
- availability delay: 13 quests
- total structural union: 175

With validated LL2–LL4 compatibility but before applying user-profile facts, the raw structural ceiling is:

- LL1 task-pool variables: 48 quests
- availability delay: 13 quests
- structural union: 61

This is not a promise that the UI will show 61 `확인 필요` entries. Completed / Locked / Unavailable states, exact variable values, and v0.1.10's pristine LL1 zero rule can resolve or mask additional rows for an actual profile.

## Why this does not become a generic heuristic

The public feed still does not define a generic rule such as:

```text
variable X = trader Y LLZ side-task completion count
quest Q completion increments X
```

Therefore the application does not infer future variable mappings or progressed LL1 values by ObjectId order, quest naming, or similarity. New IDs remain unknown until audited or exactly observed.

This preserves DEC-044 while using only current-version facts that can be demonstrated safely.

## Verification

Automated tests lock:

- exact profile variable value precedence,
- audited LL2 reconstruction,
- future LL pool deterministic zero,
- structural drift fail-closed,
- pristine LL1/current LL1 + zero trader completions → zero,
- any completed Quest for the trader → no pristine LL1 zero inference,
- LL2+ trader → no pristine LL1 zero inference.

v0.1.10 public verification: `docs/RELEASE_0.1.10.md`.
