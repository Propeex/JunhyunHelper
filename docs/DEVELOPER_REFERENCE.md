# DEVELOPER_REFERENCE — 준현 헬퍼 개발자용 시스템 설명서

상태: `ACTIVE / v1.0.0 BASELINE`

기준일: 2026-08-19

이 문서는 준현 헬퍼를 다음 세션의 개발자가 저장소만 보고 이어서 개발할 수 있도록 만든 **구현 지도와 변경 영향 설명서**입니다. 제품 의미의 최종 권위는 `PRODUCT.md`와 `DECISIONS.md`, 현재 배포 상태의 최종 권위는 `STATE.md`입니다. 이 문서는 그 요구사항이 실제 코드에서 **어디에 있고, 무엇을 참조하며, 어떤 입력을 받아 어떤 출력을 만들고, 변경하면 어디에 영향이 가는지**를 설명합니다.

---

# 1. 먼저 기억할 절대 경계

## 1.1 제품의 현재 형태

준현 헬퍼는 Windows x64용 .NET 10 WPF desktop application입니다.

현재 사용자가 실제로 사용하는 상위 영역은 다음과 같습니다.

- Profile
- Quest
- Hideout
- Items / Inventory / Needed Items / Cleanup
- Ammo
- Map / MiniMap
- Game Content update
- Program update
- Scanner tab의 `준비 중` placeholder

Scanner는 **실제 기능이 아닙니다**. 탭은 제품 UI에 남겨 두되 별도 요구가 확정되기 전까지 scanner logic, capture, OCR, automation을 추가하지 않습니다.

## 1.2 runtime AI 없음

준현 헬퍼 runtime에는 GPT/LLM/AI API가 없습니다. 데이터 판정은 deterministic code와 canonical data로 수행합니다.

## 1.3 old Tarkov-Helper는 제품 사양이 아님

`vendor/Tarkov-Helper`는 일반적으로 참고 자료일 뿐입니다. 단, Map/MiniMap은 사용자 검증을 거친 특정 donor revision을 명시적으로 기준선으로 채택한 예외입니다.

따라서:

- Quest, Hideout, Items, Ammo, updater, profile logic을 old Tarkov-Helper에서 되살리지 않습니다.
- Map donor code도 “깔끔하게 만들기 위해서” 임의 재작성하지 않습니다.
- Map에서 실제 결함, 성능 문제, 제품 계약 위반이 확인됐을 때만 필요한 범위를 수정합니다.

---

# 2. 저장소를 읽는 순서

새 작업을 시작하면 다음 순서가 가장 빠릅니다.

1. `AGENTS.md` — 작업 규약
2. `docs/STATE.md` — 현재 구현/배포 상태
3. `docs/PRODUCT.md` — 사용자 기능 계약
4. `docs/DECISIONS.md` — 장기 결정과 supersession
5. `docs/DEVELOPER_REFERENCE.md` — 코드/데이터 흐름 지도
6. `docs/ARCHITECTURE.md` — 기술 경계
7. `docs/VERSIONING.md` — 릴리즈 버전 규칙
8. 작업 영역의 전문 문서
9. 관련 코드와 테스트

Map 작업이면 추가로 `MAP_PRODUCT_REQUIREMENTS.md`, `REFERENCE_POLICY.md`를 먼저 읽습니다. Quest availability 작업이면 `QUEST_PREREQUISITE_SEMANTICS.md`, 최근 source audit 문서를 같이 읽습니다.

---

# 3. 프로젝트 의존성 구조

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned vendor/Tarkov-Helper Map/MiniMap source (limited exception)

JunhyunHelper.Application
  ├─ JunhyunHelper.Core
  └─ JunhyunHelper.Infrastructure.Storage

JunhyunHelper.Infrastructure
  └─ JunhyunHelper.Core

JunhyunHelper.Core
  └─ product external dependency 없음
```

### Core

제품의 canonical domain과 deterministic 계산을 소유합니다. WPF, HTTP, SQLite를 알면 안 됩니다.

### Application

사용자 조작을 유스케이스 단위로 묶습니다. authoritative profile을 읽고 Core 계산을 호출하고 변경된 profile을 저장합니다.

### Infrastructure

외부 데이터, HTTP, SQLite/file persistence, content build/activation, program update 같은 I/O 경계를 소유합니다.

### Desktop

WPF UI, 화면 전환, 사용자 interaction, presentation cache, Map product bridge와 startup/update UX를 소유합니다. Core의 제품 의미를 UI에서 다시 계산하지 않습니다.

---

# 4. 데이터의 권위 분류

준현 헬퍼에서 가장 중요한 설계 원칙은 **데이터 종류별 권위가 다르다**는 점입니다.

| 종류 | 권위/출처 | 저장 위치 | 대표 소비자 |
|---|---|---|---|
| Game Content | json.tarkov.dev + 검증된 보조 source → canonical import | `%LocalAppData%/JunhyunHelper/content/<mode>/content.db` | Quest, Hideout, Items, Ammo |
| User Progress | 사용자가 입력/확정한 profile facts | `%LocalAppData%/JunhyunHelper/user.db` | Quest availability, Hideout, Needed Items |
| Inventory | 사용자가 입력한 보유량 + 명시적 고정 소모 자동 차감 | `user.db` | Items/cleanup, Quest/Hideout consumption |
| Presentation preferences | 사용 편의 설정 | atomic JSON + `.bak` | Ammo favorites, Map product settings |
| Image cache | 원본 URL의 presentation cache | `image-cache/` | Item/Hideout UI |
| Map artwork/config/general markers | pinned Map bundle | release `Assets/` | Main Map, MiniMap |
| Program files | GitHub stable Release | portable folder | updater |

**Game Content update와 Program update는 완전히 별개입니다.** Game Content가 바뀌어도 EXE를 교체하지 않고, EXE를 업데이트해도 `user.db`를 덮어쓰지 않습니다.

---

# 5. 프로그램 시작 흐름

```text
App.OnStartup
  ├─ fatal exception hooks 설치
  ├─ program update apply-mode라면 updater 경로 실행 후 종료
  ├─ stale temp updater cleanup 예약
  ├─ MainWindow 생성/표시
  └─ smoke가 아니면 ProgramUpdateCoordinator.CheckAtStartupAsync 예약

