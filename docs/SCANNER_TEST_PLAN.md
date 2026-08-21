# Scanner v1.1.3 Test Plan

기준일: 2026-08-21

상태: **`RELEASE GATE DEFINED / SCANNER LAB v3.8 REGRESSION VERIFIED / LIVE TARKOV E2E DEFERRED`**

이 문서는 v1.1.3 공개 전 자동/Windows gate와 공개 후 실제 Tarkov 검증 범위를 분리합니다.

## 1. 공개 차단 gate

다음은 전부 성공해야 합니다.

1. Windows Release Desktop build
2. 전체 automated tests 0 failure
3. Scanner Lab v3.8 structural candidate regression
4. current catalog/matcher regression
5. win-x64 self-contained single-file publish
6. ProductVersion = 1.1.3
7. FIRST_RUN first line = v1.1.3
8. package root/dependency/PDB/nested-archive audit
9. actual published EXE startup
10. rendered Product UI + Scanner UI assertions
11. Main Map / Factory / MiniMap runtime smoke
12. graceful Main Window close/process exit
13. Draft ZIP/checksum/package/ProductVersion verification
14. Draft-downloaded EXE smoke
15. public/latest 전환
16. public ZIP/checksum/package/ProductVersion 재검증
17. public-downloaded EXE smoke
18. temporary release workflow cleanup

실제 최신 Tarkov 실행 E2E는 DEC-051에 따라 공개 차단 gate가 아니며 사용자 환경에서 후속 검증합니다.

## 2. Scanner Lab v3.8 recognition regression

### Structural candidate generation

반드시 유지:

- RED-X connected-component path
- RED-X anchored outer-window reconstruction
- rectangle/edge projection fallback
- IoU candidate deduplication
- candidate limit 8
- structural floor 0.34
- structure score alone으로 final inspect 확정 금지

### 고정 회귀 샘플 구조

1. cropped `Ophthalmoscope 검안경` shape
   - outer inspect 약 `3,3,672,514`
   - title ROI가 최상단 item title에 위치
2. full `Water 0.6L 물병` screenshot shape
   - inspect 약 `622,282,674,514`
   - 중앙 상세창과 title ROI 복원
3. strong inner rectangle coexistence
   - 내부 고대비 rectangle 때문에 RED-X outer candidate가 사라지면 실패
4. no RED-X
   - rectangle fallback candidate가 있어야 함
5. uniform frame
   - candidate가 없어야 함

### Title ROI

v3.8 공식:

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

## 3. Semantic candidate validation

최종 상세창 판정은 geometry 하나가 아니라 OCR + current official catalog가 포함된 semantic validation입니다.

확인:

- 최대 8개 candidate first-pass OCR
- structural floor 미만 skip
- official item으로 resolve되는 후보 우선
- 구조적으로 높은 후보라도 matcher 실패 시 final inspect 아님
- 성공 후보의 semantic confidence + structural score 결합 순위
- 상위 3개 deep OCR fallback
- verified bounds/title signature 동일 시 OCR 반복 억제
- title/inspect 변화 시 기존 Item clear 후 재검증
- no successful candidate → fail-closed

## 4. OCR

Windows `ko-KR` OCR boundary를 사용합니다.

Adaptive scale:

- title height <=14 → 8x
- <=20 → 6x
- else → 4x

Deep OCR:

1. enlarged original
2. high-contrast grayscale
3. binary white-on-black
4. inverse black-on-white

Text candidate:

- full OCR text
- individual line
- adjacent two-line combination

OCR/matcher 성공률을 위해 historical alias를 추가하거나 confidence gate를 낮추지 않습니다.

## 5. Matcher

- current official name exact match
- 작은 OCR typo + 충분한 margin
- low confidence reject
- top1/top2 margin 부족 reject
- duplicate normalized official name reject
- 짧은 이름 substring 강제 선택 금지
- 과거 이름과 현재 이름이 다르면 강제 매칭 금지

## 6. Full catalog

- 4,000개 이상 Korean catalog load
- Item ID/name/market/dimension parse
- regular / pve / pvp-season
- Korean translation + English per-key fallback
- corrupt/missing cache reject
- requested mode missing 시 wrong-mode identity 사용 금지
- zero/missing flea price → null
- invalid dimension → price/slot null
- AtomicJson backup recovery

## 7. Windows capture/runtime

Windows runner에서:

```text
dotnet build src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj -c Release
dotnet test tests/JunhyunHelper.Tests/JunhyunHelper.Tests.csproj -c Release
```

