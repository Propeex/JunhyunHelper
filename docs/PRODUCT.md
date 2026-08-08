# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항 문서입니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자와 확정한 의도는 과거 구현보다 우선합니다.

## 1. 제품 이름

`CONFIRMED`

**준현 헬퍼**

저장소 이름은 `JunhyunHelper`입니다.

## 2. 제품의 핵심 정의

`CONFIRMED`

준현 헬퍼는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 **프로그램 스스로 해석 가능한 canonical Game Content와 로컬 DB로 변환·재구축**하고, 이를 사용자의 게임 진행 상태와 결합해 플레이에 필요한 정보를 제공하는 Windows 데스크톱 헬퍼입니다.

핵심은 특정 패치의 내용을 소스 코드에 수작업으로 박아 넣는 것이 아닙니다.

> 게임 데이터의 내용이 바뀌어도 외부 형식이 기존 importer가 이해할 수 있는 범위라면 프로그램이 최신 데이터를 다시 내려받아 같은 변환 규칙으로 DB를 다시 만들 수 있어야 합니다.

일반적인 데이터 업데이트에 GPT가 개입하지 않습니다.

## 3. 최상위 데이터 원칙

`CONFIRMED`

### 변환 규칙

개발자는 개발 과정에서 다음을 구현합니다.

- 외부 데이터 구조 분석
- 외부 의미 → canonical 내부 의미의 매핑
- 필수 필드/관계 검증
- 안전한 실패 규칙

프로그램은 이후 같은 규칙을 반복 실행합니다.

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

### 스키마 안전성

- 이름/수량/요구사항/성능처럼 **내용만 바뀌는 변화**는 자동 흡수
- importer가 이해할 수 있는 호환 변화는 가능한 한 흡수
- 핵심 필드 삭제/타입·의미 변경 등 비호환 변화는 감지 후 업데이트 실패
- 검증되지 않은 새 DB로 마지막 정상 DB를 덮어쓰지 않음

### Game Content / User Progress 분리

Game Content 업데이트가 다음 사용자 사실을 삭제하거나 덮어쓰면 안 됩니다.

- Profile 설정
- Quest 완료/명시적 영구 실패
- Hideout level
- Trader 진행 사실
- FIR / Non-FIR 보유량

Needed Items, Quest 상태, cleanup 같은 파생 결과는 별도 사실로 저장하지 않습니다.

## 4. 데이터 원천

### 1차 원천

`CONFIRMED`

현재 핵심 원천은 `json.tarkov.dev`입니다.

사용 영역:

- Quest
- Hideout
- Item
- Trader
- Map 최소 메타데이터
- Barter
- Craft
- Ammo 관련 item 속성

지원 GameMode:

- regular
- pve
- pvp-season

### 보조 원천

`CONFIRMED`

TarkovTracker `tarkov-data-overlay`의 **editions 정보만** 사용합니다.

전체 community correction overlay를 자동 적용하지 않습니다.

### 지도 이미지/실시간 지도 데이터

`OPEN`

실제 Map 기능을 위한 최종 데이터 공급원은 아직 확정하지 않았습니다.

따라서 Map 탭은 UI placeholder를 먼저 둘 수 있지만, 검증되지 않은 지도 데이터 기능을 임의 구현하지 않습니다.

## 5. CORE-001 — Game Content 업데이트

`CONFIRMED`

- 선택 GameMode의 최신 온라인 데이터를 다운로드
- canonical model로 변환
- candidate DB 생성
- 스키마/관계/read-back 검증
- 성공한 경우에만 active content 교체
- 실패 시 마지막 정상 Game Content와 User Progress 유지

### 업데이트 진행 표시

`CONFIRMED / IMPLEMENTATION PENDING`

사용자가 데이터 업데이트 중 현재 진행 상황을 시각적으로 알 수 있어야 합니다.

최종 UI는 최소 다음을 제공합니다.

