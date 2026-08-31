# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

**v1.14.0 Farming Guide — 실제 조립 상태/총기 개조/수납 배치 강화**

Base main:

```text
f8728e2d18bb07d47d8d4adcb5bf683dff92bce5
```

Working branch:

```text
feature/v1.14.0-farming-guide-assembly-2026-08-31
```

Target version: **v1.14.0**

버전 근거: 빈 조립 슬롯 클릭 시 호환 아이템을 아이콘으로 탐색·즉시 장착하는 새 사용자 능력과 조립 상태 기반 표현을 추가하므로 `docs/VERSIONING.md`의 MINOR 규칙을 적용한다.

## Confirmed scope

사용자 확정 요구사항:

1. Farming Guide 장비 보드에서 인식표 슬롯을 제거한다. 현재 사용자가 장착할 수 있는 인식표 아이템이 없으므로 raid-start equipment surface에 노출하지 않는다.
2. 조립 가능한 아이템은 장착 부품 상태가 바뀌면 표시 이미지도 그 조립 상태를 반영한다. Altyn + face shield, 총기 부품 조합을 대표 사례로 검증한다.
3. 총기 부품/slot/compatibility 추적을 강화해 Farming Guide 안에서 실제 총기 개조를 구성할 수 있는 수준으로 만든다. 단순 root 1단계 attachment 편집에 머물지 않고 attachment child의 하위 슬롯까지 추적한다.
4. 리그 등 다중 storage grid를 무한 가로 나열하지 않는다. 각 grid의 실제 width×height와 GridIndex 의미는 보존하면서 사용 가능한 가로 폭 안에서 compact 2D 배치해 화면 밖 clipping/overflow를 방지한다.
5. 조립 가능한 빈 슬롯을 클릭하면 해당 슬롯에 현재 상태에서 장착 가능한 아이템을 아이콘 포함 inline UI로 표시한다. 사용자가 하나를 클릭하면 즉시 장착한다. 별도 Windows/OS 창은 사용하지 않는다. 기존 검색 결과 drag → slot drop 방식도 그대로 유지한다.

## Completed

- v1.13.3 public stable / repository state 복구 완료.
- 현재 Farming Guide architecture/state/importer/image/workbench/storage rendering 경로 분석.
- root causes 확인:
  - item image cache가 `ItemId -> IconUrl`만 사용해 assembly state를 표현할 수 없음.
  - `FarmingGuideItemState` 자체는 recursive child state를 담을 수 있으나 현재 Workbench가 root item의 direct slots만 편집함.
  - current importer는 slot `id/nameId/required/filters`는 보존하지만 `properties.defaultPreset`과 preset composed image/containsItems를 보존하지 않음.
  - carrier/workbench storage grids가 horizontal WrapPanel에만 배치되어 finite center width에서 compact 2D layout을 보장하지 않음.
- current json.tarkov.dev shape 및 공개 weapon-mod implementation 교차 확인:
  - weapon `properties.defaultPreset`은 preset item id.
  - preset item은 composed `image512pxLink`와 `containsItems`를 제공할 수 있음.
  - slot `nameId`는 `mod_barrel`, `mod_stock` 같은 실제 game slot identity로 사용 가능.
  - arbitrary gun build image generation은 별도 composite image service가 사용되는 사례가 있으나, 해당 외부 서비스의 안정성/비총기 장비 지원은 아직 product dependency로 확정하지 않음.

## Current step

- fixed dogtag persistence backward compatibility 확인.
- richer canonical assembly data 계약 설계: image fields / default preset / recursive slot tree / compatibility candidate policy.
- assembly image resolver의 exact-data / fallback 정책 확정.

## Remaining

- 제품/설계 결정 문서 작성 및 relevant architecture 갱신.
- dogtag UI/state lifecycle 제거(legacy persisted state 안전 처리 포함).
- richer item assembly metadata import 및 content compatibility 검증.
- recursive gun/gear attachment editing 구현.
- 빈 slot inline compatible-item chooser + icon loading 구현.
- assembly-aware image resolver/rendering 구현.
- finite-width compact multi-grid layout 구현.
- deterministic tests / importer fixtures / persistence regression / desktop contract tests 추가.
- v1.14.0 version/release notes/project-state 준비.
- PR exact-head CI, Windows Release build, self-contained publish, actual published EXE Product UI/Farming Guide smoke, Shutdown Race, Documentation Consistency.
- main merge, exact-main CI, v1.14.0 public tag/release/assets/checksum 검증.
