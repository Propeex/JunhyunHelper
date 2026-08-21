# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

기준일: 2026-08-21

현재 기준선: **`v1.1.5 PUBLIC RELEASE / VERIFIED`**. release source는 `3541bab6536ff91a00f394c4f7b03d5cbf112746`이며 Draft/Public 재다운로드와 별도 independent public EXE smoke까지 검증했습니다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — external image decode / PNG normalize / Scanner title-font metadata/rendering
- SharpVectors — SVG Map rendering
- Windows Runtime OCR (`Windows.Media.Ocr`, `ko-KR`)
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

Canonical domain과 deterministic 계산을 소유합니다. Quest availability, future reachability, Needed Items, Inventory cleanup, Scanner structural geometry 및 conservative name matching 같은 순수 규칙은 여기서 계산합니다.

### Application

사용자 유스케이스와 authoritative mutation을 소유합니다. Profile/Quest/Hideout/Items 변경 후 저장과 workspace 재계산을 조정합니다.

### Infrastructure

HTTP/source parsing, Game Content build/validation/activation, SQLite/file persistence, Scanner full-item catalog, program update 같은 I/O 경계를 소유합니다.

### Desktop

WPF shell/pages, presentation cache, Scanner capture/OCR/font recovery/runtime, Map product bridge, startup/update UX를 소유합니다. domain truth를 UI event handler에서 복제하지 않습니다.

## 3. 데이터 권위

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `user.db` |
| Inventory | user quantity + explicit fixed consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | canonical URL에서 검증/normalize한 presentation bytes | `image-cache/` |
| Scanner identity catalog | current full-item source + current Korean translation | `scanner/catalog/` + memory |
| Scanner title-font cache | user's current Tarkov resource asset에서 read-only로 추출한 presentation/recovery font payload | `scanner/fonts/` |
| Scanner diagnostics | runtime observation metadata only | `logs/scanner.log(.1)` |
| Map artwork/config/general markers | pinned Map bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable folder |

Scanner font cache는 **Item identity authority가 아닙니다**. Current official Korean full-item catalog가 identity authority이며 font shape는 OCR recovery evidence입니다.

Game Content update, Program update, User Progress, Scanner catalog/font cache는 서로 다른 lifecycle입니다.

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
→ canonical image prefetch
```

candidate 실패가 active를 덮지 않으며 `user.db`를 건드리지 않습니다.

Current Content schema: v7. Readable: v3~v7.

v1.1.5 image prefetch는 Quest/Hideout/Ammo subset에 제한하지 않고 `GameContentCatalog.Items` 전체 icon을 queue합니다. Scan-time network는 여전히 금지됩니다.

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

Scanner는 v1.1.3에서 복원한 Scanner Lab v3.8 multi-candidate semantic validation을 production structural 기준으로 유지합니다. v1.1.5는 overlay/data reliability와 OCR recovery를 보강하지만 이 identity 구조를 대체하지 않습니다.

### 8.1 Composition

```text
ScannerPage / MiniScannerWindow
        │
        ▼
ScannerCoordinator
        │
        ├─ ScannerSettingsService
        ├─ ScannerCatalogService
        ├─ ScannerItemPresentationService
        ├─ MiniScannerOverlayService
        ├─ ScannerLab38InspectDetector
        └─ OCR composition
             ScannerLab38OcrEngine
             → SerializedScannerOcrEngine
                ├─ Mini Scanner context detector
                └─ FontAwareScannerOcrEngine
                    └─ Scanner title runtime
                         ├─ existing normal/deep OCR
                         ├─ TarkovTitleFontProvider
                         └─ ScannerTitleFontVerifier
