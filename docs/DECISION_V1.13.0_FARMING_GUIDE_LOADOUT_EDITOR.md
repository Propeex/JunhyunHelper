# DECISION — v1.13.0 Farming Guide Loadout / Inventory Editor

Date: **2026-08-31 KST**  
Status: **CONFIRMED / IMPLEMENTING**  
Target release: **v1.13.0**

## 1. Context

사용자는 향후 레이드 중 파밍 가치 판단과 획득/폐기/교체 추천을 만들기 전에, 준현 헬퍼가 **레이드 출발 시점의 장비와 수납 가능 공간을 정확히 알 수 있는 기반 화면**을 먼저 원한다.

첫 단계의 목적은 인게임 inventory를 실시간 복제하는 것이 아니라 사용자가 출발 상태를 편집·저장하고, 제품이 그 상태를 이후 판단 엔진의 입력으로 사용할 수 있게 하는 것이다.

## 2. Confirmed product behavior

### Main navigation

- 기존 Scanner 탭 바로 오른쪽에 `파밍 가이드` 탭을 둔다.
- 화면은 착용 장비 / 수납 공간 / 검색·요약의 3개 영역으로 구성한다.

### Equipment

- 검색 결과에서 아이템을 drag하여 장비 슬롯에 장착한다.
- 첫 제품 범위에는 헤드셋, 헬멧, 얼굴/페이스커버, 완장, 방탄복, 안경, 주무기 1/2, 권총, 근접무기, PMC 인식표를 포함한다.
- 근접무기와 PMC 인식표는 매 레이드 프리셋에 반복 저장할 대상이 아니라 **사용자 고정 설정**으로 취급하며 per-profile preset과 분리한다.
- 장비가 attachment slot 또는 교체형 armor plate를 제공하면 별도 장비 설정 UI에서 호환 항목을 편집한다.
- 하위 부품이 다시 설정 가능한 slot을 가지면 같은 방식으로 nested configuration을 허용한다.

### Storage

- Pocket / Rig / Special Slot / Backpack / Secure Container를 표현한다.
- 모든 grid cell은 동일한 화면 단위를 사용한다.
- 포켓과 특수 슬롯은 고정 cell 구조로 제공한다.
- Rig / Backpack / Secure Container의 실제 grid 구성은 current validated Tarkov item source에서 가져온다.
- 아이템 footprint는 canonical item `width × height`를 사용한다.
- 단순 남은 cell 수와 실제 배치 가능성은 동일하지 않다. 배치 가능 여부는 bounds / overlap / 연속 공간을 기준으로 판정한다.

### Search and drag/drop

- 신규 아이템 추가는 오른쪽 검색 결과에서 시작한다.
- drag preview는 실제 footprint 크기를 사용한다.
- drag 중 `R` 키로 90도 회전한다.
- 유효 후보는 초록색, 불가능 후보는 빨간색으로 표시한다.
- grid 근처의 경미한 오차는 삭제보다 인접 cell snap을 우선한다.
- 명백한 grid 밖 빈 영역에 기존 배치 아이템을 drop하면 제거한다.
- 내부 수납 아이템을 가진 carrier를 일반 수납 아이템처럼 옮겨 내부 상태가 소실될 수 있는 동작은 허용하지 않는다.

### Presets and persistence

- 상단 selector는 선택한 preset 이름을 표시하고, 선택 상태가 없으면 `프리셋 선택`을 표시한다.
- 저장 아이콘을 누르면 이름을 입력해 현재 전체 출발 상태를 새 preset으로 저장한다.
- preset은 착용 장비, attachment, armor plate, carrier, stored item, grid index/position, rotation을 보존한다.
- preset 선택 시 전체 출발 상태를 복원한다.
- 불러온 preset을 수정하면 더 이상 원본과 동일한 상태가 아니므로 selector를 `프리셋 선택` 상태로 되돌린다.
- per-profile working state와 preset은 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 저장한다.
- 근접무기/PMC 인식표 fixed equipment는 같은 문서의 user-level fixed state로 저장하며 profile preset과 분리한다.

