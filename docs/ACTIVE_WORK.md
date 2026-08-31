# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

`v1.13.0` 신규 기능으로 Scanner 탭 오른쪽에 **파밍 가이드** 탭을 추가하고, 레이드 시작 상태를 구성하는 Loadout / Inventory Editor의 첫 제품 버전을 구현합니다.

## Base / branch / PR

```text
base main: 1e5e687f0f9fdc76db7a083078209222c7cb4ade
public stable: v1.12.1
working branch: feature/v1.13.0-farming-guide-loadout-editor-2026-08-31
Draft PR: #240
latest validated code checkpoint: f225f781bd58ef5de8ee99738dc6139c419ef2f8
```

## Confirmed scope

Canonical product decision:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

핵심 계약:

- Scanner 오른쪽에 `파밍 가이드` 탭을 추가한다.
- 화면은 착용 장비 / 수납 공간 / 검색·요약의 3개 영역으로 구성한다.
- 검색 결과에서 실제 Tarkov `width × height` footprint로 drag한다.
- drag 중 `R`로 90도 회전한다.
- grid snap, bounds/overlap/연속 공간 검증, 초록/빨강 valid-invalid 표시를 사용한다.
- Pocket / Rig / Special Slot / Backpack / Secure Container를 표현한다.
- Rig / Backpack / Secure Container grid와 attachment/armor plate/filter/conflict 구조는 current validated Tarkov item source를 사용한다.
- 근접무기와 PMC 인식표는 per-profile preset과 분리된 사용자 고정 설정이다.
- preset은 장비, attachment, armor plate, carrier, stored item, grid 위치/회전을 포함한 전체 출발 상태를 보존한다.
- 오른쪽 요약은 총 무게 / storage cell 사용량을 표시하며 `파밍한 가치`는 이번 slice에서 `—`이다.
- 실제 Tarkov inventory 좌표의 지속적인 1:1 동기화는 하지 않는다.
- v1.13.0에는 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동을 포함하지 않는다.

## Completed

- 제품 요구사항 확정 및 canonical decision 기록
- feature branch / Draft PR #240 생성
- Scanner 오른쪽 `파밍 가이드` 탭과 3열 editor UI 구현
- Farming Guide를 MainWindow first-class section lifecycle에 통합
- equipment / carrier / pockets / special slot / grid rendering 구현
- 검색 결과 기반 drag-and-drop 구현
- `R` 회전 / footprint / overlap / bounds / snap / valid-invalid feedback 구현
- 장비 attachment / armor plate nested 설정 UI 구현
- fixed melee / dogtag 별도 persistence 구현
- 전체 출발 상태 preset save/select/working-state persistence 구현
- `%LocalAppData%/JunhyunHelper/farming-guide.json` schema v1 추가
- current Tarkov item importer에 storage grids / filters / slots / armor slots / conflicts / blocks-headphones 구조 추가
- Content snapshot write schema를 v9로 확장하고 v3-v8 read compatibility 유지
- Desktop target/package version을 `1.13.0`으로 반영
- `FIRST_RUN_KO.txt`와 `RELEASE_NOTES_V1.13.0.md` 추가
- published EXE Product UI smoke에 Farming Guide 실제 render/section activation 검증 추가
- 결정적 회귀 테스트 추가
  - placement / rotation / overlap / fragmented contiguous-space
  - MainWindow section lifecycle
  - preset full-state round-trip / fixed equipment separation
  - Tarkov item structure importer
  - Content v9 round-trip
- 추가 코드 감사에서 carrier/state data-loss 경로 수정
  - 내용물이 든 carrier를 다른 carrier로 덮어쓰는 drop을 fail closed
  - 오래된 preset의 없는 grid / out-of-bounds / overlap / current filter 위반 placement를 current content 기준으로 제거
  - 위 동작의 deterministic tests 추가
- `f225f781...` 기준 Desktop Release build 성공
- `f225f781...` 기준 deterministic tests 성공
- `f225f781...` 기준 Shutdown Race CI `33358155538` 성공
- Documentation Consistency 정상화 완료
- current public Tarkov API/schema에서 Farming Guide가 사용하는 width/height/properties/storage grids/slot filters/armor slots/conflict 필드 존재 확인

## Current step

`f225f781bd58ef5de8ee99738dc6139c419ef2f8`에서 main CI `33358155536`가 진행 중입니다.

현재까지 같은 run에서:

- Desktop Release build: SUCCESS
- deterministic tests: SUCCESS
- Windows x64 publish: 진행 중

다음 gate는 actual published EXE Product UI + Map smoke와 package/artifact 검증입니다.

이 checkpoint 문서 커밋 이후에는 동일 코드 + 최신 문서를 대상으로 새 exact-head PR CI를 다시 통과시켜야 합니다.

## Remaining

1. published EXE Farming Guide UI/runtime smoke 및 package/artifact gate 확인
2. v1.13.0 current-state / architecture / decision index 문서 마감
3. final exact-head PR CI / Shutdown Race / Documentation Consistency
4. PR ready / main merge
5. exact-main CI / Shutdown Race / Documentation Consistency
6. v1.13.0 release workflow 실행
7. public tag / latest release / asset / checksum / exact-source identity 검증
8. PROJECT_STATE / CURRENT_STATE / STATE / README / release evidence를 public v1.13.0으로 확정
9. ACTIVE_WORK `NONE`으로 종료
