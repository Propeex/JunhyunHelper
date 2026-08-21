# Scanner — 제품/기술 계약

기준일: 2026-08-21

상태: **`v1.1.5 PUBLIC RELEASE / VERIFIED / SCANNER LAB v3.8 CONTRACT PRESERVED / FONT-AWARE RECOVERY ADDED / LIVE TARKOV E2E ONGOING`**

## 1. 목적과 안전 원칙

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행 데이터에 연결하는 입력 bridge입니다.

```text
화면 픽셀
→ structural candidates
→ title ROI
→ 한국어 OCR
→ current official Korean full-item catalog semantic validation
→ optional Tarkov-title-font recovery only after failed deep OCR
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. geometry나 font shape만으로 Item을 확정하지 않으며 current official catalog까지 안전하게 통과해야 합니다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon/image identity
- scan 순간 HTTP/API

## 2. Capture mode

### 실사용

```text
EscapeFromTarkov process/window
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ PrintWindow 우선
→ 유효 pixel이 없으면 exact client screen rectangle fallback
```

최소화/유효하지 않은 client-area에서는 인식하지 않습니다.

### 테스트

연결된 전체 디스플레이를 대상으로 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 실행합니다. screenshot을 이미지 뷰어에 띄운 상태에서도 검증할 수 있습니다.

real/test는 상호 배타적이며 test는 session-only입니다. 둘 다 OFF면 capture/OCR background loop가 없습니다.

## 3. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 Tarkov 전체 Item입니다.

```text
https://json.tarkov.dev/{gameMode}/items
https://json.tarkov.dev/{gameMode}/items_ko
```

mode: `regular`, `pve`, `pvp-season`.

catalog은 준비/명시적 최신화 시 network를 사용할 수 있지만 실제 scan 중에는 local/memory data만 사용합니다.

v1.1.5 market-health contract:

- valid Item count >= 4,000
- positive best-trader coverage >= 500
- name만 충분하고 market data가 비정상적으로 비어 있는 catalog는 unhealthy
- unhealthy candidate는 known-good cache를 덮지 못함

## 4. Scanner Lab v3.8 recognition architecture

Scanner Lab v3.8 recognition architecture가 production structural 기준입니다. v1.1.5도 이 구조를 약화하거나 대체하지 않습니다.

### Structural candidates

```text
capture
→ RED-X connected-component candidates
+
→ rectangle/edge fallback candidates
→ IoU deduplication
→ runtime 최대 8 candidates
```

- RED-X anchor 기반 outer-window reconstruction
- rectangle/edge projection fallback
- aspect/border continuity/interior darkness 기반 구조 점수
- structural floor `0.34`
- structural score는 후보 순위이며 final identity 판정이 아님

### Title ROI

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

### 기본 OCR / semantic validation

Windows `ko-KR` OCR을 사용합니다.

- title height <=14px: 8x
- <=20px: 6x
- 그 외: 4x
- first-pass 실패 시 상위 3개 candidate에 enlarged/high-contrast/binary/inverse Deep OCR
- OCR full text, individual line, adjacent two-line combination 검사
- current official Korean full-item catalog match 필요
- exact-first
- fuzzy confidence threshold + top1/top2 margin 유지
- ambiguous/low-confidence는 Item ID 미확정
- historical alias production 누적 금지

## 5. v1.1.5 Tarkov title-font recovery

### 확인된 title font contract

상세보기 창 상단 Item 이름은 현재 Tarkov UI의 `ItemInfoWindowLabels._caption` TextMeshPro text입니다.

조사된 현재 UI font stack:

- Latin / digit / 지원 glyph: **Bender family primary**
- Hangul: **Noto Sans CJK KR fallback**

한영 혼합 Item 이름은 같은 문자열 안에서 해당 fallback 관계를 따릅니다.

### 적용 원칙

사용자 요구는 실제 title font를 이용해 텍스트 스캔 정확성을 보강하는 것입니다. 이를 위해 기존 OCR을 대체하지 않고 **Deep OCR 실패 뒤의 recovery stage**로 적용했습니다.

```text
normal OCR
→ existing semantic gate
→ 성공: 기존 결과 그대로 확정
→ 실패: existing Deep OCR
→ Deep OCR semantic success: 기존 결과 그대로 확정
→ 여전히 실패:
   OCR semantic similarity로 current official-name shortlist 생성
   → 공식 이름을 Bender + Noto CJK KR로 렌더링
   → 실제 title ROI binary glyph shape와 비교
   → semantic score + visual score + top1/top2 combined margin 평가
   → 보수적 threshold 모두 통과한 경우만 official name 복구
