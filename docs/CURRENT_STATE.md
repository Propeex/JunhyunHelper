# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.0 PUBLIC RELEASE / VERIFIED — Scanner live Tarkov E2E pending`**

## 현재 공개 기준선

```text
release: v1.1.0
release id: 374188781
exact release source / target SHA: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: 80,235,043
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
automated tests: 243 passed / 0 failed / 0 skipped
public downloaded EXE smoke: SUCCESS
public/latest verification run: 32452416929
```

`32452416929`의 제품/release verification 단계는 public-downloaded EXE smoke까지 모두 성공했습니다. Actions 최종 conclusion이 `failure`인 이유는 모든 release gate가 끝난 뒤 PR 코멘트를 기록하려던 비제품 bookkeeping 단계가 integration 권한 403으로 실패했기 때문입니다.

상세: `docs/RELEASE_1.1.0.md`

## 호환성

```text
Desktop Version: 1.1.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.0.0 → v1.1.0 mandatory Game Content update: none
v1.0.0 → v1.1.0 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Map preferences / Ammo favorites는 유지됩니다.

## Scanner v1.1.0

구현된 실제 흐름:

```text
Tarkov/Display pixels
→ detail geometry detector
→ title ROI
→ Windows ko-KR OCR
→ conservative full-catalog matcher
→ Item ID
→ existing JunhyunHelper data bridge
→ Mini Scanner
```

### 실사용 `스캐너 ON/OFF`

- `EscapeFromTarkov` 게임 창 탐색
- Borderless client-area 좌표 계산
- target-window `PrintWindow` 우선
- 유효 frame이 없으면 정확한 client screen rectangle fallback
- ON 즉시 Mini Scanner standby 표시

### `테스트 ON/OFF`

- 연결된 전체 디스플레이 실시간 capture
- 실사용과 동일 detector/OCR/matcher pipeline
- Tarkov screenshot을 바탕화면/이미지 뷰어에 표시해 게임 없이 확인 가능
- session-only, 재실행 시 OFF
- real/test 상호 배타적

### 정확도/안전성

- 게임 메모리/DLL injection/패킷 접근 없음
- process-internal data read 없음
- icon 기반 식별 없음
- scan 순간 network 없음
- exact-first + conservative fuzzy
- low-confidence/ambiguous는 Item ID 확정 안 함
- `현재 필요한 수량` = `RequiredTotal`

### 진단

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

- runtime state / candidate / OCR / matcher 결과 기록
- screenshot/raw pixels 미저장
- 약 2MB rotation
- logging failure nonfatal

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`

## v1.1.0 검증 완료 범위

- Windows Release build
- 243 automated tests
- Scanner geometry/catalog/matcher/persistence regression
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN identity
- package/dependency hygiene
- actual packaged EXE rendered Product UI assertions
- Scanner `스캐너 OFF` / `테스트 OFF` safe-default controls
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- Draft release asset checksum/package verification
- Draft-downloaded EXE smoke
- public/latest 전환
- public asset re-download hash/size/ProductVersion verification
- public-downloaded EXE smoke

## live Tarkov 검증 정책

사용자가 2026-08-21 확정한 결정에 따라 **최신 Tarkov Borderless 인게임 E2E는 v1.1.0 공개 차단 조건이 아니며 현재 PENDING입니다.**

공개 후 사용자 환경에서 `scanner.log`와 함께 확인/보정할 항목:

- PrintWindow vs Borderless client-rectangle fallback
- 실제 최신 상세창 geometry threshold
- 실제 최신 한국어 title OCR
- false-positive/false-negative calibration
- 장시간 CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

새 사용자 기능을 추가하지 않는 보정은 버전 규칙상 PATCH release로 처리합니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / future protection / ledger |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / public stable updater |
| Scanner | **IMPLEMENTED / v1.1.0 WINDOWS+PACKAGE VERIFIED / LIVE TARKOV E2E PENDING** |

## 유지되는 비차단 범위

- EFT 1.0 Story Chapters는 ordinary `json.tarkov.dev/tasks` progression source 밖이며 현재 미지원
- PvE Skier LL2 task-pool drift는 exact fact가 없으면 해당 pool fail-closed
- Map donor/bridge maintenance debt는 안정성이 유지되는 동안 임의 refactor하지 않음
- code signing / installer는 현재 필수 범위 아님
- Scanner live Tarkov tuning은 공개 v1.1.0에서 로그 기반 후속 검증

## 다음 작업

현재 제품 릴리즈 작업은 완료 상태입니다.

다음 Scanner 작업은 별도 기능 개발이 아니라 실제 Tarkov 실행 후 `scanner.log` 기반의 live validation입니다. 문제가 발견될 경우 capture/detector/OCR/matcher calibration을 원인별로 수정하고 회귀 검증 후 PATCH release합니다.
