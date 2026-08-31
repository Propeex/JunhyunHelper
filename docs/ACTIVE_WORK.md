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
latest checkpoint head before this doc update: aac0e62b7ee3522cf07888b0c91b6e2088a29fdb
```

## Confirmed scope

1. Secure Container(사망해도 내부 아이템이 보존되는 보안 컨테이너)를 파밍 가이드의 컨테이너 장비 슬롯에 정상 장착할 수 있어야 한다.
2. Glock 등 총기 더블클릭 시 실제 부착물/mod 슬롯을 표시하고 직접 드래그/드롭으로 장착·교체할 수 있어야 한다.
3. 가방 안 가방, 가방 안 리그처럼 수납공간을 가진 아이템을 더블클릭하면 내부 그리드가 실제 작업 가능한 빈 칸으로 열려야 하고, 아이템을 그 안으로 드래그해 넣고 다시 꺼낼 수 있어야 한다.
4. Altyn 같은 헬멧/방어구의 부착물·교체형 방탄판은 설명/선택 콤보가 아니라 실제 드롭 슬롯으로 표시한다.
5. 기존 `장비 정보/장비 설정` 별도 Window 및 읽기 전용 그리드 미리보기 중심 UX를 폐기하고, 아이템 유형/상황에 필요한 실제 내부 구조만 보여준다. 가방은 수납 그리드, 중첩 리그는 수납 그리드, 방탄 장비는 필요한 plate/attachment 슬롯, 총기는 mod 슬롯을 보여주는 식으로 동작한다.
6. 검색 결과에서 동일한 총기가 다수 노출되는 원인을 조사해, upstream weapon preset은 검색 source에서 제외하고 실제 base weapon/item만 드래그 가능한 목록으로 노출한다.

## Root cause confirmed

- 기존 stored state에는 부모 container instance가 없어 nested storage를 실제 상태로 표현할 수 없었다.
- 기존 double-click은 별도 `FarmingGuideItemConfigurationWindow`에서 storage read-only preview와 attachment/armor ComboBox를 사용해 제품 의도와 불일치했다.
- current Tarkov source에서 Epsilon 같은 Secure Container와 Medicine Case 같은 일반 stash case가 모두 `ItemPropertiesContainer`를 사용할 수 있으므로 property type만으로 Secure Container slot 호환성을 판단할 수 없다.
- upstream item collection에는 `ItemPropertiesPreset` / `preset` assembled weapon records가 base weapon과 함께 존재해 같은 총기가 반복 노출되고 preset record에는 base weapon mod slots가 없을 수 있었다.

## Completed

- `FarmingGuideStoredItemState.ParentInstanceId` nullable field 추가. 기존 schema-v1 JSON은 missing field → null root placement로 호환.
- nested sanitize가 root → child 순으로 parent 존재/current grid/filter/bounds/overlap을 검증하고 orphan/duplicate/self-parent/cycle을 fail closed 처리.
- nested container 이동 시 same instance/subtree를 보존하고 self/descendant cycle 차단.
- destructive remove/carrier replacement 시 descendant subtree까지 함께 제거.
- 가운데 storage column의 in-page `WorkbenchHost` 구현. 오른쪽 검색은 유지해 열린 실제 grid/slot으로 drag/drop 가능.
- stored backpack/rig → 실제 internal grid, top-level worn rig → actionable plate/mod slots, weapon/helmet/body armor → attachment/plate drop slots.
- attachment/armor slot one-item contract 및 occupied slot silent overwrite 방지.
- 기존 `FarmingGuideItemConfigurationWindow` 삭제.
- Farming Guide search에서 `ItemPropertiesPreset` / `preset` 제외. canonical Game Content와 실제 base weapon/variant는 보존.
- total storage summary에 nested container grids 반영.
- current Secure Container 판정을 명시적 secure-container/pouch semantics 우선 + `ItemPropertiesContainer` narrow fallback으로 수정. generic `container/case`인 Medicine Case 등은 장착 거부.
- nested sanitize / nested persistence / preset filtering / Secure Container accept + ordinary case reject 회귀 테스트 추가.
- workbench가 열린 동안 왼쪽 equipment 재배치를 시작하면 먼저 workbench를 닫아 stale owner callback 방지 + lifecycle contract test 추가.
- Desktop version/FIRST_RUN/release notes를 v1.13.3으로 준비하고 PROJECT_STATE product target version을 1.13.3으로 정렬. publicStable은 실제 릴리즈 전까지 v1.13.2 유지.
- `docs/PRODUCT.md`, `docs/ARCHITECTURE_FARMING_GUIDE.md`, `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`, `docs/RELEASE_NOTES_V1.13.3.md`에 확정된 제품/기술 계약 반영.
- CI에서 이전 코드 HEAD 기준 Windows Release build와 deterministic tests가 성공한 것을 확인. 초기 CI 실패는 xUnit analyzer 표현 두 곳과 문서 checkpoint/version 정렬 문제였고 모두 수정.

## Current step

- 최신 exact PR head의 CI / Shutdown Race / Documentation Consistency를 다시 통과시키고 Windows x64 publish + actual Product UI/Map/graceful-shutdown smoke 결과를 확인한다.
- 장기 결정 index/reference 문서에서 v1.13.3 Farming Guide 구현 위치와 supersession 관계를 최종 정리한다.

## Remaining

- latest PR exact-head CI green 확인: Release build, full tests, publish, Product UI/Map/graceful shutdown smoke, package/artifact
- Shutdown Race / Documentation Consistency green 확인
- `docs/DECISIONS.md` / `docs/DEVELOPER_REFERENCE.md` current Farming Guide reference 정리
- PR ready 전환 및 main 병합
- exact-main CI / Shutdown Race / Documentation Consistency 검증
- automatic v1.13.3 Release workflow 성공 확인
- tag / release / latest / asset size+digest 검증
- release evidence와 canonical current-state 문서 갱신
- ACTIVE_WORK `NONE` closure
