# DECISION — v1.14.0 Farming Guide recursive assembly and validated storage layouts

Status: **CONFIRMED / PUBLIC HISTORICAL — DIMENSION GUARD CORRECTED BY v1.14.1**  
Date: **2026-09-01 KST**  
Release: **v1.14.0**

## 1. 목적

v1.14.0은 v1.13.3 Farming Guide의 실제 drag/drop inventory interaction을 다음 영역으로 확장했다.

- 총기/장비 attachment 편집을 root direct slot에서 recursive child tree로 확장
- 빈 slot에서 compatible item을 같은 page에서 선택/즉시 장착
- assembly state를 시각적으로 구분
- multi-grid carrier의 generic 배치 대신 검증된 visual metadata를 사용할 수 있는 경계 도입
- 사용자에게 직접 actionable하지 않은 PMC dogtag equipment-board surface 제거

## 2. Recursive assembly

`FarmingGuideItemState`의 attachment/armor child tree가 조립 상태다.

`FarmingGuideAssemblyPolicy`가 다음의 Core authority다.

- deep node lookup/mutation
- attachment filter / armor allowed-item validation
- item and assembly-wide conflicts
- required-slot recursion
- bounded traversal
- deterministic assembly signature
- persisted tree recursive sanitization

WPF가 별도 compatibility truth를 만들지 않는다.

## 3. Workbench interaction

- workbench는 Farming Guide 내부에 유지하고 별도 generic OS config window를 사용하지 않는다.
- installed attachment를 통해 하위 child slot으로 재귀 navigation할 수 있다.
- empty attachment/replaceable armor slot single-click은 같은 page에 compatible item picker를 연다.
- candidate single-click으로 장착한다.
- 기존 search-result drag/drop도 유지한다.
- picker와 drag/drop은 같은 `FarmingGuideAssemblyPolicy`를 사용한다.
- occupied one-item slot을 silent overwrite하지 않는다.

## 4. Assembly-aware presentation

현재 build가 authoritative imported default preset membership과 정확히 일치하고 usable composed image가 있을 때만 preset image를 사용한다.

Arbitrary build는 base image + deterministic installed-part indication fallback을 사용한다. 이는 Tarkov client의 완전한 조립 renderer라고 주장하지 않는다.

## 5. Storage authority 분리

Storage mechanics와 visual arrangement는 별도 authority다.

Mechanics authority:

- current validated Game Content grid count / width / height
- filters
- item dimensions
- actual placement legality

Visual arrangement:

- product-owned verified layout identity / coordinates
- mechanics를 변경할 권한 없음
- unknown/stale metadata는 compact fallback

Importer는 source에 존재하는 `GridLayoutName` / `gridLayoutName` / `RigLayoutName` / `rigLayoutName` 계열 identity를 `StorageLayoutName`으로 보존한다.

## 6. v1.14.0 공개 구현의 확인된 한계

v1.14.0 설계 의도는 current live grid **count/width/height signature가 검증된 profile과 정확히 일치할 때만** exact visual coordinates를 사용하는 것이었다.

그러나 공개 v1.14.0 exact source `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`을 release-closure 단계에서 다시 감사한 결과, resolver는 다음만 확인했다.

- layout identity / verified alias
- grid count
- live dimensions가 positive인지
- transformed rectangle이 finite인지
- resulting rectangles가 overlap하지 않는지

각 grid index의 **expected width/height를 profile에 저장하고 비교하는 단계는 없었다**. 따라서 dimension-only Tarkov drift가 non-overlap인 경우 stale exact coordinates가 유지될 수 있었다.

이 historical fact를 소급해 수정하지 않는다. v1.14.0 tag/source/assets는 immutable하다.

**Current authority는 `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`이며, v1.14.1이 expected per-index width/height 검증을 추가해 이 guard를 완성한다.**

## 7. Dogtag surface

PMC dogtag는 current raid-start equipment board에서 제거했다.

- old schema-v1 value는 backward-compatible하게 deserialize 가능
- current product state에서는 정상 actionable equipment state로 유지하지 않음
- 이 변경만으로 Farming Guide user-state schema를 올리지 않음

## 8. Content schema

v1.14.0부터 assembly source / layout identity를 보존하기 위해:

```text
write: v10
read: v3-v10
```

Farming Guide user-state schema는 v1을 유지한다.

## 9. Fail-closed 원칙

- current catalog에 없는 assembly child
- slot/filter가 허용하지 않는 child
- current assembly conflict
- locked/unallowed armor plate
- excessive recursive depth
- unknown/stale visual-layout profile

을 낙관적으로 허용하지 않는다.

Visual exact-layout activation의 complete dimension-signature 조건은 v1.14.1 decision이 current authority다.

## 10. 공개 상태

Historical v1.14.0 product source:

```text
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
527 passed / 0 failed / 0 skipped
release id: 380133403
```

v1.14.0 recursive assembly, inline picker, schema-v10/layout-identity functionality은 current product에 유지된다. Exact storage-layout dimension guard만 v1.14.1에서 corrected/superseded 되었다.
