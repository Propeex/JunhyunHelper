# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

## 현재 상태

`CONFIRMED — v0.1.0 release candidate architecture`

기술 스택:

- .NET 10
- C#
- WPF Desktop (`net10.0-windows`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — external image decode / PNG normalize
- SharpVectors — SVG Map rendering
- 별도 backend 없음
- runtime AI/GPT 없음

솔루션 경계:

```text
JunhyunHelper.Core
JunhyunHelper.Infrastructure
JunhyunHelper.Application
JunhyunHelper.Desktop
```

## 1. 최상위 데이터 흐름

```text
json.tarkov.dev / approved supplemental sources
→ Infrastructure source loader
→ external schema / semantic validation
→ canonical Core models
→ candidate content.db
→ relationship/read-back validation
→ atomic active content activation
→ Desktop image prefetch
→ Application + user.db
→ derived workspace
→ WPF presentation
```

원칙:

- 외부 API DTO가 Core/Application/Desktop 전체로 새지 않음
- 업데이트 실패가 마지막 정상 active content를 훼손하지 않음
- Game Content와 User Progress는 분리
- Derived state는 매번 canonical content + User Progress에서 계산
- 불명확한 사실을 런타임에서 추측하지 않음

## 2. Game Content 저장

경로:

```text
%LocalAppData%/JunhyunHelper/content/<game-mode>/content.db
%LocalAppData%/JunhyunHelper/content/<game-mode>/content.candidate.db
%LocalAppData%/JunhyunHelper/content/<game-mode>/content.previous.db
```

현재 Content snapshot schema: **v4**.

- v1: 초기 canonical snapshot
- v2: Item category metadata
- v3: `AmmoDefinition.IsWikiBallisticsListed` 추가
- v4: Quest `possibleLocations` / `zones` geometry를 canonical content에 저장

v3에서 Wiki Ballistics의 두 의미를 분리합니다.

```text
IsWikiBallisticsListed : bool?
ArmorEffectiveness     : AmmoArmorEffectiveness?
```

- `true`: healthy current Wiki table에 해당 Ammo가 존재
- `false`: healthy current Wiki table에 없음
- `null`: source state를 안전하게 확정할 수 없음
- effectiveness null은 membership false를 의미하지 않음

v4 Quest geometry는 Map Quest projection용 canonical fact입니다. Map artwork/config/general-marker bundle과 같은 저장소가 아니며, content updater가 Map artwork를 교체하지 않습니다.

이전 content schema는 새 source에서 재구축합니다. User Progress migration과 결합하지 않습니다.

## 3. User Progress 저장

경로:

```text
%LocalAppData%/JunhyunHelper/user.db
```

SQLite table schema는 현재 기존 version을 유지하고 profile payload JSON의 additive optional field로 확장합니다.

`GameProfileSnapshot` 주요 사실:

- identity / GameMode
- level / faction / edition / prestige
- Traders
- CompletedQuestIds / FailedQuestIds
- HideoutLevels
- Inventory (`Fir`, `NonFir` internal identifiers)
- QuestConsumptions
- HideoutUpgradeConsumptions

사용자 표시에서는 `Fir` 의미를 **인레이드**, `NonFir` 의미를 **일반**으로 표현합니다.

Prestige legacy null은 deserialize/product boundary에서 0으로 정규화합니다.

## 4. Inventory 자동 소비 reconciliation

### 4.1 Core record

```text
InventoryConsumption
└─ ItemId → InventoryQuantity(Fir, NonFir)
```

이 record는 **실제로 자동 차감한 양**만 기록합니다. 원래 requirement를 다시 계산해 복원하지 않습니다.

### 4.2 Application policy

`FixedInventoryConsumptionPolicy`가 차감/복원을 담당합니다.

차감:

```text
FoundInRaid requirement
→ Fir만 사용

unrestricted/general requirement
→ NonFir 먼저
→ 부족분만 Fir
```

tracked quantity보다 많이 차감하지 않으며 음수가 발생하지 않습니다.

### 4.3 Quest integration

`QuestApplicationService.CompleteAsync`

- `AcceptedItemIds.Count == 1`인 고정 requirement만 자동 소비
- alternative/flexible hand-in은 어느 candidate를 제출했는지 알 수 없으므로 자동 소비 금지
- 실제 소비량을 `QuestConsumptions[questId]`에 기록

`UndoCompletionAsync(..., restoreInventory)`:

- restore=true → exact ledger restore + ledger 제거
- restore=false → inventory/ledger 유지

restore=false에서 ledger를 유지하는 이유는 같은 Quest 재완료 때 중복 차감을 막기 위해서입니다.

### 4.4 Hideout integration

`HideoutApplicationService.SetLevelAsync`는 현재 level과 target level 사이의 각 upgrade를 순차 처리합니다.

ledger key:

```text
<stationId>:<targetLevel>
```

rollback:

- restore=true → 해당 rolled-back level들의 exact ledger 복원 + 제거
- restore=false → ledger 유지

기존 ledger가 남은 level을 다시 올릴 때는 이미 소비된 재료로 판단하여 자동 재차감하지 않습니다.

## 5. Quest availability

Core는 가능한 한 실제 prerequisite/condition을 결정론적으로 평가합니다.

Core states:

- Current
- Locked
- Unavailable
- Completed
- Indeterminate

Application 제품 policy:

```text
residual Indeterminate
→ Current for user-facing workflow
→ diagnostic reasons preserved
```

확정 가능한 Locked/Unavailable은 승격하지 않습니다.

Quest refresh와 navigation은 분리합니다.

- refresh: 기존 ScrollViewer offset 보존
- explicit stable-ID navigation: target selection + ScrollIntoView 허용

## 6. Needed Items / flexible requirement

`ItemsApplicationService`와 Core item planners가 Quest/Hideout 미래 요구를 결합합니다.

고정 requirement와 flexible requirement를 구분합니다.

Flexible:

```text
Quest objective
→ AcceptedItemIds[]
→ candidate inventory aggregate
→ group progress
```

개별 candidate를 임의 선택하지 않으며 cleanup도 목표 종료 전 보수적으로 보호합니다.

Desktop은 flexible view를 Quest별 group으로 projection합니다. 이 projection은 계산 진실의 원천이 아닙니다.

## 7. Ammo source architecture

Raw stats:

```text
json.tarkov.dev
→ AmmoDefinition
```

Wiki supplemental source:

```text
Wiki Ballistics HTML/API
→ row identity parsing
→ canonical English ammo name unique match
→ membership
→ optional six effectiveness values
```

건강성 검증이 통과한 source만 membership을 적용합니다. coverage가 비정상적으로 낮으면 schema/source 이상으로 보고 base content를 유지합니다.

Desktop filter:

- membership fact가 존재하는 healthy content → `IsWikiBallisticsListed == true`만 비교
- membership이 전부 unknown/null인 source-failure content → raw Ammo fallback

Ammo sort는 Desktop에서 user-sortable state로 저장하지 않고 penetration → damage → name으로 고정합니다.

## 8. Caliber display boundary

raw caliber code는 canonical 식별/데이터 의미로 보존합니다.

사용자 표시 label은 별도 deterministic mapping으로 표현합니다.

예:

```text
Caliber784x49  → .308 Marlin Express
Caliber93x64   → 9.3x64mm
Caliber9x18PM  → 9x18mm Makarov
Caliber127x33  → .50 Action Express
Caliber127x108 → 12.7x108mm
```

표시 label을 raw ID로 역변환해 business logic에 사용하지 않습니다.

## 9. 이미지 pipeline

cache root:

```text
%LocalAppData%/JunhyunHelper/image-cache
```

pipeline:

```text
canonical URL
→ bounded HTTP download
→ SkiaSharp decode
→ dimension/size validation
→ PNG normalize
→ cache
→ WPF BitmapImage
```

공통 Item cache key:

```text
item-<itemId>
```

Game Content update가 성공하면 다음 presentation asset을 prefetch합니다.

- Quest requirement candidates
- Hideout materials
- Ammo Items
- Hideout stations

prefetch는 update progress의 후반 stage이지만 개별 image failure는 non-fatal입니다.

## 10. Desktop navigation graph

navigation은 표시 이름이 아니라 stable canonical ID를 사용합니다.

```text
Quest Item        → Item
Quest prerequisite→ Quest
Item Quest source → Quest
Item Hideout source→ Hideout
Hideout material  → Item
Ammo unlock Quest → Quest
```

각 page는 navigation event만 발생시키고 `MainWindow`가 section 전환과 target navigation을 조정합니다.

## 11. UI filter grouping

Map variant 병합은 Desktop filter projection에서만 수행합니다.

```text
Ground Zero / Ground Zero 21+ → Ground Zero
Factory day / Factory night   → Factory
```

canonical MapReference와 Quest.MapId는 변경하지 않습니다.

## 12. Map + MiniMap architecture

Map은 JunhyunHelper의 Core Game Content renderer를 확장한 기능이 아니라 **독립 subsystem**입니다.

```text
JunhyunHelper MainWindow
└─ exact-source Tarkov Helper Map/MiniMap host
   ├─ bundled Map artwork/config/general-marker DB
   ├─ screenshot / player tracking
   ├─ floor / zoom / MiniMap runtime
   └─ JunhyunHelper product delta
      ├─ product settings persistence
      ├─ product global hotkey dispatch
      ├─ Quest projection
      ├─ extract/general marker synchronization
      └─ MiniMap product policies
```

유일한 cross-feature dependency는 Quest입니다.

```text
JunhyunHelper Quest workspace + v4 Quest geometry
→ LegacyMapQuestV2Controller
→ Main Map / MiniMap Quest projection
```

Hideout / Item / Ammo runtime은 Map과 직접 결합하지 않습니다.

### 12.1 Source boundary

현재 pinned source:

```text
Propeex/Tarkov-Helper
branch: junhyun-map-product-v2
revision: d933792b6042a51cea38dc44b686a096fe30de67
submodule: vendor/Tarkov-Helper
```

old Tarkov-Helper의 non-Map content/update subsystem은 JunhyunHelper Desktop compile에서 제외합니다.

특히 v0.1.0 release hardening부터 old `TarkovHelper.Services.UpdateService`도 제외합니다. 이 legacy updater는 `Propeex/Tarkov-Helper/update.xml`을 대상으로 하며 JunhyunHelper release/update 경계와 무관합니다.

### 12.2 Map product settings

권위 저장소:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

여기에 marker toggle, Quest marker toggle, 사용자 조정값, screenshot folder, product hotkey, MiniMap temporary hide, normal opacity, marker scale 등을 저장합니다.

legacy settings는 Map 내부 compatibility를 위해 일부 존재할 수 있지만 제품에서 확정한 JunhyunHelper-owned 값이 최종 권위입니다.

### 12.3 Global input

JunhyunHelper-owned global keyboard dispatcher가 product hotkey를 처리합니다.

허용 foreground:

- `EscapeFromTarkov`
- `EscapeFromTarkov_BE`
- `JunhyunHelper`
- legacy compatibility용 `TarkovHelper`

MiniMap을 켤 때 transplanted legacy zoom/floor hook이 다시 같은 key를 잡지 못하도록 direct legacy mapping은 비활성 상태로 유지합니다.

### 12.4 Map bundle update boundary

v0.1.0에서는 Map artwork/config/general-marker DB를 검증된 pinned bundle로 배포합니다.

향후 updater를 구현하면 반드시 다음을 한 unit으로 다룹니다.

```text
same upstream revision
├─ artwork
├─ config
└─ general-marker DB
```

서로 다른 revision의 Map asset을 섞어 활성화하지 않습니다.

## 13. 로컬 UI preferences

Ammo caliber favorites:

```text
%LocalAppData%/JunhyunHelper/ammo-favorites.json
```

Map product settings:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

둘 다 Game Content/User Progress와 분리된 presentation preference입니다. content update로 삭제하지 않습니다.

## 14. 배포 경계

v0.1.0은 Windows x64 self-contained portable 배포입니다.

- installer 없음
- 별도 .NET 설치 불필요
- 관리자 권한 불필요
- code signing 미구성
- app 자체 auto-updater 미구현

Release publish는 debug symbol을 포함하지 않으며, old AutoUpdater/WebView2/GraphX/QuikGraph dependency가 다시 들어오면 CI를 실패시킵니다.

GitHub Actions Artifact에는 publish directory를 직접 업로드합니다. Artifact 자체가 다운로드 시 ZIP이 되므로 내부 ZIP을 한 번 더 만들지 않습니다.

## 15. 실패 / 안전 원칙

- API incompatible change → update 중단, previous active 유지
- Wiki failure → base Ammo 유지, membership/effectiveness 추정 금지
- image failure → presentation asset만 누락, content 성공 유지
- flexible hand-in consumption ambiguity → 자동 차감 금지
- rollback restore 여부 → 사용자에게 확인
- 저장된 consumption ledger가 없으면 존재하지 않는 과거 소비량을 추정해 복원하지 않음
- Map initialization failure가 User Progress/Game Content를 수정하지 않음
- release/update 기능은 old Tarkov-Helper updater에 위임하지 않음
