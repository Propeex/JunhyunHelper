# DECISION — v1.14.0 Farming Guide recursive assembly and validated storage layouts

Status: **CONFIRMED / RELEASE TARGET / NOT YET PUBLIC VERIFIED**  
Date: **2026-09-01 KST**  
Target: **v1.14.0**

## 1. 문제

v1.13.3은 Farming Guide를 실제 drag/drop inventory surface로 전환했지만 다음 한계가 남았다.

- 총기/장비 부착물 편집이 root item의 직접 slot 중심이었다.
- 빈 slot에서 호환 부품을 찾아 장착하려면 오른쪽 검색 결과를 이용해야 했다.
- 조립 상태가 변해도 item presentation이 현재 build를 충분히 구분하지 못했다.
- multi-grid carrier의 generic 배치는 실제 Tarkov에서 보이는 상대 grid 배치와 다를 수 있었다.
- current data에서 storage mechanics와 UI layout identity/coordinates의 authority가 서로 다른데 이를 하나의 사실처럼 취급하면 stale visual metadata가 잘못된 수납 구조로 보일 위험이 있었다.
- 현재 제품에서 사용자가 직접 장착할 수 없는 PMC dogtag surface가 장비 보드에 남아 있었다.

## 2. 확정 제품 동작

### 2.1 Dogtag 장비 보드 surface

PMC dogtag는 raid-start equipment board에서 제거한다.

- 과거 Farming Guide state의 dogtag 값은 schema-v1 backward compatibility를 위해 deserialize할 수 있다.
- current product state에서는 legacy dogtag equipment value를 정상 장비로 유지하지 않는다.
- 이 변경만으로 Farming Guide user-state schema를 올리지 않는다.

### 2.2 Recursive assembly tree

조립 상태는 `FarmingGuideItemState`의 attachment/armor child tree로 본다.

```text
weapon / helmet / armor
└─ attachment slot
   └─ installed item
      └─ child attachment slot
         └─ installed item
            └─ ...
```

Core의 `FarmingGuideAssemblyPolicy`가 다음을 단일 authority로 담당한다.

- deep node lookup/mutation
- attachment filter validation
- armor plate allowed-item validation
- item conflict validation
- 전체 assembly tree에 대한 candidate conflict validation
- required-slot recursion
- deterministic assembly signature
- persisted tree recursive sanitization
- abnormal/unbounded tree에 대한 depth guard

WPF event handler가 별도의 compatibility 규칙을 재구현하지 않는다.

### 2.3 Workbench interaction

Workbench는 별도 Windows/OS dialog를 만들지 않고 Farming Guide 내부에 유지한다.

- 설치된 attachment를 더블클릭하면 해당 child item의 실제 하위 slot surface로 들어간다.
- 상위 부품으로 돌아갈 수 있다.
- 빈 attachment/replaceable armor slot을 single-click하면 같은 페이지 안에서 compatible item picker를 연다.
- picker는 item icon card를 표시한다.
- candidate를 single-click하면 해당 slot에 즉시 설치한다.
- 기존 search result drag → slot drop도 유지한다.
- picker selection과 drag/drop은 동일한 `FarmingGuideAssemblyPolicy` compatibility 결과를 사용한다.
- occupied one-item slot을 새 item으로 silent overwrite하지 않는다.

### 2.4 Assembly-aware presentation

현재 build가 authoritative imported default preset의 contained-item membership과 정확히 일치하고 usable composed image URL이 있으면 해당 preset image를 사용한다.

그 외 arbitrary build는 외부 renderer를 필수 의존하지 않는다.

```text
base item image
+ deterministic installed-part indicators
```

으로 현재 상태가 base item과 다름을 표현한다.

이 fallback은 실제 Tarkov client의 완전한 조립 렌더 결과라고 주장하지 않는다.

## 3. Storage layout authority 분리

Storage의 **mechanics**와 **visual arrangement**를 분리한다.

