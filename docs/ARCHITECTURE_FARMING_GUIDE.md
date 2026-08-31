# ARCHITECTURE — Farming Guide

기준일: **2026-08-31 KST**  
대상 제품: **v1.13.3+**

이 문서는 `파밍 가이드` subsystem의 책임, 데이터 흐름, persistence, Tarkov 변화 대응, 유지보수 경계를 정의한다. 제품 의미는 `docs/PRODUCT.md`, `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`, `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`, 현재 사실값은 `docs/PROJECT_STATE.json`, 실제 구현/검증 상태는 `docs/STATE.md`가 우선한다.

## 1. 목적과 비목표

Farming Guide는 **raid-start Loadout / Inventory Editor**다.

목적:

- 사용자가 레이드 시작 시점의 착용 장비와 수납 상태를 구성할 수 있게 한다.
- 제품이 current loadout, occupied space, stored items, available carrier structure를 deterministic state로 이해하게 한다.
- Tarkov item이 실제로 가진 storage / attachment / armor slot을 정보 표가 아니라 조작 가능한 inventory surface로 제공한다.
- 향후 loot 판단/recommendation이 필요할 경우 사용할 수 있는 신뢰 가능한 입력 기반을 만든다.

현재 비목표:

- 실제 Tarkov inventory grid 좌표의 지속적 1:1 실시간 mirror
- loot 가치 판단
- pickup / discard / replace 추천
- Scanner 실시간 추천 연동
- game memory / packet / process 내부 상태를 읽는 자동 동기화

새 recommendation/value semantics는 별도 제품 결정 없이 이 editor 내부에 암묵적으로 추가하지 않는다.

## 2. 시스템 경계

```text
JunhyunHelper.Desktop
  ├─ Farming Guide page / interaction / drag-drop presentation
  ├─ item icon presentation/cache binding
  ├─ geometry-backed drop-target probing
  ├─ profile context binding
  ├─ in-page item workbench
  ├─ preset dialog
  └─ section lifecycle integration / runtime smoke hooks

JunhyunHelper.Core
  ├─ deterministic editor state
  ├─ nested storage parent-instance model
  ├─ placement / packing rules
  ├─ equipment/carrier compatibility
  ├─ Farming Guide search eligibility
  ├─ pocket geometry policy
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ validated Tarkov item-structure import
  ├─ Content v9 persistence/read compatibility
  └─ Farming Guide JSON persistence boundary
```

WPF event handler가 grid legality, Tarkov compatibility 또는 persistence truth를 별도 구현하지 않는다. Presentation은 canonical item structure와 deterministic policy 결과를 소비한다.

## 3. 데이터 authority

Farming Guide는 Game Content와 사용자 상태를 분리한다.

### 3.1 Game Content

Authority:

```text
current validated Tarkov item source
→ importer
→ canonical item structure
→ Content snapshot v9
```

사용 구조:

- item width / height
- storage grids
- grid allowed/blocked filters
- equipment slots
- attachment slots
- armor plate slots
- item conflicts
- headphone blocking 등 editor compatibility에 필요한 optional structure

외부 source field가 없거나 importer가 의미를 증명할 수 없으면 값을 추측하지 않는다.

Upstream assembled weapon preset은 canonical Game Content에는 그대로 보존한다. 다만 Farming Guide는 실제 raid-start inventory item을 편집하므로 `ItemPropertiesPreset` / `preset` records를 draggable search surface에서 제외한다.

### 3.2 사용자 상태

Authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

포함:

- current working state
- saved presets
- selected preset identity
- fixed melee / PMC dogtag setting
- equipped items
- attachments / armor plates
- carrier assignments
- stored item placement / grid / rotation
- nested stored item parent instance

Program Update와 Game Content Update는 이 사용자 상태를 덮어쓰지 않는다.

## 4. Editor state model

```text
profile
├─ working raid-start state
│  ├─ equipment slots
│  ├─ carrier slots
│  └─ stored placements
│     ├─ root placement: ParentInstanceId = null
│     └─ nested placement: ParentInstanceId = stored container instance id
├─ presets
└─ selected preset identity

user-level fixed settings
├─ melee
└─ PMC dogtag
```

`FarmingGuideStoredItemState.InstanceId`는 placement identity다. `ParentInstanceId`는 어떤 stored container의 내부 grid가 해당 placement를 소유하는지를 표현한다.

