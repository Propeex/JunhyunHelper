# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

**v1.14.0 Farming Guide — 실제 조립 상태/총기 개조/검증된 수납 배치 강화**

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

버전 근거: 빈 조립 슬롯 클릭 시 호환 아이템을 아이콘으로 탐색·즉시 장착하는 새 사용자 능력, 재귀 부품 편집, 조립 상태 기반 표현을 추가하므로 `docs/VERSIONING.md`의 MINOR 규칙을 적용한다.

## Confirmed scope

사용자 확정 요구사항:

1. Farming Guide 장비 보드에서 인식표 슬롯을 제거한다. 현재 사용자가 장착할 수 있는 인식표 아이템이 없으므로 raid-start equipment surface에 노출하지 않는다.
2. 조립 가능한 아이템은 장착 부품 상태가 바뀌면 표시 이미지도 그 조립 상태를 반영한다. Altyn + face shield, 총기 부품 조합을 대표 사례로 검증한다.
3. 총기 부품/slot/compatibility 추적을 강화해 Farming Guide 안에서 실제 총기 개조를 구성할 수 있는 수준으로 만든다. root 1단계에 머물지 않고 attachment child의 하위 슬롯까지 재귀 추적한다.
4. 리그/가방/컨테이너의 다중 storage grid는 generic 가로 나열을 제품 목표로 삼지 않는다. 검증된 exact visual metadata가 있고 current live grid signature와 일치할 때만 해당 상대 배치를 사용한다. metadata가 없거나 구조가 달라지면 finite compact layout으로 fail-safe fallback하며 이를 authentic layout으로 주장하지 않는다.
5. 조립 가능한 빈 슬롯을 클릭하면 현재 조립 상태에서 장착 가능한 아이템을 아이콘 포함 inline UI로 표시한다. 사용자가 하나를 클릭하면 즉시 장착한다. 별도 Windows/OS 창은 사용하지 않는다. 기존 search drag → slot drop도 유지한다.

## Completed

- v1.13.3 public stable / repository state 복구 완료. 공개 v1.13.3 release identity는 변경하지 않았다.
- Draft PR #250 생성 및 Farming Guide architecture/state/importer/image/workbench/storage rendering 경로 분석 완료.
- dogtag 장비 보드 UI 퇴역 및 schema-v1 persistence backward compatibility 구현. legacy dogtag 값은 읽을 수 있으나 current product state에서는 제거된다.
- `FarmingGuideAssemblyPolicy` 기반 recursive assembly 구현:
  - deep attachment/armor mutation
  - recursive current-data sanitization
  - slot filter/allowed plate/conflict 검증
  - assembly-wide conflict 검증
  - required-slot recursion
  - deterministic assembly signature
- recursive WPF workbench 구현:
  - attachment child 하위 slot 탐색
  - 상위 부품으로 복귀
  - empty slot inline compatible-item picker
  - 아이콘 카드 single-click 장착
  - drag/drop과 동일한 compatibility policy 공유
  - 별도 OS/config Window 없음
- assembly-aware image presentation 구현:
  - current build가 authoritative default preset 구성과 정확히 일치하면 imported composed preset image 사용
  - 그 외 임의 조립은 base image + deterministic installed-part visual fallback
- richer assembly metadata import:
  - `properties.defaultPreset`
  - preset `image512pxLink` / `gridImageLink`
  - preset `containsItems`
  - slot `id/nameId/required/filters`
- Content snapshot write schema **v10**, readable **v3-v10**으로 확장. Farming Guide user-state schema는 v1 유지.
- storage visual layout resolver/renderer 구현:
  - live grid count/width/height signature와 exact visual metadata를 대조
  - signature가 맞을 때만 exact relative placement 적용
  - mismatch/unknown은 deterministic finite compact fallback
  - current exact catalog는 검증된 최소 alias만 보유하며, provenance/license가 확인되지 않은 외부 atlas는 포함하지 않음
- importer가 `GridLayoutName` / `gridLayoutName` / `RigLayoutName` / `rigLayoutName`을 `StorageLayoutName`으로 보존하도록 수정.
- published EXE Farming Guide smoke에 exact multi-grid Canvas 배치와 각 `GridDropTarget.GridIndex` identity 검증 추가.
- deterministic regression 추가:
  - importer layout identity 3건
  - recursive assembly 6건
  - storage visual resolver 및 기존 persistence/UI 계약
- exact PR head `7b9a96ccdff0ff1e0ddfb6f676624d24b150b7a1` 기준 검증 완료:
  - Windows Release build SUCCESS
  - **527 passed / 0 failed / 0 skipped**
  - self-contained win-x64 publish SUCCESS
  - actual published EXE Product UI/Farming Guide/Map smoke SUCCESS
  - graceful shutdown SUCCESS
  - release package/checksum verification SUCCESS
  - Shutdown Race SUCCESS
  - Documentation Consistency SUCCESS
- v1.14.0 release preparation 시작:
  - `docs/PROJECT_STATE.json` desktopVersion을 1.14.0 target으로 갱신
  - publicStable은 검증된 v1.13.3으로 유지
  - deterministic test count를 527로 갱신

## Current step

- desktop assembly version / FIRST_RUN / release notes를 v1.14.0으로 정합화한다.
- v1.14.0 Farming Guide 결정문과 architecture/current decision index를 갱신한다.
- release-prep exact PR HEAD에서 CI / Shutdown Race / Documentation Consistency / published EXE smoke를 다시 수행한다.

## Remaining

- v1.14.0 release identity 파일 정리 완료.
- PR #250 exact-head 전체 gate green 확인.
- PR ready 처리 및 main 병합.
- merged exact-main CI / Shutdown Race / Documentation Consistency green 확인.
- main CI artifact에서 자동 v1.14.0 Release workflow 완료 확인.
- public `v1.14.0` tag/release/source/assets/checksum 무결성 검증.
- 공개 사실값으로 README / PROJECT_STATE / CURRENT_STATE / STATE / DECISIONS / Farming Guide architecture / release evidence 갱신.
- `docs/ACTIVE_WORK.md`를 NONE으로 닫고 post-release docs-only main gate를 확인한다.
