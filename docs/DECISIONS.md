# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 현재 유효한 결정과 supersession 관계를 빠르게 복구하기 위한 active index다. 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`가 권위다.

기준일: **2026-09-04 KST**  
현재 제품 상태: **v1.17.0 PUBLIC STABLE / v1.17.1 Farming Guide removal in progress**

## 1. 장기 기본 원칙

- 사용자가 새로 확정한 제품 요구사항이 현재 구현보다 우선한다.
- `Propeex/Tarkov-Helper` 구형 프로토타입은 제품 사양 권위가 아니다.
- GitHub 저장소의 공식 문서, 실제 코드, 테스트, CI/release 상태가 프로젝트 기억이다.
- 사용자는 제품 판단에 집중하며 구현/Git/PR/CI/배포는 개발자가 책임진다.
- 새 사용자 기능은 MINOR, 기존 기능의 수정·보완은 PATCH를 기본으로 한다.
- user-visible WPF 변경은 actual published EXE runtime evidence까지 확인한다.
- 공개 stable source/tag/assets는 immutable historical identity다.

과거 DEC-001~DEC-029와 historical decisions는 `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md` 및 각 `DECISION_*` 문서에 보존한다.


## 2. Farming Guide removal authority

Current authority:

- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md` — **CONFIRMED / CURRENT**

Decision:

- Farming Guide is no longer a current product feature.
- Main navigation/page, editor/presets, raid advisor, packing/repacking, locks/weight/quantity flows, Scanner bridge/hotkey/Mini Scanner instruction row, persistence, services, domain policies, Game Content extension metadata and dedicated tests are removed.
- Existing legacy `farming-guide.json` is inert and is neither read nor written; it is not automatically deleted.
- Quest, Hideout, Items, Ammo, Map/MiniMap and independent Scanner behavior remain product features.

All earlier Farming Guide decision documents are **HISTORICAL / SUPERSEDED AS CURRENT PRODUCT AUTHORITY**. They remain only to explain older released versions.

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

Current public stable facts are canonical in `docs/PROJECT_STATE.json`. v1.17.1 is the active PATCH target removing Farming Guide; exact release evidence is recorded only after merge/exact-main/release verification.
