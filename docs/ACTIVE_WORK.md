# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

**v1.13.3 Farming Guide — 인게임식 장비/수납 상호작용 회귀 수정**

사용자의 v1.13.2 실사용 검증에서 확인된 장비/수납 편집 문제를 수정한다. 핵심 원칙은 더블클릭을 일반 정보 창으로 취급하지 않고, 실제 Tarkov처럼 해당 아이템이 가진 **실제 수납 공간·부착 슬롯·방탄판 슬롯을 직접 조작하는 작업면**으로 제공하는 것이다.

## Base / branch

```text
base main: 0aab5c29c675aff0300458a4ff8352c260519863
public stable: v1.13.2
working branch: fix/v1.13.3-farming-guide-interaction-2026-08-31
PR: #247 (draft)
```

## Confirmed scope

1. Secure Container(사망해도 내부 아이템이 보존되는 보안 컨테이너)를 파밍 가이드의 컨테이너 장비 슬롯에 정상 장착할 수 있어야 한다.
2. Glock 등 총기 더블클릭 시 실제 부착물/mod 슬롯을 표시하고 직접 드래그/드롭으로 장착·교체할 수 있어야 한다.
3. 가방 안 가방, 가방 안 리그처럼 수납공간을 가진 아이템을 더블클릭하면 내부 그리드가 실제 작업 가능한 빈 칸으로 열려야 하고, 아이템을 그 안으로 드래그해 넣고 다시 꺼낼 수 있어야 한다.
4. Altyn 같은 헬멧/방어구의 부착물·교체형 방탄판은 설명/선택 콤보가 아니라 실제 드롭 슬롯으로 표시한다.
5. 기존 `장비 정보/장비 설정` 별도 Window 및 읽기 전용 그리드 미리보기 중심 UX를 폐기하고, 아이템 유형/상황에 필요한 실제 내부 구조만 보여준다. 가방은 수납 그리드, 중첩 리그는 수납 그리드, 방탄 장비는 필요한 plate/attachment 슬롯, 총기는 mod 슬롯을 보여주는 식으로 동작한다.
6. 검색 결과에서 동일한 총기가 다수 노출되는 원인을 조사해, upstream weapon preset은 검색 source에서 제외하고 실제 base weapon/item만 드래그 가능한 목록으로 노출한다.

## Root cause confirmed

- `FarmingGuideStoredItemState`가 `FarmingGuideStorageKind`만 저장하고 부모 컨테이너 instance를 식별하지 않아 nested storage를 표현할 수 없었다.
- double-click이 `FarmingGuideItemConfigurationWindow`라는 별도 WPF `Window`를 열며 storage는 읽기 전용 미리보기, attachments/armor plates는 ComboBox 선택으로 구현되어 사용자 요구와 불일치했다.
- current Tarkov secure container(Epsilon 등)는 `propertiesType = ItemPropertiesContainer`로 제공되는데 v1.13.2 secure compatibility는 이 property type을 인정하지 않아 실제 데이터에서 장착이 실패할 수 있었다.
- current Tarkov item schema에는 `ItemPropertiesPreset` / `preset` 타입의 완성 weapon preset이 일반 item collection에 함께 존재한다. Farming Guide가 이를 실제 물리 아이템처럼 전부 검색 노출해 같은 총이 다수 보였고, preset에는 base weapon의 `slots`가 없어 Glock 같은 총의 attachment UI가 비어 보일 수 있었다.

## Completed

- Secure Container compatibility에 `ItemPropertiesContainer` 추가.
- `FarmingGuideStoredItemState.ParentInstanceId` nullable field 추가. 기존 schema-v1 JSON은 null root placement로 그대로 호환.
- loadout sanitize가 root → nested tree 순으로 parent 존재/그리드/filter/bounds/overlap을 검증하고 orphan/cycle/duplicate instance를 fail-closed 처리.
- nested container drag/drop surface에서 parent instance를 보존하며, container 자체 이동 시 descendants를 aggregate로 유지하고 self/descendant cycle을 차단.
- destructive delete/carrier replacement 시 nested subtree까지 함께 제거해 orphan 방지.
- 가운데 storage column에 in-page `WorkbenchHost` 추가. 오른쪽 검색 결과를 계속 사용할 수 있는 상태에서 열린 내부 공간으로 drag/drop 가능.
- stored backpack/rig 더블클릭 → 실제 내부 grid. top-level worn rig → 이미 main에 보이는 grid를 반복하지 않고 plate/mod slot만 표시.
- weapon/helmet/body armor equipment 더블클릭 → attachment/plate drop slot.
- attachment/plate slot의 직접 drag-in / replace / drag-out 지원.
- 기존 `FarmingGuideItemConfigurationWindow` 삭제.
- Farming Guide 검색에서 upstream `ItemPropertiesPreset` / `preset` 제외. base weapon/item은 유지.
- nested storage를 총 수납 칸 요약에도 반영.
- nested sanitize / secure property type / preset filter / nested persistence regression tests 추가.
- Draft PR #247 생성 및 CI 시작.
- Documentation Consistency 실패 원인이 필수 `## Completed` 섹션명 누락임을 확인하고 체크포인트 형식을 복구.

## Current step

- PR CI에서 desktop compile/test/published smoke 결과 확인 중.
- CI 결과에 따라 compile/runtime 계약 누락을 수정한 뒤 문서/버전/release 준비를 진행한다.

## Remaining

- CI compile/test failure가 있으면 수정
- Farming Guide desktop source-contract regression test 보강
- product/decision/architecture/reference/state docs 갱신
- v1.13.3 version identity 갱신
- Release build / published EXE product UI smoke 검증
- PR ready / CI green / main merge / exact-main validation
- v1.13.3 release / tag / public asset verification
