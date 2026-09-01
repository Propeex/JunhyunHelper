# ARCHITECTURE — Farming Guide

기준일: **2026-09-01 KST**  
대상 제품: **v1.14.0+**

이 문서는 `파밍 가이드` subsystem의 책임, 데이터 authority, persistence, assembly editing, storage presentation, Tarkov 변화 대응, 검증 경계를 정의한다.

제품 의미는 다음 문서를 함께 따른다.

- `docs/PRODUCT.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- 현재 사실값: `docs/PROJECT_STATE.json`
- 공개/검증 상태: `docs/CURRENT_STATE.md`, `docs/STATE.md`

## 1. 목적과 비목표

Farming Guide는 **raid-start Loadout / Inventory Editor**다.

목적:

- 레이드 시작 시점의 착용 장비와 수납 상태를 구성한다.
- 실제 Tarkov item width/height, storage grids, filters, attachment/armor slots를 조작 가능한 surface로 제공한다.
- nested container와 recursive assembly를 deterministic state로 유지한다.
- current game structure가 바뀌었을 때 impossible state를 fail closed한다.

현재 비목표:

- 실제 Tarkov inventory를 실시간으로 1:1 동기화
- process memory / packet / injection 기반 동기화
- loot 가치 판단 또는 pickup/discard 추천
- Scanner와의 실시간 추천 결합
- 모든 arbitrary weapon build를 Tarkov client와 동일한 완성 이미지로 렌더링
- 검증되지 않은 carrier visual coordinates를 authentic layout으로 추정

## 2. 시스템 경계

```text
JunhyunHelper.Desktop
  ├─ Farming Guide page / spatial equipment board
  ├─ drag/drop presentation and geometry probing
  ├─ item/assembly visual presentation
  ├─ recursive in-page workbench
  ├─ inline compatible-item picker
  ├─ storage visual-layout renderer
  ├─ preset UI
  └─ published-runtime smoke hooks

JunhyunHelper.Core
  ├─ Farming Guide state model
  ├─ nested storage parent-instance contract
  ├─ equipment/carrier compatibility
  ├─ placement / packing rules
  ├─ FarmingGuideAssemblyPolicy
  ├─ FarmingGuideStorageVisualLayoutResolver
  ├─ search eligibility
  ├─ pocket geometry policy
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ validated Tarkov item import
  ├─ assembly source import
  ├─ storage layout identity import
  ├─ Content snapshot v10 write / v3-v10 read
  └─ Farming Guide user-state persistence
```

WPF event handler가 Tarkov compatibility, persisted-state truth, exact-layout validity를 독자적으로 재구현하지 않는다.

## 3. 데이터 authority

### 3.1 Game Content mechanics

Storage와 equipment mechanics의 authority:

```text
current validated Tarkov item source
→ TarkovItemImporter
→ canonical GameItem / FarmingGuideItemLayout
→ Content snapshot v10
```

사용하는 구조:

- item width / height
- item/category/type identity
- storage grid count / width / height
- grid allowed/excluded filters
- attachment slots / required flags / filters
- replaceable armor slots / allowed plate IDs
- item conflicts
- weapon default preset reference
- preset contained item IDs
- preset image URLs
- optional storage layout identity

외부 source가 의미를 증명하지 못하는 값은 fabricate하지 않는다.

### 3.2 Storage visual arrangement

수납 **가능성**과 수납 grid의 **화면상 상대 위치**는 다른 authority다.

- legality: current Game Content grids/filters
- visual arrangement: product-owned verified `FarmingGuideStorageVisualLayout` metadata

Visual metadata는 current mechanics를 변경할 권한이 없다.

### 3.3 사용자 상태

Authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

포함:

- current working snapshot
- saved presets
- selected preset identity
- equipped items
- carrier states
- recursive attachment / armor plate tree
- root/nested stored placements
- rotation
- `ParentInstanceId`
- user-level fixed melee setting

Program Update와 Game Content Update는 사용자의 정상 저장 상태를 임의로 덮어쓰지 않는다. 다만 load 시 current game truth와 모순되는 impossible 부분은 fail closed한다.

## 4. Editor state model

```text
profile working snapshot
├─ Equipment: slot -> FarmingGuideItemState
├─ Rig / Backpack / SecureContainer: FarmingGuideItemState
└─ StoredItems
   ├─ root placement
   │  └─ ParentInstanceId = null
   └─ nested placement
      └─ ParentInstanceId = stored container instance id

