# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **`ACTIVE / v1.2.2 PUBLIC RELEASE / VERIFIED`**

기준일: 2026-08-23

이 문서는 다음 개발 세션이 대화 기억 없이 저장소만 보고 이어서 작업할 수 있도록 만든 구현 지도입니다. 제품 의미의 최종 권위는 `PRODUCT.md`와 `DECISIONS.md`, 현재 배포 상태의 최종 권위는 `STATE.md`입니다.

---

# 1. 절대 경계

준현 헬퍼는 Windows x64 .NET 10 WPF desktop application입니다.

현재 사용자 기능:

- Profile
- Quest
- Hideout
- Items / Inventory / Needed Items / Cleanup
- Ammo
- Map / MiniMap
- Game Content update
- Program update
- Scanner / Mini Scanner

Runtime GPT/LLM API는 없습니다.

`vendor/Tarkov-Helper`는 일반적인 제품 사양이 아닙니다. Map/MiniMap만 pinned donor revision을 명시적으로 채택한 제한적 예외입니다. Quest/Hideout/Items/Ammo/Scanner/updater의 제품 의미는 JunhyunHelper first-party 코드와 공식 문서가 소유합니다.

---

# 2. 저장소를 읽는 순서

1. `AGENTS.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/DEVELOPER_REFERENCE.md`
6. `docs/ARCHITECTURE.md`
7. `docs/VERSIONING.md`
8. 작업 영역 전문 문서
9. 관련 코드/tests/현재 PR

Scanner 작업이면 반드시 추가로:

- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- 최신 Scanner release record (`docs/RELEASE_1.2.2.md`)

를 먼저 읽습니다.

---

# 3. 프로젝트 의존성

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned vendor Map/MiniMap source

JunhyunHelper.Application
  ├─ Core
  └─ Infrastructure storage boundary

JunhyunHelper.Infrastructure
  └─ Core

JunhyunHelper.Core
  └─ WPF/HTTP/SQLite 의존 없음
```

- **Core**: canonical domain과 deterministic 계산.
- **Application**: authoritative user mutation/use case와 workspace orchestration.
- **Infrastructure**: source/HTTP/SQLite/files/content activation/program update/Scanner catalog.
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime, Map bridge, startup/update UX.

UI에서 Core/Application 의미를 재계산하지 않습니다.

---

# 4. 데이터 권위와 저장 위치

| 데이터 | 권위 | 저장 위치 |
|---|---|---|
| Game Content | validated canonical import | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `%LocalAppData%/JunhyunHelper/user.db` |
| Inventory | user quantity + explicit fixed consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | validated/normalized presentation image | `image-cache/` |
| Scanner Item identity/market catalog | current full-item source + Korean translation | `scanner/catalog/` + memory |
| Scanner font recovery cache | locally discovered Tarkov title fonts + generation manifest | `scanner/fonts/` |
| Scanner diagnostics | runtime observation metadata | `logs/scanner.log(.1)` |
| Map general data/artwork | pinned release Assets | portable `Assets/` |
| Program files | exact GitHub stable release | portable folder |

Game Content update와 Program update는 별개입니다. 둘 다 `user.db`를 덮지 않습니다.

---

# 5. Startup / shell

```text
App.OnStartup
  ├─ fatal exception hooks
  ├─ updater apply-mode 처리
  ├─ MainWindow 표시
  └─ smoke가 아니면 startup program update check

MainWindow.Window_Loaded
  └─ LoadProfilesAsync
      ├─ UserProfileStore
      ├─ selected GameMode content read/recovery/update
      ├─ Quest workspace
      ├─ Hideout workspace
      ├─ Items workspace
      ├─ Ammo context
      ├─ Map bridge
      └─ Scanner context
