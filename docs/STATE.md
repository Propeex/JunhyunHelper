# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP PRODUCT REFINEMENT MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

---

# 현재 Map 제품 기준 — 2026-08-09

## Exact Tarkov Helper 기준선

PR #62에서 JunhyunHelper 자체 Map 구현을 제거하고 `Propeex/Tarkov-Helper`의 Map + MiniMap subsystem을 원본 소스 기준으로 이식했습니다.

```text
PR #62 merge: 4b3d43051b48c3d00ab8fdba03814d24066a2fd0
exact baseline Tarkov-Helper revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818
```

사용자가 Windows에서 기존 Tarkov Helper의 지도 화면이 원하는 형태로 정상 표시되는 것을 직접 확인했습니다.

따라서 지도 artwork / coordinate transform / screenshot tracking / floor / MiniMap의 기준은 이 subsystem입니다. 이전 RE3MR / Wiki / Shebuka presentation 실험은 제품 기준이 아닙니다.

상세: `docs/MAP_TRANSPLANT_RESET.md`

---

## Map subsystem 독립성

사용자 확정 철학:

```text
Map subsystem = 독립
Quest만 예외
```

Map이 JunhyunHelper에서 직접 읽을 수 있는 외부 제품 정보는 다음뿐입니다.

- 현재 profile의 Quest 진행 상태
- Quest online location geometry

Quest 이외의 Hideout / Item / Ammo / 기타 화면과 Map runtime을 결합하지 않습니다.

