# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항 문서입니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자와 확정한 의도는 과거 구현보다 우선합니다.

## 1. 제품 이름

`CONFIRMED`

**준현 헬퍼**

저장소 이름은 `JunhyunHelper`입니다.

## 2. 제품 핵심 정의

`CONFIRMED`

준현 헬퍼는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 **프로그램 스스로 해석 가능한 canonical Game Content와 로컬 DB로 변환·재구축**하고, 이를 사용자 진행 상태와 결합해 플레이에 필요한 정보를 제공하는 Windows 데스크톱 헬퍼입니다.

핵심 원칙:

> 게임 데이터의 내용이 바뀌어도 외부 형식이 기존 importer가 이해할 수 있는 범위라면 프로그램이 최신 데이터를 다시 내려받아 같은 변환 규칙으로 DB를 다시 만들 수 있어야 합니다.

일반적인 데이터 업데이트에 GPT가 개입하지 않습니다.

## 3. 데이터 갱신 원칙

`CONFIRMED`

```text
온라인 데이터
→ 다운로드
→ 검증
→ canonical model 변환
→ candidate DB
→ 검증
→ active content 교체
→ User Progress와 결합
→ 파생 결과 계산
```

- 내용 변화는 기존 importer가 이해하면 자동 흡수
- 핵심 필드 삭제/타입·의미 변경 등 안전하게 이해할 수 없는 변화는 update 실패
- 검증되지 않은 candidate로 마지막 정상 active content를 덮어쓰지 않음
- Game Content update가 User Progress를 삭제/덮어쓰지 않음

User Progress 사실:

- Profile 설정
- Quest 완료/명시적 영구 실패
- Hideout level
- Trader 진행 사실
- FIR / Non-FIR inventory

파생 결과인 Quest 상태, Needed Items, cleanup 등은 별도 사실처럼 저장하지 않습니다.

## 4. 데이터 원천

### 1차 원천

`CONFIRMED`

`json.tarkov.dev`

현재 사용 영역:

- Quest
- Hideout
- Item
- Trader
- Map 최소 메타데이터
- Barter
- Craft
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

`CONFIRMED`

- TarkovTracker `tarkov-data-overlay`: editions 정보만
- Escape from Tarkov Wiki `Ballistics`: Ammo Armor Class 1~6 0~6 effectiveness 값만 optional enrichment

전체 community correction overlay를 자동 적용하지 않습니다.

Wiki Ballistics는 raw Ammo 사실의 대체 원천이 아닙니다. 실패하면 기본 Game Content update는 계속하며 해당 effectiveness 값만 unknown으로 둡니다.

### 지도 실제 기능 데이터

`OPEN`

Map 실제 기능의 최종 데이터 공급원은 아직 확정하지 않았습니다.

현재 Map 탭은 placeholder만 제공합니다.

## 5. CORE-001 — Game Content 업데이트

`CONFIRMED / IMPLEMENTED`

- 선택 GameMode의 최신 온라인 데이터 다운로드
- canonical model 변환
- candidate DB 생성
- 스키마/관계/read-back 검증
- 성공한 경우에만 active content 교체
- 실패 시 마지막 정상 Game Content와 User Progress 유지

### 진행 표시

`CONFIRMED / IMPLEMENTED`

- progress overlay
- progress bar
- 현재 단계/작업 설명
- 퍼센트
- 완료/실패 상태

가짜 timer progress를 사용하지 않습니다.

실제 source 완료 수와 다음 pipeline 단계에서 계산합니다.

## 6. CORE-002 — Profile / 진행 입력

`CONFIRMED / IMPLEMENTED`

한 GameMode당 프로필 하나를 기본으로 합니다.

상단:

- Profile dropdown
- `프로필 수정`
- `데이터 업데이트`

Profile dropdown 내부:

- 기존 프로필
- `새 프로필`

삭제는 `프로필 수정` 흐름 안에 둡니다.

입력:

- Player level: `- / 값 / +`, 정수 1단위
- Prestige: `- / 값 / +`, 정수 1단위
- 일반 Trader: LL만 기본 표시, 정수 1단위
- Fence: standing 0.1단위
- 비-Fence standing은 실제 Quest 판정에 필요한 경우에만 고급 입력

