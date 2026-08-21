# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-21

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 프로그램입니다.

핵심 기능:

- GameMode별 Profile / User Progress
- Quest availability / 진행 / 특수 상인 / profile-variable
- Hideout 진행
- Needed Items / FIR·일반 Inventory / consumption ledger
- Item 탐색 / cross-navigation
- Ammo 비교 / favorites
- Map + MiniMap
- Game Content 안전 업데이트
- 사용자 동의형 Program Update
- Scanner + Mini Scanner

Runtime GPT/AI 의존성은 없습니다.

기존 `Propeex/Tarkov-Helper`는 공식 요구사항이 아니며 Map/MiniMap의 검증된 donor source로만 제한 사용합니다.

## 2. 공개 릴리즈

현재 public stable:

```text
v1.1.1 PUBLIC RELEASE / VERIFIED
exact release source / target SHA: 1316c25d4e90509bb9286064724b778510fa9301
automated tests: 243 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.1-win-x64.zip
bytes: 80,237,511
SHA-256: db99ec44dc7ba55c6c4b238b62db41fa91fbc766e0428bbd491153a1e7d3a0e6
ProductVersion: 1.1.1+1316c25d4e90509bb9286064724b778510fa9301
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
public/latest verification run: 32458154113
```

v1.1.1은 exact source에서 Windows Release build, 243 automated tests, self-contained single-file publish, Product UI / Scanner activity / Main Map / Factory / MiniMap smoke, Draft asset 재다운로드 검증, Draft-downloaded EXE smoke, public/latest 전환, public asset 재다운로드 검증, public-downloaded EXE smoke까지 완료했습니다.

릴리즈 중 발견된 두 문제는 제품 코드가 아니라 일회성 release automation의 clean-tag exit-code 처리와 Draft metadata 조회 방식이었고, public 전환 전에 수정·재검증했습니다. 임시 v1.1.1 release/recovery/trigger workflow와 상태 marker는 공개 검증 후 제거했습니다.

## 3. 현재 개발 상태 — v1.1.1 공개 후 실사용 검증

상태:

```text
v1.1.1 PUBLIC / LATEST / VERIFIED
scope: Scanner UI / recent recognition activity / Mini Scanner direct drag
Desktop Version: 1.1.1
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
mandatory Game Content update from v1.1.0: none
user.db migration from v1.1.0: none
latest live Tarkov Borderless E2E: USER VALIDATION PENDING
```

DEC-048에 따라 기존 Scanner의 UI/사용성 개선이므로 PATCH입니다.

상세:

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/RELEASE_1.1.1.md`
- `docs/SCANNER_UI_DECISION_2026-08-21.md` — DEC-052

## 4. Scanner 핵심 파이프라인

```text
Tarkov/Display pixels
→ detail geometry detector
→ title ROI
→ Windows ko-KR OCR
→ conservative full-item matcher
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

### 실사용

```text
스캐너 ON
→ EscapeFromTarkov process/window
→ Borderless client-area
→ target PrintWindow 우선
→ 필요 시 exact client screen rectangle fallback
→ detail/title/OCR/matcher
```

### 테스트

```text
테스트 ON
→ 모든 연결 디스플레이 실시간 capture
→ 동일 detector/OCR/matcher/presentation
```

Tarkov screenshot을 바탕화면/이미지 뷰어에 표시해 게임 없이 pipeline을 확인할 수 있습니다.

real/test는 상호 배타적이고 test는 session-only입니다.

### 안전 계약

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon identity
- scan-time HTTP

오탐보다 미탐을 선호합니다. low-confidence/ambiguous match는 Item ID를 확정하지 않습니다.

`current needed`는:

```text
ItemsWorkspace.Plan.NeededItems[].RequiredTotal
```

입니다.

## 5. Scanner v1.1.1 UI

Scanner 탭은 설명서가 아니라 운용 화면입니다.

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

사용자 화면에서 제거:

- 상단 Scanner 제목/설명문
- Scanner/Test/catalog/Mini Scanner 상시 설명문
- Mini Scanner 위치 편집/초기화 controls
- Foundation preview controls

Foundation의 Item ID → presentation 내부 개발 경로는 유지합니다.

### 최근 인식 기록

각 OCR/matcher 판정을 사용자 문장으로 보여줍니다.

- 시간
- mode
- OCR text
- nearest official item
- similarity
- top1/top2 margin
- 성공/보류
- 판단 이유

기존 `scanner.log.1`과 `scanner.log`에서 bounded recent history를 복원하므로 프로그램 재실행 뒤에도 최근 판정을 확인할 수 있습니다.

개발자 로그에는 screenshot/raw pixel을 저장하지 않습니다.

## 6. Mini Scanner v1.1.1

- MiniMap과 독립 Window/service/settings/lifecycle
- ON 즉시 standby
- Item 확정 시 정보 표시
- Topmost
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- 별도 edit/reset mode 없음
- visible 상태에서 언제든 left-drag 가능
- drag 종료 위치 즉시 atomic settings 저장
- negative multi-monitor 좌표 허용

