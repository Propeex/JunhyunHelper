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
public stable: `v1.15.2`  
exact public product source: `f4974ee6bed5047865581240197f7f0e2787ba7c`

## Confirmed scope

사용자 실사용 요구사항 기준.

1. 저장된 아이템은 기본적으로 일반 테두리를 사용하고 잠금 상태만 Accent/노란색 테두리를 사용한다.
2. Key tool 하나만 예외 처리하지 않는다. 실제 Tarkov item data의 `properties.grids`와 grid filter를 보유한 저장 컨테이너는 nested storage 대상으로 취급한다.
3. 열쇠/돈/카드/문서/주사기 등 전용 컨테이너의 수납 가능 여부는 source-backed allowed/excluded category/item filter를 그대로 존중한다.
4. 검색 결과 hover + `T` 테스트 입력은 활성 raid session에서 실제 스캔과 동일한 Farming Guide recommendation 경로로 들어가야 한다.
5. 기존 v1.15.2 완제품 장비 모델(weapon/armor 내부 부품 편집 비노출)은 유지한다.

## Completed

- v1.15.2 stable 상태와 관련 코드 복구
- root cause 확인: `FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem`가 rig/backpack/secure-container 외 아이템의 `StorageGrids`를 삭제하고, `SupportsNestedStorage`도 backpack/rig로 제한하고 있음
- importer가 이미 `properties.grids`와 allowed/excluded filters를 canonical FarmingGuide layout으로 보존함을 확인
- 저장 아이템 기본 노란색 테두리가 `FarmingGuidePage.Rendering.CreateGridCanvas`와 unlock visual restore 경로에 하드코딩되어 있음을 확인
- 작업 브랜치 생성 및 체크포인트 시작

## Current step

- nested-storage runtime policy를 source-backed grid 기준으로 일반화
- 저장 아이템 잠금 시각 규칙 수정
- hover + `T` simulated scan 이벤트 경로 및 focus/hit-test 회귀 분석

## Remaining

- 코드 수정 및 회귀 테스트 추가/보완
- 전체 테스트 / Release build / Windows publish 및 관련 Product UI/runtime smoke
- 문서/버전 갱신
- PR/CI/main 병합/exact-main 검증
- v1.15.3 release/tag/assets 검증 및 ACTIVE_WORK 종료