```

`ScannerRuntimeService`는 detector/OCR/catalog/presentation/overlay lifecycle을 조정합니다.

### 8.2 Recognition data flow

```text
Tarkov client / display pixels
→ RED-X connected components + rectangle/edge fallback
→ candidate deduplication
→ 최대 8 structural candidates
→ candidate title ROI
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog resolver
→ 필요 시 top-3 Deep OCR
→ 기존 semantic gate 실패 시에만 optional font-aware recovery
→ semantic resolution 성공 candidate 선택
→ Item ID
```

Structural score는 후보 순위이며 Item identity가 아닙니다. exact-first matcher, confidence threshold, top1-top2 margin을 유지합니다.

### 8.3 Candidate/runtime stability

semantic OCR 전에 같은 quantized `GeometrySignature`가 연속 frame candidate set에 존재해야 2-hit 안정화가 완료됩니다.

verified bounds + title signature가 유지되면 OCR은 반복하지 않습니다. 대신 1초마다 presentation snapshot만 재생성해 현재 진행 데이터 변화를 반영합니다.

`SerializedScannerOcrEngine`은 shared `SemaphoreSlim`으로 Item title OCR과 inventory-context OCR의 WinRT 호출을 직렬화합니다.

### 8.4 Font-aware title recovery

상세보기 상단 Item 이름은 현재 `ItemInfoWindowLabels._caption` TextMeshPro text이며 UI font stack은 Bender primary + `Noto Sans CJK KR` Hangul fallback입니다.

Recovery는 failure-only decorator입니다.

1. `FontAwareScannerOcrEngine.ReadTextAsync`는 inner OCR을 그대로 pass-through.
2. `ReadDeepTextAsync`는 existing Deep OCR을 먼저 수행.
3. existing catalog resolver가 success면 원문을 그대로 반환.
4. semantic failure일 때만 `ScannerTitleFontVerifier` 실행.
5. OCR에 가까운 current official-name shortlist를 구성.
6. Bender Regular/Bold + Noto CJK KR fallback으로 이름을 rasterize.
7. observed title ROI를 Otsu/binary mask로 만들고 normalized-scale + tolerant glyph F1 비교.
8. semantic/visual/combined/top1-top2 margin을 모두 통과한 경우만 verified official name을 OCR variant로 추가.
9. existing matcher가 그 exact official name을 Item ID로 해결.

이 구조는 기존 success path의 behavior와 acceptance threshold를 유지합니다.

### 8.5 TarkovTitleFontProvider boundary

Font binary는 release artifact에 포함하지 않습니다.

```text
running EscapeFromTarkov process
→ executable directory
→ EscapeFromTarkov_Data/resources.assets (read-only)
→ raw SFNT signature scan
→ SFNT table bounds validation
→ SKTypeface metadata validation
→ Bender Regular/Bold + Noto Sans CJK KR extraction
→ %LocalAppData%/JunhyunHelper/scanner/fonts/
```

- asset size/parse/metadata validation fail => font recovery unavailable
- failure is nonfatal; OCR-only path remains
- game directory write 없음
- source asset mtime이 cache보다 최신이면 stale cache reuse 금지
- public distribution of Bender binary 없음

### 8.6 Inventory/stash overlay gate

`ScannerInventoryContextDetector`는 Item이 이미 match되어 overlay를 표시하려 할 때만 foreground Tarkov UI를 확인합니다.

```text
foreground EscapeFromTarkov
→ top client strip capture
→ serialized ko-KR OCR
→ current Korean navigation anchors
→ >= 2 independent anchors
→ allow overlay
```

uncertain/missing/other foreground app => hidden. Decision은 약 850ms cache합니다. raw screenshot은 persist하지 않습니다.

### 8.7 Item data bridge

```text
Item ID
→ Scanner catalog: official name / market / dimensions / icon URL
→ GameContentCatalog: canonical item
→ ItemsWorkspace: RequiredTotal
→ ScannerItemSnapshot
→ Mini Scanner
```

가격 계약:

- raw `traderPrices`에 유효 가격이 있으면 positive `priceRUB` 최댓값
- 아니면 `sellFor`의 flea source 제외 positive `priceRUB` 최댓값
- 플리 평균가 = positive `avg24hPrice`
- 슬롯 = positive `width * height`
- price/slot은 둘 다 유효할 때만 계산

catalog health는 >=4,000 valid Item + >=500 valid trader-price coverage를 요구합니다.

현재 필요한 수량 = `RequiredTotal`; Inventory를 차감한 부족량이 아닙니다.

### 8.8 Mini Scanner window/input

- matched-item-only presentation
- WPF Topmost + native HWND_TOPMOST reassert
- no-activate/tool-window extended styles
- whole-card drag hit surface
- forced Arrow cursor
- persisted multi-monitor coordinates
- Scanner settings schema v2 migration으로 icon/trader/trader-per-slot intended default 정상화

### 8.9 Diagnostics

`ScannerDiagnosticLog`는 capture/candidate/OCR/match/selected/runtime/context/font metadata를 bounded log로 남깁니다. screenshot/raw pixel buffer는 저장하지 않습니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

`로그 삭제`는 memory activity + `scanner.log` + `.1`을 clear합니다. I/O 실패는 recognition fatal이 아닙니다.

### 8.10 Scanner 금지 경계

- game memory read
- DLL injection
- packet interception
- process-internal data read
- scan-time HTTP
- icon identity
- font shape만으로 Item ID 확정

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

## 10. Release architecture / evidence

정식 release는 exact source에서 다시 build/test/publish한 뒤 Draft-first로 진행합니다.

v1.1.5 verified public identity:

```text
source/tag: 3541bab6536ff91a00f394c4f7b03d5cbf112746
249 tests passed
asset bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
Draft/public verification: 32495042444
independent public verification: 32495225958
```

상세 release evidence는 `docs/RELEASE_1.1.5.md`가 권위 기록입니다.
