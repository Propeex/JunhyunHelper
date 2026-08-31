# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

**v1.13.1 Farming Guide — 실사용 UI/drag-drop 회귀 수정**

사용자 실사용 캡처와 원래 스케치를 기준으로 v1.13.0 파밍 가이드의 UI/interaction 불일치를 수정한다. 최우선 목표는 텍스트 리스트형 화면을 **아이콘 중심의 Tarkov 인게임 인벤토리 유사 UI**로 바로잡는 것이다.

## Base

```text
base main: c578b074a36fb6703191e17cc46f17e188816010
branch: fix/v1.13.1-farming-guide-ui-regressions-2026-08-31
PR: #243
public stable: v1.13.0
status: IMPLEMENTING / VERIFYING
```

## Confirmed scope

- 전체 화면이 텍스트 리스트형으로 구현되어 원래 의도인 아이콘 중심 인게임 인벤토리형 UI와 크게 다른 문제 수정.
- 저장 아이콘 및 검색창 입력 텍스트 clipping 수정.
- 방탄복 / 리그 / 가방 / 보안 컨테이너 drag-drop 장착 실패 수정.
- drag 유효/무효 초록/빨강 target 강조가 커서를 치운 뒤 남는 문제 수정.
- drag ghost를 item 이름 텍스트 박스가 아닌 실제 item icon으로 변경.
- 장착 아이템 및 storage grid 배치 아이템을 text가 아닌 실제 item icon으로 렌더링.
- 기존 placement / compatibility / persistence / preset / fixed melee·dogtag / rotation / populated-carrier safety 계약은 보존.

## Completed

- 작업 branch와 PR #243 생성.
- 장비 영역을 vertical text list에서 spatial inventory slot board로 재구성.
- 리그/가방/보안 컨테이너를 item icon 장착 target + 실제 내부 grid 조합으로 재구성.
- storage placement와 drag ghost를 공통 item image presentation으로 변경.
- drag 중 WPF mouse capture에 의존하지 않는 geometry-backed target probing을 추가해 equipment/carrier target hit 판정을 보강.
- transient success/danger border를 probe 변경/end 시 기본 border로 되돌리는 cleanup 추가.
- save emoji를 WPF vector icon으로 교체하고 search TextBox vertical layout 보정.
- PR 최초 Documentation Consistency 실패 원인(`ACTIVE_WORK` 필수 heading 누락)을 확인함. 제품 코드 문제가 아니라 작업 체크포인트 형식 문제이며 이 문서에서 바로잡음.

## Current step

PR #243 exact-head Windows CI / Shutdown Race CI 결과를 확인하고 compile/runtime smoke 실패가 있으면 수정한다. 이후 Farming Guide regression 검증을 보강한다.

## Remaining

- exact-head CI / Shutdown Race CI / Documentation Consistency 전부 통과.
- 필요 시 deterministic regression test 또는 published EXE smoke assertion 보강.
- Windows Release publish artifact의 Farming Guide 실제 렌더/interaction smoke 확인.
- 사용자 요구사항과 실제 UI 결과가 맞는지 최종 검토.
- 상태/릴리즈 문서 갱신, v1.13.1 patch release 준비 및 검증.
- main 병합 후 exact-main CI / release/tag/asset 검증.
- 완전 종료 후 `ACTIVE_WORK`를 `NONE`으로 닫기.

## Last completed work

**v1.13.0 Farming Guide — raid-start Loadout / Inventory Editor**

```text
public stable: v1.13.0
exact product release source/tag target:
103ade0c5d54ffb59a6844330d19a930899c12fb
feature branch: feature/v1.13.0-farming-guide-loadout-editor-2026-08-31
original Draft PR: #240 — CLOSED / NOT MERGED
replacement PR: #241 — MERGED
validated feature head:
30424d0cc401a62b415dd772c52e5de4f6c931ee
exact-main CI: 33358877907 — SUCCESS
exact-main Shutdown Race CI: 33358877912 — SUCCESS
exact-main Documentation Consistency: 33358877946 — SUCCESS
release workflow: 33359054856 — SUCCESS
release id: 379519928
494 passed / 0 failed / 0 skipped
```

## Canonical records

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
