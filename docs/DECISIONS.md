# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 현재 유효한 결정과 supersession 관계를 빠르게 복구하기 위한 active index다. 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`가 권위다.

기준일: **2026-09-01 KST**  
현재 제품 상태: **v1.15.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 장기 기본 원칙

- 사용자가 새로 확정한 제품 요구사항이 현재 구현보다 우선한다.
- `Propeex/Tarkov-Helper` 구형 프로토타입은 제품 사양 권위가 아니다.
- GitHub 저장소의 공식 문서, 실제 코드, 테스트, CI/release 상태가 프로젝트 기억이다.
- 사용자는 제품 판단에 집중하며 구현/Git/PR/CI/배포는 개발자가 책임진다.
- 새 사용자 기능은 MINOR, 기존 기능의 수정·보완은 PATCH를 기본으로 한다.
- user-visible WPF 변경은 actual published EXE runtime evidence까지 확인한다.
- 공개 stable source/tag/assets는 immutable historical identity다.

과거 DEC-001~DEC-029와 historical decisions는 `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md` 및 각 `DECISION_*` 문서에 보존한다.

## 2. Farming Guide current authority

Current documents:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

Current status:

- v1.13.3 live item interaction: **PUBLIC VERIFIED / RETAINED WHERE NOT SUPERSEDED**
- v1.14.0 recursive assembly editor: **PUBLIC HISTORICAL / USER-FACING ASSEMBLY FUNCTIONALITY SUPERSEDED BY v1.15.2**
- v1.14.0/v1.14.1 source-backed storage layout identity/signature guard: **PUBLIC VERIFIED / CURRENT**
- v1.15.0 raid-session advisor: **PUBLIC VERIFIED / BASE CONTRACT**
- v1.15.1 real-play pending/lock/special-slot corrections: **PUBLIC VERIFIED / RETAINED**
- v1.15.2 complete-equipment model: **PUBLIC VERIFIED / CURRENT SUPERSEDING EQUIPMENT CONTRACT**

### Current Farming Guide contract

- Farming Guide remains both the raid-start Loadout / Inventory Editor and live raid recommendation session.
- raid start snapshots working/preset state + locks into an isolated session; raid end discards session changes and restores baseline.
- current validated Tarkov item dimensions, supported storage grids/filters, `specialSlot` classification, equipment compatibility and conflicts remain mechanical authority.
- **equipment is opaque complete-item state**: no weapon/helmet/armor internal editor, recursive attachment picker, armor-plate editing or equipment-internal raid target.
- source attachment/armor/default-preset metadata may remain in Game Content as read-only evidence, especially for authoritative complete-item imagery, but it is not current user-editable equipment state.
- legacy persisted `Attachments` / `ArmorPlates` are readable for compatibility but current runtime normalizes them to root-only equipment state.
- top-level equipment targets remain: normal PMC equipment plus Rig / Backpack / Secure Container carrier slots.
- nested storage uses `ParentInstanceId`; only stored Backpack/Rig items expose a detail storage surface.
- nested backpack/rig detail uses real grids/filters and compact measured presentation; root carrier storage remains on the main Farming Guide page.
- generic case/container internal detail is not current Farming Guide product behavior.
- occupied storage/equipment targets are not silently overwritten outside explicit legal replacement behavior.
- authoritative complete/default-preset source imagery is preferred; unsupported combinations do not receive a fabricated composite.
- product-owned exact storage coordinates are presentation-only metadata and activate only when verified layout identity + per-grid dimension signature match current mechanics.
- Scanner owns confirmed Item ID and mapped price/needed facts; Farming Guide owns store/replace/discard/top-level equip/replace-equip decision.
- every Scanner-driven recommendation is revision-bound and requires explicit user acceptance before commit.
- a newer scan silently rejects the previous unaccepted pending transaction and creates a new one against unchanged current raid state.
- manual inventory/equipment/lock edits silently invalidate stale pending instructions.
- accepted feedback is `반영 완료`; Mini Scanner action text does not repeat the scanned item name.
- F locks constrain automation; direct user edits remain authoritative.
- carrier lock protects the carrier itself without blocking automatic ordinary storage inside it.
- reserved empty-cell lock is independent reserved capacity and persists until explicitly unlocked.
- Special Slots accept canonical `specialSlot` items only; a compatible item occupies one special slot regardless of ordinary footprint.
- accepted Store/Replace/top-level Equip/ReplaceEquip actions reduce session-local remaining Needed quantity.
- loot priority remains isolated in `FarmingGuideLootPriorityPolicy`.

