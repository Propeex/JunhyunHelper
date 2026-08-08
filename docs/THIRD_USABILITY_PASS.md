# THIRD USABILITY PASS — 3차 실사용 피드백

기록일: **2026-08-09**

상태: `CONFIRMED / IMPLEMENTATION IN PROGRESS`

이번 문서는 2차 테스트 빌드 실사용 후 확정된 UI·정보 구조 개선 요구사항을 기록합니다.

## 1. ScrollBar

현재 ScrollBar가 세로 영역 전체를 차지하지 않고 작은 공처럼 보이는 문제를 수정합니다.

- 세로 ScrollBar는 scrollable viewport의 우측 높이를 정상적으로 채웁니다.
- 가로 ScrollBar는 하단 너비를 정상적으로 채웁니다.
- 둥근 모서리는 유지하되 일반적인 track + thumb 형태를 사용합니다.
- ScrollBar 전체 크기를 14×14로 고정하지 않습니다.

## 2. 유동 제출 그룹화

유동 제출 후보를 단순히 한 목록으로 이어 붙이지 않습니다.

- Quest별로 하나의 그룹을 만듭니다.
- A Quest 후보와 B Quest 후보는 별도 카드/그룹으로 구분합니다.
- 그룹 안에서 후보 Item을 선택해 Item 상세로 이동할 수 있습니다.
- 계산 방식과 cleanup 보호 정책은 변경하지 않습니다.

## 3. Hideout 필요 Item 목록화

Hideout 다음 업그레이드 재료를 문자열 bullet로 표시하지 않습니다.

각 재료를 개별 row/card로 표시합니다.

- Item icon
- 이름
- 필요 수량
- 인레이드 요구 여부

## 4. Ammo 구경 표기

내부 raw caliber 식별자를 임의로 mm 표기로 치환하지 않습니다.

사용자가 실제 Tarkov에서 익숙한 전통적인 구경명을 표시합니다.

예:

- `.45 ACP`
- `.300 Blackout`
- `.366 TKM`
- `12/70`

가능하면 현재 Wiki Ballistics의 구경 label을 self-updating 표시 메타데이터로 사용하고, source가 없을 때만 검증된 fallback mapping을 사용합니다.

## 5. Wiki Ballistics에 없는 Ammo 제외

Ammo 비교 화면은 현재 Tarkov Wiki Ballistics 표에 등록된 탄약만 대상으로 합니다.

- Wiki 표에 존재하지 않는 장난/미사용/비교 대상 외 탄약과 그로 인해 생기는 구경은 정상적인 healthy source 상태에서 표에서 제외합니다.
- 영구 hard-coded allowlist를 만들지 않습니다.
- 현재 Wiki table membership을 update 시점에 다시 해석합니다.
- Wiki source가 unavailable/비정상일 때는 마지막 정상 Game Content를 망가뜨리거나 임의 판정을 만들지 않습니다.

## 6. 사용자 표시 용어: FIR → 인레이드

내부 데이터 모델/DB의 `Fir` 식별자는 호환성을 위해 유지합니다.

사용자에게 보이는 모든 관련 표현만 다음처럼 바꿉니다.

- `FIR` → `인레이드`
- `Non-FIR` → `일반`

## 7. Needed Items 정보 구조

### 목록

필요와 보유를 각각 인레이드/일반으로 분리해 총 네 값을 바로 비교할 수 있게 합니다.

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

현재 우측의 `추가 필요`, `충분`, `정리 필요` 등 상태 badge는 목록에서 제거해 정보 밀도를 줄입니다.

### 상세

`미래 필요`, `추가 필요` 같은 문장을 중심으로 표시하지 않고 단순한 요구량을 우선합니다.

- 인레이드 필요 N개
- 일반 필요 N개

cleanup 관련 경고가 실제로 필요한 경우에는 별도 보조 정보로 유지할 수 있습니다.

### 보유량 입력

- 직접 숫자 입력 경로는 유지할 수 있습니다.
- 인레이드/일반 각각 `- / 값 / +` 조작을 제공합니다.
- `-` 또는 `+`를 누를 때마다 즉시 User Progress에 저장합니다.
- 별도 저장 버튼을 누르지 않아도 +/- 변경은 저장됩니다.

### 종류 dropdown

현재 view/filter에서 실제로 보이는 Item이 없는 종류는 종류 dropdown에서 숨깁니다.

따라서 완료되어 더 이상 필요 Item이 없는 종류는 `필요` 보기에서 나타나지 않습니다.

## 8. Quest 용어

Quest 상세의 `Wiki` 버튼을 `위키`로 표시합니다.

## 9. Profile 상인 진행 상태

Profile 편집 화면의 상인 진행 상태를 게임 사용 흐름에 맞게 나눕니다.

- Fence 우호도는 Player level/Prestige와 비슷한 최상단 주요 진행값으로 별도 배치합니다.
- 핵심 상인은 인게임 Trader 탭에서 익숙한 순서로 나열합니다.
- Lightkeeper, BTR Driver 등 핵심 Trader 탭 밖의 상인은 `특별` 섹션에 분리합니다.
- 내부 Trader ID와 Quest 판정 데이터는 변경하지 않습니다.

## 10. Ammo 방탄 효율 cell

방탄 1~6클은 여섯 칸의 **왼쪽→오른쪽 위치**로 이미 구분됩니다.

따라서 각 cell에는 효율값만 표시합니다.

예:

```text
6  6  6  5  3  2
```

현재처럼 효율값 위에 작은 `1`, `2`, `3`, `4`, `5`, `6`을 함께 표시하지 않습니다.

Tooltip에서는 필요하면 해당 cell이 몇 클래스인지 설명할 수 있지만, 표와 상세 cell 내부에는 클래스 숫자를 중복 표시하지 않습니다.

## 검증 기준

- Windows Release Desktop build
- 전체 automated tests
- Windows x64 publish
- vertical/horizontal ScrollBar 실제 layout
- Quest별 flexible group 구분
- Hideout material card 표시
- `.45 ACP` 등 구경 label 회귀 테스트
- healthy Wiki membership 기반 Ammo 필터
- Wiki 장애 시 기존 정상 데이터/기능 보존
- 사용자 화면의 FIR 잔존 표현 제거
- Item 네 수량 column 계산 정확성
- +/- 한 번마다 inventory persistence
- dynamic category dropdown
- trader 섹션 순서/분리
- armor effectiveness cell에 rating 숫자만 표시
