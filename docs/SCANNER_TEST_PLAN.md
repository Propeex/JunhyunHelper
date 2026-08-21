# Scanner Foundation Test Plan

기준일: 2026-08-21

이 문서는 실제 Tarkov가 없는 상태에서 검증 가능한 Scanner 범위와, 이후 live Tarkov에서 반드시 통과해야 하는 gate를 분리합니다.

## 1. 자동 단위 테스트

### Matcher

- 현재 공식 이름 exact match
- 작은 OCR typo + 충분한 margin
- 낮은 confidence reject
- 1위/2위 margin 부족 reject
- duplicate normalized official name reject
- 짧은 이름 substring 강제 선택 금지
- 구버전 `Water 0.6L 물병` → 현재 물병 강제 매칭 금지

### Full catalog

- 4,000개 이상 Korean catalog load
- Item ID/name/market/dimension parse
- `regular`
- `pve`
- `pvp-season`
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode cache missing 시 previous mode identity clear
- zero/missing flea price → null
- invalid/missing dimension → price/slot null
- AtomicJson backup recovery

## 2. Windows build/CI

반드시 Windows runner에서:

```text
dotnet build src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj -c Release
dotnet test tests/JunhyunHelper.Tests/JunhyunHelper.Tests.csproj -c Release
```

기존 CI의 다음 gate도 유지합니다.

- release publish
- actual WPF startup smoke
- existing Product UI assertions
- Map/Factory/MiniMap smoke
- graceful Main Window close
- package pollution checks

Scanner 추가가 기존 안정 기능을 깨면 Foundation 실패입니다.

2026-08-21 Foundation integration 검증 기록:

- Windows Release build: 성공
- automated tests: **240 passed / 0 failed / 0 skipped**
- win-x64 self-contained single-file publish: 성공
- published EXE Product UI + Main Map + Factory + MiniMap smoke: 성공
- graceful shutdown + clean portable root: 성공

이는 **게임 없는 Foundation 통합 gate**의 성공 기록이며, 아래 live Tarkov gate를 대체하지 않습니다.

## 3. Scanner OFF

fresh preference에서:

- default OFF
- detector/OCR loop 없음
- context monitor 없음
- automatic catalog HTTP 없음
- overlay window 미생성
- 앱 종료 정상

## 4. Settings persistence

확인:

- Enabled
- 7개 display toggle
- font size normalize
- X/Y position
- X/Y 중 하나만 존재하면 unset으로 normalize
- NaN/Infinity reject
- negative X/Y 정상 보존
- primary corrupt + backup good recovery
- save failure가 앱 fatal로 확대되지 않음

## 5. Catalog synchronization

- 명시적 sync 성공
- enabled + stale cache에서 pre-scan sync
- healthy fresh cache에서는 불필요한 sync 없음
- network failure + healthy same-mode cache → last-known-good 유지
- network failure + requested 다른 mode → wrong-mode identity 사용 금지
- malformed response → fail closed
- item count below minimum → fail closed
- app close 중 sync 취소가 process shutdown을 막지 않음

## 6. Preview pipeline

실제 게임 없이:

```text
valid Item ID
→ Scanner catalog item
→ current GameContentCatalog item if present
→ current ItemsWorkspace RequiredTotal
→ local icon if cached
→ overlay
```

검증:

- invalid Item ID → no snapshot/no fake data
- item이 current NeededItems에 없음 → current needed = 0
- owned quantity가 있어도 displayed current needed는 RequiredTotal
- missing trader price → trader line hidden
- missing flea price → flea line hidden
- missing dimensions → per-slot lines hidden
- missing icon → icon only omitted
- no network caused by preview icon load

## 7. Overlay

Windows manual/smoke:

- transparent / no background panel
- Topmost
- taskbar 미표시
- play mode click-through
- play mode no-activate
- game/other foreground app focus를 빼앗지 않음
- edit mode only drag
- position save/reset
- negative-coordinate monitor position persistence
- display toggle 즉시 반영
- Scanner Window와 MiniMap Window 독립
- Scanner hide/close가 MiniMap을 닫지 않음
- MiniMap hide/close가 Scanner를 닫지 않음
- background runtime callback이 WPF cross-thread exception을 만들지 않음

## 8. Runtime state machine

fake detector/OCR로 추후 Desktop test harness에서 확인:

- geometry 1 hit → OCR 없음
- geometry stable 2 hit 이후 title stability 시작
- title 1 hit → OCR 없음
- title stable 2 hit → OCR 1회
- same title 계속 → OCR 반복 없음
- different title 첫 hit → old overlay 즉시 hide
- new title stable → OCR
- two consecutive misses → overlay hide
- low-confidence match → overlay hidden
- failed title cooldown 동안 OCR 억제
- mode change → old identity/result clear
- catalog unavailable → detector/OCR 진행 금지

## 9. Resource cleanup

- Scanner disable → context monitor stop
- Scanner disable → runtime loop cancel
- app close → monitor/runtime cancel
- overlay Window close
- catalog sync cancellation
- shared Desktop `HttpClient` ownership 침범 금지
- unobserved task exception 없음

## 10. Live Tarkov gate

Foundation CI가 green이어도 아래는 미검증입니다.

### A. Window capture
- window selection / borderless / fullscreen / DPI / overlay exclusion

### B. Inspect detector
- positive/negative samples / multiple resolutions / UI contexts

### C. Current Korean OCR
- current client strings only

### D. Item identity
- false-positive audit, confidence/margin calibration

### E. E2E
- actual inspect → Mini Scanner

### F. Long run
- CPU/memory/handles/OCR rate/input/focus/Alt+Tab/minimize/MiniMap coexistence

A~F가 끝나기 전에는 Scanner를 production-complete로 선언하지 않습니다.
