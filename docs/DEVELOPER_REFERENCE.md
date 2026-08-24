# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: **ACTIVE / v1.5.0 PUBLIC RELEASE / VERIFIED**

기준일: 2026-08-24

이 문서는 다음 개발 세션이 대화 기억 없이 저장소만 보고 이어서 작업할 수 있도록 만든 구현 지도다.

권위 우선순위:

1. 사용자의 최신 확정 요구사항
2. `docs/PRODUCT.md`
3. `docs/DECISIONS.md` 및 최신 개별 decision 문서
4. `docs/STATE.md`
5. 영역별 전문 문서
6. 현재 코드/tests
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
- Scanner Ground Truth / diagnostics / regression

Runtime GPT/LLM API는 없다.

현재 공개 릴리즈:

```text
v1.5.0 PUBLIC RELEASE / VERIFIED
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
296 tests / 0 failed / 0 skipped
final PR CI: 32688080850 — SUCCESS
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
```

Durable release evidence:

- `docs/.release-v1.5.0-status.json`
- `docs/RELEASE_1.5.0.md`
- `docs/RELEASE_NOTES_V1.5.0.md`

`vendor/Tarkov-Helper`는 일반 제품 사양이 아니다. Map/MiniMap만 pinned donor revision을 명시적으로 채택한 제한적 예외다. Quest/Hideout/Items/Ammo/Scanner/updater 의미는 JunhyunHelper first-party 코드와 공식 문서가 소유한다.

---

# 2. 저장소를 읽는 순서

새 대화/새 개발 세션:

1. `AGENTS.md`
2. `docs/STATE.md`
3. `docs/CURRENT_STATE.md`
4. `docs/PRODUCT.md`
5. `docs/DECISIONS.md`
6. 최신 관련 개별 decision 문서
7. `docs/DEVELOPER_REFERENCE.md`
8. `docs/ARCHITECTURE.md`
9. 작업 영역 전문 문서
10. 관련 코드/tests/current PR

Scanner 작업이면 반드시 추가로:

- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- 최신 public release record `docs/RELEASE_1.5.0.md`

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

- **Core**: canonical domain, deterministic 계산, Quest availability, Scanner pure policies/signatures/matcher contracts.
- **Application**: authoritative user mutation/use case, workspace orchestration, planning.
- **Infrastructure**: source/HTTP/SQLite/files/content activation/program update/Scanner catalog persistence.
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge, startup/update UX.
- **vendor Map/MiniMap**: pinned donor source. broad ownership 이전 금지.

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
| Scanner settings | user presentation/hotkey/OCR substitution | `%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)` |
| Scanner font recovery cache | locally discovered Tarkov title fonts + generation manifest | `%LocalAppData%/JunhyunHelper/scanner/fonts/` |
| Scanner automatic/reviewed diagnostic Cases | runtime evidence + user Ground Truth | `%LocalAppData%/JunhyunHelper/scanner/diagnostics/` |
| Scanner/startup logs | runtime diagnostics | `%LocalAppData%/JunhyunHelper/logs/` |
| Map general data/artwork | pinned release Assets | portable `Assets/` |
| Program files | exact GitHub stable release | portable folder |

Game Content update와 Program Update는 별개다. 둘 다 `user.db` 및 Scanner reviewed Ground Truth를 덮지 않는다.

---

# 5. Startup / shell / lifecycle

대표 흐름:

