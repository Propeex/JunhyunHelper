# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 첫 실사용 피드백 반영**

상태: `IN PROGRESS`

첫 실사용 피드백 1~13의 요구는 모두 구현되었거나, 실제 기능이 아직 범위 밖인 Map/Scanner의 경우 요청된 placeholder까지 구현되어 있습니다.

현재 핵심 제품 흐름:

```text
온라인 Tarkov 데이터
→ 검증/변환
→ 모드별 Game Content DB
→ User Progress와 결합
→ Quest / Hideout / Needed Items / Ammo 계산
→ Desktop 표시
```

Map과 Scanner의 실제 기능은 후속 범위입니다.

---

## 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 데이터를 다시 읽어 수작업으로 넣는 프로그램이 아닙니다.

- 일반적인 게임 데이터 변경은 같은 importer/변환 규칙으로 다시 DB를 만들 수 있어야 함
- 같은 입력에는 같은 결과
- 외부 데이터 의미를 모르면 추측하지 않음
- Game Content와 User Progress를 분리
- Game Content 업데이트가 `user.db`를 덮어쓰지 않음
- Needed Items / cleanup / Quest 상태 같은 파생 결과를 진실의 원천으로 저장하지 않음
- 안전한 cleanup을 증명할 수 없으면 `판단 보류`
- UI는 Core/Application 규칙을 다시 구현하지 않음

---

## 기술 기준

- .NET 10
- WPF Desktop
- SQLite
- Core / Infrastructure / Application / Desktop 4계층
- 별도 backend 없음
- runtime AI/GPT 없음

기본 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

주요 저장:

```text
user.db
content/<game-mode>/content.db
content/<game-mode>/content.candidate.db
content/<game-mode>/content.previous.db
image-cache/
```

세부 구조는 `docs/ARCHITECTURE.md`를 따릅니다.

---

## 데이터 원천

### 1차 원천

`json.tarkov.dev`

- tasks
- hideout
- items
- traders
- maps — Quest 참조/필터용 최소 메타데이터
- barters
- crafts
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

- TarkovTracker `tarkov-data-overlay`: **editions 정보만**
- Escape from Tarkov Wiki `Ballistics`: **Ammo Armor Class 1~6 0~6 effectiveness 값만** optional enrichment

전체 community correction overlay는 자동 적용하지 않습니다.

Wiki Ballistics는 raw Ammo fact의 대체 원천이 아닙니다. Wiki source를 읽지 못하거나 안전하게 매칭하지 못하면 기본 Game Content 업데이트는 계속하고 해당 effectiveness만 unknown으로 둡니다.

---

## Game Content update

업데이트 안전성:

1. 기존 active data 유지
2. 온라인 원천 다운로드
3. canonical model 변환
4. 관계/필수 값 검증
5. candidate SQLite 작성
6. candidate read-back/activation 검증
7. 성공한 경우에만 active 교체
8. 실패 시 기존 active와 `user.db` 유지

### 진행률 UI

타이머로 만든 가짜 퍼센트를 사용하지 않습니다.

현재 진행률 source:

- 8개 primary source: items / traders / maps / tasks / hideout / barters / crafts / edition rules
- 1개 optional enrichment source: Wiki Ballistics effectiveness
- canonical import
- validation
- candidate write
- activation
- complete / failed

각 source task가 실제 완료될 때 완료 개수가 증가합니다.

수동 업데이트, 해당 모드 최초 데이터 생성, active 복구 업데이트 모두 같은 progress UI를 사용합니다.

---

## Profile

- 한 GameMode당 프로필 하나
- 상단 Profile dropdown
- dropdown 내부 `새 프로필`
- `프로필 수정`
- 삭제는 프로필 수정 흐름 안에 배치

입력:

- Player level: `- / 값 / +`, 1단위
- Prestige: `- / 값 / +`, 1단위
- 일반 Trader: LL만 기본 표시, `- / 값 / +`
- Fence: standing `-0.1 / 값 / +0.1`
- 정말 필요한 비-Fence standing만 고급 입력

Trader LL과 standing은 별개의 optional fact입니다.

