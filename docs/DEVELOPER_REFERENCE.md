# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **ACTIVE / v1.6.0 RELEASE CANDIDATE**

기준일: 2026-08-24

이 문서는 다음 개발 세션이 대화 기억 없이 저장소만 보고 이어서 작업할 수 있도록 만든 구현 지도다.

권위 우선순위:

1. 사용자의 최신 확정 요구사항
2. `docs/PRODUCT.md`
3. 최신 개별 decision 문서
4. `docs/STATE.md`
5. 영역별 전문 문서
6. 현재 code/tests
7. 역사적 release/decision 문서

현재 구현이 존재한다는 이유만으로 그 동작을 공식 제품 의미로 추정하지 않는다.

---

# 1. 현재 기준선

준현 헬퍼는 Windows x64 .NET 10 WPF desktop application이다.

현재 사용자 기능:

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

현재 public stable은 v1.5.0이다.

현재 source는 **v1.6.0 release candidate**이며 public release verification 전에는 stable로 표기하지 않는다.

```text
Desktop target version: 1.6.0
Content schema: v7
Readable content: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog: v1/v2 readable, v2 written
automated test suite: 296
```

v1.6.0 current records:

- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.6.0.md`
- `docs/SCANNER.md`
- `docs/CURRENT_SCANNER_WORK.md`

`vendor/Tarkov-Helper`는 일반 제품 사양이 아니다. Map/MiniMap만 pinned donor revision을 명시적으로 채택한 제한적 예외다. Quest/Hideout/Items/Ammo/Scanner/updater 의미는 JunhyunHelper first-party code/docs가 소유한다.

---

# 2. 저장소를 읽는 순서

새 대화/개발 세션:

1. `AGENTS.md`
2. `docs/STATE.md`
3. `docs/CURRENT_STATE.md`
4. `docs/PRODUCT.md`
5. 최신 관련 decision/status 문서
6. `docs/DEVELOPER_REFERENCE.md`
7. `docs/ARCHITECTURE.md`
8. 작업 영역 전문 문서
9. 관련 code/tests/current PR

Scanner 작업이면 추가로:

- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`

Quest availability 작업이면:

- `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

를 추가로 읽는다.

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

책임:

- **Core**: canonical domain, deterministic 계산, Quest availability, Scanner pure policies/signatures/matcher contracts
- **Application**: authoritative user mutation/use case, workspace orchestration, planning
- **Infrastructure**: source/HTTP/SQLite/files/content activation/program update/Scanner catalog persistence
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge, startup/update UX
- **vendor Map/MiniMap**: pinned donor source; broad ownership 이전 금지

UI event handler에서 Core/Application domain truth를 복제하지 않는다.

---

# 4. 데이터 권위와 저장 위치

| 데이터 | 권위 | 저장 위치 |
|---|---|---|
| Game Content | validated canonical import | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `%LocalAppData%/JunhyunHelper/user.db` |
| Inventory | user quantity + explicit fixed consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | validated/normalized presentation image | `%LocalAppData%/JunhyunHelper/image-cache/` |
| Scanner identity/market catalog | current full-item source + current Korean identity | `%LocalAppData%/JunhyunHelper/scanner/catalog/` + memory |
| Scanner settings | user presentation/hotkey/OCR substitution/order | `%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)` |
| Scanner font recovery cache | locally discovered Tarkov title fonts + manifest | `%LocalAppData%/JunhyunHelper/scanner/fonts/` |
| Scanner diagnostics / reviewed GT | runtime evidence + user Ground Truth | `%LocalAppData%/JunhyunHelper/scanner/diagnostics/` |
| Scanner/startup logs | runtime diagnostics | `%LocalAppData%/JunhyunHelper/logs/` |
| Map general data/artwork | pinned release Assets | portable `Assets/` |
| Program files | exact GitHub stable release | portable product folder |

Game Content update와 Program Update는 별개다. 둘 다 `user.db`와 Scanner reviewed Ground Truth를 덮지 않는다.

---

# 5. Startup / shell / lifecycle

대표 흐름:

```text
App.OnStartup
  ├─ fatal exception hooks
  ├─ updater apply-mode
  ├─ LocalAppData log path
  ├─ ScannerDiagnosticRetentionService
  ├─ MainWindow
  └─ smoke가 아니면 startup Program Update check

