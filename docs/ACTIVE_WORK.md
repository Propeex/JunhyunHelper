# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

v1.15.5 Farming Guide maintenance PATCH.

사용자 실사용 피드백을 반영하여:

- nested storage workbench가 실제 내부 grid 전체를 한눈에 보여 주도록 viewport sizing 회귀를 수정한다.
- raid Farming Guide 지시 문구를 짧고 즉시 읽히는 형태로 단순화한다.
- 재배치/폐기 부가 지시는 실제로 사용자가 별도 조작해야 하는 경우에만 `+` 형식으로 표시하고, 여러 항목은 쉼표로 구분한다.
- 장비 교체로 벗겨진 기존 장비를 자동 폐기하지 않고 하나의 파밍 아이템으로 다시 평가해 합법적 보관/재배치/최종 폐기까지 전체 상태에서 판단한다.
- 기존 보존 우선 raid-planning / equipment-upgrade / dedicated-container 로직 계약은 유지한다.

## Base / branch

```text
base main: f8ad66d7919dba9ca8b7cdf7eb55083fe42e83fe
public stable: v1.15.4
working branch: fix/v1.15.5-farming-guide-ui-instructions-2026-09-01
PR: #271
```

## Confirmed scope

1. nested container workbench는 내부 grid가 화면에 물리적으로 들어갈 수 있으면 scrollbar 없이 전체 grid를 표시한다. 창/패널 자체가 grid 크기에 맞게 충분히 확장되어야 한다.
2. raid instruction 문구는 사용자 확정 축약 규칙을 따른다.
3. 같은 보관 위치 안에서 발생하는 단순 위치 재정렬은 사용자에게 별도 지시하지 않는다.
4. 실제로 다른 위치로 옮기거나 버려야 하는 기존 아이템만 `+ {아이템} 이동 {위치}` / `+ {아이템} 버리기`로 표시한다.
5. 부가 조작이 여러 개면 쉼표로 구분하고, 각각의 동작 표현은 동일 철학을 유지한다.
6. presentation 단순화는 planner의 Action/ProposedSnapshot을 변경하지 않는다.
7. 장비/리그/가방 교체 후 기존 장비는 즉시 폐기하지 않는다. 새 장비를 장착한 뒤 벗겨진 기존 장비도 스캔한 파밍 아이템과 같은 가치/필요도/공간 판단 대상으로 되돌리고, 가방·리그·nested storage에 보관하거나 필요 시 그 내부 수납공간까지 활용하는 전체 재배치 결과를 먼저 찾는다. 합법적이고 더 가치 있는 보존 경로가 없을 때만 버린다.
8. 이 확장 판단은 특정 리그 사례에 한정하지 않고 Backpack/Rig 및 일반 top-level equipment 교체에 공통 적용할 수 있는 상태 전이로 설계한다.

## Completed

- stable v1.15.4 / main 상태 복구
- 사용자 요구사항 확정
- v1.15.5 작업 브랜치 생성
- closed draft PR #270을 non-draft PR #271로 교체
- raid planner/upgrade/repacking 흐름 재검토: 보존 우선·source-backed 제약은 유지하고 presentation만 분리
- compact raid instruction formatter 구현
- same-storage-area X/Y/grid/rotation repacking 멘트 억제
- cross-area move / removal만 `+` 부가 작업으로 표시하고 복수 작업 comma 구분
- nested Workbench 양축 viewport/scrollbar 안정화 구현
- 4x4 Key-tool-like nested container + compact instruction published-runtime smoke 추가
- 구현 head `e6834d582df311b14199d5a56efa141b9cd05629` 검증:
  - Windows Release build SUCCESS
  - deterministic tests SUCCESS
  - self-contained win-x64 publish SUCCESS
  - published EXE Product UI/Farming Guide smoke SUCCESS
  - release package verification SUCCESS
  - Shutdown Race CI 33506271930 SUCCESS
  - CI 33506271835 SUCCESS
  - Documentation Consistency 33506271972 SUCCESS
- v1.15.5 version identity / first-run notes / release notes / decision 문서 반영
- pre-PR-replacement final head `23fe805984c2aedada2d7cda4c61e27e155bc6f1`도 CI 33506969505 / Shutdown 33506969407 / Docs 33506969463 SUCCESS
- 사용자 추가 요구로 기존 장비 자동 폐기 가정이 잘못되었음을 확인하고 displaced-equipment-as-loot 계약을 확정

## Current step

PR #271은 main 병합/릴리즈를 보류한다. 기존 장비 자동 폐기 문제를 포함해 현재 파밍 가이드 판단이 놓치는 상태 전이와 가치/수납 상황을 체계적으로 분석한 뒤 planner를 보완한다.

## Remaining

- 전체 raid-planning blind-spot 분석 및 제품 동작 확정
- displaced equipment를 포함하는 preservation/value/storage 재계산 구현
- 관련 deterministic/runtime 회귀 테스트 확장
- PR #271 current-head CI / Shutdown Race / Documentation Consistency 재검증
- main merge
- exact-main CI / Shutdown Race / Documentation Consistency
- v1.15.5 release / tag / asset 무결성 검증
- 공개 상태 문서 갱신
- ACTIVE_WORK 종료
