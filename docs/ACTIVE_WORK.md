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
current deterministic suite: 478 tests
draft PR: #235
```

## Confirmed scope

1. MiniMap 최초 표시 map synchronization
   - Main Map에서 A → B로 바꾼 뒤 MiniMap을 처음 켰을 때 이전 A가 아니라 현재 B를 첫 visible frame부터 표시한다.
   - 이미 열린/reused MiniMap뿐 아니라 아직 MiniMap window가 한 번도 생성되지 않은 first-create path를 검증한다.

2. MiniMap extract / general marker 전체 점검
   - PMC / Scav / Transit extract marker filter와 실제 rendered marker를 검증한다.
   - Transit은 checkbox state만 보지 않고 packaged data의 실제 Transit grouped extract 수와 MiniMap rendered Transit marker 수가 일치해야 한다.
   - standard marker layer가 async refresh cancellation timing으로 비어도 사용자 재열기/toggle 없이 자동 복구한다.

3. Player Marker Size 변경 시 다른 presentation 보존
   - Player Marker Size를 바꿔도 사용자가 설정한 Name Size와 MiniMap Marker Size의 실제 렌더링이 초기값처럼 되돌아가지 않는다.
   - player marker 변경은 MiniMap 전체 view refresh를 거치지 않고 player marker scale에만 적용한다.

4. Mini Scanner right-click menu 제거
   - Mini Scanner 우클릭 시 `현재 결과 교정` context menu를 더 이상 표시하지 않는다.
   - Mini Scanner의 left-drag, topmost, 결과 표시 및 교정 데이터 단축키 계약은 유지한다.

## Root cause / implementation

- Main Map selection handler가 `ContextIdle` sync만 예약해 fresh MiniMap creation이 같은 input turn에 stale tracker map을 읽을 수 있었다. SelectionChanged에서 synchronous `SynchronizeCore()`를 먼저 실행하고 queued reconciliation도 유지했다.
- donor standard marker refresh는 live layer를 먼저 clear한 뒤 async work를 수행한다. 후속 refresh가 해당 work를 cancel하면 empty layer가 남을 수 있다. 표시 대상 marker가 이미 로드되어 있는데 layer만 일정 시간 비면 another refresh를 시작하지 않고 loaded `MapMarkerDbService` data에서 standard layer를 직접 재구성한다.
- Player Marker Size는 donor whole-view update를 사용하지 않고 `PlayerMarkerScale`만 직접 변경하는 product-owned isolated path로 분리했다.
- Mini Scanner의 XAML `Border.ContextMenu`와 전용 modal correction partial을 제거했다.

## Completed verification

Strong runtime candidate head before release identity bump:

```text
2b75579411c8c8aeab804213342206e3c913a9be
CI: 33344938066 SUCCESS
Shutdown Race CI: 33344938060 SUCCESS
Documentation Consistency: 33344938059 SUCCESS
478/478 deterministic tests PASS
Release build PASS
Windows x64 self-contained publish PASS
actual published EXE smoke PASS
graceful shutdown PASS
release package audit PASS
artifact upload PASS
```

Actual published EXE smoke evidence included:

```text
first-minimap-creation-boundary=ok
actual-transit-marker-render=ok
player-marker-size-isolated=ok
standard-marker-direct-recovery=ok
mini-scanner-context-menu=none
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

## Release identity progress

- Desktop version bumped to **1.11.4**.
- `packaging/FIRST_RUN_KO.txt` first line and maintenance summary updated to v1.11.4.
- `docs/RELEASE_NOTES_V1.11.4.md` created.
- schemas and pinned Map donor remain unchanged from v1.11.3.

## Current step

Run the complete gate again on the final v1.11.4 exact PR head containing release identity and release notes, then convert/recreate the PR as Ready if necessary.

## Remaining

- final exact-head CI / Shutdown Race / Documentation Consistency and actual published EXE smoke.
- ready PR / main merge.
- exact-main validation and exact-main release artifact verification.
- automatic v1.11.4 stable release.
- public latest release / tag / `Junhyun-Helper.zip` / `SHA256SUMS.txt` readback and digest verification.
- release-state docs finalization, documentation-only main validation, and `ACTIVE_WORK: NONE` closure.