- 진행 중임을 명확히 표시
- 진행률 bar
- 현재 단계/작업 설명
- 완료/실패 상태

가짜 퍼센트를 만들지 않습니다. 실제 단계 또는 측정 가능한 작업량에서 진행률을 계산합니다.

## 6. CORE-002 — Profile / 진행 입력

`CONFIRMED`

한 GameMode당 프로필 하나를 기본으로 합니다.

상단 UI:

- Profile dropdown
- `프로필 수정`
- `데이터 업데이트`

프로필 생성은 Profile dropdown 안의 `새 프로필` 항목으로 진입합니다.

프로필 삭제는 `프로필 수정` 흐름 안에 둡니다.

### Player level

- 직접 텍스트 입력/드롭다운이 아니라 `- / 값 / +`
- 정수 1단위 변경

### Prestige

- `- / 값 / +`
- 정수 1단위 변경
- 실제 제품 규칙상 unknown이 필요하면 별도 의미를 보존하고 몰래 0으로 확정하지 않음

### Trader

일반 Trader:

- 기본 입력은 LL만
- `- / 값 / +` 정수 1단위
- 일반 standing은 화면에서 기본 노출하지 않음

Fence:

- standing을 사용
- `- / 값 / +`
- 0.1 단위

비-Fence standing이 실제 Quest 판정에 필요한 경우에만 고급 입력으로 제공할 수 있습니다.

LL과 standing은 저장 구조에서도 별개의 optional fact입니다.

## 7. CORE-003 — Quest

`CONFIRMED`

사용자가 게임에서 수주 가능한 Quest는 준현 헬퍼에서 이미 진행 가능한 것으로 봅니다.

별도 Accept 버튼을 만들지 않습니다.

정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

`Indeterminate`는 정상 진행 상태가 아니라 안전한 판정에 필요한 사실/데이터 의미가 부족한 문제 상태입니다.

사용자 조작:

- 완료
- 완료 취소
- 정말 필요한 희귀 비재시작형 영구 실패에만 실패 처리/취소

Quest 보상은 핵심 범위에서 제외합니다.

### Quest list UI

`CONFIRMED`

- 행 전체 폭을 사용해 정렬된 목록으로 표시
- 이름 길이에 따라 좌측에 제각각 뭉쳐 보이는 형태를 피함
- 상인/상태 등 반복 요소의 위치를 행마다 정렬
- 전체 dark theme과 동일한 interaction style 사용

### Trader / Map filter 순서

`CONFIRMED / IMPLEMENTATION PENDING`

Trader와 Map dropdown은 알파벳순/임의 순서가 아니라 **실제 게임에서 익숙한 순서**로 고정합니다.

순서를 코드 여러 곳에 중복 하드코딩하지 않고 canonical UI ordering helper로 관리합니다.

게임 순서를 기술적으로 확정할 수 없는 항목만 사용자 확인 대상으로 남깁니다.

### Ground Zero 21+

`CONFIRMED / VERIFICATION PENDING`

`Ground Zero 21+`는 별도 지도처럼 노출하지 않고 **Ground Zero와 같은 맵 그룹**으로 취급합니다.

Quest Map filter에서도 Ground Zero 하나로 병합합니다.

## 8. CORE-004 — Hideout

`CONFIRMED`

- 최신 Hideout 데이터를 canonical model로 사용
- 시설별 현재 레벨을 사용자가 입력
- 화면은 `- / 현재 레벨 / +`

### 미입력 의미

**미입력과 Lv.0은 같은 제품 상태입니다.**

- 별도 `미입력` 상태를 사용자에게 요구하지 않음
- 저장 row가 없으면 Lv.0으로 계산 가능
- 0을 모든 station에 명시적으로 저장할 필요는 없음

### 계산

- 상세는 현재 레벨에서 바로 다음 upgrade 표시
- Needed Items는 현재 레벨보다 높은 **모든 미래 upgrade material** 포함

