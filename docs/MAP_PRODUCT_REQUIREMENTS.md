# MAP PRODUCT REQUIREMENTS — 준현 헬퍼 Map + MiniMap

기록일: 2026-08-09  
최종 갱신: 2026-08-27

상태: **v1.7.14 PUBLIC STABLE / IMPLEMENTED / MAINTENANCE MODE**

정확한 현재 release proof는 `docs/STATE.md`를 사용한다. 이 문서는 Map/MiniMap의 현재 제품 의미와 장기 유지보수 경계를 정의한다.

---

# 1. 제품 기준과 시스템 경계

Map/MiniMap은 사용자가 검증한 donor subsystem을 제한적으로 사용한다.

```text
product source repository:
SIGDrone/Tarkov-Helper

currently pinned revision:
d933792b6042a51cea38dc44b686a096fe30de67

JunhyunHelper submodule:
vendor/Tarkov-Helper
```

이 선택은 donor 전체를 JunhyunHelper 제품 사양으로 승계한다는 뜻이 아니다. **Map/MiniMap의 검증된 특정 source만 compile-link 하는 예외**다.

핵심 아키텍처:

```text
Map subsystem = 독립
└─ JunhyunHelper cross-feature bridge = Quest
   ├─ current profile / Quest state
   └─ current Quest location geometry
```

Hideout / Items / Ammo / Scanner domain truth를 Map donor에 넣지 않는다.

JunhyunHelper UI delta는 donor source를 직접 broad-edit하지 않고 first-party partial/customization boundary에서 적용한다.

대표 boundary:

- `src/JunhyunHelper.Desktop/MainWindow.LegacyMapHost.cs`
- `src/JunhyunHelper.Desktop/MainWindow.MapSmokeV014.cs`
- `src/JunhyunHelper.Desktop/Map/MapPage.JunhyunUiSimplification.cs`
- `src/JunhyunHelper.Desktop/Map/*`

---

# 2. Map Quest sidebar

왼쪽 sidebar는 **현재 선택 Map + 현재 profile에서 Current(진행 중)인 Quest만** 표시한다.

`확인 필요(Indeterminate)` Quest는 현재 진행 중임을 입증할 수 없으므로 sidebar에서 제외한다.

- 기본 접힘
- 펼침 폭 약 300px
- 접으면 지도 viewport가 실제로 넓어짐
- Quest 행 클릭 → JunhyunHelper Quest 탭의 해당 Quest 상세
- 정확한 위치 geometry가 없는 Quest도 목록 유지
- 위치가 없으면 `정확한 좌표 없음`
- 좌표를 추측하지 않음

## Quest marker

- `퀘스트 마커 표시` global toggle
- 좌표가 있는 Quest별 individual marker toggle
- global OFF는 individual 선택 상태를 지우지 않음
- individual OFF인 Quest는 Main Map/MiniMap 모두 숨김
- 표시 대상 Quest를 sidebar 순서대로 `A`, `B`, `C`... 식별
- 한 Quest의 여러 위치는 같은 식별자 공유
- sidebar / Main Map / MiniMap 모두 같은 식별자 사용
- 표시 대상/순서가 바뀌면 current sidebar 기준으로 재계산

Quest 위치 source:

```text
online possibleLocations / zones
→ canonical Content
→ donor-compatible coordinate transform
→ JunhyunHelper Quest projection
```

신뢰 가능한 Height가 있을 때만 floor를 분류한다. Height가 없으면 floor를 추측하지 않는다.

---

# 3. 일반 marker / 탈출구

제품 marker group은 pinned bundle에 실제 데이터가 존재하는 범위에서 제공한다.

대표 category:

```text
Quest
PMC Spawn
Sniper Scav
Rogue
Cultist
Boss
Raider
Lever
PMC Extract
Scav Extract
Transit
```

Bundled DB에 실제 데이터가 없는 category를 UI에 억지로 만들지 않는다.

표시 규칙:

```text
해당 category/faction ON
AND marker가 현재 Map에 속함
→ floor와 관계없이 marker/extract visual 유지
```

**Floor는 visibility filter가 아니다.** 다른 층이라는 이유만으로 marker를 `Collapsed`하거나 opacity 0으로 만들지 않는다.

Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시한다. Main Map과 MiniMap의 일반 marker/extract 표시 설정은 동기화한다.

