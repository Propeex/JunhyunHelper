# MAP IMPLEMENTATION — 지도 시스템 구현 구조

기록일: **2026-08-09**

상태: `IMPLEMENTED / AUTOMATED VERIFICATION IN PROGRESS / WINDOWS VISUAL TEST PENDING`

관련 제품 계약:

- `docs/MAP_PRODUCT_DESIGN.md`
- `docs/MAP_DATA_SOURCE_ANALYSIS.md`

## 1. 시스템 경계

지도 시스템은 세 종류의 상태를 분리합니다.

```text
Game Content
- json.tarkov.dev Map gameplay marker facts
- Quest objective possibleLocations / zones

Map Presentation Content
- Tarkov.dev layout metadata
- CC BY-NC-SA SVG map artwork
- Tarkov.dev marker icon PNG cache

User / Runtime State
- marker visibility preferences
- custom markers
- screenshot folder
- last map/floor
- current player position
- optional player trail
- MiniMap visibility/runtime view
```

Game Content update가 User marker나 사용자 설정을 덮어쓰지 않습니다.

## 2. Content schema v4

`ContentSnapshotStore.CurrentSchemaVersion = 4`.

v4 추가 canonical facts:

- `MapMarkerDefinition`
- Quest `MapLocationData`
  - `possibleLocations`
  - `zones`
  - outline / top / bottom

이전 v3 Game Content는 온라인 source에서 한 번 재구축합니다.

`user.db`는 독립되어 있으므로 Profile / Quest progress / Hideout / Inventory는 유지합니다.

## 3. Dynamic Map marker importer

`TarkovMapMarkerImporter`

현재 imports:

- PMC / Scav / shared extract
- Transit
- PMC / Scav / Sniper Scav spawn
- Boss / special AI category
- hazard
- lock
- switch
- stationary weapon
- BTR stop
- loot container
- loose loot

원천에 좌표가 없거나 유효하지 않으면 가짜 좌표를 만들지 않습니다.

## 4. Quest location importer

기존 `QuestObjective`는 Map ID만 보존했으나 v4에서 실제 지도 geometry를 보존합니다.

```text
possibleLocations
→ MapId + one or more world positions

zones
→ MapId + center position + outline + top/bottom
```

지도 UI는 이 데이터가 존재하는 Current Quest objective만 marker/zone으로 그립니다.

정확한 좌표가 없는 Current Quest는 좌측 Map Quest 목록에는 남을 수 있으나 `정확한 위치 없음`으로 표시하며 임의 marker를 만들지 않습니다.

## 5. Map layout metadata

`TarkovMapLayoutCatalogClient`

원천:

```text
https://raw.githubusercontent.com/the-hideout/tarkov-dev/refs/heads/main/src/data/maps.json
```

imports:

- normalized physical Map key
- alt Map aliases
- transform
- coordinateRotation
- bounds / svgBounds
- min/max zoom
- base SVG layer
- floor SVG layers
- floor height extents
- floor-specific X/Z bounds
- author / authorLink

Floor detection은 height만 쓰지 않습니다. Ground Zero 등 지역 한정 floor가 있으므로 **X/Z bounds + Y height**를 함께 사용합니다.

## 6. Map artwork candidate cache

`MapAssetCacheService`

root:

```text
%LocalAppData%/JunhyunHelper/map-cache
```

slots:

```text
active/
candidate/
previous/
```

update:

```text
layout metadata download
→ matching/semantic validation
→ SVG download
→ XML/SVG validation
→ layouts.json
→ marker icon best-effort download/PNG validation
→ candidate validation
→ active 교체
```

핵심 원칙:

- SVG/layout candidate가 깨지면 previous active Map asset을 유지
- 개별 marker icon 실패는 Map 전체를 실패시키지 않음
- icon 실패 시 UI fallback symbol을 사용
- Game Content 자체가 정상이라면 temporary Map artwork 장애 때문에 Quest/Ammo/etc update를 롤백하지 않음

## 7. Data update integration

`TarkovContentUpdateService`는 canonical content candidate를 먼저 정상 활성화한 뒤 Map presentation asset update를 supplemental 단계로 실행합니다.

```text
canonical candidate activate
→ Map layout/SVG/icon candidate update
→ ContentActivated(gameMode, content)
```

Map asset update 실패는 warning이며 기존 Map cache를 유지합니다.

사용자에게는 동일한 `데이터 업데이트` action 하나로 보입니다.

## 8. Map Desktop UI

`MapPage`

구성:

- Map dropdown
- multi-floor selector
- MiniMap toggle
- screenshot folder / auto-detect
- view reset
- persistent wrapping marker checkbox panel
- Current Quest left list
- interactive SVG Map surface
- marker / zone / custom marker / player / trail layers
- map artwork attribution

