# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **ACTIVE EVERGREEN IMPLEMENTATION REFERENCE / MAINTENANCE MODE**  
기준일: 2026-09-04

이 문서는 다음 개발 세션이 대화 기억 없이 저장소만 보고 **현재 구현 위치·책임·데이터 흐름·변경 영향**을 빠르게 복구하기 위한 지도다.

정확한 현재 릴리즈 SHA, CI run, asset hash, 공개 검증 상태는 이 문서에 복제하지 않는다. 반드시 `docs/STATE.md`를 본다.

제품 의미의 권위는 `AGENTS.md`의 **진실의 우선순위**를 따른다. 이 문서는 제품 요구사항을 새로 정의하지 않는다.

유지보수 작업은 `docs/MAINTENANCE_CONTRACTS.md`를 함께 읽는다.

---

# 1. 새 세션 복구 순서

1. `AGENTS.md`
2. `docs/PROJECT_STATE.json`
3. `docs/ACTIVE_WORK.md`
4. `README.md`
5. `docs/CURRENT_STATE.md`
6. `docs/STATE.md`
7. `docs/PRODUCT.md`
8. `docs/DECISIONS.md`
9. `docs/MAINTENANCE_CONTRACTS.md`
10. `docs/DEVELOPER_REFERENCE.md`
11. `docs/ARCHITECTURE.md`
12. 작업 영역 전문 문서 + 관련 code/tests/current PR/CI

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

현재 public product release 사실이 필요한 작업이면 특정 과거 릴리즈 파일을 하드코딩하지 않는다.

- current release identity: `docs/PROJECT_STATE.json`
- current release evidence: `docs/CURRENT_STATE.md` / `docs/STATE.md`
- 해당 current version의 `docs/.release-vX.Y.Z-status.json` / `docs/RELEASE_NOTES_VX.Y.Z.md`

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

기존 `Propeex/Tarkov-Helper`는 incomplete prototype이며 현재 제품 의미의 권위가 아니다. 현재 donor는 `SIGDrone/Tarkov-Helper`의 pinned Map/MiniMap revision이다.

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

Game Content Update와 Program Update는 서로 다른 lifecycle이다. 둘 다 `user.db`, reviewed Ground Truth, Map/MiniMap/Ammo/Scanner mutable settings를 덮어쓰면 안 된다.

---

# 4. Startup / lifecycle / shared UI ownership

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

Page-level shared infrastructure는 `ItemsPage`/`HideoutPage`/`AmmoPage` 등이 우연히 `Loaded`되는 순서에 맡기지 않는다. `MainWindow.OnInitialized`가 product-window lifetime의 composition owner다.

화면 내부 presentation 초기화는 해당 Page가 직접 소유한다. 예를 들어 Ammo search/detail/grid presentation은 `AmmoPage.OnInitialized`에서 Loaded dispatcher priority로 초기화되며 부모 `MainWindow`의 page-loaded handler 존재 여부에 의존하지 않는다.

## 4.1 v1.7.14 MainWindow shared in-app overlay

대표 구현:

- `Desktop/MainWindow.InAppOverlay.cs`
- `Desktop/InAppOverlayDialog.cs`
- `Desktop/Profiles/ProfileEditorWindow.ProductOverlayStyle.cs`
- `Desktop/Scanner/ScannerSettingsWindow.xaml(.cs)`
- `Desktop/Scanner/ScannerAdvancedWindow.xaml(.cs)`
- `Desktop/Map/MapPage.JunhyunUiSimplification.cs`

현재 shared overlay surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

Window-backed editor:

```text
launcher
→ MainWindow.ToggleInAppWindowAsync(key, window)
→ window Content를 shared card에 host
→ existing resources / child semantics 유지
```

Existing visual-tree UIElement:

```text
caller detach
→ MainWindow.ShowInAppElementAsync(key, title, element, size)
→ shared card에 host
→ completion 후 caller가 original parent/index 복원
```