- root placement는 기존 `StorageKind + GridIndex` surface에 속한다.
- nested placement는 `ParentInstanceId + GridIndex` surface에 속한다.
- nested child의 `StorageKind`는 ancestry 복구용 authority가 아니다. 실제 parent instance가 가진 current item layout이 authority다.

Preset을 불러온 뒤 state를 수정하면 selected preset identity를 해제한다. Fixed melee/dogtag는 raid preset과 lifecycle이 다르므로 분리한다.

Preset 삭제는 saved preset entry만 제거하며 current working snapshot을 버리지 않는다. 삭제 대상이 selected preset이면 selection만 null로 전환한다.

## 5. Equipment / carrier compatibility

Equipment는 current product design의 Tarkov 장착 위치를 spatial slot board로 표현한다.

대표 슬롯:

- headset
- helmet / headwear
- face cover / eyewear
- body armor
- armored rig
- armband
- primary weapon 1 / 2
- Holster
- melee
- PMC dogtag fixed setting

유지 계약:

- pistol / revolver / handgun 계열은 Holster target이다.
- pistol 계열을 PrimaryWeapon1/2 generic weapon으로 받아들이지 않는다.
- body armor / rig / backpack은 current `propertiesType`과 canonical type/category 의미를 함께 사용해 판정한다.
- carrier는 equipment dictionary와 별도 aggregate지만 사용자 interaction에서는 동일 drag/drop loadout surface로 동작한다.

Secure Container는 일반 container/case와 구분한다.

```text
explicit secure-container / pouch semantics
→ accept

else if PropertiesType == ItemPropertiesContainer
  and generic container/case classification is absent
→ narrow current-data compatibility fallback

ordinary container/case
→ reject as PMC Secure Container carrier
```

current source에서 Epsilon과 Medicine Case가 모두 `ItemPropertiesContainer`일 수 있으므로 property type 하나만으로 장착을 허용하지 않는다.

Storage/carrier canonical presentation:

```text
Rig
Pockets (left) + Special Slots (right)
Backpack
Secure Container
```

Rig / Backpack / Secure Container 내부 grid는 current validated item structure에서 생성한다. Special Slots는 product-owned three-slot fixed structure다.

## 6. Profile-aware pockets

Pockets는 item carrier가 아니라 active profile capability에 따른 product-owned geometry다.

```text
standard:
1×1 / 1×1 / 1×1 / 1×1

expanded:
1×1 / 1×2 / 1×2 / 1×1
```

Resolve input:

- active profile edition
- current product가 authoritative user progress에서 증명할 수 있는 Old Patterns 완료 상태

Resolve output은 단일 `FarmingGuideStorageGridDefinition` 목록이다.

동일 resolved geometry를 다음이 함께 사용한다.

- WPF storage rendering
- drag placement legality
- occupied/available storage computation
- persisted-state sanitization

UI와 load-time validation이 서로 다른 pocket layout을 재구현하지 않는다.

## 7. Presentation / double-click contract

- 장비와 수납 item은 실제 item icon을 주 presentation으로 사용한다.
- Rig / Backpack / Secure Container는 carrier icon target과 actual storage grids를 함께 표현한다.
- storage placement와 drag ghost도 같은 item image presentation을 공유한다.
- 회전된 비정사각형 image는 layout-aware transform을 사용해 rotated footprint와 일치시킨다.
- melee / PMC dogtag fixed lifecycle은 유지하지만 UI label에 `고정` 문구를 붙이지 않는다.
- preset-name dialog는 content-sized layout으로 DPI/theme 변화에서도 하단 control을 clip하지 않는다.

Double-click은 generic information dialog가 아니다.

```text
stored backpack / stored rig
→ in-page workbench / actual storage grids

worn top-level rig
→ actionable armor/mod slots

weapon
→ attachment/mod slots

helmet / body armor
→ actionable attachment / replaceable armor slots

backpack / secure container carrier
→ actual storage grids
```

v1.13.2의 `FarmingGuideItemConfigurationWindow` read-only preview / ComboBox UI는 폐기한다. Workbench는 가운데 Farming Guide column 안에서 열리며 오른쪽 item search는 계속 사용할 수 있다.

