# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

기준일: 2026-08-22

현재 공개 기준선은 **`v1.2.0 PUBLIC RELEASE / VERIFIED`**이며, **v1.2.1은 Scanner deterministic 안정성·정확성 하드닝 release candidate**입니다. v1.2.1은 live Tarkov 데이터가 필요한 recognition threshold를 추측해서 변경하지 않습니다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — 외부 image decode, Scanner title-font rendering
- SharpVectors — SVG Map rendering
- Windows x64 portable / self-contained single-file
- 별도 backend 없음
- runtime GPT/AI 없음

## 2. 프로젝트 경계

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned vendor/Tarkov-Helper Map/MiniMap source (limited exception)

JunhyunHelper.Application
  ├─ JunhyunHelper.Core
  └─ Infrastructure storage boundary

JunhyunHelper.Infrastructure
  └─ JunhyunHelper.Core

JunhyunHelper.Core
  └─ WPF/HTTP/SQLite 의존 없음
```

### Core

Canonical domain과 deterministic 계산을 소유합니다. Quest availability, future reachability, Needed Items, Inventory cleanup 같은 제품 의미는 여기서 계산합니다.

### Application

사용자 유스케이스와 authoritative mutation을 소유합니다. Profile/Quest/Hideout/Items 변경 후 저장과 workspace 재계산을 조정합니다.

### Infrastructure

HTTP/source parsing, Game Content build/validation/activation, SQLite/file persistence, Scanner full-item catalog, program update 같은 I/O 경계를 소유합니다.

### Desktop

WPF shell/pages, presentation cache, Scanner capture/OCR/runtime, Map product bridge, startup/update UX를 소유합니다. domain truth를 UI event handler에서 복제하지 않습니다.

## 3. 데이터 권위

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `user.db` |
| Inventory | user quantity + explicit fixed consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | canonical URL에서 검증/normalize한 presentation bytes | `image-cache/` |
| Scanner identity catalog | current full-item source + current Korean translation | `scanner/catalog/` + memory |
| Scanner title font cache | installed Tarkov `resources.assets` read-only extraction | `scanner/fonts/` + generation manifest |
| Scanner diagnostics | runtime observation metadata only | `logs/scanner.log(.1)` |
| Map artwork/config/general markers | pinned Map bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable folder |

Game Content update, Program update, User Progress, Scanner catalog, Scanner local font cache는 서로 다른 lifecycle입니다.

## 4. Startup / composition

```text
App.OnStartup
→ updater apply-mode 처리
→ MainWindow 표시
→ DesktopServices composition
→ profile load
→ selected GameMode content read/recovery/update
→ Quest/Hideout/Items workspace
→ Ammo/Map/Scanner context bridge
→ smoke가 아니면 program update check
```

`DesktopServices`가 non-Map first-party service composition root입니다. MainWindow는 orchestration layer이며 domain rule의 소유자가 아닙니다.

## 5. Game Content 안전 업데이트

```text
online source
→ source format/semantic validation
→ canonical build
→ GameContentValidator
→ candidate content.db
→ SQLite integrity/read-back validation
→ active replacement
→ previous known-good retention
```

candidate 실패가 active를 덮지 않으며 `user.db`를 건드리지 않습니다.

Current Content schema: v7. Readable: v3~v7.

## 6. User Progress / 계산

`GameProfileSnapshot`이 사용자의 authoritative 진행 aggregate입니다.

Quest:

- 서로 다른 prerequisite requirement = AND
- 한 requirement status set = OR
- unsupported/unknown fact는 optimistic unlock하지 않음
- `Indeterminate`를 현재 가능으로 승격하지 않음

Needed Items:

```text
future Quest reachability
+ future Hideout levels
→ fixed/flexible requirements
→ Needed Items / Cleanup protection
```

Scanner의 `현재 필요한 수량`은 이 파이프라인을 다시 구현하지 않고 `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`을 읽습니다.

## 7. Map / MiniMap 경계

Map/MiniMap은 pinned donor source를 제한적으로 compile-link한 독립 subsystem입니다.

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- general marker/artwork/config → Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content DB/hidden global commands/legacy logger는 포함하지 않음
- 구체적 defect/performance 근거 없이 broad refactor하지 않음

## 8. Scanner subsystem

Scanner는 화면 픽셀을 current official Korean item catalog의 Item ID에 연결하는 독립 subsystem입니다. Scanner Lab v3.8 structural candidate architecture를 geometry 기준으로 유지하고, v1.2.0의 title-anchor/character-policy/Tarkov-font recovery 위에 v1.2.1 lifecycle/cache hardening을 적용합니다.

### 8.1 구성

```text
ScannerPage / MiniScannerWindow
        │
        ▼
