# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

## Goal

v1.11.2 실사용에서 확인된 Items/Hideout 검색 clear 표시 회귀와 Map 지도 마커 패널의 탈출구 영역 잘림을 수정하고, Scanner Case 교정 이미지에 마우스 휠 확대/축소를 추가한다. 사용자가 전달한 최신 Scanner diagnostics/calibration bundle도 분석해 현재 Scanner 정확도 개선 근거로 기록·검증한다.

## Base

```text
public stable: v1.11.2
exact v1.11.2 product source: 5822757f6490ec82aab33793752e48de14490628
base main: f20ac2f797a7ea69999180173b849cf7ed958624
branch: fix/v1.11.3-ui-calibration-2026-08-30
target: v1.11.3 PATCH maintenance
```

## Confirmed scope

1. Items / Hideout 검색 clear
   - 검색어가 비어 있으면 `×`를 숨긴다.
   - 텍스트가 입력되면 Quest 등 정상 검색창과 같은 inline `×`를 표시한다.
   - 클릭 시 query를 지우고 검색창 focus를 유지한다.
   - v1.11.2의 user-PC evidence에서는 Items/Hideout 모두 텍스트 입력 후에도 `×`가 표시되지 않는다.

2. Map 지도 마커 패널
   - 정상적인 큰 창에서도 탈출구 체크박스 영역이 잘리지 않고 접근 가능해야 한다.
   - 창 높이가 작아 내용이 실제로 넘칠 때만 내부 vertical scrolling으로 모든 항목에 접근 가능해야 한다.
   - 현재 user-PC evidence에서는 큰 창에서 하단 탈출구 영역이 클리핑되며, 창 높이를 줄인 뒤에야 내부 scrollbar/하단 영역 일부가 보인다.

3. Scanner Case 교정 이미지 zoom
   - 교정 화면의 screenshot/ROI image 위에서 mouse wheel로 확대/축소한다.
   - 기존 ROI 선택/좌표 의미와 저장 데이터는 zoom 때문에 달라지지 않아야 한다.
   - 확대 시 필요한 경우 image viewport를 스크롤해 확인할 수 있어야 한다.

4. 최신 Scanner diagnostics/calibration bundle
   - user-provided `JunhyunHelper-Scanner-Diagnostics-20260830-232854.zip`을 분석한다.
   - bundle summary 기준 reviewed case 5개, program-correct 0개, Ground Truth correction 5개, OCR_RECOGNITION 5개이며 pipeline stage는 모두 `NOT_RUN`으로 기록되어 있다.
   - Ground Truth: `Wrench 렌치`, `Nails 못 상자`, `ELCAN Specter HCO holographic sight`, `Corrugated hose 주름진 호스`, `7.62x25mm TT P gl ammo pack (25 pcs)`.
   - 원본 사용자 diagnostic bundle을 저장소에 임의 커밋하지 않는다. 현재 regression/dataset 계약에 맞는 최소 재현 evidence만 필요 시 별도로 반영한다.

## Completed

- v1.11.2 stable / main / product source 복구.
- 사용자 실사용 화면 3장 및 diagnostics bundle 접수.
- v1.11.3 maintenance branch 생성.
- diagnostics bundle 구조/summary/environment/dataset 5건 1차 분석.
- 현재 search clear 구현이 `ProductSearchClearButtonBehavior`의 module-initializer class handler에 의존하고 있음을 확인.

## Current step

Quest/Ammo/Scanner의 실제 정상 search clear 구현과 Items/Hideout lifecycle 차이를 비교하고, Map marker panel height/scroll 측정 경로 및 ScannerCorrectionWindow image viewport/coordinate mapping을 추적한다.

## Remaining

- 세 UI 요구사항 root cause 확정 및 최소 범위 구현.
- 전달된 5 reviewed cases를 Scanner pipeline 관점에서 분석하고 개선 필요 여부 결정.
- deterministic regression coverage 추가.
- Windows Release build / publish / actual published EXE UI-runtime smoke.
- PR / exact-head CI / main merge / exact-main validation.
- v1.11.3 release/tag/assets 검증 및 공식 상태 문서 마감.
