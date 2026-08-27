# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록한다.

기준일: 2026-08-27  
상태: **v1.7.11 PUBLIC STABLE / MAINTENANCE MODE**

정확한 현재 릴리즈 SHA·CI·asset·schema 상태는 `docs/STATE.md`를 권위 있는 운영 인덱스로 사용한다. 유지보수 시 변경 원칙과 외부 Live Probe/성능 검증 경계는 `docs/MAINTENANCE_CONTRACTS.md`를 따른다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — external image decode 및 Scanner local-font rendering
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

- Quest availability/future reachability
- Needed Items / cleanup safety
- Item/Ammo canonical meaning
- Scanner pure policies/signatures/matcher contracts

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

- shell/pages/dialogs
- image cache presentation
- Scanner capture/OCR/runtime/search/diagnostics
- Map product bridge
- startup/update UX

Domain truth를 WPF event handler에 복제하지 않는다.

### Map/MiniMap donor

`vendor/Tarkov-Helper` 전체를 제품 사양으로 승계하지 않는다. Map/MiniMap만 pinned source compile-link 예외다.

Pinned revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 3. Data authority / lifecycle

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `content/<mode>/content.db` |
| User Progress | user-confirmed facts | `user.db` |
| Inventory | user quantity + consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | validated/normalized presentation bytes | `image-cache/` |
| Scanner identity/market catalog | current full-item source + official Korean identity | `scanner/catalog/` + memory |
| Scanner settings | display/hotkey/order/OCR substitution | `scanner-settings.json(.bak)` |
| Scanner title font cache | installed Tarkov assets read-only extraction | `scanner/fonts/` |
| Scanner Cases / reviewed GT | runtime evidence + user truth | `scanner/diagnostics/` |
| Scanner/startup logs | runtime diagnostics | `logs/*.log(.1)` |
| Map artwork/config/general markers | pinned donor bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable product folder |

이 lifecycle들은 서로 분리한다. Program/Game Content update가 `user.db`나 reviewed GT를 덮어쓰지 않는다.

## 4. Startup / composition / shutdown

```text
App.OnStartup
→ fatal exception hooks
→ updater apply-mode
→ LocalAppData diagnostics/log setup
→ Scanner retention service
→ MainWindow
→ startup Program Update check (non-smoke)

MainWindow.Window_Loaded
→ profile/content/workspaces
→ Map bridge
→ Scanner context/catalog/runtime
```

Shutdown은 Scanner runtime/OCR/font/retention/background resource를 정상 종료해야 한다. CI graceful-shutdown smoke가 이 경계를 검증한다.

## 5. Game Content update architecture

```text
remote source
→ parse/import
→ canonical/relational validation
→ candidate content
→ last-known-good baseline completeness guard
→ candidate content.db
→ SQLite read-back/integrity
→ atomic active replacement
→ image prefetch
```

핵심:

- candidate 완성 전 active DB overwrite 금지
- failed candidate 폐기
- known-good active 유지
- 기존 정상 snapshot이 있으면 핵심 entity/relationship/localization/resource coverage의 suspicious shrink를 `ContentUpdateCompletenessGuard`가 차단
- 현재 retained floor는 baseline 대비 50%; 이는 Tarkov 절대 행 수 제한이 아니라 부분 payload 방어다
- User Progress에 영향 없음

Top-level Game Data update는 general content activation 이후 current GameMode Scanner catalog/market refresh까지 orchestration한다. Scanner-only partial failure는 general success를 rollback하지 않는다.

외부 최신 계약 감시는 `.github/workflows/live-data-probe.yml`에서 일반 hermetic CI와 분리해 수행한다. Live Probe는 baseline-relative runtime guard를 대체하지 않는다.

## 6. Profile / Quest / Needed Items

```text
Profile facts
→ Quest availability/current/future reachability
→ Hideout future requirements
→ NeededItemRequirementBuilder
→ NeededItemCalculator
→ Needed Items / cleanup protection
```

Unknown prerequisite는 optimistic current로 바꾸지 않는다. Flexible hand-in 실제 Item을 자동 추측하지 않는다.

## 7. Map / MiniMap architecture

Map artwork/config/general markers는 donor bundle, current Quest state/geometry는 JunhyunHelper bridge를 사용한다.

Map은 독립 subsystem이고 Quest만 current JunhyunHelper data와 bridge한다.

검증된 donor code는 concrete defect/performance evidence 없이 broad refactor하지 않는다.

## 8. Scanner subsystem

Scanner는 화면 픽셀을 current official Korean item catalog의 Item ID에 연결한다.

