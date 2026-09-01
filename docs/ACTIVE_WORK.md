# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.15.4 Farming Guide repacking / raid-planning / equipment-upgrade hardening PATCH**

실제 레이드에서 발생하는 수납 단편화와 nested storage 표시 문제를 수정하고, 파밍 가이드가 불필요하게 `버리기`로 떨어지기 전에 합법적인 아이템 이동/재배치와 안전한 장비 업그레이드를 판단하도록 강화한다. Key tool을 포함한 source-backed 모든 내부 수납 UI는 물리적으로 viewport에 들어갈 수 있는 경우 셀이 잘리지 않아야 한다.

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

User-reported real-use requirements/regressions:

1. Key tool internal storage detail clips cells. The correction is generic for source-backed backpack/rig/specialized-container storage. Scroll only when the physical viewport cannot contain the complete surface.
2. A movable 1x1 item can fragment otherwise sufficient contiguous capacity for a 2x3 item, but v1.15.3 cannot move existing items and incorrectly falls through toward replacement/discard.
3. Clearly superior compatible equipment should be equipped instead of merely stored:
   - higher-class body armor/helmet and other objectively comparable protective slot items may replace the current same-slot item;
   - objectively better rig/backpack may replace the current carrier only when every modeled contained item is preserved legally;
   - ordinary body armor + ordinary rig may transition to a superior armored rig when the incoming armored rig has a strictly higher source-backed armor class and every current rig item fits legally after repacking;
   - armored rig -> body armor + ordinary rig is not inferred from one scanned item because the missing second item cannot be created by the advisor.

Decision order after the equipment requirement:

1. legal empty equipment target where applicable;
2. objectively proven, structurally safe equipment upgrade;
3. direct legal storage without moving existing items;
4. non-destructive legal repacking/movement of existing unlocked items, preferring low disruption;
5. value/need-based destructive replacement only after preservation options fail;
6. discard only when no preferable legal plan exists.

Equipment superiority rules are deliberately source-backed and conservative:

- Tarkov `properties.class` is preserved as `FarmingGuideItemLayout.ArmorClass` and drives strict protection-class upgrades;
- backpack/rig raw storage grids provide objective storage capacity;
- same-carrier upgrades cannot delete contents and use a dedicated deterministic packing transaction;
- ordinary body armor + rig -> armored rig uses protection-class improvement plus actual content-fit as the explicit product rule;
- headphone source data exposes several audio tuning parameters but no single authoritative total order, so no hard-coded/headset-name/price-as-performance ranking is introduced; headphones continue through the existing loot-value/need replacement path unless a future authoritative superiority contract is established.

Retained constraints:

- `F`-locked item instances and reserved cells are immovable automation constraints;
- carrier/equipment lock semantics remain unchanged;
- source-backed nested grids/filters and dedicated-container preference remain authoritative;
- moved containers preserve descendants and may not create self/descendant cycles;
- complete-equipment boundary remains closed;
- every proposed multi-move/multi-slot state remains one revision-bound pending transaction and commits only after explicit acceptance.

Additional hardening found during review:

- populated nested containers must not be destructively auto-replaced based only on the parent container value;
- a locked ancestor protects descendants from automated movement/removal;
- destructive fallback should reuse repacking after a legal low-priority leaf removal instead of assuming the incoming item must occupy that leaf's original cells.

## Completed

- confirmed v1.15.3 direct-fit root cause: `FindFirstFit` treats all existing placements as fixed and has no move/repack domain;
- confirmed workbench root cause: unconstrained child measurement + clamped outer host + later scrollbar width can crop cells;
- added Core `FarmingGuideRepackingPlanner` as bounded deterministic displacement search;
- added deterministic tests for one blocker, multiple blockers, cascading cross-surface movement, immovable locks, reserved cells and nested-cycle rejection;
- added hardened Desktop raid path for direct storage, non-destructive repacking, protected leaf replacement + repacking and discard-last behavior;
- hardened path uses top-level equipment targets only and does not traverse legacy equipment internals;
- populated nested containers are excluded from destructive auto-replacement;
- nested parent root-storage kinds are normalized after repacking;
- added source-backed `ArmorClass`, conservative `FarmingGuideEquipmentUpgradePolicy`, and bounded `FarmingGuideCarrierPackingPlanner`;
- added Desktop equipment-aware transaction path including ordinary body armor + rig -> superior armored rig with full rig-content preservation;
- workbench sizing is viewport-aware and under follow-up smoke tuning for exact scrollbar/chrome behavior;
- published-product smoke includes a 3x3/central-1x1/2x3 fragmentation scenario plus nested workbench viewport checks;
- first and second Windows Release/XAML builds on PR #267 succeeded;
- current deterministic suite before the equipment-upgrade additions: 569 passed / 0 failed / 0 skipped.

## Current step

Add deterministic importer/upgrade/carrier-packing tests and published-product armor+rig -> armored-rig smoke, then run the new PR head through Windows Release build and CI. In parallel, correct the remaining workbench smoke regression where the latest scrollbar reservation made a small 2x2 detail wider than the established compact-host contract.

## Remaining

- complete deterministic tests for armor-class import, protective upgrade, carrier dominance, carrier content repacking, locks/reserved cells and unsafe transition rejection;
- add published EXE smoke for body armor + populated rig -> superior armored rig and verify the complete pending snapshot;
- fix the remaining small-workbench oversizing smoke regression without reintroducing right/bottom clipping;
- continue broader realistic-raid review for bounded destructive/multi-item edge cases and performance;
- version v1.15.4 candidate and update PRODUCT/DECISIONS/architecture/release notes once implementation stabilizes;
- pass full Release build, deterministic tests, self-contained win-x64 publish, product UI smoke, graceful shutdown, Shutdown Race, package/checksum and Documentation Consistency on final PR head;
- merge, pass exact-main gate, publish v1.15.4, verify public tag/release/assets, then close ACTIVE_WORK to NONE.

v1.15.3 release evidence remains canonical in `docs/PROJECT_STATE.json`, `docs/RELEASE_1.15.3.md` and `docs/.release-v1.15.3-status.json` until v1.15.4 is publicly verified.
