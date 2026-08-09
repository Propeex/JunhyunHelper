# MAP PRODUCT DESIGN — 지도 탭 제품 설계

기록일: **2026-08-09**

상태: `PRODUCT INTENT CONFIRMED / IMPLEMENTATION STARTING`

이 문서는 새 준현 헬퍼 지도 탭의 확정 제품 의도를 기록합니다. 기존 `Propeex/Tarkov-Helper` 지도 구현은 UX/알고리즘 참고 자료일 뿐 새 제품 요구사항의 권위 원천이 아닙니다.

## 1. 핵심 목적

지도 탭은 단순 정적 지도 뷰어가 아니라 다음 세 가지를 한 화면에서 제공하는 플레이 보조 화면입니다.

1. 최신 Map Content를 기반으로 한 interactive 지도/마커
2. 현재 Profile의 진행 중 Quest와 연결된 지도 목표 정보
3. 게임 스크린샷 파일명 좌표를 통한 현재 위치 표시와 게임 위 MiniMap

Map Content와 지도 asset은 일반 `데이터 업데이트` 흐름에서 갱신합니다. 새 지도/마커/좌표/층 데이터가 검증을 통과하지 못하면 마지막 정상 지도를 보호합니다.

## 2. 기본 화면

`CONFIRMED`

- 지도 탭 안에서 dropdown으로 지도를 선택
- 선택한 지도를 중앙 주 작업 영역에 표시
- 지도는 mouse drag pan / wheel zoom / view reset 지원
- 다층 지도는 층 selector 제공
- screenshot 높이 좌표로 현재 층을 결정할 수 있으면 자동 전환
- 지도/marker 상태는 Game Content와 User/UI settings를 분리해서 관리

권장 기본 배치:

```text
┌──────────────────────────────────────────────────────────────┐
│ 지도 선택 │ 층 │ 미니맵 │ 스크린샷 경로/추적 상태          │
├──────────────┬───────────────────────────────────────────────┤
│ 진행중 퀘스트 │                                               │
│              │                지도                           │
│              │                                               │
│              │          marker / player 위치                │
│              │                                               │
│              │                      [즉시 marker toggle]     │
└──────────────┴───────────────────────────────────────────────┘
```

정확한 pixel layout은 구현 중 사용성에 맞춰 조정할 수 있으나, marker toggle을 별도 설정 화면 깊숙이 숨기지 않습니다.

## 3. Marker 표시와 접근성

`CONFIRMED`

marker on/off는 **지도 화면에서 즉시 접근 가능한 persistent checkbox control**로 제공합니다.

주요 category:

- PMC 탈출구
- Scav 탈출구
- 공용/특수 탈출구
- Transit
- PMC 스폰
- Scav 스폰
- Sniper Scav
- Boss / 특수 AI
- Quest item
- Quest objective / zone
- 위험 구역
- 잠금 지점
- 레버 / 스위치
- 고정 화기
- BTR 정류장
- loot container
- loose loot
- 사용자 marker
- player position
- player 이동 경로

각 marker category는 의미에 맞는 시각 icon을 사용합니다.

대량 marker인 loot container / loose loot는 기본 OFF로 둡니다. 일반적인 레이드 정보 marker와 Quest/User/Player marker는 기본 ON으로 시작하되 사용자의 checkbox 선택을 UI preference로 저장합니다.

## 4. Quest marker

`SOURCE VERIFIED / CONFIRMED`

현재 `json.tarkov.dev/<game-mode>/tasks` objective에는 지도 표시 가능한 정보가 존재합니다.

- `possibleLocations` → Quest item 등 가능한 위치와 `positions`
- `zones` → 특정 objective zone의 map / position / outline / top / bottom

Tarkov.dev의 현재 interactive Map도 이 두 데이터를 이용해 `quest_item`과 `quest_objective` marker를 생성합니다.

따라서 **퀘스트 지도 marker는 별도 수작업 좌표 DB 없이 데이터 업데이트로 갱신**합니다.

원칙:

- 정확한 좌표가 있는 objective만 marker 생성
- 좌표가 없는 objective에 가짜 위치를 만들지 않음
- 위치가 여러 개인 objective는 복수 marker 허용
- zone outline이 있으면 영역 표현 가능
- marker click → 관련 Quest/objective 정보

현재 Core `QuestObjective`는 MapIds만 보존하므로 구현에서 canonical objective-location model을 확장합니다.

## 5. 좌측 Quest 목록

`CONFIRMED`

좌측 목록은 **현재 선택 Map과 관련된 현재 진행 중 Quest만** 표시합니다.

- `QuestAvailabilityState.Current`만 기본 대상
- 현재 Quest의 `MapId`, objective MapIds, possibleLocations/zones 중 선택 Map과 관련된 것을 합쳐 판정
- 정확한 위치 marker가 없는 현재 Quest도 선택 Map 관련성이 확인되면 목록에는 표시
- marker가 없는 Quest는 임의 위치로 이동시키지 않고 `정확한 위치 없음` 상태를 표시
- Quest 목록 click → 위치가 있으면 해당 marker/zone으로 지도 focus
- Quest marker click → 좌측 해당 Quest 선택
- 별도 action → 기존 Quest 탭의 해당 Quest로 이동

Locked/Completed Quest를 이 좌측 목록에 섞지 않습니다.

## 6. User marker

`CONFIRMED`

