# MAP PRODUCT DESIGN — 지도 탭 제품 설계

기록일: **2026-08-09**

상태: `INTENT_CAPTURED / QUEST LOCATION SOURCE VERIFIED / OPEN DECISIONS BEFORE IMPLEMENTATION`

이 문서는 사용자가 설명한 새 준현 헬퍼의 지도 탭 의도를 기록합니다. 기존 `Propeex/Tarkov-Helper` 지도 구현은 UX/알고리즘 참고 자료일 뿐 새 제품 요구사항의 권위 원천이 아닙니다.

## 1. 핵심 목적

지도 탭은 단순 정적 지도 뷰어가 아니라 다음 세 가지를 한 화면에서 제공하는 플레이 보조 화면입니다.

1. 최신 Map Content를 기반으로 한 interactive 지도/마커
2. 현재 Profile의 Quest 진행과 연결된 지도 목표 정보
3. 게임 스크린샷 파일명 좌표를 통한 현재 위치 표시와 미니맵

Map Content는 기존 확정 정책대로 일반 `데이터 업데이트` pipeline에서 재구축합니다.

## 2. 기본 화면

`INTENT_CAPTURED`

- 지도 탭 안에서 dropdown으로 지도를 선택
- 선택한 지도를 중앙의 주된 작업 영역에 표시
- 지도는 pan / zoom 가능한 interactive view
- 다층 지도는 층 정보를 정확하게 표현해야 함
- 지도/marker 상태는 Game Content와 User/UI settings를 분리해서 관리

## 3. Marker 표시와 접근성

`INTENT_CAPTURED`

사용자는 marker on/off를 설정 화면 깊숙이 들어가서 조작하는 UX를 원하지 않습니다.

따라서 marker toggle은 **지도 화면에서 즉시 접근 가능한 persistent control**이어야 합니다.

예상 marker category:

- PMC extract
- Scav extract
- shared/special extract
- Transit
- PMC spawn
- Scav spawn
- Sniper Scav
- Boss / special AI spawn
- Quest item
- Quest objective / zone
- hazards
- locks
- lever / switch
- stationary weapon
- BTR stop
- loot container / loose loot
- user marker
- player position

각 marker category는 의미에 맞는 icon을 사용합니다.

세부 grouping/default visibility는 제품 확정 전입니다. Loot처럼 수가 많은 marker는 clutter 위험이 있으므로 기본값을 별도로 결정할 수 있습니다.

## 4. Quest marker 가능성

`SOURCE VERIFIED`

현재 `json.tarkov.dev/<game-mode>/tasks`의 objective에는 지도 표시 가능한 정보가 존재합니다.

- `possibleLocations` → quest item 등 가능한 위치와 `positions`
- `zones` → 특정 objective zone의 map/position/outline/top/bottom

Tarkov.dev의 현재 interactive Map 구현도 이 두 데이터를 이용해 `quest_item`과 `quest_objective` marker를 실제로 생성합니다.

따라서 **퀘스트 지도 marker는 별도 수작업 좌표 DB 없이 자동 갱신 가능한 기능으로 설계할 수 있습니다.**

제약:

- 모든 Map 관련 objective에 정확한 좌표가 있는 것은 아님
- 좌표/zone이 없는 objective에 임의 marker를 만들지 않음
- 위치가 여러 개인 objective는 복수 marker가 가능
- zone은 단일 점뿐 아니라 outline/높이 범위를 가질 수 있으므로 필요하면 영역으로 표현

현재 JunhyunHelper Core `QuestObjective`는 MapIds까지만 보존하고 위치 좌표를 보존하지 않으므로, 실제 Map 구현 시 canonical objective-location model을 확장해야 합니다.

## 5. 좌측 Quest 목록

`INTENT_CAPTURED / OPEN FILTER SEMANTICS`

Quest location marker 기능을 사용한다면 지도 왼쪽에 **현재 선택 Map과 관련된 Quest 목록**을 둡니다.

권장 interaction:

- Quest 목록 항목 click → 해당 Quest marker/zone으로 지도 focus
- Quest marker click → 좌측에서 해당 Quest 선택 및 objective 정보 표시
- 기존 Quest 탭으로 이동할 수 있는 link/action 제공
- 정확한 위치가 없는 Map 관련 Quest를 목록에 포함할지는 제품 결정 필요
- Locked/Active/Completed 중 어떤 상태까지 목록에 포함할지 제품 결정 필요

## 6. User marker

`INTENT_CAPTURED`

- 사용자가 지도 위 원하는 위치에 custom marker 추가 가능
- marker 이름 수정 가능
- marker 색상 수정 가능
- 삭제 가능
- Map에 귀속해서 `user.db` 또는 별도 User Progress/UI 저장소에 보존
- Game Content update로 삭제되지 않음
- 다층 지도에서는 floor/layer 귀속을 보존할 필요가 있음

