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
validated work head before this checkpoint: 6f76b06a169690c20a4413d8f19cd25c8c6a5f06
```

## Confirmed scope

### 탭 / 화면 구조

- 기존 Scanner 탭의 오른쪽에 `파밍 가이드` 탭을 추가한다.
- 파밍 가이드 화면은 크게 착용 장비 / 수납 공간 / 검색·요약 영역으로 구성한다.
- 이번 단계에서는 파밍 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동은 구현하지 않는다.

### 착용 장비

- 사용자가 검색 결과에서 아이템을 드래그하여 장비 슬롯에 장착한다.
- 대상에는 헤드셋, 헬멧, 얼굴/페이스커버, 완장, 방탄복/아머드 리그, 안경, 무기 1/2, 권총, 근접무기 등 현재 제품 설계에 필요한 착용 장비가 포함된다.
- 칼과 PMC 인식표는 레이드마다 교체하는 대상이 아니므로 사용자 고정 설정으로 취급한다.
- 장비 내부 개조/교체형 방탄판은 장착된 장비의 별도 설정 UI에서 편집한다.

### 수납 공간

- Pocket / Rig / Backpack / Secure Container / Special Slot 등 Tarkov 수납 구조를 표현한다.
- 그리드 한 칸의 화면 크기는 전역적으로 동일하다.
- 장비별 실제 grid 구성은 current validated item source에서 가져온다.
- 아이템은 실제 Tarkov `width × height`에 맞는 크기로 표시한다.

### 검색 / drag-and-drop

- 모든 신규 장비/아이템 추가는 오른쪽 검색 결과에서 시작한다.
- drag preview는 실제 아이템 footprint를 사용한다.
- drag 중 `R` 키로 90도 회전한다.
- 유효/불가 배치는 각각 초록/빨강으로 표시한다.
- grid 주변에는 snap 허용 오차를 적용한다.
- 명백한 빈 영역 drop은 기존 배치 아이템 제거로 처리한다.
- carrier 내부 아이템을 잃지 않도록, 내부가 채워진 carrier를 일반 수납 아이템처럼 옮기는 동작은 현재 모델에서 허용하지 않는다.

### 프리셋

- selector는 현재 프리셋 이름 또는 `프리셋 선택`을 표시한다.
- 저장된 프리셋 선택 시 전체 출발 상태를 복원한다.
- 저장 UI에서 이름을 입력해 현재 출발 상태를 새 프리셋으로 저장한다.
- 프리셋에는 착용 장비, 내부 개조, 방탄판, 수납 아이템, grid 위치/회전 등이 포함된다.
- 불러온 상태를 수정하면 원본 프리셋 선택 상태를 해제한다.
- 근접무기와 PMC 인식표 고정 설정은 per-profile preset과 분리한다.

### 요약 정보

오른쪽 하단에는 현재 Loadout / Inventory 요약을 표시한다.

- 파밍한 가치: 이번 slice에서는 `—`
- 총 무게
- 수납 공간 사용량 / 총량

cell 수는 참고값이며 실제 대형 아이템 수납 가능 여부는 연속 공간/packing 문제로 별도 판단한다.

### 레이드 중 상태 의미

- 이 UI는 실제 인게임 grid 좌표를 실시간 1:1 동기화하는 화면이 아니다.
- 주 목적은 출발 장비, 점유 공간, 보유 아이템, 수납 가능 공간을 제품이 알 수 있게 하는 것이다.
- 향후 판단 엔진은 자체 packing 계산으로 어느 공간에 넣을지를 안내한다.
- 레이드 session 상태와 재사용 가능한 출발 preset은 분리한다.

## Completed

- 제품 요구사항 확정 및 feature branch / Draft PR #240 생성
- Scanner 오른쪽 `파밍 가이드` 탭과 3열 editor UI 구현
- Farming Guide를 MainWindow의 first-class section lifecycle에 통합
  - profile 없음 / section visibility / busy / button state 포함
- equipment / carrier / pockets / special slot / grid rendering 구현
- 검색 결과 기반 drag-and-drop 구현
- `R` 회전 / footprint / overlap / bounds / snap / valid-invalid feedback 구현
- carrier 이동 시 내부 배치 보존 및 destructive removal 계약 정리
- 고정 근접무기/인식표의 별도 persistence 구현
- 전체 출발 상태 preset 저장/선택/working-state persistence 구현
- 장비 attachment / armor plate 설정 UI 구현
- current Tarkov item source importer에 storage grids / filters / slots / armor slots / conflicts / blocks-headphones 데이터 추가
- content schema를 v9로 확장하고 v3-v8 offline snapshot read compatibility 유지
- 결정적 테스트 추가
  - placement / rotation / overlap / contiguous packing
  - MainWindow section lifecycle
  - preset round-trip / mods / armor plates / position / rotation / fixed equipment separation
- Desktop build/XAML compile 성공 확인
- 잘못 구성된 fragmented-space test fixture 수정

## Current step

최신 기능 HEAD의 PR CI / Shutdown Race CI가 실행 중입니다. Documentation Consistency는 성공했습니다.

CI가 성공하면 published Windows x64 runtime에서 새 Farming Guide 탭을 직접 여는 UI smoke coverage를 확인·보강하고, 제품 문서/버전 갱신 후 merge/release 검증으로 진행합니다.

## Remaining

1. 최신 PR CI / Shutdown Race CI 완료 확인 및 잔여 실패 수정
2. published EXE Farming Guide UI/runtime smoke 확인·보강
3. v1.13.0 제품/아키텍처/상태/릴리즈 문서 갱신
4. assembly/package version `1.13.0` 반영
5. final exact-head PR CI
6. PR ready / main merge
7. exact-main CI / Shutdown Race / Documentation Consistency
8. v1.13.0 release 생성 및 public tag/release/asset/checksum 검증
9. canonical project state 갱신 및 ACTIVE_WORK 종료
