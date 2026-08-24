# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록한다.

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED**

현재 공개 기준선:

```text
v1.5.0
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
296 tests / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
public verifier: 32691641614 — SUCCESS
```

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — 외부 image decode 및 Scanner local-font rendering
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

Canonical domain과 deterministic 계산을 소유한다.

대표 책임:

- Quest availability / future reachability
- Needed Items / cleanup safety
- Item/Ammo canonical 의미
- Scanner pure contracts/policies/signatures

### Application

사용자 use case와 authoritative mutation을 소유한다.

Profile/Quest/Hideout/Items 변경 후 저장과 workspace 재계산을 orchestration한다.

### Infrastructure

I/O 경계를 소유한다.

- HTTP/source parsing
- Game Content build/validation/activation
- SQLite/file persistence
- Scanner full-item/market catalog
- Program Update client/applier

### Desktop

WPF/presentation/system-integration 경계를 소유한다.

- shell/pages/dialogs
- presentation/image cache
- Scanner capture/OCR/runtime/diagnostics
- Map product bridge
- startup/update UX

Domain truth를 WPF event handler에 복제하지 않는다.

### Map/MiniMap donor

`vendor/Tarkov-Helper` 전체를 제품 사양으로 승계하지 않는다.

Map/MiniMap만 pinned source를 compile-link하는 제한적 예외다.

## 3. 데이터 권위와 lifecycle

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `content/<mode>/content.db` |
| User Progress | user-confirmed profile facts | `user.db` |
| Inventory | user quantity + explicit fixed consumption ledger | `user.db` |
| Presentation preferences | user settings | atomic JSON + `.bak` |
| Image cache | canonical URL에서 검증/normalize한 presentation bytes | `image-cache/` |
| Scanner identity/market catalog | current full-item source + official Korean identity | `scanner/catalog/` + memory |
| Scanner settings | user-owned display/hotkey/OCR substitutions | `scanner-settings.json(.bak)` |
| Scanner title font cache | installed Tarkov `resources.assets` read-only extraction | `scanner/fonts/` + generation manifest |
| Scanner automatic/reviewed Cases | runtime evidence + user Ground Truth | `scanner/diagnostics/` |
| Scanner/startup logs | runtime diagnostics | `logs/*.log(.1)` |
| Map artwork/config/general markers | pinned Map bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable folder |

Game Content update, Program Update, User Progress, Scanner catalog, Scanner settings, Scanner font cache, Scanner diagnostics는 서로 다른 lifecycle이다.

## 4. Startup / composition / shutdown

```text
App.OnStartup
→ fatal exception hooks
→ updater apply-mode 처리
→ LocalAppData diagnostics/log preparation
→ Scanner diagnostic retention service
→ MainWindow 표시
→ DesktopServices composition
→ profile load
→ selected GameMode content read/recovery/update
→ Quest/Hideout/Items workspace
→ Ammo/Map/Scanner context bridge
→ smoke가 아니면 Program Update check
```

`DesktopServices`가 non-Map first-party composition root다.

MainWindow는 orchestration layer이며 domain rule의 소유자가 아니다.

Shutdown에서는 Scanner runtime, OCR serialization, font-aware resource leases, retention timer 등 owned resource가 정상 종료돼야 한다. Published EXE graceful-shutdown smoke가 이 경계를 검증한다.

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

- candidate 실패가 active를 덮지 않음
- `user.db`를 건드리지 않음
- new active read failure는 previous known-good recovery 가능

Current Content schema: v7. Readable: v3~v7.

### v1.5.0 unified data-update orchestration

사용자 top-level data update는 일반 Game Content 성공 후 current GameMode Scanner full-item/market catalog refresh까지 이어진다.

```text
general content fetch/build/activate
→ Scanner catalog/market refresh
→ combined status
```

Scanner refresh만 실패하면 general content를 rollback하지 않는다. Healthy same-mode Scanner cache가 있으면 유지한다.

## 6. User Progress / 계산

`GameProfileSnapshot`이 사용자의 authoritative 진행 aggregate다.

Quest:

- 서로 다른 prerequisite requirement = AND
- 한 requirement status set = OR
- unsupported/unknown fact는 optimistic unlock하지 않음
- `Indeterminate`를 Current로 승격하지 않음
- exact ProfileVariable fact가 있으면 권위값
- task-pool synthetic compatibility는 audited exact shape에서만 허용

Needed Items:

```text
future Quest reachability
+ future Hideout levels
→ fixed/flexible requirements
→ Needed Items / Cleanup protection
```

Scanner의 `필요 개수`는 이 파이프라인을 다시 구현하지 않고 `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal`을 읽는다.

