# DECISION — v1.13.3 Farming Guide Live Item Interaction

Status: **CONFIRMED / IMPLEMENTATION IN VALIDATION**  
Date: **2026-08-31 KST**

## Context

v1.13.2 실사용에서 Farming Guide의 장비/수납 편집이 실제 Tarkov inventory interaction과 다르게 동작하는 문제가 확인됐다.

기존 구현은 아이템 더블클릭을 일반적인 `장비 정보/장비 설정` Window로 해석했다. 해당 Window는 storage grid를 읽기 전용 미리보기로 보여주고 attachment/armor plate를 ComboBox로 선택했다. 이 설계는 사용자가 요구한 raid-start inventory editor의 의미와 맞지 않는다.

동시에 다음 data/model 문제가 확인됐다.

- nested bag/rig placement가 부모 container instance를 저장하지 않아 `가방 안 가방 안 아이템` 상태를 표현할 수 없었다.
- current Tarkov secure container는 `ItemPropertiesContainer`로 제공되지만 v1.13.2 compatibility가 이를 인정하지 않았다.
- upstream item collection에는 `ItemPropertiesPreset` / `preset` weapon preset records가 실제 base weapon과 함께 존재한다. 이를 모두 draggable item으로 노출해 같은 총기가 다수 보였고, preset record에는 base weapon의 실제 mod slot 구조가 없을 수 있다.

## Decision

### 1. Double-click은 정보 조회가 아니라 실제 내부 작업면이다

별도 OS Window 기반 generic item-information/configuration UI를 사용하지 않는다.

Farming Guide 가운데 영역에 in-page workbench를 열고, 오른쪽 item search는 계속 사용할 수 있게 유지한다.

아이템 유형/현재 위치에 따라 필요한 실제 조작면만 표시한다.

- stored backpack / stored rig: 실제 내부 storage grid
- worn/top-level rig: main inventory에 이미 storage grid가 있으므로 actionable armor/mod slots만
- weapon: actual attachment/mod slots
- helmet/body armor: actionable attachment / replaceable armor plate slots
- backpack/secure container: actual storage grids

설명용 grid metadata, read-only preview, attachment/plate ComboBox는 제품 interaction으로 사용하지 않는다.

### 2. Slot은 one-item drag/drop contract다

Attachment/armor plate slot은 실제 하나의 drop target으로 표현한다.

- compatible item만 drop 가능
- current Tarkov filter / allowed plate IDs / conflicts를 검증
- occupied slot을 묵시적으로 overwrite하지 않는다
- 기존 부품을 먼저 drag-out한 뒤 새 부품을 넣는다

이 계약은 아이템 유실을 막고 Tarkov의 한 슬롯 한 아이템 의미를 보존한다.

### 3. Nested storage는 parent instance를 명시적으로 저장한다

`FarmingGuideStoredItemState`에 nullable `ParentInstanceId`를 추가한다.

- `null`: pockets/rig/backpack/secure/special 같은 top-level storage surface
- non-null: 특정 stored container instance 내부 grid

기존 schema-v1 JSON에는 해당 필드가 없으므로 null로 deserialize되어 기존 top-level state와 backward compatible하다. 이 변경만으로 schema number를 올리거나 기존 저장 데이터를 폐기하지 않는다.

Sanitize/load는 root placement를 먼저 검증하고 nested tree를 parent가 증명된 경우에만 이어서 수용한다. orphan, duplicate instance, self-parent, unresolved cycle, invalid grid/filter/bounds/overlap은 fail closed한다.

Nested container 자체를 이동하면 descendants는 같은 parent chain을 유지한다. container를 삭제하거나 carrier를 destructive replace하면 descendants도 함께 제거해 orphan을 만들지 않는다. 자신 또는 자신의 descendant 안으로 이동하는 cycle은 금지한다.

### 4. Secure Container current-data contract

`FarmingGuideStorageKind.SecureContainer`는 current Tarkov `ItemPropertiesContainer`를 정상 carrier로 인정한다. 기존 category/type fallback도 compatibility 용도로 유지한다.

### 5. Weapon preset은 Farming Guide 검색 item이 아니다

Canonical Game Content importer는 upstream item IDs를 그대로 보존한다. Farming Guide 중복 문제를 해결하기 위해 canonical catalog를 손상시키거나 ID를 합치지 않는다.

대신 Farming Guide item-search policy에서 다음을 draggable inventory item에서 제외한다.

- `PropertiesType == ItemPropertiesPreset`
- `Types`에 `preset` 포함

실제 base weapon/item은 그대로 노출한다. 따라서 Glock 같은 base weapon의 실제 `slots`가 workbench에 사용되고 assembled preset recipe/config records가 동일 총기처럼 반복 노출되지 않는다.

## Persistence / summary

Nested storage 관계는 preset 및 working state round-trip에 보존한다.

Storage summary의 total capacity는 top-level carrier grids뿐 아니라 현재 stored containers가 제공하는 nested grids까지 포함한다.

## Supersession

이 결정은 `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`의 raid-start loadout/inventory editor 방향을 유지하면서, v1.13.0~v1.13.2의 generic configuration Window 구현을 **명시적으로 폐기/supersede**한다.

## Validation gate

완료 선언 전 다음을 확인한다.

- deterministic tests: secure container compatibility, nested sanitize, nested persistence, preset filtering
- Desktop source contract: in-page workbench + obsolete configuration Window absence
- Windows Release build
- published EXE startup/product UI/graceful shutdown smoke
- PR CI green
- main exact-head verification
- v1.13.3 public tag/release/assets verification