v1.1.0의 `WS_EX_TRANSPARENT` click-through는 always-drag 요구와 양립하지 않으므로 Mini Scanner 영역에 한해 v1.1.1에서 제거합니다. Mini Scanner 영역은 mouse hit-test를 받지만 게임 keyboard focus는 가져가지 않습니다.

## 7. Scanner persistence / diagnostics

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Scanner settings/catalog은 user/program update와 분리됩니다.

`scanner.log` 기록:

- mode/runtime state
- detector candidate metadata
- OCR text
- matcher success/reason/confidence
- runtime error metadata

저장 금지:

- screenshot
- raw pixel buffer

로그는 약 2MB에서 회전하고 실패가 Scanner/App fatal로 확대되지 않습니다.

## 8. Scanner 검증 정책

사전 검증:

- Korean text OCR
- detail-view detector
- full Tarkov screenshot detector
- full screenshot → detail → title ROI → OCR

v1.1.1 공개 release gate 완료:

- Windows Release build
- full automated tests — 243 passed
- existing detector/catalog/matcher regression
- ProductVersion/FIRST_RUN 1.1.1
- rendered `스캐너 OFF` / `테스트 OFF` / `아이템 목록 최신화`
- recent-recognition empty/readable-decision UI smoke
- removed Foundation/position controls absent from product UI
- self-contained win-x64 publish
- package/dependency hygiene
- actual packaged EXE startup
- existing Product UI / Main Map / Factory / MiniMap smoke
- graceful shutdown
- Draft asset re-download/hash/package/ProductVersion validation
- Draft-downloaded EXE smoke
- public/latest transition
- public re-download validation
- public-downloaded EXE smoke

### Live Tarkov

최신 Tarkov Borderless E2E는 사용자 결정에 따라 release blocker가 아니며 **사용자 실사용 검증 PENDING**입니다.

공개 후 확인:

- PrintWindow vs client-rectangle fallback
- current geometry
- current Korean OCR
- false positives/misses
- direct Mini Scanner drag와 game input coexistence
- CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`와 최근 인식 기록을 같이 보고 후속 PATCH로 보정합니다.

## 9. Program Update

일반 실행:

```text
latest public stable GitHub Release
→ strictly newer이면 사용자 Yes/No
→ Yes: exact Windows ZIP + SHA256SUMS
→ checksum/package validation
→ temporary self-copy updater
→ program-owned files transaction replace
→ new app restart
```

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트 비대상:

```text
%LocalAppData%/JunhyunHelper/user.db
content/
image-cache/
scanner/
scanner-settings.json(.bak)
map-product-settings.json(.bak)
ammo-favorites.json(.bak)
logs/
```

실패는 일반 앱 실행 실패로 확대하지 않습니다.

## 10. Game Content / User Progress

Game Content:

```text
schema: v7
readable: v3, v4, v5, v6, v7
```

online source → validation → canonical model → candidate DB → relation/read-back validation → active replacement 순서입니다. 실패 candidate는 last-known-good active content를 덮어쓰지 않습니다.

User Progress:

```text
user.db SQLite schema: v1
```

GameMode별 profile에 level/faction/edition/prestige, trader facts, completed/failed Quest, exact observed profile variables, special trader access, Hideout levels, Inventory, consumption ledgers를 저장합니다.

## 11. Quest / Needed Items 안전 규칙

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 `status[]` = OR
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- 증명 불가능 availability = `확인 필요`
- exact profile-variable 값이 있으면 권위값
- compatibility 구조 drift → fail-closed
- unresolved future Quest Item 계속 보호
- flexible hand-in 실제 소비 후보 자동 추정 금지
- fixed consumption ledger rollback 가능

## 12. Map / MiniMap

제품 pin:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- independent subsystem
- Quest current state/geometry만 JunhyunHelper bridge
- floor relation은 presentation이며 visibility filter가 아님
- cross-floor enabled marker 유지
- Main Map floor change viewport 보존
- MiniMap floor change exact Scale/Translate 보존
- product settings atomic `.bak` recovery

안정적인 donor path는 concrete defect/performance 근거 없이 broad refactor하지 않습니다.

## 13. 배포 계약

- Windows x64
- .NET 10 WPF
- portable
- self-contained
- single-file EXE
- installer 없음
- 관리자 권한 불필요
- code signing 없음

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

정식 release는 Draft-first입니다.

```text
exact release baseline
→ build/tests/publish/smoke
→ ZIP + SHA256SUMS
→ Draft Release
→ Draft asset re-download validation
→ public/latest
→ Public asset re-download validation
→ public downloaded EXE smoke
```

## 14. 현재 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / user validated baseline |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.1 public verified |
| Scanner | **v1.1.1 public verified / latest live Tarkov E2E user validation pending** |

## 15. 현재 비차단 범위

- EFT 1.0 Story Chapters는 ordinary task source 밖
- PvE Skier LL2 task-pool drift는 exact fact 없으면 해당 pool fail-closed
- code signing / installer는 현재 필수 범위 아님
- Scanner latest live Tarkov E2E는 로그 기반 후속 검증

## 16. 새 작업 시작 순서

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/DEVELOPER_REFERENCE.md`
6. `docs/ARCHITECTURE.md`
7. 관련 전문 문서와 코드/tests/PR

현재 코드가 존재한다는 이유만으로 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
