# MAP PRODUCT DESIGN — 지도 탭 제품 설계

기록일: **2026-08-09**

상태: `IMPLEMENTED / CODE-COMPLETE CI PASSED / WINDOWS VISUAL TEST PENDING`

이 문서는 새 준현 헬퍼 지도 탭의 확정 제품 의도를 기록합니다. 기존 `Propeex/Tarkov-Helper` 지도 구현은 UX/알고리즘 참고 자료일 뿐 새 제품 요구사항의 권위 원천이 아닙니다.

실제 구현 구조와 검증 항목은 `docs/MAP_IMPLEMENTATION.md`를 따릅니다.

## 1. 핵심 목적

지도 탭은 단순 정적 지도 뷰어가 아니라 다음 세 가지를 한 화면에서 제공하는 플레이 보조 화면입니다.

1. 최신 Map Content를 기반으로 한 interactive 지도/마커
2. 현재 Profile의 진행 중 Quest와 연결된 지도 목표 정보
3. 게임 스크린샷 파일명 좌표를 통한 현재 위치 표시와 게임 위 MiniMap

Map Content와 지도 asset은 일반 `데이터 업데이트` 흐름에서 갱신합니다. 새 지도/마커/좌표/층 데이터가 검증을 통과하지 못하면 마지막 정상 지도를 보호합니다.

## 2. 기본 화면

`CONFIRMED / IMPLEMENTED`

- 지도 탭 안에서 dropdown으로 지도를 선택
- 선택한 지도를 중앙 주 작업 영역에 표시
- 지도는 mouse drag pan / wheel zoom / view reset 지원
- 다층 지도는 층 selector 제공
- screenshot 위치의 X/Z + Y(height)로 현재 층을 결정할 수 있으면 자동 전환
- 지도/marker 상태는 Game Content와 User/UI settings를 분리해서 관리

기본 배치:

```text
┌──────────────────────────────────────────────────────────────┐
│ 지도 선택 │ 층 │ 미니맵 │ 스크린샷 경로/추적 상태          │
├──────────────┬───────────────────────────────────────────────┤
│ 진행중 퀘스트 │                                               │
│              │                지도                           │
│              │                                               │
│              │          marker / player 위치                │
│              │                                               │
│              │          [즉시 접근 marker checkbox]         │
└──────────────┴───────────────────────────────────────────────┘
```

marker toggle을 별도 설정 화면 깊숙이 숨기지 않습니다.

## 3. Marker 표시와 접근성

`CONFIRMED / IMPLEMENTED`

marker on/off는 **지도 화면에서 즉시 접근 가능한 persistent checkbox control**로 제공합니다. 창 폭에 따라 checkbox가 다음 줄로 자연스럽게 줄바꿈됩니다.

주요 category:

- PMC 탈출구
- Scav 탈출구
- 공용/특수 탈출구
- Transit
- PMC 스폰
- Scav 스폰
- Sniper Scav
- Boss / 특수 AI
- Quest item / objective / zone
- 위험 구역 / 포격 구역
- 잠금 지점
- 레버 / 스위치
- 고정 화기
- BTR 정류장
- loot container
- loose loot
- 사용자 marker
- player position
- player 이동 경로

Tarkov.dev interactive Map에서 사용하는 marker PNG를 데이터 업데이트 때 함께 내려받습니다. 개별 icon 다운로드 실패는 Map 전체 실패가 아니며 해당 marker는 의미가 구분되는 fallback symbol을 사용합니다.

대량 marker인 loot container / loose loot는 기본 OFF입니다. 일반적인 레이드 정보 marker와 Quest/User/Player marker는 기본 ON이며 사용자의 checkbox 선택을 UI preference로 저장합니다.

## 4. Quest marker

`SOURCE VERIFIED / CONFIRMED / IMPLEMENTED`

현재 `json.tarkov.dev/<game-mode>/tasks` objective에는 지도 표시 가능한 정보가 존재합니다.

- `possibleLocations` → Quest item 등 가능한 위치와 `positions`
- `zones` → 특정 objective zone의 map / position / outline / top / bottom

Tarkov.dev의 현재 interactive Map도 이 두 데이터를 이용해 `quest_item`과 `quest_objective` marker를 생성합니다.

따라서 **퀘스트 지도 marker는 별도 수작업 좌표 DB 없이 데이터 업데이트로 갱신**합니다.

