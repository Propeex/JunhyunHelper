# DECISIONS — 현재 유효한 장기 결정

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계**를 빠르게 복구하기 위한 active index입니다.

2026-08-09까지 DEC-001~DEC-029 원문은 다음 역사 파일에 보존합니다.

- [`DECISIONS_HISTORY_THROUGH_2026-08-09.md`](DECISIONS_HISTORY_THROUGH_2026-08-09.md)

사용자의 더 새로운 확정 요구와 더 새로운 결정이 과거 충돌 결정보다 우선합니다. 상세 제품/기술 의미는 `PRODUCT.md`, `STATE.md`, `ARCHITECTURE.md` 및 전문 문서를 함께 읽습니다.

---

# 1. DEC-001~029 상태 인덱스

- `DEC-001` — 새 제품은 처음부터 설계한다 — **CONFIRMED**
- `DEC-002` — 기존 Tarkov-Helper는 자동으로 제품 사양이 아니다 — **CONFIRMED**, Map/MiniMap 예외는 DEC-031
- `DEC-003` — GitHub 저장소를 프로젝트 기억의 공식 기반으로 사용 — **CONFIRMED**
- `DEC-004` — 사용자는 제품 판단에 집중하고 개발 절차는 개발자가 책임 — **CONFIRMED**
- `DEC-005` — 초기 Phase 1에서는 구현보다 설계를 선행 — **PHASE-SPECIFIC / SUPERSEDED by DEC-030**
- `DEC-006` — 공식 제품명은 준현 헬퍼 — **CONFIRMED**
- `DEC-007` — 초기 상위 기능 영역 정의 — **CONFIRMED**, Scanner 의미는 DEC-050~052
- `DEC-008` — 구두 의도는 의미를 맞춘 뒤 공식 요구사항으로 확정 — **CONFIRMED**
- `DEC-009` — Quest 원천은 json.tarkov.dev → 내부 canonical model — **CONFIRMED**
- `DEC-010` — 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주 — **CONFIRMED**
- `DEC-011` — Quest 해금에 필요한 사용자 상태는 진행 profile에서 관리 — **CONFIRMED**
- `DEC-012` — GameMode별 진행은 독립 profile — **CONFIRMED**
- `DEC-013`~`DEC-019` — Quest/Hideout/Needed Items 미래 필요·보수적 cleanup 의미 — **CONFIRMED**
- `DEC-020` — Inventory 자동 추정 금지 초기 원칙 — **PARTIALLY SUPERSEDED by DEC-025/026**
- `DEC-021`~`DEC-024` — UI/navigation/Ammo source 및 표시 경계 — **CONFIRMED**
- `DEC-025` — 고정 소모 Item은 명시적 진행 조작과 함께 자동 차감 — **CONFIRMED**
- `DEC-026` — flexible hand-in 실제 소비 Item은 자동 추정하지 않음 — **CONFIRMED**
- `DEC-027` — Wiki Ballistics membership과 effectiveness는 별도 canonical fact — **CONFIRMED**
- `DEC-028` — Prestige 기본값은 0 — **CONFIRMED**
- `DEC-029` — 제품 이미지는 Game Content update 후 prefetch — **CONFIRMED**

---

# 2. 현재 단계 결정

## DEC-030 — 확정 기능 수정은 직접 진행하고 새 제품 의미는 설계를 먼저 맞춘다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 이미 확정·구현된 기능의 버그 수정, 회귀 수정, 성능 개선, 릴리즈 하드닝은 개발자가 저장소/테스트를 조사해 직접 진행한다.
- 새 기능이나 제품 의미 변경은 사용자 의도를 먼저 확정한다.
- supersedes: DEC-005의 현재 단계 구현 금지 문장

## DEC-031 — Map/MiniMap은 사용자가 검증한 Tarkov-Helper 기준선을 제한적으로 채택한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 제품 pin: `d933792b6042a51cea38dc44b686a096fe30de67`
- Map/MiniMap에 한해서 donor source를 기준선으로 사용한다.
- old Tarkov-Helper updater/hidden command/기타 데이터 규칙은 승계하지 않는다.

## DEC-032 — Map subsystem은 독립이며 Quest만 JunhyunHelper 진행 데이터와 연결한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- artwork/config/general marker/MiniMap/screenshot tracking은 독립 subsystem이다.
- current Quest/Quest geometry만 제품 bridge로 연결한다.

## DEC-033 — 미구현 Scanner를 public UI에서 숨긴다

- 상태: `SUPERSEDED by DEC-045`
- 날짜: 2026-08-10

## DEC-034 — release/update와 product hotkey는 JunhyunHelper가 소유한다

