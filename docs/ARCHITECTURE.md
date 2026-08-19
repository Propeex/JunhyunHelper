# ARCHITECTURE — 준현 헬퍼 기술 구조

상태: `ACTIVE / v1.0.0 PUBLIC BASELINE`

기준일: 2026-08-19

이 문서는 준현 헬퍼의 현재 기술 경계를 설명합니다. 제품 의미는 `PRODUCT.md`와 `DECISIONS.md`, 실제 구현 위치·입출력·참조·변경 영향은 `DEVELOPER_REFERENCE.md`가 더 상세합니다.

---

## 1. 전체 구조

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source

JunhyunHelper.Application
  ├─ JunhyunHelper.Core
  └─ JunhyunHelper.Infrastructure.Storage

JunhyunHelper.Infrastructure
  └─ JunhyunHelper.Core

JunhyunHelper.Core
  └─ product external dependency 없음
```

기본 원칙:

- Core는 WPF/HTTP/SQLite를 모릅니다.
- Application은 사용자 유스케이스와 authoritative mutation을 소유합니다.
- Infrastructure는 network/storage/update boundary를 소유합니다.
- Desktop은 WPF presentation, interaction, composition root, Map product bridge를 소유합니다.
- UI가 domain truth를 다시 계산하지 않습니다.

---

## 2. Core

Core가 소유하는 것:

- `GameProfileSnapshot`
- Quest definition/objective/availability/failure/future reachability
- Hideout station/level/item requirements
- Inventory quantity/consumption
- Needed Items / cleanup / flexible requirement 계산
- Ammo canonical model
- trader/map/edition reference model
- `GameContentCatalog`

Core 계산은 deterministic합니다.

대표 원칙:

- unknown Quest fact를 true/Current로 추측하지 않음
- `Indeterminate`와 future `IndeterminatePotential`을 구분
- flexible hand-in의 실제 소비 item을 자동 선택하지 않음
- missing Hideout progress = Lv.0
- cleanup은 미래 필요/보호 규칙을 반영

---

## 3. Application

Application은 사용자 조작을 domain 계산 + persistence mutation으로 묶습니다.

대표 service:

- `ProfileApplicationService`
- `QuestApplicationService`
- `HideoutApplicationService`
- `ItemsApplicationService`
- `FixedInventoryConsumptionPolicy`

주요 계약:

### Quest completion

```text
current profile + content
→ availability 확인
→ fixed single-item requirement만 consume
→ consumption ledger 기록
→ CompletedQuestIds 반영
→ profile save
→ workspace recalculation
```

Flexible hand-in은 어떤 후보를 실제 제출했는지 모르므로 자동 차감하지 않습니다.

### Hideout level

```text
current level
→ target level 범위 검증
→ 상승 구간 fixed requirements consume
→ stationId:targetLevel ledger
→ level 저장
```

명시적 restore 경로만 inventory를 복구합니다.

### Items

Profile/content planning facts가 같고 Inventory 수량만 바뀌면 `FutureNeededItemsBasis`를 재사용합니다. 캐시는 결과의 의미를 바꾸지 않고 동일 입력의 deterministic expensive basis만 재사용합니다.

---

## 4. Infrastructure — User Progress

사용자 상태 저장:

```text
%LocalAppData%/JunhyunHelper/user.db
```

`UserProfileStore`:

- SQLite table schema v1
- profile payload는 JSON document
- optional/default property expansion으로 destructive migration 회피
- read cache 사용
- write 성공 뒤 canonical snapshot cache
- store instance당 schema initialization 1회
- concurrent first schema access는 `SemaphoreSlim` gate

v1.0.0 user data migration은 없습니다.

---

## 5. Infrastructure — Game Content

Game Content update:

```text
online Tarkov source
→ endpoint loading
→ canonical importer/build
→ GameContentValidator
→ candidate content.db
→ SQLite integrity/read-back
→ ContentActivationService
→ active content.db
→ previous known-good 보존
```

주요 경계:

- `TarkovJsonClient`
- `TarkovEndpointSourceLoader`
- `TarkovContentBuildService`
- source-specific importers
- `GameContentValidator`
- `ContentSnapshotStore`
- `ContentActivationService`
- `TarkovContentUpdateService`

현재:

```text
Content schema: v7
Readable: v3-v7
```

candidate 검증 전 active를 덮어쓰지 않습니다. Game Content update는 `user.db`를 수정하지 않습니다.

---

## 6. Infrastructure — preference / image cache

Atomic preference 저장:

```text
primary.json
previous readable .bak
same-directory temp
flush
atomic replace
```

대표:

- Ammo favorites
- Map product settings

Image cache:

- shared HttpClient
- byte/dimension upper bound
- decode 후 normalized image 저장
- concurrent download 제한
- corrupt cache recovery
- 개별 image 실패는 content 전체 실패로 확대하지 않음

---

## 7. Desktop composition root

`DesktopServices`가 non-Map service composition을 담당합니다.

```text
UserProfileStore
ContentActivationService
shared HttpClient
ImageCacheService
AmmoFavoriteStore
source loader/build service
TarkovContentUpdateService
ProfileApplicationService
QuestApplicationService
HideoutApplicationService
ItemsApplicationService
```

shared HTTP User-Agent는 Desktop assembly major/minor에서 파생합니다.

---

## 8. Desktop shell / pages

`App`:

- process startup
- fatal exception hooks
- updater apply mode
- MainWindow creation
- stale updater cleanup

`MainWindow`:

- profile/content/workspace lifecycle
- section navigation
- Application service orchestration
- Map host integration
- product smoke hooks
- shutdown lifecycle

Pages:

- Profile UI → profile facts input
- QuestPage → query/filter/detail/progress action/navigation
- HideoutPage → station/level/material presentation + level action
- ItemsPage → needed/cleanup/inventory/flexible/source navigation
- AmmoPage → caliber/search/details/favorites
- Scanner → visible `준비 중` placeholder only

Domain truth를 page event handler에 복제하지 않습니다.

---

## 9. Map/MiniMap pinned subsystem

Map/MiniMap은 예외적으로 pinned donor source를 compile-link합니다.

```text
Gitlink source pin:
d933792b6042a51cea38dc44b686a096fe30de67

