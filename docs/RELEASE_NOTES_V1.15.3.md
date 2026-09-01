# 준현 헬퍼 v1.15.3

## 파밍 가이드 수납·테스트 보완

v1.15.3은 v1.15.2의 완제품 장비 모델은 유지하면서, 실제 Tarkov 인벤토리에서 사용하는 특수 컨테이너 수납과 파밍 가이드 테스트 입력을 보완하는 PATCH 릴리즈입니다.

### 일반 보관 아이템의 노란색 테두리를 제거했습니다

기존에는 보관 중인 모든 아이템이 accent/노란색 테두리를 사용해 `F` 잠금 여부를 한눈에 구분하기 어려웠습니다.

v1.15.3부터:

- 일반 stored item: 기본 neutral border
- `F`로 잠근 stored item: accent/노란색 border
- 잠금 해제: 즉시 neutral border로 복귀
- 빈 칸 예약/잠금: 기존 accent reservation 표시 유지

### Key tool 같은 특수 컨테이너 내부 수납을 지원합니다

v1.15.2는 장비 내부 부품 편집을 제거하면서 nested storage 상세도 저장된 Backpack/Rig로 제한했습니다. 이 제한은 실제 Tarkov storage semantics보다 좁았습니다.

v1.15.3은 **현재 검증된 Game Content에 실제 storage grid가 존재하는 모든 저장 아이템**의 nested storage를 유지합니다.

특정 아이템 이름을 코드에 하드코딩하지 않습니다. 따라서 Key tool, 문서·돈·카드·주사기류 전용 컨테이너 및 향후 Tarkov가 추가하는 저장 아이템도 source data가 제공하는 범위에서 같은 방식으로 동작합니다.

### 컨테이너별 허용 아이템 관계를 Tarkov 데이터 그대로 따릅니다

각 내부 grid가 가진 다음 source-backed 정보를 그대로 사용합니다.

- grid width / height
- allowed category IDs
- allowed item IDs
- excluded category IDs
- excluded item IDs

따라서 예를 들어 열쇠용 컨테이너에는 current Tarkov data가 허용한 열쇠 계열만 들어가며, 관계를 JunhyunHelper가 이름이나 추측으로 재정의하지 않습니다.

### 보안 컨테이너 안 컨테이너도 재귀적으로 처리합니다

기존 `ParentInstanceId` nested-storage 모델을 그대로 일반화했습니다.

```text
보안 컨테이너
└─ Key tool / 기타 특수 컨테이너
   └─ 해당 컨테이너가 허용하는 아이템
```

컨테이너 안 컨테이너 역시 source grid/filter가 허용하는 한 같은 방식으로 관리합니다. orphan, cycle, overlap, bounds/filter 위반은 기존과 동일하게 fail closed합니다.

### 전용 컨테이너가 맞는 아이템은 그 내부를 우선 사용합니다

전용 nested grid에 positive allow-list가 있고 스캔된 아이템이 그 필터에 실제로 허용된다면, 파밍 가이드는 일반 보안 컨테이너·주머니·리그·가방의 빈칸보다 해당 전용 컨테이너 내부를 먼저 검사합니다.

예를 들어 보안 컨테이너 안에 Key tool과 일반 빈칸이 동시에 있고 현재 Tarkov 데이터상 그 열쇠가 Key tool에 들어갈 수 있다면, 열쇠 스캔 시 Key tool 내부 보관을 먼저 지시합니다.

이 우선순위 역시 `Key tool`이라는 이름을 하드코딩해서 판단하지 않습니다. source grid의 `AllowedItemIds` / `AllowedCategoryIds`가 해당 아이템을 받아들이는 경우에만 전용 storage로 취급합니다. unrestricted 가방류는 기존 일반 수납 순서를 유지합니다.

### 검색 결과 + T 테스트 스캔을 수정했습니다

검색창에서 아이템을 검색한 뒤 결과 위에 마우스를 올리고 `T`를 누르는 simulated scan이 검색 TextBox의 keyboard focus 때문에 실행되지 않던 회귀를 수정했습니다.

- 검색 결과가 hover된 상태: `T`가 simulated scan command로 우선 처리
- 검색 결과가 hover되지 않은 상태: `T`는 정상 검색 문자 입력
- active raid session에서는 실제 Scanner Item ID와 동일한 Farming Guide recommendation path 사용
- Scanner capture mode가 꺼져 있어도 동작 가능
- 앱 재시작 후 Scanner catalog가 아직 메모리에 없으면 verified same-mode local cache를 on-demand load
- snapshot 준비 실패 시 조용히 무시하지 않고 테스트 실패 상태 표시

### 장비 완제품 모델은 유지합니다

이번 변경은 storage mechanics 보완이며 장비 조립 편집을 되살리는 변경이 아닙니다.

계속 제공하지 않는 기능:

- 총기 attachment/mod 편집
- 헬멧 attachment 편집
- 방탄판 편집
- equipment-internal Equip / ReplaceEquip 파밍 지시

무기·헬멧·방탄복 등은 v1.15.2와 동일하게 opaque complete item으로 취급합니다.

## 호환성

- Desktop: v1.15.3
- Game Content schema: v10 유지
- Farming Guide state schema: v2 유지
- Scanner display settings schema: v10 유지
- Scanner catalog schema: v4 유지
- 기존 프리셋/top-level 장비/nested placement/lock state를 그대로 읽음
- source가 실제 storage grid를 제공하는 기존 저장 아이템은 별도 schema migration 없이 기존 `ParentInstanceId` 모델로 내부 공간을 사용할 수 있음

## 검증 범위

- arbitrary source-backed storage surface runtime projection
- Secure Container 안 specialized container + allowed/denied filter sanitizer
- dedicated nested storage > general root storage raid-placement priority
- neutral ↔ locked accent stored-item border contract
- hover + `T` simulated scan routing and on-demand Scanner catalog path
- v1.15.2 complete-equipment boundary preservation
- full deterministic tests / Windows Release build / published EXE product smoke / graceful shutdown / Shutdown Race / package audit / PR & exact-main CI / public release asset verification
