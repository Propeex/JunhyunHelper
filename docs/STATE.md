# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 첫 실사용 피드백 반영**

상태: `IN PROGRESS`

현재 핵심 제품 흐름은 실제 WPF Desktop에서 연결되어 있습니다.

```text
온라인 Tarkov 데이터
→ 검증/변환
→ 모드별 Game Content DB
→ User Progress와 결합
→ Quest / Hideout / Needed Items / Ammo 계산
→ Desktop 표시
```

지도와 Scanner의 실제 기능은 아직 후속 범위입니다.

---

## 현재 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 데이터를 다시 읽어 수작업으로 넣는 프로그램이 아닙니다.

- 일반적인 게임 데이터 변경은 프로그램이 같은 importer/변환 규칙으로 다시 DB를 만들 수 있어야 함
- 같은 입력에는 같은 결과
- 외부 데이터 의미를 모르면 추측하지 않음
- Game Content와 User Progress를 분리
- Game Content 업데이트가 `user.db`를 덮어쓰지 않음
- Needed Items / cleanup / Quest 상태 같은 파생 결과를 진실의 원천으로 저장하지 않음
- 안전한 cleanup을 증명할 수 없으면 `판단 보류`
- UI는 Core/Application 규칙을 다시 구현하지 않음

---

## 기술 기준

현재 구현:

- .NET 10
- WPF Desktop
- SQLite
- Core / Infrastructure / Application / Desktop 4계층
- 별도 backend 없음
- runtime AI/GPT 없음

세부 구조는 `docs/ARCHITECTURE.md`를 따릅니다.

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

---

## 현재 데이터 원천

### 1차 원천

`json.tarkov.dev`

현재 사용하는 주요 데이터:

- tasks
- hideout
- items
- traders
- maps — Quest 참조/필터용 최소 메타데이터
- barters
- crafts
- ammo에 필요한 item 속성

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

TarkovTracker `tarkov-data-overlay`의 editions 정보만 사용합니다.

전체 community correction overlay를 자동 적용하지 않습니다.

---

## Profile — 현재 구현

- 한 GameMode당 프로필 하나
- 프로필 선택 dropdown
- dropdown 내부 `새 프로필`
- `프로필 수정`
- 삭제는 프로필 수정 흐름 안에 배치

입력:

- Player level: `- / 값 / +`, 1단위
- Prestige: `- / 값 / +`, 1단위
- 일반 Trader: LL만 기본 표시, `- / 값 / +`
- Fence: standing을 `-0.1 / 값 / +0.1`
- 정말 필요한 비-Fence standing은 고급 영역에서만 노출

Trader LL과 standing은 별개의 optional fact로 저장합니다.

프로필 설정 수정은 다음 진행 사실을 보존합니다.

- completed / failed Quest
- Hideout level
- Inventory

프로필 삭제는 해당 GameMode의 User Progress 전체 삭제이며 다운로드된 Game Content는 삭제하지 않습니다.

---

## Quest — 현재 구현

정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

`Indeterminate`는 정상 진행 상태가 아니라 판정에 필요한 사실/의미가 부족한 문제 상태입니다.

현재 판정 입력:

- player level
- faction
- edition
- prestige
- 필요한 trader standing / loyalty
- prerequisite Quest status
- disabled
- explicit permanent failure
- 다른 Quest 완료로 확정 가능한 failure

사용 방식:

- 별도 Accept 버튼 없음
- 게임에서 완료하면 `완료`
- 실수 시 `완료 취소`
- 자동으로 알 수 없는 희귀한 비재시작형 영구 실패만 `실패 처리 / 실패 취소`

Quest 보상은 제품 범위에서 제외합니다.

상세 근거:

- `docs/QUEST_FAILURE_ANALYSIS.md`
- `docs/QUEST_REWARD_DECISION.md`

---

## Hideout — 현재 구현

사용자 확정 의미:

**미입력과 Lv.0은 같은 상태입니다.**

- 별도 `미입력` 상태를 제품 의미로 유지하지 않음
- 저장 row가 없으면 Lv.0으로 계산 가능
- 화면은 `- / 현재 레벨 / +`
- 현재 레벨의 바로 다음 upgrade를 상세에 표시
- Needed Items에는 현재 레벨보다 높은 모든 미래 upgrade material을 합산

Hideout station 이미지는 canonical `ImageUrl`을 사용해 목록과 상세 header에 표시하는 작업이 PR #32에 포함되어 있습니다.

---

## Needed Items / Item — 현재 구현

핵심 목적:

> 지금 필요한 것만이 아니라 현재 캐릭터가 앞으로 사용할 가능성이 있는 아이템을 미리 모으고, 더 이상 필요하지 않은 실제 보유품은 안전하게 정리하도록 돕는다.

미래 필요량에 포함:

- Current Quest 제출 아이템
- 미래에 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 모든 미래 upgrade material

제외:

- Completed Quest
- 진영/edition/disabled 등으로 영구 불가
- 완료/실패가 확정되어 닫힌 branch
- 이미 지난 Hideout upgrade

Inventory:

- FIR / Non-FIR 직접 입력
- Game Content update와 독립된 User Progress fact
- 필요량이 0이 되어도 실제 보유량이 남아 있으면 Item 화면에서 유지

cleanup:

- 미래 필요량 충족 후 남는 안전한 초과분만 계산
- FIR 최소 요구를 우선 보호
- 보유 metadata가 새 Game Content에서 사라져도 stable Item ID로 노출
- 안전성을 증명할 수 없으면 cleanup하지 않고 보호

