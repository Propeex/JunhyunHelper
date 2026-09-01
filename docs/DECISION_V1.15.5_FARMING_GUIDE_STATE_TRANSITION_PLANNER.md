# DECISION — v1.15.5 Farming Guide State-Transition Planner

Date: **2026-09-01 KST**  
Status: **CONFIRMED / IMPLEMENTATION IN PROGRESS**

## Context

Farming Guide의 기존 raid planner는 Scanner로 새로 확인된 아이템 하나를 현재 상태에 추가하는 문제를 중심으로 발전했다. v1.15.4에서 direct fit, nested storage, non-destructive repacking, source-backed equipment upgrades를 추가했지만, 장비 교체 결과로 벗겨진 기존 장비를 새 파밍 후보로 되돌리지 않았다.

이 가정은 실제 Tarkov 인벤토리 판단과 맞지 않는다. 예를 들어 더 좋은 리그나 가방을 장착한 뒤 기존 리그/가방 자체를 다른 가방에 넣고, 그 내부 공간에 다른 아이템을 옮기는 편이 더 많은 가치와 수납공간을 보존할 수 있다. 일반 방탄복, 헬멧, 헤드셋, 총기도 교체 즉시 소멸하는 것이 아니라 보관 가능한 전리품이 된다.

또한 accepted-item 누적 카운터처럼 과거 동작을 별도 상태로 기억하면, 나중에 해당 아이템을 버린 뒤에도 Needed 수량이 충족된 것으로 오판할 수 있다.

따라서 Farming Guide의 판단 단위를 `이번에 스캔한 아이템`이 아니라 **이번 스캔을 반영한 뒤 도달 가능한 전체 raid inventory 상태**로 확장한다.

## Architectural principle

향후 파밍 로직을 쉽게 변경할 수 있도록 다음 책임을 분리한다.

1. **Facts / capabilities**
   - Scanner와 canonical Game Content가 Item ID, Needed, 가격, 크기, 무게, 장비/수납 구조, filter/conflict 등 확인 가능한 사실을 제공한다.
   - 확인되지 않은 live durability, plate 상태, stack 수량, 플레이어별 실제 carry limit 등은 추측하지 않는다.

2. **Legality / geometry**
   - `FarmingGuideCompatibility`, `FarmingGuideStoragePlacementPolicy`, placement/repacking planner가 무엇을 어디에 둘 수 있는지만 판단한다.
   - 이 계층은 가격이나 "왜 보존해야 하는가"를 알지 않는다.

3. **State transitions**
   - 장착, 교체, 벗겨진 장비 반환, carrier nesting, 기존 아이템 이동/회전, 필요한 경우 제한된 폐기 후보를 조합해 가능한 candidate snapshot을 만든다.
   - 장비 교체는 기존 아이템 삭제가 아니라 **displaced loot 생성**으로 모델링한다.

4. **Retention / value policy**
   - Candidate 간 우선순위와 어떤 아이템을 최후에 버릴 수 있는지를 별도 policy가 판단한다.
   - 현재 Needed/시장가/공간 효율 계약을 보존하면서 container utility와 known weight를 additive signal로 다룰 수 있게 한다.
   - 정책은 placement mechanics와 독립적으로 교체 가능해야 한다.

5. **Presentation**
   - 최종 ProposedSnapshot의 차이만 사용해 짧은 사용자 지시를 생성한다.
   - planner 내부 search/repacking 세부사항을 사용자에게 노출하지 않는다.

## Decision 1 — displaced equipment returns to the loot pool

장비 교체 시 기존 장비는 즉시 폐기하지 않는다.

```text
incoming equipment/carrier acquired
→ candidate equipment transition
→ displaced previous equipment/carrier becomes a movable loot candidate
→ legal storage/repacking/nesting search
→ preserve when a better complete state exists
→ discard only if retention policy proves that sacrificing it is preferable/necessary
```

이 계약은 특정 리그 예외가 아니라 다음에 공통 적용한다.

- ordinary equipment slots
- PrimaryWeapon / Holster
- Rig
- Backpack
- body armor + ordinary rig → armored rig atomic transition

