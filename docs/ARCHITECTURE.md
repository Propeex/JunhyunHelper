# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록한다.

기준일: 2026-08-27  
상태: **v1.7.14 PUBLIC STABLE / MAINTENANCE MODE**

정확한 현재 릴리즈 SHA·CI·asset·schema 상태는 `docs/STATE.md`를 권위 있는 운영 인덱스로 사용한다. 유지보수 시 변경 원칙은 `docs/MAINTENANCE_CONTRACTS.md`를 따른다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — image decode / Scanner local-font rendering
- SharpVectors — SVG Map rendering
- Windows x64 portable / self-contained single-file
- 별도 backend 없음
- runtime GPT/AI 없음

## 2. Project boundaries

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

### Core

Canonical domain과 deterministic 계산:

- Quest availability / future reachability
- Needed Items / cleanup safety
- Item / Ammo canonical meaning
- Scanner pure policy / signatures / matcher contracts

### Application

Authoritative user mutation/use case와 workspace orchestration.

### Infrastructure

I/O boundary:

- HTTP/source parsing
- Game Content build/validation/activation
- SQLite/file persistence
- Scanner full-item/market catalog
- Program Update client/applier

### Desktop

WPF/presentation/system integration:

- MainWindow shell / pages / shared user-facing overlay host
- image-cache presentation
- Scanner capture/OCR/runtime/search/diagnostics
- Map first-party product bridge/customization
- startup/update UX

Domain truth를 WPF event handler에 복제하지 않는다.

### Map/MiniMap donor

`vendor/Tarkov-Helper` 전체를 제품 사양으로 승계하지 않는다. Map/MiniMap만 pinned source compile-link 예외다.

```text
pinned revision:
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper 제품 delta는 donor source를 broad-edit하지 않고 first-party partial/bridge/customization boundary에서 적용한다.

## 3. Data authority / lifecycle

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` |
| User Progress | user-confirmed facts | `%LocalAppData%/JunhyunHelper/user.db` |
| Inventory | user quantity + consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | validated/normalized bytes | `%LocalAppData%/JunhyunHelper/image-cache/` |
| Scanner identity/market catalog | current full-item source + official Korean identity | `scanner/catalog/` + memory |
| Scanner settings | display/hotkey/order/substitution state | `scanner-settings.json(.bak)` |
| Scanner title font cache | installed Tarkov assets read-only extraction | `scanner/fonts/` |
| Scanner Cases / reviewed Ground Truth | runtime evidence + user truth | `scanner/diagnostics/` |
| Runtime logs | diagnostics only | `logs/` |
| Map artwork/config/general markers | pinned donor bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable product folder |

이 lifecycle들은 서로 분리한다. Program Update와 Game Content Update가 `user.db`, reviewed Ground Truth 또는 mutable preferences를 덮어쓰지 않는다.

## 4. Startup / composition / overlay / shutdown

대표 startup 흐름:

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

Shared page infrastructure는 product-window lifetime의 `MainWindow.OnInitialized`가 소유한다. 개별 Page의 internal presentation initialization은 해당 Page가 직접 소유하며 unrelated Page의 `Loaded` 순서를 implicit trigger로 사용하지 않는다.

Ammo search/detail/grid presentation은 `AmmoPage.OnInitialized`에서 Loaded dispatcher priority로 초기화된다.

### 4.1 Shared in-app overlay owner

v1.7.14에서 다음 user-facing settings/editor surface는 MainWindow shared overlay interaction을 사용한다.

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

```text
launcher
→ MainWindow shared overlay owner
→ content/editor surface
→ same launcher / backdrop / common X → dismiss
```

두 hosting 경로:

```text
Window-backed editor
→ MainWindow.ToggleInAppWindowAsync

existing visual-tree UIElement
→ caller detach
→ MainWindow.ShowInAppElementAsync
→ caller restore original parent/index
```

`IInAppOverlayDialog`를 구현한 child surface는 dismiss 시 자체 validation/save/cancel 의미를 중재할 수 있다. MainWindow는 child의 domain/persistence semantics를 재구현하지 않는다.

Map Settings는 donor `SettingsPanel`이라는 existing UIElement를 overlay에 임시 re-parent하므로 caller인 Map first-party customization이 원래 parent/index로 복원한다.

Overlay owner가 새 domain truth를 만들면 안 된다.

### 4.2 Shutdown

Shutdown은 Scanner runtime/OCR/font/retention/background-owned resource를 정상 종료해야 한다. actual published EXE graceful-shutdown smoke가 이 경계를 검증한다.

