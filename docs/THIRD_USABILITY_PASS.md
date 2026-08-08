# THIRD USABILITY PASS — 3차 실사용 피드백

기록일: **2026-08-09**

상태: `IMPLEMENTED / WINDOWS CI VERIFIED`

2차 테스트 빌드 실사용 후 확정된 UI·정보 구조 개선 요구사항과 현재 구현을 기록합니다.

## 1. ScrollBar

기존 문제는 공처럼 보이는 thumb 자체보다 **모든 ScrollBar에 Width와 Height를 동시에 고정해 vertical ScrollBar 전체 높이까지 작아진 것**이 핵심이었습니다.

현재 구현:

- 세로 ScrollBar는 scrollable viewport 우측 높이를 정상적으로 채움
- 가로 ScrollBar는 하단 너비를 정상적으로 채움
- 일반적인 track + thumb 구조
- 둥근 dark theme 유지
- native arrow chrome 없음
- vertical은 폭만 고정하고 높이는 stretch
- horizontal은 높이만 고정하고 폭은 stretch

## 2. 유동 제출 그룹화

유동 제출 후보를 하나의 긴 목록으로 이어 붙이지 않습니다.

- QuestId별 하나의 시각적 그룹/card
- A Quest와 B Quest의 후보는 서로 분리
- 같은 Quest 안의 여러 flexible objective는 같은 Quest 그룹 안에서 진행 정보를 표시
- 그룹의 Quest 이름 클릭 → Quest 상세
- 후보 Item 클릭 → Item 상세
- 계산 및 cleanup 보호 원칙은 변경하지 않음

## 3. Hideout 필요 Item 목록화

Hideout 다음 업그레이드 재료를 문자열 bullet로 표시하지 않습니다.

각 재료를 card/row로 표시합니다.

- Item icon
- 이름
- 필요 수량
- `인레이드` 요구 badge

이미지는 기존 canonical Item URL + local image cache를 사용합니다.

## 4. Ammo 구경 표기

내부 raw caliber 식별자를 숫자 기반 mm 표현으로 그대로 보여주지 않습니다.

사용자가 Tarkov에서 익숙한 cartridge 이름을 표시합니다.

대표 예:

- `Caliber1143x23ACP` → `.45 ACP`
- `Caliber762x35` → `.300 Blackout`
- `Caliber9x33R` → `.357 Magnum`
- `Caliber86x70` → `.338 Lapua Magnum`
- `Caliber127x33` → `.50 AE`
- `Caliber12g` → `12/70`
- `Caliber366TKM` → `.366 TKM`

canonical raw caliber 값은 변경하지 않고 Desktop 표시명만 정규화합니다.

## 5. Wiki Ballistics에 없는 Ammo 제외

Ammo 비교 화면은 **현재 Wiki Ballistics 표와 안전하게 매칭된 탄약만** 정상 비교 대상으로 표시합니다.

기존 Wiki enrichment는 이미 다음을 검증합니다.

- current Ballistics table parser
- canonical 영문 Ammo 이름의 unique match
- conflicting row 제외
- 비정상적으로 낮은 전체 match coverage 감지

따라서 healthy Wiki enrichment가 존재하면 `ArmorEffectiveness`가 유효하게 매칭된 탄약만 Ammo 표에 포함합니다. 그 결과 Wiki 표에 없는 장난/미사용/비교 대상 외 탄약과 그 탄약만 가진 구경도 표/구경 dropdown에서 제외됩니다.

영구 hard-coded 탄약 allowlist는 만들지 않습니다.

Wiki source가 unavailable 또는 schema 이상으로 판단되어 enrichment 자체가 적용되지 못한 경우에는 **기본 Game Content를 삭제하거나 Ammo 화면을 빈 화면으로 만들지 않고** raw Ammo를 임시 표시하며 화면에 Wiki 목록 확인 불가 상태를 명시합니다. 외부 보조 원천 장애가 마지막 정상 Game Content나 User Progress를 손상시키지 않는 기존 안전 원칙을 유지합니다.

## 6. 사용자 표시 용어: FIR → 인레이드

내부 데이터 모델/DB의 `Fir` 식별자는 저장 호환성을 위해 유지합니다.

사용자에게 보이는 관련 표현은 다음으로 통일합니다.

