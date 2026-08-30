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
PR: not created yet
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

## Completed

- 공식 v1.11.1 stable / main / release source / maintenance contracts 복구.
- 작업 branch 생성.
- 사용자 요구사항 3건을 maintenance scope로 checkpoint에 기록.

## Current Step

관련 제품 문서와 실제 구현/테스트를 추적해 각 증상의 root cause와 영향 범위를 확인한다.

## Remaining

- Scanner correction hotkey → correction window open 경로 추적 및 회귀 테스트 작성.
- Quest/Ammo/Scanner search clear 구현과 Items/Hideout 구현 비교.
- Map/MiniMap player marker coordinate/heading transform 전체 audit, Factory 회귀 재현.
- 최소 수정 구현.
- deterministic tests / Release build / published EXE UI-runtime smoke / map-specific smoke 검증.
- PR 생성 및 CI 처리.
- main 병합 / exact-main 검증.
- v1.11.2 release/tag/assets 검증.
- 공식 상태 문서 갱신 후 `ACTIVE_WORK` 종료.