### Supersession relationship

`DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md` supersedes the user-facing recursive assembly/modification portions of v1.14.0 and the v1.15.1 rule that raid equip targets include recursive attachment and armor-plate slots. It does **not** remove source-backed storage layouts, nested storage mechanics, v1.15.1 pending replacement behavior, locks, Special Slots, explicit acceptance, or top-level equipment recommendations.

`DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md` continues to supersede conflicting v1.15.0 pending/lock/special-slot behavior.

`DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md` continues to supersede only the incomplete v1.14.0 exact-layout activation guard.

## 3. Scanner

Current specialist authority includes `docs/SCANNER.md` and the scanner decision series.

Maintained rules:

- external screen pixels + OCR only;
- no game memory read, injection, process/game hook, kernel/driver access, input automation, network manipulation or anti-cheat bypass;
- current catalog is identity authority;
- false positive is worse than miss;
- price/needed/source/relationships are not recognition evidence;
- reviewed actual Tarkov evidence is required before relaxing acceptance thresholds;
- Ground Truth is explicit user-reviewed truth only;
- Needed quantity/source presentation reuses `ItemsWorkspace.Plan.NeededItems` authority.

The Farming Guide bridge is post-recognition only. Once Item ID is confirmed, Scanner presentation facts may be projected into Farming Guide. This does not make price/needed data recognition evidence and does not move decision authority into Scanner.

## 4. Quest / Needed Items

Current task-pool compatibility authority: `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`.

- exact ProfileVariable values always win;
- audited staged compatibility is bounded to known structure;
- structural drift is indeterminate/fail closed;
- compatibility inference is not persisted as exact hidden server truth;
- Future Needed Items / cleanup stay conservative and do not inherit optimistic current-Quest UI compatibility.

## 5. Kim Taeyoung PC diagnostic

Authority: `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`.

- explicit user confirmation;
- local diagnostic ZIP only;
- no auto-upload, auto-attachment or auto-send;
- optional probes are fail-soft;
- actual cause determination requires evidence from the real PC.

## 6. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper owns product integration/lifecycle. Maintained contracts include map-selection synchronization, same transform space for player position/heading, PMC/Scav/Transit marker filtering, bounded standard-marker recovery and isolated presentation-setting changes.

## 7. Hideout / Ammo / Game Content

- Hideout FIR semantics follow source `foundInRaid` and do not accept non-FIR inventory for FIR requirements.
- Ammo direct-purchase judgment requires proven current purchase availability; flea/barter/craft/higher LL/unproven quest unlock is not promoted to current direct purchase.
- Game Content candidates must pass schema/semantic/completeness/integrity validation before activation; suspicious drift fails closed and Last Known Good is preserved.
- Program/Game Content updates do not overwrite user-owned state.

## 8. Release / maintenance

Current public stable facts are canonical in `docs/PROJECT_STATE.json`.

v1.15.2 release evidence:

- `docs/RELEASE_1.15.2.md`
- `docs/.release-v1.15.2-status.json`
- `docs/RELEASE_NOTES_V1.15.2.md`

The public v1.15.2 source/tag/assets are immutable. Documentation-only follow-up commits may describe that release but are not allowed to become its product source or replace its published package.