### 3.1 Mechanics authority

current validated Game Content가 권위다.

- grid count
- grid width/height
- allowed/excluded item/category filters
- item dimensions
- actual placement legality

오래된 UI 좌표 metadata가 current mechanics를 바꿀 수 없다.

### 3.2 Visual arrangement metadata

실제 Tarkov multi-grid 상대 배치를 표현하려면 별도의 검증된 visual metadata를 사용할 수 있다.

Importer는 upstream/raw source에서 존재하는 경우 다음 layout identity를 보존한다.

- `GridLayoutName`
- `gridLayoutName`
- `RigLayoutName`
- `rigLayoutName`

`StorageLayoutName`은 presentation resolver가 사용할 identity이며 storage legality 자체의 authority가 아니다.

### 3.3 Exact 적용 조건

`FarmingGuideStorageVisualLayoutResolver`는 exact metadata를 사용하기 전에 current live grid signature와 비교한다.

```text
expected exact metadata
  grid count + each grid width/height
        │
        ├─ current grids와 정확히 일치 → exact relative placement 사용
        └─ 불일치 / unknown           → compact fallback
```

따라서 Tarkov 업데이트로 carrier 구조가 바뀌었는데 stale coordinates만 남는 경우에도 잘못된 exact layout을 강제하지 않는다.

### 3.4 Fallback 의미

exact metadata가 없는 carrier는 finite deterministic compact layout을 사용한다.

이 fallback은:

- 무한 가로 나열을 피한다.
- 실제 grid mechanics와 drop target identity를 보존한다.
- authentic Tarkov layout이라고 표시하거나 문서화하지 않는다.

현재 product-owned exact catalog는 검증된 최소 범위만 사용한다. provenance/license/coverage가 확인되지 않은 외부 atlas를 광범위하게 복사해 제품 데이터로 포함하지 않는다.

## 4. Content schema

v1.14.0에서 Game Content snapshot은 assembly source 및 layout identity를 보존해야 하므로:

```text
write: v10
read:  v3, v4, v5, v6, v7, v8, v9, v10
```

으로 한다.

Farming Guide user state는 기존 tree/state shape로 표현 가능하므로 schema v1을 유지한다.

## 5. 실패 안전성

다음은 fail closed한다.

- current item catalog에 없는 assembly child
- 현재 slot/filter가 허용하지 않는 child
- current assembly 내 conflict
- locked/unallowed armor plate
- excessive recursive depth
- exact visual metadata와 current grid signature 불일치

사용 편의를 이유로 incompatible item, stale layout 또는 silent item loss를 허용하지 않는다.

## 6. 검증 계약

v1.14.0 release candidate는 최소 다음을 만족해야 한다.

- recursive assembly deterministic tests
- importer layout identity tests
- storage visual resolver tests
- existing Farming Guide persistence/loadout regressions
- Windows Release build
- self-contained win-x64 publish
- actual published EXE Product UI/Farming Guide smoke
- exact multi-grid Canvas placement smoke
- exact layout의 `GridDropTarget.GridIndex` identity smoke
- graceful shutdown
- Shutdown Race
- Documentation Consistency
- main 병합 후 exact-main CI
- public v1.14.0 tag/source/assets/checksum 검증

## 7. 현재 검증 상태

Release identity bump 직전 exact PR head:

```text
7b9a96ccdff0ff1e0ddfb6f676624d24b150b7a1
527 passed / 0 failed / 0 skipped
Windows Release build: SUCCESS
self-contained win-x64 publish: SUCCESS
published EXE Product UI/Farming Guide/Map smoke: SUCCESS
graceful shutdown: SUCCESS
Shutdown Race: SUCCESS
Documentation Consistency: SUCCESS
```

이 값은 **branch validation evidence**이며 공개 v1.14.0 release identity가 아니다. 공개 source/tag/assets는 main 병합과 Release workflow 완료 후 별도로 기록한다.
