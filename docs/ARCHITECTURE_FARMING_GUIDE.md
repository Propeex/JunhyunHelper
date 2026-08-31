# ARCHITECTURE — Farming Guide

기준일: **2026-08-31 KST**  
대상 제품: **v1.13.2+**

이 문서는 `파밍 가이드` subsystem의 책임, 데이터 흐름, persistence, Tarkov 변화 대응, 유지보수 경계를 정의한다. 제품 의미는 `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`, 현재 사실값은 `docs/PROJECT_STATE.json`, 실제 구현/검증 상태는 `docs/STATE.md`가 우선한다.

## 1. 목적과 비목표

Farming Guide는 **raid-start Loadout / Inventory Editor**다.

목적:

- 사용자가 레이드 시작 시점의 착용 장비와 수납 상태를 구성할 수 있게 한다.
- 제품이 current loadout, occupied space, stored items, available carrier structure를 deterministic state로 이해하게 한다.
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
  ├─ profile-aware pocket presentation
  ├─ section lifecycle integration
  └─ configuration / inspection dialogs / runtime smoke hooks

JunhyunHelper.Application / Core
  ├─ deterministic editor state
  ├─ placement / packing / compatibility rules
  ├─ profile/edition-aware pocket geometry policy
  └─ preset/fixed-state semantics

JunhyunHelper.Infrastructure
  ├─ validated Tarkov item-structure import
  ├─ Content v9 persistence/read compatibility
  └─ Farming Guide JSON persistence boundary
```

WPF event handler가 grid legality, Tarkov compatibility 또는 persistence truth를 별도 구현하지 않는다. Presentation은 canonical item structure와 deterministic placement result를 소비한다.

## 3. 데이터 authority

Farming Guide는 두 종류의 데이터를 명확히 분리한다.

### 3.1 Game Content

Authority:

```text
current validated Tarkov item source
→ importer
→ canonical item structure
→ Content snapshot v9
```

Farming Guide가 사용하는 구조:

- item width / height
- storage grids
- grid allowed/blocked filters
- equipment slots
- attachment slots
- armor plate slots
- item conflicts
- current canonical type/category classification
- 현재 editor compatibility에 필요한 기타 optional structure

외부 source field가 없거나 importer가 의미를 증명할 수 없으면 값을 추측하지 않는다.

### 3.2 사용자 상태

Authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

포함:

- current working state
- saved presets
- per-profile preset selection/state
- fixed melee / PMC dogtag setting
- equipped items
- attachments / armor plates
- carrier assignments
- stored item placement and rotation

Program Update와 Game Content Update는 이 사용자 상태를 덮어쓰지 않는다.

### 3.3 Profile facts used by Farming Guide

Pocket geometry는 item Game Content가 아니라 **현재 profile의 raid-start capability**에 속한다.

Authority:

- active profile `EditionId`
- active profile `CompletedQuestIds`
- canonical edition rules in current Game Content

현재 policy:

```text
Old Patterns 완료
OR current edition이 Old Patterns 보상을 기본 보유
→ expanded pockets: 1×1 / 1×2 / 1×2 / 1×1

otherwise
→ standard pockets: 1×1 / 1×1 / 1×1 / 1×1
```

이 resolved geometry는 presentation과 persisted-placement sanitization에 같은 값으로 전달한다. UI와 저장 검증이 서로 다른 pocket truth를 만들지 않는다.

## 4. Editor state model

대표 상태 관계:

```text
profile
├─ working raid-start state
│  ├─ equipment slots
│  ├─ carrier slots
│  └─ stored placements
├─ presets
└─ selected preset identity