```text
App.OnStartup
  ├─ fatal exception hooks
  ├─ updater apply-mode 처리
  ├─ LocalAppData log path 준비
  ├─ ScannerDiagnosticRetentionService 시작
  ├─ MainWindow 표시
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

MainWindow는 orchestration layer다. 새 domain truth를 MainWindow event handler에 넣지 않는다.

Shutdown에서는 Scanner runtime/OCR/font resources/retention timer 등 background-owned resource가 정상 종료돼야 한다. CI graceful-shutdown smoke가 이 경계를 검증한다.

---

# 6. Profile / Quest / Hideout / Items 핵심 흐름

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
- 한 requirement 내부 accepted statuses = OR
- unknown/unsupported fact를 optimistic `Current`로 만들지 않음
- exact observed ProfileVariable이 있으면 권위값
- task-pool compatibility는 audited exact shape에서만 synthetic fact 생성

`QuestFutureReachabilityEvaluator`는 현재 availability와 미래 Item 필요 가능성을 분리한다. Unknown future path는 `IndeterminatePotential`로 보호한다.

### v1.5.0 task-pool live audit

2026-08-24 live data를 `regular`, `pve`, `pvp-season`에서 감사했다.

`Core/Quests/QuestTaskPoolVariableCompatibility.cs`는 GameMode와 audited pool membership/threshold/trader/shape가 맞을 때만 compatibility를 적용한다.

Source가 drift하면 해당 pool을 fail closed한다. `확인 필요`를 UI에서 억지로 숨기지 않는다.

## 6.3 Hideout

미입력 station은 Lv.0이다. 미래 upgrade fixed material은 Needed Items에 포함한다.

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

## 7.1 v1.5.0 unified update

`MainWindow.DataUpdate.cs`의 사용자 top-level update는 일반 Game Content activation 후 현재 GameMode Scanner catalog/market refresh까지 orchestration한다.

Scanner refresh partial failure는 general content success를 rollback하지 않는다. 기존 healthy same-mode Scanner cache가 있으면 보존하고 status에서 부분 실패를 보고한다.

Scanner 전용 강제 refresh는 고급/복구 surface다.

---

# 8. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper가 소유하는 주요 bridge 예:

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

Map product smoke는 map selector, floor selector, asset source changes, viewport preservation, Factory/MainMap/MiniMap behavior 등을 실제 published EXE에서 검증한다.

---

# 9. Scanner — 전체 구현 지도

Scanner는 실제 public 제품 기능이다.

현재 specialist contract: `docs/SCANNER.md`.

버전별 주요 기준선:

- v1.1.3: Scanner Lab v3.8 multi-candidate structural/semantic recognition 복원
- v1.1.4~v1.1.x: market/Needed Items/diagnostics/icon/catalog health 보강
- v1.2.0: title anchors, Tarkov-font visual recovery, recognition diagnostics, one-shot
- v1.2.1: font/cache generation, bounded visual caches, lifecycle/capture hardening
- v1.2.2: Scanner catalog GameMode writer ordering hardening
- v1.3.x: live inspect-header/ROI/current-catalog glyph hardening
- v1.4.x: Ground Truth dataset/correction/regression 및 실제 live evidence 보강
- **v1.5.0**: mapped-data repair, unified data refresh, user OCR substitutions, candidate GT correction, latency telemetry, exact same-cycle OCR reuse, continuous title-identity stabilization, diagnostic retention, Scanner UI finishing

## 9.1 Scanner 제품 경계

```text
screen pixels
→ capture
→ structural proposals
→ semantic inspect-header validation
→ item-name ROI
→ ko-KR OCR
→ user substitution
→ official-catalog sanitation/matching
→ optional visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
```

금지:

- game memory read
- DLL injection
- packet interception
- process-internal game data
- scan-time HTTP
- icon/image identity
- current catalog 밖 Item 생성
- evidence 없는 threshold/candidate-cap 완화

## 9.2 Core Scanner 주요 파일

- `Core/Scanner/ScannerCatalogItem.cs`
  - Item ID/name/icon/market/dimension snapshot contract.
- `Core/Scanner/ScannerRecognition.cs`
  - matcher result/reason/confidence/second score contract.
- `Core/Scanner/ScannerOcrSubstitution.cs`
  - user exact substitution rule/normalization behavior.
- `Core/Scanner/ScannerTitleIdentitySignature.cs`
  - already-verified detail continuity용 title-ink shape signature.
  - Item identity proof가 아님.

## 9.3 Infrastructure Scanner

- `Infrastructure/Scanner/ScannerCatalogService.cs`
  - GameMode별 full-item catalog download/cache/load.
  - current Korean official identity catalog.
  - semantic resolver.
  - trader/flea/dimension parse.
  - wrong-mode cache fail-closed.
  - `LoadCacheAsync` / `RefreshAsync` writer ordering gate.
  - healthy same-mode cache preservation.

## 9.4 Desktop Scanner orchestration/presentation

- `Scanner/ScannerCoordinator.cs`
  - settings, catalog preparation, real/test mutual exclusion, runtime/profile lifecycle.
- `Scanner/ScannerCoordinator.CatalogStatus.cs`
  - catalog readiness/status projection.
- `Scanner/ScannerCoordinator.OcrSubstitutions.cs`
  - user substitution settings boundary.
- `Scanner/ScannerRuntimeService.cs`
  - continuous recognition lifecycle, verified state, presentation refresh.
- `Scanner/ScannerRuntimeService.OneShot.cs`
  - one-shot observation path; same candidate observation semantics 사용.
- `Scanner/ScannerRuntimeService.Latency.cs`
  - catalog-resolution latency wrapper.
- `Scanner/ScannerRuntimeService.TitleIdentity.cs`
  - candidate title continuity signature normalization.
- `Scanner/ScannerItemPresentationService.cs`
  - Item ID → catalog/GameContent/ItemsWorkspace → `ScannerItemSnapshot`.
- `Scanner/ScannerLocalIconService.cs`
  - local image-cache read-only projection + frozen ImageSource memory cache.
- `Scanner/ScannerRecognitionDebugStore.cs`
  - latest recognition evidence snapshot.
- `Scanner/ScannerPage.xaml` / partial code-behind
  - Scanner normal/settings/advanced UI.
- `Scanner/MiniScannerWindow.xaml` / partial code-behind
  - no-activate Topmost overlay + quick correction context menu.

## 9.5 Capture / detector / OCR / visual files

- `Scanner/ScannerLab38WindowsVision.cs`
  - Tarkov/display capture, Scanner Lab v3.8 proposal generation, raw WinRT OCR implementation.
- `Scanner/FontAwareScannerOcrEngine.cs`
  - semantic OCR path + constrained local-font visual corroboration/recovery.
- `Scanner/SerializedScannerOcrEngine.cs`
  - shared WinRT OCR serialization + v1.5 exact same-cycle bitmap reuse.
- `Scanner/TarkovTitleFontProvider.cs`
  - local Tarkov font discovery/cache/source-generation management.
- `Scanner/ScannerFullCatalogVisualMatcher.cs`
  - current official catalog 범위의 conservative visual recovery.
- inspect-header/refiner 관련 first-party files
  - red close-X + neutral frame + magnifier + title field를 결합해 authoritative title ROI를 만든다.

파일명이 과거 문서와 다를 수 있으므로 실제 `Scanner/` 디렉터리를 먼저 확인하고 타입/partial ownership을 판단한다.

## 9.6 v1.5 Ground Truth / diagnostics files

- `Scanner/ScannerCandidateGroundTruth.cs`
  - detector candidate evidence를 correction UI/Case persistence에 연결.
- `Scanner/ScannerCorrectionWindow.xaml(.cs)`
  - candidate-first selection + manual rectangle / `없음` fallback.
- `Scanner/ScannerDiagnosticRetentionService.cs`
  - automatic unreviewed diagnostic retention.
- `Scanner/ScannerLatencyTelemetry.cs`
  - capture/proposal/header/OCR/visual/catalog/presentation/end-to-end telemetry.
- `Scanner/ScannerOcrSubstitutionSettingsWindow.xaml(.cs)`
  - user substitution CRUD/enable/reset UI.
- `Scanner/ScannerDisplaySettings.cs`
  - Scanner settings schema v5, hotkeys/display/substitutions.

## 9.7 Capture modes

Real:

```text
EscapeFromTarkov window
→ Borderless client-area
→ PrintWindow
→ invalid frame이면 exact client screen fallback
```

Test:

```text
all connected displays
→ same detector/OCR/catalog/presentation pipeline
```

Real/test continuous mode는 동시에 실행하지 않는다.

One-shot:

- normal UI `1회 스캔`
- TarkovWindow 한 번 정밀 분석
- continuous state 영구 변경 없음
- local healthy catalog only
- candidate cap 12

Continuous cap은 8이다.

## 9.8 Structural proposal policy

```text
RED-X connected components
+
rectangle/edge fallback
→ near-duplicate cleanup
→ ranked proposals
```

계약:

- structural floor 0.34
- structural score는 final identity가 아님
- aspect prior는 약한 ranking hint
- high IoU만으로 서로 다른 edge proposal을 삭제하지 않음
- almost-identical edge jitter만 dedupe
- proposal은 semantic stage를 통과해야 함

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

First title glyph connected component가 title ROI left edge를 소유하지 않는다.

Partial/incomplete lock은 OCR identity path에 진입하지 않는다.

## 9.10 OCR / character policy / matcher

Windows `ko-KR` OCR을 primary recognizer로 사용한다.

- normal pass
- 필요 시 deep/high-contrast/binary/inverse variants
- raw OCR 별도 보존
- current official item-name alphabet/symbol policy
- exact-first
- conservative fuzzy + top1/top2 margin
- bounded unique edit/unknown-glyph recovery
- ambiguous → no identity

Current-catalog impossible glyph를 특정 `r/0/I/l`로 product-wide 강제 치환하지 않는다.

## 9.11 User OCR substitution — v1.5.0

Data flow:

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matcher
```

