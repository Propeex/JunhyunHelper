# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계를 빠르게 복구하기 위한 active index**다. 상세 현재 제품 상태와 release evidence는 `docs/STATE.md`가 권위다.

기준일: 2026-08-29 KST  
현재 공개 제품: **v1.10.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 장기 기본 결정

DEC-001~DEC-029 원문은 `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`에 보존한다. 이후 DEC-030~059와 standalone 결정은 아래 current authority chain 및 각 전문 문서가 권위다.

현재도 유지되는 핵심 장기 결정:

- 새 제품 요구사항이 현재 구현보다 우선하며 기존 프로토타입 코드를 사양으로 추정하지 않는다.
- GitHub 저장소의 공식 문서와 현재 코드/CI를 프로젝트 기억의 기준으로 사용한다.
- 사용자는 제품 판단에 집중하고 개발 절차는 개발자가 책임진다.
- Map/MiniMap은 pinned donor source만 제한적으로 사용하며 JunhyunHelper first-party bridge가 제품 의미를 소유한다.
- Windows x64 self-contained portable release와 checksum/오염 gate를 유지한다.
- 새 사용자 기능은 MINOR, 기존 기능 수정/보완은 PATCH를 기본으로 한다.
- Program Update는 latest stable release와 checksum을 검증한 뒤 사용자 동의로만 적용하며 사용자 mutable data를 덮어쓰지 않는다.
- Scanner는 current official Korean full-item catalog를 Item ID authority로 사용하고 false positive보다 miss를 선호한다.
- Scanner recognition acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion이 아니라 actual published EXE runtime evidence로 검증한다.

## 2. 현재 Scanner authority

현재 사용자-facing Scanner 필요 수량/출처 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

과거 DEC-050/054/057/058/059 등의 `RequiredTotal` 문구는 역사적 당시 계약이며 위 current authority로 superseded됐다.

Scanner identity proof에 price/needed/source/relationship metadata를 사용하지 않는다. OCR threshold, matcher, candidate cap, visual corroboration/recovery acceptance는 reviewed evidence 없이 조정하지 않는다.

## 3. 현재 standalone decision chain

### Product complete / maintenance mode

- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- 현재 요구사항 범위 제품은 complete를 기본 상태로 유지하되, 확인된 실사용 회귀와 사용자가 확정한 신규 요구사항은 maintenance/new-minor 작업으로 처리한다.

### Scanner storage / hotkeys / evidence

- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`
- `docs/DECISION_V1.7.9_MINI_SCANNER_SHOW_2026-08-26.md`
- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
- `docs/DECISION_V1.7.11_MAINTENANCE.md`
- durable Ground Truth는 explicit user-reviewed save만 사용하고 Scanner recognition은 actual Tarkov evidence 기반으로 유지한다.

### Long-term maintenance / UI lifecycle

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`
- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/DECISION_V1.8.3_VISIBLE_UI_RUNTIME_ACTIVATION.md`
- incidental page/class-level `Loaded` ownership보다 product-window/page explicit lifecycle ownership을 우선한다.
- descriptor/event/timer/hook subscription은 제품 수명주기 종료 시 가능한 범위에서 대칭적으로 해제한다.
- user-visible WPF lifecycle/runtime 변경은 actual published EXE runtime evidence로 검증한다.

### Scanner item database / Game Content relationships

- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`
- `docs/DECISION_V1.8.1_ITEM_RELATIONSHIP_COMPLETENESS.md`
- `docs/DECISION_V1.8.2_RUNTIME_LIVE_REGRESSIONS.md`
- `docs/DECISION_V1.8.4_AMMO_SCANNER_ITEM_DETAIL.md`
- Game Content v8 relationship graph, retained-floor/LKG/fail-closed, current live relationship compatibility와 Scanner item-detail presentation 계약을 유지한다.

### v1.9.0 Scanner Favorites / Recents

- `docs/DECISION_V1.9.0_SCANNER_FAVORITES_RECENTS_AND_UI_FIXES.md`
- canonical Item ID 기반 Favorites/Recents persistence, canonical item-open boundary, search/detail separation, current GameMode re-resolution을 유지한다.

### v1.9.1 Final UI / MiniMap synchronization