```

MainWindow는 orchestration layer입니다. 새 domain truth를 MainWindow event handler에 넣지 않습니다.

---

# 6. Profile / Quest / Hideout / Items 핵심 흐름

## Profile

`GameProfileSnapshot`은 GameMode별 authoritative user-progress aggregate입니다.

주요 fact:

- GameMode / Level / Faction / Edition / Prestige
- Trader LL/standing
- CompletedQuestIds / FailedQuestIds
- SpecialTraderAccessOverrides
- ProfileVariables
- HideoutLevels
- Inventory
- QuestConsumptions / HideoutUpgradeConsumptions

`UserProfileStore`가 SQLite `user.db`의 serialization과 in-process snapshot cache를 소유합니다.

## Quest

`QuestAvailabilityEvaluator`:

- Current
- Locked
- Completed
- Unavailable
- Indeterminate

서로 다른 task requirement는 AND, 한 requirement 안 accepted status set은 OR입니다. unknown/unsupported fact를 optimistic `Current`로 만들지 않습니다.

`QuestFutureReachabilityEvaluator`는 현재 가능 여부와 미래 Item 필요 가능성을 분리합니다. future planning에서 unknown은 `IndeterminatePotential`로 보호합니다.

## Hideout

미입력 station은 Lv.0입니다. 미래 upgrade level의 fixed material은 Needed Items에 포함합니다.

## Inventory / Needed Items

```text
future Quest reachability
+ future Hideout requirements
→ NeededItemRequirementBuilder
→ fixed/flexible split
→ NeededItemCalculator
→ NeededItems / Cleanup protection
```

Flexible hand-in의 실제 제출 Item을 자동 추측하지 않습니다.

Inventory-only mutation에서는 planning facts가 같으면 기존 future basis를 재사용합니다. 새 profile field가 planning 의미에 영향을 주면 `ItemsApplicationService.PlanningStateEquals`도 갱신해야 합니다.

---

# 7. Game Content update

대표 흐름:

```text
TarkovJsonClient
→ TarkovEndpointSourceLoader
→ TarkovContentBuildService
→ importers
→ GameContentValidator
→ candidate content.db
→ SQLite integrity/read-back
→ ContentActivationService
→ active content.db
```

핵심 안전 계약:

- candidate 완성 전 active를 덮지 않음
- canonical + relational validation
- previous known-good 유지
- 새 active read 실패 시 previous recovery
- update 실패가 user progress에 영향 없음

Current Content schema v7, readable v3~v7.

---

# 8. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper가 소유하는 주요 bridge:

- `MainWindow.LegacyMapHost.cs`
- `MainWindow.ProductLifecycle.cs`
- `MainWindow.MapSmokeV014.cs`
- `MainWindow.ProductUiLayoutSmoke.cs`
- `Map/GlobalKeyboardHookService.JunhyunProduct.cs`
- `Map/JunhyunExtractMarkerIcon.cs`
- `Quests/QuestPage.MapBridge.cs`
- `Quests/QuestPage.MapNavigation.cs`
- `Legacy/TarkovHelper/LegacyMapHostCompatibility.cs`

Map artwork/config/general marker는 donor bundle, current Quest state/geometry는 JunhyunHelper bridge를 사용합니다. 구체적 defect/performance 근거 없이 donor broad refactor를 하지 않습니다.

---

# 9. Scanner — 전체 구현 지도

Scanner는 **실제 제품 기능**입니다.

버전별 핵심 기준선:

- v1.1.3: Scanner Lab v3.8 multi-candidate structural/semantic recognition 구조 복원
- v1.1.4~v1.1.6: market/Needed Items/diagnostics/icon/catalog-health 보강
- v1.2.0: title anchors, Tarkov-font visual recovery, `인식 이미지`, one-shot high-precision scan
- v1.2.1: font/cache generation, bounded visual caches, one-shot/profile lifecycle, Mini Scanner OCR coalescing, shutdown/capture hardening
- v1.2.2: Scanner catalog disk-load/network-refresh GameMode transition race 제거

## 9.1 제품 경계

```text
screen pixels
→ capture/detector
→ structural candidate set
→ close/magnifier/title refinement
→ ko-KR OCR + catalog character policy
→ current official Korean full-item semantic resolver
   OR conservative Tarkov-font visual recovery
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