## 7. Map / MiniMap 경계

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- general marker/artwork/config → pinned Map bundle
- current Quest state/geometry → JunhyunHelper bridge
- donor updater/content ownership/hidden global command는 제품에 승계하지 않음
- 구체적 defect/performance evidence 없이 broad refactor하지 않음

Map은 독립 subsystem이고 Quest만 JunhyunHelper current data와 bridge한다.

## 8. Scanner subsystem

Scanner는 화면 픽셀을 current official Korean item catalog의 Item ID에 연결하는 독립 subsystem이다.

권위 전문 문서: `docs/SCANNER.md`.

### 8.1 구성

논리 구조:

```text
ScannerPage / MiniScannerWindow
        │
        ▼
ScannerCoordinator
├─ Scanner settings / hotkeys / OCR substitutions
├─ ScannerCatalogService
├─ ScannerRuntimeService
│  ├─ capture + structural proposal detector
│  ├─ semantic inspect-header/title refinement
│  ├─ SerializedScannerOcrEngine
│  │  └─ Windows ko-KR OCR
│  ├─ FontAwareScannerOcrEngine
│  │  ├─ local Tarkov font provider
│  │  └─ current-catalog visual recovery
│  ├─ ScannerLatencyTelemetry
│  ├─ verified title-continuity stabilization
│  └─ ScannerItemPresentationService
├─ ScannerRecognitionDebugStore / Ground Truth correction
└─ Mini Scanner overlay/context
```

Title OCR과 inventory-context OCR은 하나의 serialization boundary를 공유한다. Inventory-context OCR에 item-name visual recovery 의미를 섞지 않는다.

### 8.2 Recognition data flow

```text
Tarkov client/display pixels
→ capture
→ Scanner Lab v3.8-style structural proposals
→ red close-X + neutral top frame
→ bounded frame-left magnifier lane
→ magnifier morphology + dark title field/text evidence
→ HEADER_FRAME_LOCKED
→ locked item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog character/symbol policy
→ official-name semantic resolver
→ optional strict current-catalog visual corroboration/recovery
→ conservative confidence + top1/top2 margin
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

Structural score, icon evidence, OCR string, visual similarity 중 하나만으로 Item identity를 확정하지 않는다.

### 8.3 Structural proposal

- structural floor `0.34`
- RED-X connected-component + rectangle/edge fallback은 candidate proposal 역할
- historical aspect prior는 약한 ranking hint
- high IoU만으로 실제 edge가 다른 후보를 제거하지 않음
- almost-identical edge jitter만 dedupe
- continuous cap 8
- one-shot cap 12

Initial structural rectangle은 final authoritative bounds가 아니다.

### 8.4 Inspect-header semantic ownership

Required semantic evidence:

- red close-X color/body/edge + diagonal-X shape
- long neutral inspect-header/frame
- fixed/bounded frame-left search-icon lane
- magnifier ring/hollow/handle morphology
- dark title field
- text presence

Runtime gate:

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND valid magnifier
AND valid close-X
```

First title glyph connected component가 title ROI horizontal ownership을 결정하지 않는다.

Incomplete header lock은 production OCR identity path에 진입하지 않는다.

### 8.5 OCR / semantic identity

- Windows `ko-KR` OCR primary
- raw OCR forensic preservation
- normal pass + 필요 시 deep/high-contrast/binary/inverse pass
- current official Korean catalog에서 allowed character/symbol policy 파생
- catalog-impossible glyph는 특정 문자로 자동 확정하지 않음
- exact-first + conservative fuzzy + top1/top2 margin
- bounded unique unknown/edit recovery
- historical alias 무제한 누적 금지
- visual correction은 strict current-catalog evidence에서만 허용
- unavailable/error/ambiguous visual path가 healthy OCR evidence를 임의 폐기하지 않음