Map interaction:

- wheel zoom
- drag pan
- reset
- marker click detail
- Quest list ↔ Quest Map marker focus
- Quest tab navigation
- right-click empty Map space → custom marker

Marker 위 right-click은 custom marker 생성으로 bubbling되지 않게 차단합니다.

## 9. Marker visual assets

Tarkov.dev current interactive Map icon PNG를 Map asset update 때 내려받습니다.

예:

- extract_pmc / extract_scav / extract_shared / extract_transit
- spawn_pmc / spawn_scav / spawn_sniper_scav / spawn_boss / spawn_rogue
- hazard / lock / switch / stationarygun / btr_stop
- container_crate / loose_loot
- quest_objective / quest_item
- player-position

파일이 없으면 종류별 fallback badge를 그립니다.

User marker는 Tarkov.dev icon이 아니라 사용자가 고른 색상의 custom marker입니다.

## 10. User Map persistence

현재 별도 recoverable user files:

```text
%LocalAppData%/JunhyunHelper/map-settings.json
%LocalAppData%/JunhyunHelper/map-markers.json
```

저장:

- screenshot path
- last map/floor
- marker category visibility
- Quest/User/Player marker visibility
- trail ON/OFF
- custom marker ID / Map / floor / name / color / world position

이 데이터는 Game Content update 대상이 아닙니다.

## 11. Screenshot position tracking

`MapScreenshotTracker`

- `FileSystemWatcher`로 PNG 생성/변경 감지
- debounce
- EFT filename에서 X / Y / Z / quaternion parse
- quaternion → heading
- Map transform → screen location
- player marker update
- spatial floor auto selection
- optional trail append
- MiniMap update

OCR이나 이미지 분석은 사용하지 않습니다.

## 12. Raid Map auto switch

`RaidMapWatcher`

기본 log root:

```text
%LocalAppData%/Battlestate Games/EFT/Logs
```

감지:

- `TRACE-NetworkGameCreate ... Location:`
- `scene preset path:maps/...bundle`

known alias만 canonical Map group으로 resolve합니다. 모르는 alias는 추측하지 않습니다.

watcher 시작 시 기존 log EOF부터 보기 때문에 과거 Raid를 현재 Raid로 오인하지 않습니다.

## 13. MiniMap

`MiniMapWindow`

- separate always-on-top WPF window
- Map tab button으로 ON/OFF
- current physical Map / floor / marker visibility 공유
- player position + heading 공유
- optional trail 공유
- player position이 있으면 player-centered tracking viewport
- wheel로 MiniMap tracking zoom 조절
- player position이 아직 없으면 전체 Map fit
- marker/player visual은 Map 확대에 따라 과도하게 커지지 않도록 inverse scale

Legacy Tarkov-Helper MiniMap의 player-follow 핵심 사용 경험을 새 canonical Map 좌표계로 다시 구현한 것입니다.

## 14. License / attribution

사용자가 준현 헬퍼의 비상업적 도우미 사용과 CC BY-NC-SA 4.0 조건을 수용했습니다.

Map 화면에 항상 다음 정보를 표시합니다.

- map author가 metadata에 있으면 author
- `tarkov-dev-svg-maps`
- `CC BY-NC-SA 4.0`

지도 시스템은 game memory/packet을 읽는 radar/ESP로 확장하지 않습니다.

## 15. 자동 검증

추가 tests:

- dynamic Map marker import classification
- Quest possibleLocations / zones import
- Map layout alt-map matching
- spatial floor extents preservation
- existing entire Core/Application regression suite

CI 최종 번호는 code-complete checkpoint 후 `docs/STATE.md`에 기록합니다.

## 16. Windows 실사용에서 반드시 확인할 항목

자동화 테스트만으로 WPF 지도의 시각/감각은 확정하지 않습니다.

첫 Windows Map test에서 특히 확인:

- 각 physical Map이 dropdown에 정상 표시
- SVG 좌표와 marker 좌표가 실제 위치에 맞는지
- Ground Zero/Labs/Streets 등 multi-floor layer 표시
- marker icon 크기/가독성
- checkbox 배치와 혼잡도
- Current Quest 목록/marker 연동
- custom marker 생성/수정/삭제
- screenshot player marker 위치/heading
- spatial auto-floor 전환
- Raid log Map 자동 전환
- MiniMap player-follow zoom/가독성
- trail ON/OFF

실사용 결과에 따라 좌표/층/marker UI를 수정하되, 검증되지 않은 수동 좌표를 canonical source에 추가하지 않습니다.