### 이미지

`CONFIRMED`

Hideout station은 canonical `ImageUrl`을 사용하여:

- station list
- detail header

에 이미지를 표시합니다.

이미지 실패는 Hideout 데이터 실패로 취급하지 않습니다.

## 9. CORE-005 — Needed Items / Item

`CONFIRMED`

이 기능이 준현 헬퍼의 핵심 제작 이유입니다.

> 지금 당장 필요한 물건만이 아니라 현재 캐릭터가 앞으로 사용할 가능성이 있는 물건을 미리 모으고, 더 이상 필요하지 않은 실제 보유품은 안전하게 정리하도록 돕습니다.

### 미래 필요량 포함

- Current Quest 제출 아이템
- 앞으로 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 모든 미래 upgrade material

### 제외

- Completed Quest
- 현재 캐릭터에서 영구 불가임이 증명된 Quest
- 확정된 진행으로 닫힌 branch
- 이미 지난 Hideout upgrade

### Inventory

- FIR / Non-FIR 직접 입력
- User Progress의 독립 사실
- Game Content 업데이트로 삭제하지 않음

### cleanup

- 미래 필요량을 충족하고 남는 안전한 초과분만 표시
- FIR 최소 수량을 먼저 보호
- metadata가 새 Game Content에서 사라져도 보유 Item ID는 계속 노출
- 안전성을 증명하지 못하면 `판단 보류`

### Flexible hand-in

여러 Item ID 중 합계로 제출하는 요구는 그룹 단위로 계산합니다.

- 후보 하나를 임의 선택해서 저장하지 않음
- 후보별 보유량을 합산
- 목표가 끝나기 전 후보 하나만 따로 정리 가능하다고 자동 판단하지 않음
- **아직 아무 후보도 보유하지 않았더라도 모든 후보를 Item 목록에서 보여주어 첫 보유량을 입력할 수 있어야 함**

### Item 화면

`CONFIRMED`

기존 진단 dump처럼 긴 텍스트를 나열하지 않습니다.

목록 행:

- 아이콘
- 이름
- 필요 출처 요약
- 미래 필요 수량
- 보유 수량
- `추가 필요 / 충분 / 정리 / 판단 보류`

상세:

- FIR 최소 요구
- FIR / Non-FIR 보유 입력
- 전체 출처
- cleanup 보호 이유
- 선택 Item이 후보일 때만 flexible hand-in 상세

전체 flexible hand-in dump를 화면 상단에 항상 노출하지 않습니다.

### Item 이미지

canonical `GameItem.IconUrl`을 사용합니다.

## 10. CORE-006 — Ammo

`CONFIRMED`

Ammo는 선택 GameMode의 최신 Game Content를 읽는 **비교 중심 read-only 기능**입니다.

별도 사용자 진행 상태를 저장하지 않습니다.

### 기본 탐색

- 이름 검색 없음
- 구경 dropdown 사용

### 표 열

`표시 열` 메뉴에서 속성을 체크/해제합니다.

체크 해제한 속성은 표에서 숨기되 선택 Ammo의 상세에는 계속 표시합니다.

### 기본 정렬

항상:

1. penetration power 오름차순
2. 동률이면 damage 오름차순
3. 동률이면 name 순

사용자가 표 header 정렬로 이 비교 기준을 우연히 깨뜨리지 않게 합니다.

### 수급처

표에는 비교를 위한 최소 정보만 표시합니다.

예:

- Prapor LL3
- Mechanic LL2 교환
- Workbench Lv.3

상인/교환/제작의 구조화된 영구 수급 경로가 하나도 없을 때만 `레이드 획득`으로 표시합니다.

상세에는 기존처럼 실제 수급처 정보를 충분히 보여줍니다.

### 이미지

canonical Ammo Item의 `IconUrl`을 사용하여 표와 상세 header에 표시합니다.