FarmingGuideItemState
├─ ItemId
├─ Attachments: slot id -> child state
└─ ArmorPlates: slot id -> child state
```

`InstanceId`는 stored placement identity다. `ParentInstanceId`는 nested storage ownership identity다.

v1.14.0부터 장비 보드의 PMC dogtag surface는 제품 UI에서 제거된다. Legacy dogtag persistence는 schema-v1 호환성을 위해 읽을 수 있지만 current working equipment state에서 정상 장비로 유지하지 않는다.

## 5. Equipment / carrier compatibility

대표 equipment targets:

- headset
- helmet/headwear
- face cover
- armband
- body armor
- eyewear
- primary weapon 1/2
- holster
- melee

유지 계약:

- pistol/revolver/handgun은 Holster target이다.
- pistol은 generic primary weapon target으로 취급하지 않는다.
- body armor / rig / backpack은 current property/type/category 의미를 함께 사용한다.
- carrier assignment와 equipment dictionary는 저장 구조가 다르더라도 사용자 interaction에서는 동일한 drag/drop board 계약을 따른다.

Secure Container는 일반 case/container와 구분한다.

```text
explicit secure-container / pouch semantic
→ accept

else ItemPropertiesContainer
  + generic container/case classification 없음
→ narrow current-data fallback

ordinary case/container
→ reject
```

Epsilon과 Medicine Case가 모두 `ItemPropertiesContainer`일 수 있으므로 property type 하나만으로 허용하지 않는다.

## 6. Profile-aware pockets

Pockets는 item carrier가 아니라 profile capability 기반 product-owned geometry다.

```text
standard: 1×1 / 1×1 / 1×1 / 1×1
expanded: 1×1 / 1×2 / 1×2 / 1×1
```

입력:

- active profile edition
- authoritative progress로 증명 가능한 Old Patterns 상태

동일 resolved geometry를 rendering, drop legality, capacity summary, persisted-state sanitization이 공유한다.

## 7. Storage rendering architecture

### 7.1 Live grid mechanics

`FarmingGuideItemLayout.StorageGrids`가 current mechanics다.

각 grid는 width, height, item filter를 가진다.

### 7.2 Layout identity import

Importer는 존재할 경우 다음 field를 `StorageLayoutName`으로 보존한다.

- `GridLayoutName`
- `gridLayoutName`
- `RigLayoutName`
- `rigLayoutName`

`GridLayoutName` 계열이 존재하면 이를 우선한다.

### 7.3 Exact visual resolver

`FarmingGuideStorageVisualLayoutResolver` 입력:

- current GameItem
- current live `StorageGrids`
- optional `StorageLayoutName`
- product-owned exact layout catalog

Exact metadata는 grid index별 expected width/height와 relative X/Y를 가진다.

적용 조건:

```text
exact metadata found
AND exact grid count == current grid count
AND every exact grid width/height == current width/height
→ exact relative placement
```

하나라도 불일치하면 stale metadata로 판단하고 exact path를 사용하지 않는다.

### 7.4 Compact fallback

exact metadata가 없거나 signature가 다르면 finite deterministic compact packing을 사용한다.

Fallback 계약:

- 무한 수평 나열 금지
- current grid 크기 유지
- `GridIndex` identity 유지
- 실제 drop target 유지
- authentic Tarkov layout으로 주장하지 않음

현재 exact catalog는 provenance와 좌표 의미를 검증한 최소 범위만 product-owned metadata로 유지한다. 외부 atlas를 license/provenance 확인 없이 대량 복제하지 않는다.

## 8. Storage drag / placement pipeline

```text
search result / existing placement
→ DragSession
→ optional R rotation
→ root-coordinate pointer
→ visible geometry target probe
→ GridDropTarget
→ current grid filter/bounds/overlap/ancestry evaluation
→ valid/invalid feedback
→ mouse-up actual-coordinate reprobe
→ accepted mutation or fail closed
```

핵심:

- footprint는 canonical width×height를 사용한다.
- rotation은 orientation만 바꾼다.
- contiguous placement가 필요하다.
- offscreen/clipped target은 후보가 아니다.
- mouse capture에 의한 hit-test 왜곡을 고려한다.
- release 시 cached target이 아니라 실제 release point를 다시 probe한다.

Exact visual coordinates는 drop target의 위치만 결정하고 legality 규칙을 바꾸지 않는다.

## 9. Nested container contract

Stored container는 descendant placements를 간접 소유한다.

- child는 accepted parent instance가 존재할 때만 유효하다.
- parent가 실제 current storage grid를 가져야 한다.
- self/descendant cycle을 허용하지 않는다.
- parent 이동 시 동일 `InstanceId`를 유지해 descendants ownership을 보존한다.
- parent destructive remove 시 subtree를 함께 제거한다.
- populated top-level carrier를 arbitrary nested placement로 변환해 ownership을 모호하게 만들지 않는다.
- silent item loss를 허용하지 않는다.

## 10. Recursive assembly architecture

`FarmingGuideAssemblyPolicy`가 assembly tree의 deterministic Core authority다.

책임:

- `GetNode(root, ownerPath)`
- deep `SetAttachment`
- deep `SetArmorPlate`
- `CanAttach`
- `CanInstallArmorPlate`
- compatible-item enumeration
- recursive persisted-state `Sanitize`
- full-tree enumeration
- deterministic `AssemblySignature`
- recursive required-slot detection
- maximum depth guard

`ownerPath`는 attachment slot id sequence다.

```text
[]
→ root item