Secure Container는 raid 중 일반 loot처럼 교체하는 것을 별도로 적극 추천하지 않으며 기존 source-backed compatibility/lock 안전 규칙을 유지한다.

## Decision 2 — containers are both loot and storage

Rig/Backpack 등 storage grid를 가진 displaced item은 외부 footprint만 가진 물건이 아니다.

Planner는 해당 컨테이너가 다른 합법적 storage에 들어간 candidate에서 그 컨테이너의 내부 grid도 동일 candidate의 storage surface로 사용할 수 있어야 한다.

예:

```text
새 리그 장착
→ 기존 리그를 가방에 보관
→ 가방의 기존 작은 아이템을 기존 리그 내부로 이동
```

이 작업은 여러 번의 사용자 스캔을 기다리는 임시 상태가 아니라 하나의 pending recommendation / ProposedSnapshot으로 표현한다.

Nested graph는 다음을 반드시 만족한다.

- source filter를 통과한다.
- parent가 실제 candidate snapshot에 존재한다.
- 자기 자신/자손 내부로 들어가는 cycle이 없다.
- lock / reserved-cell 계약을 침범하지 않는다.

## Decision 3 — preservation before destruction remains the governing order

v1.15.4의 보존 우선 철학을 강화한다.

1. legal empty equipment target
2. source-proven equipment upgrade candidate
3. direct legal storage
4. non-destructive global repacking
5. displaced-equipment preservation / container nesting
6. value-aware bounded eviction + repacking
7. discard only when no better legal retained state exists

장비 업그레이드 자체가 유리하더라도 벗겨진 기존 장비의 가치가 자동으로 0이 되지 않는다.

## Decision 4 — destructive replacement may require more than one victim

큰 고가 아이템 하나를 보존하려면 여러 개의 저가 아이템을 비워야 할 수 있다. 기존의 `한 아이템 제거 후 재시도` 모델에 제한하지 않는다.

다만 검색 폭발과 불안정한 결과를 방지하기 위해 destructive search는 다음과 같이 bounded / deterministic하게 유지한다.

- locked item/subtree, reserved-parent, populated container는 자동 폐기 후보가 아니다.
- eviction 후보는 retention policy의 낮은 순위부터 안정적으로 정렬한다.
- 작은 prefix 집합부터 누적 제거하고 각 단계에서 동일 non-destructive repacking solver를 재사용한다.
- incoming/보존 대상이 eviction set보다 명확히 우월한 경우에만 destructive candidate를 허용한다.
- 최대 victim 수 / search nodes는 명시적 상수로 제한한다.

이 정책은 별도 Core policy boundary에 둬 향후 점수 체계 변경이 placement solver를 흔들지 않게 한다.

## Decision 5 — Needed truth is derived from current raid state

`accepted scan count`를 별도 누적 truth로 사용하지 않는다.

현재 raid에서 획득하여 아직 보유 중인 수량은 다음의 snapshot delta로 계산한다.

```text
current snapshot count(Item ID) - raid baseline snapshot count(Item ID)
```

count는 다음을 재귀적으로 포함한다.

- equipment
- rig / backpack / secure container
- stored items
- attachment tree
- armor plate tree

따라서 이전에 획득한 Needed item을 나중에 버리면 다음 스캔에서 다시 Needed로 평가된다.

## Decision 6 — weight is a known signal, not an invented hard limit

Canonical Game Content에 `WeightKg`가 있으면 알려진 item fact로 사용할 수 있다. 그러나 현재 Farming Guide state에는 PMC의 실시간 strength/status 기반 carry limit가 없다.

따라서 v1.15.5에서는:

- weight를 policy가 참조할 수 있는 additive metadata로 유지한다.
- 동일한 가치/필요도 선택에서 더 가벼운 상태를 선호할 수 있다.
- 확인되지 않은 "몇 kg 이상이면 버려라" 같은 hard threshold는 만들지 않는다.
- 향후 Scanner/사용자 설정 등 authoritative weight budget이 추가되면 policy input에 주입할 수 있게 한다.