MainWindow.Window_Loaded
  └─ LoadProfilesAsync
      ├─ UserProfileStore
      ├─ selected GameMode content read/recovery/update
      ├─ Quest workspace
      ├─ Hideout workspace
      ├─ Items workspace
      ├─ Ammo context
      ├─ Map bridge
      └─ Scanner context/catalog/runtime
```

MainWindow는 orchestration layer다. 새 domain truth를 event handler에 넣지 않는다.

Shutdown에서는 Scanner runtime/OCR/font/retention/background-owned resource가 정상 종료돼야 한다. CI graceful-shutdown smoke가 이 경계를 검증한다.

---

# 6. Profile / Quest / Hideout / Items

## 6.1 Profile

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

`UserProfileStore`가 SQLite `user.db` serialization과 in-process snapshot cache를 소유한다.

## 6.2 Quest

`QuestAvailabilityEvaluator` 결과:

- Current
- Locked
- Completed
- Unavailable
- Indeterminate

원칙:

- 서로 다른 task requirement = AND
- requirement 내부 accepted statuses = OR
- unknown/unsupported fact를 optimistic Current로 만들지 않음
- exact observed ProfileVariable이 있으면 권위값
- task-pool compatibility는 audited exact shape에서만 synthetic fact 생성

`QuestFutureReachabilityEvaluator`는 current availability와 future Item 필요 가능성을 분리한다. Unknown future path는 `IndeterminatePotential`로 보호한다.

2026-08-24 live audit는 `regular`, `pve`, `pvp-season`을 대상으로 했다. `QuestTaskPoolVariableCompatibility`는 GameMode + audited pool membership/threshold/trader/shape가 맞을 때만 compatibility를 적용한다. Drift하면 fail closed한다.

## 6.3 Hideout

미입력 station은 Lv.0이다. Future upgrade fixed material은 Needed Items에 포함한다.

## 6.4 Inventory / Needed Items

```text
future Quest reachability
+ future Hideout requirements
→ NeededItemRequirementBuilder
→ fixed/flexible split
→ NeededItemCalculator
→ NeededItems / Cleanup protection
```

Flexible hand-in 실제 제출 Item을 자동 추측하지 않는다.

Inventory-only mutation에서 planning facts가 같으면 기존 future basis를 재사용한다. 새 profile field가 planning 의미에 영향을 주면 `ItemsApplicationService.PlanningStateEquals`도 갱신해야 한다.

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

- candidate 완성 전 active DB를 덮지 않음
- canonical + relational validation
- previous known-good 유지
- 새 active read 실패 시 previous recovery
- update failure가 User Progress에 영향 없음

현재 Content schema v7, readable v3~v7.

Top-level Game Data update는 general content activation 후 current GameMode Scanner catalog/market refresh까지 orchestration한다.

Scanner refresh partial failure는 general content success를 rollback하지 않는다. 기존 healthy same-mode Scanner cache가 있으면 보존한다.

---

# 8. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper first-party bridge 예:

- `MainWindow.LegacyMapHost.cs`
- `MainWindow.ProductLifecycle.cs`
- `MainWindow.MapSmokeV014.cs`
- `MainWindow.ProductUiLayoutSmoke.cs`
- `Map/GlobalKeyboardHookService.JunhyunProduct.cs`
- `Map/JunhyunExtractMarkerIcon.cs`
- `Quests/QuestPage.MapBridge.cs`
- `Quests/QuestPage.MapNavigation.cs`
- `Legacy/TarkovHelper/LegacyMapHostCompatibility.cs`

Map artwork/config/general markers는 donor bundle, current Quest state/geometry는 JunhyunHelper bridge를 사용한다.

구체적 defect/performance evidence 없이 donor broad refactor를 하지 않는다.

Published EXE smoke는 map selector, floor selector, asset source, viewport preservation, Factory/MainMap/MiniMap behavior를 검증한다.

---

# 9. Scanner — 전체 구현 지도

Scanner specialist contract: `docs/SCANNER.md`.

버전별 기준선:

- v1.1.3: Scanner Lab v3.8 multi-candidate structural/semantic recognition 복원
- v1.1.4~v1.1.x: market/Needed Items/diagnostics/icon/catalog health 보강
- v1.2.0: title anchors, Tarkov-font visual recovery, recognition diagnostics, one-shot
- v1.2.1: font/cache generation, bounded visual caches, lifecycle/capture hardening
- v1.2.2: Scanner catalog GameMode writer ordering hardening
- v1.3.x: live inspect-header/ROI/current-catalog glyph hardening
- v1.4.x: Ground Truth dataset/correction/regression 및 live evidence 보강
- v1.5.0: mapped-data repair, unified data refresh, user OCR substitutions, candidate GT, latency telemetry, same-cycle OCR reuse, continuity stabilization, retention, UI finishing
- **v1.6.0**: normal Scanner surface/search, Mini Scanner ordered fields, settings schema v6, image-first correction, saved Case re-edit, stable release package naming

## 9.1 Scanner product boundary

```text
screen pixels
→ capture
→ structural proposals
→ semantic inspect-header validation
→ item-name ROI
→ ko-KR OCR
→ optional user substitution
→ official-catalog sanitation/matching
→ optional visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional GT correction
```

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data
- scan-time HTTP
- icon/image-only identity
- current catalog 밖 Item 생성
- evidence 없는 threshold/candidate-cap 완화
- cross-frame OCR result를 current evidence로 권위화

## 9.2 Core Scanner files

- `Core/Scanner/ScannerCatalogItem.cs` — Item ID/name/icon/market/dimension snapshot
- `Core/Scanner/ScannerRecognition.cs` — matcher result/reason/confidence/second-score
- `Core/Scanner/ScannerOcrSubstitution.cs` — user exact substitution rules
- `Core/Scanner/ScannerTitleIdentitySignature.cs` — trusted-detail continuity signature; identity proof 아님

## 9.3 Infrastructure Scanner

`Infrastructure/Scanner/ScannerCatalogService.cs`:

- GameMode full-item catalog download/cache/load
- current Korean official identity catalog
- semantic resolver
- trader/flea/dimension parse
- wrong-mode cache fail closed
- `LoadCacheAsync` / `RefreshAsync` writer ordering gate
- healthy same-mode cache preservation

## 9.4 Desktop Scanner ownership

주요 files:

- `Scanner/ScannerCoordinator.cs` — settings/catalog/runtime/profile lifecycle
- `Scanner/ScannerCoordinator.CatalogStatus.cs` — readiness/status
- `Scanner/ScannerCoordinator.OcrSubstitutions.cs` — user substitution persistence boundary
- `Scanner/ScannerCoordinator.Search.cs` — v1.6 local full-item search/details
- `Scanner/ScannerRuntimeService.cs` — continuous recognition lifecycle
- `Scanner/ScannerRuntimeService.OneShot.cs` — one-shot observation path
- `Scanner/ScannerRuntimeService.Latency.cs` — latency wrapper
- `Scanner/ScannerRuntimeService.TitleIdentity.cs` — continuity signature normalization
- `Scanner/ScannerItemPresentationService.cs` — Item ID → catalog/content/ItemsWorkspace → snapshot
- `Scanner/ScannerLocalIconService.cs` — local image-cache read-only projection
- `Scanner/ScannerRecognitionDebugStore.cs` — latest recognition evidence
- `Scanner/ScannerPage.xaml(.cs)` — v1.6 normal Scanner/search/log surface
- `Scanner/ScannerSettingsWindow.xaml(.cs)` — hotkeys + Mini Scanner fields/order
- `Scanner/ScannerAdvancedWindow.xaml(.cs)` — Display Test/correction/dataset management
- `Scanner/MiniScannerWindow.xaml(.cs)` — no-activate Topmost overlay

## 9.5 Capture / detector / OCR / visual

- `Scanner/ScannerLab38WindowsVision.cs` — Tarkov/display capture, proposal generation, raw WinRT OCR
- `Scanner/FontAwareScannerOcrEngine.cs` — semantic OCR + constrained local-font visual path
- `Scanner/SerializedScannerOcrEngine.cs` — shared OCR serialization + exact same-cycle bitmap reuse
- `Scanner/TarkovTitleFontProvider.cs` — local Tarkov font discovery/cache
- `Scanner/ScannerFullCatalogVisualMatcher.cs` — official catalog 범위 visual recovery

Inspect-header first-party files는 red close-X + neutral frame + magnifier + title field를 결합해 authoritative title ROI를 만든다.

## 9.6 Ground Truth / diagnostics files

- `Scanner/ScannerCandidateGroundTruth.cs` — detector candidate selection persistence
- `Scanner/ScannerCorrectionWindow.xaml(.cs)` — v1.6 auto-fit image + direct candidate box selection + manual/none fallback
- `Scanner/ScannerDiagnosticCaseBrowser.cs` — Case summary + re-open source parsing
- `Scanner/ScannerDiagnosticCasesWindow.xaml(.cs)` — saved Case list/delete/re-edit
- `Scanner/ScannerDiagnosticRetentionService.cs` — automatic unreviewed retention
- `Scanner/ScannerLatencyTelemetry.cs` — stage telemetry
- `Scanner/ScannerDisplaySettings.cs` — schema v6, hotkeys/display/order/substitution persistence

## 9.7 Capture modes / one-shot

Real:

```text
EscapeFromTarkov window
→ Borderless client-area
→ PrintWindow
→ invalid frame이면 exact client screen fallback
```

Test:

```text
connected displays
→ same detector/OCR/catalog/presentation pipeline
```

Real/test continuous mode는 동시에 실행하지 않는다.

One-shot 기능은 유지하지만 v1.6 normal page에는 버튼을 두지 않는다.

```text
Ctrl+Shift+F10 = one-shot Tarkov
Ctrl+Shift+F11 = one-shot Test
Ctrl+Shift+F12 = Scanner ON/OFF
```

One-shot candidate cap 12, continuous cap 8.

## 9.8 Structural proposal policy

```text
RED-X connected components
+
rectangle/edge fallback
→ near-duplicate cleanup
→ ranked proposals
```

- structural floor 0.34
- structural score ≠ final identity
- aspect prior는 약한 ranking hint
- high IoU alone으로 differing-edge proposal 삭제 금지
- almost-identical edge jitter만 dedupe
- 모든 production candidate는 semantic stage 통과 필요

## 9.9 Inspect-header semantic gate

Required evidence:

- red close-X body/edge + diagonal X shape
- long neutral inspect-header/frame
- bounded frame-left search-icon lane
- magnifier ring/hollow/handle morphology
- dark title field
- title text evidence

Runtime minimum:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND valid magnifier
AND valid close-X
```

