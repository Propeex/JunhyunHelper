# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **ACTIVE / v1.7.13 PUBLIC STABLE / MAINTENANCE MODE**  
기준일: 2026-08-27

이 문서는 다음 개발 세션이 대화 기억 없이 저장소만 보고 **현재 구현 위치·책임·데이터 흐름·변경 영향**을 빠르게 복구하기 위한 지도다.

정확한 현재 릴리즈 SHA, CI run, asset hash, 공개 검증 상태는 이 문서에 복제하지 않는다. 반드시 `docs/STATE.md`를 본다.

제품 의미의 권위는 `AGENTS.md`의 **진실의 우선순위**를 따른다. 이 문서는 제품 요구사항을 새로 정의하지 않는다.

유지보수 작업은 `docs/MAINTENANCE_CONTRACTS.md`를 함께 읽는다.

---

# 1. 새 세션 복구 순서

1. `AGENTS.md`
2. `README.md`
3. `docs/STATE.md`
4. `docs/PRODUCT.md`
5. `docs/DECISIONS.md`
6. `docs/MAINTENANCE_CONTRACTS.md`
7. `docs/DEVELOPER_REFERENCE.md`
8. `docs/ARCHITECTURE.md`
9. 작업 영역 전문 문서
10. 관련 code/tests/current PR

Scanner 작업은 추가로:

- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_GROUND_TRUTH.md`

Quest availability 작업은 추가로:

- `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

Game Content update 작업은 추가로:

- `docs/DATA_VALIDATION.md`

Map/MiniMap 작업은 추가로:

- `docs/MAP_PRODUCT_REQUIREMENTS.md`
- `docs/REFERENCE_POLICY.md`

---

# 2. 제품/프로젝트 경계

준현 헬퍼는 Windows x64 .NET 10 WPF desktop application이다.

현재 주요 사용자 기능:

- Profile
- Quest
- Hideout
- Needed Items / Inventory / Cleanup
- Items
- Ammo
- Map / MiniMap
- Game Content update
- Program update
- Scanner / Mini Scanner
- Scanner Ground Truth / diagnostics / regression dataset

Runtime GPT/LLM API는 없다.

프로젝트 의존성:

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned vendor/Tarkov-Helper Map/MiniMap source

JunhyunHelper.Application
  ├─ Core
  └─ Infrastructure storage boundary

JunhyunHelper.Infrastructure
  └─ Core

JunhyunHelper.Core
  └─ WPF/HTTP/SQLite 의존 없음
```

책임:

- **Core** — canonical domain, deterministic 계산, Quest/Needed Items/Scanner pure policy
- **Application** — authoritative user mutation/use case, workspace orchestration
- **Infrastructure** — HTTP/source parsing, content build/validation/storage, SQLite/files, Scanner catalog, Program Update
- **Desktop** — WPF, presentation, Scanner capture/OCR/runtime, Map bridge, startup/update UX
- **vendor Map/MiniMap** — pinned donor source. Map/MiniMap에만 제한적으로 사용

UI event handler에 Core/Application의 domain truth를 복제하지 않는다.

---

# 3. 데이터 소유권과 저장 위치

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated external source → canonical snapshot | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `%LocalAppData%/JunhyunHelper/user.db` |
| Inventory | user quantity + consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | validated/normalized presentation bytes | `%LocalAppData%/JunhyunHelper/image-cache/` |
| Scanner identity/market catalog | current full-item source + official Korean identity | `%LocalAppData%/JunhyunHelper/scanner/catalog/` + memory |
| Scanner settings | hotkeys/display/order/OCR substitution | `%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)` |
| Scanner local font cache | installed Tarkov assets read-only extraction | `%LocalAppData%/JunhyunHelper/scanner/fonts/` |
| Scanner diagnostics / reviewed GT | runtime evidence + user truth | `%LocalAppData%/JunhyunHelper/scanner/diagnostics/` |
| Runtime logs | diagnostic only | `%LocalAppData%/JunhyunHelper/logs/` |
| Map artwork/config/general markers | pinned donor bundle | portable `Assets/` |
| Program files | exact public stable Release | portable product folder |

Game Content update와 Program Update는 서로 다른 lifecycle이다. 둘 다 `user.db`나 reviewed Ground Truth를 덮어쓰면 안 된다.

---

# 4. Startup / lifecycle

대표 흐름:

```text
App.OnStartup
→ fatal exception hooks
→ updater apply-mode
→ LocalAppData diagnostics/log setup
→ Scanner retention setup
→ MainWindow
→ startup Program Update check (non-smoke)

