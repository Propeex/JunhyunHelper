# ARCHITECTURE — Farming Guide

기준일: **2026-08-31 KST**  
대상 제품: **v1.13.2+**

이 문서는 `파밍 가이드` subsystem의 책임, 데이터 흐름, persistence, Tarkov 변화 대응, 유지보수 경계를 정의한다. 제품 의미는 `docs/PRODUCT.md`와 `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`, 현재 사실값은 `docs/PROJECT_STATE.json`, 실제 구현/검증 상태는 `docs/STATE.md`가 우선한다.

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
  ├─ profile context binding
  ├─ internal structure / preset dialogs
  └─ section lifecycle integration / runtime smoke hooks

JunhyunHelper.Core
  ├─ deterministic editor state
  ├─ placement / packing rules
  ├─ equipment/carrier compatibility
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

Program Update와 Game Content Update는 이 사용자 상태를 덮어쓰지 않는다.

## 4. Editor state model

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

v1.13.2 compatibility contract:

- pistol / revolver / handgun 계열은 Holster target이다.
- pistol 계열을 PrimaryWeapon1/2 generic weapon으로 받아들이지 않는다.
- body armor / rig / backpack / secure container는 current `propertiesType`과 canonical type/category 의미를 함께 사용해 판정한다.
- carrier는 equipment dictionary와 별도 aggregate지만 사용자 interaction에서는 동일 drag/drop loadout surface로 동작한다.

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

## 7. Presentation contract

- 장비와 수납 item은 실제 item icon을 주 presentation으로 사용한다.
- Rig / Backpack / Secure Container는 carrier icon target과 actual storage grids를 함께 표현한다.
- storage placement와 drag ghost도 같은 item image presentation을 공유한다.
- 회전된 비정사각형 image는 layout-aware transform을 사용해 rotated footprint와 일치시킨다.
- melee / PMC dogtag fixed lifecycle은 유지하지만 UI label에 `고정` 문구를 붙이지 않는다.
- preset-name dialog는 content-sized layout으로 DPI/theme 변화에서도 하단 control을 clip하지 않는다.

## 8. Drag / placement 파이프라인

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
   └─ current filter/compatibility
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

## 9. Carrier contents 안전 계약

Carrier는 내부 stored placement를 소유하는 aggregate다.

- populated carrier의 destructive replacement를 묵시적으로 허용하지 않는다.
- 안전한 contents 이동/재배치를 증명하지 못하면 fail closed한다.
- UI 편의 때문에 silent item loss를 허용하지 않는다.

향후 populated carrier 교체 UX를 확장하려면 contents migration/overflow/conflict 의미를 먼저 제품적으로 확정해야 한다.

## 10. Persisted state sanitization

저장 당시 정상인 preset이 Tarkov data 또는 profile capability 변경으로 불가능해질 수 있다.

예:

- carrier grid 삭제/변경
- grid 크기 축소
- item dimension 변경
- filter 변경
- overlap 발생
- profile/edition pocket geometry 변경

Load 시 current Game Content와 current resolved pocket geometry를 authority로 사용한다.

```text
persisted placement
→ current item/grid/profile geometry existence
→ current bounds
→ current overlap
→ current filter
→ valid: restore
→ invalid/unknown: do not restore impossible placement
```

과거 JSON이 current Tarkov/profile truth보다 우선하지 않는다.

## 11. Attachment / armor / internal structure inspect

Attachment와 교체형 armor plate는 장착 item의 nested configuration이다.

- current item slot structure를 사용한다.
- allow/block/conflict 의미는 current validated content 기준으로 검증한다.
- preset round-trip 시 nested configuration을 보존한다.
- unknown relationship을 임의 호환으로 처리하지 않는다.

Double-click inspect contract:

- equipped editable item: 기존 attachment/replaceable armor editing 유지
- locked armor slot: read-only structure 표시
- rig/backpack/secure container: actual storage grid width/height 표시
- search result: 동일 internal structure를 read-only로 확인 가능

공통 window는 inspect와 edit presentation을 공유하지만 read-only mode가 user state를 변경하지 않는다.

## 12. Content schema compatibility

```text
Content write schema: v9
Readable schemas: v3, v4, v5, v6, v7, v8, v9
```

Old readable snapshot에는 Farming Guide 구조가 없을 수 있다. 없는 구조를 fabricate하지 않는다.

v1.13.0 → v1.13.1 → v1.13.2에는 Game Content schema 변경이 없다.

## 13. Lifecycle / MainWindow integration

Farming Guide는 MainWindow의 first-class section이다.

MainWindow lifecycle에서 다음을 기존 section들과 일관되게 처리한다.

- profile availability
- section visibility
- busy state
- navigation/button state
- active profile context for pocket geometry
- shutdown/disposal

Farming Guide 추가로 Scanner/Map/Quest 등 unrelated subsystem의 initialization ordering을 새 implicit dependency로 만들지 않는다.

## 14. Runtime 검증 계약

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

v1.13.2 exact product source `207cb948affc091c4ad67f18d7e4e4382b2f8125`은 이 gate를 통과했다. 공개 release evidence는 `docs/RELEASE_1.13.2.md`와 `docs/.release-v1.13.2-status.json`에 기록한다.

## 15. Security / Tarkov interaction boundary

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

## 16. 유지보수 규칙

Tarkov 변화 시 우선순위:

1. live source structure 변화 여부 확인
2. importer/canonical model이 새 의미를 이해하는지 확인
3. Content v9 validation 및 old readable compatibility 확인
4. active profile edition/quest facts로 pocket geometry resolve
5. existing preset을 current content/profile geometry로 sanitize
6. deterministic regression 추가
7. published EXE smoke

추측성 대규모 editor rewrite보다 실제 source drift/실사용 회귀에 필요한 범위만 수정한다.

향후 loot recommendation/value engine이 추가될 경우 editor의 canonical state를 입력으로 소비하고 editor state 자체에 recommendation-derived truth를 섞지 않는다.