공통 dismiss interaction:

- same launcher 재클릭
- backdrop click
- common overlay X

`IInAppOverlayDialog` child는 dismiss 요청 시 자체 validation/save/cancel 의미를 중재할 수 있다. MainWindow overlay는 표시/닫기 lifecycle owner이지 domain/persistence authority가 아니다.

Map Settings는 donor `SettingsPanel`을 임시 re-parent하므로 `MapPage.JunhyunUiSimplification.cs`가 original parent/index 복원을 소유한다.

동일 key overlay가 열려 있을 때 launcher를 다시 누르면 `DismissInAppOverlay(key)` 또는 `IsInAppOverlayOpen(key)` 기반으로 닫는다. 같은 surface를 별도 top-level Window로 동시에 열지 않는다.

`MainWindow`는 orchestration/presentation owner다. 새 domain truth를 여기서 만들지 않는다.

## 4.2 Shutdown

Shutdown에서는 Scanner runtime/OCR/font/retention/background-owned resource가 정상 종료돼야 한다. Actual published EXE graceful-shutdown smoke가 이 경계를 검증한다.

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

`UserProfileStore.LoadAsync`는 첫 authoritative load/save 뒤 immutable in-process snapshot을 재사용한다. Workspace 코드에 `LoadAsync`가 여러 번 보인다는 이유만으로 SQLite 병목으로 판정하지 않는다. 실제 runtime trace가 필요하다.

Profile Edit presentation은 MainWindow shared overlay를 사용하지만 validation/save authority는 기존 Profile editor/service가 유지한다.

## 5.2 Quest

`Core/Quests/QuestAvailabilityEvaluator.cs`는 Current/Locked/Completed/Unavailable/Indeterminate를 계산한다.

핵심 계약:

- 서로 다른 task requirement = AND
- requirement 내부 accepted status = OR
- unknown/unsupported fact를 optimistic Current로 만들지 않음
- exact observed ProfileVariable이 있으면 권위값
- audited compatibility만 synthetic fact로 사용

`QuestFutureReachability` 계열은 current availability와 future Item need possibility를 분리한다. Unknown future path는 보호적으로 다룬다.

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

Scanner presentation은 이 계산을 재구현하지 않는다.

```text
ItemsWorkspace.Plan.NeededItems[itemId]
├─ RemainingTotal → Scanner/Mini Scanner current needed
└─ Sources        → Scanner searched-item Quest/Hideout source rows
```

---

# 6. Game Content Update

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
- collection schema drift는 fail closed
- Wiki Ballistics enrichment는 fail-soft
- update failure가 User Progress에 영향 없음

상세 규칙은 `docs/DATA_VALIDATION.md`가 권위다.

Top-level Game Data Update는 general content activation 후 current GameMode Scanner catalog/market refresh까지 orchestration한다. Scanner refresh partial failure는 general content success를 rollback하지 않으며 healthy same-mode Scanner cache를 보존할 수 있다.

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

# 7. Items / Ammo / common search interaction

## 7.1 Items

Items는 current content + current profile + `ItemsWorkspace.Plan`을 presentation한다.

v1.7.13부터 Quest/Hideout purpose selector는 active product filter가 아니다. `All` canonical needed basis를 사용한다.

Cross-navigation은 Quest / Hideout / Ammo 등 기존 content navigation boundary를 사용한다.

## 7.2 Ammo

Ammo는 read-only comparison/favorites를 담당한다. Raw stats와 external Wiki Ballistics effectiveness를 분리하고 자체 effectiveness heuristic을 만들지 않는다.

Presentation lifecycle은 page-owned다.

```text
AmmoPage.OnInitialized
→ DispatcherPriority.Loaded
→ search/detail presentation init
→ grid presentation init
```

v1.7.13부터 detail은 session initial collapsed다. Actual published EXE smoke가 `collapsed → expanded → collapsed` 왕복을 검증한다.

