# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 현재 유효한 결정과 supersession 관계를 빠르게 복구하기 위한 active index다. 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`가 권위다.

기준일: **2026-09-01 KST**  
현재 제품 상태: **v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

Current status:

- v1.13.3 live item interaction: **PUBLIC VERIFIED / RETAINED**
- v1.14.0 recursive assembly / inline picker / layout identity: **PUBLIC HISTORICAL / FUNCTIONALITY RETAINED**
- v1.14.1 exact storage dimension-signature guard: **PUBLIC VERIFIED / CURRENT**
- v1.15.0 raid-session advisor: **PUBLIC VERIFIED / BASE CONTRACT**
- v1.15.1 real-play corrections: **PUBLIC VERIFIED / CURRENT SUPERSEDING CONTRACT**

Current contract:

- Farming Guide remains the raid-start Loadout / Inventory Editor and also owns the live raid recommendation session.
- raid start snapshots working/preset state + locks into an isolated session; raid end discards session changes and restores the baseline.
- current validated Tarkov item dimensions, grids, filters, `specialSlot` classification, equipment/attachment/armor slots and conflicts are mechanical authority.
- nested storage uses `ParentInstanceId` and impossible parent/grid/filter/bounds/overlap state fails closed.
- recursive assembly uses `FarmingGuideAssemblyPolicy`; WPF does not invent a second compatibility truth.
- empty actionable slots use a same-page compatible-item picker; search drag/drop, picker and raid equip planning share Core compatibility/conflict authority.
- occupied one-item slots are not silently overwritten.
- composed weapon/helmet imagery is used only when current canonical content contains an authoritative exact assembly-signature match; unsupported combinations do not receive a fabricated composite.
- product-owned exact storage coordinates are presentation-only metadata and activate only when the full verified dimension signature matches current mechanics.
- Scanner owns confirmed Item ID and mapped price/needed facts; Farming Guide owns the store/replace/discard/equip/replace-equip decision.
- every Scanner-driven recommendation is a revision-bound pending transaction and still requires explicit user acceptance before commit.
- a newer scan may silently reject the previous unaccepted pending transaction and create a new one against unchanged current raid state.
- manual inventory/equipment/lock edits silently invalidate stale pending instructions.
- accepted feedback is `반영 완료`; Mini Scanner action text does not repeat the scanned item name.
- F locks constrain automation; direct user edits remain authoritative.
- item/equipment/carrier locks protect the target itself from automated removal/replacement; removing/replacing the target expires that target lock.
- carrier lock does not block automatic use of the carrier's internal ordinary storage grids.
- reserved empty-cell lock is independent reserved capacity and persists until explicitly unlocked.
- Special Slots accept canonical `specialSlot` items only; a compatible item occupies one special slot regardless of ordinary footprint.
- equip targets include PMC equipment, legal rig/backpack/secure-container carrier slots, recursive attachment slots and replaceable armor-plate slots.
- accepted Store/Replace/Equip/ReplaceEquip actions all reduce session-local remaining Needed quantity.
- current loot priority remains isolated in `FarmingGuideLootPriorityPolicy` and may evolve without rewriting placement/session/Scanner identity code.

### Supersession relationship

`DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md` supersedes only the conflicting v1.15.0 real-play behaviors. In particular, the old interpretations that a carrier lock blocks its interior, that a pending recommendation must be accepted before a new scan, and that the raid advisor only recommends storage/replacement/discard are no longer current product contracts.

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
- browser may open Naver Mail compose after completion;
- optional probes are fail-soft;
- actual cause determination requires evidence from the real PC.

## 6. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper owns product integration/lifecycle. Maintained contracts include map-selection synchronization, same transform space for player position/heading, PMC/Scav/Transit marker filtering, bounded standard-marker recovery and isolated presentation-setting changes.

## 7. Hideout / Ammo / Game Content

Hideout:

- source `foundInRaid` semantics are preserved;
- non-FIR inventory cannot satisfy FIR requirements.

Ammo:

- same-caliber penetration and currently proven direct-purchase state drive pickup interpretation;
- flea/barter/craft/higher-LL/unproven unlocks are not current direct purchase;
- authoritative Ammo Pack relationships are preferred.

Game Content:

- candidate → semantic/schema/completeness/integrity validation → active promotion;
- Last Known Good is preserved on invalid candidates;
- unknown source semantics/structure fail closed;
- Content write schema v10, readable v3-v10;
- current `specialSlot` item classification is source-backed storage compatibility authority;
- user-owned state is a separate lifecycle.

## 8. Program Update / release

- latest public stable GitHub release is updater authority;
- update is user-consented;
- ZIP/checksum are verified;
- Release workflow consumes exact-main CI artifact;
- already-published source/tag/assets are immutable and are not replaced by a later documentation-only commit of the same assembly version.

Current public release evidence:

- `docs/RELEASE_1.15.1.md`
- `docs/.release-v1.15.1-status.json`
- `docs/RELEASE_NOTES_V1.15.1.md`

Canonical public identity is `v1.15.1` at exact product source `821def285e2b4964242b50981f6ba6245e996057`.

## 9. Maintenance mode

Product-complete maintenance is the default mode. v1.15.1 is a PATCH real-play correction release on top of the v1.15.0 Farming Guide feature expansion. Current priorities return to real user regressions, Tarkov changes, stability/reliability, performance, deterministic regression coverage and bounded technical-debt cleanup unless the user explicitly requests further product behavior.

## 10. Recovery order

1. `AGENTS.md`
2. `docs/PROJECT_STATE.json`
3. `docs/ACTIVE_WORK.md`
4. `README.md`
5. `docs/CURRENT_STATE.md`
6. `docs/STATE.md`
7. relevant decision/specialist documents
8. necessary code/tests
9. related PR/CI/Release state
