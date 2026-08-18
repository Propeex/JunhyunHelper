# DECISIONS — 현재 유효한 장기 결정

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계**를 빠르게 복구하기 위한 active index입니다.

2026-08-09까지의 DEC-001~DEC-029 원문은 역사 보존 파일에 그대로 있습니다.

- [`DECISIONS_HISTORY_THROUGH_2026-08-09.md`](DECISIONS_HISTORY_THROUGH_2026-08-09.md)

현재 사용자 요구와 더 새로운 결정이 과거 충돌 결정보다 우선합니다. 상세 제품/기술 계약은 `PRODUCT.md`, `STATE.md`, `ARCHITECTURE.md` 및 연결된 전문 문서를 함께 읽습니다.

---

# 1. DEC-001~029 상태 인덱스

- `DEC-001` — 새 제품은 처음부터 설계한다 — **CONFIRMED**
- `DEC-002` — 기존 Tarkov-Helper는 자동으로 제품 사양이 아니다 — **CONFIRMED**, Map/MiniMap 예외는 DEC-031
- `DEC-003` — GitHub 저장소를 프로젝트 기억의 공식 기반으로 사용 — **CONFIRMED**
- `DEC-004` — 사용자는 제품 판단에 집중하고 개발 절차는 개발자가 책임 — **CONFIRMED**
- `DEC-005` — 초기 Phase 1에서는 구현보다 설계를 선행 — **PHASE-SPECIFIC / SUPERSEDED by DEC-030**
- `DEC-006` — 공식 제품명은 준현 헬퍼 — **CONFIRMED**
- `DEC-007` — 초기 상위 기능 영역 정의 — **CONFIRMED**, Scanner 실제 기능은 PRODUCT OPEN
- `DEC-008` — 구두 의도는 의미를 맞춘 뒤 공식 요구사항으로 확정 — **CONFIRMED**
- `DEC-009` — Quest 원천은 json.tarkov.dev → 내부 canonical model — **CONFIRMED**
- `DEC-010` — 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주 — **CONFIRMED**
- `DEC-011` — Quest 해금에 필요한 사용자 상태는 진행 profile에서 관리 — **CONFIRMED**
- `DEC-012` — GameMode별 진행은 독립 profile — **CONFIRMED**
- `DEC-013`~`DEC-019` — Quest/Hideout/Needed Items의 미래 필요·보수적 cleanup 의미 — **CONFIRMED**
- `DEC-020` — Inventory 자동 추정 금지의 초기 원칙 — **PARTIALLY SUPERSEDED by DEC-025/026**
- `DEC-021`~`DEC-024` — UI/navigation/Ammo source 및 표시 경계 — **CONFIRMED**
- `DEC-025` — Quest/Hideout 고정 소모 Item은 명시적 진행 조작과 함께 자동 차감 — **CONFIRMED**
- `DEC-026` — flexible hand-in 실제 소비 Item은 자동 추정하지 않음 — **CONFIRMED**
- `DEC-027` — Wiki Ballistics membership과 effectiveness는 별도 canonical fact — **CONFIRMED**
- `DEC-028` — Prestige 기본값은 0 — **CONFIRMED**
- `DEC-029` — 제품 이미지는 Game Content update 후 prefetch — **CONFIRMED**

과거 이유/대안/영향은 역사 파일의 동일 ID를 확인합니다.

---

# 2. 현재 단계 결정

## DEC-030 — 확정 기능의 수정은 직접 진행하고 새 제품 의미는 설계를 먼저 맞춘다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: 이미 확정·구현된 기능의 버그 수정, 회귀 수정, 성능 개선, 릴리즈 하드닝은 개발자가 저장소/테스트를 조사해 직접 진행한다. 새 기능이나 제품 의미 변경은 사용자 의도를 먼저 확정한다.
- supersedes: DEC-005의 현재 단계 구현 금지 문장

