# Farming Guide deterministic rulebook — v1.16.0

Status: **APPROVED / IMPLEMENTED; LOCK SEMANTICS CORRECTED BY v1.16.4**

## Purpose

Farming Guide는 자체적인 가중치 점수나 불투명한 최적화로 행동을 선택하지 않는다. 현재 상태를 분류한 뒤 **제약 → 중요도 → 상황 대처** 순서의 명시적 규칙으로 행동한다.

## Information boundary

- Scanner가 확정하는 것은 Item ID다.
- 프로그램은 현재 Game Content에서 가격, 무게, 크기, storage grid/filter, 장비 호환성 같은 source-backed 사실을 사용할 수 있다.
- 내구도, 남은 사용횟수, 스캔한 개별 인스턴스의 실제 조립상태처럼 Scanner가 알 수 없는 값은 추측하지 않는다.
- 프로그램이 직접 관찰하지 못하는 탄약/음식/음료 소비 등 상태 변화는 사용자가 Farming Guide 상태에 반영한 값을 현재 truth로 사용한다.
- 모든 Scanner recommendation은 현재 raid revision에 묶이며 명시적 accept 전에는 상태를 변경하지 않는다.

## Importance manual

1. 남은 **Found in Raid 필요량**이 있는 아이템은 일반 경제 loot보다 우선한다.
2. 비-FIR 필요 상태는 별도 loot 우선순위를 만들지 않는다.
3. 경제 가치는 **평균 Flea Market 가격**만 사용한다. Trader 판매가는 Farming Guide 경제 비교에 사용하지 않는다.
4. 일반 중요도가 같으면 전체 Flea 가치가 높은 쪽을 우선한다.
5. 전체 Flea 가치가 같고 두 아이템의 무게를 모두 아는 경우 더 가벼운 쪽을 우선한다.
6. 마지막 동률은 더 작은 일반 footprint를 우선한다.
7. 공간 부족으로 기존 물품을 제거해야 할 때는 글로벌 ₽/slot 점수를 쓰지 않고 실제로 희생되는 **전체 victim set의 합산 Flea 가치**와 incoming 결과를 비교한다.
8. FIR 필요 아이템은 자동 희생하지 않는다.

## Equipment manual

- 빈 호환 장비칸은 장착 후보가 될 수 있다.
- 이미 장비가 있는 칸은 가격이 높다는 이유로 교체하지 않는다.
- 방탄복/헬멧: source-backed ArmorClass가 엄격히 높은 경우만 자동 우월.
- 헤드셋: source-backed hearing-distance 값이 엄격히 높은 경우만 자동 우월.
- 일반 리그/가방/보안 컨테이너: source-backed storage capacity가 엄격히 큰 경우만 자동 우월.
- 방탄 리그: ArmorClass를 먼저 비교하고, 같은 등급에서만 storage capacity를 비교한다.
- 총기/권총은 Scanner가 실제 조립/성능 상태를 알 수 없으므로 자동 우월 비교하지 않는다.

## Protected-role inheritance

- **v1.16.4부터 명시적 item lock은 자동 Farming Guide 판단에서 해당 stored item의 물리적 위치까지 고정한다.** 자동 지시는 잠긴 exact instance를 버리거나 교체하거나 다른 수납 공간/좌표로 이동하거나 회전하거나 re-parent할 수 없다.
- 잠긴 descendant가 있는 stored ancestor를 움직이거나 root carrier를 교체하여 잠긴 item을 간접 이동시키는 것도 금지한다.
- 사용자의 직접 편집은 계속 authoritative하다. 위 위치 고정은 자동 recommendation에 대한 제약이다.
- 장착 중인 리그/가방/보안 컨테이너 root lock은 해당 carrier 자체의 자동 교체를 막지만 합법적인 내부 storage 사용까지 막지는 않는다.
- stored container를 item lock한 경우 그 container 자체의 위치는 고정되지만, 내부의 별도로 잠기지 않은 contents는 독립적으로 판단할 수 있다. 단, locked descendant를 간접 이동시키는 ancestor 이동은 허용하지 않는다.
- 수납 장비 교체는 locked stored item의 위치/ancestor/root-carrier 경로를 바꾸지 않는 경우에만 가능하다.
- 예약 칸은 기존 좌표가 아니라 각 grid에서 4방향으로 연결된 shape/capacity role로 취급한다.
- 새 장비에서 모든 잠금 및 예약 제약을 함께 만족할 수 없으면 해당 장비 교체를 금지한다.
- 성공한 교체의 ProposedSnapshot과 migration된 lock/reservation state는 하나의 accept transaction으로 함께 commit한다.

### Historical correction

v1.16.0~v1.16.3 구현과 이전 문서에는 exact item lock을 "인스턴스가 사라지지만 않으면 재배치 가능"으로 해석한 내용이 있었다. 2026-09-02 실제 사용자 보고에서 잠긴 Grizzly를 옮기라는 지시가 확인되었고, 사용자가 확정한 제품 의미와 충돌하므로 이 해석은 폐기한다. 이후 구현·테스트·문서는 위 v1.16.4 위치 잠금 계약을 canonical contract로 사용한다.

## Stack quantity

- Scanner가 Item ID만으로 수량을 알 수 없는 탄약 및 canonical 화폐는 quantity 입력을 요구한다.
- Mini Scanner는 quantity 입력 중에만 keyboard focus를 받고 Enter로 제출한 뒤 일반 overlay 동작으로 복귀한다.
- quantity pending 중 accept hotkey는 상태를 commit하지 않는다. 새 scan은 이전 quantity pending을 폐기한다.
- 저장 item quantity는 Farming Guide state schema v3에 저장하며 legacy missing quantity는 1로 normalize한다.
- quantity는 snapshot count, FIR remaining calculation, stack Flea value, stack weight에 반영한다.

## Weight constraint

- weight setting은 profile별 Strength level 0–51이다.
- 현재 검증한 Tarkov rule: base max 77 kg, Strength level당 +0.6%, Elite는 약 100 kg.
- Elite에서는 sling/back/holster, 즉 Farming Guide의 PrimaryWeapon1/PrimaryWeapon2/Holster 무게를 제외한다. Melee는 제외하지 않는다.
- stimulant처럼 Scanner가 관찰할 수 없는 일시적 modifier는 자동 추정하지 않는다.
- recommendation은 final ProposedSnapshot의 무게를 검사한다. 현재 사용자가 반영한 상태가 이미 한계 초과라면 무게를 더 늘리지 않는 전환만 허용한다.

## MiniMap hotkey cleanup

- bare NumPad0..5 직접 층 선택은 제거한다.
- configurable floor up/down hotkeys는 유지한다.
- transplanted Map의 GlobalKeyboardHookService lifecycle은 호환성을 위해 유지하되 direct floor event는 dispatch하지 않는다.

## Regression contract

- v1.16.0 deterministic tests는 FIR/Flea/stack/weight/equipment/raid atomic-accept/state schema 계약을 고정한다.
- v1.16.4 published EXE decision smoke는 locked-item exact placement, indirect ancestor/root-carrier movement, secure-promotion fallback 및 final fail-closed 경계를 추가로 고정한다.
- Windows published EXE smoke는 기존 Map/MiniMap/Scanner/Farming Guide 제품 계약을 계속 통과해야 한다.