MainWindow.OnInitialized
→ Quest / Hideout / Items / Ammo image-cache binding
→ Ammo favorite-store binding
→ cross-page content navigation wiring
→ Scanner global-command lifetime wiring

MainWindow.Window_Loaded
→ profile/content/workspaces
→ Map bridge
→ Scanner context/catalog/runtime
```

Page-level shared infrastructure는 `ItemsPage`/`HideoutPage`/`AmmoPage`가 우연히 `Loaded`되는 순서에 맡기지 않는다. `MainWindow.OnInitialized`가 product-window lifetime의 composition owner다. 화면 내부의 presentation 초기화는 해당 Page가 직접 소유한다. 예를 들어 Ammo search/detail/grid presentation은 `AmmoPage.OnInitialized`에서 Loaded dispatcher priority로 초기화되며, 부모 `MainWindow`의 page-loaded handler 존재 여부에 의존하지 않는다.

v1.7.13의 user-facing profile/Scanner editor는 `MainWindow.InAppOverlay.cs`의 공통 overlay owner가 표시/닫기 lifetime을 소유한다. 기존 Window/editor content를 overlay로 옮길 때 해당 editor의 `Resources`와 validation/save semantics를 보존한다. X, backdrop click, 같은 launcher 재클릭은 overlay dismissal interaction이며 domain persistence authority가 아니다.

`MainWindow`는 orchestration layer다. 새 domain truth를 여기서 만들지 않는다.

Shutdown에서는 Scanner runtime/OCR/font/retention/background-owned resource가 정상 종료돼야 한다. 실제 published EXE graceful-shutdown smoke가 이 경계를 검증한다.

---

# 5. Profile / Quest / Hideout / Needed Items

## 5.1 Profile

`GameProfileSnapshot`은 GameMode별 authoritative user-progress aggregate다.

주요 fact:

- GameMode / Level / Faction / Edition / Prestige
- Trader LL/standing
- CompletedQuestIds / FailedQuestIds
- SpecialTraderAccessOverrides
- ProfileVariables
- HideoutLevels
- Inventory
- QuestConsumptions / HideoutUpgradeConsumptions

`Infrastructure/Storage/UserProfileStore.cs`가 `user.db` serialization과 in-process snapshot cache를 소유한다.

## 5.2 Quest

`Core/Quests/QuestAvailabilityEvaluator.cs`는 Current/Locked/Completed/Unavailable/Indeterminate를 계산한다.

핵심 계약:

- 서로 다른 task requirement = AND
- requirement 내부 accepted status = OR
- unknown/unsupported fact를 optimistic Current로 만들지 않음
- exact observed ProfileVariable이 있으면 권위값
- audited compatibility만 synthetic fact로 사용

`QuestFutureReachability` 계열은 현재 availability와 미래 Item 필요 가능성을 분리한다. Unknown future path는 보호적으로 다룬다.

## 5.3 Hideout / Needed Items

```text
Profile facts
+ future Quest reachability
+ future Hideout requirements
→ NeededItemRequirementBuilder
→ fixed/flexible split
→ NeededItemCalculator
→ NeededItems / Cleanup protection
```

주요 파일:

- `Core/Items/NeededItemRequirementBuilder.cs`
- `Core/Items/NeededItemCalculator.cs`
- `Core/Items/FutureNeededItemsPlanner.cs`
- `Core/Items/InventoryCleanupChangeDetector.cs`
- `Application/Items/ItemsApplicationService.cs`

Flexible hand-in 실제 제출 Item을 자동 추측하지 않는다.

Inventory-only mutation에서 planning fact가 같으면 future basis를 재사용할 수 있다. 새 profile field가 planning 의미에 영향을 주면 planning-state equality도 같이 검토한다.

---

# 6. Game Content update

주요 구현:

- `Infrastructure/Content/TarkovContentBuildService.cs`
- `Infrastructure/Content/TarkovContentUpdateService.cs`
- `Infrastructure/TarkovJson/TarkovEndpointSourceLoader.cs`
- `Infrastructure/TarkovJson/TarkovGameContentImporter.cs`
- `Infrastructure/TarkovJson/*Importer.cs`
- `Infrastructure/Validation/GameContentValidator.cs`
- `Infrastructure/Validation/GameContentIntegrityValidator.cs`
- `Infrastructure/Validation/ContentUpdateCompletenessGuard.cs`
- `Infrastructure/Storage/ContentSnapshotStore.cs`
- `Infrastructure/Storage/ContentActivationService.cs`

대표 흐름:

```text
TarkovJsonClient / edition source
→ TarkovEndpointSourceLoader
→ TarkovContentBuildService
→ importers
→ canonical / relational validation
→ candidate catalog
→ last-known-good completeness guard
→ candidate content.db
→ SQLite read-back / integrity
→ atomic active replacement
```

핵심 안전 계약:

- candidate 완성 전 active overwrite 금지
- 변환기가 핵심 의미를 이해하지 못하면 fail closed
- failed candidate는 current known-good를 변경하지 않음
- 기존 정상 snapshot이 있으면 핵심 entity/relationship/localization/resource coverage의 **50% 미만 급감**을 suspicious partial payload로 차단
- 이 50%는 절대 Tarkov 데이터 개수 기준이 아님
- baseline이 없으면 상대 수량만으로 첫 candidate를 거부하지 않음
- update failure가 User Progress에 영향 없음

상세 규칙은 `docs/DATA_VALIDATION.md`가 권위다.

Top-level Game Data update는 general content activation 후 current GameMode Scanner catalog/market refresh까지 orchestration한다. Scanner refresh partial failure는 general content success를 rollback하지 않으며 healthy same-mode Scanner cache를 보존할 수 있다.

## 6.1 Offline CI와 Live Data Probe

일반 PR/main CI는 인터넷 상태와 분리된 deterministic gate다.

`.github/workflows/live-data-probe.yml`은 별도 external contract monitor다.

```text
current Regular/PvE remote source
→ canonical build/import
→ current validator
→ Fatal 여부 + source warnings/entity counts
```

Live Probe는 runtime의 last-known-good completeness guard를 대체하지 않는다. 목적이 다르다.

실제 drift를 발견하면 live 한 시점에만 맞는 임시 예외보다 실패 payload/ID를 축소한 deterministic regression fixture를 먼저 남긴다.

---

# 7. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map artwork/config/general markers는 donor bundle을 사용하고 current Quest state/geometry는 JunhyunHelper first-party bridge가 연결한다.

대표 first-party boundary:

- `Desktop/MainWindow.LegacyMapHost.cs`
- `Desktop/MainWindow.ProductLifecycle.cs`
- `Desktop/Map/MapPage.JunhyunUiSimplification.cs` — v1.7.13 marker/settings launcher, trail/hotkey-description product UI simplification
- `Desktop/Map/*`
- `Desktop/Legacy/TarkovHelper/*`
- `Desktop/Quests/QuestPage.MapBridge.cs`
- `Desktop/Quests/QuestPage.MapNavigation.cs`

v1.7.13의 Map UI 정리는 donor source를 직접 수정하지 않는다. donor XAML/control을 first-party partial/customization boundary에서 재구성하므로 donor revision을 바꾸거나 upstream cleanup으로 오해하지 않는다.

이름에 `Legacy`가 있어도 compatibility bridge로 현재 실행 경로에 있을 수 있다. 이름만 보고 dead code로 판단하지 않는다.

Donor code는 concrete defect/performance evidence 없이 broad refactor하지 않는다.

---

# 8. Scanner — 구현 지도

Scanner specialist contract는 `docs/SCANNER.md`다. 아래는 subsystem navigation용 요약이다.

## 8.1 Product boundary

```text
screen pixels
→ capture
→ structural proposals
→ semantic inspect-header validation
→ item-title ROI
→ Windows ko-KR OCR
→ optional user substitution
→ current official catalog sanitation/matching
→ optional strict visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional GT correction
```

금지:

- game memory read / DLL injection / packet interception
- scan-time HTTP identity work
- icon/image-only identity
- current catalog 밖 arbitrary Item 생성
- structural score만으로 identity 확정
- stale/cross-frame OCR evidence를 current identity proof로 사용
- 근거 없는 threshold/candidate-cap 완화

## 8.2 Core Scanner

대표 파일:

- `Core/Scanner/ScannerCatalogItem.cs`
- `Core/Scanner/ScannerRecognition.cs`
- `Core/Scanner/ScannerItemMatcher.cs`
- `Core/Scanner/ScannerObservationPacingPolicy.cs`
- `Core/Scanner/ScannerOcrCharacterPolicy.cs`
- `Core/Scanner/ScannerOcrSubstitution.cs`
- `Core/Scanner/ScannerPresentationJoin.cs`
- `Core/Scanner/ScannerTitleIdentitySignature.cs`

## 8.3 Infrastructure Scanner

`Infrastructure/Scanner/ScannerCatalogService.cs`가 다음을 소유한다.

- GameMode full-item catalog download/cache/load
- official Korean identity catalog
- market/dimension parse
- semantic resolver
- wrong-mode cache fail closed
- `LoadCacheAsync` / `RefreshAsync` writer ordering
- healthy same-mode cache preservation

## 8.4 Desktop Scanner

대표 파일:

- `Scanner/ScannerCoordinator*.cs` — settings/catalog/runtime/profile lifecycle
- `Scanner/ScannerRuntimeService*.cs` — continuous/one-shot recognition lifecycle
- `Scanner/ScannerLab38WindowsVision.cs` — capture/proposal/raw WinRT OCR
- `Scanner/FontAwareScannerOcrEngine.cs` — semantic OCR + constrained font-aware path
- `Scanner/SerializedScannerOcrEngine.cs` — shared OCR serialization + same-cycle exact bitmap reuse
- `Scanner/TarkovTitleFontProvider.cs` — local Tarkov title-font discovery/cache
- `Scanner/ScannerFullCatalogVisualMatcher.cs` — official-catalog-bounded visual recovery
- `Scanner/ScannerItemPresentationService.cs` — confirmed Item ID → mapped presentation
- `Scanner/ScannerRecognitionDebugStore.cs` — latest evidence
- `Scanner/ScannerLatencyTelemetry.cs` — stage latency telemetry
- `Scanner/ScannerPage.xaml(.cs)` — normal surface/search/log/hotkey launcher
- `Scanner/ScannerSettingsWindow.xaml(.cs)` — Mini fields/order + display settings, immediate-save UI
- `Scanner/ScannerHotkeySettingsWindow.xaml(.cs)` — Scanner hotkey editor
- `Scanner/ScannerAdvancedWindow.xaml(.cs)` — Display Test/correction/dataset
- `Scanner/MiniScannerWindow.xaml(.cs)` — no-activate Topmost overlay
- `MainWindow.ScannerItemSources.cs` — searched confirmed item → authoritative NeededItems source presentation/navigation

`SerializedScannerOcrEngine`의 reflection 기반 serialization boundary는 의도적으로 남은 기술 부채다. 단순 cleanup 대상으로 취급하지 않는다.

## 8.5 Recognition safety constants

현재 유지해야 할 검증 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

200 ms는 `ScannerRuntimeService`의 observation target이며 `ScannerObservationPacingPolicy`가 cycle overrun 뒤 missed tick을 back-to-back replay하지 않게 한다. 결정론적 pacing regression은 `ScannerObservationPacingPolicyTests`가 검증한다.

wall-clock benchmark를 CI의 고정 합격선으로 만들지 않는다.

## 8.6 Inspect-header / OCR / matching

Semantic lock은 red close-X, neutral inspect frame/header, magnifier, dark title field, text evidence를 결합한다.

OCR/matching:

- Windows `ko-KR` primary
- normal + bounded deep path
- raw OCR 보존
- user substitution은 단일 ordered pass
- current-catalog-derived character policy
- exact-first
- conservative fuzzy + top1/top2 margin
- bounded unknown/edit recovery
- ambiguous → no Item ID

정상 OCR 성공 경로에 불필요한 luminance 분석/추가 normalization/추가 OCR을 삽입하지 않는다. Adaptive normalization은 miss/bounded deep path에만 사용한다.

## 8.7 Same-cycle reuse / continuity

OCR reuse 조건:

- same active scan cycle
- same pass class
- same dimensions/BPP
- exact pixel SHA-256

Cycle이 바뀌면 current evidence도 바뀐다. cross-frame OCR cache는 없다.

Title continuity signature는 verified detail의 continuity evidence일 뿐 Item identity proof가 아니다.

## 8.8 Presentation / Needed Items

```text
confirmed Item ID
→ Scanner catalog official identity/market/dimensions
→ GameContent canonical item/Wiki
→ ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
→ ScannerItemSnapshot
→ Scanner Page / Mini Scanner
```

`NeededQuantity` 의미는 **보유량 차감 후 현재 부족량**이다.

Scanner presentation이 raw inventory를 다시 빼거나 `RequiredTotal`을 표시하면 안 된다. FIR/일반 소비 의미는 `NeededItemCalculator` 결과를 그대로 재사용한다.

v1.7.13 검색 상세의 Quest/Hideout source도 같은 authoritative `ItemsWorkspace.Plan.NeededItems` row의 `Sources`를 presentation에 join한다. Scanner가 Quest/Hideout requirement를 별도로 재계산하거나 source list를 새 truth로 만들면 안 된다. `RemainingTotal`/`Sources`는 Item ID 확정 뒤 표시 데이터이며 Item identity evidence가 아니다.

Market/dimension failure는 해당 presentation field만 비우고 Item identity를 소급 무효화하지 않는다.

## 8.9 Ground Truth / diagnostics

대표 파일:

- `Scanner/ScannerCandidateGroundTruth.cs`
- `Scanner/ScannerCorrectionWindow.xaml(.cs)`
- `Scanner/ScannerDiagnosticCaseBrowser.cs`
- `Scanner/ScannerDiagnosticCasesWindow.xaml(.cs)`
- `Scanner/ScannerDiagnosticRetentionService.cs`

Correction flow:

```text
full.png
→ auto-fit display
→ candidate direct selection
→ manual/none fallback
→ original-pixel-coordinate truth
→ reviewed Case save
```

Saved Case re-edit는 same Case ID와 existing reviewed data를 보존한다. 복원 실패 시 기존 Case를 덮거나 삭제하지 않는다.

Automatic retention eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Reviewed GT는 자동 삭제하지 않는다.

Support bundle은 GT/source pixel dataset, `user.db`, 게임 계정 정보, 진행/account-identifying 데이터를 포함하면 안 된다.

## 8.10 Scanner change-impact route

Recognition:

```text
capture/proposal
→ semantic header
→ ROI
→ OCR/substitution/character policy/visual
→ catalog matcher
→ verified state
→ Item ID
→ presentation/search
→ Mini overlay
→ GT/diagnostics/replay/smoke
```

Market:

```text
source fields
→ ScannerCatalogService
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Mini Scanner / search
```

Needed count / searched-item sources:

```text
Quest/Hideout/Profile
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RemainingTotal + Sources
→ Scanner presentation/search navigation
```

Settings:

```text
ScannerDisplaySettings migration/normalize
→ Mini display/order / substitutions
→ immediate-save Settings UI

Scanner hotkey fields
→ Hotkey Settings UI
→ same atomic Scanner settings store
```

---

# 9. Ammo / image / preferences

Ammo는 read-only comparison/favorites를 담당한다. Raw stats와 external Wiki Ballistics effectiveness를 분리하고 자체 effectiveness heuristic을 만들지 않는다.

v1.7.13에서 Ammo detail은 기본 접힘이며 published EXE smoke가 `collapsed → expanded → collapsed` 렌더링 왕복을 검증한다. 기본 상태 변경 때문에 smoke가 실패하면 검사를 삭제하지 말고 현재 확정 제품 계약과 실제 rendered state를 먼저 비교한다.

`Desktop/Services/ImageCacheService.cs`는 image byte/dimension을 검증하고 decode 후 normalize한다. 개별 image 실패를 Game Content 전체 실패로 확대하지 않는다.

중요 presentation JSON은 same-directory temp + flush + atomic replace + `.bak` recovery 원칙을 사용한다.

---

# 10. Program Update / package

대표 구현:

- `Infrastructure/Updates/GitHubProgramUpdateClient.cs`
- `Infrastructure/Updates/ProgramUpdateApplier.cs`
- Desktop startup/update UX
- `packaging/New-ReleasePackage.ps1`

Program Update는 public stable Release를 확인하고 검증이 끝나기 전 current product file을 변경하지 않는다. Temporary updater가 program-owned files만 transaction 교체하며 실패 시 current installation을 보호한다.

Stable package contract:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

ZIP/folder 이름은 버전 identity가 아니다. Version identity는 project/ProductVersion/FIRST_RUN/tag/release metadata에 둔다.

정확한 현재 release proof는 `docs/STATE.md`를 사용한다.

---

# 11. Failure isolation

- Program Update network failure → app continues
- image failure → affected image only
- preference save failure → diagnostic, app continues
- invalid/incomplete Game Content candidate → known-good active 유지
- unsupported Quest gate → Indeterminate/fail closed
- Scanner low-confidence/ambiguous evidence → no Item ID
- Scanner visual/font failure → primary OCR contract 유지
- Scanner missing market/icon/dimension → affected presentation field only
- Scanner catalog refresh failure → healthy same-mode cache may remain
- saved Case restore failure → original GT preserved
- corrupt retention metadata → preserve
- updater validation failure → current program untouched

Catch-all은 best-effort presentation/recovery cleanup 경계에서만 사용한다. Domain correctness 오류를 정상값처럼 숨기지 않는다.

---

# 12. 성능 / cache / concurrency

의도된 최적화:

- profile snapshot/workspace cache
- Inventory-only mutation future-basis reuse
- bounded image concurrency/cache
- Scanner verified-detail fast path
- same-cycle exact OCR bitmap reuse
- process-local decoded icon cache
- bounded generation-aware font/visual caches
- Mini inventory OCR single-active/latest coalescing
- Scanner catalog writer serialization
- bounded diagnostic/log retention

원칙:

- deterministic/canonical result만 reuse
- cache가 cross-frame stale evidence를 권위화하면 안 됨
- 성능 변경 전 telemetry/호출 횟수/병목 위치를 확인
- 정확도 threshold 완화는 성능 최적화가 아님
- CI wall-clock threshold는 host noise 때문에 사용하지 않음

Scanner latency stages는 capture/proposal/semantic/OCR/visual/catalog/presentation/end-to-end 단위로 관찰한다.

---

# 13. 주요 first-party file index

## Core

- `Content/GameContentCatalog.cs`
- `Profiles/GameProfileSnapshot.cs`
- `Quests/QuestDefinition.cs`
- `Quests/QuestAvailabilityEvaluator.cs`
- `Quests/QuestFutureReachability.cs`
- `Quests/QuestTaskPoolVariableCompatibility.cs`
- `Hideout/HideoutStation.cs`
- `Items/GameItem.cs`
- `Items/NeededItemRequirementBuilder.cs`
- `Items/NeededItemCalculator.cs`
- `Items/FutureNeededItemsPlanner.cs`
- `Items/InventoryCleanupChangeDetector.cs`
- `Scanner/*`

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
- `TarkovJson/*`
- `Validation/*`
- `Updates/*`
- `Scanner/ScannerCatalogService.cs`

## Desktop

- `Services/*`
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

전문 영역의 세부 ownership은 해당 전문 문서가 이 index보다 우선한다.

---

# 14. Tests / verification

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Quest/Scanner contracts를 검증한다.

테스트 수를 이 문서에 고정하지 않는다. 현재 정확한 suite 결과는 `docs/STATE.md`와 해당 CI run을 본다.

WPF/Map/Scanner interaction은 xUnit만으로 충분하지 않으므로 actual published EXE smoke가 별도로 존재한다.

최소 release gate의 범주는 다음과 같다.

1. Release build
2. full deterministic tests
3. Windows x64 self-contained single-file publish
4. ProductVersion/FIRST_RUN identity
5. package/root/dependency audit
6. actual EXE startup/rendered UI smoke
7. Scanner/Mini Scanner smoke
8. Main Map/Factory/MiniMap smoke
9. graceful shutdown/process exit
10. portable-root pollution check
11. stable package generation/validation
12. exact source/tag/public release verification

외부 Live Data Probe는 이 hermetic release gate와 별개다.

---

# 15. Dead path / cleanup 판단

파일명, 오래된 버전명, `Legacy` 접두사만으로 삭제하지 않는다.

삭제 전 최소 확인:

```text
current code references
+ project/compile links
+ workflow references
+ packaging/release references
+ tests/fixtures
+ docs/history/recovery value
```

WPF에서는 handler 본문이 중복처럼 보여도 routed/class handler 또는 `Loaded` delivery를 간접적으로 유지할 수 있다. v1.7.12 audit에서 부모 page Loaded subscription 제거가 Ammo class-level Loaded initialization 회귀를 드러냈다. 따라서 lifecycle 관련 dead-code 판단은 actual published EXE smoke까지 확인한다.

현재 동작과 historical reproducibility에 가치가 있으면 참조가 적더라도 남길 수 있다.

반대로 obsolete automation이 현재 목적을 수행하지 않고 새 generic path로 완전히 대체되면 제거할 수 있다. 예: version-bound live probe는 long-lived `live-data-probe.yml`로 대체한다.

---

# 16. 하지 말아야 할 것

- 현재 코드가 존재한다는 이유만으로 공식 요구사항으로 승격
- unknown Quest condition을 true/Current로 추측
- missing ProfileVariable을 0으로 간주
- flexible Item 소비 후보 자동 추측
- content update 중 active DB 먼저 overwrite
- Program Update 검증 전 product file 변경
- user.db를 content/program update와 함께 초기화
- Map donor를 cleanup 목적으로 broad rewrite
- Scanner structural score만으로 Item 확정
- Scanner threshold/candidate cap을 인식률 때문에 임의 완화
- Scanner scan-time network/icon identity 추가
- Scanner에서 Needed Items 의미 재계산
- Scanner searched-item Quest/Hideout source를 별도 requirement 계산으로 재구현
- Scanner catalog shared writer synchronization 분리
- user substitution을 automatic global correction table로 승격
- reviewed Ground Truth 자동 삭제
- title continuity signature를 Item identity proof로 사용
- cross-frame OCR cache로 current evidence 대체
- GT saved coordinate를 display scale coordinate로 저장
- saved Case restore failure에서 기존 GT overwrite/delete
- UI event handler에 domain truth 복제
- 외부 네트워크 상태를 일반 PR CI의 mandatory invariant로 만들기

---

# 17. 빠른 영향 분석 질문

1. 이 데이터는 Game Content, User Progress, Scanner catalog, Ground Truth, presentation preference 중 무엇인가?
2. authoritative read/write boundary는 어디인가?
3. 저장 truth인가 계산 result인가?
4. unknown을 false/zero/empty로 바꾸면 안 되는가?
5. Quest current뿐 아니라 future reachability/Needed Items에도 영향이 있는가?
6. consumption ledger/undo에 영향이 있는가?
7. schema compatibility/migration이 필요한가?
8. Map donor인가 JunhyunHelper first-party인가?
9. Scanner라면 capture/proposal/header/ROI/OCR/substitution/visual/catalog/presentation/search/overlay/GT 중 어느 layer인가?
10. shared writer가 둘 이상이면 하나의 ordering boundary가 있는가?
11. failure 시 current known-good data/program/Item identity/GT를 보존하는가?
12. deterministic regression test와 actual EXE smoke 중 무엇이 필요한가?
13. 실제 Tarkov 불확실성을 diagnostics/GT/live probe로 분리할 수 있는가?
14. cache/retention이 current-frame evidence 또는 reviewed truth를 손상할 수 있는가?
15. performance optimization이 threshold 완화/stale reuse를 도입하지 않는가?
16. UI scale/coordinate transform이 original GT pixel coordinate를 보존하는가?
17. package change가 updater archive-root contract와 일치하는가?
18. 변경 후 `STATE`/전문 문서/이 reference 중 무엇을 갱신해야 다음 세션이 오해하지 않는가?