LL과 standing은 별개의 optional fact입니다.

## 7. CORE-003 — Quest

`CONFIRMED / IMPLEMENTED`

준현 헬퍼에서는 실제 게임에서 수주 가능한 Quest를 이미 수락한 것으로 봅니다.

별도 Accept 버튼을 두지 않습니다.

정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

`Indeterminate`는 정상 상태가 아니라 판정에 필요한 정보가 부족한 문제 상태입니다.

사용자 조작:

- 완료
- 완료 취소
- 정말 필요한 희귀 비재시작형 영구 실패만 실패 처리/취소

Quest reward 전체 모델은 핵심 범위에서 제외합니다.

### 목록 UI

- 행 전체 폭 정렬
- 이름 길이와 관계없이 반복 정보의 위치를 정렬
- dark theme interaction style 사용

### Trader / Map filter 순서

`CONFIRMED / IMPLEMENTED`

알파벳순이 아니라 게임에서 익숙한 고정 순서를 사용합니다.

Trader:

```text
Prapor → Therapist → Fence → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref → Lightkeeper → BTR Driver
```

미래 unknown 값은 숨기지 않고 알려진 항목 뒤에 둡니다.

### Ground Zero 21+

`CONFIRMED / IMPLEMENTED`

- canonical map ID 유지
- Quest의 실제 MapId 유지
- Quest Map filter에서만 Ground Zero와 Ground Zero 21+를 하나의 `Ground Zero` 항목으로 그룹화

## 8. CORE-004 — Hideout

`CONFIRMED / IMPLEMENTED`

- 시설별 현재 레벨을 `- / 값 / +`로 입력
- **미입력과 Lv.0은 같은 상태**
- 저장 row가 없으면 Lv.0
- 상세는 바로 다음 upgrade 표시
- Needed Items는 현재 레벨 이후 모든 미래 upgrade material 포함
- canonical `ImageUrl`을 list/detail에 표시

## 9. CORE-005 — Needed Items / Item

`CONFIRMED / IMPLEMENTED`

핵심 목적:

> 지금 필요한 것뿐 아니라 현재 캐릭터가 앞으로 사용할 가능성이 있는 아이템을 미리 모으고, 더 이상 필요하지 않은 실제 보유품은 안전하게 정리하도록 돕습니다.

미래 필요량 포함:

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
- User Progress 독립 사실
- content update로 삭제하지 않음

cleanup:

- 미래 필요량 충족 후 남는 안전한 초과분만 표시
- FIR 최소 요구 우선 보호
- metadata가 없어져도 stable Item ID 보유 기록 유지
- 안전성을 증명하지 못하면 `판단 보류`

### Flexible hand-in

- 여러 후보 Item ID를 그룹 단위로 계산
- 후보별 보유량 합산
- 후보 하나를 임의 선택하지 않음
- 목표 종료 전 후보 하나만 따로 cleanup 가능하다고 자동 판단하지 않음
- 아직 하나도 보유하지 않았더라도 모든 후보를 Item 목록에 표시

### Item 화면

목록:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요량
- 보유량
- 추가 필요 / 충분 / 정리 / 판단 보류

상세:

- FIR 최소 요구
- FIR / Non-FIR 보유 입력
- 전체 출처
- cleanup 보호 이유
- 해당 Item이 후보일 때만 flexible hand-in 상세

## 10. CORE-006 — Ammo

`CONFIRMED / IMPLEMENTED`

Ammo는 선택 GameMode의 Game Content를 읽는 **비교 중심 read-only 기능**입니다.

User Progress와 결합하지 않습니다.

### 탐색

- 이름 검색 없음
- 구경 dropdown 사용

### 표 열

`표시 열` 메뉴에서 속성을 선택/해제합니다.

숨긴 속성도 선택 Ammo 상세에는 계속 표시합니다.

### 정렬

항상:

1. penetration power 오름차순
2. damage 오름차순
3. name 순

사용자 header sort로 이 기준을 깨뜨리지 않습니다.

### 수급처

표에는 비교용 최소 경로를 표시합니다.

예:

- Prapor LL3
- Mechanic LL2 교환
- Workbench Lv.3

상인 구매/교환/제작 경로가 모두 없을 때만 `레이드 획득`으로 표시합니다.