→ existing catalog resolver가 exact official name을 Item ID로 연결
```

Invariant:

- 기존 semantic gate가 accept한 결과를 font verifier가 downgrade/replace하지 않음
- font recovery는 failure-only path
- current official Korean catalog가 여전히 identity authority
- font는 supporting evidence이며 standalone Item identity가 아님
- short name은 더 엄격한 semantic/visual/combined/margin 기준
- weak/ambiguous evidence는 no Item ID

### Font acquisition

Bender font binary를 JunhyunHelper ZIP에 재배포하지 않습니다.

`TarkovTitleFontProvider`는 실행 중인 사용자의 Tarkov 경로에서:

```text
EscapeFromTarkov_Data/resources.assets
```

를 **read-only**로 확인합니다. Embedded SFNT payload를 parse하고 SkiaSharp의 실제 font metadata로 family를 검증한 뒤 필요한 파일만 app-local cache에 저장합니다.

```text
%LocalAppData%/JunhyunHelper/scanner/fonts/Bender-Regular.otf
%LocalAppData%/JunhyunHelper/scanner/fonts/Bender-Bold.otf
%LocalAppData%/JunhyunHelper/scanner/fonts/NotoSans-CJK-KR-Regular.otf
```

- game directory write 없음
- public package font redistribution 없음
- resources asset가 더 최신이면 stale local font cache 사용 금지
- Bender Regular/Bold 모두 발견 가능하며 렌더링 비교에서 더 강한 variant 사용
- asset 탐색/parse/metadata validation 실패는 nonfatal
- 실패 시 font-aware recovery만 disable되고 기존 OCR-only pipeline 계속 사용

### OCR composition

```text
ScannerLab38OcrEngine
→ SerializedScannerOcrEngine
   ├─ Mini Scanner inventory/stash context detector: serialized OCR 직접 사용
   └─ Item title runtime: FontAwareScannerOcrEngine 사용