ScannerCoordinator
├─ ScannerSettingsService
├─ ScannerCatalogService
├─ ScannerRuntimeService
│  ├─ ScannerLab38InspectDetector
│  │  ├─ ScannerDetailGeometryDetector
│  │  └─ ScannerTitleAnchorRefiner
│  ├─ FontAwareScannerOcrEngine
│  │  ├─ SerializedScannerOcrEngine → Windows ko-KR OCR
│  │  ├─ TarkovTitleFontProvider
│  │  ├─ ScannerTitleFontVerifier
│  │  └─ ScannerFullCatalogVisualMatcher
│  └─ ScannerItemPresentationService
└─ MiniScannerOverlayService
   └─ ScannerInventoryContextDetector
```

Title OCR과 inventory-context OCR은 하나의 `SerializedScannerOcrEngine`을 공유합니다. inventory-context detector에는 item-name font-aware recovery를 연결하지 않아 UI anchor OCR과 official item-name recovery가 섞이지 않습니다.

### 8.2 Recognition data flow

```text
Tarkov client / display pixels
→ Scanner Lab v3.8 structural candidates
→ red close/X + long neutral top frame
→ bounded frame-left search-icon lane + magnifier + dark title field/text evidence
→ HEADER_FRAME_LOCKED
→ magnifier-free title ROI
   └─ incomplete header lock은 OCR identity path에 진입하지 않음
→ Windows ko-KR OCR
→ current-catalog character policy
→ official-name semantic resolver
   ├─ 성공: existing OCR result 우선
   └─ 실패/손상: current official catalog 안에서만 Tarkov-font visual recovery
→ conservative confidence + top1/top2 margin
→ Item ID or fail closed
→ ScannerItemPresentationService
→ Mini Scanner
```

Structural score, anchor score, icon 또는 visual similarity 하나만으로 Item identity를 확정하지 않습니다. current official Item ID/name catalog가 identity 권위이며 ambiguous/low-confidence는 미표시합니다.

### 8.3 Geometry / inspect-header lock

- structural floor `0.34` 유지
- RED-X connected component + rectangle/edge fallback은 **detail candidate 생성** 역할
- 동일 quantized `GeometrySignature`의 연속 관측 후 continuous semantic recognition 시도
- title ROI의 수평 ownership은 `ScannerInspectHeaderLock`이 담당
- required evidence: red close/X + long neutral top frame + bounded frame-left search-icon lane + magnifier core/morphology + dark title field + text presence
- first title glyph connected component는 ROI left edge를 결정하지 않음
- `HEADER_FRAME_LOCKED`가 아니면 refiner score를 0.47 이하로 제한
- runtime은 `HEADER_FRAME_LOCKED` + `TitleAnchorScore >= 0.68`을 다시 요구
- incomplete header lock은 Scanner Lab geometry title ROI로 semantic fallback하지 않고 OCR identity를 fail closed
- 실제 2048×1280 상세창 12개 measured geometry를 packaged-EXE synthetic regression에서 재생

### 8.4 OCR / semantic identity

- Windows `ko-KR` OCR primary
- raw OCR은 diagnostics에 유지하고 matcher에는 current-catalog sanitation 후 text를 전달
- current official Korean catalog에서 allowed character/symbol set 파생
- current catalog 밖 punctuation/symbol은 matcher evidence에서 제거
- Korean item-title contract에서 Han ideograph는 invalid OCR evidence
- exact-first + conservative fuzzy threshold + top1/top2 margin
- normalized length >= 7의 정확히 1 edit는 complete current catalog에서 후보가 유일하고 global runner-up과 10%p 이상 벌어질 때만 bounded recovery
- historical alias를 production identity source로 무제한 누적하지 않음
- visual correction은 strict current-catalog evidence가 명확한 경우에만 허용하며 unavailable/error/ambiguous visual은 healthy OCR success를 폐기하지 않음

### 8.5 Tarkov title-font ownership / generation

게임 폰트 바이너리는 public package에 번들하지 않습니다.

```text
running EscapeFromTarkov
→ EscapeFromTarkov_Data/resources.assets (read-only)
→ bounded streaming SFNT discovery
→ local scanner/fonts cache
   ├─ Bender Regular
   ├─ Bender Bold (존재 시)
   └─ Noto Sans CJK KR