Incomplete lock은 OCR identity path에 진입하지 않는다.

## 9.10 OCR / character policy / matcher

Windows `ko-KR` OCR primary.

- normal pass
- 필요 시 deep/high-contrast/binary/inverse variants
- raw OCR 별도 보존
- current official item-name alphabet/symbol policy
- exact-first
- conservative fuzzy + top1/top2 margin
- bounded unique edit/unknown-glyph recovery
- ambiguous → no identity

Catalog-impossible glyph를 특정 `r/0/I/l`로 product-wide 강제 치환하지 않는다.

## 9.11 User OCR substitution / settings schema v6

Data flow:

```text
raw OCR
→ enabled user substitutions (single ordered pass)
→ catalog sanitation / normalization
→ matcher
```

`ScannerDisplaySettings.CurrentSchemaVersion = 6`.

User substitution contract:

- default empty
- exact user-owned rules
- raw OCR forensic preservation
- raw/substituted/normalized/matched diagnostics 분리
- recursive/chained reprocessing 금지

v1.6 normal settings UI는 hotkey/Mini Scanner order를 우선하지만 기존 substitution data는 migration에서 보존한다.

v6 Mini Scanner fixed header:

- item icon
- official item name

Ordered/visible fields:

- trader sell price
- flea average
- trader price/slot
- flea price/slot
- current needed

