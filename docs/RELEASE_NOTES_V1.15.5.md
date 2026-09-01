# 준현 헬퍼 v1.15.5

상태: **PUBLIC STABLE / VERIFIED**  
기준일: **2026-09-01 KST**

v1.15.5는 Farming Guide의 raid 지시 가독성, nested Workbench viewport, 장비 교체 후 상태 전이 보존을 강화한 유지보수 PATCH다. 기존 source-backed 장비/수납 계약과 명시적 수락 transaction을 유지한다.

## 파밍 지시 단순화

Mini Scanner의 기본 행동 문구는 다음처럼 짧게 표시한다.

- 빈 장비칸: `[장비 위치] 장착`
- 방탄복 업그레이드: `방탄복 교체`
- 헤드셋 업그레이드: `헤드셋 교체`
- 기타 최상위 장비/리그/가방 교체: `[장비 위치] 교체`
- 방탄복 + 일반 리그 -> 방탄 리그: `방탄 리그 전환`
- 보관: `[보관 위치] 보관`
- 낮은 우선순위 아이템 제거 후 보관: `[보관 위치] [기존 아이템] 버리고 보관`
- 최종 폐기: `버리기`

같은 visible storage area 안에서 grid/좌표/회전만 바뀌는 재배치는 별도 멘트로 표시하지 않는다. 실제 다른 storage area로 이동하는 기존 아이템만 `+ [아이템] 이동 [위치]`, 실제 제거되는 아이템만 `+ [아이템] 버리기`로 표시하며 여러 작업은 `, `로 구분한다. 이 formatting은 planner의 `Action`과 `ProposedSnapshot`을 변경하지 않는다.

## 장비 교체 후 기존 장비 보존

v1.15.5는 장비 교체 결과를 단순한 incoming-item 처리로 보지 않고 도달 가능한 전체 raid inventory state transition으로 평가한다.

- 교체로 벗겨진 일반 장비, 총기, 리그, 가방은 즉시 삭제되지 않고 loot candidate가 된다.
- 합법적인 root/nested storage 또는 repacking으로 보존 가능한지 먼저 탐색한다.
- displaced rig/backpack이 다른 컨테이너에 들어가도 그 내부 source-backed grid를 같은 candidate snapshot에서 수납공간으로 사용할 수 있다.
- destruction은 preservation path가 실패하고 별도 retention policy가 더 나은 retained state임을 증명할 때만 bounded하게 허용한다.
- locked subtree, populated structural container, reserved cells, source filters, nested cycle safety는 유지한다.

Needed 획득량은 historical accept counter가 아니라 `current snapshot count - raid baseline count`로 계산한다. 따라서 Needed item을 획득했다가 버리면 다음 판단에서 다시 Needed로 평가된다.

## Nested storage Workbench 세로 잘림 수정

Workbench 크기는 rendered grid footprint, 제목/닫기 header, Border/Padding, ScrollViewer template chrome, 실제 필요한 scrollbar를 함께 반영한다. 가로/세로 scrollbar 필요를 상호 의존적으로 계산하며 전체 grid가 center-column viewport에 들어가면 양 축 scrollbar를 명시적으로 비활성화한다. 실제 viewport보다 큰 grid만 scrolling fallback을 사용한다.

## 보존된 핵심 계약

- source-backed storage grid/filter와 dedicated-container preference
- item/equipment/carrier locks와 reserved cells
- nested parent/descendant cycle 방지
- populated nested container의 공격적 자동 파괴 금지
- complete-equipment boundary
- source-backed equipment superiority rules
- body armor + ordinary rig -> armored rig atomic transition
- explicit accept 전 session state 미변경
- unsupported live facts에 대한 conservative fail-closed 동작

## 공개 검증

```text
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head:
2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
exact-main CI: 33520705401 — SUCCESS
Shutdown Race: 33520705533 — SUCCESS
Documentation Consistency: 33520705395 — SUCCESS
Release workflow: 33521076146 — SUCCESS
593 passed / 0 failed / 0 skipped
```

Published EXE smoke는 compact instruction, displaced-equipment transition, nested repacking, 4x4 Key-tool-like Workbench dual-axis fit, Product UI/Map startup 및 정상 종료를 포함한다.

Canonical decisions:

- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`

Release evidence:

- `docs/RELEASE_1.15.5.md`
- `docs/.release-v1.15.5-status.json`
