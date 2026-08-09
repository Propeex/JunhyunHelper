# MAP PRODUCT REQUIREMENTS — Tarkov Helper Map 제품화

기록일: **2026-08-09**

상태: `USER CONFIRMED / V2 IMPLEMENTED / WINDOWS USER VALIDATION NEXT`

## 제품 기준

지도 시스템의 기준선은 PR #62에서 exact transplant한 `Propeex/Tarkov-Helper` Map + MiniMap subsystem입니다.

핵심 아키텍처 원칙:

```text
Map subsystem은 독립 시스템
└─ 유일한 예외: Quest
   └─ JunhyunHelper 현재 profile / Quest 진행 상태 / Quest 위치 geometry를 읽어 지도에 투영
```

Map artwork, map config, 일반 marker, MiniMap, hotkey, screenshot position tracking, floor selection은 Quest 이외의 JunhyunHelper 기능과 결합하지 않습니다.

현재 제품 source:

```text
exact baseline: 9371c4769d8da8acb9df864a2c88f83ecdd42818
product source branch: Propeex/Tarkov-Helper:junhyun-map-product-v2
pinned source revision: d933792b6042a51cea38dc44b686a096fe30de67
PR #64 merge: 2339ddff5773ee385ff32b4ff5a173aab52d8050
```

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

---

# 1. 현재 Map 진행 중 Quest sidebar

왼쪽 sidebar는 **현재 선택 Map + 현재 profile에서 Current(진행 중)인 Quest만** 표시합니다.

- sidebar 펼침/접힘 가능
- 기본은 접힘 상태
- 접으면 지도 영역이 실제로 넓어짐
- Quest 행 클릭 → JunhyunHelper `퀘스트` 탭 → 해당 Quest 선택/상세 표시
- 정확한 위치 geometry가 없는 Quest도 목록에는 표시
- 위치가 없으면 `정확한 좌표 없음`
- 위치를 추측하지 않음

## Quest marker 표시

- `퀘스트 마커 표시` 전역 checkbox 제공
- 좌표가 있는 Quest는 sidebar에 개별 marker checkbox 제공
- 전역 OFF는 개별 선택 상태를 지우지 않고 화면 표시만 끔
- 개별 OFF인 Quest는 Main Map/MiniMap 모두 숨김
- 표시 대상으로 선택된 Quest는 sidebar 순서대로 `A`, `B`, `C`... 식별자 부여
- 하나의 Quest에 위치가 여러 개면 같은 식별자 공유
- sidebar와 Main Map/MiniMap marker가 같은 식별자 사용
- 표시 대상이 바뀌면 식별자를 현재 순서 기준으로 재계산

Quest 위치는 online `possibleLocations` / `zones` geometry를 사용하고 exact Tarkov Helper coordinate transform으로 화면 좌표를 계산합니다.

Quest source에 신뢰 가능한 Height가 있을 때만 marker의 소속 floor를 분류합니다. Height가 없으면 floor를 추측하지 않습니다.

---

# 2. 지도 marker 설정 UI

`지도 마커` 영역은 의미별 section/card 구조로 정리합니다.