## 8. Drag / placement 파이프라인

```text
search result / existing item / workbench child
→ drag payload with canonical item state + footprint
→ optional R rotation for grid placements
→ root-coordinate pointer
→ geometry / visible-bounds drop target probe
→ candidate equipment/carrier/grid/workbench slot
→ placement legality evaluation
   ├─ target exists
   ├─ bounds
   ├─ overlap
   ├─ contiguous footprint
   ├─ current filter/compatibility
   ├─ nested ancestry/cycle
   └─ slot occupied/conflict rules
→ valid/invalid transient presentation
→ mouse-up actual-coordinate reprobe
→ accepted state mutation or fail closed
```

핵심 계약:

- item footprint는 실제 Tarkov `width × height`를 사용한다.
- rotation은 orientation만 바꾸며 canonical base dimension을 손상시키지 않는다.
- snap tolerance는 UX 보조이며 불법 배치를 합법으로 바꾸지 않는다.
- storage cell 총량은 참고 요약값이며 실제 수납 가능 여부는 contiguous placement와 filter로 판단한다.
- WPF mouse capture 때문에 `InputHitTest`가 drag source를 반환할 수 있으므로 capture-sensitive hit test만 신뢰하지 않는다.
- geometry fallback은 target과 ScrollViewer / ScrollContentPresenter / clipping ancestor의 visible bounds를 확인한다.
- offscreen/clipped target은 drop candidate가 아니다.
- mouse-up에서는 cached last-move target 대신 release point를 다시 probe한다.
- transient success/danger border는 target 변경과 drag 종료에서 원복한다.

## 9. Nested carrier contents 안전 계약

Stored container는 자신의 descendant placement를 간접 소유하는 aggregate다.

- child placement는 parent container instance가 실제로 존재하고 current storage grids를 제공할 때만 유효하다.
- parent를 자기 자신 또는 자기 descendant 내부로 이동할 수 없다.
- parent container를 다른 grid로 이동할 때 동일 `InstanceId`를 유지해 descendants의 parent relation을 함께 보존한다.
- parent를 destructive remove하면 전체 descendant subtree를 함께 제거한다.
- populated top-level carrier의 destructive replacement를 묵시적으로 허용하지 않는다.
- top-level populated carrier를 arbitrary stored grid item으로 이동해 contents ownership을 모호하게 만드는 동작은 fail closed한다.
- UI 편의 때문에 silent item loss / orphan placement를 허용하지 않는다.

향후 populated top-level carrier를 nested item으로 직접 전환하는 UX를 확장하려면 contents ownership migration 의미를 먼저 명시적으로 설계한다.

## 10. Persisted state sanitization

저장 당시 정상인 preset이 Tarkov data 또는 profile capability 변경으로 불가능해질 수 있다.

예:

- carrier grid 삭제/변경
- grid 크기 축소
- item dimension 변경
- filter 변경
- overlap 발생
- profile/edition pocket geometry 변경
- nested parent item 제거/구조 변경
- duplicate/cyclic instance relationship

Load 시 current Game Content와 current resolved pocket geometry를 authority로 사용한다.

```text
root placements
→ current item/grid/profile geometry/filter/bounds/overlap
→ valid roots accepted

nested placements
→ parent instance accepted?
→ parent current item has requested grid?
→ current filter/bounds/overlap valid?
→ ancestry cycle absent?
→ valid child accepted

unresolved remainder
→ drop fail closed
```

과거 JSON이 current Tarkov/profile truth보다 우선하지 않는다.

기존 schema-v1 JSON에는 `ParentInstanceId`가 없으므로 null root placement로 deserialize한다. 이 additive field만으로 user-state schema version을 올리지 않는다.

## 11. Attachment / armor workbench

Attachment와 교체형 armor plate는 장착 item의 nested configuration이다.

- current item slot structure를 사용한다.
- attachment filter / allowed plate IDs / conflict 의미는 current validated content 기준으로 검증한다.
- preset round-trip 시 nested configuration을 보존한다.
- unknown relationship을 임의 호환으로 처리하지 않는다.
- slot은 한 개의 item state만 가진다.
- occupied slot에 새 item을 drop해 기존 child를 묵시적으로 삭제하지 않는다.
- 기존 child를 먼저 drag-out한 뒤 새 child를 넣는다.

