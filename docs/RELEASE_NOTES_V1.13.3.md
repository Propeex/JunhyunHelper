# 준현 헬퍼 v1.13.3 Release Notes

기준일: **2026-08-31 KST**

## Farming Guide — 인게임식 장비/수납 상호작용 회귀 수정

v1.13.2 실사용 검증에서 확인된 Farming Guide 장비·수납 편집 문제를 수정한다.

### 보안 컨테이너

- Epsilon/Gamma/Kappa 등 실제 Secure Container를 `컨테이너` carrier 슬롯에 장착할 수 있도록 current Tarkov item 분류 호환성을 보강했다.
- current source에서 Secure Container와 Medicine Case 같은 일반 case가 모두 `ItemPropertiesContainer`를 사용할 수 있으므로 generic container classification은 Secure Container로 오인하지 않는다.
- explicit `Secure containers` / pouch semantics를 우선하고 current secure-container fallback은 일반 `container/case` 분류가 없는 경우로 제한한다.

### 실제 내부 수납

- `FarmingGuideStoredItemState`에 nullable `ParentInstanceId`를 추가해 가방 안 가방, 가방 안 리그 등 nested storage를 실제 상태로 표현한다.
- stored bag/rig를 더블클릭하면 읽기 전용 미리보기가 아니라 실제 빈 grid 작업면이 열리며 오른쪽 검색 결과의 아이템을 직접 drag/drop해 수납할 수 있다.
- nested placement는 working state와 preset round-trip에 보존한다.
- orphan parent, duplicate instance, self/cyclic nesting, invalid grid/filter/bounds/overlap은 load 시 fail closed한다.
- container 이동/삭제 시 descendants를 aggregate로 보존하거나 함께 제거해 고아 아이템과 silent loss를 방지한다.

### 총기/헬멧/방어구 내부 슬롯

- 기존 `장비 정보/장비 설정` 별도 WPF Window를 제거했다.
- 더블클릭은 가운데 in-page workbench에서 해당 아이템에 실제 필요한 조작면만 연다.
- weapon은 actual attachment/mod slots, helmet/body armor는 actionable attachment/replaceable armor plate slots를 실제 drop target으로 표시한다.
- worn/top-level rig는 main inventory에 수납 grid가 이미 표시되므로 plate/mod slots만 연다.
- attachment/armor slot은 한 슬롯 한 아이템 계약을 사용하며 occupied slot을 묵시적으로 overwrite하지 않는다.

### 동일 총기 반복 검색

- upstream item feed에 base weapon 외에 `ItemPropertiesPreset` / `preset` assembled weapon records가 함께 존재하는 원인을 확인했다.
- canonical Game Content를 합치거나 삭제하지 않고 Farming Guide 검색에서 preset record만 제외한다.
- 실제 base weapon/item은 그대로 노출하므로 Glock 등 총기의 canonical mod slots를 사용한다.

## Compatibility

- 기존 `farming-guide.json` schema v1을 유지한다. 과거 저장 데이터의 `ParentInstanceId`는 missing → null root placement로 호환된다.
- Content write schema v9 / readable v3~v9는 변경하지 않는다.
- v1.13.2의 profile-aware pockets, carrier compatibility, preset delete/fixed melee-dogtag lifecycle 및 기존 주요 기능을 보존한다.

## Validation

완료 전 gate:

- deterministic unit/contract tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE startup + Product UI + Map + graceful shutdown smoke
- Shutdown Race CI
- Documentation Consistency
- exact-main CI
- public v1.13.3 tag/release/assets readback

상세 제품 결정: `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