`ScannerDisplaySettings.CurrentSchemaVersion = 5`.

계약:

- default empty
- exact user-owned rules
- add/delete/enable-disable/reset
- raw OCR forensic preservation
- raw/substituted/normalized/matched diagnostics 분리
- recursive/chained reprocessing 금지

Substitution 관련 변경 시 `Core/Scanner/ScannerOcrSubstitution.cs`, settings normalization, settings window, runtime OCR handoff, tests를 함께 확인한다.

## 9.12 Tarkov-font visual recovery

OCR이 비거나 손상된 경우 current official Item universe 안에서 constrained visual recovery를 사용할 수 있다.

- game font binary는 public package에 넣지 않음
- resources.assets read-only discovery
- generation-aware cached font identity
- bounded visual template/cache
- visual top1 + margin 필요
- current catalog 밖 arbitrary Item 생성 금지
- unavailable/error/ambiguous visual path가 healthy OCR evidence를 임의 폐기하지 않음

## 9.13 OCR serialization / exact same-cycle reuse

`SerializedScannerOcrEngine`은 title OCR과 inventory-context OCR의 serialization boundary다.

v1.5.0 reuse 조건:

- 같은 active scan cycle
- same normal/deep class
- width/height/BPP 동일
- exact pixel SHA-256 동일

