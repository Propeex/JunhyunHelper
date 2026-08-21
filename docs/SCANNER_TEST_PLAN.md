# Scanner v1.1.1 Test Plan

기준일: 2026-08-21

상태: **`RELEASE GATE DEFINED / LIVE TARKOV E2E DEFERRED`**

이 문서는 v1.1.1 공개 전에 자동/Windows 환경에서 반드시 통과할 범위와 공개 후 실제 Tarkov에서 로그와 함께 검증할 범위를 분리합니다.

## 1. v1.1.1 공개 차단 gate

다음은 전부 성공해야 합니다.

1. Windows Release Desktop build
2. 전체 automated tests 0 failure
3. Scanner detail geometry detector/catalog/matcher regression
4. win-x64 self-contained single-file publish
5. published ProductVersion = 1.1.1
6. FIRST_RUN first line = v1.1.1
7. package root/dependency/PDB/nested-archive audit
8. actual published EXE startup
9. rendered existing Product UI assertions
10. Scanner `스캐너 OFF` / `테스트 OFF` safe-default controls
11. `아이템 목록 최신화` rendered label
12. recent-recognition empty state + readable OCR/candidate/confidence/decision sentence
13. removed position edit/reset/Foundation preview controls가 product UI에 없음
14. Main Map / Factory / MiniMap runtime smoke
15. graceful Main Window close/process exit
16. Draft release ZIP/checksum 재다운로드 및 검증
17. Draft package root/ProductVersion/FIRST_RUN 검증
18. Draft-downloaded EXE smoke
19. public/latest 전환 후 public ZIP/checksum 재다운로드 검증
20. public downloaded EXE product smoke

실제 Tarkov 게임 실행은 기존 DEC-051 정책에 따라 공개 차단 gate에 포함하지 않습니다.

## 2. 기존 인식 알고리즘 회귀

v1.1.1은 capture/OCR/matcher 알고리즘을 의도적으로 변경하지 않습니다. 다음 v1.1.0 gate가 그대로 유지되어야 합니다.

### matcher

- current official name exact match
- 작은 OCR typo + 충분한 margin
- 낮은 confidence reject
- top1/top2 margin 부족 reject
- duplicate normalized official name reject
- 짧은 이름 substring 강제 선택 금지
- 과거 이름과 현재 이름이 다르면 낮은 confidence에서 강제 매칭 금지

### full catalog

- 4,000개 이상 Korean catalog load
- Item ID/name/market/dimension parse
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode cache missing 시 previous mode identity 사용 금지
- zero/missing flea price → null
- invalid/missing dimension → price/slot null
- AtomicJson backup recovery

### detail geometry

- canonical detail frame positive
- uniform/featureless frame negative
- display-test reduced-scale detail frame positive
- title ROI가 panel bounds 내부

Geometry는 OCR candidate 생성 gate일 뿐 Item identity를 직접 확정하지 않습니다.

## 3. Windows build/runtime compatibility

Windows runner에서 확인:

```text
dotnet build src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj -c Release
dotnet test tests/JunhyunHelper.Tests/JunhyunHelper.Tests.csproj -c Release
```

Windows-specific 대상:

- Win32 process/window/client rect capture
- `PrintWindow`
- `Graphics.CopyFromScreen`
- multi-monitor enumeration
- Windows `ko-KR` OCR
- WPF `BitmapSource` handoff
- Scanner tab XAML
- Mini Scanner overlay styles/direct drag

## 4. Scanner OFF / mode 전환

fresh preference:

- real Scanner default OFF
- display test default/session startup OFF
- 둘 다 OFF면 capture/detector/OCR loop 없음
- context monitor 없음
- Mini Scanner 미생성
- 앱 종료 정상

mode:

- real ON → `TarkovWindow`
- test ON → `DisplayTest`
- test ON 시 persisted real enabled OFF
- real ON 시 test session flag OFF
- 동시에 두 loop 금지
- test mode 재실행 persistence 금지
- mode change 시 previous geometry/title/item state clear

## 5. Scanner tab v1.1.1 UI

Rendered WPF smoke에서 확인:

### 상단 bar

- 좌측 `스캐너 OFF`
- 그 오른쪽 `테스트 OFF`
- 우측 `아이템 목록 최신화`
- 버튼 사용 가능 최소 폭 유지

### 제거된 사용자 UI

다음은 Scanner product page에서 렌더링되지 않아야 합니다.

- 상단 Scanner 제목/설명문
- Scanner/Test 설명문
- catalog 설명문
- Mini Scanner 설명문
- `위치 편집`
- `위치 초기화`
- `Foundation 검증 도구`
- `Item ID 미리보기`
- `자동 미리보기`
- `미리보기 숨기기`

Foundation 내부 preview API 자체는 유지 가능하며 이 검사는 **사용자 UI 노출 여부**만 다룹니다.

### 표시 정보

기존 7개 checkbox가 유지되어야 합니다.

- item name
- item icon
- trader price
- flea price
- trader/slot
- flea/slot
- current needed

## 6. 최근 인식 기록

### empty state

Scanner 판정 이력이 없을 때:

```text
아직 인식 기록이 없습니다.
```

을 표시합니다.