## DEC-031 — Map/MiniMap은 사용자가 검증한 Tarkov-Helper 기준선을 제한적으로 채택한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 제품 pin: `d933792b6042a51cea38dc44b686a096fe30de67`
- 결정: Map/MiniMap에 한해서만 donor source를 검증된 기준선으로 사용한다. old Tarkov-Helper의 updater, 숨은 명령, 데이터 규칙 등 다른 동작은 승계하지 않는다.

## DEC-032 — Map subsystem은 독립이며 Quest만 JunhyunHelper 진행 데이터와 연결한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: artwork/config/general marker/MiniMap/screenshot tracking을 독립 subsystem으로 유지하고 current Quest/Quest geometry만 제품 bridge로 연결한다.

## DEC-033 — 미구현 Scanner를 public UI에서 숨긴다

- 상태: **`SUPERSEDED by DEC-045`**
- 날짜: 2026-08-10

## DEC-034 — release/update와 product hotkey는 JunhyunHelper가 소유하며 old application behavior를 승계하지 않는다

- 상태: `CONFIRMED / UPDATED by DEC-046`
- 날짜: 2026-08-10
- 유지되는 결정: old Tarkov-Helper의 `UpdateService`, hidden commands/easter eggs, legacy hidden shortcuts/logging은 JunhyunHelper 제품 동작이 아니다. release/update와 product hotkey ownership은 JunhyunHelper에 있다.
- superseded 부분: 당시의 “program auto-update는 v0.1.0 범위가 아니다”라는 **초기 릴리즈 범위 한정 문장**은 DEC-046이 대체한다.

## DEC-035 — Windows x64 self-contained portable release를 유지한다

- 상태: `CONFIRMED / UPDATED by DEC-046`
- 날짜: 2026-08-10
- 유지되는 결정: installer 없는 Windows x64 self-contained portable ZIP, 별도 .NET 및 관리자 권한 불필요.
- superseded 부분: 당시 “application auto-updater는 v0.1.0 blocker가 아니다”는 초기 범위 설명일 뿐 현재 금지 결정이 아니다. v0.1.14 program updater는 portable 배포 계약을 유지한 채 DEC-046으로 추가한다.

## DEC-036 — Release artifact는 debug/legacy dependency를 제거하고 공급망 감사를 gate로 둔다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: PDB, legacy AutoUpdater/WebView2/GraphX/QuikGraph를 배포물에서 제외하고 NuGet vulnerability warning을 release-blocking으로 취급한다.

## DEC-037 — Map bundle update는 같은 upstream revision의 원자적 bundle 단위로만 한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: 향후 artwork/config/general-marker bundle을 갱신할 때 서로 다른 revision을 섞지 않는다.

## DEC-038 — 불완전한 Quest availability source는 추측하지 않는다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-039, DEC-043, DEC-044`
- 날짜: 2026-08-15
- 유지되는 원칙: 프로그램이 증명할 수 없는 Quest availability fact는 임의 추정하지 않는다.

## DEC-039 — 입증할 수 없는 Quest availability는 `확인 필요`로 분리한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: Core `Indeterminate`를 Application에서 optimistic Current로 바꾸지 않는다. UI에는 `확인 필요`, Map Current Quest/sidebar와 진행 중 수치에서는 제외한다. Future Needed Items는 계속 잠재 필요 Item을 보호한다.

## DEC-040 — Map floor 관계는 visibility가 아니라 presentation이다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-041`
- 날짜: 2026-08-15
- 결정: 다른 floor라는 이유만으로 marker/extract를 숨기지 않고 current/above/below relation을 표시한다.

## DEC-041 — 서로 다른 floor의 일반 marker는 X/Z가 겹쳐도 숨기지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: cross-floor near-overlap 자체는 duplicate 증거가 아니므로 visual을 유지한다. 실제 동일 물리 source duplicate라고 확인되는 경우만 정규화한다.

## DEC-042 — 층 변경은 Main Map과 MiniMap의 현재 viewport를 보존한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: Main Map은 live zoom + map-space center, MiniMap은 exact live Scale + Translate X/Y를 floor render 전후로 보존한다.

