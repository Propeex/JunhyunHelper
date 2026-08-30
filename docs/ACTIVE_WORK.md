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
PR: #233
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
   - bundle summary 기준 reviewed case 5개, program-correct 0개, Ground Truth correction 5개, OCR_RECOGNITION 5개이며 저장된 pipeline stage는 모두 `NOT_RUN`이다.
   - Ground Truth: `Wrench 렌치`, `Nails 못 상자`, `ELCAN Specter HCO holographic sight`, `Corrugated hose 주름진 호스`, `7.62x25mm TT P gl ammo pack (25 pcs)`.
   - 원본 사용자 diagnostic bundle과 screenshots는 private evidence로 유지하며 public repository에 커밋하지 않는다.

## Root Cause / Evidence Findings

### Items / Hideout search clear

공유 구현 `ProductSearchClearButtonBehavior` 자체의 inline `×` 동작은 요청한 동작과 일치한다. 문제는 Items/Hideout가 실제 visible page lifecycle에서 이 behavior를 안정적으로 attach하지 못하는 데 있었다. 더 중요한 검증 결함으로 v1.11.2 published smoke는 실제 페이지가 `×`를 만들었는지 확인하지 않고 smoke 코드가 직접 `ProductSearchClearButtonBehavior.Attach(searchBox)`를 호출한 뒤 결과를 검사했다. 따라서 실사용 회귀가 있어도 smoke가 스스로 UI를 만들어 성공할 수 있었다.

### Map marker panel

v1.11.2 body layout은 expanded panel의 높이를 현재 `MapMarkersContent.DesiredSize`에 맞추는 content-sized popup 방식이었다. 탈출구 행 이동/생성 등 content tree가 아직 완전히 정착하지 않은 시점의 작은 DesiredSize를 기준으로 tall window의 panel height를 고정할 수 있어 이후 하단 탈출구 영역이 panel 밖에서 잘렸다. 기존 smoke도 이미 선택된 짧은 panel 내부를 viewport가 채우는지만 검사해 이 결함을 놓쳤다.

### Scanner correction evidence bundle

5건 모두 저장 JSON에는 `RecognitionReason=NOT_RUN`, `OcrText=""`가 남아 있으나 bundled runtime log에서 적어도 마지막 두 case는 저장 직전 실제 OCR/matcher가 실행된 것이 확인된다.

- `Corrugated hose 주름진 호스`: WinRT OCR이 선두 Latin glyph 일부를 Han/CJK glyph로 오인해 `OCR_INVALID_CHARACTERS`로 보수적 reject했다.
- `7.62x25mm TT P gl ammo pack (25 pcs)`: nearest official candidate는 Ground Truth와 동일했지만 약 0.846 confidence / 약 0.038 margin으로 `LOW_CONFIDENCE` fail-closed 됐다.

즉 geometry/title ROI 자체는 두 case에서 정상적으로 접근됐으며, 저장된 `NOT_RUN`은 인식이 실행되지 않았다는 뜻이 아니다. `ScannerRecognitionDebugStore`가 단일 최신 frame만 유지하기 때문에 분석 완료 frame 뒤의 빠른 새 capture가 geometry-only `NOT_RUN` frame으로 덮어쓰고, 사용자가 이후 교정 hotkey를 누르면 의미 있는 OCR/matcher evidence가 유실되는 diagnostic timing defect가 확인됐다.

이 batch에서는 false-positive 우선 안전 계약을 깨는 threshold/character-policy 완화를 하지 않는다. 대신 동일한 non-empty `TitleSignature`, 동일 capture mode, 3초 이내라는 fail-closed 조건에서만 최신 exact screenshot/geometry에 직전 analyzed semantics를 보존해 교정 데이터 품질을 바로잡는다. 이 retained evidence는 live recognition 결정에는 사용하지 않는다.

## Completed

- v1.11.2 stable / main / product source 복구.
- 사용자 실사용 화면 3장 및 diagnostics bundle 접수/분석.
- v1.11.3 maintenance branch 및 PR #233 생성.
- Items/Hideout real page lifecycle에 shared `ProductSearchClearButtonBehavior`를 직접 attach하는 Loaded + `OnApplyTemplate` boundary 구현.
- published search smoke가 behavior를 직접 만들어내던 false-positive 검증 경로 제거; 실제 page lifecycle 결과만 검사하도록 변경.
- Map marker expanded panel을 content-sized popup에서 available-height viewport로 변경. 큰 창에서는 전체 available height를 사용하고 실제 overflow에서만 ScrollViewer가 scrollbar를 렌더하도록 변경.
- Map published smoke에 expanded panel full-height 및 rendered scrollbar 상태 검증 추가.
- Scanner correction screenshot을 source-pixel canvas + ScrollViewer + display-only LayoutTransform 구조로 변경. mouse wheel 1.15x step, fit~8x zoom, pan/scroll 및 pointer anchor 보존 구현.
- correction zoom published smoke 추가; 확대/축소와 source-pixel coordinate contract를 실제 WPF window에서 검사.
- 첫 correction zoom runtime smoke에서 Auto scrollbar 상태에 따라 fit scale이 0.573 → 0.596으로 달라지는 문제를 검출하고, stable arranged control bounds 기준으로 fit 계산을 수정.
- `ScannerRecognitionDebugStore`에 최근 analyzed evidence retention 추가. 동일 title signature/capture mode/3초 이내일 때만 correction snapshot에 semantic evidence를 carry하고 current image/geometry는 그대로 유지.
- correction hotkey와 수동 최신 교정 모두 `GetCorrectionSnapshot()`을 사용하도록 연결.
- v1.11.3 source-contract regression tests 추가.
- stale v1.8.3 marker-panel source-string test를 현재 available-height 제품 계약으로 갱신.
- pre-release exact-head candidate `b34335f5e89047b83550b8faf290eb6a1e3986bc` 검증 완료:
  - Documentation Consistency run `33318820812` SUCCESS
  - CI run `33318820804` SUCCESS
  - 474 passed / 0 failed / 0 skipped
  - Windows Release build SUCCESS
  - Windows x64 self-contained publish SUCCESS
  - actual published EXE Product UI / Map / Scanner smoke SUCCESS
  - correction zoom stable fit + source-pixel contract SUCCESS
  - release package build/audit + artifact upload SUCCESS
  - Shutdown Race run `33318820799` SUCCESS

## Current step

v1.11.3 release identity를 확정한다. Desktop version / FIRST_RUN / PROJECT_STATE desktopVersion / RELEASE_NOTES를 한 commit으로 맞춘 뒤 그 exact-head에서 final CI, published EXE smoke, package, Shutdown Race를 다시 통과시킨다.

## Remaining

- v1.11.3 release identity commit 생성.
- final PR exact-head Documentation Consistency / CI / Shutdown Race 성공 확인.
- PR ready / merge.
- exact-main Documentation Consistency / CI / Shutdown Race 성공 확인.
- v1.11.3 release workflow 실행 및 tag/release/assets/checksum public readback.
- PROJECT_STATE publicStable / README / CURRENT_STATE / STATE / RELEASE_1.11.3 최종 기록.
- 공식 상태 문서 정리 후 `ACTIVE_WORK`를 `NONE`으로 종료.
