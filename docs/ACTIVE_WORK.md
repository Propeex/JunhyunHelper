# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

v1.11.4 PATCH 유지보수 배치에서 v1.11.3 실사용으로 확인된 MiniMap lifecycle/marker presentation 회귀와 Mini Scanner 우클릭 메뉴를 수정하고, 실제 published EXE 경로까지 검증한 뒤 stable release한다.

## Base / Working State

```text
base main: f60096d6a404e17cdf9ace3f1bc9c5cf98c2ed62
public stable: v1.11.3
exact v1.11.3 product source: 043abad38f4c3ebc9101463a162614ef67df7536
working branch: fix/v1.11.4-minimap-lifecycle-miniscanner-2026-08-31
target version: v1.11.4 (PATCH maintenance)
baseline deterministic suite: 474 tests
```

## Confirmed scope

1. MiniMap 최초 표시 map synchronization
   - Main Map에서 A → B로 바꾼 뒤 MiniMap을 처음 켰을 때 이전 A가 아니라 현재 B를 첫 visible frame부터 표시한다.
   - 이미 열린/reused MiniMap뿐 아니라 아직 MiniMap window가 한 번도 생성되지 않은 first-create path를 검증한다.

2. Player Marker Size 변경 시 다른 Map/MiniMap presentation 보존
   - Player Marker Size를 바꿔도 사용자가 설정한 Name Size와 MiniMap Marker Size의 실제 렌더링이 초기값처럼 되돌아가지 않는다.
   - UI 값만 남고 실제 표시만 달라지는 상태를 허용하지 않는다.

3. MiniMap marker blink/disappear 회귀
   - marker가 잠깐 나타난 뒤 전부 사라지는 lifecycle/refresh race의 root cause를 수정한다.
   - MiniMap 재열기 또는 marker toggle을 사용자가 수동 복구할 필요가 없어야 한다.

4. Mini Scanner right-click menu 제거
   - Mini Scanner 우클릭 시 현재 표시되는 `현재 결과 교정` context menu를 더 이상 표시하지 않는다.
   - Mini Scanner의 left-drag, topmost, 결과 표시 및 교정 데이터 단축키 계약은 유지한다.

## Current findings

- Mini Scanner menu는 `MiniScannerWindow.xaml`의 `DragSurface`에 직접 선언된 `Border.ContextMenu`이며 `MiniScannerWindow.Correction.cs`의 modal correction handler를 호출한다. 제품 요구사항상 해당 context menu 전체를 제거할 수 있다.
- MiniMap에는 v1.11.3에서 map selection bridge와 reused-window A→B smoke가 이미 존재하지만, 사용자가 보고한 exact first-create path는 runtime smoke가 직접 검증하지 않는다.
- donor MiniMap marker refresh는 refresh 시작 시 visible marker containers를 먼저 clear한 뒤 async load를 수행한다. 이후 refresh cancellation/reentry가 발생하면 빈 layer가 남을 수 있는 구조이며 현재 product recovery timer는 사후 복구 방식이다. 실제 trigger/cancellation 경로를 추가 추적해 root cause 수준에서 수정한다.
- Player Marker Size 변경은 donor `UpdateMapView()`를 호출하며 product reapply bridge가 존재하지만 실사용에서 Name/marker presentation reset이 남아 있어 비동기 후속 render/event 순서를 추가 감사한다.

## Completed

- v1.11.3 stable / exact product source / main / 474-test baseline 복구.
- 사용자 요구사항 4건을 v1.11.4 maintenance scope로 확정.
- 작업 branch 생성.
- Mini Scanner context-menu root cause 확인.
- existing MiniMap first-create/reuse synchronization, player-marker reapply, marker-recovery 경로 1차 audit.

## Current step

MiniMap first-create lifecycle, marker async refresh cancellation path, Player Marker Size 후속 render 순서를 끝까지 추적하고 deterministic/runtime regression을 설계한 뒤 구현한다.

## Remaining

- root cause audit 완료 및 구현.
- deterministic regression + published EXE smoke 보강.
- v1.11.4 release identity/release notes 갱신.
- PR / Windows Release build / tests / publish / Product UI + Map + MiniMap + Scanner smoke / shutdown / package 검증.
- main 병합 / exact-main 검증 / automatic stable release / public tag/assets readback.
- release-state docs finalization 및 ACTIVE_WORK 종료.