- `docs/DECISION_V1.9.1_FINAL_UI_MINIMAP.md`
- Scanner favorite/Wiki action과 Map 탈출구 UI는 **IMPLEMENTED / PUBLIC VERIFIED v1.9.1**.
- v1.9.1의 MiniMap selection-sync 구현/검증은 실사용 회귀로 불완전함이 확인됐다. `SourceInitialized`/`Loaded`와 active-window state만 검증해 donor의 hidden loaded Window 재사용 경로를 놓쳤다.
- 따라서 v1.9.1 문서의 “A→B 후 MiniMap 첫 표시가 B” 성공 주장은 historical release evidence이며 현재 MiniMap correctness authority는 v1.10.0 결정이다.

### v1.10.0 MiniMap reopen sync / Mini Scanner flea minimum

- `docs/DECISION_V1.10.0_MINIMAP_REOPEN_MINISCANNER_FLEA_MINIMUM.md`
- 상태: **IMPLEMENTED / PUBLIC VERIFIED v1.10.0**.
- donor `Hide()` → same loaded Window `Show()` 재사용 경로를 별도 제품 경계로 인정한다.
- `OverlayVisibilityChanged(true)`에서 visible Main Map selector를 synchronous하게 tracker/active MiniMap에 반영한다.
- published EXE smoke는 actual A render → hide → visible selector B → same Window show → actual `MapSvg.Source` B render를 검증한다.
- Mini Scanner의 `플리마켓 최저가`는 Scanner catalog `lastLowPrice` 기반 presentation-only 필드다.
- Scanner catalog cache는 v1~v4 readable / v4 written, Scanner display settings는 v7이다.
- price는 Item ID proof에 사용하지 않고 scan-time network I/O도 추가하지 않는다.

MiniMap runtime proof:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

### v1.10.1 stability audit / lifecycle hardening

- `docs/DECISION_V1.10.1_STABILITY_AUDIT.md`
- 상태: **IMPLEMENTED / PUBLIC VERIFIED v1.10.1**.
- MainWindow header polish의 static class-level `Loaded` handler를 제거하고 `MainWindow.OnInitialized`가 explicit initialization을 소유한다.
- `DependencyPropertyDescriptor` status watcher는 `MainWindow.OnClosed`에서 명시 해제한다.
- header의 version-only 표시와 Items cleanup 오렌지 점 사용자 의미는 변경하지 않는다.
- `DesktopStartupWiringContractTests`가 class-level handler 재유입 금지와 explicit init/cleanup ownership을 고정한다.
- 현재 실행 경로에서 사용되지 않는 v1.2.1 one-off finalization helper를 제거하되 역사적 release evidence는 보존한다.
- packaged `FIRST_RUN_KO.txt`는 현재/직전 핵심 변경만 유지하고 전체 역사 authority는 GitHub Releases/docs로 둔다.
- 저장/Program Update/Scanner/Game Content/Map의 기존 방어 계약은 실제 회귀 증거가 없어 변경하지 않았다.

Public proof:

```text
exact product release source/tag target:
c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI: 33253141127 — SUCCESS
exact-main CI: 33253293015 — SUCCESS
Release workflow: 33253438908 — SUCCESS
439 passed / 0 failed / 0 skipped
release id: 378982127
public ZIP SHA-256:
c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
```

## 4. 현재 비변경 안전 계약

- Scanner OCR threshold / matcher / candidate cap / visual recovery acceptance
- Scanner capture geometry / Ground Truth ownership
- Scanner canonical Item ID identity policy
- Game Content LKG / relationship completeness / fail-closed
- Scanner Favorites / Recents semantic contract
- Ammo filtering / favorite persistence
- Map/Factory/MiniMap 기존 기능 의미
- Map donor pin `d933792b6042a51cea38dc44b686a096fe30de67`
- Program Update stable checksum / user-consent / mutable-data preservation
- user.db schema v1 / Content v3~v8 readable compatibility

## 5. 현재 결정 확인 순서

1. `docs/PRODUCT.md`
2. `docs/CURRENT_STATE.md`
3. `docs/STATE.md`
4. `docs/DECISIONS.md`
5. 최신 작업의 `docs/DECISION_*` 문서
6. `docs/ARCHITECTURE.md`
7. `docs/DEVELOPER_REFERENCE.md`
8. `docs/MAINTENANCE_CONTRACTS.md`
9. Scanner 전문 문서
10. Map 전문 문서
11. current code / PR / CI / release state

과거 release/decision 문서의 당시 값은 historical evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision과 current canonical docs가 우선한다.