- 지도에서 right-click → custom marker 추가
- 이름 수정
- 색상 수정
- 삭제
- 현재 Map과 floor/layer 귀속 보존
- content update와 독립적인 사용자 저장소에 저장
- marker toggle로 전체 표시 ON/OFF

사용자가 요청하지 않은 custom icon/category 확장 기능은 현재 범위에 넣지 않습니다.

## 7. MiniMap

`CONFIRMED`

MiniMap은 **기존 Tarkov-Helper처럼 게임 화면 위에 띄울 수 있는 always-on-top 별도 overlay window**입니다.

- 지도 탭의 `미니맵` button으로 즉시 ON/OFF
- 현재 선택 Map과 동기화
- 현재 floor와 player position을 동기화
- 지도 탭의 marker visibility 상태를 가능한 범위에서 공유
- player 중심의 작은 지도 view

기존 MiniMap 구현은 사용성이 좋았던 참고 구현으로 봅니다. 단, 과도한 설정 UI를 그대로 승계하지 않고 핵심 조작부터 구현합니다.

## 8. Screenshot 위치 추적

`CONFIRMED / LEGACY MECHANISM VERIFIED`

지도 탭에서 Escape from Tarkov screenshot folder를 지정할 수 있습니다.

```text
EFT screenshot 생성
→ FileSystemWatcher 감지
→ screenshot filename parse
→ X / Y(height) / Z + quaternion 추출
→ quaternion에서 heading 계산
→ 현재 Map layout transform 적용
→ player marker 이동
→ 필요 시 floor 자동 전환
→ MiniMap 동기화
```

중요:

- 이미지 OCR이 아니라 **screenshot filename metadata**를 사용
- watcher Created/Changed 중복 event debounce
- 파일 쓰기 완료 후 처리
- filename format 불일치 시 추측하지 않음
- current position/trail은 session runtime state이며 Game Content/User Progress의 권위 사실이 아님

## 9. Raid Map 자동 전환

`CONFIRMED`

기존 Tarkov-Helper에 있던 EFT game log 감지를 새 구조로 다시 구현합니다.

- 기본 EFT log folder 자동 탐지
- raid/scene log에서 Map alias를 감지
- canonical Map/layout key로 안전하게 resolve
- raid 시작/Transit 등으로 Map이 확정되면 지도 탭과 MiniMap을 해당 Map으로 자동 전환
- 알 수 없는 Map alias는 추측해서 다른 Map으로 보내지 않음

이 기능은 screenshot filename에 Map 이름이 없는 경우 수동 dropdown 선택 부담을 줄이는 역할을 합니다.

## 10. Player 이동 경로

`CONFIRMED`

- 기본 OFF
- 지도 화면의 즉시 접근 가능한 `이동 경로` checkbox로 ON/OFF
- ON이면 screenshot으로 갱신된 이전 player 위치를 선/점으로 연결
- `경로 지우기` action 제공 가능
- OFF로 바꿔도 현재 player marker 자체는 유지
- trail은 session runtime data이며 게임 데이터 업데이트 대상이 아님

## 11. 다층 지도

`CONFIRMED AS REQUIRED MAP BEHAVIOR`

Tarkov.dev Map metadata의 layer/height range를 사용합니다.

- 다층 Map만 floor selector 표시
- 단층 Map에서는 숨김
- marker의 높이/extent가 현재 floor와 명확히 맞지 않으면 숨김
- screenshot Y(height)가 특정 floor range에 들어오면 자동으로 해당 floor 선택
- 사용자가 수동으로 floor를 바꿀 수 있음

## 12. 기존 Tarkov-Helper에서 참고할 구현

`REFERENCE ONLY`

유지할 가치가 확인된 아이디어:

- map dropdown
- pan / zoom / reset view
- floor selector + elevation auto switch
- screenshot folder browse / auto detection
- screenshot watcher + filename coordinate parser
- player heading
- Quest marker + 좌측 Quest drawer/list
- extract/marker category toggles
- custom marker add/edit/delete
- always-on-top MiniMap
- raid log 기반 automatic Map switch
- optional player trail

기존 코드의 정적 `map_configs.json`, 수동 marker DB, 과거 Quest location DB는 새 제품의 source of truth로 승계하지 않습니다.

## 13. 구현 경계

이번 Map 기능에서 임의로 추가하지 않는 항목:

- 사용자가 요청하지 않은 waypoint routing/navigation
- 실시간 게임 메모리 읽기
- radar/ESP 형태의 다른 플레이어/AI 실시간 위치 추적
- 네트워크 패킷 분석
- custom marker용 복잡한 icon editor/category editor

현재 위치 기능은 게임이 정상적으로 생성하는 screenshot filename metadata를 사용합니다.

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
```

Map gameplay facts와 visual layout/artwork는 별도로 관리합니다.

Map layout/SVG update는 Game Content update와 같은 사용자 `데이터 업데이트` 동작에서 실행하되, asset/layout candidate가 실패하면 마지막 정상 Map asset set을 유지합니다.

## 15. 라이선스

`CONFIRMED BY USER`

준현 헬퍼는 비상업적 플레이 도우미이며 Tarkov.dev SVG Map의 CC BY-NC-SA 4.0 조건을 수용합니다.

- attribution 제공
- non-commercial 유지
- 필요한 share-alike 조건 준수
- radar / ESP / cheat client / pixel-bot 등 금지 용도에 사용하지 않음
