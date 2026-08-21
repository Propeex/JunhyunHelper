# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **`ACTIVE / v1.1.4 PUBLIC RELEASE / VERIFIED`**

기준일: 2026-08-21

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
- 최신 Scanner release record

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

Scanner는 **실제 제품 기능**입니다. v1.1.3에서 Scanner Lab v3.8 recognition 구조를 production에 복원했고 v1.1.4에서 stability/data/diagnostics를 보강했습니다.

## 9.1 제품 경계

```text
screen pixels
→ capture/detector
→ structural candidate set
→ title ROI
→ ko-KR OCR
→ current official Korean full-item semantic resolver
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

### Desktop orchestration/presentation

- `Desktop/Scanner/ScannerCoordinator.cs`
  - Scanner settings, catalog preparation, real/test mode mutual exclusion, runtime lifecycle, display settings.
  - current `ScannerDataContext` provider 연결.
- `Desktop/Scanner/ScannerRuntimeService.cs`
  - capture tick, candidate stability, semantic OCR selection, verified state, presentation refresh, Mini Scanner state.
- `Desktop/Scanner/ScannerItemPresentationService.cs`
  - Item ID를 catalog/GameContent/ItemsWorkspace와 연결해 `ScannerItemSnapshot` 생성.
- `Desktop/Scanner/ScannerLocalIconService.cs`
  - 기존 local image-cache를 read-only로 읽음.
  - v1.1.4 process-local decoded ImageSource cache.
- `Desktop/Scanner/ScannerDiagnosticLog.cs`
  - bounded diagnostic log와 recent user activity projection.
  - v1.1.4 activity/log clear.
- `Desktop/Scanner/ScannerPage.xaml(.cs)`
  - Scanner product tab.
- `Desktop/Scanner/MiniScannerWindow.xaml(.cs)`
  - no-activate Topmost overlay.
- `Desktop/Scanner/MiniScannerOverlayService.cs`
  - Mini Scanner show/standby/hide boundary.

### Capture/OCR

- `Desktop/Scanner/ScannerLab38WindowsVision.cs`
  - Tarkov/display capture와 Scanner Lab v3.8 structural candidate generation.
- `Desktop/Scanner/ScannerWindowsOcrEngine.cs`
  - Windows `ko-KR` OCR와 deep preprocessing.
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

Title ROI 기준은 `docs/SCANNER_LAB_3_8_REFERENCE.md`가 권위입니다.

## 9.5 OCR / matcher

Windows `ko-KR` OCR adaptive scale:

- title height <=14 → 8x
- <=20 → 6x
- else → 4x

first-pass 성공 후보가 없으면 상위 3개 candidate에 deep OCR:

- enlarged original
- high-contrast grayscale
- binary
- inverse binary

Resolver는 OCR full text/individual line/adjacent two-line combination을 current official Korean full-item catalog와 비교합니다.

Exact-first이며 fuzzy confidence/top1-top2 margin을 인식률 때문에 완화하지 않습니다. historical alias를 production에 누적하지 않습니다.

## 9.6 Runtime candidate stability — v1.1.4

`ScannerRuntimeService`는 semantic OCR 전에 stable candidate 2 hits를 요구합니다.

v1.1.4 핵심:

```text
current candidate GeometrySignature set
intersects
previous candidate GeometrySignature set
→ stable hit +1
```

이전에는 서로 다른 candidate가 각 frame에 하나씩만 있어도 presence hit가 누적될 수 있었습니다. 최종 semantic gate가 오탐을 막았지만 불필요한 OCR/state churn을 만들 수 있어 수정했습니다.

miss/mode/reset에서 previous signature set을 clear합니다.

## 9.7 Verified state / OCR 억제 / live presentation refresh

Item이 semantic validation을 통과하면:

- verified bounds
- verified title signature
- Item ID/snapshot

을 유지합니다.

다음 tick에서 가장 가까운 candidate가 geometry distance limit 안이고 title signature가 같으면 OCR을 다시 하지 않습니다.

v1.1.4에서는 이 verified path에서도 **1초마다 presentation snapshot만 재생성**합니다. 따라서 same detail을 열어 둔 동안 Quest/Hideout 변경으로 Needed Items가 변하면 현재 필요한 수량이 갱신됩니다.

presentation snapshot을 만들 수 없게 되면 기존 verified item을 clear하고 fail-closed 상태로 돌아갑니다.

## 9.8 Scanner market data

`ScannerCatalogService` parsing 계약:

```text
BestTraderSellPrice
= sellFor 중 source != fleaMarket이고 priceRUB > 0인 값의 Max

FleaAveragePrice
= avg24hPrice > 0 ? avg24hPrice : null