### Armor Class 1~6 0~6 effectiveness

`CONFIRMED REQUIREMENT / BLOCKED UNTIL VERIFIED`

사용자 요구:

- Class 1~6 6칸
- 각 칸 0~6 숫자
- 숫자에 대응한 색상
- Tarkov Wiki `Bullet effectiveness against armor class`와 같은 의미

임의 자체 점수는 금지합니다.

현재 raw API에 rating 자체가 없고 exact derivation/source가 아직 검증되지 않았습니다.

정확한 source 또는 동일 결과를 만드는 공식이 검증된 뒤 구현합니다.

조사 문서: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

## 11. UI-001 — 전역 디자인

`CONFIRMED`

준현 헬퍼 전체는 dark UI로 통일합니다.

- 어두운 배경
- 밝은 본문 텍스트
- 기존 accent 사용
- native WPF 흰 dropdown popup이 새지 않도록 명시적 dark template
- scroll bar / button / text box / combo box를 기본 OS 모양 그대로 두지 않고 프로그램과 어울리는 부드러운 형태로 통일
- 리스트 행은 전체 폭 정렬

제품 디자인은 장식보다 **읽기 쉬운 정보 구조와 비교성**을 우선합니다.

## 12. UI-002 — 이미지 cache

`CONFIRMED`

Item / Hideout / Ammo 이미지는 온라인 canonical URL을 사용하되 Desktop에서 로컬 cache합니다.

```text
%LocalAppData%/JunhyunHelper/image-cache
```

원칙:

- source URL이 바뀌면 새 cache entry
- cache는 권위 데이터가 아님
- 다운로드/디코딩 실패는 기능 전체 실패가 아님
- invalid payload는 삭제해 다음 요청에서 회복 가능
- Game Content/User Progress와 수명주기를 분리

## 13. UI-003 — Map / Scanner 탭

`CONFIRMED / IMPLEMENTATION PENDING`

실제 기능이 아직 미구현이어도 상단 내비게이션에:

- 지도
- 스캐너

탭을 추가합니다.

미구현 상태에서는 사용자가 혼동하지 않도록 **준비 중**임을 명확히 표시합니다.

검증되지 않은 기능을 placeholder 뒤에서 임의 실행하지 않습니다.

## 14. 범위 밖 / 후속

현재 즉시 핵심에 넣지 않는 것:

- Quest reward 전체 모델
- runtime AI/GPT
- 검증되지 않은 지도 데이터 기능
- 검증되지 않은 Ammo effectiveness heuristic
- 기존 Tarkov-Helper의 동작을 이유만으로 그대로 승계하는 기능

## 15. 현재 첫 실사용 피드백 구현 상태

| 번호 | 내용 | 상태 |
|---:|---|---|
| 1 | dark dropdown/scrollbar 및 UI 다듬기 | 구현 완료 |
| 2 | Hideout/Item/Ammo 이미지 | 구현/검증 중 (PR #32) |
| 3 | Quest/Hideout list 정렬 | 구현 완료 |
| 4 | +/- 진행 입력, 일반 Trader LL, Fence 0.1 | 구현 완료 |
| 5 | Hideout 미입력 = Lv.0 | 구현 완료 |
| 6 | Item 목록 재설계 | 구현/검증 중 (PR #32) |
| 7 | Ammo 표 최소 수급 경로 | 구현 완료 |
| 8 | 데이터 업데이트 진행률 | 구현 대기 |
| 9 | Profile 버튼 정리 | 구현 완료 |
| 10 | Ammo 검색 제거/열 선택/관통 오름차순 | 구현 완료 |
| 10-b | Class 1~6 0~6 rating | source/공식 검증 중 |
| 11 | Trader/Map 실제 게임 순서 | 구현 대기 |
| 12 | Ground Zero 21+ 병합 | 구현 상태 재검증 필요 |
| 13 | Map/Scanner placeholder 탭 | 구현 대기 |