### Summary

오른쪽 하단에는 선택 아이템 상세 대신 현재 Loadout / Inventory 전체 요약을 표시한다.

- 총 무게
- 사용 중인 storage cell / 전체 storage cell
- 파밍한 가치

v1.13.0 첫 slice에서 `파밍한 가치`는 아직 판단 정책이 없으므로 `—`로 표시한다.

## 3. Deliberately out of scope for v1.13.0

다음은 이 결정의 첫 구현 범위에 포함하지 않는다.

- 아이템 가치 판단 정책
- 획득 추천
- 폐기 추천
- 교체 추천
- Scanner 인식 결과와 실시간 파밍 추천 연결
- 실제 Tarkov inventory grid 좌표의 지속적인 1:1 동기화
- 완전한 raid session lifecycle/종료 감지

이후 추천 기능은 이 editor가 제공하는 출발 상태와 자체 packing 계산을 입력으로 사용하며, 인게임 좌표를 계속 강제 동기화하는 것을 전제로 하지 않는다.

## 4. Game Content contract

Farming Guide가 필요한 구조 정보는 기존 validated Tarkov item source에서 canonical item model로 가져온다.

- width / height / weight
- item property type
- storage grids
- grid allow/exclude filters
- attachment slots와 filters
- armor slots와 allowed plates
- conflicting item / slot metadata
- headphones block 여부
- armored-rig 판별에 필요한 구조

이 optional metadata 추가로 Content snapshot write schema는 **v9**가 된다.

```text
write: v9
readable: v3~v9
```

v3~v8 last-known-good snapshot은 offline compatibility를 위해 계속 읽을 수 있다. 구형 snapshot에 v9 optional 구조가 없으면 기존 기능은 유지하며, 해당 구조에 의존하는 Farming Guide 부분만 사용할 수 있는 정보 범위에 맞게 제한된다.

## 5. Persistence contract

Farming Guide 사용자 상태는 Game Content와 분리한다.

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema: v1
```

- atomic JSON store를 사용한다.
- profile별 working snapshot / selected preset / preset collection을 저장한다.
- fixed melee/dogtag는 profile preset과 분리한다.
- remote Game Content update가 Farming Guide 사용자 preset을 덮어쓰지 않는다.

## 6. Safety / architecture boundary

Farming Guide는 준현 헬퍼의 다른 기능과 동일하게 외부 보조 프로그램 경계를 유지한다.

- game memory read 없음
- DLL/code injection 없음
- game/process hook 없음
- input automation 없음
- packet/network manipulation 없음
- anti-cheat bypass 없음

현재 화면은 사용자가 직접 구성하는 local editor이며 Tarkov 프로세스 내부 상태를 읽어 자동 동기화하지 않는다.

## 7. Verification contract

v1.13.0 완료 조건에는 최소 다음이 포함된다.

- placement/rotation/bounds/overlap/연속 공간 deterministic tests
- preset full-state round-trip 및 fixed-equipment separation tests
- Tarkov item structure importer tests
- Content snapshot v9 round-trip / v3~v8 compatibility
- MainWindow section lifecycle contract tests
- Windows Release build
- self-contained publish
- actual published EXE에서 Farming Guide 탭을 실제로 열고 rendered layout을 확인하는 runtime smoke
- graceful shutdown / Shutdown Race 회귀 유지
- package / CI / exact-main / public release identity 검증

## 8. Authority

이 결정은 v1.13.0 Farming Guide 첫 slice의 제품 의미에 대한 canonical decision이다.

세부 진행 상태는 `docs/ACTIVE_WORK.md`, current release/state는 `docs/PROJECT_STATE.json` / `docs/CURRENT_STATE.md` / `docs/STATE.md`, 실제 구현은 `src/JunhyunHelper.*`와 결정적 테스트를 따른다.
