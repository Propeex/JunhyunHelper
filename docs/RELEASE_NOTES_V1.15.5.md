# 준현 헬퍼 v1.15.5

상태: **RELEASE CANDIDATE**  
기준일: **2026-09-01 KST**

v1.15.5는 v1.15.4 Farming Guide의 실사용 표시 문제를 수정하는 유지보수 PATCH다. 파밍 판단의 보존 우선 로직과 source-backed 장비/수납 계약은 유지하면서, Mini Scanner 지시를 짧게 만들고 Key tool 같은 nested storage 상세창의 세로 잘림을 수정한다.

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

방어등급 변화, 헤드셋 비교 설명, `내부 N개 재배치` 같은 판단 근거는 내부 로직에는 그대로 남지만 raid action text에서는 반복하지 않는다.

## 이동/버리기 부가 지시

- 같은 가방·리그·컨테이너 또는 같은 nested container 안에서 grid/좌표/회전만 바뀌는 재배치는 별도 멘트로 표시하지 않는다.
- 기존 아이템이 실제로 다른 root storage 또는 다른 nested container로 이동해야 할 때만 `+ [아이템] 이동 [위치]`를 붙인다.
- 기존 아이템을 실제로 제거해야 할 때만 `+ [아이템] 버리기`를 붙인다.
- 부가 작업이 여러 개면 `, `로 구분한다.
- destructive storage replacement에서 버릴 아이템은 기본 문장에 포함하고, 다른 위치로 옮겨야 하는 아이템만 뒤의 `+` 작업으로 표시한다.

## Nested storage Workbench 세로 잘림 수정

v1.15.4는 fitting grid의 가로 스크롤 feedback을 막았지만 실제 Key tool 같은 compact container에서 하단 칸이 몇 픽셀 잘리고 세로 스크롤바가 생길 수 있었다.

v1.15.5는 Workbench 크기를 다음 요소까지 포함해 계산한다.

- 실제 rendered grid footprint
- 제목/닫기 header
- Border/Padding
- ScrollViewer template chrome
- 실제 필요한 경우에만 시스템 scrollbar 크기

가로/세로 scrollbar가 서로 상대 축의 공간을 줄이는 효과를 함께 계산하며, 전체 grid가 center-column viewport에 들어가면 두 scrollbar를 명시적으로 비활성화한다. 실제 viewport보다 큰 grid만 scrolling fallback을 사용한다.

## 로직 점검 결과

이번 수정에서 raid planner의 의미를 다시 검토했고 다음 기존 계약을 유지했다.

1. 증명된 안전한 장비 업그레이드
2. 직접 합법 수납
3. 비파괴 재배치
4. 보존 경로 실패 후 필요도/가치 기반 파괴 교체
5. 마지막으로 버리기

또한 다음 제약도 유지한다.

- source-backed storage grid/filter
- dedicated-container preference
- item/equipment/carrier locks
- reserved cells
- nested parent/descendant cycle 방지
- populated nested container 자동 파괴 금지
- complete-equipment boundary
- body armor + ordinary rig -> armored rig atomic transition
- explicit accept 전에는 session state 미변경

지시 문구 단순화는 planner가 만든 `Action`과 `ProposedSnapshot`을 변경하지 않는 별도 presentation 단계에서 수행한다.

## 회귀 검증

published EXE smoke에는 다음 계약을 추가했다.

- 4x4 Key-tool-like nested storage가 viewport에 들어갈 때 horizontal/vertical scrollbar가 모두 비활성화되고 실제 scrollable extent가 0인지 검증
- `방탄복 장착`
- `방탄복 교체`
- `헤드셋 교체`
- 일반 장비 교체
- carrier 교체 시 같은 영역 재배치 멘트 억제
- `방탄 리그 전환`
- 직접 보관
- 같은 storage 내부 재배치 후 보관 멘트 억제
- 여러 cross-area move를 쉼표로 표시하는 destructive replacement
- `버리기`

공개 v1.15.5는 final PR head와 exact-main에서 Windows Release build, deterministic tests, self-contained publish, published EXE product smoke, graceful shutdown, Shutdown Race, package/checksum, Documentation Consistency가 모두 통과한 source만 릴리즈한다.

Canonical decision:

- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