## DEC-043 — 특수 상인 접근은 upstream 조건을 보존하고 recoverable access를 별도 모델링한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: BTR 누락 gate는 `A Helping Hand = Active`, Ref는 source gate + 검증된 GameMode unlock Complete, Lightkeeper는 ordinary prerequisite와 recoverable access를 분리한다. 접근 상실은 permanent unavailable이 아니라 recoverable Locked이다.

## DEC-044 — EFT profile-variable Quest gate는 exact read-side fact를 지원하고 미관측 값은 추측하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-17
- 결정: `globalVariable`을 `variableId`, operator, required value로 canonical v7에 보존한다. exact current `ProfileVariables` 값이 있으면 권위값으로 사용하고, 없으면 제한된 audited compatibility 외에는 `Indeterminate`로 둔다.
- supersedes: DEC-038의 globalVariable 전체 unsupported 취급 부분

## DEC-045 — Scanner placeholder 탭은 제품 UI에 유지하되 실제 기능을 가장하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-18
- 결정: 상단 `스캐너` 탭을 visible 상태로 유지하고 내용은 `준비 중` placeholder로 둔다. 실제 scanner 기능은 별도 사용자 요구 전 구현하지 않는다. maintenance/refactor에서 임의 숨김/삭제하지 않는다.
- supersedes: DEC-033

## DEC-046 — 일반 실행 시 사용자 동의형 프로그램 업데이트를 제공한다

- 상태: **`CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v0.1.14`**
- 날짜: 2026-08-18
- 사용자 확정 요구:
  1. 프로그램 실행 시 최신 버전을 조회한다.
  2. 최신 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
  3. 동의하면 업데이트 후 자동 재시작한다.
- 결정:
  - source of truth는 `Propeex/JunhyunHelper` latest public stable GitHub Release
  - current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
  - 사용자 No는 현재 실행 계속 + 다음 실행 때 다시 확인
  - check/network failure는 앱 시작을 막지 않음
  - Yes 후 exact win-x64 ZIP + `SHA256SUMS.txt`를 내려받고 SHA-256/package security contract를 검증
  - 검증 전 현재 app files를 변경하지 않음
  - 실행 중 EXE 교체는 current single-file EXE의 TEMP self-copy updater mode가 수행
  - `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/`만 transaction 교체
  - 교체 실패 시 previous files rollback 및 old EXE restart 시도
  - `%LocalAppData%/JunhyunHelper` 사용자 데이터는 교체하지 않음
  - 상시 `Updater.exe`를 공개 package에 포함하지 않음
- 릴리즈 영향:
  - updater가 latest public Release를 신뢰하므로 정식 release는 **Draft asset 검증을 끝낸 뒤에만 public/latest로 전환**
  - public 전환 후에도 실제 public ZIP을 다시 다운로드해 checksum/ProductVersion/package를 재검증
- bootstrap:
  - v0.1.13에는 updater가 없으므로 v0.1.13 → v0.1.14는 한 번 수동 교체
  - v0.1.14 이후부터 후속 stable release를 프로그램 내에서 업데이트 가능
- supersedes:
  - DEC-034의 “program auto-update는 v0.1.0 범위가 아니다”라는 초기 범위 문장
  - DEC-035의 “application auto-updater는 v0.1.0 blocker가 아니다”라는 초기 범위 문장
- 상세: `docs/PROGRAM_UPDATE.md`, `docs/RELEASE_0.1.14.md`

---

# 3. 현재 결정 확인 방법

- 제품 요구사항: `docs/PRODUCT.md`
- 기술 경계: `docs/ARCHITECTURE.md`
- 현재 구현/릴리즈 상태: `docs/STATE.md`
- Program Update: `docs/PROGRAM_UPDATE.md`
- 배포: `docs/DEPLOYMENT.md`
- Quest 선행조건/특수 상인 접근: `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- Map 세부 계약: `docs/MAP_PRODUCT_REQUIREMENTS.md`
- 기존 구현 예외/참고 정책: `docs/REFERENCE_POLICY.md`
- 과거 DEC-001~029 원문: `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`