user-level fixed settings
├─ melee
└─ PMC dogtag
```

Preset을 불러온 뒤 사용자가 state를 수정하면 원본 preset 선택 상태를 해제한다. Fixed melee/dogtag는 raid preset과 의미가 다르므로 preset payload와 lifecycle을 분리한다.

Preset 삭제 계약:

- 선택한 preset identity만 제거한다.
- 삭제 시 현재 working raid-start state를 폐기하지 않는다.
- 삭제된 preset이 selected state였다면 selector는 미선택 상태로 돌아간다.
- 동일 이름 비교는 persistence boundary에서 case-insensitive로 처리한다.

## 5. Equipment / carrier presentation

Equipment는 current product design에 필요한 Tarkov 장착 위치를 spatial slot board로 표현한다.

예:

- headset
- helmet / headwear
- face cover / eyewear
- armor / armored rig
- armband
- primary/secondary weapon
- sidearm / Holster
- melee

장착 의미:

- pistol / revolver / handgun 계열은 Holster target이다.
- pistol 계열을 PrimaryWeapon1/2의 generic weapon으로 받아들이지 않는다.
- body armor / rig / backpack / secure container는 current `propertiesType`과 canonical type/category 의미를 함께 사용해 판정한다.
- carrier는 equipment dictionary와 별도 aggregate지만 사용자 interaction에서는 동일 drag/drop loadout surface로 동작한다.

Storage/carrier 영역의 canonical presentation 순서:

```text
Rig
Pockets (left) + Special Slots (right)
Backpack
Secure Container
```

Carrier 내부 grid는 하드코딩된 시각 템플릿이 아니라 current validated item structure에서 생성한다. Pockets만 profile-aware fixed geometry policy를 사용하고 Special Slots는 product-owned fixed three-slot structure를 유지한다.

Presentation contract:

- 장비와 수납 item은 text-only row가 아니라 실제 item icon을 주 presentation으로 사용한다.
- Rig / Backpack / Secure Container는 carrier icon target과 해당 item의 actual storage grids를 함께 표현한다.
- storage placement와 drag ghost도 같은 item image presentation을 공유한다.
- 회전된 비정사각형 image는 WPF layout measure/arrange가 rotated footprint를 반영하도록 layout-aware transform을 사용한다.
- icon loading/cache는 presentation concern이며 Item ID / compatibility / placement truth를 변경하지 않는다.
- melee / PMC dogtag는 fixed lifecycle을 유지하지만 UI label에 별도 `고정` 문구를 붙이지 않는다.
- preset-name dialog는 fixed client height에 의존하지 않고 content-sized layout으로 DPI/theme 변화에서도 하단 control을 clip하지 않는다.

## 6. Drag / placement 파이프라인

```text
search result item
→ drag payload with canonical item id + footprint
→ optional R rotation
→ root-coordinate pointer
→ geometry / visible-bounds drop target probe
→ candidate grid/snap target
→ placement legality evaluation
   ├─ target exists
   ├─ bounds
   ├─ overlap
   ├─ contiguous footprint
   └─ current grid filter/compatibility
→ valid/invalid transient presentation
→ mouse-up actual-coordinate reprobe
→ accepted state mutation or fail closed
```

핵심 계약:

- item footprint는 실제 Tarkov `width × height`를 사용한다.
- rotation은 footprint orientation만 바꾸며 canonical base dimension을 손상시키지 않는다.
- snap tolerance는 UX 보조이며 불법 배치를 합법으로 바꾸는 규칙이 아니다.
- storage cell 총량은 참고 요약값이다. 실제 item 수납 가능 여부는 contiguous placement와 filter 검증으로 판단한다.
- WPF mouse capture로 `InputHitTest`가 drag source를 반환할 수 있으므로 equipment/carrier hit 판정은 capture-sensitive hit test에만 의존하지 않는다.
- geometry fallback은 target과 ScrollViewer / ScrollContentPresenter / clipping ancestor의 visible bounds를 확인한다.
- offscreen/clipped target은 drop candidate가 아니다.
- grid 인접 snap은 target rectangle 밖의 bounded tolerance를 허용할 수 있지만 ancestor viewport를 벗어나지는 못한다.
- mouse-up에서 cached last-move target을 사용하지 않고 release point를 다시 probe한다.
- transient success/danger border는 target 변경과 drag 종료에서 원복한다.

## 7. Carrier contents 안전 계약

Carrier는 내부 stored placement를 소유하는 aggregate다.

따라서 contents가 있는 carrier를 다른 carrier로 단순 덮어쓰기하면 내부 state가 고아가 되거나 소실될 수 있다.

현재 계약:

- populated carrier의 destructive replacement를 묵시적으로 허용하지 않는다.
- 안전한 contents 이동/재배치를 증명하지 못하는 operation은 fail closed한다.
- UI 편의 때문에 silent item loss를 허용하지 않는다.

향후 populated carrier 교체 UX를 확장하려면 contents migration/overflow/conflict 의미를 먼저 제품적으로 확정해야 한다.

## 8. Persisted state sanitization

저장 당시 정상인 preset이 이후 Tarkov 데이터 변경 또는 profile capability 변화로 불가능해질 수 있다.

예:

- carrier grid 삭제/변경
- grid 크기 축소
- item dimension 변경
- filter 변경
- 여러 placement가 overlap 상태가 됨
- profile/edition pocket geometry가 변경됨

Load 시 current Game Content와 current resolved pocket geometry를 authority로 사용한다.

```text
persisted placement
→ current item/grid existence
→ current profile pocket geometry when Pockets
→ current bounds
→ current overlap
→ current filter
→ valid: restore
→ invalid/unknown: do not restore impossible placement
```

즉 과거 JSON이 current Tarkov/profile truth보다 우선하지 않는다. Invalid persisted state 때문에 editor 전체를 비정상 상태로 만들지 않는다.

## 9. Internal structure inspection / configuration

Double-click은 item 내부 구조를 확인하는 공통 interaction이다.

```text
equipped / carrier / stored item
→ editable structure가 있으면 configuration window
→ storage-only 또는 edit할 구조가 없으면 read-only inspection

