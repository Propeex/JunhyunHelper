# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자가 명시한 제품 의도가 과거 구현보다 우선합니다.

## 1. 제품 정의

`CONFIRMED`

**준현 헬퍼**는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 프로그램이 스스로 canonical Game Content와 로컬 DB로 변환·재구축하고, 이를 User Progress와 결합해 플레이에 필요한 정보를 제공하는 Windows 데스크톱 헬퍼입니다.

저장소: `Propeex/JunhyunHelper`

가장 중요한 원칙:

> 게임 데이터의 내용이 바뀌어도 외부 형식이 importer가 이해할 수 있는 범위라면 프로그램이 최신 데이터를 다시 내려받아 같은 변환 규칙으로 DB를 다시 만들 수 있어야 합니다.

일반적인 데이터 업데이트에 GPT가 개입하지 않습니다.

## 2. 데이터 갱신과 저장

`CONFIRMED / IMPLEMENTED`

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

- 내용 변화는 importer가 이해하는 한 자동 흡수
- 핵심 필드 삭제/타입·의미 변경은 안전하게 update 실패
- 검증되지 않은 candidate로 마지막 정상 active content를 덮어쓰지 않음
- Game Content update가 `user.db`를 삭제/덮어쓰지 않음
- Quest 상태, Needed Items, cleanup 등 파생 결과는 진실의 원천처럼 저장하지 않음

User Progress 사실:

- Profile 설정
- Quest 완료 및 필요한 명시적 영구 실패
- Hideout level
- Trader 진행
- **인레이드 / 일반** Inventory

내부 호환성 때문에 코드/DB 필드명이 `Fir` / `NonFir`인 부분은 유지할 수 있지만 사용자 화면에서는 `인레이드` / `일반`을 사용합니다.

현재 Content snapshot schema는 **v2**이며 Item category metadata를 포함합니다. 기존 v1 content DB는 온라인 데이터로 재구축되고 `user.db`는 유지됩니다.

## 3. 데이터 원천

### 3.1 1차 원천

`json.tarkov.dev`

현재 사용 영역:

- Quest
- Hideout
- Item 및 Item category metadata
- Trader
- Map 최소 메타데이터
- Barter
- Craft
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 3.2 보조 원천

- TarkovTracker `tarkov-data-overlay`: edition rules만
- Escape from Tarkov Wiki `Ballistics`: Ammo 비교 대상 membership 및 Armor Class 1~6의 명시된 0~6 effectiveness

Wiki Ballistics는 raw Ammo 성능의 대체 원천이 아닙니다.

Wiki source가 정상일 때는 현재 Ballistics 표와 안전하게 매칭된 탄약만 Ammo 비교 화면에 표시합니다. Wiki 장애/구조 이상으로 정상 membership을 확인할 수 없을 때는 기본 Ammo Game Content를 삭제하지 않고 임시 표시하며 source 상태를 명시합니다.

영구 하드코딩된 Ammo allowlist는 만들지 않습니다.

### 3.3 Map 실제 기능

`OPEN`

Map 실제 기능의 최종 데이터 공급원은 아직 확정하지 않았습니다. 현재 Map 탭은 placeholder입니다.

## 4. Game Content 업데이트 UI

`CONFIRMED / IMPLEMENTED`

- 수동 업데이트 및 최초/복구 업데이트에서 같은 pipeline 사용
- progress overlay / progress bar / 현재 단계 / 퍼센트 표시
- timer 기반 가짜 진행률 금지
- 실제 source 완료 수와 실제 pipeline 단계에서 계산
- 실패 시 마지막 정상 active content와 User Progress 유지

## 5. Profile / 진행 입력

`CONFIRMED / IMPLEMENTED`

한 GameMode당 프로필 하나를 기본으로 합니다.

상단:

- Profile dropdown
- `프로필 수정`
- `데이터 업데이트`

Profile dropdown 내부:

- 기존 프로필
- `새 프로필`

삭제는 `프로필 수정` 안에 둡니다.

### 5.1 주요 진행값

