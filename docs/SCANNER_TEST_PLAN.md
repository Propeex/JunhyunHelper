# Scanner v1.1.0 Test Plan

기준일: 2026-08-21

상태: **`RELEASE GATE DEFINED / LIVE TARKOV E2E DEFERRED`**

이 문서는 v1.1.0 공개 전에 자동/Windows 환경에서 반드시 통과할 범위와, 공개 후 실제 Tarkov에서 로그와 함께 검증할 범위를 분리합니다.

## 1. v1.1.0 공개 차단 gate

다음은 전부 성공해야 합니다.

1. Windows Release Desktop build
2. 전체 automated tests 0 failure
3. Scanner detail geometry detector regression tests
4. Scanner catalog/matcher/persistence tests
5. win-x64 self-contained single-file publish
6. published ProductVersion = 1.1.0
7. FIRST_RUN first line = v1.1.0
8. package root/dependency/PDB/nested-archive audit
9. actual published EXE startup
10. rendered existing Product UI assertions
11. Scanner `스캐너 OFF` / `테스트 OFF` safe-default rendered controls
12. Main Map / Factory / MiniMap runtime smoke
13. graceful Main Window close/process exit
14. Draft release ZIP/checksum 재다운로드 및 검증
15. Draft package root/ProductVersion/FIRST_RUN 검증
16. public/latest 전환 후 public ZIP/checksum 재다운로드 검증
17. public downloaded EXE product smoke

실제 Tarkov 게임 실행은 사용자 결정에 따라 이 공개 차단 gate에 포함하지 않습니다.

## 2. 자동 단위 테스트 — matcher

- current official name exact match
- 작은 OCR typo + 충분한 margin
- 낮은 confidence reject
- top1/top2 margin 부족 reject
- duplicate normalized official name reject
- 짧은 이름 substring 강제 선택 금지
- 과거 이름과 현재 이름이 다르면 낮은 confidence에서 강제 매칭 금지

## 3. 자동 단위 테스트 — full catalog

- 4,000개 이상 Korean catalog load
- Item ID/name/market/dimension parse
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode cache missing 시 previous mode identity 사용 금지
- zero/missing flea price → null
- invalid/missing dimension → price/slot null
- AtomicJson backup recovery

## 4. 자동 단위 테스트 — detail geometry detector

- centered canonical detail frame positive
- uniform/featureless frame negative
- display-test reduced-scale detail frame positive
- title ROI가 panel bounds 내부인지 확인

Geometry는 OCR candidate 생성 gate일 뿐 Item identity를 직접 확정하지 않습니다.

## 5. Windows build/runtime compatibility

반드시 Windows runner에서 실행:

```text
dotnet build src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj -c Release
dotnet test tests/JunhyunHelper.Tests/JunhyunHelper.Tests.csproj -c Release
```

Windows-specific compile 대상:

- Win32 process/window/client rect capture
- `PrintWindow`
- `Graphics.CopyFromScreen`
- multi-monitor enumeration
- Windows `ko-KR` OCR
- WPF `BitmapSource` handoff
- Mini Scanner overlay window styles

## 6. Scanner OFF

fresh preference에서:

- real Scanner default OFF
- display test default/session startup OFF
- capture/detector/OCR loop 없음
- context monitor 없음
- automatic catalog HTTP 없음
- Mini Scanner window 미생성
- 앱 종료 정상

## 7. real/test 모드 전환

- real ON → `TarkovWindow`
- test ON → `DisplayTest`
- test ON 시 persisted real enabled를 OFF로 전환
- real ON 시 test session flag OFF
- 두 mode 동시 loop 금지
- test mode 재실행 persistence 금지
- mode change 시 previous geometry/title/item state clear

## 8. Settings persistence

확인:

- real Scanner Enabled
- 7개 display toggle
- font size normalize
- nullable X/Y position
- NaN/Infinity reject
- negative X/Y 정상 보존
- primary corrupt + backup good recovery
- save failure가 앱 fatal로 확대되지 않음
- test mode는 persistence 대상 아님

