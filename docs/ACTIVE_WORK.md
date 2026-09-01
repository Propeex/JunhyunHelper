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

## Current step

PR CI is running. The first Documentation Consistency attempt failed because the initial ACTIVE_WORK checkpoint predated the repository-required heading template; this checkpoint now uses the required canonical sections. CI/build/test results still need to be evaluated and any code failures corrected.

## Remaining

- obtain green PR CI / Shutdown Race CI / Documentation Consistency
- correct any build or deterministic-test failures
- run/confirm Release publish and affected WPF/published EXE smoke from CI
- update release/current-state documentation and version metadata for v1.15.1
- mark PR ready and merge
- verify exact-main CI
- create and verify public v1.15.1 tag/release/assets/checksums
- close ACTIVE_WORK to NONE after release closure

External evidence after release:

- further user actual-PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
