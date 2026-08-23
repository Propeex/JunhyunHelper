# Scanner — 제품/기술 계약

기준일: 2026-08-23

상태: **`v1.2.2 PUBLIC VERIFIED / SCANNER LAB v3.8 CONTRACT PRESERVED / LIVE TARKOV E2E ONGOING`**

## 1. 목적과 안전 원칙

Scanner는 Tarkov 화면을 기존 JunhyunHelper Item ID와 진행 데이터에 연결하는 화면 기반 입력 bridge입니다.

```text
화면 픽셀
→ structural candidates
→ close/magnifier/title anchor refinement
→ title ROI
→ Korean OCR + catalog character policy
→ official-name semantic matching
   OR conservative Tarkov-font visual recovery
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. geometry, icon, OCR 한 조각 또는 시각 유사도 하나만으로 Item을 확정하지 않으며 current official catalog까지 안전하게 통과해야 합니다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- icon/image 단독 identity 확정
- scan 순간 HTTP/API

### v1.2.1 deterministic hardening

v1.2.1은 live Tarkov 캡처가 필요한 recognition threshold를 추측해서 조정하지 않는 PATCH입니다. v1.2.0의 인식 의미를 유지하면서 코드/자동 검증만으로 확정할 수 있는 실행 수명, cache generation, 메모리 사용, stale-state 문제를 보강합니다.

- `resources.assets` font discovery: whole-file allocation → bounded streaming scan
- font cache: source manifest + actual Bender/Noto binary generation hash
- visual template caches: generation-aware + bounded
- Mini Scanner inventory/stash OCR: single active probe + latest-request coalescing + stale epoch rejection
- one-shot/profile monitor: shared state 직렬화 + current-mode/current-context 재확인
- shutdown: active font-recovery operation 종료 뒤 Skia/font resource disposal
- `PrintWindow` pre-validation: duplicate whole-frame managed copy 제거
- title-anchor diagnostics: actual component score 보존

### v1.2.2 catalog mode-transition hardening

v1.2.2는 Scanner catalog가 GameMode/profile transition 중 오래된 operation에 의해 되돌아갈 수 있는 deterministic race를 수정한 PATCH입니다.

- `ScannerCatalogService.RefreshAsync`와 `LoadCacheAsync`가 동일 `_refreshGate` 사용
- local cache load와 network refresh가 같은 in-memory identity/market state writer이므로 operation ordering을 직렬화
- cross-GameMode `ClearForMode`를 refresh가 gate를 획득한 뒤 수행
- older in-flight refresh가 newer profile/GameMode cache load를 뒤늦게 덮어쓰는 상태 역전 차단
- cache load가 Scanner catalog lifetime cancellation과 연결되어 shutdown 중 gate wait 종료 가능
- 실제 race ordering을 강제하는 regression test 유지

v1.2.1/v1.2.2에서 변경하지 않는 것:

- Scanner Lab v3.8 structural floor `0.34`
- OCR semantic matcher confidence threshold
- visual recovery acceptance threshold
- top1/top2 margin
- current official catalog identity contract
- two-anchor inventory/stash fail-closed gate
- 최고 상점가/플리 평균가/`RequiredTotal` 의미
- scan-time network / game-memory / injection / packet 금지 경계

실제 플레이에서 얻는 miss/false-positive evidence는 `scanner.log`와 `인식 이미지`를 근거로 별도 후속 calibration에 사용합니다.

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

`PrintWindow` 결과의 visual-content 사전 검사는 locked bitmap의 sparse pixels를 직접 읽습니다. 이 확인만을 위해 1440p/4K 전체 프레임을 별도 managed `byte[]`로 복사하지 않으며 실제 detector에 필요한 normalized BGRA copy는 그대로 유지합니다.

### 테스트

연결된 전체 디스플레이를 대상으로 실사용과 동일한 detector/OCR/catalog/presentation pipeline을 실행합니다. screenshot을 이미지 뷰어에 띄운 상태에서도 검증할 수 있습니다.

real/test는 상호 배타적이며 test는 session-only입니다. 둘 다 OFF면 continuous capture/OCR background loop가 없습니다.

## 3. Full Item identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 Tarkov 전체 Item입니다.

```text
https://json.tarkov.dev/{gameMode}/items
https://json.tarkov.dev/{gameMode}/items_ko
```

mode: `regular`, `pve`, `pvp-season`.

catalog은 준비/명시적 최신화 시 network를 사용할 수 있지만 실제 scan 중에는 local/memory data만 사용합니다.

Identity health:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

시장 가격 coverage는 identity health와 분리합니다.

### v1.2.2 catalog operation ordering

Catalog state replacement는 단일 operation boundary를 사용합니다.

```text
LoadCacheAsync(mode)
        \
         → _refreshGate → ReplaceData/ClearForMode → matcher/OCR policy catalog
        /