- Player level: `- / 값 / +`, 정수 1단위
- Prestige: `- / 값 / +`, 정수 1단위
- **펜스 우호도**: 상단 주요 진행값으로 별도 배치, 0.1 단위

### 5.2 상인

핵심 상인은 인게임에서 익숙한 순서로 LL만 기본 표시합니다.

Fence를 제외한 핵심 순서:

```text
Prapor → Therapist → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref
```

일반 핵심 상인 밖의 Trader는 `특별` 섹션으로 분리합니다.

현재 알려진 순서:

```text
Lightkeeper → BTR Driver → future unknown traders
```

Quest 판정에 실제 standing이 필요한 비-Fence 상인만 고급 입력을 제공합니다. LL과 standing은 별개의 optional fact입니다.

## 6. Quest

`CONFIRMED / IMPLEMENTED`

실제 게임에서 수주 가능한 Quest는 준현 헬퍼에서 이미 수락한 것으로 봅니다. 별도 Accept 버튼은 두지 않습니다.

사용자에게 보이는 정상 상태:

- 진행 중(Current)
- 잠김(Locked)
- 사용 불가(Unavailable)
- 완료(Completed)

Core의 `Indeterminate`는 내부 진단 상태로 유지합니다. 현재 판정 시스템으로도 끝까지 `Indeterminate`인 Quest는 Application 제품 경계에서 **진행 중(Current)** 으로 보여주되 diagnostic reason은 보존합니다. 확정 가능한 Locked/Unavailable은 승격하지 않습니다.

사용자 조작:

- 완료
- 완료 취소
- 자동 추론할 수 없는 희귀 비재시작형 영구 실패만 실패 처리/취소

Quest reward 전체 모델은 핵심 범위에서 제외합니다.

### 6.1 Quest 필터

Trader filter는 게임식 순서를 사용합니다. Map도 검증된 고정 순서를 사용하며 unknown 값은 알려진 값 뒤에 표시합니다.

Ground Zero와 Ground Zero 21+는 canonical ID를 보존하되 Quest Map filter에서 하나의 `Ground Zero` 그룹으로 표시합니다.

### 6.2 Quest 상세

- `위키` 버튼
- 제출 Item을 문자열 dump가 아니라 card/list로 표시
- Item icon / 이름 / 수량 / `인레이드` 여부 / 유동 제출 후보 여부
- Quest Item 클릭 → Item 상세
- 선행 Quest 클릭 → 해당 Quest 상세
- 이름이 아니라 canonical stable ID로 이동

## 7. Hideout

`CONFIRMED / IMPLEMENTED`

- 미입력 = Lv.0
- 시설별 `- / 현재 레벨 / +`
- 상세는 바로 다음 upgrade 표시
- Needed Items는 현재 레벨보다 높은 모든 미래 upgrade material 합산
- canonical station image 표시

다음 업그레이드의 필요 Item은 문자열 bullet가 아니라 card/list로 표시합니다.

각 재료:

- Item icon
- 이름
- 필요 수량
- `인레이드` 요구 여부

## 8. Needed Items / Item

`CONFIRMED / IMPLEMENTED`

핵심 목적:

> 현재뿐 아니라 앞으로 사용할 가능성이 있는 Item을 미리 모으고, 더 이상 필요하지 않은 실제 보유품만 안전하게 정리하도록 돕습니다.

### 8.1 미래 필요량

포함:

- Current Quest 제출 Item
- 미래에 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 요구
- 현재 Hideout level 이후 모든 미래 upgrade material

제외:

- Completed Quest
- 현재 캐릭터에서 영구 불가임이 증명된 Quest
- 닫힌 branch
- 이미 지난 Hideout upgrade

### 8.2 Inventory / cleanup

- 인레이드 / 일반 보유량은 User Progress 독립 사실
- Game Content update로 삭제/자동 차감하지 않음
- 미래 필요량 충족 후 남는 안전한 초과분만 cleanup 대상으로 계산
- 인레이드 요구를 우선 보호
- 유동 제출 후보는 목표 종료 전 보수적으로 보호
- metadata가 사라져도 stable Item ID 보유 기록 유지
- 안전한 정리량을 증명할 수 없으면 `판단 보류`