v1.7.14 popup true-toggle:

대표 구현:

- `Desktop/Ammo/AmmoPage.PopupToggleFixes.cs`

WPF `Popup.StaysOpen=False`는 launcher Preview 단계에서 popup을 먼저 닫을 수 있고, 기존 Button Click handler가 이어서 다시 열 수 있다. 따라서 이미 열린 popup의 launcher 재클릭은 `OnPreviewMouseDown`에서:

1. target popup을 닫고
2. routed event를 handled 처리하고
3. 기존 Click reopen path까지 진행하지 않는다.

Timer/delay로 우회하지 않는다.

## 7.3 Product search clear affordance

대표 구현:

- `Desktop/Controls/ProductSearchClearButtonBehavior.cs`
- Ammo explicit attachment in product search setup
- Scanner explicit attachment in `ScannerPage.ProductUsability.cs`

Quest / Hideout / Items / Ammo / Scanner 주요 검색창은 입력창 오른쪽 내부 `×`를 사용한다.

이 behavior는 검색어를 clear하고 focus를 유지하는 presentation helper다. 각 feature의 filtering/domain logic을 새로 소유하지 않는다.

---

# 8. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map artwork/config/general markers는 donor bundle을 사용하고 current Quest state/geometry는 JunhyunHelper first-party bridge가 연결한다.

대표 first-party boundary:

- `Desktop/MainWindow.LegacyMapHost.cs`
- `Desktop/MainWindow.MapSmokeV014.cs`
- `Desktop/MainWindow.ProductLifecycle.cs`
- `Desktop/Map/MapPage.JunhyunUiSimplification.cs`
- `Desktop/Map/*`
- `Desktop/Legacy/TarkovHelper/*`
- `Desktop/Quests/QuestPage.MapBridge.cs`
- `Desktop/Quests/QuestPage.MapNavigation.cs`

v1.7.14 `MapPage.JunhyunUiSimplification.cs` responsibilities:

- MiniMap launcher 주변 donor residual padding/background/help-button space 제거
- `지도 마커` launcher의 donor transparent local values를 clear하고 product Button chrome 적용
- marker panel collapsed 상태에서 min-width/padding/background/border 제거
- marker panel expanded 상태에서 map viewport 기반 충분한 세로 공간 확보
- donor `SettingsPanel`을 MainWindow shared overlay로 host
- overlay completion 후 `SettingsPanel` original parent/index 복원

이 first-party customization은 pinned donor source 자체를 수정하지 않는다.

Map floor relation은 visibility filter가 아니라 presentation relation이다. Enabled off-floor marker를 숨기지 않는다. Main Map/Factory/MiniMap actual smoke가 이 contract와 floor/viewport preservation을 검증한다.

이름에 `Legacy`가 있어도 compatibility bridge로 현재 실행 경로에 있을 수 있다. 이름만 보고 dead code로 판단하지 않는다.

Donor code는 concrete defect/performance evidence 없이 broad refactor하지 않는다.

---

# 9. Scanner — 구현 지도

Scanner specialist contract는 `docs/SCANNER.md`다. 아래는 subsystem navigation용 요약이다.

## 9.1 Product boundary

```text
screen pixels
→ capture
→ structural proposals
→ semantic inspect-header validation
→ item-title ROI
→ Windows ko-KR OCR
→ optional user substitution
→ conditional environment normalization
→ current official catalog sanitation/matching
→ optional strict visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional Ground Truth correction
```

금지:

- game memory read / DLL injection / packet interception
- scan-time HTTP identity work
- icon/image-only identity
- current catalog 밖 arbitrary Item 생성
- structural score만으로 identity 확정
- stale/cross-frame OCR/visual evidence를 current identity proof로 사용
- Item ID 전 price/needed/slot/source/previous-frame metadata identity evidence 사용
- 근거 없는 threshold/candidate-cap/matcher/visual acceptance 완화

