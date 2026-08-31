# 준현 헬퍼 v1.11.4

v1.11.4는 v1.11.3 이후 실사용에서 확인된 MiniMap lifecycle/marker 표시 회귀와 Mini Scanner 우클릭 UX를 수정하는 PATCH 유지보수 릴리즈입니다.

## MiniMap

### 최초 표시 지도 동기화

Main Map에서 지도를 변경한 직후 MiniMap을 처음 생성하면 이전 지도 상태를 첫 프레임에서 읽을 수 있던 dispatcher timing race를 수정했습니다.

- Main Map의 실제 선택 변경 시 `MapTrackerService`와 MiniMap product state를 동기적으로 먼저 갱신합니다.
- 기존 queued reconciliation도 유지해 같은 dispatcher cycle의 후속 donor 동작까지 다시 맞춥니다.
- 이미 생성된 MiniMap 재표시뿐 아니라 **한 번도 생성되지 않은 fresh first-create path**도 실제 published EXE smoke로 검증합니다.

### Transit 및 전체 extract marker 경로 점검

MiniMap의 PMC / Scav / Transit extract marker 데이터, 필터, projection, rendering 경로를 다시 점검했습니다.

- Transit은 `ExtractFaction.Transit`과 `ShowTransits` 상태를 사용합니다.
- Main Map의 실제 PMC / Scav / Transit checkbox 상태가 MiniMap 렌더링 상태와 연결됩니다.
- packaged extract data에서 실제 Transit이 존재하는 맵을 runtime smoke가 자동으로 찾아, 예상 grouped Transit 수와 실제 MiniMap의 렌더링된 Transit 마커 수가 같은지 확인합니다.
- 따라서 단순히 Transit checkbox가 존재하거나 값이 true인지가 아니라 **실제 Transit marker visual 생성까지** 릴리즈 게이트가 검증합니다.

### 일반 마커가 잠깐 보였다가 사라지는 문제

기존 donor marker refresh는 새 refresh가 시작될 때 live marker layer를 먼저 비운 뒤 비동기 로딩을 수행합니다. 그 비동기 작업이 후속 refresh에 의해 취소되면 빈 layer가 남을 수 있었습니다.

v1.11.4에서는 표시 대상 marker data가 이미 메모리에 로드되어 있는데 standard marker layer만 일정 시간 비어 있는 경우:

- 또 다른 `QueueMarkerRefresh()`를 시작하지 않고,
- 이미 로드된 `MapMarkerDbService` 데이터에서 standard marker layer만 직접 재구성하고,
- 현재 floor / filter / marker scale presentation을 다시 적용합니다.

이 복구 자체가 새 clear/cancel race를 만들지 않도록 설계했습니다.

### Player Marker Size 격리

Player Marker Size 변경이 MiniMap 전체 view update를 호출하면서 unrelated marker/name transform을 다시 덮을 수 있던 경로를 제거했습니다.

이제 Player Marker Size 변경은:

- MiniMap player marker scale만 변경하고,
- 저장된 player marker setting만 갱신하며,
- Name Size / MiniMap Marker Size / 일반·퀘스트·탈출구 marker presentation은 건드리지 않습니다.

## Mini Scanner

Mini Scanner의 우클릭 context menu를 제거했습니다.

- 우클릭 시 `현재 결과 교정` 메뉴가 더 이상 표시되지 않습니다.
- 좌클릭 드래그 이동은 유지됩니다.
- Mini Scanner topmost/result display 동작은 유지됩니다.
- `교정 데이터 추가` global hotkey를 통한 evidence 저장 계약은 유지됩니다.

## 회귀 검증

v1.11.4 deterministic suite는 **478 tests**입니다.

Release 후보는 다음을 통과해야 합니다.

- Desktop Release build
- 478/478 deterministic tests
- Windows x64 self-contained single-file publish
- actual published EXE Product UI / Map / Factory / MiniMap / Scanner smoke
- graceful shutdown 및 portable-root cleanliness
- release package / SHA256 manifest audit
- Shutdown Race CI
- Documentation Consistency

특히 actual published EXE smoke는 다음 evidence를 직접 요구합니다.

```text
first-minimap-creation-boundary=ok
actual-transit-marker-render=ok
player-marker-size-isolated=ok
standard-marker-direct-recovery=ok
mini-scanner-context-menu=none
```

기존 MiniMap A → B 재표시 검증도 유지합니다.

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

## 호환성

v1.11.4는 PATCH 유지보수 릴리즈이며 v1.11.3의 제품 데이터 계약을 변경하지 않습니다.

- content DB schema: unchanged
- user DB schema: unchanged
- Scanner display settings schema: unchanged
- Scanner catalog schema: unchanged
- pinned Map donor: `SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67`

실제 Tarkov 사용자 PC 검증은 자동화 release gate와 별도로 계속 실사용 회귀 증거로 취급합니다.