## 9. Catalog synchronization

- 명시적 sync 성공
- enabled + stale cache에서 pre-scan sync
- healthy fresh cache에서는 불필요한 sync 없음
- network failure + healthy same-mode cache → last-known-good 유지
- network failure + requested 다른 mode → wrong-mode identity 사용 금지
- malformed response → fail closed
- item count below minimum → fail closed
- app close 중 sync 취소가 process shutdown을 막지 않음

## 10. Preview/presentation pipeline

```text
valid Item ID
→ Scanner catalog item
→ current GameContentCatalog item if present
→ ItemsWorkspace RequiredTotal
→ local icon if cached
→ Mini Scanner
```

검증:

- invalid Item ID → no fake snapshot
- current NeededItems에 없음 → current needed = 0
- displayed current needed = RequiredTotal
- missing trader/flea price → 해당 line hidden
- missing dimensions → per-slot lines hidden
- missing icon → icon only omitted
- preview/icon load가 network 요청을 만들지 않음

## 11. Overlay

Windows smoke/manual:

- transparent / no background panel
- Topmost
- taskbar 미표시
- play mode click-through/no-activate
- edit mode only drag
- position save/reset
- negative-coordinate position persistence
- display toggle 즉시 반영
- Scanner Window와 MiniMap Window 독립
- ON 직후 standby 상태가 표시되고 OFF에서만 완전히 숨김
- background callback이 WPF cross-thread exception을 만들지 않음

## 12. Runtime state machine

- geometry first hit → OCR 없음
- geometry stable >= 2 이후 title stability 시작
- title first hit → OCR 없음
- title stable >= 2 → OCR 1회
- same title 유지 → OCR 반복 없음
- different title → old Item state clear 후 재안정화
- consecutive miss → waiting state
- low-confidence match → Item 확정 금지
- failed title cooldown 동안 OCR 억제
- catalog unavailable → recognition 금지

## 13. 진단 로그

경로:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

검증:

- Scanner runtime start/stop/state 기록
- candidate bounds/signature 기록
- OCR title 기록
- matcher result/confidence 기록
- 전체 screenshot/raw pixels 미저장
- 약 2MB에서 rotation
- log I/O 실패 nonfatal

## 14. v1.1.0 공개 후 실제 Tarkov gate

아래는 **공개 후 후속 검증**입니다.

### A. Borderless window capture
- `EscapeFromTarkov` window discovery
- client rect 좌표
- PrintWindow 성공 여부
- screen-rectangle fallback 필요 여부
- DPI/멀티모니터
- minimize/Alt+Tab

### B. Current inspect detector
- inventory/stash/raid positive sample
- negative UI contexts
- 여러 해상도/UI scale
- false-positive audit

### C. Current Korean OCR
- 현재 client official Korean/mixed/English item names
- 짧은 이름
- 긴 이름
- 숫자/괄호 포함 이름

### D. Item identity
- exact/fuzzy 분포
- confidence/margin calibration
- ambiguous reject

### E. End-to-end

```text
실제 상세창
→ capture
→ detector
→ title OCR
→ Item ID
→ Mini Scanner
```

### F. Long run
- CPU
- memory
- handles
- OCR rate
- focus/input
- MiniMap coexistence

## 15. 릴리즈 판정

v1.1.0은 Scanner가 실제 구현된 정식 MINOR 릴리즈입니다.

다만 공개 시점의 정확한 표현은 다음과 같습니다.

```text
Scanner implementation: IMPLEMENTED
Windows build/CI/package: VERIFIED
offline screenshot/OCR experiments: VERIFIED
latest live Tarkov Borderless E2E: NOT YET VERIFIED
```

인게임 문제가 발견되면 release 자체를 되돌리기보다 `scanner.log`를 근거로 capture/detector/OCR threshold를 보정하고 버전 정책에 따라 후속 PATCH 릴리즈로 처리합니다.