MainWindow.Window_Loaded
  └─ LoadProfilesAsync
      ├─ ProfileApplicationService.LoadAllAsync
      ├─ UserProfileStore.LoadAllAsync
      ├─ profile selector 구성
      └─ 선택 profile이 있으면 LoadSelectedProfileAsync
           ├─ ReadOrCreateContentAsync
           │   ├─ active content.db 없음 → Game Content update
           │   ├─ active read/validation
           │   └─ 실패 + previous 있으면 recovery / 아니면 update 재시도
           ├─ Quest workspace 생성
           ├─ Hideout workspace 생성
           ├─ Items workspace 생성
           ├─ Ammo page 데이터 설정
           └─ active section 표시
```

중요한 순서:

- MainWindow를 먼저 표시한 뒤 program update check를 비동기로 합니다. 네트워크 실패 때문에 앱 시작을 막지 않습니다.
- profile이 없으면 content 기능을 억지로 초기화하지 않고 profile 생성 UX를 보여 줍니다.
- content DB는 사용할 때 read/validation을 통과해야 합니다.

---

# 6. Profile 시스템

## 책임

게임 모드별 사용자의 장기 진행 상태를 저장합니다. 동일 GameMode profile은 하나만 허용합니다.

## 핵심 파일

- `Core/Profiles/GameProfileSnapshot.cs`
- `Core/Profiles/GameMode.cs`
- `Core/Profiles/PmcFaction.cs`
- `Core/Profiles/TraderProgress.cs`
- `Application/Profiles/ProfileApplicationService.cs`
- `Infrastructure/Storage/UserProfileStore.cs`
- `Desktop/Profiles/ProfileModeWindow.*`
- `Desktop/Profiles/ProfileEditorWindow.*`

## GameProfileSnapshot 주요 필드

- `ProfileId` — 현재 GameMode data key와 동일한 안정 ID
- `GameMode`
- `Level`
- `Faction`
- `EditionId`
- `PrestigeLevel`
- `Traders` — loyalty/standing의 부분 사실 허용
- `CompletedQuestIds`
- `FailedQuestIds`
- `SpecialTraderAccessOverrides`
- `ProfileVariables`
- `HideoutLevels`
- `Inventory`
- `QuestConsumptions`
- `HideoutUpgradeConsumptions`

## 저장

`UserProfileStore`가 SQLite `profiles` table의 JSON payload를 소유합니다.

- DB schema: v1
- JSON property 추가는 optional/default 방식으로 유지해 destructive migration을 피합니다.
- 읽은 snapshot은 in-process cache에 보관합니다.
- write가 성공한 뒤 canonicalized snapshot만 cache에 넣습니다.
- schema 생성은 store instance에서 한 번만 수행하며 동시 초기화는 gate로 직렬화합니다.

## 변경 영향

`GameProfileSnapshot`에 새 필드를 추가하면 최소한 다음을 같이 확인합니다.

1. `UserProfileStore.ProfileDocument.From`
2. `ProfileDocument.ToSnapshot`
3. validation/default/legacy compatibility
4. `ItemsApplicationService.PlanningStateEquals` — Needed Items basis에 영향을 주는 필드인지
5. profile editor 입력 필요 여부
6. Quest/Hideout/Items tests

---

# 7. Quest 시스템

## canonical 입력

- `QuestDefinition`
- `QuestObjective`
- `QuestItemRequirement`
- edition/trader/map references
- user profile

## QuestDefinition의 핵심 availability 정보

- minimum player level
- faction
- prestige
- task prerequisite + accepted status set
- trader standing
- trader loyalty
- special trader access
- exact profile variable requirements
- unsupported availability types
- completion failure conditions
- unsupported failure conditions
- restartable
- availability delay

## availability 상태

`QuestAvailabilityEvaluator`의 결과는 의미가 엄격합니다.

- `Current` — Helper가 현재 진행 가능함을 증명
- `Locked` — 아직 조건을 만족하지 않음
- `Completed` — 완료 fact 존재
- `Unavailable` — 이 profile에서 영구적으로 닫힘/실패 등
- `Indeterminate` — source 또는 profile fact가 부족해 증명 불가

`Indeterminate`를 UI/Application에서 임의로 `Current`로 승격하지 않습니다.

## prerequisite 의미

- 서로 다른 task requirement는 AND
- 하나의 requirement 안 accepted status들은 OR
- Helper에서 받을 수 있는 quest는 별도 “Available” 단계 없이 즉시 accepted/current로 간주
- missing referenced quest / dependency cycle / unsupported condition은 optimistic unlock하지 않음

## 특수 상인

BTR/Ref/Lightkeeper는 generic prerequisite만으로 정확히 표현되지 않는 verified rule이 있어 canonical special access requirement를 사용합니다. Lightkeeper처럼 unlock 뒤 access가 다시 사라질 수 있는 경우에만 sparse manual override를 허용합니다.

## profile variable

exact `ProfileVariables`가 있으면 그것이 우선입니다. 값이 없을 때 0이라고 가정하면 안 됩니다. 현재 audited task-pool 구조에 한해서만 `QuestTaskPoolVariableCompatibility`가 제한적으로 inference하고, upstream 구조가 달라지면 fail-closed합니다.

## Quest 사용자 조작

`QuestApplicationService`가 authoritative mutation boundary입니다.

### Complete

1. 현재 상태가 `Current` 또는 `Indeterminate`인지 확인
2. fixed single-item requirements만 `FixedInventoryConsumptionPolicy`로 차감
3. flexible hand-in은 무엇을 제출했는지 모르므로 자동 차감하지 않음
4. consumption ledger 기록
5. completed set에 추가, explicit failed set에서 제거
6. profile save
7. workspace 재계산

### Fail

수동 영구 실패 입력이 필요한 quest만 허용합니다. 일반 quest에 임의 failure fact를 쓰지 않습니다.

### Undo completion

- 기본은 inventory를 자동 복구하지 않음
- 사용자가 restore를 명시한 경로에서만 ledger를 역적용
- 복구하지 않았다면 ledger를 남겨 재완료 때 중복 차감하지 않음

### Undo failure

explicit failed set에서만 제거합니다.

## 화면

`Desktop/Quests/QuestPage.*`는 search/filter/detail/navigation/presentation을 담당합니다. Map navigation bridge는 Quest geometry를 Map subsystem에 전달하지만 Quest availability 의미 자체는 Map에서 다시 계산하지 않습니다.

---

# 8. 미래 Quest reachability와 Needed Items

Quest의 “지금 가능 여부”와 “미래에 아이템이 필요할 가능성”은 다릅니다.

`QuestFutureReachabilityEvaluator`는 다음을 구분합니다.

- `Potential`
- `Completed`
- `Unavailable`
- `IndeterminatePotential`

레벨/loyalty/prestige처럼 나중에 충족 가능한 gate는 미래 필요에서 제외하지 않습니다. faction/edition/disabled/permanent failure처럼 영구 배제되는 조건만 제거합니다. 모르는 조건은 `IndeterminatePotential`로 두어 사용자가 필요한 item을 버리게 만들지 않습니다.

---

# 9. Hideout 시스템

## 핵심 파일

- `Core/Hideout/HideoutStation.cs`
- `Application/Hideout/HideoutApplicationService.cs`
- `Desktop/Hideout/HideoutPage.*`

## 현재 level 규칙

profile `HideoutLevels`에 station key가 없으면 **Lv.0**입니다. “미입력이라 알 수 없음”이라는 별도 상태는 v1.0.0 제품 규칙에 존재하지 않습니다.

## level 변경

`HideoutApplicationService.SetLevelAsync`:

- content에 station이 있는지 확인
- 0..max 범위 검증
- level 상승 시 각 target level의 fixed item requirement를 순서대로 inventory에서 차감
- consumption ledger key는 `stationId:targetLevel`
- 이미 ledger가 있으면 같은 upgrade를 재차감하지 않음
- level 하락 시 기본은 item 자동 복구 없음
- explicit restore 경로에서는 ledger를 역적용하고 제거
- 최종 profile save 후 workspace 재계산

Needed Items는 **현재 level보다 높은 모든 미래 level**의 재료를 포함합니다.

---

# 10. Items / Inventory / Cleanup 시스템

## 핵심 파일

- `Core/Items/InventoryQuantity.cs`
- `Core/Items/ItemRequirement.cs`
- `Core/Items/NeededItemRequirementBuilder.cs`
- `Core/Items/NeededItemCalculator.cs`
- `Core/Items/FlexibleQuestItemRequirementCalculator.cs`
- `Core/Items/FutureNeededItemsPlanner.cs`
- `Core/Items/InventoryCleanupChangeDetector.cs`
- `Application/Items/FixedInventoryConsumptionPolicy.cs`
- `Application/Items/ItemsApplicationService.cs`
- `Desktop/Items/ItemsPage.*`

## 계산 파이프라인

```text
Game Content + Profile planning facts
  └─ QuestFutureReachabilityEvaluator
      └─ future quest ids

