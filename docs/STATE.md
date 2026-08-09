# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + Windows 실사용 피드백 반복 개선**

상태: `MAP QUEST RENDER / FLOOR / MARKER HOTFIX MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

기준일: **2026-08-10**

---

# 제품 전체 원칙

온라인 게임 데이터는 프로그램이 직접 내려받고 다음 흐름으로 갱신합니다.

```text
온라인 source
→ 다운로드
→ 형식 검증
→ canonical 변환
→ candidate
→ 검증
→ active 교체
→ 실패 시 마지막 정상 데이터 유지
```

- 일반 Tarkov 데이터 내용 변경은 importer 규칙으로 자동 대응합니다.
- 외부 형식/의미 자체가 바뀐 경우에만 코드 변경이 필요합니다.
- runtime GPT/AI 의존성은 없습니다.
- Game Content와 사용자 진행도 `user.db`는 분리합니다.
- update 실패가 사용자 진행도나 마지막 정상 데이터를 손상시키면 안 됩니다.

---

# Core 제품 상태

구현됨:

- Profile: mode별 profile, level/faction/edition/prestige/trader/Fence
- Quest: 진행/잠김/사용불가/완료, prerequisite, item requirement, inventory ledger
- Hideout: 시설 level, next upgrade material, upgrade/rollback ledger
- Needed Items: FIR/일반 필요량/보유량/검색/필터
- Ammo: json.tarkov.dev raw 성능 + Wiki Ballistics 비교 + favorite
- Map + MiniMap: Tarkov Helper exact subsystem 기반 제품화 진행 중

Scanner는 탭/placeholder만 있으며 실제 기능 요구사항은 아직 확정 전입니다.

Windows publish는 한국어 제품 정책에 따라 `SatelliteResourceLanguages=ko`를 사용합니다.

사용자 제공 얼굴 이미지를 다음 brand icon으로 사용합니다.

- `JunhyunHelper.exe` icon
- Window icon
- 좌측 상단 `준현 헬퍼` 왼쪽 brand icon

---

# Map 시스템 기준

## Exact Tarkov Helper 기준선

JunhyunHelper에서 자체적으로 새로 만들었던 Map 구현은 폐기했습니다.

PR #62에서 `Propeex/Tarkov-Helper`의 Map + MiniMap subsystem을 exact source 기준으로 이식했고 사용자가 Windows에서 이 지도 artwork/구조를 원하는 기준으로 직접 확인했습니다.

```text
exact baseline PR #62 merge:
4b3d43051b48c3d00ab8fdba03814d24066a2fd0

exact baseline Tarkov-Helper revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818

product source repository:
Propeex/Tarkov-Helper

product source branch:
junhyun-map-product-v2

currently pinned source revision:
d933792b6042a51cea38dc44b686a096fe30de67

JunhyunHelper submodule:
vendor/Tarkov-Helper
```

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

## Map 독립성

사용자 확정 원칙:

```text
Map subsystem = 독립
└─ Quest만 JunhyunHelper 현재 데이터와 연결
```

Map이 JunhyunHelper에서 읽는 제품 정보:

- 현재 profile의 Quest 진행 상태
- online Quest location geometry

Hideout / Item / Ammo 등과 Map runtime을 결합하지 않습니다.

상세 요구사항: `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# 현재 Map UI / 기능

## Quest sidebar

- 현재 선택 Map의 **진행 중(Current) Quest만** 표시합니다.
- 기본 접힘 상태이며 펼치면 300px입니다.
- 접으면 실제 지도 영역이 넓어집니다.
- Quest 행 클릭 → JunhyunHelper `퀘스트` 탭 → 해당 Quest 상세로 이동합니다.
- 정확한 좌표가 없는 Quest도 목록에는 남기고 `정확한 좌표 없음`으로 표시합니다.
- 좌표 Quest는 개별 marker checkbox를 가집니다.
- 표시 대상으로 선택된 Quest는 sidebar 순서대로 `A`, `B`, `C`... 식별자를 사용합니다.
- 한 Quest에 위치가 여러 개면 같은 식별자를 공유합니다.
- checkbox lane / A-B-C badge lane / Quest text lane은 고정 column입니다.
- A/B/C badge 자체의 X 위치와 Quest text 시작점이 모든 row에서 동일해야 합니다.

