# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.15.4 Farming Guide repacking / raid-planning hardening PATCH**

실제 레이드에서 발생하는 수납 단편화와 nested storage 표시 문제를 수정하고, 파밍 가이드가 불필요하게 `버리기`로 떨어지기 전에 합법적인 아이템 이동/재배치를 판단하도록 강화한다. Key tool을 포함한 source-backed 모든 내부 수납 UI는 물리적으로 viewport에 들어갈 수 있는 경우 셀이 잘리지 않아야 한다.

## Base

branch: `fix/v1.15.4-farming-guide-repacking-hardening-2026-09-01`

```text
public stable baseline: v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
main documentation-close head at work start:
53dbc640adeb988ba00dba761ea5e40388fd1453
draft PR: #267
```

## Confirmed scope

User-reported regressions:

1. Key tool internal storage detail clips cells. The correction is generic for source-backed backpack/rig/specialized-container storage. Scroll only when the physical viewport cannot contain the complete surface.
2. A movable 1x1 item can fragment otherwise sufficient contiguous capacity for a 2x3 item, but v1.15.3 cannot move existing items and incorrectly falls through toward replacement/discard.

Target decision order:

1. legal empty equipment target where applicable;
2. direct legal storage without moving existing items;
3. non-destructive legal repacking/movement of existing unlocked items, preferring low disruption;
4. destructive replacement only after preservation options fail;
5. discard only when no preferable legal plan exists.

Retained constraints:

- `F`-locked item instances and reserved cells are immovable automation constraints;
- carrier/equipment lock semantics remain unchanged;
- source-backed nested grids/filters and dedicated-container preference remain authoritative;
- moved containers preserve descendants and may not create self/descendant cycles;
- complete-equipment boundary remains closed;
- every proposed multi-move state remains one revision-bound pending transaction and commits only after explicit acceptance.

Additional hardening found during review:

- populated nested containers must not be destructively auto-replaced based only on the parent container value;
- a locked ancestor protects descendants from automated movement/removal;
- destructive fallback should reuse repacking after a legal low-priority leaf removal instead of assuming the incoming item must occupy that leaf's original cells.

## Completed

- confirmed v1.15.3 direct-fit root cause: `FindFirstFit` treats all existing placements as fixed and has no move/repack domain;
- confirmed workbench root cause: unconstrained child measurement + clamped outer host + later scrollbar width can crop cells;
- added Core `FarmingGuideRepackingPlanner` as bounded deterministic displacement search;
- added deterministic tests for one blocker, multiple blockers, cascading cross-surface movement, immovable locks, reserved cells and nested-cycle rejection;
- added hardened Desktop raid path:
  - empty equipment;
  - direct storage;
  - non-destructive repacking;
  - equipment replacement;
  - protected leaf replacement + repacking;
  - discard last;
- hardened path uses top-level equipment targets only and does not traverse legacy equipment internals;
- populated nested containers are excluded from destructive auto-replacement;
- nested parent root-storage kinds are normalized after repacking;
- workbench sizing is now viewport-aware, accounts for a vertical scrollbar before final width, and enables horizontal scrolling only as a physical fallback;
- published-product smoke now calls the hardened planner and includes a 3x3/central-1x1/2x3 fragmentation scenario plus nested workbench viewport checks;
- first Windows Release/XAML build on PR #267 succeeded.

## Current step

PR #267 validation is running. Documentation Consistency initially failed only because this ACTIVE_WORK file did not use the required canonical section headings; this checkpoint corrects that format. Full deterministic tests are still running on the previous head and will be rerun on this new head.

## Remaining

- inspect first deterministic-test/published-smoke results and fix any implementation/test defects;
- continue broader realistic-raid review for bounded destructive/multi-item edge cases and performance without speculative product-policy changes;
- verify nested lock/reserved-cell semantics through automatic movement;
- version v1.15.4 candidate and update PRODUCT/DECISIONS/architecture/release notes once implementation stabilizes;
- pass full Release build, deterministic tests, self-contained win-x64 publish, product UI smoke, graceful shutdown, Shutdown Race, package/checksum and Documentation Consistency on final PR head;
- merge, pass exact-main gate, publish v1.15.4, verify public tag/release/assets, then close ACTIVE_WORK to NONE.

v1.15.3 release evidence remains canonical in `docs/PROJECT_STATE.json`, `docs/RELEASE_1.15.3.md` and `docs/.release-v1.15.3-status.json` until v1.15.4 is publicly verified.
