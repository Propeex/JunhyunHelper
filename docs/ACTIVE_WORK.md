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
PR: not created yet
```

## Confirmed scope

1. Secure Container(사망해도 내부 아이템이 보존되는 보안 컨테이너)를 파밍 가이드의 컨테이너 장비 슬롯에 정상 장착할 수 있어야 한다.
2. Glock 등 총기 더블클릭 시 실제 부착물/mod 슬롯을 표시하고 직접 드래그/드롭으로 장착·교체할 수 있어야 한다.
3. 가방 안 가방, 가방 안 리그처럼 수납공간을 가진 아이템을 더블클릭하면 내부 그리드가 실제 작업 가능한 빈 칸으로 열려야 하고, 아이템을 그 안으로 드래그해 넣고 다시 꺼낼 수 있어야 한다.
4. Altyn 같은 헬멧/방어구의 부착물·교체형 방탄판은 설명/선택 콤보가 아니라 실제 드롭 슬롯으로 표시한다.
5. 기존 `장비 정보/장비 설정` 별도 Window 및 읽기 전용 그리드 미리보기 중심 UX를 폐기하고, 아이템 유형/상황에 필요한 실제 내부 구조만 보여준다. 가방은 수납 그리드, 중첩 리그는 수납 그리드, 방탄 장비는 필요한 plate/attachment 슬롯, 총기는 mod 슬롯을 보여주는 식으로 동작한다.
6. 검색 결과에서 동일한 총기가 다수 노출되는 원인을 조사해, 의미가 같은 upstream 변형/중복이면 사용자 검색 목록에서 canonical item 하나로 정규화하되 실제로 다른 Tarkov 아이템 변형은 보존한다.

## Root cause confirmed so far

- `FarmingGuideStoredItemState`는 `FarmingGuideStorageKind`만 저장하고 **부모 컨테이너 instance를 식별할 수 없어 nested storage를 표현할 수 없다.** 따라서 현재 중첩 가방/리그 내부는 읽기 전용 구조 미리보기밖에 만들 수 없는 상태다.
- 현재 double-click은 `FarmingGuideItemConfigurationWindow`라는 별도 WPF `Window`를 열며, storage는 미리보기, attachments/armor plates는 ComboBox 선택으로 구현되어 있어 사용자 의도와 불일치한다.
- Secure Container는 top-level `FarmingGuideStorageKind.SecureContainer` carrier로 별도 모델링되어 있으므로 compatibility와 drag/drop target 경로를 함께 점검한다.
- `TarkovItemImporter`는 upstream item id 단위로 모든 items를 그대로 canonical catalog에 넣으므로 검색 UI가 이름만 기준으로 보면 동일 총기처럼 보이는 항목을 다수 노출할 수 있다. 실제 source semantics를 확인한 뒤 UI용 dedupe 정책을 결정한다.

## Completed

- repository/current stable 상태 복구
- v1.13.2 Farming Guide architecture/decision 및 관련 Core/Desktop 구현 분석
- nested-storage state 모델 결손 확인
- generic configuration Window/ComboBox 기반 interaction mismatch 확인
- item importer의 id 단위 catalog 보존 방식 확인
- 작업 브랜치 생성

## Current step

- 실제 item data 구조와 현재 regression tests를 확인해 secure container / Glock attachment / Altyn slot / duplicate weapon의 정확한 data-path를 확정한다.
- 이후 nested storage address/state 모델과 reusable live inventory surface를 구현한다.

## Remaining

- secure container compatibility/data-path 수정
- nested container address/state + sanitize/persistence/move/remove 로직 구현
- reusable interactive internal inventory surface 구현
- attachment / armor plate live drop targets 구현
- item-type/context-aware double-click routing 구현
- weapon search duplicate normalization
- regression tests
- product/decision/architecture/reference/state docs 갱신
- test / Release build / published EXE smoke
- PR / CI / main merge / exact-main validation
- v1.13.3 release / tag / public asset verification