- 상태: `CONFIRMED / UPDATED by DEC-046`
- 날짜: 2026-08-10
- legacy updater/hidden command/easter egg/legacy hidden shortcut는 제품 동작이 아니다.

## DEC-035 — Windows x64 self-contained portable release를 유지한다

- 상태: `CONFIRMED / UPDATED by DEC-046`
- 날짜: 2026-08-10
- installer 없는 Windows x64 self-contained portable ZIP, 별도 .NET/관리자 권한 불필요.

## DEC-036 — Release artifact 공급망/오염 검사를 gate로 둔다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- PDB/legacy AutoUpdater/WebView2/GraphX/QuikGraph를 배포물에서 제외한다.
- NuGet vulnerability warning은 release-blocking이다.

## DEC-037 — Map bundle update는 같은 upstream revision의 원자적 bundle로 한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10

## DEC-038 — 불완전한 Quest availability source는 추측하지 않는다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-039/043/044`
- 날짜: 2026-08-15

## DEC-039 — 입증할 수 없는 Quest availability는 `확인 필요`로 분리한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- Core `Indeterminate`를 optimistic Current로 바꾸지 않는다.
- Future Needed Items는 잠재 필요 Item을 계속 보호한다.

## DEC-040 — Map floor 관계는 visibility가 아니라 presentation이다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-041`
- 날짜: 2026-08-15

## DEC-041 — 서로 다른 floor의 일반 marker는 X/Z가 겹쳐도 숨기지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15

## DEC-042 — 층 변경은 Main Map과 MiniMap의 현재 viewport를 보존한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15

## DEC-043 — 특수 상인 접근은 upstream 조건을 보존하고 recoverable access를 별도 모델링한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15

## DEC-044 — EFT profile-variable Quest gate는 exact read-side fact를 지원하고 미관측 값은 추측하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-17
- exact current `ProfileVariables`가 있으면 권위값으로 사용한다.
- 없으면 제한된 audited compatibility 외에는 `Indeterminate`로 둔다.

## DEC-045 — Scanner placeholder 탭은 UI에 유지하되 실제 기능을 가장하지 않는다

- 상태: `SUPERSEDED by DEC-050/051`
- 날짜: 2026-08-18
- supersedes: DEC-033

## DEC-046 — 일반 실행 시 사용자 동의형 프로그램 업데이트를 제공한다

- 상태: `CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED`
- 날짜: 2026-08-18
- source of truth: `Propeex/JunhyunHelper` latest public stable GitHub Release
- current보다 strictly newer stable만 대상
- 사용자 동의 후 exact Windows ZIP + SHA256 검증
- 검증 전 현재 app file 변경 금지
- temporary self-copy updater가 program-owned files transaction 교체
- 사용자 데이터 교체 금지
- 정식 release는 Draft asset 검증 후 public/latest 전환
- 상세: `docs/PROGRAM_UPDATE.md`, `docs/DEPLOYMENT.md`

## DEC-047 — v1.0.0은 기능 확장이 아닌 정식 안정판 승격이다

- 상태: `CONFIRMED / PUBLIC VERIFIED`
- 날짜: 2026-08-19
- public release source: `3147ad1b48c3d30df529d95b148c5c444a77d649`
- release workflow: `32219746319 — SUCCESS`
- public ZIP SHA-256: `0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c`

## DEC-048 — v1 이후 새 기능=MINOR, 기존 기능 보완=PATCH

- 상태: `CONFIRMED`
- 날짜: 2026-08-19
- 새 사용자 기능 → `MINOR + 1`, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → `PATCH + 1`
- 혼합 변경은 MINOR 우선
- 1.0.0에서 Scanner 실제 기능 추가 → 1.1.0
- 1.1.0 Scanner UI/사용성 보완 → 1.1.1
- 상세: `docs/VERSIONING.md`

## DEC-049 — Map donor는 source pin과 fetch origin을 분리한다

- 상태: `CONFIRMED / IMPLEMENTED`
- 날짜: 2026-08-19
- Map/MiniMap 제품 source identity는 gitlink commit SHA로 고정한다.
- fetch origin이 달라도 gitlink SHA가 같으면 Map source 변경으로 취급하지 않는다.

## DEC-050 — Scanner는 한국어 Tarkov 화면을 Item ID로 변환하는 독립 입력 subsystem이다

- 상태: `CONFIRMED / IMPLEMENTED / PARTIALLY SUPERSEDED by DEC-051/052`
- 날짜: 2026-08-21
- 자동 detail/title 인식 → current Korean official item name → Item ID
- Item ID 이후 기존 JunhyunHelper 데이터 사용
- false positive보다 miss 선호, confidence 부족 시 no identity
- Mini Scanner는 MiniMap과 독립
- 금지: game memory, DLL injection, packet interception, icon identity, scan-time network
- full Tarkov Item identity catalog 사용
- exact-first + conservative fuzzy + confidence/margin
- `현재 필요한 수량` = `RequiredTotal`
- Scanner 설정은 별도 atomic JSON
- 실게임 구현/릴리즈 제한 부분은 DEC-051이 supersede
- Mini Scanner click-through 부분은 DEC-052가 supersede
- 상세: `docs/SCANNER.md`

