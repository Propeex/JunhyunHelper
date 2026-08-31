# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

**v1.14.0 Farming Guide — 실제 조립 상태/총기 개조/수납 배치 강화**

## Base

Base main:

```text
f8728e2d18bb07d47d8d4adcb5bf683dff92bce5
```

Working branch:

```text
feature/v1.14.0-farming-guide-assembly-2026-08-31
```

Draft PR: **#250**  
Target version: **v1.14.0**

버전 근거: 빈 조립 슬롯 클릭 시 호환 아이템을 아이콘으로 탐색·즉시 장착하는 새 사용자 능력과 조립 상태 기반 표현을 추가하므로 `docs/VERSIONING.md`의 MINOR 규칙을 적용한다.

## Confirmed scope

사용자 확정 요구사항:

1. Farming Guide 장비 보드에서 인식표 슬롯을 제거한다. 현재 사용자가 장착할 수 있는 인식표 아이템이 없으므로 raid-start equipment surface에 노출하지 않는다.
2. 조립 가능한 아이템은 장착 부품 상태가 바뀌면 표시 이미지도 그 조립 상태를 반영한다. Altyn + face shield, 총기 부품 조합을 대표 사례로 검증한다.
3. 총기 부품/slot/compatibility 추적을 강화해 Farming Guide 안에서 실제 총기 개조를 구성할 수 있는 수준으로 만든다. 단순 root 1단계 attachment 편집에 머물지 않고 attachment child의 하위 슬롯까지 추적한다.
4. 리그/가방/컨테이너의 다중 storage grid는 단순 가로 나열이나 generic compact packing을 제품 목표로 삼지 않는다. 가능한 경우 실제 Escape from Tarkov 인벤토리에서 보이는 grid 묶음의 상대적 배치를 그대로 재현한다. live storage mechanics(width/height/filter)는 current tarkov.dev를 authority로 유지하고, UI layout identity/coordinates는 별도 검증된 metadata로 관리한다. exact metadata가 없거나 live grid signature와 맞지 않는 신규/변경 아이템에만 compact 2D packing을 fallback으로 사용하며, fallback을 exact layout으로 오인하지 않는다.
5. 조립 가능한 빈 슬롯을 클릭하면 해당 슬롯에 현재 상태에서 장착 가능한 아이템을 아이콘 포함 inline UI로 표시한다. 사용자가 하나를 클릭하면 즉시 장착한다. 별도 Windows/OS 창은 사용하지 않는다. 기존 검색 결과 drag → slot drop 방식도 그대로 유지한다.

## Completed

- v1.13.3 public stable / repository state 복구 완료.
- 현재 Farming Guide architecture/state/importer/image/workbench/storage rendering 경로 분석.
- Draft PR #250 생성; public v1.13.3에는 영향 없음.
- dogtag UI 퇴역 및 schema-v1 persistence backward compatibility 구현. legacy dogtag 값은 역직렬화 가능하지만 product state에서는 null로 정규화한다.
- recursive assembly validation 기반 추가. persisted attachment/plate tree를 current slot/filter/conflict 계약으로 재귀 sanitize하는 방향을 연결했다.
- current weapon/preset metadata 분석 및 richer assembly metadata import 기반 추가:
  - weapon `properties.defaultPreset`
  - preset composed `image512pxLink` / `gridImageLink`
  - preset `containsItems`
  - slot `id/nameId/required/filters`
- Content snapshot write schema를 v10으로 확장하고 v3-v9 readability를 유지했다.
- assembly-aware image/workbench 기반 구현 진행:
  - authoritative preset 일치 시 composed image 사용 가능하도록 metadata 확보
  - arbitrary build는 외부 renderer 필수 의존 없이 deterministic attachment-aware 표시
  - recursive workbench navigation 및 empty-slot inline compatible-item picker 골격 연결
- 기존 infinite-horizontal multi-grid rendering을 finite-width fallback으로 교정했다. 이는 exact layout 미확인 시 fallback으로만 유지한다.
- authentic storage layout 데이터 경로 분석 완료:
  - current tarkov.dev `ItemStorageGrid`는 width/height/filter를 제공하지만 UI X/Y와 `RigLayoutName`은 노출하지 않는다.
  - Tarkov raw item template에는 `RigLayoutName`이 존재한다.
  - current raw backend mirror `carlsmei/tarkovdata`에서 현재 item id -> `RigLayoutName` 값을 추적할 수 있음을 확인했다.
  - Tarkov client `TemplatedGridsView`는 layout prefab의 실제 `GridView` transform 좌표를 사용한다. 공개 UIFixes 구현도 이 좌표를 64px cell 기준으로 읽어 grid 순서를 계산한다.
  - `bmpq/stash_canvas`의 `GridTemplates/*.asset`에는 Tarkov `RigLayoutName`과 같은 layout key 및 각 grid의 x/y/width/height가 이미 factual atlas 형태로 존재함을 확인했다(대표 `mbss_rig`). atlas provenance/license/coverage 및 좌표 정규화 규칙을 추가 검증 중이다.
- PR #250 1차 CI:
  - Windows Release desktop build SUCCESS.
  - Shutdown Race SUCCESS.
  - core tests 512/513, 유일한 실패는 dogtag 폐기 후 기존 persistence test의 obsolete expectation. 새 계약으로 수정 완료.
  - Documentation Consistency 실패 원인은 ACTIVE_WORK 필수 `## Base` 헤더 누락. 현재 문서에서 복구 완료.

## Current step

- authentic GridTemplate atlas의 license/coverage 및 좌표 방향/단위 검증.
- `RigLayoutName + grid index -> normalized X/Y` factual metadata를 JunhyunHelper-owned 최소 catalog로 변환하는 정책 구현.
- exact metadata 적용 전 live grid count/width/height signature 검증 및 safe fallback 구현.
- PR #250 CI 재검증과 recursive assembly/UI deterministic tests 확장.

## Remaining

- authentic storage layout Core/catalog/resolver 및 renderer 구현.
- current item id -> `RigLayoutName` refresh tooling/metadata 구축; runtime GitHub dependency는 두지 않는다.
- 제품/설계 결정 문서 작성 및 relevant architecture 갱신.
- recursive gun/gear attachment editing 완성 및 nested drag/drop path safety 점검.
- 빈 slot inline compatible-item chooser 아이콘/selection UX 완성.
- assembly-aware image resolver/rendering 완성 및 Altyn/weapon 대표 회귀 검증.
- deterministic tests / importer fixtures / persistence regression / desktop contract tests 추가.
- v1.14.0 version/release notes/project-state 준비.
- PR exact-head CI, Windows Release build, self-contained publish, actual published EXE Product UI/Farming Guide smoke, Shutdown Race, Documentation Consistency.
- main merge, exact-main CI, v1.14.0 public tag/release/assets/checksum 검증.
