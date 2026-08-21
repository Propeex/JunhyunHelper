# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.0 RELEASE CANDIDATE — Scanner implemented, live Tarkov E2E pending`**

## 현재 공개 기준선

현재 실제 공개 stable은 **v1.0.0**입니다.

공개 v1.0.0 검증 기록:

```text
exact release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
release workflow: 32219746319 — SUCCESS
automated tests: 232 passed
asset: Junhyun-Helper-v1.0.0-win-x64.zip
bytes: 74,088,334
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
public downloaded EXE smoke: passed
```

## v1.1.0 목표

버전 정책상 v1.0.0에 실제 Scanner 사용자 기능을 추가하므로 **MINOR release v1.1.0**입니다.

```text
Desktop Version: 1.1.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.0.0 → v1.1.0 mandatory Game Content update: none
v1.0.0 → v1.1.0 user.db migration: none
```

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
- 필요 시 정확한 client screen rectangle fallback
- ON 즉시 Mini Scanner standby 표시

### `테스트 ON/OFF`

- 연결된 전체 디스플레이 실시간 캡처
- 실사용과 동일 detector/OCR/matcher pipeline
- Tarkov screenshot을 바탕화면/이미지 뷰어에 표시해 게임 없이 확인 가능
- session-only, 재실행 시 OFF
- real/test 상호 배타적

### 정확도/안전성

- 게임 메모리/DLL injection/패킷 접근 없음
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

- state/candidate/OCR/matcher 결과 기록
- 전체 screenshot/raw pixels 미저장
- 약 2MB rotation

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`

## 현재 자동 검증 기준

v1.1.0 release candidate는 다음을 통과해야 합니다.

1. Windows Release build
2. 전체 automated tests
3. Scanner geometry/catalog/matcher/persistence regression tests
4. win-x64 self-contained single-file publish
5. ProductVersion/FIRST_RUN 1.1.0 identity
6. package/dependency hygiene
7. actual published EXE startup
8. rendered existing Product UI assertions
9. rendered Scanner OFF/OFF safe-default controls
10. Main Map / Factory / MiniMap smoke
11. graceful shutdown
12. Draft release asset 재다운로드/hash/package 검증
13. public 전환 후 public asset 재검증 및 public EXE smoke

## live Tarkov 검증 정책

사용자가 2026-08-21 확정한 결정에 따라 **최신 Tarkov Borderless 인게임 E2E는 v1.1.0 공개 차단 조건이 아닙니다.**

공개 후 다음을 사용자 환경에서 `scanner.log`와 함께 검증/보정합니다.

- PrintWindow vs Borderless client-rectangle fallback
- 실제 최신 상세창 geometry threshold
- 실제 최신 한국어 title OCR
- false-positive/false-negative calibration
- 장시간 CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

문제가 발견되면 후속 PATCH에서 보정합니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / future protection / ledger |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / stable release updater |
| Scanner | **v1.1.0 실제 기능 구현 / Windows CI 검증 / live Tarkov E2E 후속** |

## 유지되는 비차단 범위

- EFT 1.0 Story Chapters는 ordinary `json.tarkov.dev/tasks` progression source 밖이며 현재 미지원
- PvE Skier LL2 task-pool drift는 exact fact가 없으면 해당 pool fail-closed
- Map donor/bridge maintenance debt는 안정성이 유지되는 동안 임의 refactor하지 않음
- code signing / installer는 현재 필수 범위 아님
- Scanner live Tarkov tuning은 v1.1.0 공개 후 로그 기반 후속 검증

## v1.1.0 공개 완료 조건

1. PR #108 final CI 성공
2. final diff/review 확인
3. main 병합
4. exact main release SHA에서 release workflow 실행
5. build/tests/publish/product smoke 재통과
6. Draft v1.1.0 ZIP/checksum 생성
7. Draft assets 재다운로드 검증
8. public/latest 전환
9. public assets 재다운로드 검증
10. public downloaded EXE smoke
11. release-only workflow 제거
12. 공식 상태 문서에 final SHA/hash/run 기록

인게임 Tarkov E2E는 12개 공개 완료 조건에 포함하지 않습니다.
