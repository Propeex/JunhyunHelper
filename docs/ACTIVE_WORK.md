# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Current work

준현 헬퍼 **v1.15.1 Farming Guide real-play regression fixes**를 진행한다.

Branch:

`fix/v1.15.1-farming-guide-real-play-2026-09-01`

Base main:

`a682bfb1b1200c2db851c606a4ea7616fce4bcc2`

## User-confirmed product changes

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
   - lock visuals must survive rerender/state changes
   - lock toggling must not cause visible UI stalls

3. Special slots
   - enforce Tarkov-compatible special-slot item eligibility
   - eligible special-slot items occupy one special slot regardless of ordinary inventory footprint (for example Surv12)
   - rendering, collision, summary, manual placement, and raid advisor must use special-slot footprint semantics consistently

4. Raid advisor
   - implement equipment and nested attachment/armor-plate placement recommendations
   - support both empty-target equip and replace-equipped/attached-item recommendations
   - accepted equip actions count toward raid-acquired needed quantity exactly like stored loot

5. UI / interaction
   - move pistol/holster below eyewear
   - remove the inactive raid explanatory sentence below `레이드 시작`
   - change storage hint to `R: 회전 · F: 아이템/장비/빈 칸 잠금`
   - simulated `T` scan must follow normal Mini Scanner lifetime and not remain stuck
   - translate raw modification slot IDs such as `mod_*` into understandable Korean labels
   - investigate whether current Tarkov data/assets can render assembled weapon/helmet appearance; do not fabricate inaccurate composite imagery when source data cannot support it

## Current checkpoint

- repository state recovered from v1.15.0 stable
- user real-play findings and exact wording contract captured
- implementation analysis in progress
- no code changes completed yet

## Required verification before closure

- deterministic regression tests for pending replacement, equip/attachment recommendations, special-slot filters/footprints, and lock lifetime
- full deterministic test suite
- Release build
- published EXE / WPF smoke for affected Farming Guide and Mini Scanner flows
- PR / CI / exact-main verification
- v1.15.1 public release and asset integrity verification

## Remaining external evidence after release

- further user actual-PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
