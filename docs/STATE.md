# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + Windows 실사용 피드백 반복 개선**

상태: `PR #69 MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

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
- Map + MiniMap: Tarkov Helper exact subsystem 기반 제품화

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

PR #62에서 `Propeex/Tarkov-Helper`의 Map + MiniMap subsystem을 exact source 기준으로 이식했고, 사용자가 Windows에서 이 지도 artwork/구조를 원하는 기준으로 직접 확인했습니다.

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

# 현재 Map 제품 동작

## Quest sidebar / marker

- 현재 선택 Map의 **진행 중(Current) Quest만** 표시
- 기본 접힘, 펼침 300px
- Quest 행 클릭 → JunhyunHelper Quest 상세 이동
- 좌표가 없는 Quest도 목록 유지 + `정확한 좌표 없음`
- 좌표 Quest별 marker checkbox
- 표시 대상 Quest를 sidebar 순서대로 `A`, `B`, `C`... 식별
- 한 Quest의 여러 위치는 같은 식별자 공유
- checkbox / A-B-C badge / Quest text 고정 column 정렬
- 우측 `지도 마커 > 퀘스트` global toggle 제공
- global OFF는 개별 Quest 선택 상태를 지우지 않음
- Main Map / MiniMap 동일 A/B/C identity
- Quest visual은 PR #67부터 **0x0 Canvas anchor + child offset** V3 renderer 사용

Quest content schema는 **v4**이며 online `possibleLocations` / `zones`를 Quest domain에 저장합니다. v3는 offline fallback으로 계속 읽습니다.

`좌표 N개` 표시는 raw metadata count가 아니라 Map filter와 `MapTrackerService.TransformGameCoordinate(X,Z)`까지 성공한 최종 projection 개수입니다.

## Marker

현재 product marker group:

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

bundled DB:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

일반 marker 표시 규칙:

```text
현재 선택 floor
AND 해당 category checkbox ON
→ 표시
```

Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시합니다.

## Floor / screenshot

- screenshot으로 floor를 판정하지 않음
- 사용자가 exact Tarkov Helper 원본 `CmbFloorSelect` 또는 floor hotkey로 선택
- floor hotkey는 PR #69부터 원본 `CmbFloorSelect.SelectionChanged` 경로와 MiniMap `MoveFloorUp/Down`을 함께 실행
- 현재 선택 floor만 표시
- 다른 floor opacity 0%
- screenshot 사용: Map 감지/자동 Map 전환/player X-Z/가능한 경우 heading

## MiniMap 고정 정책

- 우측 상단 anchor
- drag 불가
- 마우스 resize 불가
- 우측 하단 legacy resize grip 제거
- 크기 조절은 size increase/decrease hotkey만 사용
- resize 후 우측 상단 자동 재배치
- opacity 100%
- cursor hover → 일시 0%, 이탈 → 100%
- click-through 항상 ON
- ViewMode = PlayerTracking 고정
- 다른 floor opacity 0%
- AutoFloorSelection OFF
- Main Map과 Quest/general marker 표현 동기화

## Hotkey

Main Map `설정`에서 편집합니다.

- MiniMap ON/OFF
- Main Map + MiniMap zoom in/out
- Main Map + MiniMap floor up/down
- MiniMap size increase/decrease
- MiniMap 일시 투명
- 일시 투명 시간 1~15초

규칙:

- 같은 key는 마지막으로 지정한 한 동작에만 남음
- Delete / Backspace = 미지정
- Esc = 취소
- NumPad 0~5 = 직접 floor 선택 예약
- JunhyunHelper-owned persisted hotkey가 runtime 권위값
- `EscapeFromTarkov`, `EscapeFromTarkov_BE`, `JunhyunHelper`, `TarkovHelper` foreground에서 전역 hotkey 허용

MiniMap timed hide와 hover hide는 같은 presentation loop에서 결합합니다.

```text
timed hide 활성 OR cursor hover
→ opacity 0%

