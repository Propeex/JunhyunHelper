# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

`v1.13.0` 신규 기능으로 Scanner 탭 오른쪽에 **파밍 가이드** 탭을 추가하고, 레이드 시작 상태를 구성하는 Loadout / Inventory Editor의 첫 제품 버전을 구현합니다.

## Base / branch

```text
base main: 1e5e687f0f9fdc76db7a083078209222c7cb4ade
public stable: v1.12.1
working branch: feature/v1.13.0-farming-guide-loadout-editor-2026-08-31
```

## Confirmed scope

### 탭 / 화면 구조

- 기존 Scanner 탭의 오른쪽에 `파밍 가이드` 탭을 추가한다.
- 파밍 가이드 화면은 크게 착용 장비 / 수납 공간 / 검색·요약 영역으로 구성한다.
- 이번 단계에서는 파밍 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동은 구현하지 않는다.

### 착용 장비

- 사용자가 검색 결과에서 아이템을 드래그하여 장비 슬롯에 장착한다.
- 대상에는 헤드셋, 헬멧, 얼굴/페이스커버, 완장, 방탄복/아머드 리그, 안경, 무기 1/2, 권총, 근접무기 등 현재 제품 설계에 필요한 착용 장비가 포함된다.
- 칼과 PMC 인식표는 레이드마다 교체하는 대상이 아니므로 사용자 설정값을 고정 표시하는 방향으로 취급한다.
- 장비 내부 개조/슬롯은 장착된 장비를 선택하면 별도 상세/개조 UI에서 편집하는 자연스러운 방식으로 시작하고, 세부 UX는 이후 실사용에 따라 조정한다.

### 수납 공간

- Pocket / Rig / Backpack / Secure Container / Special Slot 등 실제 Tarkov 수납 구조를 표현한다.
- 그리드 한 칸의 화면 크기는 전역적으로 동일해야 한다.
- 장비에 따라 전체 영역의 크기와 grid 구성은 달라질 수 있다.
- 아이템은 실제 Tarkov `width × height`에 맞는 크기로 표시한다.

### 검색 / drag-and-drop

- 모든 신규 장비/아이템 추가는 오른쪽 검색창에서 검색 후 결과 목록에서 시작한다.
- 검색 결과의 아이템을 끌면 실제 아이템 크기와 같은 grid 크기의 drag preview가 커서에 따라 움직인다.
- drag 중 `R` 키로 90도 회전한다.
- 유효한 배치 후보는 초록색, 불가능한 후보는 빨간색으로 표시한다.
- 픽셀 단위 정확도를 요구하지 않고 인접 grid로 snap하는 허용 오차를 둔다.
- 명백히 grid 밖의 빈 영역에 drop하면 해당 배치 아이템을 제거한다.
- grid 근처의 경미한 오차는 삭제보다 snap을 우선한다.

### 프리셋

- 상단 preset selector는 현재 선택한 프리셋 이름을 표시한다.
- 선택된 프리셋이 없으면 `프리셋 선택`을 표시한다.
- 클릭하면 저장된 프리셋 목록을 dropdown으로 표시하고 선택 시 전체 출발 상태를 복원한다.
- 우측 저장 아이콘은 별도 창을 열어 프리셋 이름을 입력받고 현재 전체 출발 상태를 새 프리셋으로 저장한다.
- 프리셋에는 착용 장비, 내부 개조, 방탄판, 수납 아이템, 위치/회전 등 레이드 출발 상태 전체를 저장한다.
- 불러온 프리셋을 사용자가 수정하면 더 이상 원본 프리셋과 동일하지 않은 상태로 간주하고 selector를 `프리셋 선택` 상태로 돌린다.

### 요약 정보

오른쪽 하단의 정보 영역은 선택 아이템 상세가 아니라 현재 Loadout / Inventory 요약을 표시한다.

초기 항목:

- 파밍한 가치
- 총 무게
- 수납 공간 사용량 / 총량

수납 공간 값은 cell 참고값이며, 실제 대형 아이템 수납 가능 여부는 연속 공간/packing 문제와 별개임을 전제로 한다.

### 레이드 중 상태 의미

- 이 UI는 레이드 중 실제 인게임 grid 좌표를 계속 1:1 동기화하기 위한 화면이 아니다.
- 주 목적은 프로그램에 사용자의 출발 장비, 점유 공간, 보유 아이템과 수납 가능 공간을 알려주는 것이다.
- 향후 파밍 가이드 판단 엔진은 실제 인게임 좌표를 강제 동기화하지 않고 자체 packing 계산으로 `어느 장비/공간에 넣을지`를 안내한다.
- 레이드 종료 시 session 상태는 초기화하고 재사용 가능한 출발 프리셋은 보존하는 방향이다.

## Completed

- 사용자 제품 의도 및 첫 구현 범위 확정
- `v1.13.0` feature branch 생성
- ACTIVE_WORK 시작

## Current step

- 현재 WPF 탭 구조, Scanner 오른쪽 탭 배치 위치, 아이템 데이터 모델/검색 서비스, drag/drop 재사용 가능 구성요소, user persistence 구조를 확인한다.
- 이후 첫 구현과 결정적 테스트를 추가한다.

## Remaining

1. 관련 공식 문서/코드 구조 확인
2. 파밍 가이드 product/architecture 세부 설계 반영
3. 탭 및 Loadout Editor UI 구현
4. item/grid/placement/preset persistence 구현
5. 장비 슬롯 및 초기 mod-detail flow 구현
6. 검색 결과 → drag/drop / R 회전 / snap / valid-invalid highlight 구현
7. 요약 정보 계산 구현
8. 결정적 테스트 추가 및 전체 테스트
9. Release build / published EXE WPF smoke
10. PR / CI / main merge / exact-main 검증
11. v1.13.0 release와 공식 문서 갱신