오탐보다 미탐을 선호합니다. geometry 하나를 Item identity로 사용하지 않습니다.

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data
- scan-time HTTP
- icon/image identity

## 9.2 핵심 파일과 책임

### Core

- `Core/Scanner/ScannerCatalogItem.cs`
  - Scanner full-item catalog의 Item ID/name/icon/market/dimension snapshot.
- `Core/Scanner/ScannerRecognition.cs`
  - matcher 결과, reason/confidence/second score.

### Infrastructure

- `Infrastructure/Scanner/ScannerCatalogService.cs`
  - GameMode별 full-item catalog download/cache/load.
  - current Korean official names + English fallback.
  - OCR resolver.
  - trader/flea/dimension parse.
  - wrong-mode cache identity fail-closed.
  - v1.2.2: `LoadCacheAsync`와 `RefreshAsync`를 동일 `_refreshGate`로 직렬화하고 cross-mode clear를 gate 안에서 수행.

### Desktop orchestration/presentation

- `Desktop/Scanner/ScannerCoordinator.cs`
  - Scanner settings, catalog preparation, real/test mode mutual exclusion, runtime lifecycle, one-shot/profile lifecycle, display settings.
- `Desktop/Scanner/ScannerRuntimeService.cs`
  - capture tick, candidate stability, title refinement, semantic/visual selection, verified state, presentation refresh, Mini Scanner state.
- `Desktop/Scanner/ScannerItemPresentationService.cs`
  - Item ID를 catalog/GameContent/ItemsWorkspace와 연결해 `ScannerItemSnapshot` 생성.
- `Desktop/Scanner/ScannerLocalIconService.cs`
  - 기존 local image-cache를 read-only로 읽고 decoded frozen `ImageSource`를 process-local cache.
- `Desktop/Scanner/ScannerDiagnosticLog.cs`
  - bounded diagnostic log와 recent user activity projection, activity/log clear.
- `Desktop/Scanner/ScannerRecognitionDebugStore.cs` / `ScannerRecognitionDebugWindow.cs`
  - 최신 in-memory diagnostic frame 1개와 `인식 이미지` UI.
- `Desktop/Scanner/ScannerPage.xaml(.cs)`
  - Scanner product tab.
- `Desktop/Scanner/MiniScannerWindow.xaml(.cs)`
  - no-activate Topmost overlay.
- `Desktop/Scanner/MiniScannerOverlayService.cs`
  - Mini Scanner visibility/context/drag boundary.

### Capture/OCR/font recovery

- `Desktop/Scanner/ScannerLab38WindowsVision.cs`
  - Tarkov/display capture와 Scanner Lab v3.8 structural candidate generation.
- `Desktop/Scanner/ScannerWindowsOcrEngine.cs`
  - Windows `ko-KR` OCR와 deep preprocessing.
- `Desktop/Scanner/SerializedScannerOcrEngine.cs`
  - title/inventory OCR의 shared serialization boundary.
- `Desktop/Scanner/FontAwareScannerOcrEngine.cs`
  - OCR semantic path + constrained font-aware recovery 및 operation lifetime.
- `Desktop/Scanner/TarkovTitleFontProvider.cs`
  - local Tarkov font discovery/cache/source-generation 관리.
- `Desktop/Scanner/ScannerFullCatalogVisualMatcher.cs`
  - current official catalog 범위의 conservative visual recovery.
- `Desktop/Scanner/ScannerContracts.cs` / `ScannerModels.cs`
  - runtime/candidate/capture/presentation contracts.

## 9.3 Capture

Real:

```text
EscapeFromTarkov window
→ Borderless client-area
→ PrintWindow
→ invalid frame이면 exact client screen CopyFromScreen fallback
```