둘 다 비활성
→ opacity 100%
```

## Map 설정 저장

PR #68부터 Map 제품 설정은 JunhyunHelper가 소유합니다.

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

저장 대상:

- 일반 / 탈출구 / Raider / Quest global marker toggle
- 개별 Quest A/B/C marker toggle
- marker/player/extract 조정값
- Map 설정 combo 값
- screenshot 폴더
- 제품 hotkey
- MiniMap timed-hide duration

legacy Tarkov Helper가 async 초기화 후반에 옛 hotkey 값을 읽더라도 JunhyunHelper product 설정이 최종 권위값이 되도록 초기화 안정 구간에 재적용합니다. PR #69부터 실제 key dispatch도 이 persisted 값을 직접 우선 조회합니다.

## 제거된 Map 기능

- Full Screen
- Custom Marker
- screenshot floor auto-detection
- Fixed View 선택
- PlayerTracking 선택 UI
- click-through 선택 UI
- MiniMap mouse resize / 우측 하단 resize grip
- 다른 층 opacity 설정
- 현재 층만 표시 설정
- auto-floor 관련 설정/복귀
- MiniMap 도움말 `?`
- 별도 MiniMap settings 진입 UI
- 의미 없는 old Quest marker style/color/name-size/marker-size 설정

---

# PR #68 — Windows settings / input / lifecycle / performance — MERGED

```text
PR: #68 Fix Windows product settings, controls, and lifecycle
merge: f75644002766f45fc0b1d0929ab556bba55a801a
final head: b990c32544c8740851e4b4f86d30918e0a218599
CI: 31349320391
artifact: 9048426054
artifact digest: sha256:4ae8a9d530d710714b7a1b4606686f7f8d8cc3cce3673644fb30217eeeaaf112
```

상세: `docs/PRODUCT_FEEDBACK_2026-08-10_04.md`

## 반영 사항

1. Map marker / Quest marker / hotkey / 사용자 조정값 재시작 영속화
2. Hideout / Items / Ammo icon cache를 cold start부터 연결
3. Main Map zoom/floor hotkey 직접 동작
4. 별도 MiniMap settings 진입 UI 제거
5. 설정 가능한 MiniMap N초 일시 투명 기능
6. Ammo inactive-selection 백화 수정
7. Ammo vertical column separator 추가
8. Main Window 종료 시 Map/MiniMap/hook cleanup + process 종료 보장
9. Map viewport clip + marker overlay max-height/scroll
10. 상태 변경 성능 개선
11. 기존 Profile editor close-to-save

## 성능 개선 기준

지연은 제품 특성상 불가피한 것으로 보지 않습니다. PR #68에서 다음 중복 작업을 제거했습니다.

- `UserProfileStore` process-local canonical profile cache
- Quest / Hideout / Items workspace memoization
- Quest 변경: Quest 결과 재사용 + Items 영향 갱신, Hideout rebuild 생략
- Hideout 변경: Hideout 결과 재사용 + Quest/Items 영향 갱신
- Item 수량 변경: Items 결과 재사용 + Quest 영향 갱신, Hideout rebuild 생략
- Item +/- 약 160ms 연속 입력 coalescing
- Hideout level +/- 약 180ms 연속 입력 coalescing
- Items/Hideout 기존 icon 재사용

cache는 persisted SQLite round-trip과 동일한 canonical normalization을 유지합니다. 기존 자동 테스트가 `PrestigeLevel null → 0` 차이를 검출했고 이 의미 차이가 생기지 않도록 수정했습니다.

## 종료 자동 검증

CI는 이제 force-kill만 사용하는 smoke가 아닙니다.

```text
published JunhyunHelper.exe 실행
→ exact Map subsystem 초기화
→ Main Window에 정상 close 요청
→ 7초 안에 process exit 확인
```

최종 PR #68 CI 결과:

```text
Desktop Release build: success
automated tests: success
Windows x64 self-contained publish: success
Startup + exact Map smoke: success
graceful Main Window close + process exit: success
ZIP creation/upload: success
```

---

# PR #69 — Map/MiniMap hotkey + MiniMap input policy — MERGED

```text
PR: #69 Fix MiniMap zoom, floor hotkeys, and resize grip
merge: 24a9bcb5c89ce30067b84427b7df7ec755aaa9de
final head: 0753febeab62d1a41921c285d7e0ed2a4df0ab94
CI: 31350388320
artifact: 9048751983
artifact digest: sha256:20521163d31bf58c8dbf25b12fe5a93f1195df6182aa7c003c40df3519e4d99b
```

상세: `docs/MAP_HOTKEY_FEEDBACK_2026-08-10_05.md`

반영 사항:

1. zoom in/out hotkey를 Main Map + active MiniMap에 동시 전달
2. floor up/down hotkey를 Main Map original selector + active MiniMap에 동시 전달
3. persisted JunhyunHelper hotkey를 실제 runtime dispatch 권위값으로 사용
4. 게임 foreground에서 전역 hotkey 사용 정책 유지
5. MiniMap mouse resize 비활성화
6. MiniMap 우측 하단 resize grip 제거

최종 CI:

```text
Desktop Release build: success
automated tests: success
Windows x64 self-contained publish: success
Startup + exact Map smoke: success
graceful Main Window close + process exit: success
ZIP creation/upload: success
```

---

# 최근 Map PR 이력

- PR #64 — Map product refinement V2 — merge `2339ddff5773ee385ff32b4ff5a173aab52d8050`
- PR #65 — screenshot Map switch / brand icon — merge `480a49ce7df5f1a17ca91d1caecbb6a81451811a`
- PR #66 — Quest UI / Korean-only publish — merge `2f9f07f64d9c6a8259504a8425c254a95673f8ea`
- PR #67 — Quest renderer / floor / marker visibility — merge `7d248d7346760d126b839d69318648e504ac39fc`
- PR #68 — settings / lifecycle / performance — merge `f75644002766f45fc0b1d0929ab556bba55a801a`
- PR #69 — MiniMap zoom / floor hotkey / resize grip — merge `24a9bcb5c89ce30067b84427b7df7ec755aaa9de`

---

# 다음 작업

1. **PR #69 Windows 사용자 검증**
2. 우선 확인:
   - Escape from Tarkov가 foreground인 상태에서 zoom in/out hotkey
   - Main Map과 MiniMap이 함께 확대/축소되는지
   - 게임 foreground에서 floor up/down hotkey
   - Main Map과 MiniMap이 동일 방향의 층으로 함께 전환되는지
   - MiniMap 우측 하단 resize grip 제거 및 mouse resize 차단
   - MiniMap size increase/decrease hotkey는 계속 정상인지
3. PR #68의 나머지 사용자 피드백 항목은 사용자 실사용에서 정상 확인됨
4. 실사용 차이가 있으면 해당 경로만 수정
5. Map 제품 동작 검증 완료 후 artwork/config/general-marker DB를 동일 revision 단위로 교체하는 atomic Map bundle updater 구현