상세: `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# PR #63 — Map product refinement — MERGED

```text
PR #63: Refine transplanted Map product behavior
merge commit: 4606b693c229f7cc2dbc1e09cd4ef423774003bc
final PR head: b7421c6d805e2594f3337c145a33594f9bd2f902
final PR CI: 31313312720
```

old Map 전용 제품 source:

```text
repository: Propeex/Tarkov-Helper
branch: junhyun-map-product-v1
pinned revision: 23230102b40377a9b33e9c72f29b85941ad4098d
JunhyunHelper submodule: vendor/Tarkov-Helper
```

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다. Exact baseline과 JunhyunHelper 전용 변경 diff를 분리해서 관리합니다.

최종 자동 검증:

```text
Desktop Release build: success
existing automated tests: success
Windows x64 self-contained publish: success
Startup + Map smoke: success
ZIP creation/upload: success
```

Startup + Map smoke는 publish된 실제 Windows EXE에서 lazy Map subsystem과 product adapter까지 생성한 뒤 12초 이상 정상 생존하는지 확인합니다.

---

# 현재 Map 기능

## Main Map UI

제거:

- 전체화면 기능/버튼
- 상단 탈출구 checkbox
- 상단 고정 뷰 checkbox
- MiniMap 옆 `?` 도움말

`SetFullScreenMode(bool)` compatibility contract는 exact source compile용으로만 남아 있으며 JunhyunHelper에서는 no-op입니다.

## 현재 맵의 진행 중 Quest sidebar

왼쪽 sidebar는 **현재 선택 Map + 현재 profile의 Current(진행 중) Quest만** 표시합니다.

- 완료 / 잠김 / 미래 Quest 제외
- online `possibleLocations` / `zones` 사용
- 여러 유효 위치면 모두 marker
- 정확한 위치가 없으면 `정확한 좌표 없음`
- 위치를 추측하지 않음
- 외부 데이터에 Height가 없으면 Y=0으로 만들지 않고 floor unknown 유지
- exact Tarkov Helper coordinate transform 사용
- Main Map / MiniMap 공통 Quest marker factory 사용
- 상단 `퀘스트 마커` checkbox가 Main/MiniMap 동시 제어
- marker 크기 / Quest 이름 크기 설정은 양쪽에 즉시 반영

옛 Tarkov Helper Quest DB 전용 drawer 및 현재 Quest projection에 의미 없는 옛 marker style/color 설정은 비활성화했습니다.

## Quest data update

Content schema는 **v4**입니다.

v4에서 추가된 것은 Quest 위치 geometry뿐입니다.

```text
v3 active 있음
→ offline에서도 정상 읽기
→ Map 최초 사용 시 v4 online update 1회 자동 시도
→ 성공: v4 active
→ 실패: v3 + user.db 그대로 유지하고 정상 실행
```

사용자에게 cache 삭제나 수동 변환을 요구하지 않습니다.

## 지도 marker settings

별도 `탈출구 설정`을 없애고 `지도 마커`에 통합했습니다.

현재 설정 대상:

- PMC spawn
- Sniper Scav
- Rogue
- Cultist
- Lever
- Boss
- Raider
- PMC extract
- Scav extract
- Transit
- extract name size

### 추가 marker 검토

Exact bundled `MapMarkers` DB 실제 값:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

빈 ScavSpawn / Keys UI는 만들지 않았고 Reserve에 실제 2개 위치가 있는 Raider만 추가했습니다.

## Main Map ↔ MiniMap marker synchronization

동기화 대상:

- category visibility
- icon
- 화면상 marker size
- Quest marker / text size
- extract icon / color / name size
- Raider visual
- floor filter
- player marker size

MiniMap extract icon은 Main Map 원본 emergency-exit path geometry를 그대로 재사용합니다.

## MiniMap position / opacity / interaction

- 원본 `PositionToTopRight()`의 우측 상단 위치에 고정
- window mouse drag 이동 금지
- resize / size hotkey 후 같은 top-right anchor로 즉시 재정렬
- 전체 opacity 100% 고정
- cursor가 MiniMap 영역 위에 있으면 일시적으로 0%
- cursor가 빠지면 즉시 100%
- per-monitor DPI 좌표 변환 적용
- 기존 Click-through는 hover transparency와 별도 기능으로 유지
- MiniMap 내부 map pan/zoom은 유지

## configurable hotkeys

설정 가능:

- MiniMap ON/OFF
- Map zoom in/out
- floor up/down
- MiniMap size increase/decrease
- resume automatic floor tracking

규칙:

- 동일 key는 한 동작에만 지정
- 새 배정이 기존 배정을 해제
- Delete / Backspace = 미지정
- Esc = 취소
- NumPad 0~5 = 직접 층 선택 예약

기존 안정화된 zoom/floor는 old global hook을 유지하고, 원본에 없던 MiniMap Toggle / Size +/-만 JunhyunHelper supplemental hook이 처리합니다.

## player marker size

Main Map / MiniMap 공통 범위:

```text
9 ~ 54 px
= MiniMap 0.5x ~ 3.0x
legacy base 18 px
```

두 설정 화면은 양방향 동기화합니다.

---

# 기존 Core 제품 상태

Map 외 Core 기능은 유지합니다.

## Profile

- 한 GameMode당 profile 하나
- 새 프로필 / 수정 / 삭제
- level / faction / edition / prestige / trader 상태
- Fence reputation 진행값

## Quest

- 진행 중 / 잠김 / 사용 불가 / 완료
- prerequisite / item requirement
- 제출 / 취소 inventory ledger
- Map에는 Current Quest만 투영

## Hideout

- 시설 레벨 추적
- next-upgrade material
- upgrade / rollback inventory ledger

## Needed Items / Item

- FIR / 일반 필요량과 보유량
- 검색 / 종류 / 용도 / 상태 필터

## Ammo

- json.tarkov.dev raw 성능
- Wiki Ballistics 비교 정보
- favorite shortcut popup

---

# 프로젝트 전체 업데이트 원칙

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

- 일반적인 데이터 내용 변화는 같은 importer / 변환 규칙으로 자동 처리
- 외부 형식/의미 자체가 변경된 경우에만 코드 수정
- runtime AI/GPT 없음
- `user.db`와 Game Content 분리
- 업데이트 실패가 사용자 진행도/기존 정상 데이터 손상시키지 않음

Quest 좌표는 이 원칙으로 online v4 content에 포함됩니다.

---

# Map artwork / general marker update — 다음 인프라 작업

현재 exact Map artwork/config/general marker DB는 검증된 pinned bundle을 사용합니다.

다음 Map 시스템 작업은 **Map subsystem 내부 atomic bundle updater**입니다.

목표:

```text
동일 upstream Tarkov Helper revision
→ map_configs.json
→ SVG map / marker assets
→ map DB
→ bundle 전체 검증
→ 모두 정상일 때만 active 교체
→ 실패 시 마지막 정상 bundle 유지
```

지도 이미지/config/DB를 서로 다른 revision에서 따로 갱신하지 않습니다.

PR #63 Windows 사용자 검증 후 이 updater를 구현합니다.

---

# Scanner

탭과 placeholder만 있습니다. 실제 Scanner 요구사항은 아직 확정 전입니다.

---

## 현재 다음 작업

1. PR #63 Windows 테스트 빌드 사용자 검증
2. 확인:
   - 진행 중 Quest sidebar / Quest marker 위치와 floor
   - 제거된 UI
   - marker settings / Raider / extracts 통합
   - Main/MiniMap marker 표현 동기화
   - MiniMap top-right 고정 / resize anchor
   - hover transparency + 기존 Click-through
   - configurable hotkey
   - player marker size sync
3. 실제 화면/사용감 차이만 보정
4. exact Map atomic bundle updater 구현