- `FIR` → `인레이드`
- `Non-FIR` 의미 → `일반`

적용 대상에는 Quest 제출 Item, Hideout 재료, Needed Items, flexible hand-in 진행 표시가 포함됩니다.

## 7. Needed Items 정보 구조

### 목록

필요와 보유를 각각 인레이드/일반으로 분리해 네 값을 바로 비교합니다.

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

여기서 `일반 필요`는 `전체 필요 - 인레이드로 반드시 필요한 수량`인 unrestricted 요구량입니다.

기존 우측의 `+N 필요 / 충분 / 정리 / 판단 보류` status badge는 제거했습니다.

### 상세

상세의 주 정보는 다음 두 값으로 단순화했습니다.

- 인레이드 필요 N개
- 일반 필요 N개

`미래 필요`, `추가 필요` 같은 장문 상태 문장을 주 정보로 사용하지 않습니다. 실제 안전한 초과분이 있을 때의 cleanup 경고와 유동 제출 보호 설명은 별도 보조 정보로 유지합니다.

### 보유량 입력

인레이드/일반 각각:

```text
− / 값 / +
```

- `-` 또는 `+`를 누를 때마다 즉시 Inventory User Progress 저장 요청
- 0 미만으로 내려가지 않음
- 직접 숫자 입력도 유지
- 직접 입력은 `직접 입력 저장`으로 명시적으로 저장

### 종류 dropdown

종류 dropdown은 **현재 view + 검색 + 상태 filter를 통과한 실제 Item 종류만** 보여줍니다.

따라서 기본 `필요` 보기에서 더 이상 필요한 Item이 없는 종류는 dropdown에서 사라집니다. `전체` 보기에서는 실제 보유/참고 row가 있으면 다시 나타날 수 있습니다.

## 8. Quest 용어

Quest 상세의 `Wiki` 버튼은 `위키`로 표시합니다.

## 9. Profile 상인 진행 상태

Profile 편집 화면을 다음처럼 재구성했습니다.

### 상단 주요 진행값

- Player level
- Prestige
- **펜스 우호도** — 0.1 단위

Fence는 일반 LL 목록에서 제거하고 독립된 주요 진행값으로 배치합니다.

### 핵심 상인

공유 `UiReferenceOrder`의 게임식 순서를 재사용합니다.

Fence를 제외한 핵심 목록:

```text
Prapor → Therapist → Skier → Peacekeeper → Mechanic
→ Ragman → Jaeger → Ref
```

### 특별

일반 핵심 Trader 탭 밖의 상인은 별도 `특별` 섹션에 둡니다.

현재 알려진 순서:

```text
Lightkeeper → BTR Driver → future unknown traders
```

Quest 판정에 실제 standing이 필요한 비-Fence 상인의 고급 입력은 기존처럼 별도 advanced 영역에 유지합니다.

## 10. Ammo 방탄 효율 cell

방탄 1~6클은 여섯 칸의 **왼쪽→오른쪽 위치**로 구분합니다.

각 cell 안에는 효율값만 표시합니다.

```text
6  6  6  5  3  2
```

효율값 위/아래에 작은 `1, 2, 3, 4, 5, 6` 클래스 숫자를 중복 표시하지 않습니다.

Tooltip에는 해당 cell의 armor class와 값의 의미를 계속 설명할 수 있습니다.

## 검증

코드 checkpoint `3bb437d7e04fb9fc453c6da00ba5ee756b5f7f48`:

- Windows Release Desktop build 성공
- 전체 automated tests 성공
- Windows x64 publish 성공
- ZIP/artifact 생성 성공
- GitHub Actions run `31271990036` 성공

PR 병합 전 문서 변경까지 포함한 최종 CI를 한 번 더 확인합니다.

## 변경하지 않은 핵심 원칙

- 일반 Game Content 업데이트에 GPT 불필요
- Game Content와 `user.db` 분리
- raw Ammo stats의 1차 원천은 계속 `json.tarkov.dev`
- Wiki Ballistics는 Ammo 비교 범위/명시 effectiveness를 위한 보조 원천이며 raw stats를 대체하지 않음
- 유동 제출 계산에서 후보 하나를 임의 선택하지 않음
- 안전한 cleanup을 증명할 수 없으면 보호
- canonical stable ID 기반 Quest ↔ Item 관계 유지