future quest item requirements
+ hideout levels > current level
  └─ NeededItemRequirementBuilder
      ├─ fixed requirements
      └─ flexible/alternative quest requirements

fixed requirements + Inventory
  ├─ NeededItemCalculator → NeededItems
  └─ InventorySurplusCalculator → CleanupItems

flexible requirements
  ├─ FlexibleQuestItemRequirementCalculator → 진행 표시
  └─ candidate item cleanup protection
```

## fixed vs flexible

accepted item ID가 하나인 requirement는 fixed입니다. 여러 candidate 중 하나를 제출할 수 있는 requirement는 flexible입니다.

Flexible requirement는:

- 어느 item이 실제로 제출될지 자동 추측하지 않음
- candidate item을 arbitrary cleanup 대상으로 만들지 않음
- 별도 progress group으로 표시

## cleanup 의미

Cleanup은 **현재 알려진 미래 fixed requirements에 비해 초과인 보유량**입니다.

FIR requirement가 있으면 FIR 필요량을 먼저 보호하고, unrestricted requirement에는 non-FIR를 우선 활용한 뒤 부족분만 FIR로 채우는 계산을 사용합니다.

## inventory mutation 최적화

`ItemsApplicationService`는 content reference + immutable profile snapshot에 대해 workspace cache를 둡니다. Inventory만 바뀐 경우 profile의 planning facts가 동일하면 기존 `FutureNeededItemsBasis`를 재사용해 Quest reachability와 requirement graph 전체를 다시 만들지 않습니다.

새 profile field가 Needed Items 의미에 영향을 주면 반드시 `PlanningStateEquals` 비교에도 추가해야 합니다.

---

# 11. Ammo 시스템

## 데이터

Ammo canonical data는 Tarkov source에서 가져오고, effectiveness는 별도 검증된 Wiki ballistics source를 통해 보완합니다. “ammo membership”과 “effectiveness”는 서로 다른 fact로 취급합니다.

## 화면

- `Desktop/Ammo/AmmoPage.xaml/.cs`
- `AmmoPage.ProductGridFixes.cs`
- `AmmoPage.ProductSearchAndDetails.cs`

UI는 caliber grouping, 검색, 상세 ballistic 표시, favorites를 담당합니다.

## favorites

`AmmoFavoriteStore` → `AtomicJsonFileStore`를 사용합니다.

- primary JSON
- 직전 정상 `.bak`
- same-directory temp + replace
- preference 저장 실패는 app fatal로 확대하지 않고 diagnostic만 남김

---

# 12. Game Content update

## source → canonical

대표 경로:

```text
TarkovJsonClient
  → TarkovEndpointSourceLoader
  → TarkovContentBuildService
      → item importer
      → quest importer/objective importer
      → hideout importer
      → ammo importer
      → reference importers
      → edition catalog
      → ballistics effectiveness
  → GameContentValidator
  → candidate content.db
  → candidate read-back + validation
  → ContentActivationService
  → active content.db