## 9.2 Core Scanner

대표 파일:

- `Core/Scanner/ScannerCatalogItem.cs`
- `Core/Scanner/ScannerRecognition.cs`
- `Core/Scanner/ScannerItemMatcher.cs`
- `Core/Scanner/ScannerObservationPacingPolicy.cs`
- `Core/Scanner/ScannerOcrCharacterPolicy.cs`
- `Core/Scanner/ScannerOcrSubstitution.cs`
- `Core/Scanner/ScannerPresentationJoin.cs`
- `Core/Scanner/ScannerTitleIdentitySignature.cs`

## 9.3 Infrastructure Scanner

`Infrastructure/Scanner/ScannerCatalogService.cs`가 다음을 소유한다.

- GameMode full-item catalog download/cache/load
- official Korean identity catalog
- market/dimension parse
- semantic resolver
- wrong-mode cache fail closed
- `LoadCacheAsync` / `RefreshAsync` writer ordering
- healthy same-mode cache preservation

## 9.4 Desktop Scanner

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
- `Scanner/ScannerPage.xaml(.cs)` — normal surface/search/log/runtime controls
- `Scanner/ScannerPage.ProductUsability.cs` — v1.7.13+ source presentation, v1.7.14 Settings/Advanced overlay routing/search clear
- `Scanner/ScannerSettingsWindow.xaml(.cs)` — Mini fields/order + global Scanner hotkey editing, immediate persistence
- `Scanner/ScannerAdvancedWindow.xaml(.cs)` — Display Test/correction/dataset/support diagnostics; shared overlay dialog
- `Scanner/MiniScannerWindow.xaml(.cs)` — no-activate Topmost overlay
- `MainWindow.ScannerItemSources.cs` — searched confirmed item → authoritative NeededItems source presentation/navigation
- `MainWindow.ProductUiLayoutSmoke.cs` — actual product surface/Scanner Advanced overlay smoke

**v1.7.14에는 `ScannerHotkeySettingsWindow.xaml/.cs`가 없다.** Hotkey editor는 `ScannerSettingsWindow`에 통합됐다. 회귀 test가 old dedicated hotkey Window의 재도입을 금지한다.

`SerializedScannerOcrEngine`의 reflection 기반 diagnostic serialization adapter는 의도적으로 남은 기술 부채다. 단순 cleanup 대상으로 취급하지 않는다.

## 9.5 Recognition safety constants

현재 유지해야 할 검증 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

200 ms는 observation target이며 pacing policy가 cycle overrun 뒤 missed tick을 back-to-back replay하지 않게 한다.

Wall-clock benchmark를 CI의 고정 합격선으로 만들지 않는다.

## 9.6 Inspect-header / OCR / matching

Semantic lock은 red close-X, neutral inspect frame/header, magnifier, dark title field, text evidence를 결합한다.

OCR/matching:

- Windows `ko-KR` primary
- normal + bounded deep path
- raw OCR 보존
- user substitution 단일 ordered pass
- current-catalog-derived character policy
- exact-first
- conservative fuzzy + top1/top2 margin
- bounded unknown/edit recovery
- ambiguous → no Item ID

정상 OCR success path에 불필요한 luminance 분석/추가 normalization/추가 OCR을 삽입하지 않는다. Adaptive normalization은 miss/bounded deep path에만 사용한다.

## 9.7 Same-cycle reuse / continuity

OCR/visual reuse 조건은 current active cycle의 exact pixel identity에 한정한다.

- same active scan cycle
- compatible pass class
- same dimensions/BPP where relevant
- exact pixel identity/hash

Cycle이 바뀌면 current evidence도 바뀐다. Cross-frame identity cache는 없다.

Title continuity signature는 verified detail continuity evidence일 뿐 Item identity proof가 아니다.

## 9.8 Presentation / Needed Items