Test:

```text
all connected displays
→ same detector/OCR/catalog/presentation pipeline
```

real/test는 동시에 실행하지 않으며 test mode는 session-only입니다. 둘 다 OFF면 capture/OCR loop가 없습니다.

`PrintWindow` sparse visual validation은 locked bitmap을 직접 검사하며 detector 전에 전체 frame managed copy를 하나 더 만들지 않습니다.

## 9.4 Scanner Lab v3.8 structural detector

```text
RED-X connected components
+
rectangle/edge projection fallback
→ candidates
→ IoU dedup
→ 최대 8
```

RED-X는 anchor이고 outer inspect window를 복원합니다. rectangle/edge path는 RED-X가 없거나 약한 경우 fallback입니다.

Structural score는 final identity가 아닙니다. candidate limit=8, structural floor=0.34 계약을 유지합니다.

Title ROI fallback 기준은 `docs/SCANNER_LAB_3_8_REFERENCE.md`가 권위입니다.

## 9.5 Title anchors / OCR / matcher

Header refinement evidence:

- red close/X
- magnifier/search icon
- dark title field

magnifier가 신뢰되면 실제 title OCR ROI는 magnifier 오른쪽에서 시작합니다. anchor가 불확실하면 Scanner Lab v3.8 geometry ROI로 돌아갑니다.

Windows `ko-KR` OCR adaptive scale:

- title height <=14 → 8x
- <=20 → 6x
- else → 4x

first-pass 성공 후보가 없으면 상위 candidate에 deep OCR을 수행합니다.

Resolver는 OCR full text/individual line/adjacent line combination을 current official Korean full-item catalog와 비교합니다.

Exact-first이며 fuzzy confidence/top1-top2 margin을 인식률 때문에 완화하지 않습니다. historical alias를 production에 누적하지 않습니다.

`ScannerOcrCharacterPolicy`는 current official Korean catalog에서 허용 문자를 파생합니다. catalog에 없는 unexpected character와 Korean item-title contract의 Han ideograph는 corrupted OCR evidence로 다룹니다.

## 9.6 Tarkov-font visual recovery — v1.2.0/v1.2.1

OCR이 비거나 손상된 경우에만 current official item-name universe 안에서 constrained visual recovery를 사용할 수 있습니다.

- Bender + Noto local support
- current catalog 밖 arbitrary Item 생성 금지
- conservative top1 score + top1/top2 margin
- OCR semantic success가 있으면 visual recovery가 덮어쓰지 않음
- game font binary는 배포하지 않음

v1.2.1 generation/lifetime 계약:

- `resources.assets` bounded streaming SFNT discovery
- source path/length/last-write manifest
- actual cached Bender/Noto SHA-256 generation key
- generation-aware bounded render/mask/aspect caches
- source generation 변경 시 stale loaded/rendered cache 폐기
- active-operation lease 종료 뒤 Skia/font disposal

## 9.7 Runtime candidate stability / verified state

Semantic OCR 전에 동일 quantized `GeometrySignature`가 연속 관측되어야 continuous path가 안정화됩니다. miss/mode/reset에서 previous signature set을 clear합니다.

Item이 semantic/visual validation을 통과하면:

- verified bounds
- verified title signature
- Item ID/snapshot

을 유지합니다.

다음 tick에서 candidate geometry와 title signature가 유지되면 OCR을 다시 하지 않습니다. verified path에서도 약 1초마다 presentation snapshot만 재생성하여 Quest/Hideout 변경으로 `RequiredTotal`이 달라지면 같은 detail을 열어 둔 상태에서도 표시를 갱신합니다.

presentation snapshot을 만들 수 없게 되면 기존 verified item을 clear하고 fail-closed 상태로 돌아갑니다.

## 9.8 Scanner catalog operation ordering — v1.2.2

`ScannerCatalogService`의 두 writer:

```text
network RefreshAsync(mode)
local   LoadCacheAsync(mode)
```

둘 다 다음 shared state를 교체할 수 있습니다.

- loaded mode
- generated timestamp
- Item dictionary
- semantic matcher catalog
- OCR character-policy catalog
- diagnostics

따라서 둘은 동일 `_refreshGate`를 사용합니다. `RefreshAsync`의 cross-mode clear도 gate 획득 뒤 수행합니다.

금지되는 상태 순서:

```text
old-mode refresh starts
→ new-mode cache load applies
→ old-mode refresh finishes
→ old mode becomes final state
```

`ScannerCatalogConcurrencyTests`가 이 ordering을 의도적으로 재현하고 newer mode cache load가 final writer가 되는지 검증합니다.

## 9.9 Scanner market data

`ScannerCatalogService` parsing 계약:

```text
BestTraderSellPrice
= valid non-flea RUB sale price Max

FleaAveragePrice
= avg24hPrice > 0 ? avg24hPrice : null

Slots
= width > 0 && height > 0 ? width * height : 0
```

raw `traderPrices`와 derived `sellFor`를 모두 지원합니다. price/slot은 price와 slots가 모두 유효할 때만 계산합니다.

시장 coverage는 Item identity catalog health와 분리합니다. market/dimension 누락은 해당 표시 필드만 fail closed합니다.

## 9.10 필요한 개수

Scanner는 Needed Items 계산을 복제하지 않습니다.

```text
ScannerDataContext.ItemsWorkspace
→ Plan.NeededItems
→ ItemId lookup
→ RequiredTotal
```

현재 필요한 수량은 `RequiredTotal`입니다. Inventory를 뺀 remaining/shortage가 아닙니다. Item이 NeededItems에 없으면 0입니다.

이 의미를 바꾸려면 Scanner만 수정하면 안 됩니다. Needed Items product contract부터 검토해야 합니다.

## 9.11 Icon

`ScannerLocalIconService`는 `ImageCacheService`의 기존 파일명/hash contract와 동일한 path를 계산하지만 HTTP를 호출하지 않습니다.

stableId+sourceUrl별 frozen `ImageSource`를 memory cache해 verified presentation refresh마다 같은 PNG를 다시 decode하지 않습니다.

## 9.12 Scanner diagnostics / recent activity

파일:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

약 2MB에서 1회 회전합니다.

기록 예:

- runtime-start/stop/error
- status
- geometry-candidates
- title-anchor evidence
- OCR text/pass
- matcher/visual result/confidence/second score
- semantic-selected
- inventory context
- catalog sync outcome/counts

screenshot/raw pixel buffer를 저장하지 않습니다. `인식 이미지`는 process memory의 최신 frame 1개만 유지합니다.

### 로그 삭제

`ScannerDiagnosticLog.Clear()`는 history hydration/pending OCR/recent activity/current/rotated log를 정리하고 `ActivitiesCleared`를 발생시킵니다. Scanner Page는 최신 in-memory recognition image도 함께 clear합니다.

I/O 실패는 bool/diagnostic으로 보고하고 Scanner runtime fatal로 확대하지 않습니다. 새 runtime diagnostic이 발생하면 새 log는 다시 생성될 수 있습니다.

## 9.13 Mini Scanner

- MiniMap과 독립
- Topmost + native HWND_TOPMOST
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- matched Item 결과만 표시
- 실제 mode에서는 Tarkov foreground + inventory/stash context fail-closed gate
- visible 상태 direct left-drag
- drag 종료 위치 저장
- negative monitor coordinates 허용

v1.2.1부터 inventory/stash OCR probe는 동시에 하나만 실행하며 반복 요청은 latest snapshot으로 coalesce하고 stale epoch 결과를 폐기합니다.

---

# 10. Scanner 변경 영향 체크리스트

## Detector/geometry 변경