```

따라서 context detector에는 Item-title font recovery 비용/의미가 섞이지 않습니다.

## 6. Runtime 안정화

semantic OCR 전에 candidate가 2회 안정적으로 관측되어야 합니다. 연속 candidate 집합 사이에 동일한 quantized `GeometrySignature`가 겹칠 때만 안정화 hit를 누적합니다.

이미 Item ID가 확정된 뒤 verified bounds와 title signature가 계속 일치하면 OCR을 반복하지 않습니다. title/geometry가 바뀌면 기존 Item을 clear하고 다시 안정화/semantic validation합니다.

같은 verified 상세창을 계속 보는 동안에는 1초 간격으로 presentation snapshot만 다시 구성합니다. Quest/Hideout 진행으로 `RequiredTotal`이 변하면 상세창을 닫지 않아도 표시가 갱신됩니다.

Title OCR과 inventory-context OCR은 하나의 `SerializedScannerOcrEngine` semaphore boundary를 사용해 concurrent WinRT OCR을 방지합니다.

## 7. Item ID 이후 표시 데이터

Scanner는 Quest/Hideout/Inventory 의미를 복제하지 않습니다.

```text
Item ID
→ ScannerItemPresentationService
→ Scanner catalog + GameContentCatalog + ItemsWorkspace
→ Mini Scanner
```

표시 가능 정보:

- official name
- local cached icon
- 최고 상점가
- 플리마켓 24시간 평균가
- 슬롯 수
- 상점가/슬롯
- 플리 평균가/슬롯
- 현재 필요한 수량

### 최고 상점가 — v1.1.5

1. raw JSON `traderPrices`에 positive trader 가격이 있으면 해당 `priceRUB` 최댓값 사용
2. raw trader 가격이 없으면 derived `sellFor`에서 flea source를 제외한 positive `priceRUB` 최댓값 사용

### 플리마켓 평균가

`avg24hPrice > 0`만 사용합니다. trader sell price와 독립된 필드입니다.

### 슬롯 / 슬롯당 가격

positive `width * height`만 유효합니다. dimension 또는 price가 유효하지 않으면 해당 price-per-slot만 비웁니다.

### 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

부족량이나 보유량 차감 결과가 아닙니다. Needed Items에 없으면 0입니다.

## 8. Icon contract / optimization

Scanner는 scan 중 아이콘을 network로 받지 않습니다. `%LocalAppData%/JunhyunHelper/image-cache`의 PNG만 읽습니다.

- 성공적으로 decode/freeze한 Scanner icon은 process-local memory cache 재사용
- v1.1.5부터 explicit Game Content update가 **전체 canonical `GameContentCatalog.Items` icon**을 prefetch
- 기존 valid cache file은 재다운로드하지 않음
- 개별 icon failure는 Game Content update 전체 fatal이 아님

## 9. Scanner 탭

상단 bar:

- 왼쪽: `스캐너`, `테스트`
- 오른쪽: `아이템 목록 최신화`

그 아래:

- 7개 표시 정보 checkbox
- 최근 인식 기록

최근 인식 기록 header 우측 상단에는 `로그 삭제` 버튼이 있습니다.

Foundation preview/verification controls와 Mini Scanner 별도 위치 편집/초기화 controls는 일반 사용자 UI에 노출하지 않습니다.

## 10. 최근 인식 기록 / 개발자 로그

사용자 activity는 timestamp, mode, OCR text, nearest official candidate, confidence, second score/margin, success/hold, reason을 표시합니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

구조 candidate, candidate별 OCR pass, match reason/confidence, semantic-selected, runtime error metadata를 기록합니다. screenshot/raw pixel buffer는 저장하지 않습니다.

v1.1.5 context/font diagnostics:

```text
inventory-context
title-font-cache-load-failed
title-font-extract-ready
title-font-extract-missing
title-font-extract-failed
title-font-verify-accepted
title-font-verify-rejected
title-font-recovery-error
```

약 2MB에서 회전하며 logging 실패는 Scanner fatal이 아닙니다.

`로그 삭제`는 다음을 함께 clear합니다.

- process memory의 recent activity
- `scanner.log`
- `scanner.log.1`

삭제 실패도 Scanner 인식 실패로 확대하지 않습니다.

## 11. Mini Scanner — v1.1.5

### 표시 의미

- MiniMap과 독립 Window/service/settings/lifecycle
- matched Item result만 표시
- Scanner runtime waiting/OCR/error/diagnostic text는 표시하지 않음
- Scanner OFF 또는 uncertain/invalid context면 hidden

### Foreground inventory/stash gate

실사용에서 Item snapshot을 표시하려 할 때 `ScannerInventoryContextDetector`가 foreground `EscapeFromTarkov` client인지 확인하고 작은 상단 client band를 OCR합니다.

현재 Korean semantic anchors:

- `장비`
- `건강상태` / `건강 상태`
- `스킬`
- `지도`
- `종합정보` / `종합 정보`

**2개 이상** 필요합니다. regular OCR 후 필요 시 Deep OCR을 사용하며 decision은 850ms cache합니다. 확신할 수 없으면 hidden입니다.

Display-test/explicit preview는 deterministic product validation을 위해 gate bypass입니다.

### Window/input contract

- WPF `Topmost=True`
- native `SetWindowPos(HWND_TOPMOST, ..., SWP_NOACTIVATE)` 재assert
- `ShowActivated=false`
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- whole root card = drag hitbox
- near-transparent nonzero-alpha root background로 layered WPF hit testing 보장
- `PreviewMouseLeftButtonDown`으로 child text/icon 위에서도 drag 시작
- `ForceCursor=True`, Arrow cursor
- drag 종료 위치 즉시 settings 저장
- negative multi-monitor coordinate 허용

### Display settings migration

Scanner display settings schema v2에서 기존 install을 1회 normalize해 intended matched-item defaults를 켭니다.

- icon
- trader price
- trader price per slot

그 뒤 사용자의 checkbox 선택은 정상적으로 persist합니다.

## 12. Persistence

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/fonts/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

settings/catalog은 same-directory temp + flush + atomic replacement + last-known-good `.bak` recovery를 사용합니다. Font cache는 game asset 기반 presentation/recovery cache이며 Item identity source가 아닙니다.

## 13. v1.1.5 검증 계약 — 완료

Final public evidence:

```text
release source: 3541bab6536ff91a00f394c4f7b03d5cbf112746
PR final CI: 32493986403 — SUCCESS
automated tests: 249 / 0 failed / 0 skipped
Draft/public verification: 32495042444 — SUCCESS
independent public verification: 32495225958 — SUCCESS
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
public/latest: VERIFIED
public tag exact source: VERIFIED
Draft-downloaded EXE smoke: SUCCESS
public-downloaded EXE smoke: SUCCESS
independent public-downloaded EXE smoke: SUCCESS
```

Release gate 완료:

- Windows Release build
- 249 automated tests
- Scanner Lab v3.8 geometry/title ROI regressions
- raw market-shape / market-health regressions
- SFNT parser + Hangul fallback smoke
- win-x64 self-contained single-file publish
- exact ProductVersion/FIRST_RUN
- actual published EXE Product UI / Mini Scanner / Scanner / Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft asset re-download SHA/root/ProductVersion/FIRST_RUN + EXE smoke
- public/latest + exact tag source
- public asset re-download SHA/root/ProductVersion/FIRST_RUN + EXE smoke
- separate independent public verification runner

## 14. 실사용 후속 검증

CI runner에는 Tarkov 설치가 없으므로 다음은 latest live environment에서 계속 확인합니다.

- Korean inventory/stash anchor set과 실제 현재 UI의 대응
- 실제 `resources.assets`에서 Bender/Noto SFNT 추출
- font verifier accept/reject 품질

이 empirical 영역은 release blocker가 아니지만 반드시 fail closed / OCR-only fallback이어야 합니다. 실사용 문제를 고칠 때 current official-name matcher의 confidence/margin을 낮춰 false positive를 늘리지 않습니다.