기존 Tarkov-Helper의 `right-click → marker 추가`, edit/delete 구조는 재검토 가치가 있습니다.

사용자가 요청하지 않은 추가 custom icon/category system은 현재 요구사항에 넣지 않습니다.

## 7. MiniMap

`INTENT_CAPTURED / PRESENTATION MODE OPEN`

- 지도 탭에서 한 번의 button으로 MiniMap on/off
- MiniMap은 현재 선택된 지도의 **작은 동기화 view**
- player position 및 현재 표시 중인 필요한 marker를 같은 Map state에서 표현

기존 Tarkov-Helper에는 별도 always-on-top overlay MiniMap이 존재했습니다. 새 제품에서도 game overlay로 사용할지, 앱 내부 MiniMap을 의미하는지 최종 확인이 필요합니다.

기존 MiniMap에 있었던 다수 hotkey/opacity/click-through/view-mode 설정은 현재 사용자 요구사항이 아니므로 자동 승계하지 않습니다.

## 8. Screenshot 위치 추적

`INTENT_CAPTURED / LEGACY MECHANISM VERIFIED`

지도 탭에서 Escape from Tarkov screenshot folder를 지정할 수 있어야 합니다.

기존 프로그램에서 검증된 방식:

```text
EFT screenshot 생성
→ FileSystemWatcher 감지
→ screenshot filename parse
→ X / Y(height) / Z + quaternion 추출
→ quaternion에서 heading 계산
→ 선택 Map의 coordinate transform 적용
→ player marker 이동
```

중요:

- 이미지 내용 OCR이 아니라 **EFT가 screenshot filename에 기록하는 좌표/rotation metadata를 파싱**하는 방식
- watcher는 duplicate Created/Changed event를 debounce
- 파일 생성 완료를 기다린 뒤 처리
- filename format이 바뀌면 추측하지 않고 parsing failure 처리
- current player position은 User Progress 권위 데이터가 아니라 session/runtime state

새 Map 구조에서는 Tarkov.dev Map layout의 transform/rotation을 canonical layout으로 변환하여 player/quest/API marker 모두 동일한 coordinate system을 사용하도록 설계합니다.

## 9. 기존 Tarkov-Helper에서 확인된 관련 기능

`REFERENCE ONLY`

기존 구현에서 사용자 의도와 밀접해 재검토할 가치가 있는 기능:

- map dropdown
- pan / zoom / reset view
- multi-floor selector
- screenshot folder browse / auto detection
- screenshot watcher + filename coordinate parser
- player heading
- automatic floor switching using screenshot elevation
- quest marker + quest drawer
- extract marker grouping
- map marker type toggles
- custom marker add/edit/delete
- MiniMap overlay
- raid log 기반 current Map 자동 전환
- player movement trail
- full-screen map

이 목록 전체가 새 제품 요구사항으로 확정된 것은 아닙니다.

## 10. 사용자 설명에서 빠졌지만 설계상 확인할 항목

구현 결과가 달라지는 항목만 사용자에게 확인합니다.

### A. Quest 목록 범위

현재 선택 Map의:

1. 현재 진행 중 Quest만
2. 진행 중 + Locked(미래 Quest)
3. 진행 중 + 완료 포함 전체

중 무엇을 기본 목록으로 볼지.

또한 정확한 location marker가 없는 Map-related Quest를 좌측 목록에는 표시할지.

### B. MiniMap 표시 형태

- 기존처럼 게임 화면 위 always-on-top overlay인지
- JunhyunHelper 창 내부의 작은 보조 view인지

### C. 자동 Map 전환

기존 프로그램처럼 게임 로그에서 raid map을 감지하면 Map dropdown도 자동으로 해당 Map으로 전환할지.

### D. Player trail

스크린샷을 여러 번 찍었을 때 이전 위치를 선/점으로 이어 이동 경로를 남길지, 현재 위치 marker만 표시할지.

## 11. 개발자가 제품상 자연스럽다고 보는 기본 동작

아래는 별도 기능 확장이 아니라 요청 기능을 일관되게 만드는 interaction 제안입니다.

- 지도 변경 시 해당 Map Quest 목록/marker/custom marker 즉시 갱신
- marker click → 이름/종류/조건/Quest 등 최소 상세 popup
- Quest ↔ Map marker 양방향 focus
- extract marker popup에는 available condition가 source에 있으면 표시
- custom marker는 right-click으로 추가하고 marker context action으로 수정/삭제
- marker toggle 상태는 UI preference로 저장
- user marker는 content update와 독립 유지
- marker가 현재 floor와 맞지 않으면 숨기거나 floor 전환을 유도
- physical Map이 같은 day/night 또는 level variant는 UI에서 중복을 피하도록 grouping 검토

이 중 사용자의 의도에서 벗어날 가능성이 있는 항목은 구현 전에 확인합니다.
