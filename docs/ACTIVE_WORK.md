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
   - a headset replaces the current headset only when source-backed listening distance is no worse, distortion is no worse, and at least one of those two facts strictly improves;
   - objectively better rig/backpack may replace the current carrier only when every modeled contained item is preserved legally;
   - ordinary body armor + ordinary rig may transition to a superior armored rig when the incoming armored rig has a strictly higher source-backed representative armor class and every current rig item fits legally after repacking;
   - armored rig -> body armor + ordinary rig is not inferred from one scanned item because the missing second item cannot be created by the advisor.

Decision order after the equipment requirement:

1. legal empty equipment target where applicable;
2. objectively proven, structurally safe equipment upgrade;
3. direct legal storage without moving existing items;
4. non-destructive legal repacking/movement of existing unlocked items, preferring low disruption;
5. value/need-based destructive replacement only after preservation options fail;
6. discard only when no preferable legal plan exists.

Equipment superiority rules are source-backed and conservative:

- Tarkov `properties.class` is preserved as `FarmingGuideItemLayout.ArmorClass` and is used as the complete-equipment model's representative top-level protection class; open plate internals remain intentionally unmodeled, so the advisor never invents a user's in-raid plate configuration;
- backpack/rig raw storage grids provide objective storage capacity;
- headset `distanceModifier` and `distortion` are preserved; only Pareto improvement is considered objectively superior and tuning trade-offs are not auto-ranked;
- same-carrier upgrades cannot delete contents and use a dedicated deterministic packing transaction;
- ordinary body armor + rig -> armored rig uses protection-class improvement plus actual content-fit as the explicit product rule;
- an incoming armored rig while body armor exists is fail-closed: the atomic combined transition must succeed or the armored rig is not reinterpreted as a normal rig replacement.

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
- destructive fallback reuses repacking after a legal low-priority leaf removal instead of assuming the incoming item must occupy that leaf's original cells;
- Game Content schema advances from v10 to v11 so armor/headset comparison facts are persisted canonically; v3-v10 stay readable as offline last-known-good snapshots, while Desktop opportunistically refreshes an older readable snapshot once active content is available and falls back safely if the network/update is unavailable.

## Completed

- confirmed v1.15.3 direct-fit root cause: `FindFirstFit` treats all existing placements as fixed and has no move/repack domain;
- added Core `FarmingGuideRepackingPlanner` as bounded deterministic displacement search;
- added deterministic tests for one blocker, multiple blockers, cascading cross-surface movement, immovable locks, reserved cells and nested-cycle rejection;
- added hardened Desktop raid path for direct storage, non-destructive repacking, protected leaf replacement + repacking and discard-last behavior;
- hardened path uses top-level equipment targets only and does not traverse legacy equipment internals;
- populated nested containers are excluded from destructive auto-replacement;
- nested parent root-storage kinds are normalized after repacking;
- added source-backed `ArmorClass`, `HeadsetDistanceModifier`, `HeadsetDistortion`, conservative `FarmingGuideEquipmentUpgradePolicy`, and bounded `FarmingGuideCarrierPackingPlanner`;
- added Desktop equipment-aware transaction path including ordinary body armor + rig -> superior armored rig with full rig-content preservation and fail-closed partial-transition guard;
- added deterministic armor/headset/carrier upgrade tests, importer equipment-fact tests, carrier-packing tests and v11 snapshot round-trip/refresh-contract tests;
- added published-product armor+populated-rig -> armored-rig smoke including content preservation, repacking, canonical sanitization and reverse-transition rejection;
- pre-v11 head passed Windows Release build, 583 deterministic tests and self-contained win-x64 publish; its only main-CI failure was the published WPF nested-workbench horizontal-scroll smoke;
- workbench follow-up now disables horizontal scrolling when content fits the effective viewport and enables Auto only when constrained measured content is genuinely wider;
- Documentation Consistency has remained green through the schema-state update.

## Current step

Run the latest head through Windows Release build/tests/published EXE smoke after the workbench and content-schema corrections. Inspect any new compiler/runtime regression, then freeze the implementation before candidate versioning.

## Remaining

- obtain green final-head CI / Shutdown Race / Documentation Consistency including published EXE Farming Guide smoke;
- confirm v11 current-schema refresh code compiles and does not interfere with product-smoke startup/offline fallback;
- continue bounded review for unsafe armored-rig transition/lock/content-fit cases without adding speculative equipment ranking;
- version v1.15.4 candidate and update PRODUCT/DECISIONS/architecture/release notes;
- pass full Release build, deterministic tests, self-contained win-x64 publish, product UI smoke, graceful shutdown, package/checksum and all PR gates on the frozen candidate head;
- merge, pass exact-main gate, publish v1.15.4, verify public tag/release/assets, then close ACTIVE_WORK to NONE.

v1.15.3 release evidence remains canonical in `docs/PROJECT_STATE.json`, `docs/RELEASE_1.15.3.md` and `docs/.release-v1.15.3-status.json` until v1.15.4 is publicly verified.