Workbench slot은 ComboBox option list가 아니라 실제 icon drop target이다. locked/non-actionable armor structure는 별도 설명용 UI로 반복 표시하지 않는다.

## 12. Search source normalization

Canonical item catalog는 upstream item identity를 보존한다. Farming Guide UI가 동일 이름을 이유로 catalog item을 합치거나 삭제하지 않는다.

다만 upstream에는 assembled weapon presets가 item collection에 함께 존재하므로 다음은 Farming Guide의 draggable search result에서 제외한다.

- `FarmingGuideData.PropertiesType == ItemPropertiesPreset`
- `Types`에 `preset` semantic 포함

이렇게 하면 실제 base weapon은 한 물리 아이템으로 노출되고 그 item의 actual attachment slots를 workbench가 소비한다. 실제로 서로 다른 Tarkov item variant는 그대로 유지한다.

## 13. Content / user-state schema compatibility

```text
Content write schema: v9
Readable schemas: v3, v4, v5, v6, v7, v8, v9
Farming Guide user state schema: v1
```

Old readable Content snapshot에는 Farming Guide 구조가 없을 수 있다. 없는 구조를 fabricate하지 않는다.

v1.13.0 → v1.13.1 → v1.13.2 → v1.13.3에는 mandatory Game Content schema migration이 없다. v1.13.3 nested parent field는 additive user-state compatibility다.

## 14. Summary semantics

총 무게는 equipment/carrier/fixed/stored item과 그 attachment/armor child tree를 합산한다.

Storage summary의 total cell은 현재 사용 가능한 top-level storage grids에 더해 실제 stored container instances가 제공하는 nested storage grids도 포함한다. used cell은 실제 stored placement footprint를 기준으로 계산한다.

이 요약값은 informational capacity summary이며 legality authority는 각 실제 grid placement engine이다.

## 15. Lifecycle / MainWindow integration

Farming Guide는 MainWindow의 first-class section이다.

MainWindow lifecycle에서 다음을 기존 section들과 일관되게 처리한다.

- profile availability
- section visibility
- busy state
- navigation/button state
- active profile context for pocket geometry
- shutdown/disposal

Preset 선택으로 working snapshot이 교체될 때 열린 workbench는 닫아 stale item callback이 새 state에 적용되지 않게 한다.

Farming Guide 추가로 Scanner/Map/Quest 등 unrelated subsystem의 initialization ordering을 새 implicit dependency로 만들지 않는다.

## 16. Runtime 검증 계약

사용자에게 보이는 WPF 기능이므로 source/XAML assertion만으로 완료 선언하지 않는다.

Release 후보에서 최소 다음을 검증한다.

- Windows Release build / XAML compile
- deterministic placement/preset/content tests
- Windows x64 self-contained publish
- actual published EXE launch
- Farming Guide section 실제 activation/render
- 기존 Product UI / Map / Scanner smoke 비회귀
- normal Main Window close
- active async shutdown-race smoke
- clean portable root/package audit

v1.13.3은 이 gate와 exact-main/release verification이 모두 끝난 뒤 public stable로 선언한다.

## 17. Security / Tarkov interaction boundary

Farming Guide는 사용자 입력과 validated external Game Content만으로 상태를 구성한다.

다음은 사용하지 않는다.

- game process memory read
- DLL/code injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

향후 Scanner와 연결하더라도 기존 Scanner의 external screen pixel/OCR 안전 계약을 유지해야 한다.

## 18. 유지보수 규칙

Tarkov 변화 시 우선순위:

1. live source structure 변화 여부 확인
2. importer/canonical model이 새 의미를 이해하는지 확인
3. Content v9 validation 및 old readable compatibility 확인
4. active profile edition/quest facts로 pocket geometry resolve
5. existing preset을 current content/profile geometry로 sanitize
6. secure-container와 generic container/case를 구분하는 current semantics 확인
7. weapon preset/search policy drift 확인
8. deterministic regression 추가
9. published EXE smoke

추측성 대규모 editor rewrite보다 실제 source drift/실사용 회귀에 필요한 범위만 수정한다.

향후 loot recommendation/value engine이 추가될 경우 editor의 canonical state를 입력으로 소비하고 editor state 자체에 recommendation-derived truth를 섞지 않는다.
