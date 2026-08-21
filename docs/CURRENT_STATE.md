# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.1 RELEASE CANDIDATE — Scanner 운용 UI/최근 인식 기록/Mini Scanner 직접 이동`**

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

v1.1.0은 Scanner 실제 기능을 처음 공개한 verified stable입니다. 최신 Tarkov Borderless live E2E는 사용자 결정대로 후속 검증입니다.

## 다음 릴리즈 — v1.1.1

DEC-048에 따라 새 Scanner 기능 추가가 아니라 기존 기능의 UI/사용성 개선이므로 PATCH입니다.

```text
Desktop Version: 1.1.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.0 → v1.1.1 mandatory Game Content update: none
v1.1.0 → v1.1.1 user.db migration: none
```

### Scanner 탭

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

제거된 사용자 UI:

- 상단 Scanner 제목/설명
- Scanner/Test/catalog/Mini Scanner 설명문
- Mini Scanner 위치 편집/초기화 controls
- Foundation verification/preview controls

Foundation Item ID → presentation 내부 경로는 개발자 진단용으로 유지합니다.

### 최근 인식 기록

각 OCR/matcher 시도를 사용자 문장으로 표시합니다.

- 시각
- Scanner/Test mode
- OCR text
- nearest official Item
- similarity
- top1/top2 margin
- 성공/보류
- 판단 이유

기존 bounded `scanner.log.1` → `scanner.log`에서 최근 판정을 복원하므로 앱 재실행 뒤에도 최근 기록을 확인할 수 있습니다.

개발자 상세 로그는 계속:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

이며 screenshot/raw pixels는 저장하지 않습니다.

### Mini Scanner

v1.1.1부터 별도 edit mode가 없습니다.

- 보이는 동안 직접 left-drag
- drag 완료 즉시 saved X/Y 갱신
- negative multi-monitor 좌표 유지
- Topmost / ShowActivated=false / `WS_EX_NOACTIVATE` 유지
- always-drag를 위해 Mini Scanner 자기 영역의 `WS_EX_TRANSPARENT` click-through는 제거

따라서 Mini Scanner 작은 표시 영역은 mouse hit-test를 받지만 게임 키보드 focus는 가져가지 않습니다.

## Scanner 핵심 파이프라인 — 변경 없음

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

- real: `EscapeFromTarkov` Borderless client-area
- test: all connected displays
- real/test mutually exclusive
- no memory/DLL injection/packet interception
- no icon identity
- no scan-time network
- current needed = `RequiredTotal`
- low confidence/ambiguity = no Item ID

## v1.1.1 release gate

- Windows Release build
- full automated tests
- ProductVersion/FIRST_RUN = 1.1.1
- existing Scanner detector/catalog/matcher regression
- rendered Scanner top bar + `아이템 목록 최신화`
- recent-recognition empty/readable decision UI smoke
- removed Foundation/position controls absent from product UI
- win-x64 self-contained publish
- package/dependency hygiene
- actual packaged EXE startup
- existing Product UI / Main Map / Factory / MiniMap smoke
- graceful shutdown
- Draft asset checksum/package/ProductVersion validation
- Draft-downloaded EXE smoke
- public/latest transition
- public asset re-download validation
- public-downloaded EXE smoke

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/RELEASE_1.1.1.md`, `docs/SCANNER_UI_DECISION_2026-08-21.md`.

## live Tarkov 검증

실제 최신 Borderless Tarkov E2E는 계속 후속 검증입니다. v1.1.1 공개 후 사용자 환경에서 다음을 확인합니다.

- capture route
- current detail geometry
- Korean OCR
- Item match confidence
- Mini Scanner direct drag와 실제 게임 입력 coexistence
- false positives / misses
- long-run resource behavior
- Alt+Tab/minimize/MiniMap coexistence

문제가 있으면 `scanner.log`와 최근 인식 기록을 함께 보고 후속 PATCH로 보정합니다.

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
| Scanner | **v1.1.1 usability candidate / core pipeline unchanged / live Tarkov E2E pending** |

## 현재 비차단 범위

- EFT 1.0 Story Chapters ordinary task source 밖
- PvE Skier LL2 task-pool drift는 exact fact 없으면 fail-closed
- code signing / installer는 현재 필수 범위 아님
- Scanner latest live Tarkov E2E는 로그 기반 후속 검증
