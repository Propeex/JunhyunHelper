# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

기준일: 2026-08-21

현재 개발 기준선: **`v1.1.4 RELEASE CANDIDATE`**. 현재 public stable은 v1.1.3이며 최종 public 검증 후 상태 문서를 갱신합니다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — 외부 image decode / PNG normalize
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
| Scanner diagnostics | runtime observation metadata only | `logs/scanner.log(.1)` |
| Map artwork/config/general markers | pinned Map bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable folder |

Game Content update, Program update, User Progress, Scanner catalog은 서로 다른 lifecycle입니다.

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

Scanner는 v1.1.0부터 실제 기능이며 v1.1.3에서 Scanner Lab v3.8 multi-candidate semantic validation이 production 기준으로 복원되었습니다. v1.1.4는 이 recognition architecture를 유지하며 runtime/data/diagnostics를 보강합니다.

### 8.1 구성

```text
ScannerPage / MiniScannerWindow
        │
        ▼
ScannerCoordinator
        │
        ├─ ScannerSettingsService
        ├─ ScannerCatalogService
        ├─ ScannerRuntimeService
        ├─ ScannerItemPresentationService
        └─ ScannerDiagnosticLog
                    │
ScannerRuntimeService│
        ├─ ScannerLab38WindowsVision (capture + structural detector)
        ├─ ScannerWindowsOcrEngine (ko-KR OCR)
        ├─ ScannerCatalogService (semantic identity)
        ├─ ScannerItemPresentationService
        └─ MiniScannerOverlayService
```

### 8.2 Recognition data flow

```text
Tarkov client / display pixels
→ RED-X connected components + rectangle/edge fallback
→ candidate deduplication
→ 최대 8 structural candidates
→ candidate title ROI
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog resolver
→ 필요 시 top-3 deep OCR
→ semantic resolution 성공 candidate 선택
→ Item ID
```

Structural score는 후보 순위이며 Item identity가 아닙니다. exact-first matcher, confidence threshold, top1-top2 margin을 유지합니다.

### 8.3 Runtime stability — v1.1.4

semantic OCR 전에 두 번의 안정 관측을 요구합니다.

```text
frame N candidate GeometrySignature set
∩
frame N+1 candidate GeometrySignature set
!= empty
→ stable hit 누적
```

서로 다른 candidate가 번갈아 나타나는 것만으로 stable 상태가 되지 않습니다. miss/mode/reset에서 signature history를 버립니다.

verified bounds + title signature가 유지되면 OCR은 반복하지 않습니다. 대신 1초마다 presentation snapshot만 재생성해 현재 진행 데이터 변화를 반영합니다.

### 8.4 Item data bridge

```text
Item ID
→ Scanner catalog: official name / market / dimensions / icon URL
→ GameContentCatalog: canonical item
→ ItemsWorkspace: RequiredTotal
→ ScannerItemSnapshot
→ Mini Scanner
```

가격 계약:

- 최고 상점가 = fleaMarket을 제외한 유효 `sellFor.priceRUB` 최댓값
- 플리 평균가 = positive `avg24hPrice`
- 슬롯 = positive `width * height`
- price/slot은 둘 다 유효할 때만 계산

현재 필요한 수량 = `RequiredTotal`; Inventory를 차감한 부족량이 아닙니다.

### 8.5 Icon/cache

scan 중 icon HTTP를 하지 않습니다. 기존 local image-cache만 읽습니다. v1.1.4부터 성공적으로 decode/freeze한 icon은 process-local cache에서 재사용합니다.

### 8.6 Diagnostics

`ScannerDiagnosticLog`는 capture/candidate/OCR/match/selected/runtime metadata를 bounded log로 남깁니다. screenshot/raw pixel buffer는 저장하지 않습니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

최근 인식 activity는 같은 diagnostic stream에서 projection합니다.

`로그 삭제`는 memory activity + `scanner.log` + `.1`을 clear합니다. I/O 실패는 recognition fatal이 아닙니다.

### 8.7 Scanner 금지 경계

- game memory read
- DLL injection
- packet interception
- process-internal data read
- scan-time HTTP
- icon identity

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
- Scanner diagnostic/log deletion failure → Scanner 계속
- Scanner missing market/icon → 해당 표시 field만 omit
- updater validation/replacement failure → current program 보존/rollback 시도

## 13. 검증 구조

Core/Application/Infrastructure 의미는 xUnit으로 검증합니다. WPF/Map/Scanner UI는 실제 published EXE smoke도 사용합니다.

v1.1.4 gate:

1. Windows Release build
2. 247 automated tests
3. Scanner Lab v3.8 geometry/title ROI regression
4. Scanner market regression
5. win-x64 self-contained single-file publish
6. ProductVersion/FIRST_RUN/package audit
7. actual EXE rendered Product UI/Scanner assertions
8. Scanner activity/log 생성 후 `로그 삭제` end-to-end smoke
9. Main Map/Factory/MiniMap smoke
10. normal close/process exit/portable-root cleanliness
11. Draft/public asset checksum/package/ProductVersion verification
12. exact public tag verification
13. public-downloaded EXE smoke

실제 최신 Tarkov Borderless E2E는 release blocker가 아니며 사용자 환경에서 후속 검증합니다.

## 14. 변경 영향 추적

Scanner recognition 변경:

```text
capture/detector
→ candidate geometry/title ROI
→ OCR
→ ScannerCatalogService matcher
→ ScannerRuntimeService stability/selection
→ Item ID
→ presentation
→ Mini Scanner
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
- `docs/PRODUCT.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/DECISIONS.md`
- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/RELEASE_1.1.4.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