search result
→ read-only inspection
```

표시 대상:

- actual storage grid width / height preview
- attachment slots
- armor plate slots
- locked/internal armor slots
- 현재 nested configuration

Attachment와 교체형 armor plate는 장착 item의 nested configuration이다.

- current item slot structure를 사용한다.
- slot allow/block/conflict 의미를 current validated content에 맞춰 검증한다.
- preset round-trip 시 nested configuration을 보존한다.
- unknown slot/item relationship을 임의 호환으로 처리하지 않는다.
- search result inspection은 read-only이며 working loadout을 수정하지 않는다.

## 10. Content schema compatibility

Farming Guide용 optional item structure를 보존하기 위해:

```text
Content write schema: v9
Readable schemas: v3, v4, v5, v6, v7, v8, v9
```

Old readable snapshot에는 Farming Guide용 구조가 없을 수 있다. 없는 구조를 fabricate하지 않는다. 일반 기존 기능이 읽을 수 있는 snapshot compatibility와 Farming Guide가 실제 editor structure를 제공할 수 있는지는 구분한다.

v1.13.0 → v1.13.1 → v1.13.2에는 Game Content schema 변경이 없다.

## 11. Lifecycle / MainWindow integration

Farming Guide는 Scanner에 종속된 숨은 panel이 아니라 MainWindow의 first-class section이다.

MainWindow lifecycle에서 다음을 기존 section들과 일관되게 처리한다.

- profile availability
- section visibility
- busy state
- navigation/button state
- active profile context for pocket geometry
- shutdown/disposal

Farming Guide 추가로 Scanner/Map/Quest 등 unrelated subsystem의 initialization ordering을 새 implicit dependency로 만들지 않는다.

## 12. Runtime 검증 계약

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

v1.13.1 exact product source `302f83e88cc65b5fae9b86b5cae294b2586c85a0`은 이 gate를 통과했다. v1.13.2의 최종 release evidence는 공개 완료 후 `docs/RELEASE_1.13.2.md`에 기록한다.

## 13. Security / Tarkov interaction boundary

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

## 14. 유지보수 규칙

Tarkov 변화 시 우선순위:

1. live source structure 변화 여부 확인
2. importer/canonical model이 새 의미를 이해하는지 확인
3. Content v9 validation 및 old readable compatibility 확인
4. active profile edition/quest facts로 pocket geometry를 resolve
5. existing Farming Guide preset을 current content/profile geometry로 sanitize
6. deterministic regression 추가
7. published EXE smoke

추측성 대규모 editor rewrite보다 실제 source drift/실사용 회귀에 필요한 범위만 수정한다.

향후 loot recommendation/value engine이 추가될 경우 editor의 canonical state를 입력으로 소비하도록 설계하고, editor state 자체에 recommendation-derived truth를 섞지 않는다.