원칙:

- 정확한 좌표가 있는 objective만 marker 생성
- 좌표가 없는 objective에 가짜 위치를 만들지 않음
- 위치가 여러 개인 objective는 복수 marker 허용
- zone outline이 있으면 영역 표현
- marker click → 관련 Quest/objective 정보

Content schema v4부터 canonical `QuestObjective`가 Map ID뿐 아니라 `possibleLocations`와 `zones`의 실제 world geometry를 보존합니다.

## 5. 좌측 Quest 목록

`CONFIRMED / IMPLEMENTED`

좌측 목록은 **현재 선택 Map과 관련된 현재 진행 중 Quest만** 표시합니다.

- `QuestAvailabilityState.Current`만 기본 대상
- 현재 Quest의 `MapId`, objective MapIds, possibleLocations/zones 중 선택 Map과 관련된 것을 합쳐 판정
- 정확한 위치 marker가 없는 현재 Quest도 선택 Map 관련성이 확인되면 목록에는 표시
- marker가 없는 Quest는 임의 위치로 이동시키지 않고 `정확한 위치 없음` 상태를 표시
- Quest 목록 click → 위치가 있으면 해당 marker/zone으로 지도 focus
- Quest marker click → 좌측 해당 Quest 선택
- `퀘스트 탭에서 보기` → 기존 Quest 탭의 해당 Quest로 이동

Locked/Completed Quest를 이 좌측 목록에 섞지 않습니다. Profile 변경 시 현재 Quest workspace도 다시 읽어 지도 목록을 갱신합니다.

## 6. User marker

`CONFIRMED / IMPLEMENTED`

- 지도 빈 공간 right-click → custom marker 추가
- 이름 수정
- 색상 수정
- 삭제
- 현재 physical Map과 stable SVG floor/layer ID 귀속 보존
- content update와 독립적인 사용자 저장소에 저장
- marker toggle로 전체 표시 ON/OFF

사용자가 요청하지 않은 custom icon/category 확장 기능은 현재 범위에 넣지 않습니다.

## 7. MiniMap

`CONFIRMED / IMPLEMENTED`

MiniMap은 **기존 Tarkov-Helper처럼 게임 화면 위에 띄울 수 있는 always-on-top 별도 overlay window**입니다.

- 지도 탭의 `미니맵` button으로 즉시 ON/OFF
- 현재 선택 Map과 동기화
- 현재 floor와 player position/heading 동기화
- 지도 탭의 marker visibility 상태 공유
- player 위치가 있으면 player 중심으로 지도가 따라오는 작은 지도 view
- player 위치가 아직 없으면 전체 지도 fit
- MiniMap 위 mouse wheel로 추적 확대/축소
- 이동 경로 checkbox 상태 공유

기존 MiniMap의 핵심 player-follow 사용 경험을 새 canonical Map 좌표계로 다시 구현했습니다. 사용자가 요청하지 않은 복잡한 hotkey/click-through 설정은 이번 범위에 추가하지 않았습니다.

## 8. Screenshot 위치 추적

`CONFIRMED / IMPLEMENTED`

지도 탭에서 Escape from Tarkov screenshot folder를 지정하거나 자동 탐지할 수 있습니다.

```text
EFT screenshot 생성
→ FileSystemWatcher 감지
→ screenshot filename parse
→ X / Y(height) / Z + quaternion 추출
→ quaternion에서 heading 계산
→ 현재 Map layout transform 적용
→ player marker 이동
→ X/Z 구역 + Y height로 floor 자동 전환
→ MiniMap 동기화
```

중요:

- 이미지 OCR이 아니라 **screenshot filename metadata**를 사용
- watcher Created/Changed 중복 event debounce
- filename format 불일치 시 추측하지 않음
- current position/trail은 session runtime state이며 Game Content/User Progress의 권위 사실이 아님

## 9. Raid Map 자동 전환

`CONFIRMED / IMPLEMENTED`

기존 Tarkov-Helper에 있던 EFT game log 감지를 새 구조로 다시 구현했습니다.

- 기본 EFT log folder 자동 탐지
- raid/scene log에서 Map alias 감지
- canonical physical Map group으로 안전하게 resolve
- raid 시작 등으로 Map이 확정되면 지도 탭과 MiniMap을 해당 Map으로 자동 전환
- watcher 시작 시 기존 log의 EOF부터 보기 때문에 과거 Raid를 현재 Raid로 오인하지 않음
- 알 수 없는 Map alias는 추측해서 다른 Map으로 보내지 않음