만족 시 기존 WinRT OCR result를 재사용할 수 있다.

Cycle이 바뀌면 cache를 폐기한다. Frame 간 OCR cache는 없다.

## 9.14 Continuous verified-state stabilization

Continuous path는 stable geometry/semantic identity 이후 OCR을 수행한다.

Verified state에는:

- verified bounds
- title continuity evidence
- Item ID
- presentation snapshot

을 유지한다.

v1.5.0은 raw BGRA title hash 대신 dark-background variation을 덜 타는 title-ink shape signature를 continuity evidence로 사용할 수 있다.

중요:

- signature는 Item identity proof가 아님
- different visible glyph shape는 identity change evidence
- title ink 없음은 fail closed
- geometry/title identity change 시 stale verified result 즉시 clear
- generic detector miss는 기존 bounded miss policy 사용

Verified detail을 계속 보고 있으면 OCR 자체는 불필요하게 반복하지 않고 presentation만 주기적으로 재생성해 `RequiredTotal` 같은 현재 값을 다시 연결한다.

## 9.15 Stage latency telemetry

`ScannerLatencyTelemetry` stage:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

Optimization은 이 telemetry 근거를 사용한다. Accuracy threshold 완화를 성능 최적화로 취급하지 않는다.

## 9.16 Scanner catalog operation ordering

두 writer:

```text
network RefreshAsync(mode)
local   LoadCacheAsync(mode)
```

둘 다 loaded mode/items/matcher/OCR policy/diagnostics를 교체할 수 있으므로 동일 operation gate에서 순서를 보장한다.

금지되는 상태 역전:

```text
old-mode refresh starts
→ new-mode cache load applies
→ old-mode refresh finishes
→ old mode becomes final state
```