프로필 설정 수정은 Quest 진행, Hideout level, Inventory를 보존합니다.

---

## Quest

정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

`Indeterminate`는 정상 상태가 아니라 판정에 필요한 사실/의미가 부족한 문제 상태입니다.

사용 방식:

- 별도 Accept 버튼 없음
- 게임에서 완료하면 `완료`
- 실수 시 `완료 취소`
- 자동 판정할 수 없는 희귀 비재시작형 영구 실패만 `실패 처리 / 실패 취소`

Quest 보상은 제품 범위에서 제외합니다.

### filter order

Trader:

```text
Prapor → Therapist → Fence → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref → Lightkeeper → BTR Driver
```

Map도 검증된 게임식 고정 표시 순서를 `UiReferenceOrder` 한 곳에서 관리합니다. 미래 unknown 값은 숨기지 않고 알려진 값 뒤에 표시합니다.

### Ground Zero 21+

canonical map ID는 보존합니다.

Quest map filter에서만 `Ground Zero`와 `Ground Zero 21+`를 하나의 Ground Zero 그룹으로 표시합니다.

---

## Hideout

**미입력과 Lv.0은 같은 제품 상태입니다.**

- 저장 row가 없으면 Lv.0
- 화면은 `- / 현재 레벨 / +`
- 상세는 바로 다음 upgrade 표시
- Needed Items는 현재 레벨보다 높은 모든 미래 upgrade material 합산
- canonical `ImageUrl`을 목록과 상세 header에 표시

---

## Needed Items / Item

목적:

> 지금 당장 필요한 것뿐 아니라 현재 캐릭터가 앞으로 사용할 가능성이 있는 아이템을 미리 모으고, 더 이상 필요하지 않은 실제 보유품은 안전하게 정리하도록 돕는다.

포함:

- Current Quest 제출 아이템
- 미래에 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 모든 미래 upgrade material

제외:

- Completed Quest
- 현재 캐릭터에서 영구 불가임이 증명된 Quest
- 닫힌 branch
- 이미 지난 Hideout upgrade

Inventory:

- FIR / Non-FIR 직접 입력
- Game Content와 독립된 User Progress fact
- 실제 보유량이 남아 있으면 필요량 0이어도 Item 화면에 유지

cleanup:

- 미래 필요량 충족 후 남는 안전한 초과분만 계산
- FIR 최소 요구 우선 보호
- metadata가 사라져도 stable Item ID 보유 기록 유지
- 안전성을 증명하지 못하면 `판단 보류`

Item UI:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요 수량
- 보유 수량
- 추가 필요 / 충분 / 정리 / 판단 보류

유동 제출 후보는 아직 하나도 보유하지 않았더라도 모든 후보를 목록에 표시합니다.

---

## Ammo

Ammo는 User Progress와 분리된 read-only 비교 화면입니다.

구현:

- 이름 검색 없음
- 구경 dropdown 중심
- `표시 열` 메뉴로 열 표시/숨김
- 상세는 열 숨김과 관계없이 전체 정보 유지
- `관통력 오름차순 → 피해량 오름차순 → 이름`
- 표에는 실제 최소 수급 경로 요약
- 상인/교환/제작 경로가 없을 때만 `레이드 획득`
- 상세 수급처 카드 유지
- canonical item icon 표시

### Armor Class 1~6 effectiveness — PR #35

사용자가 지정한 Tarkov Wiki `Bullet effectiveness against armor class`와 같은 0~6 값을 사용합니다.

구현 원칙:

- `json.tarkov.dev` raw Ammo stats는 그대로 1차 원천
- Wiki Ballistics의 Class 1~6 **명시 값**만 optional enrichment
- penetration/class ratio나 자체 threshold로 숫자를 생성하지 않음
- canonical 영문 Ammo 이름에 유일하게 매칭되는 row만 적용
- 충돌/모호함/미매칭은 unknown
- 전체 매칭 수가 비정상적으로 적으면 schema 변화로 보고 해당 업데이트의 effectiveness를 전부 적용하지 않음
- Wiki 요청은 20초로 제한하여 optional source가 core update를 장시간 막지 않음

UI:

- 표에 Class 1~6 여섯 칸
- 각 칸 0~6 숫자 + dark-theme 색상
- 숫자를 항상 표시하여 색상만으로 의미를 전달하지 않음
- unknown은 `?`
- `표시 열`에서 effectiveness 열을 숨겨도 상세에는 계속 표시

대표 parser/matching 회귀값:

- `.50 AE JHP` → `6,1,0,0,0,0`
- `.50 AE Copper Solid` → `6,6,6,5,3,2`
- `.300 Blackout Whisper` → `6,4,2,1,0,0`
- `.366 TKM AP-M` → `6,6,6,6,5,4`
- `12/70 Flechette` → `6,6,6,5,5,5`

상세 근거: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

---

## 이미지 cache

Desktop 비권위 cache:

```text
%LocalAppData%/JunhyunHelper/image-cache
```

대상:

- Item
- Hideout station
- Ammo item

원칙:

- canonical Game Content URL 사용
- URL hash를 cache key에 포함
- 동시 다운로드/크기 제한
- 이미지 실패는 Game Content/User Progress 실패가 아님
- invalid payload는 삭제하여 다음 요청에서 회복 가능

---

## Map / Scanner

상단 내비게이션:

- 지도
- 스캐너

현재 실제 기능은 미구현이며 `준비 중` placeholder만 표시합니다.

검증되지 않은 지도 데이터나 스캐너 로직은 실행하지 않습니다.

---

## 첫 실사용 피드백 1~13 진행 상태

| 번호 | 요구 | 상태 |
|---:|---|---|
| 1 | 전역 dark dropdown/scrollbar 및 부드러운 UI | 구현/병합 완료 (PR #28) |
| 2 | Hideout/Item/Ammo 이미지 + 온라인 URL 기반 cache | 구현/병합 완료 (PR #32) |
| 3 | Quest/Hideout 리스트 행 정렬/형태 개선 | 구현/병합 완료 (PR #28) |
| 4 | level/trader 수치 +/- 입력, Fence 0.1 | 구현/병합 완료 (PR #30) |
| 5 | Hideout 미입력 = Lv.0 | 구현/병합 완료 (PR #29) |
| 6 | Item 목록을 실제 판단용 목록으로 재설계 | 구현/병합 완료 (PR #32) |
| 7 | Ammo 표에 간단한 수급 경로 표시 | 구현/병합 완료 (PR #31) |
| 8 | 데이터 업데이트 진행률 시각화 | 구현/병합 완료 (PR #34) |
| 9 | Profile 버튼 공간 절약 | 구현/병합 완료 (PR #30) |
| 10 | Ammo 검색 제거/열 선택/관통 오름차순 | 구현/병합 완료 (PR #31) |
| 10-b | Wiki-equivalent Class 1~6 0~6 효율 cell | 구현 완료 (PR #35) |
| 11 | Trader/Map dropdown 실제 게임 순서 | 구현/병합 완료 (PR #34) |
| 12 | Ground Zero 21+ → Ground Zero filter grouping | 구현/병합 완료 (PR #34) |
| 13 | Map/Scanner 탭 placeholder | 구현/병합 완료 (PR #34) |

---

## 현재 작업

### PR #35 — `agent/ammo-armor-effectiveness`

- Wiki Ballistics optional source adapter
- 안전한 Ammo 이름 매칭
- snapshot persistence
- Class 1~6 colored cells
- unknown/failure semantics
- parser/enrichment/persistence tests

코드 구현과 CI가 통과했습니다. 공식 문서/최종 리뷰 확인 후 병합합니다.

---

## 다음 작업

PR #35 종료 후 첫 실사용 피드백 통합 Windows build를 사용자 실사용 대상으로 확인합니다.

그 다음 우선순위는 사용자의 새 피드백이며, 별도 요구가 없다면:

1. Map 실제 기능의 데이터 공급원/사용 경험 정의
2. Scanner 실제 기능의 요구사항 정의
3. 추가 실사용에서 발견되는 UI/데이터 회귀 수정

중요한 새 제품 결정을 확정하는 즉시 공식 문서에 반영합니다.
