# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

Farming Guide 실사용 보완 PATCH `v1.15.3` 작업.

- 일반 보관 아이템의 노란색 테두리를 제거하고, 사용자가 `F`로 잠금/고정한 경우에만 노란색 테두리를 표시한다.
- Key tool을 포함해 Tarkov 데이터에 실제 내부 storage grid가 정의된 컨테이너 아이템의 nested storage를 보존하고 사용 가능하게 한다.
- 특수 컨테이너 내부 배치는 이름 하드코딩이 아니라 authoritative item grid/filter 데이터에 따라 허용/차단한다.
- 파밍 가이드 검색 결과에 커서를 올리고 `T`를 눌러 스캔 입력을 모사하는 테스트 기능의 현재 회귀를 수정한다.

## Base

base main: `5352dfe6bf673a79d2833b44491ebe11ed6af65f`  
working branch: `fix/v1.15.3-farming-guide-storage-scan-sim-2026-09-01`  
PR: `#264` (draft validation)  
public stable: `v1.15.2`  
exact public product source: `f4974ee6bed5047865581240197f7f0e2787ba7c`

## Confirmed scope

사용자 실사용 요구사항 기준.

1. 저장된 아이템은 기본적으로 일반 테두리를 사용하고 잠금 상태만 Accent/노란색 테두리를 사용한다.
2. Key tool 하나만 예외 처리하지 않는다. 실제 Tarkov item data의 `properties.grids`와 grid filter를 보유한 저장 컨테이너는 nested storage 대상으로 취급한다.
3. 열쇠/돈/카드/문서/주사기 등 전용 컨테이너의 수납 가능 여부는 source-backed allowed/excluded category/item filter를 그대로 존중한다.
4. 검색 결과 hover + `T` 테스트 입력은 활성 raid session에서 실제 스캔과 동일한 Farming Guide recommendation 경로로 들어가야 한다.
5. 기존 v1.15.2 완제품 장비 모델(weapon/armor 내부 부품 편집 비노출)은 유지한다.

Canonical correction: `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`.

## Completed

- v1.15.2 stable 상태와 관련 코드 복구
- importer가 이미 `properties.grids`와 allowed/excluded filters를 canonical FarmingGuide layout으로 보존함을 확인
- root cause 수정
  - complete-equipment runtime projection이 모든 source-backed `StorageGrids`를 유지하도록 변경
  - `SupportsNestedStorage`를 backpack/rig 이름/분류가 아니라 실제 storage grid 존재 여부로 일반화
- 기존 recursive `ParentInstanceId` sanitizer/raid-surface 탐색이 generic container nesting과 grid filter를 이미 공통 지원함을 확인
- stored item 기본 렌더링 border를 neutral `BorderBrush`로 변경
- lock apply/unlock restore가 accent ↔ neutral로 명확히 왕복하도록 정리
- nested detail render 직후에도 lock visual을 적용하도록 보완
- hover + `T` test command가 Search TextBox focus보다 hovered result를 우선하도록 수정
- Scanner runtime이 꺼져 있거나 in-memory catalog 미초기화 상태에서도 same-mode verified local Scanner cache를 on-demand load하는 simulated snapshot resolver 추가
- simulated snapshot 준비 실패가 silent no-op이 아니라 명시적 상태로 보이도록 처리
- deterministic tests 추가
  - arbitrary source-backed specialized storage runtime preservation
  - Secure Container 안 specialized container의 allowed/denied filter enforcement
- v1.15.2 equipment-internal editor boundary 유지
- initial PR `#264` CI evidence
  - Desktop Release build: SUCCESS
  - Shutdown Race CI `33484816329`: SUCCESS
  - Documentation Consistency `33484816312`: SUCCESS
  - Core tests: 562? existing + new total 563 중 1개 stale maintenance-contract assertion 실패
  - 실패 원인은 v1.15.2의 `bag/rig only` 문구를 고정한 `FarmingGuideDesktopSectionContractTests`; 구현 compile 오류 아님
- stale maintenance contract를 v1.15.3 source-backed storage/T/border contract로 갱신
- Desktop version `1.15.3`, FIRST_RUN, PRODUCT, decision, release notes 갱신

## Current step

최신 PR head에서 full CI / Shutdown Race / Documentation Consistency를 다시 검증하고, 통과 후 published EXE smoke 및 release metadata/state를 finalization한다.

## Remaining

- 최신 head full CI 결과 확인 및 발견 회귀 수정
- published Windows product smoke / package audit 확인
- PR ready/merge 및 exact-main CI / Shutdown Race / Documentation Consistency
- README/CURRENT_STATE/STATE/PROJECT_STATE를 exact release evidence로 최종 갱신
- v1.15.3 release/tag/assets/checksum 검증
- release-close docs commit 후 ACTIVE_WORK `NONE`
