# DECISION — v1.15.5 Farming Guide concise raid instructions and nested Workbench viewport

Status: **CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED**  
Date: **2026-09-01 KST**

## Context

v1.15.4의 보존 우선 planner는 유지하되 raid 중 읽는 Mini Scanner 문구는 과도하게 길었고, Key tool 같은 compact nested storage는 전체 grid가 들어가는 경우에도 WPF ScrollViewer feedback 때문에 하단 셀이 잘릴 수 있었다.

## Decision 1 — compact action vocabulary

Planner가 complete proposed state를 소유하고 presentation은 별도 마지막 단계다. Presentation은 action type, proposed snapshot, locks, storage legality, equipment comparison, loot priority를 바꾸지 않는다.

Primary wording:

- empty equipment target: `[장비 위치] 장착`
- body armor replacement: `방탄복 교체`
- headset replacement: `헤드셋 교체`
- other top-level equipment/carrier replacement: `[장비 위치] 교체`
- body armor + ordinary rig -> armored rig: `방탄 리그 전환`
- store/repack: `[보관 위치] 보관`
- destructive storage replacement: `[보관 위치] [기존 아이템] 버리고 보관`
- no preferable legal plan: `버리기`

방어등급 delta, headset tuning, `내부 N개 재배치` 같은 판단 근거는 내부 logic에 남지만 raid action text에서는 반복하지 않는다.

## Decision 2 — only materially distinct manipulations are spoken

같은 visible storage area 내부의 grid index, X/Y, rotation 변화는 instruction에서 생략한다.

- 다른 root storage 또는 다른 nested-container instance로 이동: `+ [아이템] 이동 [위치]`
- modeled raid inventory에서 실제 제거: `+ [아이템] 버리기`
- 여러 부가 조작: `, `로 구분

Instruction suppression용 storage-area identity는 root에서는 `FarmingGuideStorageKind`, nested에서는 owning `ParentInstanceId`다. Mechanical placement validation은 여전히 exact grid/coordinates/rotation을 사용한다.

## Decision 3 — Workbench fit owns both scroll axes

Nested Workbench sizing은 real rendered grid footprint, title/close chrome, border/padding, ScrollViewer template chrome을 포함한다. Horizontal/vertical scrollbar need를 함께 해결한다.

전체 grid가 effective center-column viewport에 들어가면 horizontal과 vertical scrollbar를 모두 명시적으로 비활성화하고 전체 grid를 노출한다. 실제 overflow가 있을 때만 scrolling fallback을 사용한다.

## Regression contract

Published-product smoke는 4x4 Key-tool-like nested storage fixture에서 fitting content의 양 축 scrollbar가 disabled이고 `ScrollableWidth`/`ScrollableHeight`가 사실상 0인지 검증한다. 동일 smoke는 compact wording과 same-area suppression, cross-area move/discard 표현을 검증한다.

## Public verification

v1.15.5 exact product source `62466a957a7e32a623a0ffcfad96bfb16504f823`에서 exact-main CI `33520705401`, Shutdown Race `33520705533`, Documentation Consistency `33520705395`, Release workflow `33521076146`가 모두 성공했다. 공개 tag/release/latest도 동일 source를 가리킨다.

## Non-changes

- preservation-first planning principle
- source-backed equipment superiority rules
- dedicated-container preference
- storage filters/dimensions/rotation mechanics
- lock/reserved-cell constraints
- complete-equipment boundary
- explicit acceptance transaction semantics

State-transition 확장은 별도 canonical decision `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`에 기록한다.