검증 대상:

- EscapeFromTarkov process/window discovery
- GetClientRect + ClientToScreen
- PrintWindow
- Graphics.CopyFromScreen fallback
- multi-monitor enumeration
- Windows ko-KR OCR
- WPF BitmapSource handoff
- Scanner tab
- Mini Scanner direct drag/no-activate

## 8. Scanner OFF / mode 전환

- real Scanner default OFF
- display test session startup OFF
- 둘 다 OFF → capture/detector/OCR loop 없음
- real ON → TarkovWindow
- test ON → DisplayTest
- mutually exclusive
- test persistence 금지
- mode change 시 previous candidate/item state clear

## 9. Scanner tab UI

유지:

- 좌측 `스캐너 OFF`, `테스트 OFF`
- 우측 `아이템 목록 최신화`
- 7개 display checkboxes
- recent recognition activity

제품 UI에서 없어야 함:

- Scanner 상단 설명문
- toggle/catalog/Mini Scanner 설명문
- 위치 편집/초기화
- Foundation verification/preview controls

## 10. 최근 인식 기록 / diagnostics

사용자 activity는 OCR/matcher 결과를 사람이 읽을 수 있게 표시합니다.

- timestamp
- mode
- OCR text
- nearest official candidate
- confidence
- second score / margin
- success/hold
- reason

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

v1.1.3 추가 관찰점:

- geometry-candidates
- candidate-semantic
- OCR pass (`ORIGINAL` / deep preprocessing)
- candidate structure reason/score/bounds
- match reason/confidence
- semantic-selected

screenshot/raw pixels는 저장하지 않습니다. 로그 I/O 실패는 nonfatal이어야 합니다.

## 11. Mini Scanner

- Topmost
- ShowActivated=false
- WS_EX_NOACTIVATE
- WS_EX_TOOLWINDOW
- 별도 edit/reset UI 없음
- visible 상태 direct left-drag
- drag 종료 위치 저장
- negative multi-monitor coordinates
- MiniMap과 독립 lifecycle
- ON standby / OFF hidden

## 12. Presentation

```text
valid Item ID
→ Scanner catalog
→ GameContentCatalog
→ ItemsWorkspace RequiredTotal
→ local icon if cached
→ Mini Scanner
```

- invalid ID → fake snapshot 금지
- NeededItems 없음 → current needed 0
- current needed = RequiredTotal
- missing price/icon은 해당 표시만 omit
- presentation/icon load network 없음

## 13. v1.1.3 자동 검증 기준선

Scanner Lab 복원 코드 validation:

```text
CI run: #1222
run id: 32466187224
Windows Release build: SUCCESS
automated tests: 245 passed / 0 failed / 0 skipped
win-x64 self-contained single-file publish: SUCCESS
actual candidate EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
```

이 검증은 제품 코드 `f412d5910c338e07937faff4b697c114fd8306be` 기준 복원부를 검사한 것입니다. 최종 v1.1.3 버전/문서가 포함된 release PR에서도 동일 gate를 다시 통과해야 합니다.

## 14. 공개 후 실제 Tarkov gate

### A. Borderless capture
- process/window discovery
- PrintWindow vs exact client-screen fallback
- DPI/multi-monitor
- minimize/Alt+Tab

### B. Structural candidates
- actual current inspect positive
- negative contexts
- RED-X / rectangle candidate distribution
- candidate count/score

### C. Korean OCR
- Korean/mixed/English official names
- short/long/numeric/parentheses
- adaptive/deep pass distribution

### D. Semantic identity
- which candidate wins
- exact/fuzzy distribution
- confidence/margin
- ambiguous reject

### E. E2E

```text
실제 상세창
→ capture
→ candidates
→ OCR
→ semantic catalog validation
→ Item ID
→ Mini Scanner
```

### F. Input coexistence
- Mini Scanner direct drag
- game focus 유지
- MiniMap coexistence

### G. Long run
- CPU
- memory
- handles
- OCR rate
- Alt+Tab/minimize

## 15. 판정

v1.1.3은 v1.1.2 Scanner recognition regression을 실제 Scanner Lab v3.8 구조로 복원하는 PATCH입니다.

공개 전 표현:

```text
Scanner Lab v3.8 restoration: IMPLEMENTED + AUTOMATED/EXE VALIDATED
latest live Tarkov Borderless revalidation: PENDING
```

인게임 문제가 발견되면 `scanner.log`와 최근 인식 기록을 기준으로 후속 PATCH로 보정합니다.