```text
ScannerLab38WindowsVision
→ ScannerInspectCandidate GeometrySignature/TitleSignature/Bounds
→ ScannerRuntimeService stability
→ title-anchor refinement
→ OCR rate / semantic selection
→ diagnostic geometry-candidates
→ v3.8 geometry regression
→ actual EXE smoke
```

## OCR/matcher/visual 변경

```text
ScannerWindowsOcrEngine / FontAwareScannerOcrEngine
→ ScannerOcrCharacterPolicy
→ ScannerCatalogService.ResolveOcrText
→ ScannerFullCatalogVisualMatcher
→ confidence/margin
→ candidate selection
→ activity/log reason
→ matcher/visual regression
```

Confidence threshold/margin을 단순히 인식률 때문에 낮추지 않습니다.

## Catalog load/refresh 변경

```text
profile GameMode
→ ScannerCoordinator catalog preparation/context
→ ScannerCatalogService LoadCacheAsync / RefreshAsync
→ shared _refreshGate
→ ReplaceData/ClearForMode
→ matcher + OCR character-policy catalog
→ Scanner runtime Item identity/market state
```

mode transition operation ordering regression을 반드시 유지합니다.

## 가격 변경

```text
source items fields
→ ScannerCatalogService parser
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Mini Scanner
→ market regression
```

## 현재 필요한 수량 변경

```text
Quest/Hideout/Profile
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RequiredTotal
→ ScannerItemPresentationService
```

Scanner에 별도 shortage 계산을 넣지 않습니다.

## Scanner UI 변경

`ScannerPage.xaml(.cs)` 변경 뒤 `MainWindow.ProductUiLayoutSmoke.cs`에 실제 rendered contract assertion이 필요한지 검토합니다.

---

# 11. Ammo / image / preference persistence

Ammo는 read-only comparison/favorites를 담당합니다. Ammo raw stats와 Wiki Ballistics effectiveness를 분리하고 자체 effectiveness heuristic을 만들지 않습니다.

`ImageCacheService`는 최대 byte/dimension을 검증하고 decode 후 PNG normalize합니다. 개별 image 실패는 Game Content update 전체 실패가 아닙니다.

Presentation JSON은 `AtomicJsonFileStore` 계열 same-directory temp + flush + atomic replace + `.bak` recovery를 사용합니다. Scanner settings/catalog도 동일한 last-known-good 원칙을 따릅니다.

---

# 12. Program update

`GitHubProgramUpdateClient`는 `Propeex/JunhyunHelper` latest public stable을 확인합니다.

대상:

- non-draft / non-prerelease
- strict `vMAJOR.MINOR.PATCH`
- current assembly보다 newer
- exact Windows ZIP + `SHA256SUMS.txt`

검증 전 current product file을 건드리지 않습니다.

Updater는 TEMP self-copy runner에서 parent 종료 후 program-owned files만 transaction 교체하고 실패 시 rollback/restart를 시도합니다.

---

# 13. 오류 격리

- program update check network failure → app 계속
- image failure → 해당 image만 누락
- preference save failure → diagnostic, app 계속
- invalid Game Content candidate → active known-good 유지
- unsupported Quest availability → Indeterminate/fail-closed
- Scanner missing/ambiguous OCR → no Item ID
- Scanner missing icon/price → 해당 표시만 누락
- Scanner catalog refresh/cache failure → same-mode healthy known-good가 있으면 보존, mode identity는 fail-closed
- Scanner diagnostic/log-clear failure → Scanner 계속
- updater validation failure → current program untouched
- fatal WPF exception → LocalAppData diagnostic + 종료

Catch-all은 best-effort presentation/recovery cleanup 경계에만 사용하고 domain correctness 오류를 정상값처럼 숨기지 않습니다.

---

# 14. 성능 구조

현재 의도된 재사용/제한:

