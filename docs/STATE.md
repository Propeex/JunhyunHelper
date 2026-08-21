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
v1.1.3 PUBLIC RELEASE / VERIFIED
exact release source SHA: 8803f899341859887281ad50135911f4625a64f3
release verification run: 32470606548
automated tests: 245 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
published EXE bytes: 83,826,070
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
```

v1.1.3은 exact source에서 Windows Release build, **245 automated tests**, self-contained single-file publish, Product UI / Scanner / Main Map / Factory / MiniMap smoke, Draft asset 재다운로드 검증, Draft-downloaded EXE smoke, public/latest 전환, exact public tag 검증, public asset 재다운로드 검증, public-downloaded EXE smoke까지 완료했습니다.

릴리즈 중 발견된 오류는 제품 코드가 아니라 일회성 GitHub Actions release automation의 null 처리와 PowerShell refspec 보간 문제였습니다. 최종 v3 release workflow에서 GitHub API 기반 exact-tag 검증으로 교체해 전체 gate를 통과한 뒤 임시 release/diagnostic/dispatch workflow는 제거합니다.

상세: `docs/RELEASE_1.1.3.md`

## 3. 현재 개발 상태

```text
v1.1.3 PUBLIC / LATEST / VERIFIED
scope: Scanner Lab v3.8 recognition restoration
Desktop Version: 1.1.3
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
mandatory Game Content update from v1.1.2: none
user.db migration from v1.1.2: none
latest live Tarkov Borderless E2E: USER REVALIDATION IN PROGRESS
```

DEC-048에 따라 새 기능 추가가 아니라 Scanner 인식 회귀 복구이므로 PATCH입니다.

중요 문서:

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/RELEASE_1.1.3.md`
- `docs/DECISIONS.md` — DEC-050~DEC-053

## 4. Scanner 핵심 파이프라인 — v1.1.3

Scanner Lab v3.8에서 실제로 성공했던 recognition architecture를 복원했습니다.

```text
Tarkov / Display pixels
→ RED-X candidate generation
+
→ rectangle/edge structural fallback candidates
→ IoU deduplication
→ 최대 8개 structural candidates
→ candidate별 title ROI
→ adaptive 4x / 6x / 8x Windows ko-KR OCR
→ current official Korean full-item catalog resolver
→ 필요 시 상위 3개 candidate deep OCR
→ semantic resolution을 통과한 candidate만 실제 inspect window로 확정
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

### 핵심 설계 원칙

- geometry/structural score는 **후보 생성과 순위**에만 사용
- 가장 높은 geometry candidate 하나를 즉시 상세창으로 확정하지 않음
- current official Korean full-item catalog를 semantic validator로 사용
- 최대 8개 후보를 OCR 가능
- 1차 실패 시 상위 3개 candidate에 enlarged/high-contrast/binary/inverse deep OCR
- 개별 OCR line + 인접 두 line 결합 후보 검사
- matcher confidence / top1-top2 margin 완화 금지
- historical Scanner Lab alias production 누적 금지
- low-confidence/ambiguous 결과는 Item ID 미확정

이 원칙은 DEC-053에서 장기 설계 결정으로 고정합니다.

### 실사용

```text
스캐너 ON
→ EscapeFromTarkov process/window
→ Borderless client-area
→ target PrintWindow 우선
→ 필요 시 exact client screen rectangle fallback
→ v3.8 candidate/OCR/semantic validation
```

### 테스트

```text
테스트 ON
→ 모든 연결 디스플레이 실시간 capture
→ 동일 candidate/OCR/catalog/presentation pipeline
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

오탐보다 미탐을 선호합니다.

`current needed`는:

```text
ItemsWorkspace.Plan.NeededItems[].RequiredTotal
```

입니다.

## 5. Scanner UI

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

Foundation의 Item ID → presentation 내부 개발 경로는 유지할 수 있지만 일반 UI에는 노출하지 않습니다.

### 최근 인식 기록

각 OCR/resolver 판정을 사용자 문장으로 보여줍니다.

- 시간
- mode
- OCR text
- nearest official item
- similarity / confidence
- top1/top2 margin
- 성공/보류
- 판단 이유

`scanner.log.1`과 `scanner.log`에서 bounded recent history를 복원합니다.

## 6. Mini Scanner

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

always-drag 요구 때문에 Mini Scanner 자기 영역은 mouse hit-test를 받습니다. 게임 keyboard focus는 가져가지 않습니다.

## 7. Scanner persistence / diagnostics

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

`scanner.log`에는 다음을 기록합니다.

- mode/runtime state
- structural candidate metadata
- candidate별 OCR pass
- matcher/resolver result / confidence
- selected candidate
- runtime error metadata

저장 금지:

- screenshot
- raw pixel buffer

로그는 bounded rotation을 사용하고 기록 실패가 Scanner/App fatal로 확대되지 않습니다.

## 8. Scanner 검증 정책

v1.1.3 public release gate 완료:

- Windows Release build
- **245/245 automated tests**
- Scanner Lab v3.8 geometry regression
- `Ophthalmoscope 검안경` outer inspect/title ROI regression
- full Water screenshot central inspect/title ROI regression
- strong inner rectangle coexistence
- no RED-X rectangle fallback
- uniform frame fail-closed
- self-contained win-x64 publish
- ProductVersion/FIRST_RUN 1.1.3
- actual packaged EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft asset re-download/hash/package/ProductVersion validation
- Draft-downloaded EXE smoke
- public/latest transition
- exact public tag → release source SHA verification
- public asset re-download validation
- public-downloaded EXE smoke

### Live Tarkov

최신 Tarkov Borderless E2E는 DEC-051에 따라 release blocker가 아니며 **사용자 실사용 재검증 중**입니다.

공개 후 확인:

- current Borderless capture path
- structural candidate generation
- current title OCR
- semantic candidate selection
- false positives/misses
- CPU/memory/handle/OCR rate
- direct Mini Scanner drag와 game input coexistence
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`와 최근 인식 기록을 기준으로 후속 PATCH를 진행합니다.

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
→ Draft-downloaded EXE smoke
→ public/latest
→ exact public tag verification
→ Public asset re-download validation
→ public-downloaded EXE smoke
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
| Program Update | 구현 완료 / v1.1.3 public verified |
| Scanner | **v1.1.3 public verified / Scanner Lab v3.8 recognition restored / latest live Tarkov user revalidation ongoing** |

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