## Quest 좌표 데이터

Quest content schema는 **v4**입니다.

온라인 task objective에서 다음 위치 정보를 Quest domain에만 저장합니다.

- `possibleLocations`
- `zones`
- Map ID
- X/Z
- source가 제공한 경우 Height
- zone outline/top/bottom

기존 v3 snapshot은 offline fallback으로 계속 읽습니다. Map 최초 사용 시 v4 online update를 한 번 시도하고 실패하면 기존 v3 + `user.db`를 유지합니다.

중요한 현재 검증 결과:

```text
sidebar의 `좌표 N개`
= raw metadata count가 아님
= MapLocations 읽기 성공
→ 현재 Map 필터 성공
→ MapTrackerService.TransformGameCoordinate(X,Z) 성공
→ 실제 render projection으로 남은 개수
```

따라서 사용자 화면에 `좌표 2개`, `좌표 3개`가 보이면 Quest 좌표 수집과 Map 좌표 변환은 이미 성공한 상태입니다.

## Quest marker

- 우측 `지도 마커 > 퀘스트`에 제품용 `퀘스트 마커 표시` global checkbox가 있습니다.
- global OFF는 per-Quest 선택 상태를 지우지 않습니다.
- Main Map / MiniMap은 동일 A/B/C identity를 사용합니다.
- PR #67부터 Quest marker visual은 exact Tarkov Helper 일반 marker와 동일한 **0x0 Canvas anchor + child offset** 방식의 V3 renderer를 사용합니다.
- 이전의 `0x0 Grid + child RenderTransform` 방식은 Windows에서 projection이 존재해도 badge가 arrange되지 않을 수 있어 폐기했습니다.

## 일반 marker 설정

현재 section:

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

bundled DB 검토:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 빈 ScavSpawn/Keys UI는 만들지 않고 Raider만 추가했습니다.

PR #67에서 직전 Windows artifact의 일반 marker 데이터를 추가 검증했습니다.

```text
MapMarkers records: 454
playerMarkerTransform 후 image bounds 밖: 0
multi-floor FloorId와 config layerId 불일치: 0
```

일부 marker 미표시 원인은 좌표 손상이 아니라 V2 visibility bridge가 원본 category 상태를 덮어쓰던 충돌로 확인했습니다.

현재 규칙:

```text
marker visible
= 현재 선택 floor에 해당
AND 실제 화면의 해당 category checkbox가 ON
```

Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시합니다.

## Floor

- screenshot으로 floor를 판정하지 않습니다.
- 사용자가 floor selector / floor hotkey로 직접 선택합니다.
- V2에서 만들었던 복제 floor ComboBox는 PR #67에서 폐기했습니다.
- **exact Tarkov Helper 원본 `CmbFloorSelect`와 원본 `CmbFloorSelect_SelectionChanged` 로직을 직접 사용**합니다.
- 선택된 현재 floor만 표시합니다.
- non-selected floor opacity는 0% 정책입니다.
- floor selection 직전 현재 선택 floor를 visual default로 지정하여 exact loader가 예전 default floor를 반투명 background로 추가하지 않게 합니다.

## Screenshot tracking

사용:

- Map 감지
- 감지 Map으로 Main Map 자동 전환
- player X/Z 위치
- 가능한 경우 heading

사용하지 않음:

- floor 자동 판정/전환

## MiniMap 고정 정책

