# MAP PRODUCT REQUIREMENTS — Tarkov Helper Map 제품화

기록일: **2026-08-09**

상태: `USER CONFIRMED / V2 IMPLEMENTATION IN PROGRESS`

## 제품 기준

지도 시스템의 기준선은 PR #62에서 이식하고 사용자가 Windows에서 직접 확인한 `Propeex/Tarkov-Helper` Map + MiniMap subsystem입니다.

핵심 아키텍처 원칙:

```text
Map subsystem은 독립 시스템
└─ 유일한 예외: Quest
   └─ JunhyunHelper 현재 profile / Quest 진행 상태 / Quest 위치 geometry를 읽어 지도에 투영
```

Map artwork, map config, 일반 marker, MiniMap, hotkey, screenshot position tracking, floor selection은 Quest 이외의 JunhyunHelper 기능과 결합하지 않습니다.

---

# V2 사용자 확정 요구사항

## 1. 현재 맵 진행 중 Quest sidebar

왼쪽 sidebar는 **현재 선택 Map + 현재 profile에서 Current(진행 중)인 Quest만** 표시합니다.

- sidebar는 펼침/접힘 가능
- 접으면 지도 영역이 실제로 넓어져야 함
- Quest 행을 누르면 JunhyunHelper `퀘스트` 탭으로 이동하고 해당 Quest를 선택하여 상세 정보를 표시
- 정확한 위치 geometry가 없는 Quest도 sidebar에는 표시하되 `정확한 좌표 없음`으로 표시
- 위치를 추측하지 않음

### Quest marker 표시 선택

- `퀘스트 마커 표시` 전역 checkbox 제공
- 좌표가 있는 Quest는 sidebar에 개별 marker 표시 checkbox 제공
- 전역 checkbox OFF는 개별 선택 상태를 지우지 않고 화면 표시만 끔
- 개별 checkbox OFF인 Quest의 marker는 Main Map/MiniMap 모두 숨김
- 표시 대상으로 선택된 Quest는 sidebar 순서대로 `A`, `B`, `C`... 식별자 부여
- 한 Quest에 위치가 여러 개면 같은 식별자를 공유
- marker badge와 Quest 행에서 같은 식별자를 보여 사용자가 지도에서 쉽게 대응할 수 있게 함
- 식별자는 현재 표시 대상으로 선택된 Quest 순서에 따라 재계산

Quest 위치는 online `possibleLocations` / `zones` geometry를 사용하고 exact Tarkov Helper coordinate transform으로 화면 좌표를 계산합니다.

---

## 2. 지도 marker 설정 UI

`지도 마커` 영역은 단순 나열이 아니라 의미별로 시각적으로 정리합니다.

권장 grouping:

```text
Quest
- Quest marker 전체 표시

전투 / Spawn
- PMC Spawn
- Sniper Scav
- Rogue
- Cultist
- Boss
- Raider

지도 요소
- Lever

탈출 / 이동
- PMC Extract
- Scav Extract
- Transit
```

- 각 행의 checkbox / icon / 이름 정렬을 통일
- section/card 단위로 구분하여 빠르게 읽을 수 있게 함
- `탈출구 이름 크기`는 marker 목록에서 제거하고 `설정` 패널로 이동

실제 bundled DB 검토 결과:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 데이터 없는 ScavSpawn/Keys UI는 만들지 않고 Raider만 유지합니다.

---

## 3. 의미 없는 Quest marker presentation 설정 제거

기존 Map 설정의 다음 값은 현재 제품 Quest marker에 의미가 없으므로 제거합니다.

- Quest 이름 크기 `20`
- Quest marker 크기 `18`
- old Tarkov Helper Quest marker style/color 설정

현재 Quest marker는 JunhyunHelper 공통 visual 규칙으로 표시합니다.

Player marker 크기 설정은 별도 기능이므로 유지합니다.

---

## 4. Screenshot tracking 정책

스크린샷은 계속 사용합니다.

사용 목적:

- 현재 Map 감지
- 감지된 Map으로 Main Map/MiniMap 자동 전환
- Player X/Z 위치 추적
- 가능한 경우 heading 표시

사용하지 않는 목적:

- **현재 층 자동 판정**

스크린샷의 높이/좌표를 이용한 floor auto-detection, floor auto-selection 및 관련 설정/상태/복귀 기능은 제거합니다.