Fetch origin:
https://github.com/SIGDrone/Tarkov-Helper.git
```

fetch origin은 source identity가 아닙니다. product source identity는 gitlink SHA입니다.

Desktop project는 donor의 제한된 Models/Services/Map/MiniMap/dialog source만 포함하고 다음 legacy app subsystem은 배제합니다.

- old updater
- old content DB/update pipeline
- hidden global commands
- old logger
- old non-Map product services

Map은 Hideout/Item/Ammo runtime과 직접 결합하지 않습니다.

Product-owned bridge가 책임지는 것:

- current Quest projection
- persisted product settings
- map/floor selection synchronization
- product hotkeys
- error containment
- floor presentation compatibility
- rendered release smoke hooks

Donor renderer 내부의 안정적인 경로는 구체적 regression/performance 이유 없이 wholesale refactor하지 않습니다.

---

## 10. Map floor architecture

제품 계약:

**floor는 visibility filter가 아니라 relation/presentation입니다.**

- enabled 타층 marker 유지
- current/above/below presentation
- Main Map floor change → zoom + map-space viewport center 보존
- MiniMap floor change → exact live Scale + Translate X/Y 보존

### 10.1 donor legacy conflict

Pinned donor에는 legacy current-floor-only standard-marker filter가 남아 있습니다.

```text
_sharedMarkerFilterTimer
→ 200 ms interval
→ max 12 ticks
→ off-floor Visible marker
→ _sharedFloorHiddenMarkers 기록
→ Visibility=Collapsed
```

v1.0.0 exact-release smoke에서 이 filter가 product floor presentation보다 늦게 실행되는 race를 검출했습니다.

### 10.2 product compatibility

관련 first-party source:

- `Map/MapPage.JunhyunCrossFloorMarkerPolicy.cs`
- `Map/LegacyStandardMarkerFloorPresentationBridge.cs`
- `Map/JunhyunFloorPresentation.cs`

처리:

```text
donor filter tick
→ donor가 floor 때문에 직접 숨긴 element set
→ product post-filter correction
→ 정확히 그 set만 Visible 복구
→ Junhyun floor presentation 재적용
```

category/faction/user visibility는 donor가 소유합니다. first-party가 전체 marker tree를 다시 추론하지 않습니다.

새 permanent polling을 추가하지 않고 donor의 bounded timer에 callback만 결합합니다. donor timer가 page Unloaded/Loaded lifecycle에서 재생성되면 callback을 다시 연결합니다.

상세는 `MAP_RUNTIME_COMPATIBILITY.md`가 권위 문서입니다.

---

## 11. Quest → Map bridge

Quest availability/progress의 권위는 JunhyunHelper Core/Application입니다.

Map donor가 Quest 진행 상태를 자체 데이터로 다시 계산하지 않습니다.

```text
JunhyunHelper Quest workspace
→ current Quest projection
→ Quest geometry/reference
→ product Map bridge
→ donor renderer marker/sidebar
```

일반 marker/artwork/config은 pinned Map bundle에서 오고, current Quest state는 JunhyunHelper content/profile에서 옵니다.

---

## 12. Program Update architecture

Program Update는 Game Content update와 독립된 subsystem입니다.

### 12.1 Check

```text
MainWindow visible
→ ProgramUpdateCoordinator.CheckAtStartupAsync
→ GitHubProgramUpdateClient.GetLatestReleaseAsync
→ latest public stable parse
→ latest > current ? consent UI : no-op
```

조회 실패는 startup fatal이 아닙니다.

### 12.2 Download / verify

```text
user consent
→ LocalAppData updates/pending
→ SHA256SUMS
→ exact win-x64 ZIP
→ SHA-256
→ ZIP/package security validation
→ staging
```

Validation:

- strict stable semantic version
- exact asset names
- HTTPS trusted GitHub Release scope
- checksum entry
- path traversal reject
- symlink reject
- duplicate reject
- unexpected root reject
- PDB reject
- required files non-empty

검증 전 current product files를 수정하지 않습니다.

### 12.3 Apply

```text
current single-file EXE
→ TEMP self-copy updater mode
→ parent exit wait
→ new files prepare
→ existing owned files previous name으로 move
→ new files commit
→ success: cleanup + restart
→ failure: rollback + old app restart attempt
```

Program-owned boundary:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

LocalAppData user/content/cache/preferences/logs는 transaction 밖입니다.

---

## 13. Runtime failure containment

Nonfatal presentation/support examples:

- Map/Ammo preference save failure
- product hotkey async failure
- floor async failure
- keyboard hook install failure
- image download/decode failure
- program update check/download/validation failure

Fatal examples:

- 필수 startup construction failure
- canonical active content를 사용할 수 없는 structural corruption

가능한 diagnostic은 `%LocalAppData%/JunhyunHelper/logs/startup.log` 또는 subsystem diagnostic에 남깁니다.

---

## 14. Release architecture

상시 CI `.github/workflows/ci.yml`:

1. clean checkout + pinned donor
2. Release build
3. 전체 tests
4. win-x64 self-contained single-file publish
5. Version/ProductVersion/FIRST_RUN identity
6. root/dependency/PDB/nested-archive hygiene
7. actual published EXE launch
8. rendered Product UI assertions
9. Main Map / Factory / MiniMap smoke
10. donor late floor-filter window 이후 final marker state 검증
11. graceful close/process exit
12. artifact upload

정식 release는 Draft-first입니다.

```text
exact source baseline
→ build/test/publish/smoke
→ package + SHA256SUMS
→ Draft Release
→ Draft asset re-download verification
→ public/latest
→ public asset re-download verification
→ public-downloaded executable smoke
```

Release/verification workflow는 one-shot이며 완료 후 저장소에서 제거합니다.

---

## 15. Scanner boundary

Scanner는 현재 실제 subsystem이 아닙니다.

```text
Desktop tab: visible
Content: 준비 중
Runtime scanner implementation: none
```

별도 사용자 요구사항 확정 전 Core/Infrastructure/Application에 Scanner architecture를 추가하지 않습니다.

---

## 16. 현재 공개 baseline

```text
Release: v1.0.0
Exact release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
Automated tests: 232 passed
Public ZIP bytes: 74,088,334
Public ZIP SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
Content schema: v7
user.db schema: v1
Public-downloaded Product UI/Main Map/Factory/MiniMap/graceful shutdown smoke: PASS
Existing v0.x GitHub Releases remaining: 0
```

상세 릴리즈 기록은 `RELEASE_1.0.0.md`, 전체 감사는 `FINAL_AUDIT_1.0.0.md`, Map runtime compatibility는 `MAP_RUNTIME_COMPATIBILITY.md`를 기준으로 합니다.
