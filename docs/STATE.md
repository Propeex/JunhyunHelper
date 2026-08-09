# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP PRODUCT REFINEMENT V2 MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

---

# 현재 Map 제품 기준 — 2026-08-09

## Exact Tarkov Helper 기준선

PR #62에서 JunhyunHelper 자체 Map 재구현을 폐기하고 `Propeex/Tarkov-Helper`의 Map + MiniMap subsystem을 exact source 기준으로 이식했습니다.

```text
PR #62 merge: 4b3d43051b48c3d00ab8fdba03814d24066a2fd0
exact baseline Tarkov-Helper revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818
```

사용자가 Windows에서 기존 Tarkov Helper의 지도 artwork/구조가 원하는 형태임을 직접 확인했습니다.

현재 제품 전용 Map source는 별도 branch에서 관리합니다.

```text
repository: Propeex/Tarkov-Helper
branch: junhyun-map-product-v2
pinned revision: d933792b6042a51cea38dc44b686a096fe30de67
JunhyunHelper submodule: vendor/Tarkov-Helper
```

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

---

# Map subsystem 독립성

사용자 확정 원칙:

```text
Map subsystem = 독립
└─ Quest만 예외
```

Map이 JunhyunHelper에서 읽는 제품 정보는 다음뿐입니다.

- 현재 profile의 Quest 진행 상태
- online Quest location geometry

Hideout / Item / Ammo 등과 Map runtime을 결합하지 않습니다.

상세 요구사항: `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# PR #64 — Map product refinement V2 — MERGED

```text
PR #64: Refine Map Quest and MiniMap controls v2
merge commit: 2339ddff5773ee385ff32b4ff5a173aab52d8050
final PR head: ae7839e15a26d8d0a0643802ed08ab0f5b80f520
final PR CI: 31320921128
```

최종 자동 검증:

```text
Desktop Release build: success
existing automated tests: success
Windows x64 self-contained publish: success
Startup + Map smoke: success
ZIP creation/upload: success
```

`Startup + Map smoke`는 publish된 Windows EXE가 lazy Map subsystem과 V2 product adapter까지 실제로 생성한 뒤 정상 생존하는지 확인합니다.

---

# V2 구현 상태

## Quest sidebar

왼쪽에는 현재 선택 Map의 **Current(진행 중) Quest만** 표시합니다.

- 기본 접힘 상태: 34px handle만 사용
- 펼침 상태: 300px
- 접으면 지도 영역이 실제로 넓어짐
- Quest 행 클릭 → JunhyunHelper `퀘스트` 탭 → 해당 Quest 상세 선택/스크롤
- 좌표가 없는 Quest도 목록에는 표시하고 `정확한 좌표 없음` 표시

## Quest marker

- `퀘스트 마커 표시` 전역 checkbox를 지도 marker 목록에 제공
- 좌표가 있는 Quest는 sidebar에 개별 marker checkbox 제공
- 전역 OFF는 개별 선택 상태를 지우지 않음
- 개별 표시 대상 Quest를 sidebar 순서대로 `A`, `B`, `C`...로 식별
- 하나의 Quest에 위치가 여러 개면 모두 같은 문자 사용
- sidebar와 Main Map/MiniMap이 동일한 문자 사용
- old Quest marker style / color / name-size / marker-size 설정은 제거
- source에 높이가 없으면 층을 추측하지 않음

Quest content schema는 **v4**이며 online `possibleLocations` / `zones`를 Quest domain에만 저장합니다.

기존 v3 snapshot은 offline fallback으로 계속 읽고, Map 최초 사용 시 v4 online update를 1회 시도합니다. 실패하면 v3와 `user.db`를 그대로 유지합니다.

## 지도 marker 설정

marker UI를 section/card 구조로 정리했습니다.

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

실제 bundled DB 검토 결과:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 빈 ScavSpawn/Keys UI는 만들지 않고 Raider만 유지합니다.

`탈출구 이름 크기`는 marker 목록에서 제거하고 Main Map `설정` 패널로 이동했습니다.

## Screenshot tracking / floor

스크린샷은 다음 용도로만 사용합니다.

- Map 감지
- 감지된 Map으로 자동 전환
- player 위치
- 가능한 경우 heading

스크린샷 좌표로 floor를 판정하지 않습니다.

제품 callback은 원본 screenshot position handler에서 floor auto-switch 경로를 제거하고 Map 전환 + player tracking만 수행합니다.

Floor 정책:

- 사용자가 floor selector 또는 floor hotkey로 직접 선택
- 선택된 현재 층만 표시
- 다른 층 opacity 0% 고정
- `다른 층 투명도` 설정 제거
- `현재 층만 표시` 설정 제거
- auto-floor 설정/복귀 기능 제거
- auto-floor-resume hotkey 제거

Quest source 자체에 신뢰 가능한 Height가 있는 경우에만 Quest marker의 소속 floor를 분류합니다. 이는 screenshot floor 추정과 별개입니다.

## MiniMap 고정 정책

- 우측 상단 `PositionToTopRight()` anchor
- 창 drag 이동 금지
- resize 후 동일 anchor 유지
- opacity 100% 기본
- cursor hover 시 일시적으로 0%, 이탈 시 즉시 100%
- **Click-through 항상 ON**
- **ViewMode 항상 PlayerTracking**
- **다른 층 opacity 항상 0%**
- **AutoFloorSelection 항상 OFF**

이 값들은 단순 UI 숨김이 아니라 settings model setter 단계에서도 legacy 저장값을 무시하므로 이전 설정이 다시 기능을 활성화할 수 없습니다.

MiniMap 별도 설정창은 현재 조정 가능한 확대율/플레이어 marker 크기만 남기고 다음을 제거했습니다.

- 다른 층 투명도
- 현재 층만 표시 옵션
- auto floor
- Fixed / PlayerTracking 선택
- Click-through 선택
- hotkey section
- 즉시 작업

## Hotkey

단축키 편집기는 별도 MiniMap dialog에서 제거하고 Main Map `설정` 패널에 직접 배치했습니다.

설정 가능:

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

## Custom marker

제품에서 Custom Marker 기능을 제거했습니다.

- 우측 custom marker sidebar 비노출
- add context-menu 비노출
- marker container 비노출
- 편집/삭제/opacity/list UI 비노출

원본 source의 호환 타입이 남더라도 JunhyunHelper 제품 runtime/UI에서는 사용자가 접근하지 않습니다.

---

# Map 외 Core 제품 상태

유지:

- Profile: mode별 profile, level/faction/edition/prestige/trader/Fence
- Quest: 진행/잠김/사용불가/완료, prerequisite, item requirement, inventory ledger
- Hideout: 시설 level, next upgrade material, upgrade/rollback ledger
- Needed Items: FIR/일반 필요량/보유량/검색/필터
- Ammo: json.tarkov.dev raw 성능 + Wiki Ballistics 비교 + favorite

Scanner는 탭/placeholder만 있으며 실제 요구사항은 아직 확정 전입니다.

---

# 프로젝트 전체 update 원칙

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

- 일반 데이터 내용 변화는 importer 규칙으로 자동 재구축
- 외부 형식/의미 자체가 변경된 경우에만 코드 변경
- runtime AI/GPT 없음
- `user.db`와 Game Content 분리
- update 실패가 사용자 진행도/기존 정상 데이터를 손상시키지 않음

---

# 다음 작업

1. V2 Windows 테스트 빌드 사용자 검증
2. 실제 화면/사용감 차이만 보정
3. 사용자 검증 이후 exact Map artwork/config/general-marker DB를 동일 revision으로 교체하는 atomic bundle updater 구현
