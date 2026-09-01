# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

v1.15.5 Farming Guide maintenance PATCH.

사용자 실사용 피드백을 반영하여:

- nested storage workbench가 실제 내부 grid 전체를 한눈에 보여 주도록 viewport sizing 회귀를 수정한다.
- raid Farming Guide 지시 문구를 짧고 즉시 읽히는 형태로 단순화한다.
- 재배치/폐기 부가 지시는 실제로 사용자가 별도 조작해야 하는 경우에만 `+` 형식으로 표시하고, 여러 항목은 쉼표로 구분한다.
- 기존 보존 우선 raid-planning / equipment-upgrade / dedicated-container 로직 계약은 유지한다.

## Base / branch

```text
base main: f8ad66d7919dba9ca8b7cdf7eb55083fe42e83fe
public stable: v1.15.4
working branch: fix/v1.15.5-farming-guide-ui-instructions-2026-09-01
PR: #270
```

## Confirmed scope

1. nested container workbench는 내부 grid가 화면에 물리적으로 들어갈 수 있으면 scrollbar 없이 전체 grid를 표시한다. 창/패널 자체가 grid 크기에 맞게 충분히 확장되어야 한다.
2. raid instruction 문구는 사용자 확정 축약 규칙을 따른다.
3. 같은 보관 위치 안에서 발생하는 단순 위치 재정렬은 사용자에게 별도 지시하지 않는다.
4. 실제로 다른 위치로 옮기거나 버려야 하는 기존 아이템만 `+ {아이템} 이동 {위치}` / `+ {아이템} 버리기`로 표시한다.
5. 부가 조작이 여러 개면 쉼표로 구분하고, 각각의 동작 표현은 동일 철학을 유지한다.
6. presentation 단순화는 planner의 Action/ProposedSnapshot을 변경하지 않는다.

## Completed

- stable v1.15.4 / main 상태 복구
- 사용자 요구사항 확정
- v1.15.5 작업 브랜치 및 Draft PR #270 생성
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

## Current step

최종 v1.15.5 PR head의 CI를 다시 통과시킨 뒤 main 병합 및 exact-main 검증으로 진행한다.

## Remaining

- 최종 PR head CI / Shutdown Race / Documentation Consistency
- PR ready / main merge
- exact-main CI / Shutdown Race / Documentation Consistency
- v1.15.5 release / tag / asset 무결성 검증
- 공개 상태 문서 갱신
- ACTIVE_WORK 종료