## 5. Game Content update architecture

```text
remote source
→ parse/import
→ canonical/relational validation
→ candidate content
→ last-known-good completeness guard
→ candidate content.db
→ SQLite read-back/integrity
→ atomic active replacement
→ image prefetch
```

핵심:

- candidate 완성 전 active DB overwrite 금지
- failed candidate 폐기
- known-good active 유지
- 기존 정상 snapshot이 있으면 `ContentUpdateCompletenessGuard`가 suspicious shrink를 차단
- retained floor 50%는 Tarkov 절대 행 수 제한이 아니라 baseline-relative partial-payload 방어
- collection schema drift를 importer가 이해하지 못하면 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress에 영향 없음

Top-level Game Data Update는 general content activation 이후 current GameMode Scanner catalog/market refresh까지 orchestration한다. Scanner-only partial failure는 general success를 rollback하지 않는다.

외부 최신 계약 감시는 `.github/workflows/live-data-probe.yml`에서 hermetic PR/main CI와 분리한다.

## 6. Profile / Quest / Needed Items

```text
Profile facts
→ Quest availability/current/future reachability
→ Hideout future requirements
→ NeededItemRequirementBuilder
→ NeededItemCalculator
→ ItemsWorkspace.Plan.NeededItems
→ Needed Items / cleanup protection
```

Unknown prerequisite는 optimistic current로 바꾸지 않는다. Flexible hand-in 실제 Item을 자동 추측하지 않는다.

`ItemsWorkspace.Plan.NeededItems`는 Scanner current-needed/source presentation authority이기도 하다.

```text
NeededItems[itemId]
├─ RemainingTotal → Scanner current needed
└─ Sources        → Scanner searched-item Quest/Hideout source rows
```

Scanner가 Quest/Hideout requirement/source를 별도 재계산하지 않는다. 이 정보는 Item ID 확정 뒤 presentation에만 사용한다.

## 7. Items / Ammo presentation architecture

Items는 canonical content + profile/inventory + `ItemsWorkspace.Plan`을 presentation한다. v1.7.13부터 Quest/Hideout purpose selector는 active product filter가 아니다.

Ammo는 read-only comparison + persisted favorites를 제공한다. Search/detail/grid presentation은 page-owned initialization이다.

v1.7.14 Ammo popup true-toggle:

```text
launcher PreviewMouseDown
→ if target Popup already open
   → close Popup
   → mark routed event handled
   → do not continue to Button Click reopen path
```

이 방식은 `Popup.StaysOpen=False`가 먼저 자동 닫힌 뒤 기존 Click handler가 다시 여는 WPF 순서 문제를 timer/delay 없이 해결한다.

Quest/Hideout/Items/Ammo/Scanner 주요 검색창의 `×`는 `ProductSearchClearButtonBehavior`가 presentation-only로 제공한다. Filtering truth를 재구현하지 않는다.

## 8. Map / MiniMap architecture

Map artwork/config/general markers는 donor bundle을 사용하고 current Quest state/geometry는 JunhyunHelper bridge가 연결한다.

Map은 독립 subsystem이고 Quest만 current JunhyunHelper data와 bridge한다.

대표 first-party boundary:

- `Desktop/MainWindow.LegacyMapHost.cs`
- `Desktop/MainWindow.MapSmokeV014.cs`
- `Desktop/Map/MapPage.JunhyunUiSimplification.cs`
- `Desktop/Map/*`
- `Desktop/Legacy/TarkovHelper/*`
- `Desktop/Quests/QuestPage.MapBridge.cs`
- `Desktop/Quests/QuestPage.MapNavigation.cs`

v1.7.14 first-party presentation changes:

- MiniMap launcher donor residual chrome 제거
- `지도 마커` launcher product Button chrome 적용
- collapsed marker panel empty chrome 제거
- expanded marker panel viewport-based height 확보
- Map/MiniMap donor `SettingsPanel`을 shared MainWindow overlay로 host

Pinned donor XAML/source revision은 바꾸지 않는다.

Floor relation은 visibility filter가 아니라 presentation relation이다. Enabled off-floor marker를 숨기지 않고 current/above/below relation을 제품 presentation으로 표시한다.

이름에 `Legacy`가 있어도 현재 compatibility bridge일 수 있다. 실제 reference/execution evidence 없이 dead code로 판단하지 않는다.

검증된 donor code는 concrete defect/performance evidence 없이 broad refactor하지 않는다.

## 9. Scanner subsystem