### 8.3 Item 목록

기본 list row는 비교에 필요한 네 수량을 직접 보여줍니다.

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

`일반 필요 = 전체 필요 - 인레이드로 반드시 필요한 수량`입니다.

목록 우측의 `+N 필요 / 충분 / 정리 / 판단 보류` status badge는 표시하지 않습니다.

### 8.4 Item 상세

주요 요구량은 단순히:

- 인레이드 필요 N개
- 일반 필요 N개

로 표시합니다.

보유량은 인레이드/일반 각각 `- / 값 / +`를 제공하며 `-` 또는 `+` 클릭마다 즉시 저장합니다. 직접 숫자 입력도 유지하고 명시적 저장으로 반영합니다.

실제 cleanup 경고, 필요 출처, 유동 제출 보호 설명은 필요한 경우 별도 보조 정보로 유지합니다.

### 8.5 Item 종류

분류 권위 데이터는 현재 `json.tarkov.dev` Item category metadata입니다.

상위 표시 그룹:

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

unknown future category는 숨기지 않고 `기타` fallback으로 둡니다.

종류 dropdown은 **현재 view + 검색 + 상태 filter를 통과한 실제 row가 있는 종류만** 보여줍니다. 따라서 기본 `필요` 보기에서 해당 종류의 필요 Item이 모두 사라지면 그 종류도 dropdown에서 사라집니다.

### 8.6 유동 제출

여러 후보 Item ID는 하나의 objective 합계로 계산하며 후보 하나를 임의 선택하지 않습니다.

UI는 `유동 제출 보기`를 별도 제공하고 **Quest별로 그룹화**합니다.

- A Quest 후보와 B Quest 후보는 서로 다른 group/card
- Quest 이름 클릭 → Quest 상세
- 후보 Item 클릭 → Item 상세
- 후보별 보유량을 인레이드/일반로 확인 가능
- 일반 고정 필요/실제 보유에도 관련된 후보는 일반 Item 목록에도 남을 수 있음

### 8.7 Item → Quest

Item 상세의 Quest 필요 출처 또는 유동 제출 Quest를 클릭하면 해당 Quest 상세로 이동합니다. stable ID로 연결합니다.

## 9. Ammo

`CONFIRMED / IMPLEMENTED`

Ammo는 선택 GameMode의 Game Content를 읽는 read-only 비교 화면이며 User Progress와 결합하지 않습니다.

### 9.1 탐색/정렬

- 이름 검색 없음
- 구경 dropdown 중심
- 항상 penetration 오름차순 → damage 오름차순 → name
- header sort로 이 기준을 깨지 않음

구경 표시명은 raw 식별자를 기계적으로 mm화하지 않고 Tarkov에서 익숙한 cartridge 표현을 사용합니다.

예:

- `.45 ACP`
- `.357 Magnum`
- `.300 Blackout`
- `.338 Lapua Magnum`
- `.50 AE`
- `.366 TKM`
- `12/70`

raw caliber ID는 canonical data로 그대로 보존합니다.

### 9.2 비교 대상

healthy Wiki Ballistics enrichment가 존재할 때는 현재 Ballistics 표와 안전하게 unique-match된 Ammo만 표와 구경 dropdown에 포함합니다.

따라서 Wiki 표에 없는 장난/미사용/비교 대상 외 탄약 및 그 탄약만 가진 구경은 정상 비교 화면에서 제외됩니다.

Wiki source 장애 시 raw Game Content를 파괴하지 않고 기본 Ammo를 임시 표시하며 source 상태를 명시합니다.

### 9.3 표 열과 상세

`표시 열` 메뉴에서 속성을 선택/해제합니다. 숨긴 속성도 상세에는 계속 표시합니다.

표에는 최소 수급 경로를 표시하고, 상인 구매/교환/제작 경로가 모두 없을 때만 `레이드 획득`을 표시합니다. 상세에는 전체 acquisition 정보를 유지합니다.