Slots
= width > 0 && height > 0 ? width * height : 0
```

price/slot은 price와 slots가 모두 유효할 때만 계산합니다.

v1.1.4 tests는 Therapist/Mechanic/fleaMarket이 동시에 있고 flea가 가장 높은 fixture를 사용해 flea가 BestTrader에 섞이지 않는지 검증합니다.

## 9.9 필요한 개수

Scanner는 Needed Items 계산을 복제하지 않습니다.

```text
ScannerDataContext.ItemsWorkspace
→ Plan.NeededItems
→ ItemId lookup
→ RequiredTotal
```

현재 필요한 수량은 `RequiredTotal`입니다. Inventory를 뺀 remaining/shortage가 아닙니다. Item이 NeededItems에 없으면 0입니다.

이 의미를 바꾸려면 Scanner만 수정하면 안 됩니다. Needed Items product contract부터 검토해야 합니다.

## 9.10 Icon

`ScannerLocalIconService`는 `ImageCacheService`의 기존 파일명/hash contract와 동일한 path를 계산하지만 HTTP를 호출하지 않습니다.

v1.1.4에서 stableId+sourceUrl별 frozen `ImageSource`를 memory cache해 verified presentation refresh마다 같은 PNG를 다시 decode하지 않습니다.

## 9.11 Scanner diagnostics / recent activity

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
- candidate-semantic
- OCR text/pass
- matcher result/confidence/second score
- semantic-selected

screenshot/raw pixel buffer를 저장하지 않습니다.

`ocr-result` + `match-result`를 user-facing `ScannerActivityEntry`로 projection하고 기존 bounded log에서 최근 기록을 복원합니다.

### 로그 삭제

`ScannerDiagnosticLog.Clear()`:

1. history hydration을 완료 상태로 고정
2. pending OCR map clear
3. recent activity clear
4. `scanner.log` delete
5. `scanner.log.1` delete
6. `ActivitiesCleared` event

I/O 실패는 bool로 보고하고 Scanner runtime fatal로 확대하지 않습니다. 새 runtime diagnostic이 발생하면 새 log는 다시 생성될 수 있습니다.

`ScannerPage`의 `로그 삭제` 버튼은 recent activity header 우측 상단에 있고 이 API를 호출합니다.

## 9.12 Mini Scanner

- MiniMap과 독립
- Topmost
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- ON standby/result, OFF hidden
- visible 상태 direct left-drag
- drag 종료 위치 저장
- negative monitor coordinates 허용

---

# 10. Scanner 변경 영향 체크리스트

## Detector/geometry 변경

```text
ScannerLab38WindowsVision
→ ScannerInspectCandidate GeometrySignature/TitleSignature/Bounds
→ ScannerRuntimeService stability
→ OCR rate / semantic selection
→ diagnostic geometry-candidates
→ v3.8 geometry regression
→ actual EXE smoke
```

## OCR/matcher 변경

```text
ScannerWindowsOcrEngine
→ ScannerCatalogService.ResolveOcrText
→ confidence/margin
→ candidate search selection
→ activity/log reason
→ matcher regression
```

Confidence threshold/margin을 단순히 인식률 때문에 낮추지 않습니다.

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
- Scanner diagnostic/log-clear failure → Scanner 계속
- updater validation failure → current program untouched
- fatal WPF exception → LocalAppData diagnostic + 종료

Catch-all은 best-effort presentation/recovery cleanup 경계에만 사용하고 domain correctness 오류를 정상값처럼 숨기지 않습니다.

---

# 14. 성능 구조

현재 의도된 재사용:

- UserProfileStore in-memory snapshot cache
- schema initialization once per store instance
- Application workspace reference cache
- Inventory-only mutation 시 future planning basis 재사용
- Items image object/lazy-load 재사용
- image download concurrency 6
- Scanner verified detail OCR 반복 억제
- Scanner process-local decoded icon cache
- Scanner verified presentation은 1초 주기 Item ID 이후 bridge만 refresh

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

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Scanner catalog/detector regression을 검사합니다.

v1.1.4 automated total: **247**.

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
9. Main Map / Factory / MiniMap smoke
10. normal MainWindow close
11. process exit
12. portable root pollution 확인

정식 public release에서는 Draft/public asset re-download + SHA-256 + ProductVersion + actual downloaded EXE smoke를 추가합니다.

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
- OCR 성공률을 위해 matcher confidence/margin 임의 완화
- Scanner historical alias를 production에 무제한 누적
- Scanner scan-time HTTP/icon identity 추가
- Scanner에서 Needed Items/shortage 의미 별도 재계산
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

v1.1.4는 v1.1.3 Scanner의 hardening이므로 PATCH입니다.

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
9. Scanner라면 detector/OCR/matcher/presentation 중 어느 계층인가?
10. failure가 기존 known-good data/program/Item identity를 보존하는가?
11. actual published EXE smoke에 assertion을 추가해야 하는가?
12. 실제 Tarkov에서 남는 불확실성은 diagnostic으로 분리 가능한가?

이 질문에 답할 수 있으면 변경 범위를 대체로 정확히 잡을 수 있습니다.
