# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-09-01 KST**

## Goal

현재 진행 중인 개발 작업 없음.

## Base

branch: `main`  
public stable: `v1.15.2`  
exact product source: `f4974ee6bed5047865581240197f7f0e2787ba7c`

## Confirmed scope

완료된 v1.15.2 제품 계약은 `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`, `docs/PRODUCT.md`, `docs/CURRENT_STATE.md`, `docs/STATE.md`를 기준으로 한다.

## Completed

- Farming Guide 장비를 내부 부품 편집 없는 완제품 모델로 전환
- 저장된 backpack/rig의 nested storage만 내부 상세 수납 surface로 유지
- nested storage 상세 화면을 실제 grid 크기에 맞춘 compact view로 변경
- authoritative complete/default-preset 장비 이미지 우선 사용 및 equipment-slot 이미지 점유율 개선
- 장비 내부 attachment/armor 편집과 raid Equip/ReplaceEquip target 제거, 최상위 장비 칸 판단 유지
- legacy assembly state를 root-only equipment state로 정리
- PR #262 병합
- exact-main `f4974ee6bed5047865581240197f7f0e2787ba7c` 검증 완료
- 562 passed / 0 failed / 0 skipped
- Windows x64 publish, Product UI smoke, graceful shutdown, Shutdown Race, package/checksum 검증 완료
- Release workflow `33481956300` 성공
- public latest `v1.15.2` 게시 및 tag/release/assets 무결성 확인

## Current step

없음. v1.15.2는 공개 stable 상태다.

## Remaining

자동화된 릴리즈 작업은 없음. 별도 실환경 evidence만 남아 있다.

- 사용자 실제 Tarkov 플레이에서 v1.15.2 Farming Guide 시각/동작 검증
- 필요 시 실사용 피드백 기반 후속 PATCH
- 김태영 실제 PC 진단 ZIP 수집/분석은 해당 진단 작업을 진행할 때 별도로 수행
