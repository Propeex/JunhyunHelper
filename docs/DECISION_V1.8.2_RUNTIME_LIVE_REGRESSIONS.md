# Decision — v1.8.2 Runtime UI / Live Game Content Regression Repair

기준일: 2026-08-28 KST

## 배경

v1.8.1 공개 이후 두 종류의 실사용 회귀가 확인되었다.

1. v1.7.15에서 구현한 Ammo 구경/즐겨찾기 드롭다운 아이콘 UI가 소스/테스트에는 존재하지만 published executable에서 실제 초기화되지 않을 수 있었다.
2. 현재 json.tarkov.dev live relationship payload가 기존 canonical 관계 계약과 충돌했다.

이 수정의 목표는 기존 제품 의미와 fail-closed/LKG 계약을 약화하지 않고, 실제 공개 실행 파일과 현재 live source 모두에서 계약을 만족시키는 것이다.

## 1. Ammo published runtime UI

### 원인

`AmmoPage`의 Loaded class handler 등록이 static-field side effect에만 의존했다. 명시적인 type initializer가 없으면 CLR이 해당 타입을 `beforefieldinit`로 취급할 수 있고, 실제 `AmmoPage` 인스턴스 생성 전에 side effect가 실행된다는 보장이 없다.

그 결과 published executable에서는 runtime polish가 적용되지 않고 text-only XAML fallback이 남을 수 있었다.

### 결정

- `AmmoPage`에 명시적인 static constructor를 두어 class-handler 등록을 인스턴스 생성 전에 결정적으로 실행한다.
- runtime polish 후 다음 계약을 즉시 검증한다.
  - 구경 ComboBox에 runtime icon template이 설치되어 있음
  - 즐겨찾기 ComboBox가 실제 생성되어 있음
  - 두 selector가 동일한 icon template을 공유함
  - legacy favorite menu는 숨겨지고 hit-test 불가 상태임
- published executable smoke는 단순 초기화 marker가 아니라 실제 rendered `Image`와 `Image.Source`, geometry, shared timer-cycle까지 검증한다.

구경/즐겨찾기 필터 의미와 아이콘 순환 규칙 자체는 변경하지 않는다.

## 2. json.tarkov.dev passive Bitcoin production

### 관찰된 upstream shape

현재 live crafts에는 Bitcoin Farm 생산 항목이 일반 craft endpoint에 포함되지만 `requiredItems`가 비어 있다. 이 항목은 재료를 소비하는 일반 recipe가 아니라 GPU/station state에 의해 진행되는 passive hideout production이다.

- craft id: `5d5c205bd582a50d042a3c0e`
- station id: `5d494a445b56502f18c98a10`
- result item id: `59faff1d86f7746c51718c9c`

이를 정상적인 zero-cost craft로 canonicalize하면 Scanner에 잘못된 제작 수급 관계가 생성된다.

### 결정

위 세 identity가 모두 일치하는 audited passive Bitcoin production만 relationship import에서 제외한다.

다른 empty-required craft는 계속 fail closed한다. 즉, 일반적인 schema/source loss를 허용하기 위해 validator를 완화하지 않는다.

## 3. duplicate trader purchase offers

### 관찰된 upstream shape

현재 live items의 `buyFromTrader`에 canonical model이 표현하는 모든 필드가 동일한 offer가 동일 item 아래 두세 번 반복될 수 있다.

이를 그대로 보존하면 실제로는 하나인 구매 경로를 여러 개로 만들어 canonical uniqueness validation을 실패시킨다.

### 결정

canonical model 기준으로 완전히 동일한 direct-purchase record만 importer에서 deduplicate한다.

다음 중 하나라도 다르면 별도 offer로 유지한다.

- item
- trader
- loyalty level
- quest unlock
- price
- currency item
- buy limit / reset-time 등 canonical record의 나머지 의미 필드

따라서 upstream의 실질적으로 다른 구매 조건을 합치지 않는다.

## 4. 변경하지 않는 계약

- Game Content candidate/LKG 분리
- relationship reference/price/count/limit integrity validation
- relation 및 material-edge 50% completeness floor
- critical relationship collection empty fail-closed
- candidate read-back / activation / active recovery validation
- v3~v7 legacy relationship-null compatibility
- Scanner OCR threshold, candidate cap, matcher, visual recovery acceptance
- Scanner recognition identity proof
- Map/MiniMap donor revision 및 ownership boundary

## 5. Release gate

v1.8.2는 다음을 모두 통과한 exact main source만 공개한다.

1. full Release test suite
2. Windows x64 publish/package validation
3. published executable startup + rendered Product UI + Main Map/Factory/MiniMap smoke
4. Ammo 구경/즐겨찾기 실제 rendered icon + shared timer-cycle smoke
5. current Regular/PvE live data probe
6. exact-main CI success
7. public tag/release/assets/checksum readback

임시 diagnostic workflow는 원인 확인 후 제품 branch에서 제거한다.
