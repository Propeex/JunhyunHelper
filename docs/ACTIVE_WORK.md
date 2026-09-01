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

1. Equipment is an opaque complete item; weapon/helmet/armor attachment and armor-plate editing is removed.
2. Only stored backpack/rig nested storage may open an internal detail surface; root carrier storage remains on the main page.
3. Nested storage detail sizes to the actual grid footprint instead of covering the center column.
4. Equipment imagery prefers authoritative complete/default-preset source images and uses substantially more of the equipment slot.
5. Raid guidance keeps top-level equipment-slot Equip/ReplaceEquip but removes equipment-internal attachment/armor targets.

Canonical decision: `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`.

## Completed

- recovered v1.15.1 public-stable state and opened the v1.15.2 maintenance branch
- recorded the user-confirmed complete-equipment decision
- added `FarmingGuideCompleteEquipmentPolicy` runtime projection
  - attachment/armor slot definitions are removed from Farming Guide runtime content
  - unsupported generic-case internal grids are removed from Farming Guide runtime surfaces
  - backpack/rig/secure-container root storage mechanics remain source-backed
  - authoritative default-preset/source image is projected as the complete-item Farming Guide icon when available
- changed Farming Guide SetData/search to use the complete-equipment runtime catalog
- legacy saved attachment/armor state now sanitizes to root-only state because runtime items expose no internal equipment slots
- replaced equipment assembly rendering with one complete-item image; removed part tiles/composed user assembly presentation
- reduced weapon/equipment image safety margins so long weapons fill their equipment slots substantially better
- reduced the workbench to stored backpack/rig nested storage only
  - top-level equipment and root carrier workbench entry points are intentionally no-op
  - nested detail remains an interactive grid surface
  - detail host measures its actual grid and uses compact bounded width/height
  - the main storage surface stays visible behind the compact detail
- rewrote published-product Farming Guide smoke for compact nested storage and disabled equipment-internal editor behavior
- added deterministic complete-equipment policy tests and updated desktop maintenance contract tests

## Current step

Open a draft PR and run Build/Test/published runtime smoke to catch compile or stale-contract failures before version/release metadata is finalized.

## Remaining

- fix any compile/test/runtime regressions discovered by CI
- confirm no equipment-internal recommendation is reachable in the runtime planner while top-level Equip/ReplaceEquip remains
- bump v1.15.2 release metadata and release notes
- update PRODUCT/DECISIONS/ARCHITECTURE/current-state documentation for the simplified model
- PR CI / Shutdown Race / Documentation Consistency
- merge and exact-main verification
- verify public v1.15.2 release/tag/assets/checksums
- close release documentation and ACTIVE_WORK