---

# 4. Main Map UI 계약 — v1.7.14

Map 조작 surface는 기능을 유지하면서 반복 조작, donor 잔여 chrome, 별도 settings presentation을 JunhyunHelper 제품 규칙에 맞춘다.

## 4.1 지도 마커 selector

- 기본 상태 = 접힘
- 별도 arrow launcher 없음
- `지도 마커` 버튼 자체가 open/close toggle
- same launcher 재클릭 → 닫힘
- launcher는 donor transparent text-like local values가 아니라 **JunhyunHelper 일반 Button chrome** 사용
- collapsed 상태에서 selector container는 빈 min-width/padding/background/border를 남기지 않음
- expanded 상태에서 일반 desktop map viewport의 현재 marker checkbox를 가능한 한 한 화면에 표시할 충분한 세로 공간 확보
- selector open/close 자체가 marker enable state를 바꾸지 않음

현재 implementation은 map viewport height를 기준으로 expanded panel 높이를 bounded 계산한다. 특정 해상도 절대값에 맞춘 고정 full-screen panel이 아니다.

## 4.2 MiniMap launcher

MiniMap launcher 주변에 donor wrapper의 불필요한 빈 공간이 남으면 안 된다.

- hidden donor help button은 layout/interaction에 개입하지 않음
- help button을 위해 남아 있던 parent Border padding/background/border 제거
- 실제 themed MiniMap button이 launcher chrome을 단독 소유

## 4.3 Map / MiniMap Settings

v1.7.14부터 product Settings는 donor 오른쪽 drawer를 최종 user-facing surface로 사용하지 않는다.

```text
Map Settings launcher
→ donor SettingsPanel detach
→ MainWindow.ShowInAppElementAsync("map-settings", ...)
→ shared in-app overlay
→ dismiss
→ donor SettingsPanel original parent/index 복원
```

공통 dismiss:

- same settings launcher 재클릭
- backdrop click
- common overlay X

SettingsPanel의 기존 controls/settings authority는 유지한다. MainWindow가 Map preference 의미를 재구현하지 않는다.

Overlay에 temporary re-parent된 `SettingsPanel`은 닫힌 뒤 original visual tree parent/index로 반드시 복원한다.

## 4.4 Trail / route

v1.7.13부터 trail/path는 JunhyunHelper 제품 surface가 아니다.

- route/trail visual 표시 안 함
- `경로 지우기` control 표시 안 함
- 숨겨진 stale trail geometry가 hit-test/presentation에 개입하지 않음

## 4.5 설명 copy

Long hotkey instruction copy는 제품 Map surface에서 제거 상태를 유지한다. 기능성 hotkey 자체는 유지한다.

현재 first-party implementation boundary:

```text
src/JunhyunHelper.Desktop/Map/MapPage.JunhyunUiSimplification.cs
```

Donor revision은 이 UI 변경 때문에 바뀌지 않는다.

---

# 5. Floor 정책

Floor는 사용자가 직접 선택한다.

허용 방식:

- donor floor dropdown
- floor up/down product hotkey
- NumPad 0~5 direct floor selection compatibility

정책:

- screenshot으로 floor를 자동 판정하지 않음
- AutoFloorSelection OFF
- 지도 artwork는 선택한 current floor만 표시
- enabled marker/extract는 다른 floor라는 이유만으로 숨기지 않음
- 다른 floor opacity를 사용자가 별도 임의 policy로 제어하지 않음
- 자동 층 추적 복귀 없음

## 5.1 marker floor 표현

Current selected floor와 marker floor를 Map config의 `Floor.Order`로 비교한다.

Marker 고유 type/icon 의미는 유지하고 floor relation은 compact indicator로 표현한다.

```text
현재 층 marker → Current relation
위층 marker     → Above relation
아래층 marker   → Below relation
floor 불명확    → 방향 추측 안 함
```

Current/above/below를 작은 ring과 필요 시 방향 glyph로 구분한다. Indicator가 marker 자체를 가리거나 hit-test를 가져가면 안 된다.

## 5.2 서로 다른 floor의 near-overlap

서로 다른 known floor의 일반 marker가 같은 type이고 X/Z상 겹치거나 가까워도 그것만으로 대표 하나만 남기지 않는다.

```text
category ON
AND marker가 current Map에 속함
→ 각 marker visual 유지
→ 각 marker floor relation presentation 적용
```