Scanner는 화면 픽셀을 current official Korean item catalog의 Item ID에 연결한다.

Canonical specialist document: `docs/SCANNER.md`.

### 9.1 Current logical composition

```text
MainWindow shared overlay host
├─ Profile editor
├─ Scanner Settings
├─ Scanner Advanced
└─ Map/MiniMap Settings UIElement

ScannerPage
├─ ON/OFF
├─ Settings launcher
├─ Advanced launcher
├─ current result correction
├─ item search
└─ recognition log

ScannerSettingsWindow
├─ Mini Scanner optional field visibility/order
└─ global Scanner hotkey editing

ScannerCoordinator
├─ settings schema v6 / hotkeys / substitutions / Mini order
├─ local item search/details
├─ ScannerCatalogService
├─ ScannerRuntimeService
│  ├─ capture + structural proposals
│  ├─ semantic inspect-header/title refinement
│  ├─ SerializedScannerOcrEngine
│  ├─ FontAwareScannerOcrEngine
│  ├─ ScannerLatencyTelemetry
│  └─ ScannerItemPresentationService
├─ ScannerRecognitionDebugStore
├─ Ground Truth correction / saved Case re-edit
└─ Mini Scanner overlay/context
```

Old dedicated `ScannerHotkeySettingsWindow`은 v1.7.14에서 제거됐다. Hotkey authority는 `ScannerSettingsWindow` + existing `ScannerCoordinator` setter/persistence path다.

Title OCR과 inventory-context OCR은 하나의 WinRT serialization boundary를 공유한다.

### 9.2 Recognition data flow

```text
Tarkov client/display pixels
→ capture
→ structural proposals
→ close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user substitution
→ conditional environment normalization where needed
→ current-catalog sanitation/matching
→ optional strict current-pixel visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

Single weak evidence source만으로 Item identity를 확정하지 않는다.

### 9.3 Recognition safety contract

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- geometry = proposal evidence, not identity
- current official Korean full-item catalog = identity authority
- exact-first conservative matcher
- ambiguity = no Item ID
- scan-time network identity work 금지
- Item ID 확정 전 price/needed/slot/source/previous-frame metadata identity evidence 금지
- cross-frame OCR/visual identity cache 금지
- reviewed evidence 없이 threshold/candidate/matcher/visual acceptance 완화 금지

### 9.4 OCR / matching / same-cycle reuse

- Windows ko-KR primary
- raw OCR preserved
- normal + bounded deep variants
- adaptive environment normalization은 normal miss/deep path에만 조건부 적용
- user substitution single ordered pass
- current-catalog character policy
- conservative fuzzy + top1/top2 margin
- bounded unknown/edit recovery
- strict current-catalog visual recovery

OCR/visual reuse는 same active cycle + exact current-pixel identity에만 한정한다. Title continuity signature는 detail continuity evidence이지 Item identity proof가 아니다.

### 9.5 Item presentation / search

```text
confirmed Item ID
→ Scanner catalog official identity/market/dimensions
→ GameContent canonical item/Wiki
→ ItemsWorkspace.Plan.NeededItems[itemId]
   ├─ RemainingTotal
   └─ Sources