### 8.6 User OCR substitutions — settings schema v5

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matcher
```

- default empty
- exact user-owned rules
- add/delete/enable-disable/reset
- raw/substituted/normalized/matched evidence 분리
- recursive/cyclic reprocessing 없음
- product-wide automatic substitution table 아님

### 8.7 Tarkov title-font ownership / generation

게임 font binary는 public package에 번들하지 않는다.

```text
EscapeFromTarkov_Data/resources.assets (read-only)
→ bounded SFNT discovery/extraction
→ local scanner/fonts cache
→ source manifest + actual font SHA generation
→ generation-aware rendered official-item templates/features
```

- source generation 변경 시 stale loaded/rendered cache invalidate
- visual caches bounded
- corrupt/unavailable font recovery는 primary OCR path를 fatal로 만들지 않음

### 8.8 OCR serialization / same-cycle exact reuse

`SerializedScannerOcrEngine`은 WinRT OCR 공유 serialization boundary다.

v1.5.0 exact reuse 조건:

```text
same active scan cycle
AND same normal/deep class
AND same width/height/BPP
AND exact pixel SHA-256 match
```

Cycle이 바뀌면 cache를 폐기한다. Cross-frame OCR cache는 없다.

### 8.9 Runtime verified-state stabilization

Continuous path에서 already-verified detail의 title continuity를 raw BGRA exact hash만으로 판단하지 않는다.

Title-ink shape signature는:

- dark background variation에 둔감
- unused trailing ROI width 무시 가능
- visible glyph shape 변화에는 민감
- visible title ink 없음은 fail closed

하지만 **Item identity proof가 아니다**. Semantic gate를 통과한 trusted result continuity에만 사용한다.

Different geometry/title identity evidence가 나타나면 stale verified result를 clear한다.

### 8.10 Stage latency telemetry

계측 stage:

- capture
- rectangle-proposal
- semantic-header
- ocr-normal
- ocr-deep
- visual-recovery
- catalog-matching
- presentation
- end-to-end

성능 최적화는 이 telemetry를 근거로 duplicate work/OCR/copy/recovery 비용을 줄인다. Threshold 완화를 성능 개선으로 사용하지 않는다.

### 8.11 Scanner catalog operation ordering

Catalog shared-state writers:

```text
network RefreshAsync(mode)
local LoadCacheAsync(mode)
```

둘은 동일 operation gate에서 ordering을 보장한다.

금지 상태:

```text
old-mode refresh starts
→ new-mode cache load applies
→ old-mode refresh finishes
→ old mode becomes final state
```

Mode transition regression을 유지한다.

### 8.12 Item data bridge

```text
Item ID
→ Scanner catalog: official name / market / dimensions / icon URL
→ GameContentCatalog: canonical item
→ ItemsWorkspace: RequiredTotal
→ ScannerItemSnapshot
→ Scanner Page / Mini Scanner
```

가격/수량 계약:

- 최고 상점가 = flea 제외 valid RUB-equivalent sell price max
- 최고가 상인명 = 해당 source가 신뢰 가능할 때
- flea 평균가 = positive `avg24hPrice`
- slots = positive `width × height`
- trader/flea price-per-slot = price와 slots 모두 valid일 때
- 필요한 개수 = `RequiredTotal`
- Inventory 차감 shortage가 아님

Market/dimension missing은 해당 presentation field만 omit하고 healthy Item identity를 버리지 않는다.

### 8.13 Ground Truth / correction architecture

Diagnostics root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

Correction 기본 경로:

```text
detail candidate
→ close-X candidate
→ magnifier candidate
→ item-name ROI candidate
→ correct item/text
→ reviewed Ground Truth
```

Fallback:

- correct candidate가 없으면 manual rectangle
- detector가 semantic object를 생성하지 못했으면 `없음`

Candidate ID/rank/score/geometry를 user truth와 함께 저장한다.

자동 diagnostic Case는 Ground Truth가 아니다.

### 8.14 Regression architecture

```text
reviewed full.png
→ current proposal detector
→ current semantic header lock
→ current item-name ROI
→ current OCR/deep/user substitution/visual recovery
→ current official catalog match
→ final Item ID
```

Result classification:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

기존 정상 reviewed Case가 실패하면 평균 성능과 무관하게 regression이다.

### 8.15 Diagnostics / retention

Automatic diagnostic Case auto-delete eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Bounds:

- max age 30 days
- max automatic cases 300
- max automatic bytes 512 MiB
- recent safety window 2 hours

Reviewed Ground Truth는 자동 삭제 금지.

Corrupt/unknown metadata는 fail closed하여 보존한다.

Logs:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
%LocalAppData%/JunhyunHelper/logs/startup.log(.1)
```

bounded rotation을 사용한다.

### 8.16 Scanner UI architecture

Normal surface:

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- recent recognition history

Settings:

- hotkeys
- OCR substitutions
- Mini Scanner display options

Advanced/diagnostic:

- Display Test
- recognition image
- regression
- Ground Truth export/manage
- catalog force refresh/recovery
- log clear
- diagnostic storage status

Mini Scanner right-click `현재 결과 교정`은 latest debug snapshot을 correction flow에 연결한다.

## 9. Program Update

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

Program Update는 `%LocalAppData%/JunhyunHelper` 사용자 데이터를 교체하지 않는다.

정식 release는 exact source tag, draft redownload, public stable/latest, independent anonymous public redownload + EXE smoke를 모두 통과한다.

## 10. Persistence / atomicity