서로 다른 floor라는 사실은 같은 물리 항목이라는 증거가 아니다.

Source상 실제 같은 물리 항목의 semantic duplicate임을 확인할 수 있을 때만 별도 duplicate normalization을 적용한다.

## 5.3 floor 변경 시 viewport 보존

Floor change는 artwork change이지 viewport reset이 아니다.

Main Map:

```text
current zoom + viewport center의 map-space coordinate 저장
→ floor artwork/marker refresh
→ same zoom + same map-space center 복원
```

MiniMap:

```text
current live transform 기준 zoom/map-space center 저장
→ floor artwork 변경
→ same zoom + same map-space center 복원
→ persisted offset과 live transform 재동기화
```

MiniMap PlayerTracking의 live transform을 stale persisted offset이 덮어써서 floor change 시 초기 위치로 되돌리면 안 된다.

## 5.4 async refresh 안정화

Donor async refresh가 map/floor/filter 변경 직후 visual tree를 다시 만들 수 있다.

허용:

- 실제 map/floor/filter event 기반 refresh
- bounded stabilization
- O(1) structure signature 기반 필요한 재적용

금지:

- 프로그램 전체 lifetime marker full-tree permanent polling
- floor가 다르다는 이유로 반복 `Collapsed`/opacity 0 처리
- async load 뒤 cross-floor marker를 뒤늦게 제거
- floor render 뒤 stale persisted offset으로 live MiniMap viewport 덮어쓰기

---

# 6. Screenshot tracking

Screenshot은 다음 용도로 유지한다.

- current Map 감지
- 감지된 Map으로 자동 전환
- Player X/Z 위치 추적
- 가능한 경우 heading 표시

사용하지 않는 목적:

- floor 자동 판정

Screenshot 실패/미사용 상태에서도 manual Map/floor 조작은 가능해야 한다.

---

# 7. Main Map 제품 surface

유지:

- Map 선택 / floor 선택
- Quest sidebar / Quest markers
- 일반 marker/extract selector
- shared-overlay product Settings launcher
- screenshot tracking
- product hotkeys
- MiniMap launcher

비노출/제거:

- Full Screen
- custom marker 추가/편집/삭제 user surface
- screenshot floor auto-detection
- Fixed View selector
- current-floor-only visibility policy
- 다른 floor를 숨기는 별도 policy
- old Quest marker style/color/name-size/marker-size 설정
- trail/path visual + `경로 지우기`
- long hotkey explanatory copy
- hidden MiniMap help control의 residual layout chrome
- donor right-drawer를 final user-facing Settings surface로 쓰는 방식

Old donor type/method가 compatibility를 위해 compile에 남을 수 있으나 current product surface가 아니면 이름만 보고 다시 활성화하지 않는다.

---

# 8. MiniMap 제품 정책

MiniMap은 Main Map과 동일한 selected map/floor 및 marker relation 의미를 사용한다.

핵심:

- Topmost/no-activate product overlay
- click-through gameplay behavior 유지
- PlayerTracking 중심 product behavior 유지
- manual selected floor 사용
- artwork는 selected floor만 표시
- enabled 다른 층 marker/extract는 숨기지 않고 floor relation 표현
- floor change 시 live viewport 보존
- first-open 전에 current Main Map selection을 `MapTrackerService`에 동기화
- window width/height는 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장·복원하고 safe range로 clamp

MiniMap의 구체적인 resize/position/input implementation을 변경할 때는 current code와 actual smoke를 먼저 확인한다. 오래된 donor UI를 제품 요구사항으로 복원하지 않는다.

## Temporary hide / opacity

제품 설정이 제공하는 temporary-hide 및 normal opacity 의미는 유지한다.

```text
hide condition active
→ MiniMap temporarily invisible

hide condition inactive
→ configured normal opacity
```

Persisted JunhyunHelper product setting이 donor legacy opacity보다 우선한다.

## MiniMap marker scale

Player marker와 일반 marker scale ownership을 구분한다. 일반 marker scale은 zoom compensation 뒤 product scale을 적용한다.

Main Map/MiniMap에서 Quest/general/extract marker의 floor relation 의미가 일치해야 한다.

---

# 9. Global hotkey

Map product hotkey는 JunhyunHelper-owned persisted binding을 사용한다.

