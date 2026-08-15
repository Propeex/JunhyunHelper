# MAP PRODUCT REQUIREMENTS — 준현 헬퍼 Map + MiniMap

기록일: **2026-08-09**  
최종 갱신: **2026-08-15**

상태: `USER CONFIRMED / IMPLEMENTED / V0.1.5 MAP REGRESSION FIX IN PROGRESS`

---

# 1. 제품 기준과 시스템 경계

지도 시스템의 기준선은 사용자가 실제 artwork/구조를 확인한 `Propeex/Tarkov-Helper` Map + MiniMap subsystem입니다.

```text
exact baseline revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818

product source repository:
Propeex/Tarkov-Helper

product source branch:
junhyun-map-product-v2

currently pinned revision:
d933792b6042a51cea38dc44b686a096fe30de67

JunhyunHelper submodule:
vendor/Tarkov-Helper
```

이 선택은 기존 Tarkov-Helper 전체를 새 제품 사양으로 승계한다는 뜻이 아닙니다. **Map/MiniMap의 검증된 특정 기준선만 명시적으로 채택한 예외**입니다.

핵심 아키텍처 원칙:

```text
Map subsystem = 독립
└─ 유일한 cross-feature dependency: Quest
   ├─ JunhyunHelper current profile
   ├─ Quest 진행 상태
   └─ online Quest location geometry
```

Hideout / Item / Ammo runtime을 Map과 결합하지 않습니다.

---

# 2. Map Quest sidebar

왼쪽 sidebar는 **현재 선택 Map + 현재 profile에서 Current(진행 중)인 Quest만** 표시합니다.

`확인 필요(Indeterminate)` Quest는 현재 진행 중임을 프로그램이 입증하지 못하므로 sidebar에서 제외합니다.

- 기본 접힘
- 펼침 폭 약 300px
- 접으면 지도 영역이 실제로 넓어짐
- Quest 행 클릭 → JunhyunHelper Quest 탭의 해당 Quest 상세
- 정확한 위치 geometry가 없는 Quest도 목록 유지
- 위치가 없으면 `정확한 좌표 없음`
- 좌표를 추측하지 않음

## Quest marker

- `퀘스트 마커 표시` global checkbox
- 좌표가 있는 Quest별 individual marker checkbox
- global OFF는 individual 선택 상태를 지우지 않음
- individual OFF인 Quest는 Main Map/MiniMap 모두 숨김
- 표시 대상 Quest를 sidebar 순서대로 `A`, `B`, `C`... 식별
- 한 Quest의 여러 위치는 같은 식별자 공유
- sidebar / Main Map / MiniMap 모두 같은 식별자 사용
- 표시 대상/순서가 바뀌면 현재 sidebar 순서 기준으로 재계산

Quest 위치 source:

```text
online possibleLocations / zones
→ canonical Content schema v4+
→ exact Tarkov Helper coordinate transform
→ JunhyunHelper Quest projection
```

신뢰 가능한 Height가 있을 때만 floor를 분류합니다. Height가 없으면 floor를 추측하지 않습니다.

Quest marker renderer는 **0×0 Canvas anchor + child offset** 구조를 사용하여 zoom/floor 렌더에서 anchor 위치가 흔들리지 않게 합니다.

---

# 3. 일반 marker / 탈출구

제품 marker group:

```text
Quest
- 퀘스트 마커 표시

전투 / 스폰
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

bundled DB에서 실제 데이터가 없는 category를 억지로 UI에 만들지 않습니다.

현재 pinned bundle 확인 기준:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

marker 표시 규칙:

```text
해당 category/faction ON
AND marker가 현재 Map에 속함
→ floor와 관계없이 marker/extract 자체는 유지
```

**Floor는 visibility filter가 아닙니다.** 다른 층이라는 이유로 `Visibility.Collapsed`하거나 `Opacity=0`으로 숨기지 않습니다. Floor 표현은 4절의 공통 규칙을 따릅니다. Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시합니다. Main Map과 MiniMap의 일반 marker / extract 표시 설정은 동기화합니다.

---

# 4. Floor 정책

Floor는 사용자가 직접 선택합니다.

허용 방식:

- 원본 `CmbFloorSelect` dropdown
- floor up/down product hotkey
- NumPad 0~5 direct floor selection compatibility

정책:

- screenshot으로 floor를 자동 판정하지 않음
- AutoFloorSelection OFF
- **지도 artwork는 선택한 현재 floor만 표시**
- enabled marker/extract는 다른 floor라는 이유만으로 숨기지 않음
- 다른 floor opacity를 사용자가 조절하는 별도 설정은 없음
- 자동 층 추적 복귀 없음

## 4.1 marker floor 표현

현재 선택 floor와 marker floor를 Map config의 `Floor.Order`로 비교합니다.

marker의 고유 type/icon 색상은 그대로 유지하고, floor 관계는 작고 일정한 색상 ring으로 표현합니다.

```text
현재 층 marker → 작은 초록 ring + 정상 opacity
위층 marker     → 작은 빨강 ring + 약 75% opacity
아래층 marker   → 작은 파랑 ring + 약 75% opacity
floor 불명확    → floor 색상/방향 추측 안 함
```

- 층 이름 문자열을 보고 위/아래를 추정하지 않음
- Main Map과 MiniMap에서 같은 의미를 사용
- Quest A/B/C marker, 일반 marker, Raider 등 JunhyunHelper 추가 marker, extract에 같은 relation 의미 적용
- 빨강/파랑 색상만으로 관계를 구분하기 어려운 경우를 위해 위/아래에는 **매우 작은 방향 glyph**를 보조적으로 둘 수 있음
- 기존처럼 marker를 가리는 큰 `↑/↓` badge는 사용하지 않음
- floor indicator는 hit-test를 받지 않음

### 4.1.1 Main Map 동일/근접 X/Z의 서로 다른 floor marker

서로 다른 known floor의 일반 marker가 같은 type이고 X/Z상 겹치거나 가까워도, 그것만으로 대표 하나만 남기거나 다른 marker를 `Opacity=0`/`Collapsed` 처리하지 않습니다.

```text
category ON
AND marker가 현재 Map에 속함
→ 각 marker visual을 모두 유지
→ 각 marker의 Current / Above / Below relation presentation 적용
```

서로 다른 floor라는 사실은 일반 marker를 같은 물리 항목으로 간주할 근거가 아닙니다. 겹쳐 보일 수 있더라도 floor ring과 작은 방향 glyph로 관계를 구분하며, 타층 marker 자체를 삭제하거나 숨기지 않습니다.

source상 실제로 **같은 물리 항목의 중복 record**라고 확인할 수 있는 경우에만 별도의 semantic duplicate 규칙을 적용합니다. 예를 들어 Factory `Gate 3`처럼 같은 이름·같은 정규화 floor·거의 같은 위치의 PMC/Scav extract는 faction filter 의미를 보존하면서 대표 visual 하나로 정규화할 수 있습니다. 이 규칙을 서로 다른 floor의 일반 marker에 확대 적용하지 않습니다.

## 4.2 floor 변경 시 viewport 보존

floor up/down product hotkey와 NumPad 0~5 direct floor selection은 **층 artwork를 바꾸는 동작**이며, 사용자가 보고 있던 위치를 다시 중앙 초기값으로 보내는 동작이 아닙니다.

Main Map:

```text
현재 zoom + viewport 중앙의 map-space 좌표 저장
→ Main Map floor SVG 변경
→ Main Map extract / Quest / 일반 marker refresh 완료
→ 같은 zoom + 같은 map-space 중앙 좌표 복원
```

MiniMap:

```text
현재 live MapScale + MapTranslate에서 zoom / map-space 중심 저장
→ live transform을 persisted MapOffset에도 동기화
→ MiniMap floor SVG 변경 완료까지 await
→ 같은 zoom + 같은 map-space 중앙 좌표 복원
→ persisted MapOffset과 live MapTranslate 재동기화
```

MiniMap은 `PlayerTracking` 고정이므로 실제 현재 viewport는 persisted offset보다 live transform이 우선합니다. PlayerTracking이 `MapTranslate`만 갱신한 상태에서 floor renderer가 stale `MapOffsetX/Y`를 다시 적용하여 중심을 초기/이전 위치로 되돌려서는 안 됩니다.

따라서 층을 바꿔도 Main Map과 MiniMap 모두 사용자가 보고 있던 지도 위치와 zoom을 유지해야 합니다. 새로운 screenshot player position이나 Map 자체 변경처럼 의미상 viewport가 바뀌어야 하는 이벤트는 이 규칙의 대상이 아닙니다.

legacy direct SelectionChanged 단축키 경로는 제품 입력으로 dispatch하지 않습니다.

## 4.3 legacy refresh와 안정화

transplanted Map의 async refresh가 floor/map 변경 직후 marker tree를 다시 만들 수 있습니다. JunhyunHelper는 이를 보정할 수 있으나 **permanent full-tree polling을 사용하지 않습니다.**

허용 방식:

- 실제 map/floor/filter 변경 event
- marker container의 O(1) 구조 signature 변화
- 변경 직후 제한된 횟수의 bounded stabilization

금지 방식:

- 프로그램이 켜져 있는 동안 200ms마다 marker 전체를 영구 순회
- floor가 다르다는 이유로 별도 policy timer가 marker를 반복적으로 `Collapsed` 처리
- async load가 완료된 뒤 cross-floor near-overlap marker를 뒤늦게 `Opacity=0`으로 처리
- MiniMap floor SVG 교체 뒤 stale persisted offset으로 live PlayerTracking viewport를 덮어쓰기

---

# 5. Screenshot tracking

Screenshot은 계속 사용합니다.

사용 목적:

- 현재 Map 감지
- 감지된 Map으로 자동 전환
- Player X/Z 위치 추적
- 가능한 경우 heading 표시

사용하지 않는 목적:

- floor 자동 판정

Screenshot 실패/미사용 상태에서도 manual Map/floor 조작은 가능해야 합니다.

---

# 6. Main Map UI

- 전체 화면 기능 없음
- 의미 없는 상단 `탈출구` checkbox 없음
- `고정 뷰` checkbox 없음
- custom marker 사용자 surface 없음
- old Quest marker style/color/name-size/marker-size 설정 없음
- marker panel은 지도/상단 조작을 가리지 않도록 max-height + 내부 scroll
- 지도 viewport는 상단 UI와 겹치지 않고 bounds에서 clip

Player marker size와 extract name size 등 실제 제품에서 사용하는 조정값만 유지합니다.

---

# 7. MiniMap 고정 제품 정책

## 위치/창

- 화면 우측 상단 고정
- drag 이동 금지
- mouse resize 금지
- 우측 하단 legacy resize grip 없음
- 크기 조절은 size increase/decrease hotkey만 사용
- resize 후 우측 상단에 다시 anchor

## 입력

- Click-through 항상 ON
- Click-through 선택 UI 없음
- ViewMode = PlayerTracking 고정
- Fixed view 선택 없음

## Floor

- manual selected floor 사용
- artwork는 selected floor만 표시
- enabled 다른 층 marker/extract는 숨기지 않고 4.1의 floor relation presentation 적용
- floor 변경 시 4.2의 live viewport 보존 적용
- AutoFloorSelection OFF

## Hover / temporary hide

MiniMap을 가리고 게임 UI를 확인하기 위한 두 hide 조건을 제공합니다.

```text
cursor hover
OR
설정된 temporary-hide hotkey 활성
→ MiniMap window opacity 0%
```

둘 다 비활성이면 설정한 평상시 opacity로 복귀합니다.

Temporary hide:

- 별도 product hotkey
- duration 1~15초
- 재시작 후 유지

## 평상시 opacity

- 사용자 slider: 10%~100%
- 기본값: 100%
- 활성 MiniMap에 즉시 반영
- 재시작 후 유지

legacy `_settings.Opacity` 값은 제품 권위값이 아닙니다. JunhyunHelper product presentation layer가 normal opacity / hover / timed-hide를 최종 결정합니다.

---

# 8. MiniMap marker size

MiniMap의 비플레이어 marker를 별도로 조절합니다.

`미니맵 마커 크기`:

- 25%~150%
- 5% 단위
- 기본 100%
- 즉시 반영
- 재시작 후 유지

적용 대상:

- Quest marker + floor indicator
- 일반 Map marker + floor indicator
- PMC / Scav / Transit extract marker + label + floor indicator
- Raider 등 JunhyunHelper 추가 marker

제외:

- Player position marker

Player marker는 기존 player-marker size 설정을 그대로 사용합니다.

MiniMap marker scale은 zoom 역보정 이후에 곱합니다.

```text
visual marker scale
= inverse MiniMap zoom compensation × configured MiniMap marker scale
```

따라서 MiniMap zoom을 바꿔도 사용자가 정한 marker 상대 크기가 유지됩니다.

---

# 9. Global hotkey

Main Map `설정`에서 편집합니다.

제품 동작:

- MiniMap ON/OFF
- Main Map + MiniMap zoom in
- Main Map + MiniMap zoom out
- Main Map + MiniMap floor up
- Main Map + MiniMap floor down
- MiniMap size increase
- MiniMap size decrease
- MiniMap temporary hide

규칙:

- 같은 key는 마지막으로 지정한 한 동작에만 남음
- 새 동작에 같은 key 지정 → 이전 배정 해제
- Delete / Backspace → 미지정
- Esc → 편집 취소
- NumPad 0~5 → 직접 floor 선택용 예약 범위
- direct floor와 floor up/down 모두 Main Map과 MiniMap의 viewport-safe floor render 사용

JunhyunHelper-owned persisted hotkey가 runtime 권위값입니다.

허용 foreground:

- `EscapeFromTarkov`
- `EscapeFromTarkov_BE`
- `준현 헬퍼` / `JunhyunHelper`
- legacy host compatibility용 `TarkovHelper`

즉, **게임 플레이 중 게임 창이 활성 상태여도 hotkey가 동작해야 합니다.**

MiniMap을 켤 때 transplanted legacy zoom/floor hook이 같은 key를 다시 등록하지 못하도록 old direct mapping은 비활성 상태로 유지합니다.

---

# 10. Map 제품 설정 저장

권위 경로:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

저장 대상:

- 일반 marker toggle
- PMC/Scav/Transit extract toggle
- Raider toggle
- Quest global toggle
- Quest별 marker toggle
- marker/player/extract 조정값
- combo 설정
- screenshot folder
- product hotkeys
- MiniMap temporary-hide key + duration
- MiniMap normal opacity
- MiniMap marker scale

쓰기 방식은 temporary file → overwrite move로 하여 가능한 범위에서 원자적으로 교체합니다.

legacy Tarkov Helper settings가 async 초기화 후반에 값을 다시 읽더라도 JunhyunHelper product 값이 최종 권위가 되도록 안정화 구간에 재적용합니다.

---

# 11. 제거/비노출 기능

다음 old Tarkov Helper 기능은 JunhyunHelper 제품 surface가 아닙니다.

- Full Screen
- Custom Marker 추가/편집/삭제
- custom marker sidebar/context menu
- screenshot floor auto-detection
- Fixed View
- PlayerTracking 선택 UI
- click-through 선택 UI
- MiniMap mouse resize / resize grip
- 다른 floor opacity 설정
- current-floor-only 설정
- auto-floor 설정/복귀
- MiniMap `?` help
- 별도 MiniMap settings 진입 UI
- MiniMap `즉시 작업` section
- old Quest marker style/color/name-size/marker-size 설정

호환을 위해 일부 old type/method가 compile에 남을 수 있지만 사용자에게 접근 가능한 제품 동작으로 취급하지 않습니다.

---

# 12. Map bundle 데이터 정책

현재 artwork/config/general-marker DB는 **검증된 pinned bundle**로 배포합니다.

이는 Quest/Hideout/Item/Ammo 온라인 Game Content updater와 분리됩니다.

향후 Map bundle updater를 구현할 경우 다음을 같은 upstream revision의 한 unit으로 처리해야 합니다.

```text
same revision
├─ artwork
├─ config
└─ general-marker DB
```

서로 다른 revision을 섞은 candidate를 active로 만들지 않습니다.

---

# 13. 자동 검증 기준

Map smoke는 단순히 Main Window가 켜지는지만 보지 않습니다.

Release candidate에서 최소 다음을 실제 WPF runtime으로 검증합니다.

- exact Map subsystem 초기화
- multi-floor Map 선택
- Main Map dropdown floor 전환 시 SVG source 실제 변경
- 타층 floor relation이 `Floor.Order` 기준 Above/Below로 계산되는지 확인
- 현재/위/아래 marker floor indicator가 초록/빨강/파랑 의미를 갖는지 확인
- 알려진 타층 marker가 약 75% opacity로 유지되고 floor 때문에 Collapsed되지 않는지 확인
- 실제 async Main Map standard-marker build와 bounded settle 이후에도 enabled known off-floor marker가 `Opacity=0`으로 사라지지 않는지 확인
- same-type cross-floor near-overlap이 존재해도 타층 일반 marker를 대표 하나로 축약하지 않는지 확인
- product floor hotkey가 실제 Main Map SVG를 변경
- product floor hotkey 전후 Main Map zoom 유지
- product floor hotkey 전후 Main Map viewport 중앙의 map-space 좌표 유지
- MiniMap 실제 window 표시
- MiniMap `ResizeMode=NoResize`
- legacy bottom-right resize grip 없음
- live MiniMap marker에 product marker scale 적용
- 100% → 50% scale에서 실제 `RenderTransform` 감소
- transplanted legacy zoom/floor hotkey mapping이 0으로 비활성
- MiniMap zoom API 사용 시 실제 ZoomLevel 변경
- MiniMap floor product API 사용 시 실제 floor indicator 변경
- PlayerTracking 상황처럼 live `MapTranslate`와 persisted `MapOffsetX/Y`가 서로 다르더라도 MiniMap floor 변경 전후 zoom 유지
- 같은 조건에서 MiniMap floor 변경 전후 viewport 중앙의 map-space X/Y 유지
- floor render 완료 후 persisted `MapOffsetX/Y`와 live `MapTranslate` 재동기화
- zoom/floor 이후에도 legacy hook이 다시 활성화되지 않음
- Main Window 정상 close 후 process 종료

자동 검증은 사용자 실사용 검증을 대체하지 않지만, 과거 발생했던 “코드는 연결됐지만 실제 Main Map/MiniMap에서는 안 됨” 회귀를 CI에서 막는 역할을 합니다.

---

# 14. 현재 완료 기준

현재 public release v0.1.4에서 확인된 두 Map 회귀를 v0.1.5에서 수정합니다.

- Main Map에서 enabled 타층 marker가 async 로딩과 안정화 이후에도 실제로 보임
- current-floor-only visibility loop 없음
- current/above/below relation을 초록/빨강/파랑 compact ring으로 표시
- 위/아래의 방향 glyph는 marker를 가리지 않는 매우 작은 보조 표현
- same-type cross-floor near-overlap을 이유로 타층 일반 marker를 숨기지 않음
- 실제 같은 물리 항목의 semantic duplicate extract 정규화는 유지
- floor/map 변경 뒤 legacy async refresh가 relation presentation을 영구적으로 덮어쓰지 않음
- permanent full-tree polling을 재도입하지 않음
- Main Map floor 변경 전후 zoom + map-space 중심 유지
- MiniMap floor 변경 전후 zoom + map-space 중심 유지
- PlayerTracking의 stale persisted offset이 MiniMap floor 변경 시 live viewport를 초기화하지 않음
- floor up/down과 NumPad direct floor selection 모두 viewport-safe 경로 사용
- MiniMap 기존 opacity / marker scale / 우측 상단 고정 / click-through 정상 동작 유지

향후 변경은 실제 사용 중 새 버그, 새 제품 요구사항, upstream Map bundle 갱신 또는 게임 패치로 pinned Map data가 실제 게임과 불일치할 때 진행합니다.