Floor는 사용자가 map/floor selector 또는 floor hotkey로 직접 선택합니다.

---

## 5. Floor 표시 정책 고정

- 선택된 **현재 층만 표시**
- 다른 층 opacity는 **0% 고정**
- `다른 층 투명도` 설정 제거
- `현재 층만 표시` 설정 제거
- `위치 기반 자동 층 선택` 설정 제거
- `자동 층 추적 복귀` 기능/버튼/hotkey 제거

Floor up/down manual hotkey는 유지합니다.

---

## 6. MiniMap 상호작용 정책 고정

### 위치

- 원본 `PositionToTopRight()` 기준 우측 상단 고정
- 창 drag 이동 금지
- resize 후 우측 상단 anchor 유지

### 표시

- 평상시 opacity 100%
- cursor hover 시 일시적으로 0% 투명
- cursor가 빠지면 즉시 100% 복귀
- DPI-aware hover 판정 유지

### 입력

- **Click-through 항상 ON 고정**
- Click-through 설정/토글 제거
- hover transparency는 계속 유지

### View mode

- **PlayerTracking 고정**
- Fixed view 제거
- Fixed / PlayerTracking 선택 설정 제거
- player position update가 들어오면 미니맵은 항상 player를 추적

### 즉시 작업

Overlay/MiniMap settings의 `즉시 작업` section은 사용하지 않으므로 제거합니다.

---

## 7. Hotkey 설정

단축키는 별도 MiniMap settings dialog의 단축키 section에 두지 않고 **지도 탭 `설정` 패널에 직접 배치**합니다.

사용자 지정 동작:

- MiniMap ON/OFF
- Map zoom in
- Map zoom out
- floor up
- floor down
- MiniMap size increase
- MiniMap size decrease

삭제:

- 자동 층 추적 복귀

규칙:

- 같은 key는 한 동작에만 지정
- 새 동작에 같은 key를 지정하면 이전 배정 자동 해제
- Delete / Backspace = 미지정
- Esc = 입력 취소
- NumPad 0~5는 직접 floor 선택용 예약

---

## 8. Custom marker 제거

사용자 custom marker 기능은 제품에서 제거합니다.

제거 대상:

- 우측 custom marker sidebar
- custom marker 추가 context menu
- custom marker 편집/삭제 UI
- custom marker opacity/list controls
- 사용자에게 노출되는 custom marker 관련 동작

호환을 위해 원본 소스 타입이 남더라도 제품 runtime/UI에서는 사용하지 않습니다.

---

# 기존 유지 요구사항

- 전체 화면 기능 제거
- 상단 `탈출구` checkbox 제거
- 상단 `고정 뷰` checkbox 제거
- MiniMap `?` 도움말 제거
- Main Map/MiniMap marker visibility/icon/presentation 동기화
- Quest만 JunhyunHelper와 Map의 cross-feature dependency
- Player marker size Main/MiniMap 동기화
- Quest content schema v4 + v3 offline fallback 유지
- update 실패 시 기존 정상 data / user progress 보호

---

# 변경 관리

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

```text
exact baseline: 9371c4769d8da8acb9df864a2c88f83ecdd42818
V1 product source: junhyun-map-product-v1 @ 23230102b40377a9b33e9c72f29b85941ad4098d
V2 product source branch: junhyun-map-product-v2
JunhyunHelper work branch: agent/map-product-refinement-v2
```

Exact baseline과 제품 변경 diff를 계속 분리해 관리합니다.

---

# V2 검증 기준

- sidebar collapse 시 실제 지도 폭이 회복됨
- Quest click → Quest tab + 해당 Quest detail 선택
- global/per-Quest marker checkbox 정상
- A/B/C marker identity Main/MiniMap 일치
- screenshot map detection + auto map switch 유지
- screenshot floor auto-detection 제거
- manual floor 선택/hotkey 유지
- other-floor opacity 0 고정
- MiniMap click-through ON 고정
- MiniMap PlayerTracking 고정
- overlay 즉시 작업/불필요 설정 제거
- hotkey editor가 Main Map settings 안에 직접 존재
- custom marker UI/runtime 비노출
- Desktop Release build / tests / Windows x64 publish / Startup + Map smoke 통과