이 기능은 screenshot filename에 Map 이름이 없는 경우 수동 dropdown 선택 부담을 줄입니다.

## 10. Player 이동 경로

`CONFIRMED / IMPLEMENTED`

- 기본 OFF
- 지도 화면의 즉시 접근 가능한 `이동 경로` checkbox로 ON/OFF
- ON이면 screenshot으로 갱신된 이전 player 위치를 선으로 연결
- `경로 지우기` action 제공
- OFF로 바꿔도 현재 player marker 자체는 유지
- trail은 session runtime data이며 게임 데이터 업데이트 대상이 아님

## 11. 다층 지도

`CONFIRMED / IMPLEMENTED`

Tarkov.dev Map metadata의 layer/height/region extent를 사용합니다.

- 다층 Map만 floor selector 표시
- 단층 Map에서는 숨김
- floor 식별자는 업데이트 순서에 영향을 받는 `layer-0` 같은 번호가 아니라 stable SVG layer ID를 사용
- screenshot 자동 floor 선택은 단순 높이만 보지 않고 **X/Z spatial bounds + Y height**를 함께 사용
- 사용자가 수동으로 floor를 바꿀 수 있음

## 12. 기존 Tarkov-Helper에서 참고한 구현

`REFERENCE ONLY`

참고한 아이디어:

- map dropdown
- pan / zoom / reset view
- floor selector + elevation/spatial auto switch
- screenshot folder browse / auto detection
- screenshot watcher + filename coordinate parser
- player heading
- Quest marker + 좌측 Quest list
- extract/marker category toggles
- custom marker add/edit/delete
- always-on-top MiniMap + player-follow
- raid log 기반 automatic Map switch
- optional player trail

기존 코드의 정적 `map_configs.json`, 수동 marker DB, 과거 Quest location DB는 새 제품의 source of truth로 승계하지 않습니다.

## 13. 구현 경계

이번 Map 기능에 추가하지 않은 항목:

- 사용자가 요청하지 않은 waypoint routing/navigation
- 실시간 게임 메모리 읽기
- radar/ESP 형태의 다른 플레이어/AI 실시간 위치 추적
- 네트워크 패킷 분석
- custom marker용 복잡한 icon editor/category editor

현재 위치 기능은 게임이 정상적으로 생성하는 screenshot filename metadata만 사용합니다.

## 14. 데이터 구조 원칙

```text
json.tarkov.dev maps
→ dynamic Map marker facts

json.tarkov.dev tasks
→ Quest objective locations/zones

Tarkov.dev public map metadata
→ bounds / transform / rotation / floor layout

tarkov-dev-svg-maps (CC BY-NC-SA 4.0)
→ visual map artwork

Tarkov.dev interactive marker PNG
→ visual marker assets
```

Map gameplay facts와 visual layout/artwork는 별도로 관리합니다.

Map layout/SVG/icon update는 Game Content update와 같은 사용자 `데이터 업데이트` 동작에서 실행하되, asset/layout candidate가 실패하면 마지막 정상 Map asset set을 유지합니다. 개별 marker icon 실패는 해당 icon만 fallback으로 처리합니다.

## 15. 라이선스

`CONFIRMED BY USER / IMPLEMENTED`

준현 헬퍼는 비상업적 플레이 도우미이며 Tarkov.dev SVG Map의 CC BY-NC-SA 4.0 조건을 수용합니다.

- Map 화면에 author(제공될 경우) / `tarkov-dev-svg-maps` / `CC BY-NC-SA 4.0` 표시
- non-commercial 유지
- 필요한 share-alike 조건 준수
- radar / ESP / cheat client / pixel-bot 등 금지 용도에 사용하지 않음

## 16. 검증 상태

코드 완료 checkpoint:

```text
CI run: 31293451255
Windows Release Desktop build: success
full automated tests: success
Windows x64 publish: success
ZIP/package creation: success
artifact upload: success
```

자동 검증은 통과했습니다. 실제 Windows에서 SVG 좌표 정합성, marker 위치/크기, 다층 지도, screenshot 위치/heading, raid 자동 전환, MiniMap player-follow 감각은 사용자 실사용 테스트로 최종 검증합니다.