Concurrency regression을 유지한다.

## 9.17 Market/mapped data

Source fields
→ `ScannerCatalogService`
→ `ScannerCatalogItem`
→ `ScannerItemPresentationService`
→ Scanner Page / Mini Scanner.

계약:

```text
BestTraderSellPrice = max valid non-flea RUB-equivalent price
FleaAveragePrice = positive avg24hPrice
Slots = positive width × height
TraderPricePerSlot = valid trader price / valid slots
FleaPricePerSlot = valid flea price / valid slots
RequiredTotal = ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

가능하면 best trader name도 presentation에 포함한다.

Market/dimension missing은 affected field만 fail closed. Item identity health와 분리한다.

Scanner에서 별도 shortage 계산을 추가하지 않는다.

## 9.18 Ground Truth correction

기본 correction은 candidate-first다.

```text
detail candidate
→ close-X candidate
→ magnifier candidate
→ item-name ROI candidate
→ correct item/text
→ reviewed Case save
```

Fallback:

- candidate에 정답이 없으면 manual rectangle
- object 자체를 탐지하지 못했다면 `없음`

저장 시 candidate ID/rank/score/geometry를 함께 보존한다.

이 정보로 proposal recall/ranking/semantic anchor/ROI/OCR/matcher failure를 분리한다.

## 9.19 Diagnostics / retention

Diagnostics root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 Case evidence:

- full/detail/title/processed images
- annotated image
- case.json
- raw/substituted/normalized OCR
- Item ID / official name
- matcher top candidates
- structural/header evidence
- detector candidates
- mapped presentation
- user Ground Truth

Automatic diagnostic Case는 Ground Truth가 아니다.

Retention:

```text
eligible only if:
retention == automatic_sample
AND review_status == unreviewed

max age = 30 days
max cases = 300
max bytes = 512 MiB
recent protection = 2 hours
```

Reviewed Ground Truth는 자동 삭제 금지. Corrupt/unknown metadata는 fail closed하여 보존한다.

Logs:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
%LocalAppData%/JunhyunHelper/logs/startup.log(.1)
```

bounded rotation을 사용한다.

## 9.20 Scanner UI

Normal surface:

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- recent recognition history

Settings expander:

- hotkeys
- OCR substitutions
- Mini Scanner display options

Advanced/diagnostic expander:

- Display Test
- 인식 이미지
- regression
- Ground Truth export/manage
- catalog recovery/force refresh
- 로그 삭제
- diagnostic storage state

`ScannerPage.xaml` 변경 뒤 `MainWindow.ProductUiLayoutSmoke.cs`와 Scanner-specific mini smoke가 새 rendered product contract를 충분히 검증하는지 확인한다.

## 9.21 Mini Scanner

- MiniMap과 독립
- Topmost / no-activate
- matched Item 결과만 표시
- visible 상태 drag 가능
- position settings persist
- actual mode에서는 Tarkov foreground + inventory/stash context fail-closed gate
- inventory OCR single-active + latest coalescing
- stale epoch reject

v1.5.0 quick correction:

- right-click context menu `현재 결과 교정`
- latest `ScannerRecognitionDebugStore` snapshot 사용
- current Scanner coordinator와 correction window 연결

## 9.22 Scanner 변경 영향 체크리스트

### Detector/geometry

```text
capture/proposal implementation
→ candidate bounds/signatures
→ semantic header validation
→ runtime stability
→ OCR rate
→ Ground Truth candidate evidence
→ diagnostics
→ synthetic/live regression
→ packaged EXE smoke
```

### OCR/substitution/matcher/visual

```text
raw OCR engine
→ user substitutions
→ ScannerOcrCharacterPolicy
→ ScannerCatalogService resolver
→ visual matcher/recovery
→ confidence/margin
→ candidate selection
→ diagnostics/GT
→ regression
```

### Catalog load/refresh

```text
profile GameMode
→ ScannerCoordinator context
→ LoadCacheAsync / RefreshAsync
→ shared operation gate
→ loaded catalog/matcher/OCR policy
→ runtime identity/market state
```

### Market presentation

```text
source item fields
→ ScannerCatalogService parser
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Scanner Page / Mini Scanner
→ market shape tests
```

