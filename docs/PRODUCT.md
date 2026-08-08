# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자와 확정한 의도는 과거 구현보다 우선합니다.

## 1. 제품 이름

`CONFIRMED`

**준현 헬퍼**

저장소: `Propeex/JunhyunHelper`

## 2. 제품 핵심 정의

`CONFIRMED`

준현 헬퍼는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 프로그램이 스스로 이해할 수 있는 **canonical Game Content와 로컬 DB로 변환·재구축**하고, 이를 사용자 진행 상태와 결합해 플레이에 필요한 정보를 제공하는 Windows 데스크톱 헬퍼입니다.

핵심 원칙:

> 게임 데이터의 내용이 바뀌더라도 외부 형식이 기존 importer가 이해할 수 있는 범위라면 프로그램이 최신 데이터를 다시 내려받아 같은 변환 규칙으로 DB를 다시 만들 수 있어야 합니다.

일반적인 게임 데이터 업데이트에 GPT가 개입하지 않습니다.

## 3. 데이터 갱신 및 저장 원칙

`CONFIRMED`

```text
온라인 데이터
→ 다운로드
→ 외부 형식/필수 의미 검증
→ canonical model 변환
→ candidate DB
→ 관계/read-back 검증
→ active Game Content 교체
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

원칙:

- 내용만 바뀌는 변화는 importer가 이해하는 한 자동 흡수
- 핵심 필드 삭제/타입·의미 변경처럼 안전하게 이해할 수 없는 변화는 update 실패
- 검증되지 않은 candidate로 마지막 정상 active content를 덮어쓰지 않음
- Game Content update가 User Progress를 삭제/덮어쓰지 않음
- 파생 결과를 별도 진실의 원천처럼 저장하지 않음

User Progress 사실:

- Profile 설정
- Quest 완료 및 필요한 명시적 영구 실패
- Hideout level
- Trader 진행 사실
- FIR / Non-FIR inventory

파생 결과:

- Quest Current/Locked/Unavailable
- Needed Items
- cleanup
- flexible hand-in progress
- next Hideout upgrade

## 4. 데이터 원천

### 4.1 1차 원천

`CONFIRMED`

`json.tarkov.dev`

현재 사용 영역:

- Quest
- Hideout
- Item
- Item category metadata
- Trader
- Map 최소 메타데이터
- Barter
- Craft
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 4.2 보조 원천

`CONFIRMED`

- TarkovTracker `tarkov-data-overlay`: editions 정보만
- Escape from Tarkov Wiki `Ballistics`: Ammo Armor Class 1~6의 **명시된 0~6 effectiveness 값만** optional enrichment

전체 community correction overlay는 자동 적용하지 않습니다.

Wiki Ballistics는 raw Ammo 사실의 대체 원천이 아닙니다. Wiki enrichment가 실패하면 기본 Game Content update는 계속하고 해당 effectiveness만 unknown으로 둡니다.

### 4.3 Map 실제 기능 데이터

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

### 5.1 진행 표시

`CONFIRMED / IMPLEMENTED`

- progress overlay
- progress bar
- 현재 단계/작업 설명
- 퍼센트
- 완료/실패 상태

가짜 timer progress를 사용하지 않습니다. 실제 source 완료 수와 실제 pipeline 단계에서 계산합니다.

### 5.2 Content schema 변경

canonical Game Content 구조가 의도적으로 확장되어 이전 snapshot을 안전하게 그대로 해석할 수 없으면 schema version을 올립니다.

현재 2차 실사용 개선에서 Item category metadata가 추가되어 content snapshot은 v2가 됩니다.

- 기존 v1 content.db는 자동 재구축
- `user.db`는 분리되어 있으므로 사용자 진행은 유지

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
- 일반 Trader: LL 중심, 정수 1단위
- Fence: standing 0.1단위
- 비-Fence standing은 실제 Quest 판정에 필요한 경우에만 고급 입력

LL과 standing은 별개의 optional fact입니다.

## 7. CORE-003 — Quest

`CONFIRMED / IMPLEMENTED`

준현 헬퍼에서는 실제 게임에서 수주 가능한 Quest를 이미 수락한 것으로 봅니다.

별도 Accept 버튼은 두지 않습니다.

### 7.1 상태

사용자에게 보이는 정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

Core는 진단을 위해 `Indeterminate`도 유지합니다.

### 7.2 residual Indeterminate 제품 정책

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

현재 시스템이 지원하는 해금/선행 조건을 모두 적용한 뒤에도 끝까지 `Indeterminate`로 남는 Quest는 **사용자 화면에서는 Current(진행 중)** 로 취급합니다.

이 정책은 Application 제품 경계에서 적용합니다.

```text
Core: Indeterminate + diagnostic reasons
→ Application product policy
→ Current + same diagnostic reasons
→ Desktop: 진행 중
```

원칙:

- Core의 진단 reason은 보존
- 확정 가능한 Locked/Unavailable은 변경하지 않음
- residual Indeterminate에만 적용
- 해당 Quest는 완료 처리 가능
- 별도 `판정 문제` 목록으로 사용 흐름을 막지 않음

### 7.3 사용자 조작

- 완료
- 완료 취소
- 정말 필요한 희귀 비재시작형 영구 실패만 실패 처리/취소

Quest reward 전체 모델은 핵심 범위에서 제외합니다.

### 7.4 목록 UI

- 행 전체 폭 정렬
- 이름 길이와 관계없이 반복 정보 위치 정렬
- dark theme interaction style

### 7.5 Trader / Map filter 순서

`CONFIRMED / IMPLEMENTED`

알파벳순이 아니라 게임에서 익숙한 고정 순서를 사용합니다.

Trader:

```text
Prapor → Therapist → Fence → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref → Lightkeeper → BTR Driver
```

미래 unknown 값은 숨기지 않고 알려진 항목 뒤에 둡니다.

### 7.6 Ground Zero 21+

`CONFIRMED / IMPLEMENTED`

- canonical map ID 유지
- Quest 실제 MapId 유지
- Quest Map filter에서만 Ground Zero와 Ground Zero 21+를 하나의 `Ground Zero` 항목으로 그룹화

### 7.7 Quest 제출 Item UI

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

제출 Item을 긴 문자열로 나열하지 않습니다.

각 Item을 독립된 card/row로 표시합니다.

표시:

- Item icon
- Item 이름
- 요구 수량
- FIR 여부
- 유동 제출 후보 여부

유동 제출 requirement는 후보별 card를 보여주되, 수량이 그룹 전체 합계 목표임을 명시합니다.

### 7.8 Quest 정보 연결

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

- Quest 제출 Item 클릭 → 해당 Item 상세
- 선행 Quest 클릭 → 해당 Quest 상세

연결은 표시 이름이 아니라 canonical stable ID를 사용합니다.

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

### 9.1 미래 필요량 포함

- Current Quest 제출 아이템
- 미래에 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 모든 미래 upgrade material

### 9.2 제외

- Completed Quest
- 현재 캐릭터에서 영구 불가임이 증명된 Quest
- 닫힌 branch
- 이미 지난 Hideout upgrade

### 9.3 Inventory

- FIR / Non-FIR 직접 입력
- User Progress 독립 사실
- Game Content update로 삭제하지 않음

### 9.4 cleanup

- 미래 필요량 충족 후 남는 안전한 초과분만 표시
- FIR 최소 요구 우선 보호
- metadata가 사라져도 stable Item ID 보유 기록 유지
- 안전성을 증명하지 못하면 `판단 보류`

### 9.5 Flexible hand-in 계산

- 여러 후보 Item ID를 그룹 단위로 계산
- 후보별 보유량 합산
- 후보 하나를 임의 선택하지 않음
- 목표 종료 전 후보 하나만 따로 cleanup 가능하다고 자동 판단하지 않음
- 아직 하나도 보유하지 않았더라도 모든 후보에 접근할 수 있어야 함

### 9.6 Flexible hand-in UI 분리

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

유동 제출 후보가 일반 Item 목록을 과도하게 채우지 않도록 별도 view로 분리합니다.

- **유동 제출 때문에만** 목록에 들어온 후보는 일반 목록에서 제외
- `유동 제출 보기`에서 모든 후보 제공
- 일반 고정 필요량이나 실제 보유량도 있는 후보는 일반 목록에도 남을 수 있음
- 계산/cleanup 보호 의미는 변경하지 않음

### 9.7 Item 종류 분류

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

분류 권위 데이터는 과거 Tarkov-Helper 하드코딩이 아니라 현재 `json.tarkov.dev`의 Item category metadata입니다.

```text
item.categories[]
→ category IDs
→ itemCategories[id].normalizedName
→ canonical GameItem category metadata
→ Desktop 상위 표시 그룹
```

현재 상위 표시 그룹:

- 무기
- 무기 부품
- 장비
- 탄약
- 의약품
- 식량/음료
- 물물교환
- 열쇠
- 정보
- 특수 장비
- 퀘스트 아이템
- 화폐
- 지도
- 기타

원칙:

- 외부 category ID는 변경하지 않음
- 매 Game Content update 때 category metadata도 다시 읽음
- 알 수 없는 미래 category는 숨기지 않고 `기타` fallback
- Item 화면에 종류 filter 제공

### 9.8 Item 화면

일반 목록:

- icon
- 이름
- 종류
- 필요 출처 요약
- 미래 필요량
- 보유량
- 추가 필요 / 충분 / 정리 / 판단 보류

상세:

- 종류
- FIR 최소 요구
- FIR / Non-FIR 보유 입력
- 전체 필요 출처
- cleanup 보호 이유
- 해당 Item이 후보일 때 flexible hand-in 상세

### 9.9 Item → Quest 연결

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

- Item 상세의 Quest 필요 출처 클릭 → 해당 Quest 상세
- Flexible hand-in Quest 클릭 → 해당 Quest 상세
- Quest 상세에서 현재 Needed 목록에 없는 Item을 열어도 canonical Item이 존재하면 reference detail 제공

stable ID로 연결합니다.

## 10. CORE-006 — Ammo

`CONFIRMED / IMPLEMENTED`

Ammo는 선택 GameMode의 Game Content를 읽는 **비교 중심 read-only 기능**입니다.

User Progress와 결합하지 않습니다.

### 10.1 탐색

- 이름 검색 없음
- 구경 dropdown 사용

### 10.2 표 열

`표시 열` 메뉴에서 속성을 선택/해제합니다.

숨긴 속성도 선택 Ammo 상세에는 계속 표시합니다.

### 10.3 정렬

항상:

1. penetration power 오름차순
2. damage 오름차순
3. name 순

사용자 header sort로 이 기준을 깨뜨리지 않습니다.

### 10.4 수급처

표에는 비교용 최소 경로를 표시합니다.

예:

- Prapor LL3
- Mechanic LL2 교환
- Workbench Lv.3

상인 구매/교환/제작 경로가 모두 없을 때만 `레이드 획득`으로 표시합니다.

상세에는 전체 수급처 정보를 유지합니다.

### 10.5 Armor Class 1~6 effectiveness

`CONFIRMED / IMPLEMENTED`

- Class 1~6 여섯 칸
- 각 칸 0~6 숫자
- 숫자에 대응하는 색상
- Tarkov Wiki `Bullet effectiveness against armor class`와 같은 값/의미

정책:

1. raw Ammo stats는 `json.tarkov.dev`
2. 0~6 값은 Wiki Ballistics의 명시 값만 optional enrichment
3. 자체 penetration/class ratio 또는 threshold heuristic 금지
4. canonical 영문 Ammo 이름과 유일하게 매칭되는 Wiki row만 적용
5. 모호함/충돌/미매칭은 unknown
6. 비정상적 전체 match coverage는 Wiki schema 변화로 간주하고 해당 update 값 미적용
7. Wiki 장애는 기본 Game Content update 실패가 아님
8. unknown은 `?`

근거: `docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`

## 11. UI-001 — 전역 디자인

`CONFIRMED / IMPLEMENTED`

- dark background
- 밝은 본문
- 기존 accent
- native white dropdown popup 방지
- dropdown / button / textbox / list를 일관된 부드러운 형태로 통일
- list row 전체 폭 정렬

장식보다 읽기 쉬운 정보 구조와 비교성을 우선합니다.

### 11.1 ScrollBar

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

색상 일부만 덮고 native WPF chrome을 남기지 않습니다.

- vertical / horizontal 전체 ControlTemplate 교체
- native arrow button chrome 제거
- rounded dark track
- rounded thumb
- hover/drag accent

## 12. UI-002 — 이미지 cache

`CONFIRMED / IMPLEMENTED`

Item / Hideout / Ammo / Quest Item 이미지는 canonical URL을 사용하고 Desktop에서 local cache합니다.

```text
%LocalAppData%/JunhyunHelper/image-cache
```

### 12.1 source image decode 정책

`CONFIRMED / IMPLEMENTED IN SECOND USABILITY PASS`

canonical source에는 WebP 등 WPF 기본 decoder가 PC 환경에 따라 안정적으로 읽지 못하는 형식이 존재할 수 있습니다.

따라서:

```text
canonical URL
→ bytes 다운로드
→ SkiaSharp decode
→ 크기/유효성 검증
→ PNG normalize
→ local cache
→ WPF BitmapImage
```

원칙:

- 원본 URL은 canonical Game Content가 소유
- cache는 권위 데이터가 아님
- source URL 변경 시 새 cache entry
- download/decode 실패는 기능 전체 실패 아님
- invalid payload는 제거하여 다음 요청에서 회복 가능
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
- 기존 Tarkov-Helper 동작을 존재한다는 이유만으로 승계하는 기능

## 15. 실사용 피드백 상태

### 첫 실사용 피드백 1~13

`IMPLEMENTED / MERGED`

- dark controls
- images/cache 1차
- Quest/Hideout row layout
- +/- 입력 및 Trader/Fence 정책
- Hideout unset = Lv.0
- Item 판단용 목록
- Ammo 최소 수급 경로
- update progress
- Profile control 정리
- Ammo column control / 고정 정렬
- Armor effectiveness
- Trader/Map order
- Ground Zero grouping
- Map/Scanner placeholder

### 2차 실사용 피드백 1~7

`IMPLEMENTED / VERIFICATION IN PROGRESS — PR #36`

1. WebP 포함 icon 표시 안정화
2. residual Indeterminate Quest → Current 제품 정책
3. ScrollBar 전체 template 수정
4. flexible-only 후보 별도 view
5. Tarkov category metadata 기반 Item 종류 filter
6. Quest 제출 Item icon/card UI
7. Quest ↔ Item 및 prerequisite Quest 상호 이동

상세 설계/근거: `docs/SECOND_USABILITY_PASS.md`
