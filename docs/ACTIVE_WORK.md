# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

## Goal

v1.11.2 유지보수 배치에서 사용자 실사용으로 확인된 Scanner 교정 저장 UX, Items/Hideout 검색창 clear UI 회귀, Map player marker 위치/방향 정확도를 수정·검증한다.

## Base / Working State

```text
base main: 20a0ccab22bb5717bdbbf98102ab01702f0d5f70
public stable: v1.11.1
exact v1.11.1 product source: 6314eaf866539747eadd69f8da4450bd8d5939e1
working branch: fix/v1.11.2-runtime-ui-map-2026-08-30
target version: v1.11.2 (PATCH maintenance)
PR: #231 (DRAFT)
initial implementation head: 94920edffe6625ca45895ba8d20fd287eb4dd057
initial PR CI: 33307222341
initial Shutdown Race CI: 33307222304
initial Documentation Consistency: 33307222365
```

## Confirmed Scope

1. `교정 데이터 추가` global hotkey
   - 레이드 중 단축키 사용 시 교정 데이터 창을 자동으로 열지 않는다.
   - 저장 성공 시 기존 의도대로 Mini Scanner의 짧은 `저장 완료` 피드백만 제공한다.
   - Saved Case/evidence-only/no automatic Ground Truth/duplicate explicit save 계약은 유지한다.

2. Items / Hideout 검색창 clear UI
   - v1.11.1에서 추가한 항상 보이는 별도 `×` 버튼 형태를 제거한다.
   - Quest/Ammo/Scanner 검색창과 동일하게 query가 비어 있을 때는 clear glyph가 보이지 않고, 텍스트가 있을 때만 같은 방식의 `×` clear control이 나타나도록 맞춘다.
   - clear 시 기존 검색/필터 계약 및 focus 복구를 유지한다.
   - 사용자가 Quest 검색창 입력 전/후와 현재 Hideout 화면 캡처를 실사용 기준으로 제공했다.

3. Map player marker 위치/방향 정확도 audit
   - Factory에서 screenshot 기반 player marker가 실제 바라보는 방향보다 약 90° 반시계 방향으로 틀어진 것 같다는 사용자 실사용 보고가 있다.
   - Factory를 포함한 전체 map projection/heading 변환을 점검한다.
   - 위치와 방향이 원본 screenshot/player pose 의미를 정확히 반영하는지 donor transform, map-specific transform, floor/rotation path를 추적한다.
   - 공통 변환 오류가 확인되면 전체 map에 일관되게 수정하고, map-specific 차이가 필요하면 근거 있는 최소 범위로 처리한다.

## Root Cause / Design Findings

### Scanner correction hotkey

`ScannerCoordinator.CorrectionCapture.cs`의 hotkey 성공 경로가 evidence 저장과 `저장 완료` transient feedback 후 `ScannerDiagnosticCasesWindow.ShowDialog()`를 직접 호출하고 MainWindow의 Scanner section으로 focus를 이동한다. 이 창 표시/focus 이동은 hotkey 저장 계약에 불필요하며 레이드 중 사용자 입력을 방해한다. 저장 semantics는 그대로 두고 hotkey 성공 경로에서 자동 창 표시/focus 이동만 제거한다.

### Items / Hideout search clear

제품에는 이미 `ProductSearchClearButtonBehavior`가 Quest/Hideout/Items에 공통 적용되어 query가 비었을 때 clear glyph를 숨기고 query가 있을 때만 inline `×`를 표시한다. v1.11.1에서 Hideout/Items에 별도의 `SearchClearButtonInstaller`와 page partial을 추가하면서 항상 보이는 표준 Button `×`가 중복 삽입됐다. 중복 installer/partial만 제거하고 기존 공통 behavior를 canonical 구현으로 유지한다.

### Map player position / heading

Pinned donor의 screenshot 위치 경로는 `ScreenshotCoordinateParser → MapTrackerService → MapCoordinateTransformer.TryTransformPlayerPosition`이며, player 위치에는 맵별 `playerMarkerTransform [a,b,c,d,tx,ty]` affine transform을 적용한다. 이 위치 경로 자체는 현재 map config와 일치한다.

방향은 screenshot quaternion에서 얻은 raw yaw를 `ScreenPosition.Angle`에 그대로 전달한다. Main Map은 Factory `+90°`, Labs `-90°`만 이름 하드코딩으로 보정하고 MiniMap은 보정 없이 raw angle을 그대로 사용한다. 따라서 Factory MiniMap의 약 90° 오차는 실제 코드 경로로 재현되며, Labs도 같은 종류의 오차가 있고 Reserve/Labyrinth처럼 회전된 affine transform을 쓰는 맵도 방향 변환이 완전하지 않다.

수정 원칙은 맵 이름별 예외를 늘리는 것이 아니라 player 위치에 사용하는 affine transform의 선형부 `[a,b;c,d]`를 raw heading vector에도 동일하게 적용하는 것이다. 이 방식은 기존 Factory `+90°`/Labs `-90°` 의미를 자동으로 재현하면서 Reserve/Labyrinth 등 전체 현재 map config를 같은 좌표계 계약으로 처리한다. Main Map과 MiniMap 모두 donor render 이후 같은 projected heading을 최종 적용한다.

## Completed

- 공식 v1.11.1 stable / main / release source / maintenance contracts 복구.
- 작업 branch 생성.
- 사용자 요구사항 3건을 maintenance scope로 checkpoint에 기록.
- Scanner hotkey 자동 correction-window open root cause 확인 및 자동 window/focus 제거 구현.
- Scanner evidence-only / `저장 완료` feedback / no-modal regression contract 추가.
- Items/Hideout duplicate clear-button root cause 확인 및 `SearchClearButtonInstaller` + 두 page partial 제거.
- Quest/Hideout/Items가 `ProductSearchClearButtonBehavior`의 conditional inline clear를 공유한다는 회귀 계약 추가.
- Factory를 포함한 Map/MiniMap player heading 변환 audit 완료; position affine와 heading 좌표계 불일치 및 MiniMap 90° 오차 경로 확인.
- pure `PlayerHeadingProjection` 추가: player position affine의 선형부를 heading vector에 동일 적용.
- Main Map/MiniMap donor render 뒤 projected heading을 최종 적용하는 product bridge 연결.
- Factory/Labs/Reserve/Labyrinth known orientation 및 현재 모든 `playerMarkerTransform` 대상 deterministic regression coverage 추가.
- Draft PR #231 생성; initial PR CI 3종 queued.

## Current Step

PR #231 initial CI에서 compile/test/documentation 결과를 확인하고 실패가 있으면 branch에서 수정한다.

## Remaining

- PR #231 initial CI 확인 및 필요한 수정.
- 버전/릴리즈 문서 v1.11.2 정리.
- deterministic tests / Release build / published EXE UI-runtime smoke / map-specific smoke 검증.
- PR ready 전환 및 final exact-head CI 확인.
- main 병합 / exact-main 검증.
- v1.11.2 release/tag/assets 검증.
- 공식 상태 문서 갱신 후 `ACTIVE_WORK` 종료.