## DEC-051 — Scanner v1.1.0은 실제 구현을 공개하고 live Tarkov 검증은 로그 기반 후속으로 진행한다

- 상태: `CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v1.1.0 / LIVE E2E PENDING`
- 날짜: 2026-08-21
- `스캐너`: `EscapeFromTarkov` Borderless client-area 실시간 감지
- `PrintWindow` 우선, 필요 시 exact client rectangle screen capture fallback
- `테스트`: 모든 연결 디스플레이, 동일 pipeline
- real/test mutually exclusive, test session-only
- production Windows `ko-KR` OCR
- ON 상태 Mini Scanner standby → Item result
- `%LocalAppData%/JunhyunHelper/logs/scanner.log`에 상태/candidate/OCR/matcher metadata
- screenshot/raw pixel 미저장
- Windows build/tests/publish/rendered UI/Map smoke/Draft+Public package 검증은 release blocker
- 최신 Tarkov Borderless live E2E는 release blocker가 아님
- 공개 후 문제는 scanner.log 기반 PATCH 보정
- v1.1.0 exact release source: `ac24f7717e81cf6fa32cb2e0ade63949ed87ade5`
- public ZIP SHA-256: `8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce`
- 상세: `docs/SCANNER.md`, `docs/RELEASE_1.1.0.md`

## DEC-052 — Scanner 탭은 운용 UI와 사용자용 인식 기록을 사용하고 Mini Scanner는 항상 직접 이동 가능하다

- 상태: **`CONFIRMED / IMPLEMENTING FOR v1.1.1`**
- 날짜: 2026-08-21
- Scanner 탭의 상단 제목/상시 기능 설명문을 제거한다.
- 상단 bar 왼쪽에 `스캐너`, `테스트`; 오른쪽에 `아이템 목록 최신화`를 둔다.
- bar 아래에 7개 표시 정보 checkbox를 둔다.
- 하단에 최근 인식 기록을 둔다.
- 최근 기록은 OCR text, nearest official Item, similarity, top1/top2 margin, 성공/보류, reason을 사용자 문장으로 표시한다.
- 기존 bounded `scanner.log(.1)`에서 최근 판정을 복원해 재실행 뒤에도 확인 가능하게 한다.
- Foundation Item ID → presentation 내부 preview 경로는 유지하되 product Scanner 탭에서 숨긴다.
- 별도 위치 편집/초기화 controls는 제거한다.
- Mini Scanner는 visible 상태에서 언제든 left-drag 가능하며 drag 완료 좌표를 저장한다.
- always-drag 요구 때문에 Mini Scanner 자기 영역의 `WS_EX_TRANSPARENT` click-through는 제거한다.
- Topmost, ShowActivated=false, `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`는 유지한다.
- Mini Scanner 영역은 mouse hit-test를 받지만 게임 keyboard focus는 가져가지 않는다.
- 신규 기능이 아니라 v1.1.0 Scanner 사용성 보완이므로 DEC-048에 따라 **v1.1.1 PATCH**다.
- partially supersedes: DEC-050/051의 Mini Scanner play-mode click-through 계약
- 상세: `docs/SCANNER_UI_DECISION_2026-08-21.md`, `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`

---

# 3. 현재 결정 확인 방법

- 제품 요구사항: `docs/PRODUCT.md`
- 현재 구현/릴리즈 상태: `docs/STATE.md`, `docs/CURRENT_STATE.md`
- 기술 경계: `docs/ARCHITECTURE.md`
- 개발자 구현/참조 지도: `docs/DEVELOPER_REFERENCE.md`
- Scanner 제품/기술 계약: `docs/SCANNER.md`
- Scanner v1.1.1 UI 결정: `docs/SCANNER_UI_DECISION_2026-08-21.md`
- Scanner 검증 gate: `docs/SCANNER_TEST_PLAN.md`
- 버전 정책: `docs/VERSIONING.md`
- Program Update: `docs/PROGRAM_UPDATE.md`
- 배포: `docs/DEPLOYMENT.md`
- Quest 선행조건/특수 상인 접근: `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- Map 세부 계약: `docs/MAP_PRODUCT_REQUIREMENTS.md`
- 기존 구현 참고 정책: `docs/REFERENCE_POLICY.md`
- DEC-001~029 원문: `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`