- 우측 상단 anchor
- drag 이동 불가
- resize 후 우측 상단으로 자동 재배치
- 기본 opacity 100%
- cursor hover 시 일시적으로 완전 투명, 이탈 즉시 100% 복귀
- click-through 항상 ON
- ViewMode 항상 PlayerTracking
- 다른 floor opacity 0%
- AutoFloorSelection OFF
- Main Map과 Quest/general marker 표현 동기화

## Hotkey

Main Map `설정` 안에서 편집합니다.

- MiniMap ON/OFF
- Map zoom in/out
- floor up/down
- MiniMap size increase/decrease

규칙:

- 같은 key는 한 동작에만 지정
- 새 배정이 이전 배정을 해제
- Delete / Backspace = 미지정
- Esc = 취소
- NumPad 0~5 = 직접 floor 선택 예약

`자동 층 추적 복귀`는 삭제했습니다.

## 제거된 Map 기능

- Full Screen
- Custom Marker
- screenshot floor auto-detection
- Fixed View 선택
- PlayerTracking 선택 UI
- click-through 선택 UI
- 다른 층 opacity 설정
- 현재 층만 표시 설정
- auto-floor 관련 설정/복귀
- MiniMap 도움말 `?`
- 의미 없는 old Quest marker style/color/name-size/marker-size 설정

---

# 최근 Map PR

## PR #64 — Map product refinement V2

```text
merge: 2339ddff5773ee385ff32b4ff5a173aab52d8050
CI: 31320921128
```

Quest sidebar, marker grouping, MiniMap fixed policies, hotkey UI 등 V2 제품 요구사항 구현.

## PR #65 — Windows hotfix / screenshot / icon

```text
merge: 480a49ce7df5f1a17ca91d1caecbb6a81451811a
CI: 31324134472
```

- screenshot Map UI 전환 bug 수정
- 사용자 brand icon 적용

상세: `docs/MAP_V2_HOTFIX_2026-08-10.md`

## PR #66 — Quest UI / Korean-only publish

```text
merge: 2f9f07f64d9c6a8259504a8425c254a95673f8ea
CI: 31325539763
```

- Quest marker global product checkbox
- sidebar 1차 정렬 보정
- Korean-only satellite resource publish

상세: `docs/MAP_V2_FEEDBACK_2026-08-10_02.md`

## PR #67 — Quest rendering / floor / marker visibility — MERGED

```text
PR: #67 Fix Quest marker rendering and restore floor switching
merge: 7d248d7346760d126b839d69318648e504ac39fc
final head: 81cddd9bcd151a9b4bea19d764e00cc1798f7d65
CI: 31328655090
artifact: 9042291967
artifact digest: sha256:bd0b18d3a9d54bd12b3e797f8f2b898a9fc57326b34d783a48e286dcf1a232bc
```

최종 자동 검증:

```text
Desktop Release build: success
automated tests: success
Windows x64 self-contained publish: success
enhanced Startup + Map smoke: success
ZIP creation/upload: success
```

강화된 Map smoke에서 실제로 확인:

- V3 Quest Canvas marker visual 생성
- Customs multi-floor selector 생성
- 다른 floor 선택
- floor 변경 후 Map SVG source 교체

상세: `docs/MAP_V2_FEEDBACK_2026-08-10_03.md`

---

# 다음 작업

1. PR #67 Windows 테스트 빌드 사용자 검증
2. 확인:
   - A/B/C badge 자체가 동일 X 위치인지
   - `좌표 N개` Quest의 A/B/C가 Main Map에 실제 표시되는지
   - MiniMap에도 동일 Quest marker가 표시되는지
   - Customs / Reserve / Factory 등 multi-floor Map에서 floor 변경이 실제로 동작하는지
   - 선택하지 않은 floor가 보이지 않는지
   - 일반 marker checkbox ON/OFF와 실제 marker 표시가 일치하는지
3. 실사용 차이가 있으면 해당 경로만 수정
4. 시각/동작 검증 완료 후 Map artwork/config/general-marker DB를 동일 revision 단위로 교체하는 atomic Map bundle updater 구현