- UserProfileStore in-memory snapshot cache
- schema initialization once per store instance
- Application workspace reference cache
- Inventory-only mutation 시 future planning basis 재사용
- Items image object/lazy-load 재사용
- image download concurrency 6
- Scanner verified detail OCR 반복 억제
- Scanner process-local decoded icon cache
- Scanner verified presentation은 약 1초 주기 Item ID 이후 bridge만 refresh
- Scanner OCR-guided/full-catalog visual caches bounded + font-generation aware
- Mini Scanner inventory OCR single-active + coalesced
- Scanner catalog shared writer operations serialized

동일 입력의 deterministic 결과를 재사용해야 하며 캐시가 제품 의미를 바꾸면 안 됩니다.

---

# 15. 주요 first-party 파일 색인

## Core

- `Content/GameContentCatalog.cs`
- `Profiles/GameProfileSnapshot.cs`
- `Quests/QuestDefinition.cs`
- `Quests/QuestAvailabilityEvaluator.cs`
- `Quests/QuestFutureReachability.cs`
- `Hideout/HideoutStation.cs`
- `Items/GameItem.cs`
- `Items/NeededItemRequirementBuilder.cs`
- `Items/NeededItemCalculator.cs`
- `Items/FutureNeededItemsPlanner.cs`
- `Items/InventoryCleanupChangeDetector.cs`
- `Scanner/ScannerCatalogItem.cs`
- `Scanner/ScannerRecognition.cs`

## Application

- `Profiles/ProfileApplicationService.cs`
- `Quests/QuestApplicationService.cs`
- `Hideout/HideoutApplicationService.cs`
- `Items/ItemsApplicationService.cs`
- `Items/FixedInventoryConsumptionPolicy.cs`

## Infrastructure

- `Storage/UserProfileStore.cs`
- `Storage/ContentSnapshotStore.cs`
- `Storage/ContentActivationService.cs`
- `Storage/AtomicJsonFileStore.cs`
- `Content/TarkovContentBuildService.cs`
- `Content/TarkovContentUpdateService.cs`
- `TarkovJson/*Importer.cs`
- `Validation/GameContentValidator.cs`
- `Updates/GitHubProgramUpdateClient.cs`
- `Updates/ProgramUpdateApplier.cs`
- `Scanner/ScannerCatalogService.cs`

## Desktop

- `Services/DesktopServices.cs`
- `Services/ImageCacheService.cs`
- `App.xaml.cs`
- `MainWindow*.cs`
- `Profiles/*`
- `Quests/*`
- `Hideout/*`
- `Items/*`
- `Ammo/*`
- `Map/*`
- `Legacy/TarkovHelper/*`
- `Scanner/*`

Scanner first-party detail은 이 문서 9절과 `docs/SCANNER.md`가 권위입니다.

---

# 16. 테스트 구조

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Scanner catalog/detector/OCR/lifecycle/concurrency regression을 검사합니다.

v1.2.2 automated total: **256**.

Desktop WPF/Map/Scanner interaction은 xUnit만으로 충분하지 않으므로 CI에서 실제 publish된 EXE를 실행합니다.

Release candidate smoke:

1. Release build
2. full tests
3. Windows x64 self-contained single-file publish
4. ProductVersion / FIRST_RUN exact identity
5. root layout / PDB / legacy dependency audit
6. actual EXE startup
7. rendered Product UI assertions
8. Scanner activity/log 생성 후 `로그 삭제` button 실제 click/clear
9. Scanner one-shot/title-anchor/Mini Scanner contract assertions
10. Main Map / Factory / MiniMap smoke
11. normal MainWindow close
12. process exit
13. portable root pollution 확인

정식 public release에서는 Draft/public asset re-download + SHA-256 + ProductVersion + actual downloaded EXE smoke를 추가합니다.

v1.2.2에서는 독립 public finalizer까지 수행하여 public/latest, exact tag, asset set, checksum, package identity와 public-downloaded EXE smoke를 재검증했습니다.

---

# 17. 하지 말아야 할 것

