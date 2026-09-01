# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Current work

**v1.15.4 Farming Guide repacking / raid-planning hardening PATCH**

Branch:

`fix/v1.15.4-farming-guide-repacking-hardening-2026-09-01`

Public stable baseline:

```text
v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
main documentation-close head at work start:
53dbc640adeb988ba00dba761ea5e40388fd1453
```

## User-reported real-use problems

1. Nested storage detail/workbench can clip the right/bottom part of the actual grid. Key tool is a confirmed example. The correction must be generic for source-backed backpack/rig/specialized-container storage and must not crop cells. Scrollbars are acceptable only when the physical viewport cannot contain the complete surface.
2. Raid planning treats every existing placement as immovable during empty-space search. A small movable item can fragment otherwise sufficient contiguous capacity, causing an incoming 2x3-type item to incorrectly fall through to replacement/discard. The advisor must reason about legal item movement/repacking before destructive replacement or discard.

## Product intent confirmed in conversation

This is a maintenance/hardening pass, not an unrelated feature expansion. The goal is to make Farming Guide closer to a complete real-raid advisor by reviewing and strengthening the surrounding placement logic, including realistic fragmentation, nested storage, dedicated containers, locks, rotation, cross-surface moves and replacement fallback.

Target decision order:

1. legal empty equipment target where applicable;
2. direct legal storage without moving existing items;
3. non-destructive legal repacking/movement of existing unlocked items, preferring the least user disruption;
4. only then destructive replacement based on loot priority and minimum loss;
5. discard only when no preferable legal plan exists.

Constraints retained:

- `F`-locked item instances and reserved cells are immovable constraints for automation;
- carrier/equipment lock semantics remain unchanged;
- nested source-backed storage grids/filters are authoritative;
- compatible positive-allow-list dedicated nested storage remains preferred for matching items;
- moved storage containers must preserve legal descendant state and may not be moved into themselves/descendants;
- v1.15.2+ complete-equipment boundary remains closed; no weapon/helmet/armor internal assembly advice;
- acceptance remains explicit and revision-bound; the whole proposed movement/repacking transaction is committed only after user acceptance.

## Current analysis

Confirmed current planner behavior:

- `PlanScannedItem` first checks direct `TryFindFit` against the current placements;
- `TryFindFit` uses `FarmingGuidePlacementEngine.FindFirstFit`, which scans for the first currently free rectangle and never moves existing items;
- destructive storage fallback removes one candidate subtree at a time and retries the direct fit;
- therefore ordinary fragmentation and multi-item relocation are not represented in the planning domain;
- workbench sizing measures unconstrained content, clamps the outer host to viewport bounds and disables horizontal scrolling, which can leave the later vertical scrollbar reducing usable width and clipping grid cells; WrapPanel fallback measurement can also differ from final constrained layout.

## Planned implementation / verification

- introduce a deterministic, bounded non-destructive repacking planner that can relocate one or multiple unlocked stored items across legal root/nested surfaces while respecting filters, rotation, locks/reservations and ancestry;
- optimize first for no discard, then fewer moved item roots / lower disruption, with deterministic tie-breaking;
- keep direct placement as the fastest/preferred path;
- strengthen destructive fallback so fragmentation can be resolved before deciding an item must be lost;
- produce an actionable movement summary for the Mini Scanner instruction while committing the complete proposed snapshot on acceptance;
- make nested workbench sizing constraint-aware and guarantee full cell visibility when the viewport can physically contain the content;
- add deterministic regression tests for reported and realistic raid layouts, plus published EXE product smoke for workbench clipping and repacking behavior;
- run Release build, full deterministic tests, self-contained win-x64 publish, product UI smoke, graceful shutdown, Shutdown Race, package/checksum, PR CI, exact-main CI and release validation before v1.15.4 is closed.

## Last completed stable

v1.15.3 release evidence remains canonical in:

- `docs/PROJECT_STATE.json`
- `docs/RELEASE_1.15.3.md`
- `docs/.release-v1.15.3-status.json`

Do not replace the immutable v1.15.3 product identity while this PATCH is in progress.
