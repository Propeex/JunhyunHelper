# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `EXACT TARKOV-HELPER MAP BASELINE CONFIRMED / MAP PRODUCT REFINEMENT IMPLEMENTED / WINDOWS USER VALIDATION NEXT`

---

# 현재 Map 제품 기준 — 2026-08-09

## 1. exact transplant 기준선

PR #62에서 JunhyunHelper 자체 Map 구현을 제거하고 기존 `Propeex/Tarkov-Helper`의 Map + MiniMap subsystem을 원본 소스 기준으로 이식했습니다.

```text
PR #62
merge: 4b3d43051b48c3d00ab8fdba03814d24066a2fd0
baseline Tarkov-Helper revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818
```

사용자가 Windows 테스트에서 **기존 Tarkov Helper의 지도 화면이 원하는 형태로 정상 표시되는 것을 직접 확인**했습니다.

따라서 앞으로 지도 artwork / coordinate transform / screenshot tracking / floor / MiniMap의 기반은 이 exact subsystem입니다. 이전 RE3MR / Wiki / Shebuka presentation 실험은 제품 기준이 아닙니다.

상세:

- `docs/MAP_TRANSPLANT_RESET.md`

---

## 2. Map subsystem 독립성

사용자 확정 철학:

```text
Map subsystem = 독립
Quest만 예외
```

허용된 외부 결합:

- 현재 JunhyunHelper profile의 Quest 진행 상태
- Quest online location geometry

Quest 이외에는 JunhyunHelper의 Hideout / Item / Ammo / 기타 화면과 Map runtime을 결합하지 않습니다.

상세:

- `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# PR #63 — Map product refinement

작업 브랜치:

```text
agent/map-product-refinement-v1
```

old Map 전용 source branch:

```text
Propeex/Tarkov-Helper:junhyun-map-product-v1
pinned revision: 23230102b40377a9b33e9c72f29b85941ad4098d
```

기존 `Propeex/Tarkov-Helper` main은 수정하지 않습니다.

제품 코드 검증 checkpoint:

```text
validated code head: 9b99733b4215659e91b3319b8ca4b6d2ae547a27
CI: 31313163552
Desktop Release build: success
existing automated tests: success
Windows x64 self-contained publish: success
Startup + Map smoke: success
ZIP creation/upload: success
```

Startup + Map smoke는 단순 프로세스 생존 확인이 아니라 publish된 실제 Windows EXE에서 lazy Map subsystem과 product adapter까지 생성한 뒤 12초 이상 정상 생존하는지 검증합니다.

---

# Map product refinement 구현 내용

## Main Map UI

제거:

- 전체화면 기능/버튼
- 상단 탈출구 체크박스
- 상단 고정 뷰 체크박스
- MiniMap 옆 `?` 도움말

`SetFullScreenMode(bool)` compatibility contract는 exact source compile을 위해 존재하지만 JunhyunHelper에서는 no-op입니다.

## Quest sidebar / markers

왼쪽 JunhyunHelper sidebar는 **현재 선택 Map의 진행 중(Current) Quest만** 표시합니다.

- 완료 / 잠김 / 미래 Quest 제외
- online `possibleLocations` / `zones` 위치 사용
- 여러 위치면 모두 marker
- 정확한 위치가 없으면 sidebar에 `정확한 좌표 없음`
- 위치를 추측하지 않음
- 외부 데이터에 Height가 없으면 Y=0으로 만들지 않고 층 미확정으로 보존
- exact Tarkov Helper coordinate transform을 그대로 사용
- Main Map / MiniMap이 같은 Quest marker factory와 크기/이름 크기 설정 사용
- 상단 `퀘스트 마커` 체크박스가 Main/MiniMap 동시 제어

옛 Tarkov Helper Quest DB 전용 drawer와 의미 없는 옛 Quest marker style/color 설정은 비활성화했습니다.

## Quest content update

Content schema:

```text
v4
```

v4 추가 범위는 **Quest 위치 geometry뿐**입니다.

기존 v3는 offline fallback으로 계속 읽을 수 있습니다.

```text
v3 active 있음
→ 앱 정상 시작 가능
→ Map 최초 사용 시 v4 online update 1회 자동 시도
→ 성공: v4 active
→ 실패: v3 유지, user.db 유지, 앱 계속 사용
```

## marker settings 통합

`탈출구 설정`을 별도 영역으로 두지 않고 `지도 마커`에 통합했습니다.

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

### 추가 marker 데이터 검토

exact bundled `MapMarkers` DB 실제 값:

```text
ScavSpawn: 0
Keys: 0
RaiderSpawn: 2
```

따라서 데이터가 없는 ScavSpawn/Keys UI는 만들지 않았고, Reserve에 실제 2개 위치가 있는 Raider만 추가했습니다.

## marker visual synchronization

Main Map ↔ MiniMap 동기화:

- visible category state
- icon
- screen marker size
- Quest marker / text size
- extract icon / color / name size
- Raider visual
- floor filtering
- player marker size

extract MiniMap icon은 Main Map 원본 emergency-exit path geometry를 재사용합니다.

## MiniMap position / opacity

- exact 원본 `PositionToTopRight()` 위치에 고정
- window drag 이동 금지
- resize / size hotkey 후 top-right 재정렬
- 전체 opacity 100% 고정
- cursor가 MiniMap 영역 위에 있으면 일시적으로 0% 투명
- cursor가 빠지면 즉시 100% 복귀
- per-monitor DPI 좌표 변환 적용
- 기존 Click-through는 hover transparency와 별도 기능으로 유지

## MiniMap hotkey

설정 가능한 동작:

- MiniMap ON/OFF
- Map zoom in/out
- floor up/down
- MiniMap size increase/decrease
- resume automatic floor tracking

규칙:

- 동일 key 한 동작만 허용
- 새 배정이 기존 배정을 해제
- Delete/Backspace 미지정
- Esc 취소
- NumPad 0~5 직접 층 선택 예약

기존 안정화된 zoom/floor 처리에는 old global hook을 유지하고, 원본에 없던 Toggle / Size +/-만 JunhyunHelper supplemental hook에서 처리합니다.

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

Map 외 제품 기능은 유지합니다.

## Profile

- 한 GameMode당 profile 하나
- 새 프로필 / 수정 / 삭제
- level / faction / edition / prestige / trader 상태
- Fence reputation 진행값

## Quest

- 진행 중 / 잠김 / 사용 불가 / 완료
- prerequisite / item requirement
- 제출 / 취소 inventory ledger
- Map에는 현재 진행 중 Quest만 투영

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

Quest 좌표는 이 원칙으로 v4 online content에 포함되었습니다.

---

# Map artwork / general marker update — 다음 시스템 작업

현재 exact Map artwork/config/general marker DB는 검증된 pinned bundle을 사용합니다.

다음 Map 인프라 작업은 **Map subsystem 내부의 atomic bundle updater**입니다.

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

이 updater는 현재 PR #63의 product UI/UX 사용자 검증 이후 진행합니다.

---

# Scanner

탭과 placeholder만 있습니다. 실제 Scanner 요구사항은 아직 확정 전입니다.

---

## 현재 다음 작업

1. PR #63 정리/병합
2. Windows 테스트 빌드 사용자 검증
3. 확인 항목:
   - 현재 Map의 진행 중 Quest sidebar
   - Quest marker 위치/층
   - 전체화면/상단 탈출구/고정 뷰/? 제거
   - marker settings + Raider + extracts 통합
   - Main/MiniMap marker 표현 동기화
   - MiniMap top-right 고정 / resize anchor
   - hover transparency + 기존 click-through
   - configurable hotkey
   - player marker size sync
4. UI/실사용 차이만 보정
5. exact Map bundle atomic updater 구현
