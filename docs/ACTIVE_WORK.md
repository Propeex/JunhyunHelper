# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.15.4 Farming Guide repacking / raid-planning / equipment-upgrade hardening PATCH**

실제 레이드에서 수납 공간이 단편화됐을 때 불필요한 파괴/버리기를 피하고, source-backed fact로 객관적 우위를 증명할 수 있는 장비는 현재 수납·잠금·내용물을 보존하는 범위에서 안전하게 업그레이드하도록 강화한다. Key tool 등 source-backed nested storage 상세창은 물리적으로 viewport에 들어갈 수 있는 경우 셀이 잘리거나 불필요한 horizontal scrollbar가 생기지 않아야 한다.

## Base / delivery

```text
public stable baseline: v1.15.3
v1.15.3 exact product source: c35204da66eb0af454b50550c830b071a0897835
work branch: fix/v1.15.4-farming-guide-repacking-hardening-2026-09-01
PR: #267 (draft until final candidate gates pass)
current desktop candidate version: 1.15.4
Game Content write schema: 11
Game Content readable schemas: 3..11
```

Public stable evidence in `docs/PROJECT_STATE.json` intentionally remains v1.15.3 until v1.15.4 tag/release/assets are publicly verified.

## Confirmed scope

- Preserve-first live raid planning: direct legal storage and non-destructive repacking must be exhausted before destructive replacement/discard.
- Source-backed nested storage and dedicated-container semantics remain authoritative.
- `F` locks, locked ancestors, carrier/equipment locks, reserved cells, filters and nested parent/descendant constraints are automation invariants.
- Objectively superior compatible top-level equipment may be equipped before ordinary storage, but superiority must be source-backed and conservative rather than inferred from price/name.
- Ordinary body armor + ordinary rig → superior armored rig is an atomic fail-closed transition that must preserve every modeled rig content item legally.
- Complete-equipment boundary stays closed: weapon/helmet attachment and armor-plate user state are not reintroduced.
- Game Content schema v11 persists the new armor/headset comparison facts while v3..v10 remain readable offline fallback.
- Nested Workbench scrollbars are physical overflow fallback only; a grid that fits the effective viewport must render without manufactured horizontal scrolling/cell clipping.

## Canonical product decision

`docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`

Core decisions:

1. Raid planning is preservation-first: empty equipment → proven safe equipment upgrade → direct storage → non-destructive repacking → destructive low-priority replacement → discard last.
2. Repacking is bounded/deterministic and may move/rotate multiple unlocked items across legal root/nested surfaces while preserving filters, reservations, locks and parent graph.
3. Market value is not equipment performance. Auto equipment upgrades require conservative source-backed superiority.
4. Protective equipment uses representative top-level `properties.class`; customized plate internals remain intentionally unmodeled by the complete-equipment product boundary.
5. Headset superiority requires `distanceModifier` no worse + `distortion` no worse + at least one strict improvement. Trade-offs are not auto-ranked.
6. Backpack/Rig upgrades require objectively superior source-backed capacity and complete legal content preservation.
7. Ordinary body armor + ordinary rig → superior armored rig is one fail-closed atomic pending transaction. Partial transitions are forbidden; reverse creation of a missing ordinary rig is not inferred.
8. Game Content v11 persists armor/headset comparison facts. Readable v3-v10 snapshots remain safe offline fallback and are opportunistically refreshed through the normal transactional Data Update boundary.
9. Nested Workbench horizontal scrolling is a physical fallback only when content genuinely exceeds effective viewport width.

## Implemented

- Core `FarmingGuideRepackingPlanner` with deterministic bounded displacement search.
- One/multiple blocker movement, rotation, cross-surface legal moves, reserved-cell/lock/filter/cycle protections.
- Populated nested containers excluded from destructive value-only replacement; locked ancestors protect descendants.
- Desktop direct-store → repack → protected destructive fallback → discard-last raid flow.
- Source-backed `ArmorClass`, `HeadsetDistanceModifier`, `HeadsetDistortion` import/runtime persistence.
- `FarmingGuideEquipmentUpgradePolicy` and `FarmingGuideCarrierPackingPlanner`.
- Safe same-slot protective/headset/carrier upgrades.
- Atomic body armor + populated ordinary rig → superior armored rig transition with full content repack/preservation and canonical sanitizer check.
- Fail-closed guard preventing illegal body armor + armored-rig state after a failed combined transition.
- Reverse armored-rig → body armor + fabricated ordinary rig transition prohibited.
- Nested Workbench viewport correction: horizontal scrolling disabled when constrained content fits, Auto only when genuinely wider.
- Game Content schema v11 with v3..v10 readable fallback.
- Opportunistic one-shot legacy-content refresh integrated through the existing `MainWindow.ProductLifecycle` lifecycle owner; no competing WPF lifecycle override.
- Deterministic tests for repacking, carrier packing, equipment/headset policy, importer equipment facts, schema v11 round-trip/refresh contract.
- Published EXE smoke for fragmented repacking, nested Workbench viewport, and body armor + populated rig → armored rig preservation/repacking.
- v1.15.4 package first-run notes and `docs/RELEASE_NOTES_V1.15.4.md` added.

## Pre-freeze validated implementation evidence

Implementation head `9b8c317cecec375cf5ddbfc67cf0207e29cdc125` passed all PR gates before the version bump:

- CI run `33498171195`: success
- Shutdown Race run `33498171241`: success
- Documentation Consistency run `33498171191`: success
- deterministic tests: **585 passed / 0 failed / 0 skipped**
- Windows Release build: success
- self-contained win-x64 publish: success
- actual published EXE Product UI / Map / Farming Guide smoke: success
- package/checksum verification: success
- graceful shutdown: success
- artifact `JunhyunHelper-win-x64`: id `9796675755`, bytes `242508051`, SHA256 `5659947d283a313988866abd3120323948c48fa8f4b3c9274ba6b9282d2dc47c`

This is implementation evidence only. Because that head still packaged version 1.15.3, it is **not** final v1.15.4 release evidence.

## Current step

The v1.15.4 candidate is frozen at product version/schema/package/release-note level. Run the resulting PR head through the complete gate again. Only a head that packages **1.15.4** and passes CI + Shutdown Race + Documentation Consistency is eligible to merge.

## Remaining

1. Confirm final candidate head has:
   - 585 deterministic tests passing with zero failures/skips;
   - Windows Release build;
   - self-contained win-x64 publish;
   - actual published EXE Product UI / Map / Farming Guide smoke;
   - fragmented repacking + Workbench viewport + armored-rig transition smoke;
   - package/checksum verification;
   - graceful shutdown;
   - Shutdown Race and Documentation Consistency.
2. Update PR #267 validation summary and mark ready.
3. Merge PR #267.
4. Verify the exact main product-source SHA through CI / Shutdown Race / Documentation Consistency.
5. Publish tag/release **v1.15.4** from that exact product source and verify release workflow, ZIP, checksum asset, sizes and SHA256.
6. Perform documentation-only release closure: update `PROJECT_STATE`, README, CURRENT_STATE, STATE, PRODUCT, DECISIONS, release evidence/status docs with public v1.15.4 facts.
7. Close this file to `Status: NONE` only after the public release is verified.
