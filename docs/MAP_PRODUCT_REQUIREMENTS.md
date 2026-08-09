# MAP PRODUCT REQUIREMENTS — exact transplant 이후 제품화

기록일: **2026-08-09**

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## 기준

현재 지도 시스템의 기준선은 PR #62에서 이식한 `Propeex/Tarkov-Helper` Map + MiniMap subsystem입니다.

이 문서는 그 원본 이식본 위에 적용할 **사용자 확정 변경사항**을 기록합니다.

핵심 아키텍처 원칙:

```text
Map subsystem은 독립 시스템
└─ 예외: Quest만 JunhyunHelper 현재 프로필/진행 상태를 읽어 지도에 투영
```

Map marker, MiniMap, hotkey, map settings, position tracking, floor state는 Quest 이외의 JunhyunHelper 기능과 결합하지 않습니다.

## 사용자 확정 요구사항

### 지도 탭

- 추가할 가치가 있는 지도 마커 타입을 실제 데이터와 실사용 가치 기준으로 검토합니다.
- 왼쪽에 퀘스트 사이드바를 유지/정리하고 **현재 선택한 맵의 진행 중 퀘스트만** 표시합니다.
- 해당 진행 중 퀘스트에 지도 좌표가 있으면 지도에 퀘스트 마커를 표시합니다.
- 하나의 퀘스트에 여러 위치 목표가 있으면 유효한 위치를 모두 표시합니다.
- 완료/잠김/미래 퀘스트는 지도 사이드바/마커 대상이 아닙니다.
- 전체화면 기능을 삭제합니다.
- 상단 `탈출구` 체크박스를 삭제합니다.
- 상단 `고정 뷰` 체크박스를 삭제합니다.
- 별도 `탈출구 설정` 영역을 제거하고 지도 마커 설정 안에 병합합니다.
- 지도 마커의 아이콘은 타입 의미에 맞게 정리합니다.

### 지도 마커 설정

기존 지도 마커 설정과 탈출구 필터를 하나의 마커 설정 계층으로 통합합니다.

최소 포함:

- PMC 스폰
- 스나이퍼 스캐브
- 로그
- 컬티스트
- 레버
- 보스
- PMC 탈출구
- Scav 탈출구
- Transit

원본 모델에 이미 존재하는 `ScavSpawn`, `RaiderSpawn`, `Keys`는 실제 bundled DB에 데이터가 있고 실사용 가치가 확인되면 추가합니다.

### MiniMap 위치/상호작용

- MiniMap 위치는 **화면 우측 상단에 고정**합니다.
- 기준 위치는 현재 원본 MiniMap에서 더블클릭 시 실행되는 `PositionToTopRight()` 위치를 사용합니다.
- 마우스 드래그로 MiniMap 창 자체를 이동할 수 없게 합니다.
- MiniMap 크기가 변경되면 같은 우측 상단 anchor/margin을 유지하도록 위치를 즉시 다시 계산합니다.
- 해상도/작업영역이 바뀌어도 우측 상단 기준으로 재배치합니다.
- MiniMap 기본 불투명도는 **100% 고정**합니다.
- 기존 `Click-through` 기능은 유지합니다.
- 별도로, MiniMap 뒤의 게임 화면을 잠깐 확인하기 위한 **hover transparency** 기능을 추가합니다.
  - 커서가 MiniMap 영역 위에 올라가면 MiniMap 표시가 일시적으로 완전 투명해집니다.
  - 커서가 영역에서 벗어나면 즉시 100%로 복귀합니다.
  - 이 기능은 기존 Click-through 상태와 별개입니다.
  - Click-through 자체를 대체하거나 제거하지 않습니다.
- 지도 탭의 MiniMap 옆 `?` 도움말 버튼/툴팁을 제거합니다.

### Main Map ↔ MiniMap 동기화

MiniMap은 항상 Main Map의 표시 정책과 동기화합니다.

동기화 대상:

- 마커 종류별 표시 여부
- 마커 아이콘
- 마커 크기
- 마커 라벨/글자 크기
- 탈출구 필터
- 층 상태/층 표시 정책
- 플레이어 마커 크기

동일한 의미의 마커가 Main Map과 MiniMap에서 서로 다른 visual rule을 별도로 갖지 않도록 공통 presentation state를 사용합니다.

### 설정 / 단축키

설정에서 사용자가 단축키를 직접 지정할 수 있게 합니다.

사용자 지정 필수 동작:

- MiniMap ON/OFF
- 지도 확대
- 지도 축소
- 위층 전환
- 아래층 전환
- MiniMap 크기 증가
- MiniMap 크기 감소

추가 유지 권장 동작:

- 자동 층 추적 복귀

불투명도 증가/감소 hotkey는 MiniMap 불투명도 100% 고정 요구와 충돌하므로 제거합니다.

같은 키는 한 동작에만 지정할 수 있고, 새 동작에 배정하면 이전 배정을 해제합니다.

### 플레이어 마커

설정에 플레이어 마커 크기 조절 Slider를 제공합니다.

Main Map과 MiniMap이 동일한 사용자 설정값을 사용하도록 합니다.

## 변경 관리 방식

기존 `Propeex/Tarkov-Helper` main은 변경하지 않습니다.

```text
Propeex/Tarkov-Helper@9371c476...
→ branch: junhyun-map-product-v1
→ 사용자 확정 Map 제품 변경만 적용
→ JunhyunHelper submodule이 검증된 commit을 pin
```

이를 통해 원본 이식 기준과 JunhyunHelper 전용 변경의 diff를 분리해 추적합니다.

## 검증 기준

- original Map rendering이 유지됨
- Main Map / MiniMap build 및 startup 정상
- 기존 screenshot position tracking / raid map switching / floor detection 회귀 없음
- 현재 맵의 진행 중 Quest만 sidebar/marker에 표시
- Quest 이외 Map 기능이 JunhyunHelper 다른 기능에 의존하지 않음
- MiniMap 우측 상단 고정 및 resize 후 anchor 유지
- Click-through와 hover transparency가 서로 독립적으로 동작
- Main Map/MiniMap marker visual state가 동일
- 모든 configurable hotkey conflict 처리