RefreshAsync(mode)
```

`LoadCacheAsync`가 gate 밖에서 새 mode를 먼저 적용하고 오래된 `RefreshAsync`가 나중에 이전 mode를 덮어쓰는 순서를 허용하지 않습니다. `RefreshAsync`의 cross-mode clear도 gate 밖에서 수행하지 않습니다.

## 4. Scanner Lab v3.8 structural architecture

v1.1.3에서 복원한 Scanner Lab v3.8 recognition architecture가 production geometry 기준입니다. v1.2.0은 이를 대체하지 않고 title-anchor refinement와 semantic/visual recovery를 위에 추가했고, v1.2.1/v1.2.2는 그 recognition 의미를 바꾸지 않고 deterministic runtime/catalog reliability를 보강합니다.

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

### Stable candidate contract

semantic recognition 전에 동일 quantized `GeometrySignature`가 연속 관측되어야 continuous path가 안정화됩니다.

이미 Item ID가 확정된 뒤 verified bounds와 title signature가 계속 일치하면 OCR을 반복하지 않습니다. title/geometry가 바뀌면 기존 Item을 clear하고 다시 검증합니다.

## 5. v1.2.0 title anchor refinement

기존 Scanner Lab title ROI는 fallback 계약으로 유지합니다.

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

v1.2.0에서는 상세창 헤더의 다음 evidence를 추가로 분석합니다.

- 우측 빨간 close/X
- 좌측 magnifier/search icon
- 어두운 title-field strip

`ScannerTitleAnchorRefiner`는 structural candidate 안에서 anchor evidence를 평가하고 실제 title bounds를 보정합니다.

v1.2.1부터 diagnostic anchor score는 anchor가 존재한다는 이유만으로 100%로 승격하지 않고 실제 detected component score를 보존합니다. 이는 진단 정확성 개선이며 Item ID confidence threshold를 낮추지 않습니다.

### Magnifier exclusion

magnifier anchor가 충분히 신뢰되면 OCR title ROI의 왼쪽 경계는 magnifier 오른쪽 padding 이후로 이동합니다.

따라서 돋보기의 원/손잡이 픽셀이 글자로 OCR되는 문제를 구조적으로 차단합니다.

### Fail-closed fallback

anchor가 불확실하거나 title refinement가 유효한 rectangle을 만들지 못하면 기존 Scanner Lab v3.8 title ROI로 되돌아갑니다. 불확실한 anchor 때문에 임의의 새로운 영역을 OCR하지 않습니다.

## 6. OCR / character policy / semantic matching

Primary OCR은 Windows `ko-KR` OCR입니다.

- title height <=14px: 8x
- <=20px: 6x
- 그 외: 4x
- first-pass 실패 시 enlarged/high-contrast/binary/inverse deep OCR
- OCR full text, line 및 유효 조합을 current catalog와 비교
- exact-first
- fuzzy confidence threshold + top1/top2 margin 유지
- ambiguous/low-confidence는 Item ID 미확정
- historical alias production 누적 금지

### Catalog-derived allowed characters

`ScannerOcrCharacterPolicy`는 current official Korean item-name catalog에서 실제 허용 문자 집합을 계산합니다.

- Hangul, Latin, 숫자, 실제 이름에 존재하는 기호만 catalog evidence에 따라 허용
- 공식 이름에 없는 예상치 못한 문자는 corrupted OCR evidence
- Han ideograph는 Korean-client item-title contract에서 invalid evidence
- 특정 오인식 문자를 임의로 다른 문자로 치환하지 않음

이 정책은 고정 blacklist가 아니라 현재 catalog에서 파생되므로 게임 데이터 변경에 맞춰 자동으로 갱신됩니다.

## 7. Tarkov-font visual recovery

OCR이 비거나 corrupted character policy를 통과하지 못하거나 semantic confidence가 부족한 경우, OCR-independent visual recovery를 사용할 수 있습니다.

```text
actual title pixels
→ normalized glyph/title representation
→ current official Korean full-item names
→ Tarkov-compatible local font rendering
→ visual score ranking
→ conservative top1 score + top1/top2 margin
→ Item ID or fail closed
```

핵심 계약:

- 전체 공식 Item 이름 집합 안에서만 후보를 생성
- 네트워크 호출 없음
- visual score만 높다고 arbitrary text/Item을 발명하지 않음
- top1 confidence와 top1/top2 margin을 모두 요구
- ambiguous candidate는 거부
- successful existing OCR path를 우회하거나 약화하지 않음

이 경로의 목적은 일반 OCR 엔진을 대체하는 것이 아니라 Tarkov UI glyph와 공식 이름 집합을 이용한 constrained recovery입니다.

### Font source / generation contract — v1.2.1

게임 폰트 바이너리는 JunhyunHelper public package에 넣지 않습니다.

```text
running EscapeFromTarkov
→ EscapeFromTarkov_Data/resources.assets (read-only)
→ bounded streaming SFNT discovery
→ local Scanner font cache
   ├─ Bender Regular
   ├─ Bender Bold (존재 시)
   └─ Noto Sans CJK KR