```

## 주요 파일

Infrastructure:

- `TarkovJson/TarkovJsonClient.cs` — HTTP/JSON 경계
- `TarkovJson/TarkovEndpointSourceLoader.cs` — endpoint document 모음
- `Content/TarkovContentBuildService.cs` — 전체 canonical build orchestration
- `TarkovJson/*Importer.cs` — source shape → domain conversion
- `Validation/GameContentValidator.cs` — 관계/값 gate
- `Storage/ContentSnapshotStore.cs` — SQLite snapshot serialization/read/integrity
- `Storage/ContentActivationService.cs` — candidate/active/previous lifecycle
- `Content/TarkovContentUpdateService.cs` — build→write→activate orchestration

## 안전 계약

- 새 데이터는 active file에 바로 쓰지 않음
- candidate를 먼저 완성
- canonical validation 통과
- candidate DB write
- DB integrity/read-back validation
- 그 뒤에만 active와 교체
- 이전 active는 `content.previous.db`로 보존
- 새 active 검증 실패 시 previous 복구
- update 실패가 `user.db`를 건드리지 않음

## schema

- current Content schema: v7
- readable: v3-v7
- 오래된 readable snapshot은 runtime compatibility transform을 거칠 수 있음
- storage shape migration과 interpretation fix를 구분함

---

# 13. Image cache

`Desktop/Services/ImageCacheService.cs`가 item/hideout presentation image를 담당합니다.

안전/성능 규칙:

- 최대 다운로드 8 MiB
- 최대 dimension 4096
- response Content-Length가 없어도 streaming byte count로 상한 확인
- decode 후 PNG로 normalize
- temp file → destination move
- concurrent download 6개 제한
- content update 후 필요한 image를 prefetch
- 개별 image 실패는 Game Content 전체 실패로 확대하지 않음
- corrupt local image는 삭제하고 재다운로드 가능 상태로 만듦

---

# 14. Map / MiniMap subsystem

## 경계

Map은 일반 JunhyunHelper layer와 다르게 pinned donor source를 포함합니다.

Desktop project가 `vendor/Tarkov-Helper/TarkovHelper`의 제한된 Models/Services/Map page/MiniMap/dialog source를 compile-link합니다. old updater, old content DB services, hidden global hook, old logger 등은 명시적으로 제외됩니다.

## JunhyunHelper가 소유하는 Map 연결

- `MainWindow.LegacyMapHost.cs` — donor Map host와 제품 shell의 연결
- `MainWindow.ProductLifecycle.cs` — product lifecycle 정리
- `MainWindow.MapSmokeV014.cs` — 실제 Map/Factory/MiniMap smoke harness; 파일명의 `V014`는 역사적 도입 시점 이름일 뿐 현재 product version source가 아님
- `MainWindow.ProductUiLayoutSmoke.cs` — rendered product UI assertions
- `Map/GlobalKeyboardHookService.JunhyunProduct.cs` — old hidden global command를 배제한 제품 소유 hotkey compatibility
- `Map/JunhyunExtractMarkerIcon.cs` — 제품 marker presentation
- `Quests/QuestPage.MapBridge.cs`, `QuestPage.MapNavigation.cs` — Quest → Map navigation
- `Legacy/TarkovHelper/LegacyMapHostCompatibility.cs` — donor source가 기대하는 최소 compatibility surface

## 데이터 분리

Map artwork/config/general marker bundle은 release `Assets/`에 있습니다. Quest current state/geometry는 current JunhyunHelper content/profile에서 bridge합니다.

즉:

- 일반 marker/artwork/config → Map bundle
- Quest availability/progress → JunhyunHelper canonical/content/profile

둘을 하나의 업데이트 경로로 섞지 않습니다.

## floor 계약

- 다른 floor의 marker를 단지 X/Z가 겹친다는 이유로 숨기지 않음
- floor relation은 presentation으로 표시
- floor switch 시 Main Map zoom + map-space center 유지
- MiniMap Scale + Translate X/Y exact transform 유지

## 수정 시 주의

Map donor source를 broad refactor하지 않습니다. 변경 전후 실제 publish EXE smoke에서 Main Map, Factory, MiniMap, floor switching, close lifecycle을 확인합니다.

---

# 15. Program update

## 확인

`GitHubProgramUpdateClient`가 `Propeex/JunhyunHelper`의 latest public stable GitHub Release를 확인합니다.

대상 조건:

- draft 아님
- prerelease 아님
- strict `vMAJOR.MINOR.PATCH`
- 현재 assembly version보다 큼
- exact package name `Junhyun-Helper-vX.Y.Z-win-x64.zip`
- `SHA256SUMS.txt` 존재

## 다운로드/검증

- asset URL은 JunhyunHelper GitHub Release download host/path만 허용
- checksum entry 형식 검증
- SHA-256 검증
- ZIP absolute/path traversal/colon/`.`/`..` 차단
- symlink 차단
- duplicate archive entry 차단
- PDB 차단
- 허용 root는 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets`
- staging directory에 필수 파일이 실제로 존재하고 비어 있지 않은지 확인

## 교체

현재 EXE 자체를 바로 덮어쓰지 않습니다.

1. TEMP에 현재 EXE self-copy runner 생성
2. runner가 parent process 종료 대기
3. staging file을 target directory의 temporary next file로 durable copy
4. 기존 product file을 previous path로 move
5. 새 file commit
6. 성공하면 previous 삭제
7. 실패하면 가능한 범위에서 rollback
8. product restart

User data는 `%LocalAppData%/JunhyunHelper`에 있어 portable product file transaction과 분리됩니다.

---

# 16. DesktopServices — composition root

`Desktop/Services/DesktopServices.cs`는 non-Map product service composition root입니다.

생성하는 것:

- `UserProfileStore`
- `ContentActivationService`
- shared `HttpClient`
- `ImageCacheService`
- `AmmoFavoriteStore`
- Tarkov source loader/build service
- `TarkovContentUpdateService`
- `ProfileApplicationService`
- `QuestApplicationService`
- `HideoutApplicationService`
- `ItemsApplicationService`

shared `HttpClient`의 User-Agent major/minor는 Desktop assembly version에서 파생합니다. 버전 문자열을 별도로 하드코딩하지 않습니다.

---

# 17. MainWindow 책임

`MainWindow` partial 파일은 product shell orchestration을 나눠 가집니다.

- `MainWindow.xaml.cs` — profile/content/workspace 기본 lifecycle, section, data update
- `MainWindow.FastMutations.cs` — Quest/Hideout/Inventory 같은 빠른 mutation 후 필요한 workspace만 갱신
- `MainWindow.Images.cs` — image 관련 integration
- `MainWindow.ProfileDeletion.cs` — profile delete UX/lifecycle
- `MainWindow.ProfileUsability.cs` — selected profile usability/availability 보조
- `MainWindow.SpecialTraderAccess.cs` — special trader access interaction
- `MainWindow.LegacyMapHost.cs` — Map host integration
- `MainWindow.ProductLifecycle.cs` — shutdown/lifecycle cleanup
- `MainWindow.ProductUiLayoutSmoke.cs` — rendered shell/product UI contract assertion
- `MainWindow.MapSmokeV014.cs` — Map/MiniMap asynchronous smoke assertion

MainWindow에 새 domain rule을 넣지 않습니다. MainWindow는 Application service를 호출하고 결과 workspace를 화면에 배포하는 orchestration layer입니다.

---

# 18. 페이지별 UI 책임

## QuestPage

- quest list/search/filter/status presentation
- detail 표시
- explicit complete/fail/undo action 요청 event
- special trader access UI
- Quest → Map navigation
- scroll/navigation state 보존

## HideoutPage

- station/current/next level presentation
- level change 요청
- 재료와 연결 정보 presentation

## ItemsPage

- needed/cleanup/satisfied/deferred view
- category/usage/search filter
- inventory FIR/non-FIR edit
- flexible requirement group
- source click → Quest/Hideout navigation
- icon lazy load/cache
- cleanup increase notice

## AmmoPage

- caliber 기반 group/list
- search/details
- effectiveness presentation
- favorites

## Scanner placeholder

실제 service나 domain model이 없어야 정상입니다. placeholder를 유지하는 것 자체가 제품 계약입니다.

---

# 19. 저장 파일과 복구 정책

기본 root:

`%LocalAppData%/JunhyunHelper`

대표 경로:

- `user.db` — 사용자 progress/inventory
- `content/<mode>/content.db` — active game content
- `content/<mode>/content.candidate.db` — update candidate
- `content/<mode>/content.previous.db` — previous known-good
- `image-cache/` — presentation image
- `ammo-favorites.json` + `.bak`
- `map-product-settings.json` + `.bak`
- `updates/pending/` — program update temporary payload
- `logs/startup.log` — app/update diagnostics

portable release root는 별도이며 원칙적으로 다음만 둡니다.

- `준현 헬퍼.exe`
- `FIRST_RUN_KO.txt`
- `Assets/`

runtime log를 portable root에 만들지 않습니다.

---

# 20. 오류 격리 원칙

오류가 어느 경계까지 영향을 줄지 의도적으로 제한합니다.

- program update check network failure → app 정상 실행 계속
- image load failure → 해당 image만 비어 있음
- favorite/map preference save failure → diagnostic, app 계속
- invalid candidate Game Content → active known-good 유지
- unsupported Quest availability → `Indeterminate`, optimistic unlock 금지
- updater validation failure → current program files untouched
- updater replacement failure → rollback/restart 시도
- fatal WPF/unhandled exception → LocalAppData diagnostic + 사용자 오류 표시 + 종료

catch-all을 써도 되는 곳은 **best-effort presentation/recovery cleanup 경계**뿐입니다. domain correctness 오류를 catch해서 정상값처럼 숨기지 않습니다.

---

# 21. 성능 구조

현재 의도된 주요 최적화 지점:

- `UserProfileStore` in-memory snapshot cache
- schema initialization once per store instance
- Application workspace reference cache
- Items inventory-only mutation 시 `FutureNeededItemsBasis` 재사용
- Items icon 기존 object 재사용 + async lazy load
- image download concurrency 6
- content는 canonical snapshot 단위로 읽고 UI에서 원본 JSON을 반복 파싱하지 않음
- Map donor는 성능 문제 증거 없이 broad rewrite하지 않음

새 최적화를 할 때 계산 결과를 바꾸는 캐시를 넣지 말고, **동일 입력에 대한 deterministic 결과 재사용**임을 테스트로 증명합니다.

---

# 22. first-party 파일 책임 카탈로그

아래는 v1.0.0 기준 first-party source를 기능별로 빠르게 찾기 위한 색인입니다.

## Core / Ammo

- `AmmoCaliberDisplay.cs` — caliber 표시 문자열 정규화
- `AmmoDefinition.cs` — canonical ammunition fact model

## Core / Content

- `GameContentCatalog.cs` — runtime canonical content aggregate

## Core / Editions

- `EditionDefinition.cs` — edition availability/reference model

## Core / Hideout

- `HideoutStation.cs` — station/level/item requirement model

## Core / Items

- `GameItem.cs` — canonical item model
- `InventoryQuantity.cs` — FIR/non-FIR quantity 및 normalize
- `InventoryConsumption.cs` — 자동 소모 ledger value
- `ItemRequirement.cs` — fixed future requirement + source
- `NeededItem.cs` — Needed Items 계산 결과
- `NeededItemRequirementBuilder.cs` — Quest/Hideout raw requirement를 fixed/flexible로 분리
- `NeededItemCalculator.cs` — required/owned/remaining 계산
- `FlexibleQuestItemRequirementCalculator.cs` — alternative candidate group progress
- `FlexibleQuestItemGroupStateEvaluator.cs` — flexible group 상태 보조
- `FutureNeededItemsPlanner.cs` — 미래 reachability + hideout + cleanup 통합
- `NeededItemsQuery.cs` — Needed item query helper
- `InventoryCleanupChangeDetector.cs` — mutation 전후 새 cleanup 증가 감지

## Core / Profiles

- `GameMode.cs` — Regular/PvE 등의 mode와 data key
- `GameProfileSnapshot.cs` — authoritative user progress aggregate
- `PmcFaction.cs` — faction
- `TraderProgress.cs` — loyalty/standing partial facts

## Core / Quests

- `QuestDefinition.cs` — canonical quest gate/failure/special-access model
- `QuestObjective.cs` — objective data
- `QuestAvailability.cs` — availability result/reason types
- `QuestAvailabilityEvaluator.cs` — current availability engine
- `QuestCatalogQuery.cs` — content/profile → UI-consumable quest entries
- `QuestFailureEvaluator.cs` — explicit/automatic failure set 계산
- `QuestFutureReachability.cs` — future item planning reachability
- `QuestTaskPoolVariableCompatibility.cs` — audited task-pool variable runtime compatibility

## Core / Reference

- `TraderDefinition.cs` — trader reference
- `MapReference.cs` — map reference

## Application

- `Profiles/ProfileApplicationService.cs` — profile CRUD/settings mutation
- `Quests/QuestApplicationService.cs` — quest mutation/workspace
- `Hideout/HideoutApplicationService.cs` — hideout mutation/workspace
- `Items/ItemsApplicationService.cs` — item workspace/inventory mutation/cache
- `Items/FixedInventoryConsumptionPolicy.cs` — fixed item consume/restore rule

## Infrastructure / Storage

- `UserProfileStore.cs` — user.db
- `ContentSnapshotStore.cs` — canonical content.db read/write/schema/integrity
- `ContentActivationService.cs` — candidate/active/previous transaction
- `AtomicJsonFileStore.cs` — preference JSON atomic save + backup recovery

## Infrastructure / Content and source

- `ContentUpdateProgress.cs` — progress stage model
- `TarkovContentBuildService.cs` — entire online build orchestration
- `TarkovContentUpdateService.cs` — build/write/activate operation
- `WikiBallisticsEffectivenessClient.cs` — effectiveness source adapter
- `TarkovEditionCatalogClient.cs` — edition source adapter
- `TarkovJsonClient.cs` — HTTP JSON fetch
- `TarkovJsonDocument.cs` / `TarkovJsonReader.cs` — source JSON access helpers
- `TarkovEndpoint.cs` / `TarkovEndpointSource.cs` / `TarkovEndpointSourceLoader.cs` — endpoint definitions/source loading
- `TarkovTranslationCatalog.cs` — translation fact lookup
- `TarkovGameContentImporter.cs` — aggregate canonical import/legacy compatibility
- `Items/TarkovItemImporter.cs` — item import
- `Quests/TarkovQuestImporter.cs` — quest/gate import
- `Quests/TarkovQuestObjectiveImporter.cs` — objective/item requirement import
- `Quests/TarkovDialogueAvailabilityCompatibility.cs` — verified dialogue interpretation compatibility
- `Hideout/TarkovHideoutImporter.cs` — hideout import
- `Ammo/TarkovAmmoImporter.cs` — ammo import
- `Reference/TarkovReferenceImporters.cs` — trader/map reference import
- `GameContentValidator.cs` — candidate canonical gate

## Infrastructure / Program update

- `GitHubProgramUpdateClient.cs` — latest release fetch/download/checksum/package validation
- `ProgramUpdateApplier.cs` — product file transaction/rollback

## Desktop / services

- `DesktopServices.cs` — composition root
- `ImageCacheService.cs` — image fetch/cache/normalize
- `AmmoFavoriteStore.cs` — favorites persistence adapter
- `UiReferenceOrder.cs` — product UI reference ordering

## Desktop / shell and pages

- `App.xaml/.cs` — process startup, fatal diagnostics, updater apply mode
- `MainWindow*` — shell orchestration/Map integration/smoke
- `Profiles/*` — profile mode/editor windows
- `Quests/*` — quest page + navigation/special access/Map bridge
- `Hideout/*` — hideout page
- `Items/*` — items/inventory/flexible/source navigation
- `Ammo/*` — ammo page
- `Controls/ProductSearchClearButtonBehavior.cs` — reusable search clear interaction
- `Updates/ProgramUpdateCoordinator.cs` — startup consent UX + temp runner launch
- `Map/*` 및 `Legacy/TarkovHelper/*` — JunhyunHelper-owned Map compatibility/product bridge
- `Build/GenerateApplicationIcon.ps1` — AppIcon.png → build ICO generation

## vendor Map

`vendor/Tarkov-Helper/TarkovHelper` 아래 compile-linked Map source는 **외부 pinned module**로 취급합니다. 해당 donor 파일을 이 문서에서 first-party처럼 개별 재정의하지 않습니다. 실제 포함/제외 목록의 권위는 `JunhyunHelper.Desktop.csproj`입니다.

---

# 23. 테스트 구조

`tests/JunhyunHelper.Tests`는 제품 의미가 있는 deterministic logic과 storage/update boundary를 검사합니다.

대표 영역:

- `Application/*` — Profile/Quest/Hideout mutation, inventory consumption
- `Items/*` 및 root item tests — needed/cleanup/flexible/reachability
- `Quests/*` — availability, edition, special trader, task pool variable, partial facts
- `Infrastructure/*` — source/update/program updater/ballistics compatibility
- `Storage/*` — user/content/atomic preference storage/recovery
- `Resilience/*` — major upstream/update drift fail-closed behavior

Desktop WPF/Map는 xUnit만으로 충분하지 않아 CI에서 **실제 publish된 EXE**를 실행합니다.

CI release candidate gate:

1. Release build
2. full tests
3. Windows x64 self-contained single-file publish
4. ProductVersion / FIRST_RUN version identity 확인
5. root layout 확인
6. DLL/PDB/nested archive/legacy dependency 오염 확인
7. actual EXE startup
8. rendered Product UI assertions
9. Main Map / Factory / MiniMap smoke
10. normal MainWindow close
11. process 종료 확인
12. portable root runtime pollution 확인

---

# 24. 기능을 수정할 때의 영향 추적 방법

## Quest gate를 바꿀 때

확인 순서:

`source field → importer → QuestDefinition → QuestAvailabilityEvaluator → QuestFutureReachabilityEvaluator → QuestCatalogQuery → QuestApplicationService/UI → Needed Items → tests/docs`

## profile fact를 추가할 때

`GameProfileSnapshot → UserProfileStore document/from/to/validation → profile editor/import path → evaluator → Items PlanningStateEquals 여부 → tests`

## Game Content field를 추가할 때

`source adapter → canonical model → importer → validator → ContentSnapshot schema compatibility → consumers → resilience test`

storage shape가 변하면 Content schema 증가를 검토합니다. 단순 runtime interpretation fix라면 불필요하게 schema를 올리지 않습니다.

## Items 계산을 바꿀 때

`raw Quest/Hideout requirements → builder → fixed/flexible → calculator → cleanup protection → UI filters/source navigation → mutation cleanup change detection`

## Program update를 바꿀 때

`release parsing → URL trust → checksum → ZIP validation → staging → applier transaction/rollback → restart → release workflow/public verification`

## Map을 바꿀 때

`PRODUCT/Map requirement → donor/product bridge 위치 판별 → 최소 코드 변경 → actual published EXE Main Map/Factory/MiniMap/floor/lifecycle smoke`

---

# 25. 하지 말아야 할 것

- 현재 코드가 있으므로 그것을 자동으로 제품 요구사항이라고 간주
- unknown Quest condition을 true/Current로 추측
- missing profile variable을 0으로 간주
- flexible item 실제 소비를 자동 추측
- content update 중 active DB를 먼저 덮어쓰기
- program update에서 checksum/package validation 전에 product file 수정
- user.db를 content/program update와 함께 초기화
- Map donor를 style cleanup 목적으로 대규모 재작성
- Scanner placeholder를 “미완성이라 보기 싫다”는 이유로 숨기거나 실제 기능처럼 꾸미기
- UI event handler에 domain truth 계산 복제
- 버전 문자열을 여러 곳에 서로 다른 값으로 수동 하드코딩

---

# 26. 의도적으로 남아 있는 비기능/한계

## Scanner

visible `준비 중` placeholder. 실제 Scanner 기능 미구현이 정상 상태입니다.

## EFT Story Chapters

현재 ordinary task source 범위 밖입니다. source가 없는 진행 의미를 추측하지 않습니다.

## 일부 profile-variable/task-pool 구조

audited current compatibility만 사용하며 upstream 구조 drift는 fail-closed합니다.

## Map donor debt

일부 legacy 구조/경고가 존재하지만 pinned subsystem의 검증된 동작을 깨지 않는 것이 우선입니다. 구체적 문제 없이 clean architecture 목적만으로 리팩터링하지 않습니다.

---

# 27. 버전과 릴리즈

버전 정책의 권위는 `VERSIONING.md`입니다.

- 새 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/안정성/성능 개선 → PATCH +1
- 새 기능과 수정이 함께 있으면 MINOR 규칙

릴리즈 시 project `<Version>`, published ProductVersion, `FIRST_RUN_KO.txt`, GitHub tag, ZIP filename, release notes가 일치해야 합니다.

v1.0.0부터 public stable release가 self-updater의 source of truth이므로 **draft asset 검증을 끝내기 전에 public/latest로 만들면 안 됩니다**.

---

# 28. 빠른 진단 질문

새 문제를 받을 때 아래 질문을 코드에 던지면 범위를 빨리 찾을 수 있습니다.

1. 이것은 Game Content 사실인가, User Progress 사실인가, presentation preference인가?
2. authoritative write boundary는 어디인가?
3. 계산 결과인가 저장 truth인가?
4. unknown을 어떻게 표현해야 하는가? false/zero로 바꾸면 안 되는가?
5. 이 변경은 Quest current availability뿐 아니라 future reachability/Needed Items에도 영향을 주는가?
6. inventory consumption ledger와 undo/re-completion에 영향이 있는가?
7. content schema/user schema compatibility가 필요한가?
8. Map donor 영역인가 first-party 영역인가?
9. 실패 시 기존 정상 데이터/프로그램을 보존하는가?
10. 기존 실제 publish smoke가 이 변경을 잡을 수 있는가, 추가 assertion이 필요한가?

이 열 가지에 답할 수 있으면 대부분의 변경 범위를 정확히 잡을 수 있습니다.
