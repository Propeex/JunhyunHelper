# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

준현 헬퍼 **v1.15.2 Farming Guide complete-equipment simplification**을 구현·검증·공개 릴리즈한다.

## Base

branch: `fix/v1.15.2-farming-guide-complete-equipment-2026-09-01`  
base main: `e25bf52812a4a4ffe77e69f83e1c444555a2dd70`  
public stable: `v1.15.1` / exact product source `821def285e2b4964242b50981f6ba6245e996057`

## Confirmed scope

1. Equipment becomes opaque complete items
   - remove Farming Guide weapon/helmet/armor attachment and armor-plate editing UI
   - do not expose or edit equipment-internal assembly state
   - raid advisor must not recommend attaching/replacing parts inside equipment
   - top-level equipment-slot equip/replace guidance remains
   - bags and rigs may expose only their real storage grids; nested bag-in-bag and rig-in-bag storage remains supported

2. Nested storage detail view
   - bag/rig internal view must size itself to the actual storage grids instead of covering the full storage column
   - retain normal drag/drop interaction inside nested storage

3. Complete-item imagery
   - equipment, especially weapons, must show source-backed complete-item/default-preset imagery instead of the bare receiver/action icon when such authoritative imagery exists
   - no fabricated assembly composition

4. Equipment icon scale
   - weapon/equipment imagery should visually fill its equipment slot more like Tarkov while preserving aspect ratio and clipping safely inside the slot

5. Farming Guide instruction scope
   - remove instructions that target an equipment-internal attachment/armor slot
   - keep instructions that target a top-level equipment slot such as primary weapon, pistol, helmet, armor, rig, backpack, secure container

Canonical decision will be recorded in `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`.

## Completed

- recovered v1.15.1 public-stable state and confirmed `ACTIVE_WORK` was NONE
- created maintenance branch from current main
- captured the user-confirmed complete-equipment product direction

## Current step

Inspect the Farming Guide workbench, image rendering, nested-storage sizing, persisted assembly state and raid-planning paths; then remove equipment-internal behavior without regressing nested storage.

## Remaining

- record v1.15.2 decision
- implement complete-item normalization and remove assembly UI/edit paths
- restrict workbench/detail view to storage-bearing bag/rig containers
- resize nested storage detail view to actual grid footprint
- restore authoritative complete-item/default-preset images and improve slot image scale
- remove equipment-internal Equip/ReplaceEquip planner targets while retaining top-level equipment targets
- add/update deterministic regression tests and product smoke
- bump v1.15.2 release metadata
- PR/CI/Shutdown Race/Documentation Consistency
- merge exact-main and verify public v1.15.2 release/tag/assets/checksums
- close release documentation and ACTIVE_WORK