중요한 JSON preference는 same-directory temp + flush + atomic replacement + `.bak` recovery를 사용한다.

대표 경로:

```text
user.db
content/<mode>/content.db
image-cache/
ammo-favorites.json(.bak)
map-product-settings.json(.bak)
scanner-settings.json(.bak)
scanner/catalog/
scanner/fonts/font-cache.json
scanner/diagnostics/
logs/scanner.log(.1)
logs/startup.log(.1)
```

Runtime mutable state/log를 portable release root에 만들지 않는다.

## 11. 성능 원칙

- immutable/canonical 결과 재사용
- UserProfileStore in-memory snapshot cache
- Items inventory-only mutation에서 future planning basis 재사용
- image download concurrency 제한
- Scanner verified detail의 불필요 OCR 반복 억제
- Scanner same-cycle exact bitmap OCR reuse
- Scanner icon process-memory decode cache
- Scanner font/visual template generation-aware bounded cache
- Mini Scanner inventory-context OCR queue 누적 금지
- Scanner `PrintWindow` validation duplicate full-frame allocation 금지
- Scanner catalog shared writer serialization
- Scanner automatic diagnostics bounded retention
- Scanner logs bounded rotation
- Map donor는 evidence 없이 broad rewrite하지 않음

Cache는 제품 의미를 바꾸면 안 되며 동일 입력/evidence의 deterministic 결과 재사용이어야 한다.

## 12. 오류 격리

- Program Update network failure → app 계속
- image failure → 해당 image만 누락
- preference save failure → diagnostic, app 계속
- invalid content candidate → known-good active 유지
- unsupported Quest gate → fail closed / Indeterminate
- Scanner low confidence/ambiguity → no Item ID
- Scanner font extract/render/cache failure → primary OCR path 유지, visual recovery 생략
- Scanner inventory-context uncertainty → overlay hidden
- Scanner missing market/icon → 해당 presentation field만 omit
- Scanner catalog refresh failure → healthy same-mode cache 보존 가능
- Scanner diagnostic/log deletion failure → Scanner 계속
- corrupt retention metadata → preserve
- updater validation/replacement failure → current program 보존/rollback 시도

## 13. 검증 구조

Core/Application/Infrastructure 의미는 automated tests로 검증한다. WPF/Map/Scanner UI는 실제 published EXE smoke도 사용한다.

v1.5.0 final gate:

```text
final PR #172 CI: 32688080850 — SUCCESS
296 tests / 0 failed / 0 skipped
Windows Release build: PASS
win-x64 publish/package audit: PASS
Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: PASS
graceful shutdown / clean portable root: PASS
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
public redownload / SHA256 / layout / ProductVersion / FIRST_RUN / EXE smoke: PASS
```

실제 Tarkov live E2E는 계속 Ground Truth를 축적하는 운영 범위지만, live evidence 없이 recognition threshold를 완화하지 않는다.

## 14. 변경 영향 추적

### Scanner recognition 변경

```text
capture/proposal
→ semantic header
→ title ROI
→ OCR / user substitution / character policy / visual recovery
→ Scanner catalog matcher
→ runtime verified-state stability
→ Item ID
→ mapped presentation
→ Mini Scanner context/overlay
→ Ground Truth / diagnostics / regression / smoke
```

### Scanner 가격 변경

```text
source item market fields
→ ScannerCatalogService parse
→ ScannerCatalogItem
→ ScannerItemPresentationService
→ Scanner Page / Mini Scanner
→ market shape tests
```

### Needed Items 의미 변경

```text
Quest/Hideout/Profile facts
→ FutureNeededItemsPlanner
→ ItemsWorkspace.Plan.NeededItems
→ RequiredTotal
→ ScannerItemPresentationService
```

Scanner가 이 계산을 독자적으로 복제하지 않는다.

### OCR substitution 변경

```text
ScannerOcrSubstitution core rule
→ ScannerDisplaySettings schema/normalize
→ settings UI
→ OCR handoff
→ diagnostics raw/substituted separation
→ matcher
→ tests/regression
```

### Retention 변경

```text
Case metadata policy
→ ScannerDiagnosticRetentionService
→ automatic unreviewed selection
→ recent safety window
→ delete/re-read race protection
→ reviewed GT preservation
```

Reviewed Ground Truth가 삭제 가능한 경로로 들어가지 않는지 가장 먼저 검증한다.

## 15. 관련 문서

- `docs/STATE.md`
- `docs/CURRENT_STATE.md`
- `docs/PRODUCT.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/SCANNER.md`
- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/SCANNER_TEST_PLAN.md`
- `docs/SCANNER_LAB_3_8_REFERENCE.md`
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`
- `docs/RELEASE_1.5.0.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