→ ScannerItemSnapshot / search presentation
→ Mini Scanner / Scanner search details
```

- current needed = `RemainingTotal`
- searched needed-item source rows = existing `Sources`
- Scanner가 raw inventory를 다시 빼거나 `RequiredTotal`로 되돌리지 않음
- Scanner가 Quest/Hideout source를 새 truth로 재구축하지 않음
- source navigation은 existing cross-page navigation boundary 사용
- market/dimension failure는 affected field only
- search-time network 금지

### 9.6 Ground Truth / retention

```text
full.png
→ auto-fit display
→ candidate direct selection
→ manual/none fallback
→ original-pixel-coordinate Ground Truth
→ reviewed save
```

Display scale은 dataset coordinate authority가 아니다. Saved Case restore failure는 original GT를 보존한다.

Automatic delete eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Reviewed GT는 자동 삭제하지 않는다. Corrupt/unknown state는 preserve fail closed한다.

`SerializedScannerOcrEngine` diagnostic reflection adapter는 현재 의도적으로 남은 technical debt이며 runtime evidence 없이 cleanup하지 않는다.

## 10. Program Update / release architecture

```text
latest stable check
→ user consent
→ exact release asset + checksum
→ archive/root validation
→ staging
→ temporary updater
→ program-owned transaction
→ restart
```

Canonical stable package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

LocalAppData user data는 untouched다.

Release workflow는 successful main CI의 exact artifact를 내려받아 ProductVersion/FIRST_RUN/checksum을 다시 검증한 뒤 게시한다.

Public stable은 immutable하다. 이후 documentation-only main commit이 동일 assembly version에서 다른 ProductVersion commit metadata/bytes를 만들어도 이미 공개된 release asset을 교체하지 않는다.

상세는 `docs/PROGRAM_UPDATE.md`, `docs/DEPLOYMENT.md`, `docs/RELEASE_1.7.14.md`를 사용한다.

## 11. Persistence / atomicity

Important JSON preferences는 same-directory temp + flush + atomic replacement + `.bak` recovery 원칙을 사용한다.

Mutable runtime state/logs는 portable executable beside-root에 쓰지 않는다.

## 12. Performance principles

- deterministic/canonical result만 reuse
- image work bounded
- Scanner verified-detail fast path 유지
- same-cycle exact bitmap reuse only
- bounded/generation-aware font/visual cache
- Scanner catalog writer serialization
- bounded diagnostics/log retention
- Map donor broad rewrite 금지 without evidence

Performance는 Scanner identity threshold 완화나 stale cross-frame reuse의 근거가 될 수 없다.

`UserProfileStore.LoadAsync`는 authoritative read/save 뒤 immutable in-process snapshot cache를 사용한다. 반복 workspace load 호출을 runtime trace 없이 SQLite 병목으로 가정하지 않는다.

Wall-clock benchmark를 일반 CI의 고정 합격선으로 사용하지 않는다. Deterministic policy regression + runtime telemetry + 필요 시 reviewed Ground Truth replay를 사용한다.

## 13. Failure isolation

- Program Update network failure → app continues
- image failure → affected image only
- preference save failure → diagnostic, app continues where safe
- invalid/incomplete content candidate → known-good retained
- unsupported Quest gate → Indeterminate/fail closed
- Scanner low confidence/ambiguity → no Item ID
- Scanner visual/font failure → primary OCR remains where valid
- missing market/icon/dimension → affected presentation only
- Scanner catalog refresh failure → healthy same-mode cache may remain
- saved Case restore failure → original GT preserved
- corrupt retention metadata → preserve
- updater validation failure → current program untouched

## 14. Verification architecture

Domain/storage/update semantics는 deterministic tests로 검증한다. WPF/Scanner/Map interaction은 actual published EXE smoke를 추가한다.

Runtime release candidate 최소 gate:

- Release build
- full automated tests / no failures/skips
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN identity
- Product UI + Scanner/Mini Scanner smoke
- Scanner Advanced shared-overlay smoke
- Main Map/Factory/MiniMap smoke
- graceful shutdown / clean portable root
- `Junhyun-Helper.zip` generation + checksum/layout validation
- exact main source rerun
- Release workflow exact artifact verification
- exact tag/source
- public latest release metadata/assets readback
- public asset digest/tag-ref = verified main-CI package/source

가능한 환경에서는 independent public binary redownload/hash/layout/EXE smoke를 추가한다. 수행하지 않은 anonymous binary verification을 완료했다고 기록하지 않는다.

현재 exact release proof는 `docs/STATE.md`, `docs/RELEASE_1.7.14.md`가 권위다.

## 15. Change-impact routes

Scanner recognition:

```text
capture/proposal
→ semantic header
→ ROI
→ OCR/substitution/visual
→ catalog matcher
→ Item ID
→ presentation/search
→ Mini overlay
→ GT/diagnostics/replay/smoke
```

Needed count/source:

```text
Quest/Hideout/Profile
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RemainingTotal + Sources
→ Scanner presentation/search navigation
```

Scanner settings v1.7.14:

```text
ScannerDisplaySettings v6 normalize/migration
→ Mini display/order state
→ ScannerSettingsWindow immediate-save

hotkey fields
→ ScannerSettingsWindow hotkey capture
→ ScannerCoordinator Set*Hotkey
→ same settings persistence authority
```

Shared overlay:

```text
launcher
→ MainWindow ToggleInAppWindowAsync / ShowInAppElementAsync
→ shared card/backdrop/X
→ child validation/save authority
→ dismiss
```

Ground Truth:

```text
Case evidence
→ display transform
→ candidate/manual/none selection
→ original pixel ROI
→ durable reviewed case
→ re-edit/replay
```

## 16. Related docs

- `docs/STATE.md`
- `docs/CURRENT_STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DATA_VALIDATION.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/MAP_PRODUCT_REQUIREMENTS.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/RELEASE_1.7.14.md`