```text
confirmed Item ID
→ Scanner catalog official identity/market/dimensions
→ GameContent canonical item/Wiki
→ ItemsWorkspace.Plan.NeededItems[itemId]
   ├─ RemainingTotal
   └─ Sources
→ ScannerItemSnapshot
→ Scanner search / Mini Scanner
```

`RemainingTotal` 의미는 현재 Inventory/FIR 조건을 반영한 **현재 부족량**이다.

Scanner presentation이 raw inventory를 다시 빼거나 `RequiredTotal`을 사용자 표시값으로 사용하면 안 된다.

`Sources`는 searched confirmed item의 Quest/Hideout source rows authority다. Scanner가 Quest/Hideout requirements를 별도 재계산하거나 source list를 새 truth로 만들면 안 된다.

`RemainingTotal`/`Sources`는 Item ID 확정 뒤 presentation data이며 identity evidence가 아니다.

Market/dimension failure는 해당 presentation field만 비우고 Item identity를 소급 무효화하지 않는다.

## 9.9 Scanner Settings / hotkeys / Advanced

Scanner display settings schema의 current 값은 `docs/PROJECT_STATE.json`과 `ScannerDisplaySettings.CurrentSchemaVersion`을 따른다. UI maintenance가 schema migration 의미를 임의로 만들지 않는다.

`ScannerSettingsWindow` owns:

- Mini Scanner optional field visibility/order
- 1회 인게임 global hotkey
- 1회 테스트 global hotkey
- Scanner ON/OFF global hotkey

Hotkey capture:

- modifiers-only 입력은 preview
- Delete/Backspace → 미지정
- Esc → capture cancel
- Windows modifier unsupported
- 다른 Scanner 기능과 duplicate combination은 저장하지 않음
- persistence authority는 existing `ScannerCoordinator.Set*Hotkey` path

`ScannerAdvancedWindow`:

- standalone `Show()` product path 사용하지 않음
- `MainWindow.ToggleInAppWindowAsync("scanner-advanced", ...)`로 host
- 내용 자체의 `닫기` button 없음
- same launcher/backdrop/common X는 shared dismiss path
- existing advanced actions/validation semantics 유지

## 9.10 Ground Truth / diagnostics

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

Reviewed Ground Truth는 자동 삭제하지 않는다.

Support bundle은 Ground Truth/source pixel dataset, `user.db`, 게임 계정 정보, 진행/account-identifying 데이터를 포함하면 안 된다.

