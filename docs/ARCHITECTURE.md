# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록한다. 세부 subsystem 계약은 specialist 문서로 분리하고, 이 문서는 전체 책임/데이터 흐름/lifecycle의 canonical architecture index 역할을 한다.

기준일: **2026-09-04 KST**  
상태: **EVERGREEN CURRENT ARCHITECTURE / PRODUCT COMPLETE / MAINTENANCE MODE**

정확한 현재 release SHA·CI·asset·schema 사실값은 `docs/PROJECT_STATE.json`과 `docs/STATE.md`를 사용한다. 제품 의미는 `docs/PRODUCT.md`와 최신 `docs/DECISION_*`이 우선한다.

## 1. 기술 스택

- .NET 10 / C#
- WPF Desktop (`net10.0-windows10.0.19041.0`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — image decode / Scanner local rendering
- SharpVectors — SVG Map rendering
- Windows x64 portable / self-contained single-file
- 별도 backend 없음
- runtime GPT/AI 없음

## 2. Project boundary

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Map/MiniMap donor compile-link boundary

JunhyunHelper.Application
  ├─ Core
  └─ Infrastructure storage/use-case boundary

JunhyunHelper.Infrastructure
  └─ Core

JunhyunHelper.Core
  └─ WPF / HTTP / SQLite 의존 없음
```

### Core

Canonical domain과 deterministic 계산을 소유한다.

- Quest prerequisite/availability semantics
- Needed Items / cleanup safety
- Item / Ammo canonical meaning
- Scanner pure matching/policy/signature 계약

### Application

사용자 mutation/use case와 workspace orchestration을 소유한다.

- profile/progress mutation
- inventory/consumption orchestration
- cross-feature workspace coordination
- presentation이 사용할 derived plan/state 구성

### Infrastructure

I/O 경계를 소유한다.

- remote Tarkov source parsing/import
- Game Content build/validation/activation
- SQLite/file persistence
- Scanner catalog/cache
- Program Update client/applier

### Desktop

WPF와 OS integration을 소유한다.

- MainWindow shell / first-class sections
- page/view rendering and interaction
- shared in-app overlay host
- image-cache presentation
- Scanner capture/OCR/runtime/diagnostics
- Map first-party bridge/customization
- startup/update/shutdown UX

Domain truth를 WPF event handler에 복제하지 않는다.

## 3. Data authority / lifecycle

| 데이터 | 권위 | 저장/소비 |
|---|---|---|
| Game Content | validated online source → canonical snapshot | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` |
| User Progress | user-confirmed facts | `%LocalAppData%/JunhyunHelper/user.db` |
| Inventory | user quantity + consumption ledger | `user.db` |
| Presentation settings | user preferences | subsystem JSON / SQLite as defined |
| Image cache | validated/normalized bytes | `%LocalAppData%/JunhyunHelper/image-cache/` |
| Scanner identity/market catalog | current full-item source + official identity | `scanner/catalog/` + memory |
| Scanner settings | display/hotkey/order/substitution | scanner settings store |
| Scanner evidence / Ground Truth | runtime evidence + explicit user review | `scanner/diagnostics/` |
| Runtime logs | diagnostics only | `%LocalAppData%/JunhyunHelper/logs/` |
| Map artwork/config/general markers | pinned donor bundle | release `Assets/` |
| Program files | exact GitHub stable Release | portable product folder |

각 lifecycle은 분리한다. Program Update와 Game Content Update가 `user.db`, reviewed Ground Truth 또는 mutable preferences를 덮어쓰지 않는다.

## 4. MainWindow / section lifecycle

MainWindow는 제품 shell과 first-class section lifecycle을 소유한다.

대표 section:

- Quest
- Hideout
- Items
- Ammo
- Map
- Scanner

Section integration은 다음 공통 상태를 명시적으로 처리한다.

- profile availability
- visibility/navigation
- busy state
- button/selection state
- startup activation
- shutdown/disposal

새 section이 unrelated page의 `Loaded` 순서를 implicit initialization trigger로 사용하지 않는다.

## 5. Shared in-app overlay

Profile Edit, Scanner settings/advanced, Map/MiniMap settings 등 user-facing editor/settings surface는 MainWindow shared overlay interaction을 사용할 수 있다.

```text
launcher
→ MainWindow overlay owner
→ child editor/settings surface
→ same launcher / backdrop / common X → dismiss
```

Overlay owner는 표시/닫기 lifetime을 관리한다. Child의 validation/save/cancel/domain semantics를 재구현하지 않는다.

Existing visual-tree UIElement를 overlay로 옮기는 경우 caller가 원래 parent/index 복원 책임을 가진다.

## 6. Startup / shutdown

대표 startup:

```text
App.OnStartup
→ fatal exception hooks
→ updater apply mode
→ LocalAppData diagnostics/log setup
→ retention/background setup
→ MainWindow composition
→ optional startup Program Update check
→ profile/content/workspaces
→ Map / Scanner section context
```

Shutdown은 Scanner OCR/runtime, font/cache/background work와 기타 owned async resource를 정상 종료해야 한다.

Release verification은 단순 process kill이 아니라:

- actual Main Window close
- bounded process termination
- active async work 중 close race

를 검증한다.

## 7. Game Content update architecture

```text
remote source
→ parse/import
→ semantic/schema validation
→ canonical candidate
→ completeness / Last Known Good guard
→ candidate content.db
→ SQLite read-back/integrity
→ atomic active replacement
→ image prefetch
```

계약:

- candidate 완성 전 active overwrite 금지
- failed candidate 폐기
- healthy active/LKG 유지
- suspicious shrink를 baseline-relative guard로 차단
- importer가 이해하지 못하는 schema drift는 fail closed
- optional enrichment는 필요한 범위에서 fail-soft
- User Progress / Scanner user state에 영향 없음

Top-level Game Data Update는 general content activation 뒤 current GameMode Scanner catalog/market refresh까지 orchestration한다. Scanner-only partial failure가 general content success를 rollback하지 않는다.

외부 최신 source 계약 검증은 hermetic PR/main CI와 분리한다.


## 8. Content schema / Item structure

Canonical Item keeps only fields that remain current product data authority, including identity/category/type, ordinary dimensions/weight/base price/flea-tradability and other independently used content.

Farming Guide-only storage/equipment/attachment/armor/layout extension metadata was removed in v1.17.1 because no remaining product feature consumes it.

Current content contract:

```text
Content write schema: v12
Readable schemas: v3~v12
```

Older v12 snapshots may contain historical JSON properties that current models ignore. The application does not reinterpret those removed properties as another feature's authority.

## 9. Profile / Quest / Needed Items flow

```text
Profile facts
→ Quest availability/current/future reachability
→ Hideout future requirements
→ NeededItemRequirementBuilder
→ NeededItemCalculator
→ ItemsWorkspace.Plan.NeededItems
→ Items / Scanner / cleanup presentation
```

- exact user fact와 derived result를 분리한다.
- unknown prerequisite를 optimistic current로 바꾸지 않는다.
- flexible hand-in 실제 item을 자동 추측하지 않는다.
- current Quest UI compatibility와 future cleanup safety를 분리한다.

Scanner needed presentation authority:

```text
NeededItems[itemId].RemainingTotal
NeededItems[itemId].Sources
```

Scanner가 별도 requirement truth를 재구축하지 않는다.

## 10. Items / Ammo

Items는 current canonical content + profile/inventory + Needed Items plan을 presentation한다.

Ammo는 read-only comparison과 persisted favorites를 제공한다. Pickup 판단은 same-caliber penetration과 current profile에서 증명된 direct purchase state를 사용한다.

UI filtering/search/navigation은 domain authority를 재구현하지 않는다. 공통 search-clear 등은 presentation behavior로만 구현한다.

## 11. Map / MiniMap architecture

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

`vendor/Tarkov-Helper` 전체를 제품 사양으로 승계하지 않는다. Map/MiniMap만 pinned source compile-link 예외이며 JunhyunHelper first-party bridge가 제품 lifecycle/selection/presentation 의미를 소유한다.

핵심:

- current Quest와 Map navigation bridge
- Main Map ↔ fresh/reused MiniMap selection synchronization
- player position/heading transform consistency
- PMC / Scav / Transit marker/filter semantics
- bounded marker-layer recovery
- settings/presentation isolation

검증된 donor source는 concrete defect/performance evidence 없이 broad-edit하지 않는다.

## 12. Scanner architecture

Scanner는 screen pixel evidence를 current catalog Item ID로 연결한다.

```text
Tarkov client/display pixels
→ capture
→ structural proposals / inspect-header validation
→ item-name ROI
→ serialized ko-KR OCR
→ bounded substitution/normalization when needed
→ current-catalog sanitation/matching
→ optional strict current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
```

Identity와 presentation metadata를 분리한다.

- geometry/structure는 proposal evidence
- price/needed/source는 Item ID 확정 후 presentation
- scan-time network identity proof 금지
- cross-frame stale identity를 proof로 재사용하지 않음
- reviewed actual Tarkov evidence 없이 recognition acceptance 완화 금지

Scanner safety:

- external screen pixels + OCR only
- game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass 금지

Canonical specialist doc: `docs/SCANNER.md`.

## 13. Scanner Ground Truth / diagnostics

Ground Truth는 explicit user-reviewed save만 authoritative하다.

```text
runtime capture/evidence
→ Saved Case
→ user review/correction
→ Ground Truth
→ regression dataset/test
```

Display scaling은 original-pixel dataset coordinate authority를 변경하지 않는다. Corrupt/unknown review state는 자동 삭제보다 preserve fail-closed를 우선한다.

김태영 PC 진단은 별도 opt-in support exporter다. 자동 upload/email send를 하지 않으며 allowlist evidence만 수집한다.


## 14. Retired Farming Guide boundary

Farming Guide is not a current subsystem as of v1.17.1.

There is no current Farming Guide UI, service, persistence, Scanner bridge, planner, optimizer or domain model.

Historical decision/release documents may describe the removed subsystem for reproducibility, but they are not architecture authority for current code.

## 15. Program Update / release architecture

```text
latest public stable check
→ user consent
→ exact release asset + checksum
→ archive/root validation
→ staging
→ updater transaction
→ restart
```

LocalAppData user data는 untouched다.

Release:

```text
exact main source
→ CI build/test/publish/runtime smoke/package
→ immutable Actions artifact
→ Release workflow artifact download + identity/hash recheck
→ stable tag/release/assets
→ public read-back verification
```

Release workflow가 public용 제품을 별도로 다시 빌드해 exact-main artifact와 다른 bytes를 만들지 않는다.

Public stable tag/source/assets는 immutable historical identity다. 후속 documentation-only main commit은 기존 version의 product source를 재정의하지 않는다.

## 16. Persistence compatibility

현재 주요 schema:

```text
user.db: v1
Content write: v12 / readable v3~v12
Scanner display settings: v10
Scanner catalog write: v4 / readable v1~v4
```

Schema 변경 시:

- write/read compatibility를 명시한다.
- user-owned state의 mandatory migration 여부를 기록한다.
- unknown future schema를 낙관적으로 해석하지 않는다.
- corrupt state recovery는 가능한 경우 backup/fail-closed를 사용한다.

## 17. Verification architecture

Deterministic unit/integration test만으로 user-visible WPF 완료를 선언하지 않는다.

변경 성격에 따라 다음 gate를 사용한다.

- deterministic tests
- Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE startup
- relevant Product UI activation/render smoke
- Map/Scanner 비회귀 smoke
- normal shutdown
- active async Shutdown Race
- clean portable root
- package/checksum audit
- CI / Documentation Consistency
- exact-main artifact identity
- public tag/release/assets verification

실사용에서 보고된 실제 증상은 automated test보다 높은 우선순위의 regression evidence로 취급한다.

## 18. 변경 원칙

- current user-confirmed product intent가 최우선이다.
- 제품 요구사항과 구현을 구분한다.
- 한 subsystem 수정이 unrelated subsystem의 truth/lifecycle을 암묵적으로 바꾸지 않게 한다.
- concrete defect/evidence 없이 broad refactor하지 않는다.
- Tarkov drift는 source semantics → importer → canonical model → persistence → presentation 순서로 추적한다.
- user-visible change는 runtime evidence까지 검증한다.
- 새 product semantics는 결정 문서 없이 existing code behavior에서 추론해 확정하지 않는다.

상세 유지보수 규칙은 `docs/MAINTENANCE_CONTRACTS.md`, 현재 구현 상태는 `docs/STATE.md`, 제품 요구사항은 `docs/PRODUCT.md`를 따른다.