→ font-cache.json source manifest
→ actual cached font SHA-256 generation key
→ generation-aware visual templates
```

- `resources.assets` 전체를 하나의 managed byte array로 읽지 않음
- source manifest: full path + length + last-write ticks
- actual Bender/Noto cache bytes 조합으로 generation key 생성
- partial extraction은 manifest가 마지막에 commit되기 전 정상 cache로 인정하지 않음
- source/font generation 변경 시 loaded typeface + rendered template invalidate
- legacy cache freshness는 존재하는 모든 Bender variant와 Noto를 고려
- corrupt/unavailable cache는 visual recovery만 fail-soft로 건너뛰며 primary OCR path를 fatal로 만들지 않음

### 8.6 Visual recovery caches

- OCR-guided title template cache bounded
- full-catalog glyph-mask cache bounded
- full-catalog rendered-aspect cache bounded
- 모든 key에 exact font generation 포함
- generation 변경 시 cache clear

캐시 한도를 넘었다는 이유로 acceptance threshold를 낮추거나 identity 의미를 바꾸지 않습니다.

### 8.7 Runtime / one-shot lifecycle

Continuous path:

- verified bounds + title signature가 유지되면 OCR 반복 억제
- 약 1초 간격으로 presentation snapshot만 갱신해 `RequiredTotal` 등 현재 상태 반영
- miss/mode/reset 시 verified state 폐기

One-shot:

```text
remember requested mode
→ stop continuous loop
→ await actual loop completion
→ one-shot detector/OCR/presentation
→ latest product state 재확인
→ 동일 mode가 여전히 요청될 때만 restart
```

Profile/GameMode monitor는 one-shot gate 획득 후 최신 context를 다시 읽습니다. 따라서 one-shot 중 발생한 stale monitor tick이 이전 profile/mode runtime을 뒤늦게 복구하지 않습니다.

### 8.8 Mini Scanner inventory-context lifecycle

실제 Scanner에서 Mini Scanner는 foreground Tarkov + Korean inventory/stash context가 보수적으로 확인될 때만 표시합니다.

v1.2.1:

- inventory/stash OCR probe 동시 실행 최대 1개
- 350ms 반복 `Show`는 새 OCR queue가 아니라 latest pending snapshot으로 coalesce
- Item/visibility epoch 변경 시 old probe cancel
- 늦게 끝난 old epoch 결과는 화면에 적용하지 않음
- 기존 두 개 이상의 inventory navigation anchor 요구는 유지

### 8.9 Shutdown / resource lifetime

`FontAwareScannerOcrEngine`은 active-operation lease를 사용합니다.

- Dispose 이후 새 OCR/recovery operation 거부
- 이미 실행 중인 operation count 추적
- 마지막 operation이 종료된 뒤 matcher/verifier/font provider의 Skia/font resource 해제
- WPF UI thread에서 Scanner task를 동기 대기하지 않음

### 8.10 Capture allocation

`PrintWindow` visual-content 사전 검사는 locked bitmap의 sparse pixels를 직접 읽습니다. 이 확인만을 위해 1440p/4K 전체 framebuffer를 별도 managed array로 복사하지 않으며 실제 detector에 필요한 normalized BGRA copy만 유지합니다.

### 8.11 Item data bridge

```text
Item ID
→ Scanner catalog: official name / market / dimensions / icon URL
→ GameContentCatalog: canonical item
→ ItemsWorkspace: RequiredTotal
→ ScannerItemSnapshot
→ Mini Scanner
```

가격 계약:

- 최고 상점가 = fleaMarket을 제외한 유효 trader sell price 최댓값
- 플리 평균가 = positive `avg24hPrice`
- 슬롯 = positive `width * height`
- price/slot은 둘 다 유효할 때만 계산
- 현재 필요한 수량 = `RequiredTotal`; Inventory 차감 부족량이 아님

### 8.12 Diagnostics / 금지 경계

`ScannerDiagnosticLog`는 bounded metadata log를 `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`에 기록합니다. 최신 `인식 이미지` 한 장은 process memory에 유지하며 자동 screenshot 저장은 하지 않습니다. 사용자가 명시적으로 `이미지 저장`을 선택한 경우에만 실제 분석 원본 frame을 지정한 PNG로 export합니다; diagnostic overlay는 PNG에 합성하지 않습니다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal data read
- scan-time HTTP
- icon 단독 identity
- live evidence 없이 confidence/margin 완화

## 9. Program update

```text
latest stable check
→ explicit user consent
→ exact ZIP + SHA256SUMS
→ checksum/archive/root validation
→ staging
→ temporary self-copy updater
→ program-owned files transaction
→ restart
```

Program update는 `%LocalAppData%/JunhyunHelper` 사용자 데이터를 교체하지 않습니다.

## 10. Persistence / atomicity

중요한 JSON preference는 same-directory temp + flush + atomic replacement + `.bak` recovery를 사용합니다.

대표 경로:

```text
user.db
content/<mode>/content.db
image-cache/
ammo-favorites.json(.bak)
map-product-settings.json(.bak)
scanner-settings.json(.bak)
scanner/catalog/items-{mode}-ko.json(.bak)
scanner/fonts/font-cache.json
logs/scanner.log(.1)
```

runtime log를 portable release root에 만들지 않습니다.

## 11. 성능 원칙

- immutable/canonical 결과 재사용
- UserProfileStore in-memory snapshot cache
- Items inventory-only mutation에서 future planning basis 재사용
- image download concurrency 제한
- Scanner verified detail OCR 반복 억제
- Scanner icon process-memory decode cache
- Scanner font/visual templates generation-aware bounded cache
- Scanner inventory-context OCR queue 누적 금지
- Scanner `PrintWindow` validation duplicate full-frame allocation 금지
- Scanner presentation refresh는 Item ID 이후 데이터 bridge만 수행
- Map donor는 증거 없이 broad rewrite하지 않음

캐시는 제품 의미를 바꾸면 안 되며 동일 입력의 deterministic 결과 재사용이어야 합니다.

## 12. 오류 격리

- program update network failure → app 계속
- image failure → 해당 image만 누락
- preference save failure → diagnostic, app 계속
- invalid content candidate → known-good active 유지
- unsupported Quest gate → fail-closed/Indeterminate
- Scanner low confidence/ambiguity → no Item ID
- Scanner font extract/render/cache failure → primary OCR path 유지, visual recovery 생략
- Scanner inventory-context uncertainty → overlay hidden
- Scanner diagnostic/log deletion failure → Scanner 계속
- Scanner missing market/icon → 해당 표시 field만 omit
- updater validation/replacement failure → current program 보존/rollback 시도

## 13. 검증 구조

Core/Application/Infrastructure 의미는 xUnit으로 검증합니다. WPF/Map/Scanner UI는 실제 published EXE smoke도 사용합니다.

v1.2.0 public gate:

```text
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439
255 passed / 0 failed / 0 skipped
public-downloaded EXE smoke: SUCCESS
```

v1.2.1 pre-release final static hardening candidate:

```text
CI run: 32539676032 — SUCCESS
255 passed / 0 failed / 0 skipped
Windows Release build: PASS
win-x64 publish/package audit: PASS
actual published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: PASS
graceful shutdown / clean portable root: PASS
```

v1.2.1 final version/FIRST_RUN/docs head는 같은 CI gate를 다시 통과해야 하며, merge 후 exact release-source build와 Draft/Public 재다운로드 검증을 별도로 수행합니다.

실제 최신 Tarkov Borderless E2E는 release blocker가 아니며 사용자 환경에서 후속 검증합니다. live evidence 없이 recognition threshold를 완화하지 않습니다.

## 14. 변경 영향 추적

Scanner recognition 변경:

```text
capture/detector
→ candidate geometry/title ROI
→ OCR / character policy / visual recovery
→ ScannerCatalogService matcher
→ ScannerRuntimeService stability/selection
→ Item ID
→ presentation
→ Mini Scanner inventory-context gate
→ diagnostics/tests/docs
```

Scanner 가격 변경:

```text
json.tarkov.dev item fields
→ ScannerCatalogService parse
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Mini Scanner
→ market regression tests
```

Needed Items 의미 변경:

```text
Quest/Hideout/Profile facts
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RequiredTotal
→ ScannerItemPresentationService
```

Scanner가 이 계산을 독자적으로 복제하지 않습니다.

## 15. 관련 문서

- `docs/STATE.md`
- `docs/CURRENT_STATE.md`
- `docs/PRODUCT.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/DECISIONS.md`
- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/RELEASE_1.2.0.md`
- `docs/RELEASE_1.2.1.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