### 실제 판정 projection

Runtime의 연속 이벤트:

```text
ocr-result
→ match-result
```

를 하나의 사용자 record로 묶습니다.

각 record:

- timestamp
- mode
- OCR text
- nearest official candidate
- confidence
- second score / margin
- success/hold
- reason

예시 smoke:

```text
OCR: 들격소총
candidate: 돌격소총
confidence: 94.4%
result: 식별 성공
reason: 유사도 기준 통과
```

사용자 문장에 OCR 문자열, 후보 이름, percentage가 모두 포함되어야 합니다.

### 기존 로그 복원

앱 시작 후 `scanner.log.1` → `scanner.log` 순으로 bounded history를 읽고 최근 `ocr-result` + `match-result` pair를 복원합니다.

확인:

- malformed line skip
- unknown event skip
- mode parse 실패 skip
- missing OCR에서도 matcher record 생성 가능
- 최대 recent history 수 제한
- log read 실패 nonfatal
- 새로운 판정은 즉시 UI event로 전달
- 개발자 log file I/O 실패 시에도 현재 session activity projection은 nonfatal

## 7. Mini Scanner v1.1.1 direct drag

사용자용 별도 edit/reset UI는 없습니다.

Windows manual/smoke 계약:

- transparent visual background
- Topmost
- taskbar 미표시
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- `WS_EX_TRANSPARENT` 제거
- visible 상태에서 left mouse drag 가능
- drag 종료 위치 저장
- 재표시/재실행 시 saved position 사용
- negative multi-monitor 좌표 정상
- Scanner Window와 MiniMap Window 독립
- ON 직후 standby, OFF에서 숨김
- background callback WPF cross-thread exception 없음

항상 drag 가능 요구 때문에 Mini Scanner 자기 영역은 mouse hit-test를 받습니다. 게임 키보드 focus는 가져가지 않아야 합니다.

## 8. Settings / catalog synchronization

Settings:

- real Scanner Enabled
- 7 display toggles
- nullable X/Y
- NaN/Infinity reject
- negative X/Y preserve
- primary corrupt + backup good recovery
- save failure nonfatal
- test mode session-only

Catalog:

- `아이템 목록 최신화` 명시적 sync 성공
- enabled + stale cache pre-scan sync
- healthy fresh cache 불필요 sync 없음
- network failure + healthy same-mode cache → LKG 유지
- wrong-mode identity 금지
- malformed/too-small response fail closed
- close 중 cancellation이 shutdown을 막지 않음

## 9. Presentation

```text
valid Item ID
→ Scanner catalog
→ GameContentCatalog
→ ItemsWorkspace RequiredTotal
→ local icon if cached
→ Mini Scanner
```

- invalid Item ID → fake snapshot 없음
- NeededItems에 없음 → current needed = 0
- current needed = `RequiredTotal`
- missing price line hidden
- invalid dimensions → per-slot hidden
- missing icon → icon만 omit
- presentation/icon load network 없음

## 10. Runtime state machine

- geometry first hit → OCR 없음
- geometry stable >=2 이후 title stability
- title first hit → OCR 없음
- title stable >=2 → OCR 1회
- same successful title → 반복 OCR 없음
- different title → old Item clear 후 재안정화
- consecutive miss → waiting
- low-confidence → Item 확정 금지
- failed-title cooldown
- catalog unavailable → recognition 금지

## 11. 개발자 진단 로그

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

- runtime state
- candidate bounds/signature
- OCR title
- matcher result/confidence
- screenshot/raw pixels 미저장
- ~2MB rotation
- log I/O failure nonfatal

사용자 최근 기록과 개발자 log는 역할이 다릅니다. 사용자 UI는 low-level capture metadata를 직접 노출하지 않습니다.

## 12. 공개 후 실제 Tarkov gate

### A. Borderless window capture
- window discovery
- client rect
- PrintWindow vs screen fallback
- DPI/multi-monitor
- minimize/Alt+Tab

### B. Current inspect detector
- inventory/stash/raid positive
- negative contexts
- 해상도/UI scale
- false-positive audit

### C. Current Korean OCR
- Korean/mixed/English official names
- short/long names
- number/parentheses

### D. Identity
- exact/fuzzy distribution
- confidence/margin calibration
- ambiguous reject

### E. E2E

```text
실제 상세창 → capture → detector → OCR → Item ID → Mini Scanner
```

### F. Mini Scanner/input coexistence
- direct drag
- game focus 유지
- inventory interaction과 overlay hit-test 영향
- MiniMap coexistence

### G. Long run
- CPU
- memory
- handles
- OCR rate
- Alt+Tab/minimize

## 13. 릴리즈 판정

v1.1.1은 Scanner의 기존 기능을 줄이지 않고 운용 UI/진단 가독성/위치 조작을 보완하는 PATCH입니다.

공개 상태 표현:

```text
Scanner implementation: IMPLEMENTED
v1.1.1 Windows build/package/UI: VERIFIED after gates
offline screenshot/OCR experiments: VERIFIED
latest live Tarkov Borderless E2E: PENDING
```

인게임 문제가 발견되면 `scanner.log`와 최근 인식 기록을 기준으로 후속 PATCH로 보정합니다.