## 9.11 Scanner change-impact route

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
ScannerDisplaySettings v6 migration/normalize
→ Mini display/order + persisted hotkey fields
→ ScannerSettingsWindow
→ ScannerCoordinator persistence authority
```

Overlay:

```text
Scanner Settings / Advanced launcher
→ MainWindow shared overlay
→ shared dismissal
→ child semantics preserved
```

---

# 10. Images / preferences

`Desktop/Services/ImageCacheService.cs`는 image byte/dimension을 검증하고 decode 후 normalize한다. 개별 image 실패를 Game Content 전체 실패로 확대하지 않는다.

중요 presentation JSON은 same-directory temp + flush + atomic replace + `.bak` recovery 원칙을 사용한다.

Map/Ammo/Scanner preference 및 MiniMap window size는 user mutable data이며 Program Update가 덮어쓰지 않는다.

---

# 11. Program Update / package

대표 구현:

- `Infrastructure/Updates/GitHubProgramUpdateClient.cs`
- `Infrastructure/Updates/ProgramUpdateApplier.cs`
- Desktop startup/update UX
- `packaging/New-ReleasePackage.ps1`
- `.github/workflows/ci.yml`
- stable Release workflow

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

Release source contract:

```text
runtime change PR
→ exact reviewed PR head
→ merge commit on main
→ full main CI
→ verified main CI artifact
→ Release workflow
→ tag/release exact merge source
→ public asset digest/tag readback
```

Published stable release는 immutable하다. Documentation-only main commit이 같은 assembly version으로 다른 ProductVersion commit metadata/bytes를 만들 수 있어도 이미 공개된 stable asset을 교체하지 않는다.

정확한 현재 release proof는 `docs/STATE.md`와 `docs/RELEASE_1.7.14.md`를 사용한다.

---

# 12. Failure isolation

- Program Update network failure → app continues
- image failure → affected image only
- preference save failure → diagnostic, app continues where safe
- invalid/incomplete Game Content candidate → known-good active 유지
- unsupported Quest gate → Indeterminate/fail closed
- Scanner low-confidence/ambiguous evidence → no Item ID
- Scanner visual/font failure → primary OCR contract 유지
- Scanner missing market/icon/dimension → affected presentation field only
- Scanner catalog refresh failure → healthy same-mode cache may remain
- saved Case restore failure → original Ground Truth preserved
- corrupt retention metadata → preserve
- updater validation failure → current program untouched

Catch-all은 best-effort presentation/recovery cleanup 경계에서만 사용한다. Domain correctness 오류를 정상값처럼 숨기지 않는다.

---

# 13. 성능 / cache / concurrency

의도된 최적화:

- profile snapshot/workspace cache
- Inventory-only mutation future-basis reuse
- bounded image concurrency/cache
- Scanner verified-detail fast path
- same-cycle exact OCR/current-pixel reuse
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

`UserProfileStore`의 existing snapshot cache 때문에 반복 `LoadAsync` 문법만 보고 새 global cache, one-read/multi-build 또는 병렬화를 추가하지 않는다. 실제 runtime trace가 병목을 보여야 한다.

---

# 14. 주요 first-party file index

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
- `InAppOverlayDialog.cs`
- `Controls/ProductSearchClearButtonBehavior.cs`
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

# 15. Tests / verification

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Quest/Scanner/maintenance contracts를 검증한다.

테스트 수를 이 문서에 장기 상수로 고정하지 않는다. 현재 exact suite 결과는 `docs/STATE.md`와 해당 CI run을 본다. v1.7.14 product release 시점은 **407 passed / 0 failed / 0 skipped**다.

v1.7.14 UI regression:

- `tests/JunhyunHelper.Tests/Maintenance/V1714UiConsistencyContractTests.cs`

이 test는 Ammo popup, shared overlay, Scanner Settings/Advanced, old hotkey Window removal, Map launcher/settings, Profile card, search clear contract를 source level에서 고정한다.

WPF/Map/Scanner interaction은 xUnit만으로 충분하지 않으므로 actual published EXE smoke가 별도로 존재한다.

최소 release gate 범주:

1. Release build
2. full deterministic tests
3. Windows x64 self-contained single-file publish
4. ProductVersion/FIRST_RUN identity
5. package/root/dependency audit
6. actual EXE startup/rendered Product UI smoke
7. Scanner normal surface + Scanner Advanced shared-overlay smoke
8. Mini Scanner smoke
9. Main Map/Factory/MiniMap smoke
10. graceful shutdown/process exit
11. portable-root pollution check
12. stable package generation/checksum validation
13. exact main source CI
14. exact Release workflow artifact validation
15. exact source/tag/public release verification

외부 Live Data Probe는 이 hermetic release gate와 별개다.

---

# 16. Dead path / cleanup 판단

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

WPF에서는 handler 본문이 중복처럼 보여도 routed/class handler 또는 `Loaded` delivery를 간접적으로 유지할 수 있으므로 lifecycle 관련 dead-code 판단은 actual published EXE smoke까지 확인한다. 다만 current XAML/constructor/explicit lifecycle가 같은 역할을 직접 소유하게 된 뒤에는 과거 runtime rebinding이나 hidden proxy path를 보존하지 않는다.

현재 동작과 historical reproducibility에 가치가 있으면 참조가 적더라도 남길 수 있다.

현재 명시적으로 유지하는 항목:

- active `Legacy` Map/MiniMap bridge
- Main Map/Factory/MiniMap actual smoke
- Scanner diagnostic reflection adapter
- lifecycle evidence가 실제 제품 경로에 남아 있는 current handler/overlay/Map bridge only

반대로 obsolete UI가 새 authoritative path로 완전히 대체되면 제거할 수 있다. v1.7.14의 old `ScannerHotkeySettingsWindow`는 Scanner Settings 통합 뒤 제품/코드 path에서 삭제했다.

---

# 17. 하지 말아야 할 것

- 현재 코드가 존재한다는 이유만으로 공식 요구사항으로 승격
- unknown Quest condition을 true/Current로 추측
- missing ProfileVariable을 0으로 간주
- flexible Item 소비 후보 자동 추측
- content update 중 active DB 먼저 overwrite
- Program Update 검증 전 product file 변경
- user.db를 content/program update와 함께 초기화
- Map donor를 cleanup 목적으로 broad rewrite
- `Legacy` 이름만 보고 active Map bridge 삭제
- Scanner structural score만으로 Item 확정
- Scanner threshold/candidate cap을 인식률 때문에 임의 완화
- Scanner scan-time network/icon identity 추가
- Scanner에서 Needed Items 의미 재계산
- Scanner searched-item Quest/Hideout source를 별도 requirement 계산으로 재구현
- Scanner catalog shared writer synchronization 분리
- user substitution을 automatic global correction table로 승격
- reviewed Ground Truth 자동 삭제
- title continuity signature를 Item identity proof로 사용
- cross-frame OCR/visual cache로 current evidence 대체
- Ground Truth saved coordinate를 display-scale coordinate로 저장
- saved Case restore failure에서 기존 Ground Truth overwrite/delete
- UI event handler에 domain truth 복제
- child editor validation/save semantics를 MainWindow overlay에 복제
- Map Settings UIElement를 overlay에서 닫은 뒤 original visual tree에 복원하지 않기
- old dedicated Scanner hotkey Window를 Settings authority와 병렬로 다시 만들기
- 외부 네트워크 상태를 일반 PR CI의 mandatory invariant로 만들기
- published stable release를 docs-only build bytes로 교체

---

# 18. 빠른 영향 분석 질문

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
11. failure 시 current known-good data/program/Item identity/Ground Truth를 보존하는가?
12. deterministic regression test와 actual EXE smoke 중 무엇이 필요한가?
13. 실제 Tarkov 불확실성을 diagnostics/Ground Truth/live probe로 분리할 수 있는가?
14. cache/retention이 current-frame evidence 또는 reviewed truth를 손상할 수 있는가?
15. performance optimization이 threshold 완화/stale reuse를 도입하지 않는가?
16. UI scale/coordinate transform이 original Ground Truth pixel coordinate를 보존하는가?
17. package change가 updater archive-root contract와 일치하는가?
18. user-facing editor라면 shared overlay와 child validation/save authority가 분리되는가?
19. existing UIElement를 overlay에 옮겼다면 original parent/index 복원이 보장되는가?
20. 변경 후 `STATE`/`PRODUCT`/`ARCHITECTURE`/전문 문서/이 reference 중 무엇을 갱신해야 다음 세션이 오해하지 않는가?

---

# 19. 현재 기준선

현재 product stable은 v1.7.14이고 exact release source는 `docs/STATE.md`에 기록된 SHA다. 이 문서가 있는 docs-only commit을 product release source로 해석하지 않는다.

현재 진행 중 제품 작업의 유무와 중단 지점은 `docs/ACTIVE_WORK.md`를 기준으로 판단한다. 새 유지보수/개발 작업은 실제 runtime error, Tarkov 변화, reviewed Scanner evidence 또는 사용자가 새로 확정한 제품 요구사항을 근거로 시작한다.