- 현재 구현을 자동으로 제품 요구사항이라고 간주
- unknown Quest condition을 true/Current로 추측
- missing profile variable을 0으로 간주
- flexible Item 소비 후보 자동 추측
- content update 중 active DB 먼저 덮기
- program update 검증 전 product file 변경
- user.db를 content/program update와 함께 초기화
- Map donor를 style cleanup 목적으로 broad rewrite
- Scanner structural score만으로 Item 확정
- OCR 성공률을 위해 matcher/visual confidence/margin 임의 완화
- Scanner historical alias를 production에 무제한 누적
- Scanner scan-time HTTP/icon identity 추가
- Scanner에서 Needed Items/shortage 의미 별도 재계산
- Scanner catalog shared state writer를 서로 다른 synchronization boundary로 분리
- UI event handler에 domain truth 복제

---

# 18. 의도적으로 남은 범위

- 최신 Tarkov Borderless Scanner live E2E: public release blocker가 아니며 실제 사용자 환경에서 후속 검증/튜닝
- EFT 1.0 Story Chapters: ordinary task source 밖
- 일부 profile-variable/task-pool drift: exact evidence 없으면 fail-closed
- code signing / installer 없음
- pinned Map donor의 legacy warning/debt: 구체적 문제 없이 cleanup 리팩터링하지 않음

---

# 19. 버전 / 릴리즈

권위: `docs/VERSIONING.md`.

- 새 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/bug/performance/stability → PATCH +1

최근 Scanner 계열:

- v1.2.0: 사용자 기능 추가 → MINOR
- v1.2.1: deterministic runtime/cache/capture/resource hardening → PATCH
- v1.2.2: catalog GameMode transition concurrency defect → PATCH

현재 public release:

```text
v1.2.2
source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
final PR CI: 32590303579
exact-source release run: 32590701086
independent public finalizer: 32607942093
256 passed / 0 failed / 0 skipped
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
```

릴리즈 시 다음 identity가 일치해야 합니다.

- project `<Version>`
- published ProductVersion
- `FIRST_RUN_KO.txt`
- GitHub tag
- ZIP filename
- release notes

정식 release는 Draft asset을 실제 다운로드/검증/smoke한 뒤 public/latest로 전환합니다.

---

# 20. 빠른 영향 분석 질문

1. 이것은 Game Content, User Progress, Scanner identity catalog, presentation preference 중 무엇인가?
2. authoritative write/read boundary는 어디인가?
3. 저장 truth인가 계산 결과인가?
4. unknown을 false/zero로 바꾸면 안 되는가?
5. Quest current뿐 아니라 future reachability/Needed Items에도 영향이 있는가?
6. consumption ledger/undo에 영향이 있는가?
7. schema compatibility가 필요한가?
8. Map donor인가 first-party인가?
9. Scanner라면 capture/detector/anchor/OCR/visual/catalog/presentation/overlay 중 어느 계층인가?
10. shared state writer가 둘 이상이면 operation ordering이 하나의 synchronization boundary에서 보장되는가?
11. failure가 기존 known-good data/program/Item identity를 보존하는가?
12. actual published EXE smoke에 assertion을 추가해야 하는가?
13. 실제 Tarkov에서 남는 불확실성은 diagnostic으로 분리 가능한가?

이 질문에 답할 수 있으면 변경 범위를 대체로 정확히 잡을 수 있습니다.

## v1.3.0 Scanner implementation addendum

Relevant implementation: `ScannerRecognitionDebugWindow`, `ScannerCoordinator.OneShot`, `ScannerRuntimeService.OneShot`, `ScannerGlobalHotkeyService`, `ScannerHotkeySettingsWindow`, `ScannerDisplaySettings`, `MainWindow.ProductLifecycle`, `ScannerPage`, `ScannerPage.MiniScannerSmoke`, `MainWindow.ProductUiLayoutSmoke`. Full responsibility/impact map: `docs/V1.3.0_PROJECT_DELTA.md`.
