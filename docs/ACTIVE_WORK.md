# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Current work

**v1.13.1 Farming Guide — 실사용 UI/drag-drop 회귀 수정**

```text
base main: c578b074a36fb6703191e17cc46f17e188816010
branch: fix/v1.13.1-farming-guide-ui-regressions-2026-08-31
public stable: v1.13.0
status: IMPLEMENTING
```

사용자 실사용 캡처와 원래 스케치를 기준으로 v1.13.0 파밍 가이드의 UI/interaction 불일치를 수정한다.

### Confirmed user evidence / required fixes

- 전체 화면이 텍스트 리스트형으로 구현되어 원래 의도인 **아이콘 중심의 Tarkov 인게임 인벤토리 유사 UI**와 크게 다름.
- 저장 아이콘이 잘리고 검색창 입력 텍스트가 상하로 잘림.
- 방탄복 / 리그 / 가방 / 보안 컨테이너 장착이 실제 UI에서 실패함.
- drag 중 유효/무효 drop target의 초록/빨강 강조가 커서를 치운 뒤에도 남음.
- drag ghost가 item icon이 아니라 item 이름 텍스트 박스임.
- 장착 아이템 및 storage grid 배치 아이템이 icon 대신 text로 렌더링됨.

### Implementation direction

- 장비 영역을 vertical list가 아닌 인벤토리형 slot board로 재구성하고 slot 내부에 실제 item icon을 렌더링한다.
- 리그/가방/컨테이너 carrier도 아이콘 중심의 장착 target + 실제 내부 grid가 한 덩어리로 보이게 한다.
- storage placement와 drag ghost를 동일한 item image presentation으로 통일한다.
- drag overlay를 hit-test에서 제외하고 target probing/cleanup을 보강하여 equipment/carrier drop을 정상화한다.
- 이전 target의 transient success/danger border를 매 probe/end에서 원복한다.
- preset save button/search textbox clipping을 WPF-safe vector/icon/layout으로 수정한다.
- 기존 placement/compatibility/persistence 계약과 preset/fixed equipment semantics는 보존한다.

### Verification plan

- deterministic Farming Guide UI/interaction regression tests 추가/강화
- full deterministic test suite
- Windows Release build + publish
- published EXE Farming Guide smoke: icon render, equipment/carrier drop, grid placement, drag highlight cleanup, preset/search control clipping
- PR exact-head CI 및 merge 후 exact-main 검증

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