### Required count

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

Ammo는 read-only comparison/favorites를 담당한다. Ammo raw stats와 Wiki Ballistics effectiveness를 분리하고 자체 effectiveness heuristic을 만들지 않는다.

`ImageCacheService`는 최대 byte/dimension을 검증하고 decode 후 PNG normalize한다. 개별 image failure는 Game Content update 전체 실패가 아니다.

Presentation JSON은 `AtomicJsonFileStore` 계열 same-directory temp + flush + atomic replace + `.bak` recovery를 사용한다. Scanner settings도 같은 last-known-good 원칙을 따른다.

---

# 11. Program Update

`GitHubProgramUpdateClient`는 `Propeex/JunhyunHelper` latest public stable을 확인한다.

대상:

- non-draft / non-prerelease
- strict `vMAJOR.MINOR.PATCH`
- current assembly보다 newer
- exact Windows ZIP + `SHA256SUMS.txt`

검증 전 current product file을 건드리지 않는다.

Updater는 TEMP self-copy runner에서 parent 종료 후 program-owned files만 transaction 교체하고 실패 시 rollback/restart를 시도한다.

v1.5.0 public package는 independent anonymous redownload + EXE smoke까지 검증됐다.

---

# 12. 오류 격리

- Program Update check network failure → app 계속
- image failure → 해당 image만 누락
- preference save failure → diagnostic, app 계속
- invalid Game Content candidate → active known-good 유지
- unsupported Quest availability → Indeterminate/fail closed
- Scanner missing/ambiguous OCR → no Item ID
- Scanner missing icon/market/dimension → 해당 presentation field만 누락
- Scanner catalog refresh failure → healthy same-mode known-good가 있으면 보존
- wrong-mode Scanner catalog identity → fail closed
- Scanner diagnostics/log clear failure → Scanner runtime fatal로 확대하지 않음
- corrupt/unknown diagnostic retention metadata → 보존
- updater validation failure → current program untouched
- fatal WPF exception → LocalAppData diagnostic + 종료

Catch-all은 best-effort presentation/recovery cleanup 경계에만 사용하고 domain correctness 오류를 정상값처럼 숨기지 않는다.

---

# 13. 성능 구조

현재 의도된 재사용/제한:

- UserProfileStore in-memory snapshot cache
- schema initialization once per store instance
- Application workspace reference cache
- Inventory-only mutation 시 future planning basis 재사용
- Items image/lazy-load reuse
- image download bounded concurrency
- Scanner verified detail에서 불필요한 OCR 반복 억제
- Scanner same-active-cycle exact OCR bitmap reuse
- Scanner process-local decoded icon cache
- verified Scanner presentation periodic refresh
- visual caches bounded + font-generation aware
- Mini Scanner inventory OCR single-active + coalesced
- Scanner catalog shared writer operations serialized
- automatic diagnostic dataset bounded retention
- Scanner/startup log rotation

동일 입력의 deterministic 결과만 재사용한다. Cache가 제품 의미를 바꾸거나 cross-frame stale evidence를 권위화하면 안 된다.

---

# 14. 주요 first-party 파일 색인

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

# 15. 테스트 / release gate

`tests/JunhyunHelper.Tests`는 deterministic domain/storage/update/Quest/Scanner catalog/detector/OCR/substitution/title-identity/lifecycle/concurrency regression을 검사한다.

v1.5.0 public source automated total:

```text
296 passed / 0 failed / 0 skipped
```

Desktop WPF/Map/Scanner interaction은 xUnit만으로 충분하지 않으므로 CI에서 실제 publish된 EXE를 실행한다.

Release-candidate gate:

1. Release build
2. full tests
3. Windows x64 self-contained single-file publish
4. ProductVersion / FIRST_RUN exact identity
5. root layout / PDB / legacy dependency audit
6. actual EXE startup
7. rendered Product UI assertions
8. Scanner recognition/settings/log/product contract assertions
9. Scanner/Mini Scanner smoke
10. Main Map / Factory / MiniMap smoke
11. normal MainWindow close
12. process exit
13. portable root pollution check

정식 public release는 추가로:

- exact source tag
- draft asset redownload + hash/package/ProductVersion/FIRST_RUN/EXE smoke
- public stable/latest
- independent fresh-runner anonymous public ZIP + SHA256SUMS redownload
- hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded Product UI/Map/Scanner EXE smoke
- durable `docs/.release-vX.Y.Z-status.json`
- one-shot release/verifier workflow cleanup

을 요구한다.

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
- Scanner에서 Needed Items/shortage 의미 별도 재계산
- Scanner catalog shared writer를 서로 다른 synchronization boundary로 분리
- user substitution을 automatic global OCR correction table로 승격
- reviewed Ground Truth를 automatic retention으로 삭제
- title continuity signature를 Item identity proof로 사용
- cross-frame OCR cache로 현재 evidence를 대체
- UI event handler에 domain truth 복제

---

# 17. 의도적으로 남은 범위 / 관찰 항목

- 실제 Tarkov의 다양한 resolution/DPI/UI scale 추가 validation
- `r`, `0`, slash-zero-like glyph, complex Hangul OCR 자체의 환경별 편차
- short/sparse title OCR
- near-name ambiguity false positive 감시
- mapped market data source 변화 감시
- 빠른 Item 전환 stale-result isolation
- 장시간 Scanner CPU/memory/UI responsiveness
- telemetry 기반 실제 OCR/visual recovery 병목 분석
- EFT Story Chapters 등 ordinary task source 밖 영역
- code signing / installer 없음
- pinned Map donor legacy warning/debt는 구체적 문제 없이 cleanup refactor하지 않음

### 작은 기술 부채

`Scanner/ScannerLatencyTypeAliases.cs`는 telemetry 통합 과정에서 `ScannerDetectedCandidate`에 대한 type alias로 남은 작은 구현 부채다. v1.5.0 source는 이미 공개 검증됐으므로 release source를 흔들기 위해 제거하지 않는다. 향후 PATCH에서 관련 선언을 실제 type으로 정리할 경우 full build/tests/publish smoke를 다시 수행한다.

---

# 18. 버전 / 릴리즈

권위: `docs/VERSIONING.md`.

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/bug/performance/stability → PATCH +1
- 혼합 변경은 MINOR 우선

v1.5.0은 사용자 OCR 설정, candidate correction UX 등 새 사용자 기능과 product finishing scope가 포함되어 MINOR다.

현재 public release:

```text
v1.5.0
source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
296 passed / 0 failed / 0 skipped
release run: 32691423654
public verifier: 32691641614
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
```

Release identity가 일치해야 한다.

- project `<Version>`
- published ProductVersion
- `FIRST_RUN_KO.txt`
- GitHub tag
- ZIP filename
- release notes

---

# 19. 빠른 영향 분석 질문

1. 이것은 Game Content, User Progress, Scanner identity catalog, Ground Truth, presentation preference 중 무엇인가?
2. authoritative write/read boundary는 어디인가?
3. 저장 truth인가 계산 결과인가?
4. unknown을 false/zero로 바꾸면 안 되는가?
5. Quest current뿐 아니라 future reachability/Needed Items에도 영향이 있는가?
6. consumption ledger/undo에 영향이 있는가?
7. schema compatibility가 필요한가?
8. Map donor인가 first-party인가?
9. Scanner라면 capture/proposal/header/ROI/OCR/substitution/visual/catalog/presentation/overlay/GT 중 어느 계층인가?
10. shared state writer가 둘 이상이면 operation ordering이 하나의 synchronization boundary에서 보장되는가?
11. failure가 기존 known-good data/program/Item identity/GT를 보존하는가?
12. actual published EXE smoke에 assertion을 추가해야 하는가?
13. 실제 Tarkov에서 남는 불확실성은 diagnostics/GT로 분리 가능한가?
14. retention 또는 cache가 reviewed evidence/current-frame evidence를 잘못 삭제·대체할 가능성이 있는가?
15. 새로운 성능 최적화가 threshold 완화나 stale cross-frame reuse를 몰래 도입하지 않는가?

이 질문에 답할 수 있으면 변경 범위를 대체로 정확히 잡을 수 있다.