## 9.12 Tarkov-font visual recovery

- game font binary public package 포함 금지
- resources.assets read-only discovery
- generation-aware cached font identity
- bounded visual template/cache
- visual top1 + margin 필요
- current catalog 밖 arbitrary Item 생성 금지
- unavailable/error/ambiguous visual path가 healthy OCR evidence를 임의 폐기하지 않음

## 9.13 OCR serialization / exact same-cycle reuse

Reuse conditions:

- same active scan cycle
- same normal/deep class
- width/height/BPP identical
- exact pixel SHA-256 identical

Cycle change → clear. Frame 간 OCR cache 없음.

## 9.14 Continuous verified-state stabilization

Verified state는 bounds/title continuity/Item ID/presentation을 유지한다.

Title-ink shape signature는 continuity evidence일 뿐 identity proof가 아니다.

- different visible glyph shape → identity-change evidence
- title ink 없음 → fail closed
- geometry/title identity change → stale verified result clear
- generic detector miss → bounded miss policy

Verified detail을 계속 보고 있으면 불필요한 OCR 대신 presentation을 재생성해 RequiredTotal 같은 current values를 다시 연결할 수 있다.

## 9.15 Stage latency telemetry

Stages:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

Optimization은 telemetry evidence를 사용한다. Accuracy threshold 완화를 성능 최적화로 취급하지 않는다.

