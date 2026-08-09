# MAP PRODUCT REQUIREMENTS — exact transplant 이후 제품화

기록일: **2026-08-09**

상태: `IMPLEMENTED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 제품 기준

지도 시스템의 기준선은 PR #62에서 이식하고 사용자가 Windows에서 직접 확인한 `Propeex/Tarkov-Helper` Map + MiniMap subsystem입니다.

핵심 아키텍처 원칙:

```text
Map subsystem은 독립 시스템
└─ 유일한 예외: Quest
   └─ JunhyunHelper 현재 profile / Quest 진행 상태 / Quest 위치 geometry를 읽어 지도에 투영
```

Map artwork, map config, 일반 marker, MiniMap, hotkey, position tracking, floor state는 Quest 이외의 JunhyunHelper 기능과 결합하지 않습니다.

---

# 사용자 확정 요구사항과 구현

## 1. 현재 맵의 진행 중 Quest sidebar

왼쪽에 JunhyunHelper 전용 Quest sidebar를 둡니다.

표시 대상:

```text
현재 선택한 Map
AND 현재 profile에서 상태가 Current(진행 중)
```

- 완료 Quest 제외
- 잠긴/미래 Quest 제외
- 하나의 Quest에 유효한 위치가 여러 개면 모두 marker로 표시
- 정확한 위치 geometry가 없는 Quest는 sidebar에는 표시하되 `정확한 좌표 없음`으로 표시
- 위치를 추측해서 marker를 만들지 않음

PR #62 원본 Tarkov Helper Quest drawer는 옛 Tarkov Helper Quest DB와 연결되어 있으므로 비활성화하고, 현재 JunhyunHelper Quest workspace만 사용합니다.

### Quest 위치 업데이트

Map 전체를 JunhyunHelper Game Content에 다시 결합하지 않습니다.

Quest domain에만 다음 온라인 위치 정보를 보존합니다.

```text
Quest objective
→ possibleLocations / zones
→ map id + world X/Z
→ 신뢰 가능한 경우에만 Height(Y)
→ zone outline / top / bottom
```

외부 데이터에 Height가 없으면 `0`으로 추측하지 않고 **층 미확정**으로 유지합니다.

화면 좌표 변환은 exact Tarkov Helper의 기존 `playerMarkerTransform`을 그대로 사용합니다.

Content schema는 Quest geometry 추가로 **v4**입니다.

- 새 content는 v4로 저장
- 기존 v3는 Quest 좌표가 없는 degraded 정상 데이터로 계속 읽을 수 있음
- Map을 처음 사용할 때 v3이면 일반 online content update를 자동 1회 시도
- 갱신 성공 시 v4로 전환
- 네트워크/업데이트 실패 시 기존 v3와 `user.db`를 유지한 채 정상 실행

---

## 2. 지도 탭 UI 정리

제품 표면에서 제거:

- 전체 화면 버튼/기능
- 상단 `탈출구` 체크박스
- 상단 `고정 뷰` 체크박스
- MiniMap 옆 `?` 도움말 버튼/툴팁

원본 full-screen 호출용 compatibility contract는 소스 호환을 위해 남아 있지만 JunhyunHelper에서는 no-op이며 사용자가 실행할 수 없습니다.

---

## 3. 지도 마커 설정 통합

기존 `지도 마커`와 별도 `탈출구 설정`을 하나의 marker settings 영역으로 합칩니다.

포함:

- PMC 스폰
- 스나이퍼 스캐브
- 로그
- 컬티스트
- 레버
- 보스
- 레이더
- PMC 탈출구
- Scav 탈출구
- Transit
- 탈출구 이름 크기

### 추가 marker 검토 결과

PR #62 exact bundle의 실제 `Assets/tarkov_data.db`를 기준으로, 모델에는 존재하지만 UI에 없던 타입을 확인했습니다.

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 빈 UI를 만드는 `ScavSpawn`, `Keys`는 추가하지 않습니다.

실제 데이터가 있는 `RaiderSpawn`만 추가합니다.

- Reserve 2개 위치
- Main Map / MiniMap 동일한 전용 Raider visual 사용
- 향후 source DB에 ScavSpawn/Keys 데이터가 실제로 생기면 같은 원칙으로 재검토

---

## 4. Marker visual 동기화

Main Map과 MiniMap은 같은 의미의 marker가 서로 다른 표현 규칙을 갖지 않도록 동기화합니다.

동기화 범위:

- marker 종류별 표시 여부
- marker icon
- marker 화면상 크기
- Quest marker 크기/이름 크기
- extract icon/색/이름 크기
- Raider visual
- floor filter
- player marker 크기

구체 구현:

- 일반 marker는 Main Map의 24px 기준과 MiniMap의 기존 18px 기준 차이를 보정
- Quest는 Main/MiniMap 공통 visual factory 사용
- Raider는 Main/MiniMap 공통 visual factory 사용
- extract는 원본 Main Map의 emergency-exit SVG path geometry를 MiniMap도 그대로 사용
- Quest marker는 Map zoom과 무관하게 화면상 크기가 일정하도록 원본 marker 방식과 같은 inverse-scale 적용
- 상단 `퀘스트 마커` 체크박스가 Main Map과 MiniMap의 현재 Quest marker를 함께 제어

옛 Tarkov Helper Quest DB 전용 설정 중 현재 Quest marker에 의미 없는 항목은 숨깁니다. 현재 유지하는 Quest presentation 설정은 실제로 연결되는 marker 크기 / Quest 이름 크기입니다.

---

## 5. MiniMap 위치와 상호작용

### 위치

MiniMap은 **우측 상단 고정**입니다.

기준 위치는 사용자가 선호한다고 확정한 기존 double-click reset 위치, 즉 원본 `PositionToTopRight()` 계산을 그대로 사용합니다.

- 창 자체 mouse drag 이동 금지
- 수동/외부 위치 변경이 발생해도 우측 상단으로 snap-back
- resize 시 즉시 같은 top-right anchor로 위치 재계산
- MiniMap 크기 단축키 사용 시에도 같은 anchor 유지

MiniMap 내부 지도 pan/zoom 기능은 별도이며 창 위치 고정과 충돌하지 않습니다.

### 불투명도 / hover

- 평상시 전체 불투명도 **100% 고정**
- 불투명도 증가/감소 설정 및 hotkey 제거
- 커서가 MiniMap 화면 영역에 들어오면 **일시적으로 완전 투명(0%)**
- 커서가 영역을 벗어나면 즉시 100% 복귀
- Windows 125%/150% 등 DPI에서도 정확하도록 physical cursor 좌표를 WPF 좌표로 변환 후 판정

### Click-through

기존 Click-through는 그대로 유지하는 **독립 기능**입니다.

```text
hover transparency = 뒤 화면을 잠깐 보기 위한 시각 기능
click-through       = mouse 입력을 뒤 게임으로 통과시키는 입력 기능
```

서로 대체하지 않습니다.

---

## 6. MiniMap 크기

설정된 단축키로 MiniMap 자체 크기를 증가/감소할 수 있습니다.

- 1회 ±40px
- 기존 aspect ratio 유지
- 기존 안전 최소/최대 범위 사용
- 크기 변경 후 즉시 우측 상단 재정렬
- 설정 저장

---

## 7. 설정 / configurable hotkey

지도 탭의 `설정` 패널에서 `미니맵 및 단축키 설정`으로 진입할 수 있습니다.

사용자 지정 동작:

- MiniMap ON/OFF
- 지도 확대
- 지도 축소
- 위층 전환
- 아래층 전환
- MiniMap 크기 증가
- MiniMap 크기 감소
- 자동 층 추적 복귀

규칙:

- 같은 key는 한 동작에만 지정
- 새 동작에 같은 key를 지정하면 이전 배정 자동 해제
- Delete / Backspace = 미지정
- Esc = 입력 취소
- NumPad 0~5 = 기존 직접 층 선택과 충돌하므로 예약
- 기존 안정화된 zoom/floor hotkey 처리는 Tarkov Helper global hook 유지
- 원본에 없던 `MiniMap ON/OFF`, `MiniMap size +/-`만 JunhyunHelper 보조 hook으로 처리
- MiniMap이 꺼져 있어도 ON/OFF hotkey로 다시 켤 수 있음

---

## 8. Player marker 크기

Main Map 설정의 player marker Slider와 MiniMap 설정을 양방향 동기화합니다.

공통 범위:

```text
Main Map: 9 ~ 54 px
MiniMap: 0.5x ~ 3.0x
legacy base: 18 px
```

어느 설정 화면에서 바꾸더라도 같은 값을 사용합니다.

---

# 변경 관리 방식

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

JunhyunHelper 전용 old-Map product 변경은 별도 source branch에서 관리합니다.

```text
base: Propeex/Tarkov-Helper@9371c476...
branch: junhyun-map-product-v1
pinned product revision: 23230102b40377a9b33e9c72f29b85941ad4098d
JunhyunHelper submodule: vendor/Tarkov-Helper
```

이를 통해 exact transplant 기준과 JunhyunHelper 제품 변경 diff를 계속 분리합니다.

---

# 자동 검증 checkpoint

제품 코드 검증 head:

```text
9b99733b4215659e91b3319b8ca4b6d2ae547a27
CI: 31313163552
```

통과:

- Desktop Release build
- existing automated tests
- Windows x64 self-contained publish
- published EXE Startup + Map smoke
  - 실제 lazy Map subsystem / product adapter 생성
  - 12초 이상 정상 생존
- ZIP 생성 / artifact upload

자동화는 UI 의미/게임 중 체감까지 확정하지 않으므로 다음 gate는 사용자 Windows 검증입니다.

---

# 다음 단계

현재 product refinement 사용자 검증 후, Map의 독립 업데이트 대응을 이어갑니다.

목표:

```text
동일한 검증된 Tarkov Helper upstream revision
→ map_configs.json
→ SVG maps / marker assets
→ map DB
→ 전체 bundle 검증
→ 모두 성공한 경우에만 active 교체
→ 실패하면 마지막 정상 bundle 유지
```

Map image/config/DB를 서로 다른 revision으로 개별 갱신하지 않습니다.
