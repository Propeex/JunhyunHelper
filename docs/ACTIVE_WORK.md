# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

준현 헬퍼 **v1.15.1 Farming Guide real-play regression fixes**를 구현·검증·공개 릴리즈한다.

## Base

branch: `fix/v1.15.1-farming-guide-real-play-2026-09-01`  
base main: `a682bfb1b1200c2db851c606a4ea7616fce4bcc2`  
PR: **#258**

## Confirmed scope

1. Mini Scanner Farming Guide text
   - Store: `[보관할 장소]에 보관`
   - Replace stored item: `[보관할 장소]의 [기존 아이템 명]과 교체`
   - Discard: `버리기`
   - Equip: `[장착할 장소]에 장착`
   - Replace equipped/attached item: `[장착할 장소]의 [기존 아이템 명]과 교체`
   - Acceptance feedback: `반영 완료`
   - a new scan rejects the previous unaccepted instruction; never require `먼저 수락`
   - manual inventory/lock changes silently invalidate pending advice without user-facing cancellation text
   - scanned item name is not repeated in Farming Guide text

2. Lock lifetime and semantics
   - item/carrier/equipment-target locks follow the locked target and disappear when that target is removed/replaced
   - empty-cell lock remains an independent reserved-space constraint for reload magazine headroom
   - locking a rig/backpack/secure container protects the carrier itself from automated replacement/removal, but does not block automated storage inside it
   - lock visuals survive rerender/state changes
   - lock toggling must not cause visible UI stalls

3. Special slots
   - enforce Tarkov-compatible special-slot item eligibility from canonical `specialSlot` type classification
   - eligible special-slot items occupy one special slot regardless of ordinary inventory footprint
   - rendering, collision, summary, manual placement, sanitizer, and raid advisor share this rule

4. Raid advisor
   - implement equipment and nested attachment/armor-plate placement recommendations
   - support both empty-target equip and replace-equipped/attached-item recommendations
   - accepted equip actions count toward raid-acquired needed quantity exactly like stored loot

5. UI / interaction
   - move pistol/holster below eyewear
   - remove the inactive raid explanatory sentence below `레이드 시작`
   - storage hint: `R: 회전 · F: 아이템/장비/빈 칸 잠금`
   - simulated `T` scan expires instead of remaining indefinitely
   - translate raw modification slot IDs such as `mod_*` into understandable Korean labels
   - use only exact source-backed composed/preset images for changed weapon/helmet appearance; never fabricate an inaccurate composite

Canonical correction decision:

`docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`

## Completed

- recovered v1.15.0 stable state and captured user-confirmed real-play corrections
- implemented source-backed special-slot eligibility and one-slot footprint policy
- aligned persisted-state sanitizer, manual drag/drop, rendering, capacity summary, and raid placement with special-slot semantics
- added first-class Equip / ReplaceEquip pending actions
- implemented top-level equipment, carrier, recursive attachment, and armor-plate recommendation targets
- changed new-scan behavior to reject the previous unaccepted pending transaction without state mutation
- applied user-confirmed concise recommendation wording and `반영 완료`
- separated carrier-lock ownership from internal storage availability
- made target locks expire when the locked target is removed/replaced while preserving empty-cell reservations
- fixed lock-highlight rerender loss and removed full-page redraw from ordinary F lock toggles
- moved pistol below eyewear and simplified Farming Guide helper text
- added bounded 3-second lifecycle for simulated T-scan Mini Scanner presentation with real-scan race protection
- added Korean attachment/armor slot label policy
- expanded authoritative exact-assembly image lookup while preserving non-fabricated fallback presentation
- split raid session, lock, and planning code into narrow partial files
- added deterministic tests for special slots, slot labels, pending replacement, and equip transaction actions
- opened draft PR #258
- bumped Desktop/Product candidate version to 1.15.1 while keeping publicStable authority at v1.15.0 until exact-main publication
- updated FIRST_RUN_KO.txt and added docs/RELEASE_NOTES_V1.15.1.md
- analyzed the first full deterministic run: 556 passed / 2 failed / 0 skipped; both failures were stale v1.15.0 source-contract assertions rather than runtime/test failures
- rewrote those two maintenance contracts to assert the v1.15.1 carrier-lock and silent pending-invalidation behavior instead of deleting coverage
- final validated code checkpoint `c4bdce6812fdd7eb75edc9b82c7ff3cde8c76fa4`:
  - CI run `33474959447` SUCCESS
  - Shutdown Race run `33474959473` SUCCESS
  - Documentation Consistency run `33474959441` SUCCESS
  - 558 passed / 0 failed / 0 skipped
  - Release build and win-x64 publish SUCCESS
  - published ProductVersion `1.15.1+a03e7cee2076ceefd153e2f0cfbcd26b022d27dd` verified
  - Startup + rendered Product UI + full Map/Factory/MiniMap + Scanner smoke SUCCESS
  - graceful shutdown + clean portable root SUCCESS
  - candidate `Junhyun-Helper.zip`: 80,658,846 bytes; SHA-256 `13b8e534aad3af81548f3d822fbee619bb892f5696e607e11eb30c83a6ec5d44`
  - CI artifact `JunhyunHelper-win-x64`: id `9787880290`, 241,908,544 bytes, SHA-256 `21a5f4689d5ee6eda477678e11c8f6ac6d354f15a5c4680f128b67526f3fd544`
- staged canonical candidate facts: desktopVersion 1.15.1, deterministicTestCount 558; publicStable remains v1.15.0 until publication succeeds

## Current step

Only documentation/checkpoint facts changed after the fully validated code checkpoint. Obtain green checks on the final PR HEAD, then mark PR #258 ready and merge. After merge, verify the exact-main build/tests/runtime smoke and automatic v1.15.1 public release before starting the documentation-only release-closure PR.

## Remaining

- obtain green PR CI / Shutdown Race CI / Documentation Consistency on the final PR HEAD
- mark PR #258 ready and merge
- verify exact-main CI and Shutdown Race / Documentation Consistency on merged main
- verify automatic v1.15.1 release workflow, public tag/release/latest status, assets and checksums
- create and merge documentation-only v1.15.1 release closure updating README/CURRENT_STATE/STATE/PROJECT_STATE/release evidence
- close ACTIVE_WORK to NONE only after release closure documents are on exact main

External evidence after release:

- further user actual-PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