[slotA]
→ root.slotA child

[slotA, slotB]
→ root.slotA.slotB child
```

Armor plate child는 상태 tree에는 존재하지만 navigation owner path는 attachment hierarchy를 기준으로 한다.

## 11. Assembly compatibility contract

Attachment candidate는 최소 다음을 만족해야 한다.

- owner node/current item 존재
- current slot filter 허용
- owner item과 conflict 없음
- 현재 assembly의 다른 설치 item과 conflict 없음
- draggable canonical inventory item

Armor plate candidate:

- slot unlocked
- allowed plate ID에 포함
- owner/current assembly conflict 없음

Persisted child가 current data에서 더 이상 유효하지 않으면 sanitize가 제거한다. 오래된 preset이 current game truth보다 우선하지 않는다.

## 12. Recursive in-page workbench

Double-click/interaction은 generic information Window가 아니다.

```text
stored bag/rig
→ actual storage surface

weapon / helmet / body armor / worn rig
→ actionable slots workbench

installed attachment with child slots
→ double-click child
→ child workbench level
```

Workbench는 가운데 Farming Guide column 안에서 동작하며 오른쪽 search는 계속 사용할 수 있다.

v1.14.0 slots UX:

- occupied child는 actual icon/assembly visual로 표시
- attachment child에 하위 slots가 있으면 double-click으로 진입
- `← 상위 부품`으로 복귀
- empty slot single-click → inline compatible-item picker
- picker candidate card single-click → install
- search drag → slot drop도 유지
- 둘 다 Core compatibility policy를 공유
- occupied slot silent overwrite 금지

## 13. Assembly-aware item image contract

### 13.1 Authoritative composed preset

Root item이 `DefaultPresetItemId`를 가리키고, 해당 preset source의 `ContainedItemIds`와 current assembly membership이 정확히 일치하며 usable preset image가 있으면 composed preset image를 사용한다.

### 13.2 Arbitrary build fallback

정확한 preset match가 아니면:

```text
base item image
+ installed descendant part indicators
```

를 사용한다.

이 fallback은 assembly state 변화를 사용자에게 보여주기 위한 deterministic UI이며 Tarkov client의 완전한 weapon rendering을 재현한다고 주장하지 않는다.

## 14. Search normalization

Canonical catalog는 upstream identity를 보존한다.

Farming Guide draggable search에서만 다음 assembled preset records를 제외한다.

- `PropertiesType == ItemPropertiesPreset`
- `Types`에 `preset` semantic 포함

Base weapon은 유지하며 actual attachment slots를 사용한다. 실제 다른 item variant를 이름이 같다는 이유로 병합하지 않는다.

## 15. Persistence sanitization

Load 시 current truth로 다음을 재검증한다.

Stored placements:

- item existence
- duplicate instance IDs
- parent existence
- cycle
- current grid existence
- filter
- bounds
- overlap
- pocket profile geometry

Assembly tree:

- child item existence
- current slot existence
- current filter/allowed IDs
- conflicts
- recursion depth

불가능한 부분은 fail closed한다.

기존 schema-v1 JSON의 `ParentInstanceId` 누락은 null root placement로 해석한다.

## 16. Schema compatibility

```text
Game Content write: v10
Game Content read:  v3, v4, v5, v6, v7, v8, v9, v10
Farming Guide user state: v1
```

v10은 assembly source와 storage layout identity를 보존하기 위한 Content schema 확장이다. Old readable snapshots에 없는 structure를 fabricate하지 않는다.

## 17. Summary semantics

총 무게는 equipment, carriers, stored items와 recursive attachment/armor descendants를 합산한다.

Storage summary:

- total cells: current top-level grids + accepted nested container grids
- used cells: accepted stored placement footprints

Summary는 informational capacity다. 실제 item 수납 가능 여부는 contiguous footprint와 current filter가 결정한다.

## 18. Lifecycle safety

Workbench owner가 이동/제거되기 시작하면 열린 workbench를 닫아 stale `_workbenchApply`가 과거 owner 위치에 write-back하지 못하게 한다.

Data/profile context 교체 시 workbench state를 닫고 current item catalog/state로 다시 렌더링한다.

Async image loading은 Dispatcher shutdown 상태를 확인하고 종료 중 UI write를 피한다.

## 19. 검증 계약

Deterministic tests:

- equipment/carrier compatibility
- secure container vs ordinary case
- profile pockets
- nested placement/sanitize/cycle/overlap
- preset persistence
- search preset exclusion
- recursive assembly mutation/sanitize/conflict/required/signature
- importer layout identity
- storage visual resolver exact/mismatch/fallback
- desktop source/lifecycle contracts

Published EXE smoke:

- page 실제 render
- nested storage interactive drop
- attachment slot drag/drop/drag-out
- occupied-slot overwrite block
- exact multi-grid Canvas placement
- exact layout의 `GridDropTarget.GridIndex` identity
- 전체 Product UI/Map smoke와 정상 종료

Release gate:

- Windows Release build
- deterministic tests
- self-contained win-x64 publish
- actual published EXE smoke
- package/checksum verification
- Shutdown Race
- Documentation Consistency
- exact-main validation
- public tag/release/assets checksum verification

## 20. v1.14.0 release-prep evidence

Release identity bump 직전 PR exact head:

```text
7b9a96ccdff0ff1e0ddfb6f676624d24b150b7a1
527 passed / 0 failed / 0 skipped
Windows Release build: SUCCESS
self-contained win-x64 publish: SUCCESS
published EXE Product UI/Farming Guide/Map smoke: SUCCESS
graceful shutdown: SUCCESS
Shutdown Race: SUCCESS
Documentation Consistency: SUCCESS
```

이는 branch validation evidence다. Public v1.14.0 source/tag/assets는 merge/release 완료 후 `docs/PROJECT_STATE.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`, release evidence 문서에 기록한다.