→ font-cache.json source manifest
→ actual cached font SHA-256 generation key
→ generation-aware visual templates
```

- `resources.assets` 전체를 한 managed byte array로 읽지 않음
- source path/length/last-write가 달라지면 loaded generation 재검증
- Bender/Noto 실제 cache bytes 조합이 generation key
- manifest를 마지막에 commit해 부분 extraction을 정상 generation으로 인정하지 않음
- legacy cache freshness는 존재하는 모든 Bender variant + Noto를 고려
- source/font generation이 바뀌면 기존 rendered template 폐기
- corrupt/unavailable font cache는 visual recovery만 fail-soft로 건너뛰며 primary OCR을 fatal로 만들지 않음

OCR-guided template cache와 full-catalog mask/aspect cache는 bounded이며 font generation이 바뀌면 clear합니다.

## 8. Runtime recognition flow

Continuous path:

```text
capture
→ candidates
→ geometry stability
→ title anchor refinement
→ OCR passes
→ catalog semantic match
→ optional visual recovery
→ verified Item ID
→ presentation snapshot
→ Mini Scanner
```

동일 verified detail/title signature가 유지되는 동안에는 OCR 재실행을 억제하고 presentation snapshot만 약 1초 간격으로 재생성할 수 있습니다. Quest/Hideout 진행 변화로 `RequiredTotal`이 바뀌면 같은 상세창을 열어 둔 상태에서도 표시가 갱신됩니다.

## 9. 1회 고정밀 스캔

v1.2.0 사용자 기능입니다.

- continuous Scanner가 OFF여도 한 번만 정밀 recognition 가능
- Scanner 탭 버튼 제공
- default global hotkey: `Ctrl+Shift+F10`
- hotkey 변경/비활성화 가능
- Scanner display settings schema v3

One-shot precision path는 continuous 350ms loop보다 한 번의 recognition에 더 많은 CPU budget을 허용합니다.

- structural candidate 상위 집합을 더 폭넓게 평가
- original OCR
- deep OCR
- visual recovery
- 최종 combined evidence ranking

### Continuous/one-shot concurrency

실시간 Scanner/Test가 이미 실행 중이면 one-shot은 공유 state를 동시에 건드리지 않습니다.

```text
remember active mode
→ StopLoop()
→ await previous loop Task completion
→ one-shot capture/OCR/presentation
→ latest user setting 확인
→ 같은 mode가 여전히 요청된 경우에만 restart
```

`ScannerCoordinator`의 one-shot gate는 중복 단축키/버튼 호출을 직렬화합니다. v1.2.1부터 profile/GameMode monitor도 같은 gate 뒤에서 최신 context를 다시 읽으므로 one-shot 중 발생한 오래된 context tick이 이전 runtime을 되살리지 않습니다.

## 10. OCR serialization / resource lifetime

Item-title OCR과 inventory-context OCR은 `SerializedScannerOcrEngine`을 통해 동일 WinRT OCR boundary를 공유합니다.

동시에 여러 OCR call이 WinRT 엔진을 경쟁하지 않도록 `SemaphoreSlim`으로 직렬화합니다.

One-shot은 이 OCR serialization뿐 아니라 continuous runtime loop 자체의 종료를 await하여 detector/presentation state race도 차단합니다.

v1.2.1에서 `FontAwareScannerOcrEngine`은 active-operation lease를 사용합니다. Dispose 요청 후 새 operation은 거부하고, 이미 진행 중인 title OCR/visual recovery가 끝난 뒤 `ScannerFullCatalogVisualMatcher`, `ScannerTitleFontVerifier`, `TarkovTitleFontProvider`의 Skia/font 자원을 해제합니다. UI thread에서 Scanner task 종료를 동기 대기하지 않습니다.

## 11. 인식 이미지

v1.2.0 사용자 진단 기능입니다.

`ScannerRecognitionDebugStore`는 process memory에 최신 frame 1개만 유지합니다.

Frame metadata:

- `BitmapSource Image`
- capture origin/source
- selected detail bounds
- title bounds
- magnifier bounds
- close bounds
- structural score/reason
- title-anchor score/reason
- title signature
- recognition pass
- OCR text
- candidate official name
- recognition reason
- confidence
- second score

`ScannerRecognitionDebugWindow`는 이 frame을 렌더링해 사용자가 실제 인식 영역을 확인할 수 있게 합니다.

### Privacy/persistence

- screenshot/raw pixels는 디스크에 저장하지 않음
- 최신 frame 1개만 memory에 보관
- 로그에는 구조/점수/문자열 metadata만 기록

최종 recognition이 선택된 뒤 debug analysis를 갱신하여 중간에 버려진 후보의 confidence가 최종 선택처럼 표시되지 않도록 합니다.

## 12. Global hotkey

`ScannerGlobalHotkeyService`는 Windows `RegisterHotKey`를 사용합니다.

- default `Ctrl+Shift+F10`
- Ctrl/Alt/Shift 중 하나 이상의 modifier 필요
- `MOD_NOREPEAT` 사용
- 동일 handler 중복 실행 방지
- registration 실패 시 Scanner UI status에 표시
- window lifecycle에 맞춰 unregister

`ScannerHotkeyCaptureWindow`에서 새 gesture를 입력하거나 단축키 사용 안 함을 선택할 수 있습니다.

## 13. Item ID 이후 표시 데이터

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

raw `traderPrices` 또는 derived `sellFor`에서 fleaMarket을 제외하고 유효한 RUB 환산 판매가 최댓값을 사용합니다.

### 플리마켓 평균가

`avg24hPrice > 0`만 사용합니다. trader sell price와 독립된 필드입니다.

### 슬롯 / 슬롯당 가격

positive `width * height`만 유효합니다. dimension 또는 price가 유효하지 않으면 해당 price-per-slot만 비웁니다.

### 현재 필요한 수량

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

부족량이나 보유량 차감 결과가 아닙니다. Needed Items에 없으면 0입니다.

## 14. Icon contract / optimization

Scanner는 scan 중 아이콘을 network로 받지 않습니다. `%LocalAppData%/JunhyunHelper/image-cache`에 이미 있는 파일만 읽습니다.

Game Content update는 canonical item 전체 icon을 prefetch합니다. 성공적으로 decode/freeze한 Scanner icon은 process-local memory cache에서 재사용할 수 있습니다.

개별 icon 실패는 identity catalog나 전체 Game Content update를 fatal로 만들지 않습니다.

## 15. Scanner 탭

주요 controls:

- `스캐너 ON/OFF`
- `테스트 ON/OFF`
- `1회 고정밀 스캔`
- `인식 이미지`
- one-shot hotkey 표시/변경
- `아이템 목록 최신화`
- 표시 정보 checkboxes
- 최근 인식 기록
- `로그 삭제`

Foundation preview/verification controls와 Mini Scanner 별도 위치 편집/초기화 controls는 일반 사용자 UI에 노출하지 않습니다.

## 16. 최근 인식 기록 / 개발자 로그

사용자 activity는 timestamp, mode, OCR text, nearest official candidate, confidence, second score/margin, success/hold, reason을 표시합니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

기록 예:

- structural candidates
- title anchor evidence
- candidate별 OCR pass
- character-policy rejection
- semantic/visual match reason/confidence
- one-shot candidate/selected result
- inventory-context result
- manual catalog-sync counts/outcome
- runtime error metadata

약 2MB에서 회전하며 logging 실패는 Scanner fatal이 아닙니다.

`로그 삭제`는 process memory recent activity와 `scanner.log`, `scanner.log.1`, 최신 in-memory recognition image를 함께 clear합니다.

## 17. Mini Scanner

- MiniMap과 독립 Window/service/settings/lifecycle
- item match 성공 시 item 정보만 표시
- standby/runtime/status text는 overlay에 표시하지 않음
- actual Scanner mode에서는 Tarkov foreground + inventory/stash visual context가 허용될 때만 표시
- uncertain context는 hidden
- test/preview path는 deterministic 검증을 위해 context gate bypass 가능
- Topmost + native HWND_TOPMOST
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- 전체 card rectangular area가 drag hit surface
- Arrow cursor 유지
- drag 종료 위치 저장
- negative multi-monitor coordinate 허용

v1.2.1에서 실제 모드의 inventory/stash context probe는 동시에 최대 1개만 실행합니다. 350ms runtime refresh가 반복돼도 OCR 요청을 queue로 누적하지 않고 최신 snapshot으로 합칩니다. Item/visibility epoch가 바뀌면 이전 probe를 취소하며 늦게 완료된 이전 결과는 표시하지 않습니다. 기존 Korean inventory anchor 2개 이상 요구는 유지합니다.

## 18. Persistence

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/fonts/
```