상세에는 전체 수급처 정보를 유지합니다.

### 이미지

canonical Ammo Item `IconUrl`을 표와 상세에 표시합니다.

### Armor Class 1~6 0~6 effectiveness

`CONFIRMED / IMPLEMENTED`

사용자 요구:

- Class 1~6 여섯 칸
- 각 칸 0~6 숫자
- 숫자에 대응하는 색상
- Tarkov Wiki `Bullet effectiveness against armor class`와 같은 값/의미

구현 정책:

1. raw Ammo stats는 계속 `json.tarkov.dev` 사용
2. Class 1~6 0~6 값은 Wiki Ballistics의 **명시된 값**을 optional enrichment로 사용
3. 자체 penetration/class ratio나 threshold heuristic 금지
4. canonical 영문 Ammo 이름과 유일하게 매칭되는 Wiki row만 적용
5. 모호함/충돌/미매칭은 `unknown`
6. 전체 매칭 수가 비정상적으로 적으면 Wiki schema 변화로 보고 그 update의 effectiveness를 적용하지 않음
7. Wiki 요청 장애는 기본 Game Content update 실패로 취급하지 않음
8. optional Wiki 요청은 제한 시간을 둠

UI:

- 표에 Class 1~6 colored cells
- 숫자는 항상 표시
- 색상은 보조 신호
- unknown은 `?`
- 표 열을 숨겨도 상세에는 계속 표시

근거: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

## 11. UI-001 — 전역 디자인

`CONFIRMED / IMPLEMENTED`

- dark background
- 밝은 본문
- 기존 accent
- native white dropdown popup 방지
- dropdown / scrollbar / button / textbox를 프로그램과 어울리는 부드러운 형태로 통일
- list row 전체 폭 정렬

장식보다 읽기 쉬운 정보 구조와 비교성을 우선합니다.

## 12. UI-002 — 이미지 cache

`CONFIRMED / IMPLEMENTED`

Item / Hideout / Ammo 이미지는 canonical URL을 사용하고 Desktop에서 local cache합니다.

```text
%LocalAppData%/JunhyunHelper/image-cache
```

- source URL 변경 시 새 cache entry
- cache는 권위 데이터 아님
- download/decode 실패는 기능 전체 실패 아님
- invalid payload 삭제 후 다음 요청에서 회복 가능
- Game Content/User Progress와 수명주기 분리

## 13. UI-003 — Map / Scanner 탭

`CONFIRMED / PLACEHOLDER IMPLEMENTED`

상단에:

- 지도
- 스캐너

탭이 존재합니다.

현재 실제 기능은 미구현이며 `준비 중`을 명확히 표시합니다.

검증되지 않은 기능을 뒤에서 실행하지 않습니다.

## 14. 현재 범위 밖 / 후속

- Quest reward 전체 모델
- runtime AI/GPT
- 검증되지 않은 Map 실제 기능
- Scanner 실제 기능 — 요구사항 확정 후 구현
- 기존 Tarkov-Helper를 이유만으로 승계하는 기능

## 15. 첫 실사용 피드백 구현 상태

| 번호 | 내용 | 상태 |
|---:|---|---|
| 1 | dark dropdown/scrollbar 및 UI 다듬기 | 구현 완료 |
| 2 | Hideout/Item/Ammo 이미지 | 구현 완료 |
| 3 | Quest/Hideout list 정렬 | 구현 완료 |
| 4 | +/- 진행 입력, 일반 Trader LL, Fence 0.1 | 구현 완료 |
| 5 | Hideout 미입력 = Lv.0 | 구현 완료 |
| 6 | Item 목록 재설계 | 구현 완료 |
| 7 | Ammo 표 최소 수급 경로 | 구현 완료 |
| 8 | 데이터 업데이트 진행률 | 구현 완료 |
| 9 | Profile 버튼 정리 | 구현 완료 |
| 10 | Ammo 검색 제거/열 선택/관통 오름차순 | 구현 완료 |
| 10-b | Class 1~6 0~6 rating | 구현 완료 |
| 11 | Trader/Map 실제 게임 순서 | 구현 완료 |
| 12 | Ground Zero 21+ 병합 | 구현 완료 |
| 13 | Map/Scanner placeholder 탭 | 구현 완료 |
