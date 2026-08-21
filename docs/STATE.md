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
- **Scanner + Mini Scanner**

Runtime GPT/AI 의존성은 없습니다.

기존 `Propeex/Tarkov-Helper`는 공식 요구사항이 아니며 Map/MiniMap의 검증된 donor source로만 제한 사용합니다.

## 2. 릴리즈 상태

현재 공개 stable:

```text
v1.0.0 PUBLIC VERIFIED
release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
release workflow: 32219746319 — SUCCESS
automated tests: 232 passed
asset: Junhyun-Helper-v1.0.0-win-x64.zip
bytes: 74,088,334
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
public downloaded EXE smoke: passed
```

현재 개발/다음 릴리즈:

```text
v1.1.0 RELEASE CANDIDATE
major change: actual Scanner feature
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
mandatory Game Content update from v1.0.0: none
user.db migration from v1.0.0: none
```

버전은 `docs/VERSIONING.md`에 따라 Scanner 새 사용자 기능 추가 = MINOR 증가로 **1.1.0**입니다.

## 3. Scanner v1.1.0

공식 상세 계약:

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `DEC-050`, `DEC-051`

### 실제 모드

```text
스캐너 ON
→ EscapeFromTarkov 프로세스/window 탐색
→ Borderless client-area 계산
→ target-window capture 우선
→ 필요 시 exact client screen rectangle fallback
→ detail geometry detector
→ title ROI
→ Windows ko-KR OCR
→ conservative full-item matcher
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

### 테스트 모드

```text
테스트 ON
→ 모든 연결 디스플레이 실시간 캡처
→ 동일 detail detector / OCR / matcher / presentation
```

Tarkov 전체 screenshot을 바탕화면 또는 이미지 뷰어에 표시해 게임 없이 pipeline을 확인할 수 있습니다.

real/test mode는 상호 배타적이며 test mode는 session-only입니다.

### Scanner 안전 계약

금지:

- 게임 메모리 읽기
- DLL injection
- packet interception
- game process 내부 데이터 접근
- icon 기반 identity
- scan-time HTTP

오탐보다 미탐을 선호합니다. low-confidence/ambiguous match는 실패합니다.

### Mini Scanner

- MiniMap과 독립 Window/service/settings/lifecycle
- Scanner ON 즉시 standby 표시
- Item ID 확정 시 정보 표시로 전환
- play mode click-through / no-activate / Topmost
- edit mode에서만 drag 가능

표시 가능:

- official name
- local cached icon
- trader/flea price
- trader/flea price per slot
- current needed

`current needed`는 `ItemsWorkspace.Plan.NeededItems[].RequiredTotal`입니다.

### Scanner persistence / cache

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json
%LocalAppData%/JunhyunHelper/scanner-settings.json.bak
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
```

### Scanner diagnostics

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

기록:

- mode/runtime state
- detector candidate bounds/signature
- title OCR text
- matcher success/reason/confidence
- runtime error metadata

저장하지 않음:

- screenshot
- raw pixel buffer

로그는 약 2MB에서 회전하며 실패해도 Scanner/App 동작에 영향이 없습니다.

## 4. Scanner 검증 및 릴리즈 정책

사전 실험에서 검증된 범위:

- 한국어 text OCR
- detail-view image detector
- full Tarkov screenshot detail detector
- full screenshot → detail → title ROI → OCR

v1.1.0 공개 차단 gate:

- Windows Release build
- 전체 automated tests
- Scanner detector/catalog/matcher/persistence tests
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN identity
- package/dependency hygiene
- actual published EXE launch
- rendered existing Product UI assertions
- rendered Scanner OFF/OFF controls
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- Draft asset checksum/package validation
- public asset re-download validation
- public downloaded EXE smoke

### Live Tarkov E2E

**사용자가 2026-08-21 명시적으로 v1.1.0 공개 차단 조건에서 제외했습니다.**

공개 후 실제 Borderless Tarkov에서 다음을 확인합니다.

- PrintWindow vs client-rectangle fallback
- current UI detector calibration
- current Korean title OCR
- false positive / false negative
- long-run CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`를 함께 보고 후속 PATCH로 보정합니다.

## 5. Program Update

일반 실행:

```text
latest public stable GitHub Release 조회
→ current보다 newer이면 사용자 Yes/No
→ Yes: exact Windows ZIP + SHA256SUMS download
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

## 6. Game Content / User Progress

### Game Content

```text
schema: v7
readable: v3, v4, v5, v6, v7
```

온라인 source → validation → canonical model → candidate DB → relation/read-back validation → active replacement 순서입니다.

실패 candidate는 last-known-good active content를 덮어쓰지 않습니다.

### User Progress

```text
user.db SQLite schema: v1
```

GameMode별 독립 profile에 level/faction/edition/prestige, trader facts, completed/failed Quest, exact observed profile variables, special trader access, Hideout levels, Inventory, consumption ledgers를 저장합니다.

Program Update와 Game Content Update는 `user.db`를 파괴적으로 재생성하지 않습니다.

## 7. Quest / Needed Items 핵심 안전 규칙

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 `status[]` = OR
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- 증명할 수 없는 availability = `확인 필요`
- exact profile-variable 값이 있으면 권위값
- audited compatibility 구조가 drift하면 fail-closed
- unresolved future Quest Item은 Needed Items에서 계속 보호
- flexible hand-in 실제 소비 후보는 자동 추정하지 않음
- fixed consumption은 ledger로 rollback 가능

## 8. Map / MiniMap

제품 pin:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- independent subsystem
- Quest geometry/current state만 JunhyunHelper bridge
- floor = presentation relation, visibility filter 아님
- enabled cross-floor marker 유지
- Main Map floor change → zoom + map-space center 유지
- MiniMap floor change → exact Scale + Translate frame 유지
- current Quest sidebar lane = `30px checkbox | 34px marker ID | * Quest text`
- product settings atomic `.bak` recovery

안정적인 donor path는 concrete defect/performance 근거 없이 broad refactor하지 않습니다.

## 9. 배포 계약

- Windows x64
- .NET 10 WPF
- portable
- self-contained
- single-file EXE
- installer 없음
- 관리자 권한 불필요
- 현재 code signing 없음

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

정식 release는 updater가 latest stable을 신뢰하므로 **Draft first**입니다.

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

## 10. 현재 제품 기능 상태

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
| Program Update | 구현 완료 / public verified |
| Scanner | **v1.1.0 구현 완료 / Windows release gate 검증 대상 / live Tarkov 후속** |

## 11. 현재 알려진 비차단 범위

- EFT 1.0 Story Chapters는 ordinary task source 밖이며 현재 미지원
- PvE Skier LL2 task-pool drift는 exact fact가 없으면 해당 pool만 fail-closed
- code signing / installer는 현재 필수 범위 아님
- Scanner current live Tarkov E2E는 v1.1.0 공개 후 로그 기반 검증

## 12. 새 작업을 시작할 때

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/DEVELOPER_REFERENCE.md`
6. `docs/ARCHITECTURE.md`
7. 관련 전문 문서와 코드/tests/PR

현재 코드가 존재한다는 이유만으로 공식 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
