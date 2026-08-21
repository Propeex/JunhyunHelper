# Scanner — 제품/기술 계약

기준일: 2026-08-21

상태: **`v1.1.4 RELEASE CANDIDATE / SCANNER LAB v3.8 CONTRACT PRESERVED / LIVE TARKOV E2E ONGOING`**

## 1. 목적과 안전 원칙

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행 데이터에 연결하는 입력 bridge입니다.

```text
화면 픽셀
→ structural candidates
→ title ROI
→ 한국어 OCR
→ current official Korean full-item catalog semantic validation
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. geometry만으로 Item을 확정하지 않으며 current official catalog까지 안전하게 통과해야 합니다.

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

## 4. Scanner Lab v3.8 recognition architecture

v1.1.3에서 복원한 Scanner Lab v3.8 recognition architecture가 production 기준입니다. v1.1.4는 이를 약화하거나 대체하지 않습니다.

### Structural candidates

```text
capture
→ RED-X connected-component candidates
+
→ rectangle/edge fallback candidates
→ IoU deduplication
→ 최대 8개 candidates
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

### OCR / semantic validation

Windows `ko-KR` OCR을 사용합니다.

- title height <=14px: 8x
- <=20px: 6x
- 그 외: 4x
- first-pass 실패 시 상위 3개 candidate에 enlarged/high-contrast/binary/inverse deep OCR
- OCR full text, individual line, adjacent two-line combination 검사
- current official Korean full-item catalog match 필요
- exact-first
- fuzzy confidence threshold + top1/top2 margin 유지
- ambiguous/low-confidence는 Item ID 미확정
- historical alias production 누적 금지

## 5. Runtime 안정화 — v1.1.4

semantic OCR 전에 candidate가 2회 안정적으로 관측되어야 합니다.

v1.1.4부터 단순히 두 프레임 모두 candidate가 있다는 사실만 세지 않습니다. 연속 candidate 집합 사이에 동일한 quantized `GeometrySignature`가 겹칠 때만 안정화 hit를 누적합니다.

이미 Item ID가 확정된 뒤 verified bounds와 title signature가 계속 일치하면 OCR을 반복하지 않습니다. title/geometry가 바뀌면 기존 Item을 clear하고 다시 안정화/semantic validation합니다.

## 6. Item ID 이후 표시 데이터

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

### 최고 상점가

`ScannerCatalogService`는 `sellFor` 중 `source == fleaMarket`을 제외하고 `priceRUB`가 유효한 행의 최댓값을 사용합니다. 여러 trader가 있으면 가장 높은 RUB 환산 판매가가 선택됩니다.

### 플리마켓 평균가

`avg24hPrice > 0`만 사용합니다. trader sell price와 독립된 필드입니다.

### 슬롯 / 슬롯당 가격

positive `width * height`만 유효합니다. dimension 또는 price가 유효하지 않으면 해당 price-per-slot만 비웁니다.

### 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

부족량이나 보유량 차감 결과가 아닙니다. Needed Items에 없으면 0입니다.

v1.1.4에서는 같은 verified 상세창을 계속 보는 동안에도 1초 간격으로 presentation snapshot을 재구성합니다. 따라서 Quest/Hideout 진행으로 `RequiredTotal`이 변하면 상세창을 닫지 않아도 표시가 갱신됩니다. 이 refresh는 OCR을 재실행하지 않습니다.

## 7. Icon contract / optimization

Scanner는 scan 중 아이콘을 network로 받지 않습니다. 기존 `%LocalAppData%/JunhyunHelper/image-cache`에 이미 있는 PNG만 읽습니다.

v1.1.4에서는 성공적으로 decode/freeze한 Scanner icon을 process-local memory cache에 보관해 반복 presentation refresh에서 같은 PNG를 다시 decode하지 않습니다.

## 8. Scanner 탭

상단 bar:

- 왼쪽: `스캐너`, `테스트`
- 오른쪽: `아이템 목록 최신화`

그 아래:

- 7개 표시 정보 checkbox
- 최근 인식 기록

최근 인식 기록 header 우측 상단에는 `로그 삭제` 버튼이 있습니다.

Foundation preview/verification controls와 Mini Scanner 별도 위치 편집/초기화 controls는 일반 사용자 UI에 노출하지 않습니다.

## 9. 최근 인식 기록 / 개발자 로그

사용자 activity는 timestamp, mode, OCR text, nearest official candidate, confidence, second score/margin, success/hold, reason을 표시합니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

구조 candidate, candidate별 OCR pass, match reason/confidence, semantic-selected, runtime error metadata를 기록합니다. screenshot/raw pixel buffer는 저장하지 않습니다.

약 2MB에서 회전하며 logging 실패는 Scanner fatal이 아닙니다.

### 로그 삭제 — v1.1.4

`로그 삭제`는 다음을 함께 clear합니다.

- process memory의 recent activity
- `scanner.log`
- `scanner.log.1`

삭제 실패도 Scanner 인식 실패로 확대하지 않습니다. 삭제 직후 새 runtime diagnostic이 발생하면 새 log가 생성될 수 있습니다.

## 10. Mini Scanner

- MiniMap과 독립 Window/service/settings/lifecycle
- ON 상태: standby 또는 Item result
- OFF: hidden
- Topmost
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- visible 상태에서 direct left-drag
- drag 종료 위치 즉시 저장
- negative multi-monitor coordinate 허용

## 11. Persistence

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
```

same-directory temp + flush + atomic replacement + last-known-good `.bak` recovery를 사용합니다.

## 12. 검증 계약

v1.1.4 release gate:

- Windows Release build
- 247 automated tests / 0 failure
- Scanner Lab v3.8 geometry/title ROI regressions
- Scanner market field regressions
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN exact version check
- actual published EXE rendered Product UI / Scanner / Map / Factory / MiniMap smoke
- `로그 삭제` 실제 activity/log 생성-삭제 smoke
- graceful shutdown / clean portable root
- Draft package download/hash/ProductVersion validation
- Draft-downloaded EXE smoke
- public/latest exact tag verification
- public package re-download validation
- public-downloaded EXE smoke

실제 최신 Tarkov Borderless E2E는 release blocker가 아니며 사용자 환경에서 계속 검증합니다.

상세: `docs/SCANNER_TEST_PLAN.md`, `docs/SCANNER_LAB_3_8_REFERENCE.md`, `docs/RELEASE_1.1.4.md`.