## Decision 7 — unsupported live facts fail conservatively

현재 snapshot이 표현하지 않는 사실은 임의 계산하지 않는다.

- durability / armor current HP
- actual installed plate condition beyond modeled item structure
- stack quantity / partial ammo stack
- current hydration/energy/HP
- player-specific live carry threshold
- weapon magazine ammo count / chamber state

이 정보가 필요한 결정을 source-backed하게 증명할 수 없다면 자동으로 공격적인 교체를 확대하지 않는다. 각 항목은 향후 fact provider를 추가할 수 있는 extension point로 남긴다.

## Decision 8 — tactical reserve policy remains extensible

사용자가 이미 제품 방향으로 지정한 최소 음식/음료, 현재 사용 총기의 탄약, 재장전용 빈 공간 등 tactical reserve는 일반 시장가만으로 희생하면 안 된다.

이번 구조 변경에서는 이 정보를 geometry/value 계층에 하드코딩하지 않는다. 별도의 retention constraint / policy input으로 표현할 수 있게 경계를 유지한다. 실제 source/state가 충분히 연결된 항목부터 deterministic하게 적용한다.

## Implementation contract

### Core

- Repacking parent-graph validation은 현재 incoming item이 container인 경우 그 내부 surface를 candidate parent로 허용한다.
- incoming↔existing 사이 parent cycle은 계속 fail closed한다.
- snapshot inventory counter를 Core에 둔다.
- destructive eviction-set 비교는 별도 retention policy에 둔다.

### Desktop orchestration

- historical v1.15.4 planner를 무리하게 비대화하지 않고 v1.15.5 transition orchestration을 별도 partial file에 둔다.
- proposed root carrier/equipment snapshot을 기준으로 storage surfaces를 계산할 수 있어야 한다.
- displaced carrier는 stable synthetic instance ID를 가진 stored candidate로 투영하고 원래 contents를 그 parent 아래로 reparent할 수 있어야 한다.
- 최종 recommendation은 기존 `FarmingGuidePendingInstruction` transaction 계약을 그대로 사용한다.

### Presentation

기존 v1.15.5 compact wording 계약을 유지한다.

실제로 다른 storage area로 이동하거나 버리는 조작만 `+` 부가 지시로 노출하며, 같은 storage area 내부의 단순 grid/X/Y/rotation 재정렬은 숨긴다.

## Regression scenarios

최소 다음을 deterministic/runtime contract로 고정한다.

1. 더 좋은 일반 장비 교체 후 기존 장비가 공간이 있으면 보관되고 사라지지 않는다.
2. 새 리그 장착 후 기존 리그를 가방에 넣고 가방 blocker를 그 기존 리그 내부로 이동할 수 있다.
3. 새 가방 장착 후 기존 가방도 동일 원칙으로 nested 보존 가능하다.
4. incoming container 내부 surface를 사용하되 자기/자손 cycle은 생성하지 않는다.
5. 하나의 큰 고가 아이템을 위해 여러 저가 leaf를 버리는 편이 명확히 유리한 경우 bounded multi-eviction이 가능하다.
6. populated/locked container 및 locked subtree는 자동 폐기하지 않는다.
7. Needed item을 획득했다가 이후 버리면 Needed count가 다시 복구된다.
8. same-storage-area 재배치 presentation suppression은 유지된다.
9. existing v1.15.4 equipment superiority / dedicated-container / nested-filter / reserved-cell contracts가 회귀하지 않는다.

## Consequences

- Farming Guide는 개별 아이템에 대한 if/else 집합보다 **candidate state transition engine**에 가까워진다.
- 장비 종류가 추가되어도 displacement → preservation → retention이라는 공통 경로를 재사용할 수 있다.
- 가치 정책을 수정해도 legal placement/repacking solver를 다시 작성할 필요가 없다.
- 향후 weight budget, tactical reserve, durability/stack facts가 추가되어도 fact/policy input을 확장하는 방식으로 연결할 수 있다.
- 계산량 증가는 bounded search와 deterministic candidate ordering으로 통제한다.