대표 동작:

- MiniMap ON/OFF
- Main Map + MiniMap zoom in/out
- floor up/down
- MiniMap size control where currently supported
- MiniMap temporary hide

Current matching contract:

- primary key 일치 필수
- 등록된 Ctrl/Alt/Shift는 모두 필요
- 등록하지 않은 Ctrl/Alt/Shift 추가 입력 허용
- same primary key의 compatible binding 중 더 많은 modifier를 요구하는 more-specific binding 우선
- 동률은 stable existing priority
- Windows modifier 미지원
- bare NumPad0~5 direct floor selection 유지

Product hotkey는 Tarkov foreground에서도 동작해야 한다. Donor legacy direct hotkey가 same gesture를 중복 소유하지 않게 한다.

Long hotkey explanatory copy는 제거 상태지만 hotkey 기능은 유지한다.

---

# 10. Map 제품 설정 저장

Authority path:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

대표 저장값:

- general marker/extract/Quest marker toggles
- marker/player/extract presentation settings
- screenshot folder
- product hotkeys
- MiniMap temporary-hide / opacity / marker scale 등 current product preferences

Important JSON은 atomic replacement + `.bak` recovery 원칙을 따른다.

Donor async initialization이 legacy 값을 다시 적용하더라도 JunhyunHelper product setting이 final authority가 되도록 한다.

Shared overlay로 표시 위치가 바뀌어도 설정 저장 authority는 바뀌지 않는다.

---

# 11. Map bundle 데이터 정책

Artwork/config/general-marker DB는 verified pinned donor bundle로 배포한다.

Quest/Hideout/Items/Ammo 일반 online Game Content updater와 Map donor bundle lifecycle을 섞지 않는다.

향후 Map bundle을 갱신할 경우 same upstream revision의 한 unit으로 검증한다.

```text
same revision
├─ artwork
├─ config
└─ general-marker data
```

서로 다른 revision을 섞은 candidate를 active로 만들지 않는다.

---

# 12. 자동 검증 기준

Actual published EXE Map smoke는 단순 startup만 검증하지 않는다.

대표 contract:

- exact pinned Map subsystem 초기화
- multi-floor Map 선택
- floor transition 시 artwork source 실제 변경
- current/above/below floor relation 계산
- enabled known off-floor marker가 async settle 뒤에도 숨지 않음
- cross-floor near-overlap을 이유로 marker 제거하지 않음
- product floor action 전후 Main Map zoom + map-space center 유지
- Factory extract/marker floor identity regression
- MiniMap 실제 표시/initial map sync
- MiniMap floor change 전후 zoom + map-space center 유지
- stale persisted offset이 live PlayerTracking viewport를 덮어쓰지 않음
- marker product scale/opacity behavior 유지
- donor duplicate hotkey hook 비활성/중복 방지
- Main Window normal close 후 process 종료

v1.7.13 Product UI smoke에서 유지되는 contract:

- 지도 마커 selector 기본 접힘
- `지도 마커` launcher open/close toggle
- trail/clear-trail product surface 제거

v1.7.14 deterministic/source + actual Map/Product smoke가 추가로 보호:

- MiniMap launcher residual chrome cleanup
- `지도 마커` product Button chrome
- collapsed marker panel empty chrome 제거
- expanded marker panel height policy
- Map Settings shared-overlay route
- actual Main Map/Factory/MiniMap rendering regression 없음

---

# 13. 현재 완료 상태

v1.7.14 public stable에서 Map/MiniMap은 **IMPLEMENTED / MAINTENANCE MODE**다.

```text
exact product release source/tag target:
0a51375de36cd13047216006c2c0311728b1bd89

main CI:
33060827905 — SUCCESS

Product UI / Main Map / Factory / MiniMap smoke:
SUCCESS

407 passed / 0 failed / 0 skipped
```

공식 최신 UI 결정:

- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/RELEASE_1.7.14.md`

향후 변경은 다음 중 하나의 근거가 있을 때만 진행한다.

- actual usage에서 재현 가능한 Map/MiniMap bug
- 사용자가 확정한 새 제품 요구사항
- pinned donor bundle update 결정
- Tarkov 변화로 current bundled Map data가 실제 게임과 불일치한다는 evidence

단순 cleanup이나 donor code 미관을 이유로 broad rewrite하지 않는다.