### 9.4 Armor Class 1~6 effectiveness

- Tarkov Wiki Ballistics의 명시된 0~6 값만 사용
- 자체 penetration/class heuristic 금지
- 모호함/충돌/미매칭은 추측하지 않음
- schema 이상/비정상 match coverage는 enrichment 미적용

UI는 여섯 칸의 **위치 자체가 Class 1→6**임을 사용합니다.

각 cell 안에는 effectiveness 숫자만 표시합니다.

```text
6  6  6  5  3  2
```

작은 `1,2,3,4,5,6` armor class 숫자를 cell 안에 중복 표시하지 않습니다. Tooltip에서는 armor class를 설명할 수 있습니다.

## 10. UI / 이미지

`CONFIRMED / IMPLEMENTED`

- dark background / 밝은 본문 / 기존 accent
- white native ComboBox popup 방지
- list row 전체 폭 정렬
- 장식보다 비교성과 읽기 쉬운 정보 구조 우선

### 10.1 ScrollBar

- vertical/horizontal 전체 WPF ControlTemplate 사용
- native arrow chrome 없음
- normal track + thumb 형태
- vertical ScrollBar는 viewport 세로 영역을 채우고 폭만 고정
- horizontal ScrollBar는 viewport 가로 영역을 채우고 높이만 고정
- 작은 공처럼 보이는 고정 14×14 ScrollBar 금지

### 10.2 이미지 cache

Item / Hideout / Ammo / Quest Item 이미지는 canonical URL을 사용합니다.

```text
canonical URL
→ bytes download
→ SkiaSharp decode
→ 크기/유효성 검증
→ PNG normalize
→ %LocalAppData%/JunhyunHelper/image-cache
→ WPF
```

이미지 실패는 Game Content/User Progress 실패가 아닙니다.

## 11. Map / Scanner

`CONFIRMED / PLACEHOLDER IMPLEMENTED`

상단에 `지도`, `스캐너` 탭이 존재하며 현재 실제 기능은 `준비 중` placeholder입니다. 검증되지 않은 기능은 뒤에서 실행하지 않습니다.

## 12. 현재 범위 밖

- Quest reward 전체 모델
- runtime AI/GPT
- 검증되지 않은 Map 실제 기능
- Scanner 실제 기능 — 요구사항 확정 후 구현
- 기존 `Propeex/Tarkov-Helper` 동작을 존재한다는 이유만으로 승계하는 기능

## 13. 실사용 피드백 상태

### 첫 실사용 피드백

`IMPLEMENTED / MERGED`

초기 dark UI, 이미지, 정렬, +/- 진행 입력, Hideout Lv.0, Item 판단 목록, Ammo 비교, update progress, Profile 정리, Armor effectiveness, Map/Trader order, Ground Zero grouping, Map/Scanner placeholder까지 반영했습니다.

### 2차 실사용 피드백

`IMPLEMENTED / MERGED — PR #36`

- WebP 포함 icon decode 안정화
- residual Indeterminate → Current
- ScrollBar template 1차 수정
- 유동 제출 별도 view
- Tarkov category 기반 Item 분류
- Quest Item card/icon
- Quest ↔ Item / prerequisite navigation

상세: `docs/SECOND_USABILITY_PASS.md`

### 3차 실사용 피드백

`IMPLEMENTED / WINDOWS CI VERIFIED — PR #37`

- ScrollBar 정상 track 크기/형태
- Quest별 유동 제출 그룹
- Hideout 재료 card/list
- conventional Ammo caliber label
- healthy Wiki Ballistics membership 기반 Ammo 비교 대상 제한
- 사용자 표시 `FIR` → `인레이드`
- Needed Items 네 수량 column + 간결한 상세 + +/- 즉시 저장 + dynamic category dropdown
- Quest `위키`
- Fence 상단 분리 + 핵심/특별 Trader 구조
- Armor effectiveness cell 내부 class 숫자 제거

상세: `docs/THIRD_USABILITY_PASS.md`