Canonical specialist document: `docs/SCANNER.md`.

### 8.1 Logical composition

```text
ScannerPage / ScannerSettingsWindow / ScannerAdvancedWindow / MiniScannerWindow
        │
        ▼
ScannerCoordinator
├─ settings schema v6 / hotkeys / OCR substitutions / Mini order
├─ local item search/details
├─ ScannerCatalogService
├─ ScannerRuntimeService
│  ├─ capture + structural proposals
│  ├─ semantic inspect-header/title refinement
│  ├─ SerializedScannerOcrEngine
│  ├─ FontAwareScannerOcrEngine
│  ├─ ScannerLatencyTelemetry
│  ├─ verified title continuity
│  └─ ScannerItemPresentationService
├─ ScannerRecognitionDebugStore
├─ Ground Truth correction / saved Case re-edit
└─ Mini Scanner overlay/context
```

Title OCR과 inventory-context OCR은 하나의 WinRT serialization boundary를 공유한다.

### 8.2 Recognition data flow

```text
Tarkov client/display pixels
→ capture
→ structural proposals
→ red close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user substitution
→ current-catalog sanitation/normalization
→ conservative official-name matching
→ optional strict visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

Single evidence source만으로 Item identity를 확정하지 않는다.

### 8.3 Structural / semantic contract

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous cap = 8
one-shot cap = 12
```

Proposal policy:

- RED-X connected component + rectangle/edge fallback
- aspect prior = weak ranking hint
- high IoU alone ≠ duplicate
- different real edges survive to semantic validation
- near-identical jitter only deduped

Semantic required evidence:

- red close-X morphology
- neutral inspect header/frame
- bounded frame-left magnifier lane
- magnifier ring/hollow/handle
- dark title field
- title text evidence

### 8.4 OCR / matching

- Windows ko-KR primary
- raw OCR preserved
- normal + bounded deep variants
- current catalog-derived character policy
- exact-first
- conservative fuzzy + top1/top2 margin
- bounded unique unknown/edit recovery
- impossible glyph not globally forced to r/0/I/l
- visual recovery restricted to current official item universe
- cross-frame OCR cache prohibited

### 8.5 User OCR substitutions / schema v6

```text
raw OCR
→ enabled user substitutions (single ordered pass)
→ sanitation / normalization
→ matcher
```

User substitutions remain default-empty, exact, non-recursive and preserve raw evidence.

Settings schema v6 additionally owns Mini Scanner ordered fields.

### 8.6 Same-cycle reuse / continuity

OCR reuse requires same active cycle + same pass class + dimensions/BPP + exact pixel SHA-256.

Title continuity signature is only trusted-detail continuity evidence, never Item identity proof.

### 8.7 Catalog writer ordering

`LoadCacheAsync(mode)` and `RefreshAsync(mode)` share ordering so an older GameMode writer cannot overwrite a newer final state.

### 8.8 Item presentation / search

```text
Item ID
→ Scanner catalog official identity/market/dimensions
→ GameContent canonical item/Wiki
→ ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
→ ScannerItemSnapshot
→ Mini Scanner / Scanner search details
```

Mapped values:

- best non-flea trader RUB price
- best trader name when trusted
- flea positive avg24hPrice
- positive width × height slots
- trader/flea price per slot
- **current needed = `NeededItemCalculator`가 계산한 보유량 차감 후 `RemainingTotal`**

Scanner presentation에서 raw inventory를 별도로 빼거나 `RequiredTotal`로 되돌리지 않는다. FIR/일반 소비 의미는 기존 Needed Items 계산 결과를 그대로 재사용한다.

Market/dimension failure clears affected field only.

v1.6 이후 item search는 current local/memory full-item catalog와 local icon cache를 사용한다. Search-time network is forbidden.

### 8.9 Scanner UI architecture

Normal:

- Scanner ON/OFF
- Settings
- Advanced
- item search
- recognition log

Settings:

- 3 global hotkeys
- Mini Scanner field visibility/order

Advanced:

- Display Test
- current result correction
- correction dataset management

One-shot remains via global hotkeys; visible normal-page one-shot button is no longer required.

### 8.10 Mini Scanner schema v6

Fixed identity header:

- icon
- official name

Ordered/optional rows:

- trader sell price
- flea average
- trader price/slot
- flea price/slot
- current needed

Window remains Topmost, no-activate, no-taskbar, draggable, stale-result-safe.

### 8.11 Ground Truth correction / re-edit