Scanner display settings schema:

```text
v3 written/current
```

Catalog cache:

```text
v1/v2 readable
v2 written
```

same-directory temp + flush + atomic replacement + last-known-good `.bak` recovery를 사용합니다. Scanner font cache는 source manifest와 generation hash를 별도로 사용합니다.

v1.2.2에서 cache file schema는 변경하지 않았고 cache/network operation의 shared in-memory 적용 순서만 직렬화했습니다.

## 19. 검증 계약

현재 public baseline:

```text
version: v1.2.2 PUBLIC RELEASE / VERIFIED
release source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
final PR CI: 32590303579 — SUCCESS
exact-source release run: 32590701086 — SUCCESS
independent public finalizer: 32607942093 — SUCCESS
256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.2-win-x64.zip
bytes: 80,302,910
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
ProductVersion: 1.2.2+e3925cbc55215c7de0502c9b6b1ff1428d2f272b
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

v1.2.2 regression gate는 기존 v1.2.1 gate에 Scanner catalog operation-ordering regression을 추가합니다.

Release gate:

- Windows Release build
- Scanner Lab v3.8 geometry regressions
- current official catalog matcher regressions
- OCR character-policy tests
- 4,000-item identity/market regressions
- catalog cache load/network refresh concurrency regression
- raw traderPrices / sellFor / avg24hPrice regressions
- published EXE Scanner UI smoke
- settings schema v3/default hotkey smoke
- synthetic inspect-header magnifier exclusion smoke
- Mini Scanner matched-item-only/topmost/no-activate/drag/trader-price smoke
- Main Map / Factory / MiniMap smoke
- win-x64 self-contained single-file package audit
- exact ProductVersion/FIRST_RUN provenance
- Draft re-download/hash/root/ProductVersion verification
- Draft-downloaded EXE smoke
- public/latest + exact tag source verification
- public asset re-download verification
- public-downloaded EXE smoke

실제 최신 Tarkov Borderless E2E는 release blocker가 아니며 사용자 환경에서 계속 검증합니다. 문제 발생 시 `scanner.log`와 `인식 이미지`로 capture → geometry → anchors → ROI → OCR/visual matcher → catalog → presentation → overlay를 분리해 진단합니다.

상세: `docs/SCANNER_TEST_PLAN.md`, `docs/SCANNER_LAB_3_8_REFERENCE.md`, `docs/RELEASE_1.2.0.md`, `docs/RELEASE_1.2.1.md`, `docs/RELEASE_1.2.2.md`.