### Item UI — PR #32

기존 진단 dump 형태를 제거하고 비교 가능한 행으로 정리합니다.

행:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요 수량
- 보유 수량
- 추가 필요 / 충분 / 정리 / 판단 보류 상태

상세:

- FIR 요구
- FIR / Non-FIR 보유 입력
- 전체 필요 출처
- cleanup 보호 이유
- 해당 아이템이 후보인 경우에만 flexible hand-in 그룹 정보

유동 제출 후보는 아직 하나도 보유하지 않았더라도 **모든 후보를 목록에 표시**해 첫 보유량 입력 경로를 보장합니다.

아이콘은 canonical `GameItem.IconUrl`을 사용합니다.

---

## Ammo — 현재 구현

Ammo는 User Progress와 분리된 읽기 전용 비교 화면입니다.

첫 실사용 피드백 반영 완료:

- 이름 검색 제거
- 구경 dropdown 중심
- `표시 열` 메뉴에서 열 표시/숨김
- 상세 정보는 열 숨김과 관계없이 유지
- 표 정렬은 `관통력 오름차순 → 피해량 오름차순 → 이름`
- 수급처를 `N개`가 아니라 최소한의 실제 경로로 요약
  - 예: Trader LL
  - 교환
  - Hideout 제작 Lv.
- 구조화된 상인/교환/제작 경로가 없을 때만 `레이드 획득`
- 상세 수급처 카드는 유지

PR #32에서는 canonical item icon을 Ammo 표와 상세 header에도 표시합니다.

### Armor Class 1~6 effectiveness

사용자 요구는 확정되어 있습니다.

- Class 1~6
- 각 칸 0~6 숫자
- 값에 대응한 색상
- Tarkov Wiki `Bullet effectiveness against armor class`와 같은 의미

하지만 정확한 derivation/source가 아직 검증되지 않았으므로 임의 heuristic으로 구현하지 않습니다.

현재 조사 상태는 `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`를 따릅니다.

---

## 이미지 cache — PR #32

Desktop 비권위 cache:

```text
%LocalAppData%/JunhyunHelper/image-cache
```

대상:

- Item
- Hideout station
- Ammo item

원칙:

- canonical Game Content의 URL만 사용
- URL hash를 cache 파일명에 포함해 변경된 URL은 새 파일로 취급
- 동시 다운로드 제한
- 이미지 크기 제한
- 이미지 실패는 Game Content/User Progress 실패가 아님
- 잘못된 payload가 디코딩 실패하면 cache entry를 삭제하여 다음 요청에서 회복 가능

---

## 첫 실사용 피드백 1~13 진행 상태

| 번호 | 요구 | 상태 |
|---:|---|---|
| 1 | 전역 dark dropdown/scrollbar 및 부드러운 UI | 구현/병합 완료 (PR #28) |
| 2 | Hideout/Item/Ammo 이미지 + 온라인 URL 기반 cache | PR #32 검증 중 |
| 3 | Quest/Hideout 리스트 행 정렬/형태 개선 | 구현/병합 완료 (PR #28) |
| 4 | level/trader 수치 +/- 입력, Fence 0.1 | 구현/병합 완료 (PR #30) |
| 5 | Hideout 미입력 = Lv.0 | 구현/병합 완료 (PR #29) |
| 6 | Item 목록을 실제 판단용 목록으로 재설계 | PR #32 검증 중 |
| 7 | Ammo 표에 간단한 수급 경로 표시 | 구현/병합 완료 (PR #31) |
| 8 | 데이터 업데이트 진행률 시각화 | 미구현 |
| 9 | Profile 버튼 공간 절약 | 구현/병합 완료 (PR #30) |
| 10 | Ammo 검색 제거/열 선택/관통 오름차순 | 구현/병합 완료 (PR #31) |
| 10-b | Wiki-equivalent Class 1~6 0~6 효율 cell | 공식/source 검증 중, 미구현 |
| 11 | Trader/Map dropdown을 실제 게임 순서로 고정 | 미검증/미구현 |
| 12 | Ground Zero 21+를 Ground Zero로 병합 | 구현 여부 재검증 필요 |
| 13 | Map/Scanner 탭 placeholder 추가 | 미구현 |

---

## 현재 작업 PR

### PR #32 — `agent/item-icons-redesign`

목적:

- Item 목록 재설계
- Item/Hideout/Ammo canonical 이미지 표시
- LocalAppData image cache

자동 리뷰에서 확인된 보완 사항:

1. 미보유 flexible 후보도 Item 행에 포함
2. invalid image cache payload 자동 제거/재시도 가능
3. 현재 UI/cache 구조를 공식 문서에 기록

위 보완을 반영하고 CI/리뷰를 다시 통과시킨 뒤 병합합니다.

---

## 다음 작업 순서

PR #32 종료 후 다음 순서로 첫 실사용 피드백을 계속 처리합니다.

1. 데이터 업데이트 진행률 UI (#8)
2. Trader / Map 실제 게임 순서 고정 (#11)
3. Ground Zero 21+ alias 정규화 검증 및 필요 시 수정 (#12)
4. Map / Scanner placeholder 탭 (#13)
5. Armor Class 1~6 exact rating 조사 계속 (#10-b)

각 단계에서 기존 Core/데이터 의미를 임의 변경하지 않고 테스트와 공식 문서를 함께 갱신합니다.