```text
full.png
→ auto-fit display
→ candidate boxes direct click
→ manual/none fallback
→ original-pixel-coordinate GT
→ reviewed save
```

Display scale is not dataset coordinate authority.

Saved Case re-edit:

```text
case.json + full.png + candidate_selection.json
→ restore truth/selections
→ same correction editor
→ same Case ID reviewed update
```

Restore failure preserves original data.

### 8.12 Regression / retention

Replay:

```text
reviewed full.png
→ current production pipeline
→ STILL_CORRECT / SOLVED / STILL_FAILING / REGRESSION / ERROR
```

Existing correct Case failure = REGRESSION regardless of average.

Automatic delete eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Bounds: 30 days / 300 cases / 512 MiB / 2h recent protection. Reviewed GT never auto-deleted. Corrupt metadata preserved fail closed.

## 9. Program Update / release package

Program Update:

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

LocalAppData user data is untouched.

Stable package layout:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

ZIP/folder names are stable product names, not version identity. Version comes from project/ProductVersion/tag/release metadata.

`packaging/New-ReleasePackage.ps1` owns archive construction and top-level path validation.

## 10. Persistence / atomicity

Important JSON preferences use same-directory temp + flush + atomic replacement + `.bak` recovery.

Mutable runtime state/logs must not be written beside portable executable.

## 11. Performance principles

- deterministic/canonical results only reused
- image download bounded
- Scanner verified detail avoids unnecessary OCR
- same-cycle exact bitmap OCR reuse only
- icon process-memory decode cache
- font/visual caches bounded + generation-aware
- Mini inventory OCR single-active/latest coalescing
- Scanner catalog shared writer serialization
- diagnostics/log bounded retention
- Map donor no broad rewrite without evidence

Performance cannot justify identity threshold relaxation or stale cross-frame reuse.

Wall-clock benchmark를 일반 CI의 고정 합격선으로 사용하지 않는다. pacing/candidate/retry 같은 결정론적 policy contract + runtime latency/응답성 telemetry + 필요 시 Ground Truth replay를 성능 회귀 근거로 사용한다.

## 12. Failure isolation

- Program Update network failure → app continues
- image failure → affected image only
- preference save failure → diagnostic, app continues
- invalid/incomplete content candidate → known-good retained
- unsupported Quest gate → Indeterminate/fail closed
- Scanner low confidence/ambiguity → no Item ID
- Scanner visual/font failure → primary OCR remains
- missing market/icon → affected presentation only
- catalog refresh failure → healthy same-mode cache may remain
- saved Case restore failure → original GT preserved
- corrupt retention metadata → preserve
- updater validation failure → current program untouched

## 13. Verification architecture

Domain/storage/update semantics use automated tests. WPF/Map/Scanner use actual published EXE smoke where applicable.

현재 공개 릴리즈의 정확한 테스트 수와 proof run은 `docs/STATE.md`에만 고정한다. 이 아키텍처 문서에 과거 릴리즈의 테스트 수를 장기 계약처럼 복제하지 않는다.

Release candidate는 최소 다음을 검증한다.

- Release build
- automated tests / no failures/skips
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN identity
- Product UI + Scanner/Mini Scanner smoke
- Main Map/Factory/MiniMap smoke
- graceful shutdown / clean portable root
- stable `Junhyun-Helper.zip` generation
- archive root `준현 헬퍼/` validation
- exact tag/source
- public latest release
- independent public ZIP redownload/hash/layout
- public-downloaded EXE smoke

구체적인 현재 release gate와 검증 run은 `docs/STATE.md` 및 해당 release 문서를 사용한다.

## 14. Change-impact routes

Scanner recognition:

```text
capture/proposal
→ semantic header
→ ROI
→ OCR/substitution/character policy/visual
→ catalog matcher
→ verified state
→ Item ID
→ mapped presentation/search
→ Mini overlay
→ GT/diagnostics/replay/smoke
```

Scanner market:

```text
source market fields
→ ScannerCatalogService
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Mini Scanner / item search
```

Needed count:

```text
Quest/Hideout/Profile
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ NeededItemCalculator RemainingTotal
→ Scanner presentation
```

Settings schema:

```text
ScannerDisplaySettings normalize/migration
→ hotkeys / Mini display/order / substitutions
→ Settings UI
→ Mini rendering
→ smoke/regression
```

Ground Truth UI:

```text
Case evidence
→ image scale transform
→ candidate/manual/none selection
→ original pixel ROI
→ candidate_selection.json / case.json
→ re-edit/replay
```

## 15. Related docs

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
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