```text
Quest
- 퀘스트 마커 표시

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

- checkbox / icon / 이름 정렬을 통일
- section 단위로 빠르게 읽을 수 있게 구성
- `탈출구 이름 크기`는 marker 목록이 아니라 Main Map `설정` 패널에 배치

실제 bundled DB 검토 결과:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 데이터 없는 ScavSpawn/Keys UI는 만들지 않고 Raider만 유지합니다.

---

# 3. 의미 없는 old Quest presentation 설정 제거

다음 old Tarkov Helper Quest marker 설정은 현재 JunhyunHelper Quest projection에 의미가 없으므로 제거합니다.

- Quest 이름 크기 `20`
- Quest marker 크기 `18`
- old Quest marker style
- old Quest marker type별 color 설정

현재 Quest marker는 JunhyunHelper 공통 A/B/C visual 규칙을 사용합니다.

Player marker 크기는 별도 기능이므로 유지합니다.

---

# 4. Screenshot tracking 정책

스크린샷은 계속 사용합니다.

사용 목적:

- 현재 Map 감지
- 감지된 Map으로 자동 전환
- Player X/Z 위치 추적
- 가능한 경우 heading 표시

사용하지 않는 목적:

- **현재 floor 자동 판정**

원본 screenshot position callback에서 floor auto-switch 경로를 제거합니다. Screenshot 데이터가 들어와도 floor는 바꾸지 않습니다.

Floor는 사용자가 selector 또는 floor hotkey로 직접 선택합니다.

---

# 5. Floor 표시 정책

- 선택된 **현재 floor만 표시**
- 다른 floor opacity는 **0% 고정**
- `다른 층 투명도` 설정 제거
- `현재 층만 표시` 설정 제거
- screenshot/위치 기반 auto-floor 설정 제거
- `자동 층 추적 복귀` 기능/버튼/hotkey 제거
- floor up/down manual hotkey는 유지

---

# 6. MiniMap 상호작용 정책

## 위치

- 원본 `PositionToTopRight()` 기준 우측 상단 고정
- 창 drag 이동 금지
- resize 후 동일 top-right anchor 유지

## 표시

- 평상시 opacity 100%
- cursor hover 시 일시적으로 0% 투명
- cursor가 빠지면 즉시 100% 복귀
- DPI-aware hover 판정 유지

## 입력

- **Click-through 항상 ON 고정**
- Click-through 설정/토글 제거

## View mode

- **PlayerTracking 고정**
- Fixed view 제거
- Fixed / PlayerTracking 선택 설정 제거
- player position update가 들어오면 MiniMap이 player를 추적

## Floor

- 다른 floor opacity 0% 고정
- AutoFloorSelection OFF 고정
- 현재 수동 선택 floor만 사용

## 설정 안정성

위 고정 정책은 UI만 숨기는 방식이 아닙니다. Legacy settings JSON에 과거 값이 남아 있어도 product settings model이 해당 값을 무시해 제거된 기능이 다시 활성화되지 않아야 합니다.

## 즉시 작업

MiniMap/Overlay settings의 `즉시 작업` section은 제거합니다.

---

# 7. Hotkey 설정

단축키 편집기는 별도 MiniMap settings dialog에서 제거하고 **Main Map `설정` 패널에 직접 배치**합니다.

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
- 새 동작에 같은 key를 지정하면 이전 배정 해제
- Delete / Backspace = 미지정
- Esc = 입력 취소
- NumPad 0~5 = 직접 floor 선택 예약

---

# 8. Custom marker 제거

사용자 Custom Marker 기능은 제품에서 제거합니다.

제품에서 제거할 surface:

- 우측 custom marker sidebar
- custom marker 추가 context menu
- custom marker container
- custom marker 편집/삭제 UI
- custom marker opacity/list controls
- 사용자에게 노출되는 custom marker 동작

원본 source 호환 타입이 남더라도 JunhyunHelper 제품 UI/runtime에서는 사용자가 접근하지 않습니다.

---

# 9. 기존 유지 요구사항

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

# 검증 기준

- sidebar collapse 시 실제 지도 폭 회복
- Quest click → Quest tab + 해당 Quest detail 선택
- global/per-Quest marker checkbox 정상
- A/B/C marker identity Main/MiniMap 일치
- screenshot Map detection + auto Map switch 유지
- screenshot floor auto-detection 제거
- manual floor 선택/hotkey 유지
- current-floor-only + other-floor opacity 0 고정
- MiniMap click-through ON 고정
- MiniMap PlayerTracking 고정
- 불필요 MiniMap settings / 즉시 작업 제거
- hotkey editor가 Main Map settings에 직접 존재
- custom marker product UI 비노출
- Desktop Release build / tests / Windows x64 publish / Startup + Map smoke 통과

최종 자동 검증:

```text
PR #64 final head: ae7839e15a26d8d0a0643802ed08ab0f5b80f520
CI: 31320921128
Desktop Release build: success
existing tests: success
Windows x64 publish: success
Startup + Map smoke: success
ZIP upload: success
```