## 9.16 Scanner catalog operation ordering

```text
network RefreshAsync(mode)
local   LoadCacheAsync(mode)
```

둘 다 loaded mode/items/matcher/OCR policy를 교체할 수 있으므로 동일 operation gate에서 순서를 보장한다.

Old-mode writer가 new-mode final state를 덮는 상태 역전을 허용하지 않는다.

## 9.17 Market / mapped data

```text
source fields
→ ScannerCatalogService
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Scanner Page / Mini Scanner / item search details
```

계약:

```text
BestTraderSellPrice = max valid non-flea RUB-equivalent price
BestTraderName = trusted selected source
FleaAveragePrice = positive avg24hPrice
Slots = positive width × height
TraderPricePerSlot = valid trader price / valid slots
FleaPricePerSlot = valid flea price / valid slots
RequiredTotal = ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Market/dimension missing은 affected field만 fail closed. Item identity와 분리한다. Scanner에서 별도 shortage 계산을 추가하지 않는다.

## 9.18 v1.6 Ground Truth correction / re-edit

기본 correction:

```text
detail candidate
→ close-X candidate
→ magnifier candidate
→ item-name ROI candidate
→ correct item/text
→ reviewed Case save
```

UI:

- source image가 커도 viewport에 auto-fit
- display scale과 saved pixel coordinate 분리
- candidate box를 image 위에서 직접 click

Fallback:

- candidate에 정답 없음 → manual rectangle
- object 자체 없음 → `없음`

Saved Case re-edit:

```text
case.json
+ full.png
+ candidate_selection.json
→ restore existing GT/candidate selections
→ same correction editor
→ same Case ID reviewed save
```

복원 failure는 기존 Case를 삭제/추정하지 않고 보존한다.

## 9.19 Diagnostics / retention

Diagnostics root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 evidence:

- full/detail/title/processed images
- annotated image
- case.json
- candidate_selection.json
- raw/substituted/normalized OCR
- Item ID / official name
- matcher top candidates
- structural/header evidence
- mapped presentation
- user Ground Truth

Automatic Case ≠ Ground Truth.

Retention eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Bounds:

```text
max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Reviewed GT 자동 삭제 금지. Corrupt/unknown metadata는 preserve fail closed.

Logs:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
%LocalAppData%/JunhyunHelper/logs/startup.log(.1)
```

bounded rotation.

## 9.20 Scanner UI — v1.6

Normal surface:

- Scanner ON/OFF
- Settings
- Advanced
- item search
- recent recognition log

Settings window:

- three global hotkeys
- Mini Scanner visible fields/order

Advanced window:

- Display Test
- current result correction
- correction dataset management

일반 page에 catalog force-refresh/regression/export/log-delete 같은 developer action을 노출하지 않는다.

`ScannerPage.xaml` 변경 뒤 `MainWindow.ProductUiLayoutSmoke.cs`와 Scanner-specific smoke가 rendered product contract를 검증해야 한다.

## 9.21 Mini Scanner

- MiniMap과 독립
- Topmost / no-activate / no taskbar
- matched Item presentation only
- full-surface drag
- position persists
- actual mode에서 Tarkov foreground + inventory/stash context fail-closed gate
- inventory OCR single-active + latest coalescing
- stale epoch reject
- icon/name fixed header
- five presentation rows ordered per schema-v6 settings

## 9.22 Scanner change impact checklist

Detector/geometry:

```text
capture/proposal
→ candidate bounds/signatures
→ semantic header
→ runtime stability
→ OCR rate
→ GT candidate evidence
→ diagnostics
→ regression
→ published EXE smoke
```

OCR/substitution/matcher/visual:

```text
raw OCR
→ user substitutions
→ character policy
→ catalog resolver
→ visual matcher/recovery
→ confidence/margin
→ diagnostics/GT
→ regression
```

Catalog:

```text
profile GameMode
→ ScannerCoordinator context
→ LoadCacheAsync / RefreshAsync
→ shared operation gate
→ loaded catalog/matcher/OCR policy
→ runtime/search/market state
```

Required count:

```text
Quest/Hideout/Profile
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RequiredTotal
→ ScannerItemPresentationService
```

Scanner에서 이 의미를 재정의하지 않는다.

---

# 10. Ammo / image / preference persistence

Ammo는 read-only comparison/favorites를 담당한다. Raw stats와 Wiki Ballistics effectiveness를 분리하고 자체 effectiveness heuristic을 만들지 않는다.

`ImageCacheService`는 최대 byte/dimension을 검증하고 decode 후 PNG normalize한다. 개별 image failure는 Game Content update 전체 failure가 아니다.

Presentation JSON은 same-directory temp + flush + atomic replace + `.bak` recovery를 사용한다. Scanner settings도 last-known-good 원칙을 따른다.

---

# 11. Program Update / package contract

`GitHubProgramUpdateClient`는 `Propeex/JunhyunHelper` latest public stable을 확인한다.

대상:

- non-draft / non-prerelease
- strict `vMAJOR.MINOR.PATCH`
- current assembly보다 newer
- exact user-facing release asset + checksum

검증 전 current product file을 건드리지 않는다.

Updater는 TEMP self-copy runner에서 parent 종료 후 program-owned files만 transaction 교체하고 실패 시 rollback/restart를 시도한다.

v1.6.0 user-facing package contract:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

Package/folder name에는 version을 넣지 않는다. Version identity는 ProductVersion/tag/release metadata에 둔다.

`packaging/New-ReleasePackage.ps1`가 stable ZIP 생성과 top-level folder/required file validation을 소유한다.

---

# 12. 오류 격리

- Program Update check network failure → app 계속
- image failure → 해당 image만 누락
- preference save failure → diagnostic, app 계속
- invalid Game Content candidate → active known-good 유지
- unsupported Quest availability → Indeterminate/fail closed
- Scanner missing/ambiguous OCR → no Item ID
- Scanner missing icon/market/dimension → affected presentation field만 누락
- Scanner catalog refresh failure → healthy same-mode known-good 보존
- wrong-mode Scanner catalog identity → fail closed
- Scanner diagnostic management failure → runtime identity fatal로 확대하지 않음
- corrupt/unknown retention metadata → preserve
- saved Case re-edit restore failure → original Case preserve
- updater validation failure → current program untouched
- fatal WPF exception → LocalAppData diagnostic + 종료

Catch-all은 best-effort presentation/recovery cleanup 경계에만 사용하고 domain correctness 오류를 정상값처럼 숨기지 않는다.

---

# 13. 성능 구조

의도된 reuse/limit:

- UserProfileStore in-memory snapshot cache
- schema initialization once/store
- Application workspace reference cache
- Inventory-only mutation 시 future planning basis reuse
- Items image/lazy-load reuse
- image download bounded concurrency
- Scanner verified detail에서 unnecessary OCR 억제
- Scanner same-active-cycle exact OCR bitmap reuse
- Scanner process-local decoded icon cache
- verified Scanner presentation periodic refresh
- visual caches bounded + font-generation aware
- Mini Scanner inventory OCR single-active + coalesced
- Scanner catalog writer operations serialized
- automatic diagnostic dataset bounded retention
- Scanner/startup log rotation

동일 입력의 deterministic 결과만 reuse한다. Cache가 cross-frame stale evidence를 권위화하면 안 된다.

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
- `Scanner/ScannerCatalogItem.cs`
- `Scanner/ScannerRecognition.cs`
- `Scanner/ScannerOcrSubstitution.cs`
- `Scanner/ScannerTitleIdentitySignature.cs`

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

Scanner 상세 ownership은 이 문서 9절과 `docs/SCANNER.md`가 권위다.

---

# 15. Tests / release gate

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Quest/Scanner catalog/detector/OCR/substitution/title-identity/lifecycle/concurrency regression을 검사한다.

Current automated suite:

```text
296 tests
```

WPF/Map/Scanner interaction은 xUnit만으로 충분하지 않으므로 CI에서 실제 published EXE를 실행한다.

v1.6 release-candidate gate:

1. Release build
2. full tests
3. Windows x64 self-contained single-file publish
4. ProductVersion / FIRST_RUN exact identity
5. root layout / PDB / legacy dependency audit
6. actual EXE startup
7. rendered Product UI assertions
8. Scanner schema-v6 / normal surface / Mini Scanner assertions
9. Main Map / Factory / MiniMap smoke
10. normal MainWindow close
11. process exit
12. portable root pollution check
13. `packaging/New-ReleasePackage.ps1`
14. `Junhyun-Helper.zip` + `준현 헬퍼/` path validation
15. release artifact upload

Public release는 추가로:

- exact source tag
- stable/latest publication
- fresh independent anonymous public ZIP/checksum redownload
- hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded Product UI/Map/Scanner EXE smoke
- durable `docs/.release-v1.6.0-status.json`
- temporary release/verifier workflow cleanup

Intermediate green gate CI `32700507526` passed build/296 tests/publish/Product UI/Scanner/Map/graceful shutdown before final version/package/doc changes. Latest HEAD must pass again.

---

# 16. 하지 말아야 할 것

- 현재 구현을 자동으로 제품 요구사항이라고 간주
- unknown Quest condition을 true/Current로 추측
- missing ProfileVariable을 0으로 간주
- flexible Item 소비 후보 자동 추측
- content update 중 active DB 먼저 덮기
- Program Update 검증 전 product file 변경
- user.db를 content/program update와 함께 초기화
- Map donor를 style cleanup 목적으로 broad rewrite
- Scanner structural score만으로 Item 확정
- Scanner header/matcher threshold를 인식률 때문에 임의 완화
- Scanner historical alias를 production에 무제한 누적
- Scanner scan-time HTTP/icon identity 추가
- Scanner에서 Needed Items/shortage 의미 재계산
- Scanner catalog shared writer를 다른 synchronization boundary로 분리
- user substitution을 automatic global OCR correction table로 승격
- reviewed Ground Truth를 automatic retention으로 삭제
- title continuity signature를 Item identity proof로 사용
- cross-frame OCR cache로 current evidence 대체
- v1.6 image scale을 saved GT coordinate로 착각
- saved Case restore failure에서 기존 GT overwrite/delete
- UI event handler에 domain truth 복제

---

# 17. 의도적으로 남은 관찰 항목

- 실제 Tarkov 다양한 resolution/DPI/UI scale validation
- `r`, `0`, slash-zero-like glyph, complex Hangul OCR 편차
- short/sparse title OCR
- near-name ambiguity false positive
- mapped market source shape 변화
- 빠른 Item 전환 stale-result isolation
- 장시간 Scanner CPU/memory/UI responsiveness
- telemetry 기반 OCR/visual bottleneck
- EFT Story Chapters 등 ordinary task source 밖 영역
- code signing / installer 없음
- pinned Map donor legacy warning/debt는 구체적 문제 없이 cleanup refactor하지 않음

작은 기술 부채:

`Scanner/ScannerLatencyTypeAliases.cs`는 `ScannerDetectedCandidate` type alias다. v1.6 release risk를 감수해 제거할 제품 이점이 없다. 향후 PATCH에서 제거 시 full build/tests/publish/Product UI/Map/Scanner smoke를 다시 수행한다.

---

# 18. Version / release

권위: `docs/VERSIONING.md`.

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/bug/performance/stability → PATCH +1
- mixed change는 MINOR 우선

v1.6.0은 local Scanner item search, Mini Scanner field order, saved GT Case re-edit 등 새 user capability가 있으므로 MINOR다.

Release identity:

- project `<Version>`
- published ProductVersion
- FIRST_RUN first line
- GitHub tag
- GitHub Release metadata/notes

v1.6.0부터 user ZIP filename/folder는 stable product name이며 version identity field가 아니다.

---

# 19. 빠른 영향 분석 질문

1. 이것은 Game Content, User Progress, Scanner identity catalog, Ground Truth, presentation preference 중 무엇인가?
2. authoritative write/read boundary는 어디인가?
3. 저장 truth인가 계산 result인가?
4. unknown을 false/zero로 바꾸면 안 되는가?
5. Quest current뿐 아니라 future reachability/Needed Items에도 영향이 있는가?
6. consumption ledger/undo에 영향이 있는가?
7. schema compatibility가 필요한가?
8. Map donor인가 first-party인가?
9. Scanner라면 capture/proposal/header/ROI/OCR/substitution/visual/catalog/presentation/search/overlay/GT 중 어느 layer인가?
10. shared state writer가 둘 이상이면 operation ordering이 하나의 synchronization boundary에서 보장되는가?
11. failure가 existing known-good data/program/Item identity/GT를 보존하는가?
12. actual published EXE smoke에 assertion을 추가해야 하는가?
13. 실제 Tarkov 불확실성은 diagnostics/GT로 분리 가능한가?
14. retention/cache가 reviewed evidence/current-frame evidence를 잘못 삭제·대체할 수 있는가?
15. performance optimization이 threshold 완화나 stale cross-frame reuse를 도입하지 않는가?
16. UI scale/coordinate transform이 original GT pixel coordinate를 보존하는가?
17. saved Case re-edit가 same Case ID와 reviewed evidence를 안전하게 유지하는가?
18. release package change가 Program Update의 archive-root validation과 일치하는가?

이 질문에 답할 수 있으면 변경 범위를 대체로 정확히 잡을 수 있다.
