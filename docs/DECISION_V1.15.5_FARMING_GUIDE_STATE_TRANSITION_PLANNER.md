# DECISION — v1.15.5 Farming Guide State-Transition Planner

Date: **2026-09-01 KST**  
Status: **CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED**

## Context

Farming Guide raid planner는 Scanner로 확인된 incoming item 하나를 추가하는 문제에서 출발했다. 장비 교체 시 벗겨진 기존 장비를 사라진 것으로 취급하면 실제 Tarkov의 보존 가능한 loot과 storage capacity를 잃는다. Historical accepted-scan counter 역시 이후 폐기된 Needed item을 계속 보유한 것으로 오판할 수 있다.

따라서 v1.15.5의 판단 단위는 **이번 스캔을 반영한 뒤 도달 가능한 전체 modeled raid inventory state**다.

## Architecture

책임은 다음처럼 분리한다.

1. **Facts / capabilities** — Scanner와 validated Game Content가 확인 가능한 item facts와 equipment/storage structure를 제공한다.
2. **Legality / geometry** — compatibility, placement, repacking 계층은 무엇을 어디에 둘 수 있는지만 판단한다.
3. **State transitions** — 장착/교체, displaced equipment 반환, carrier nesting, item 이동/회전, bounded eviction을 조합해 candidate snapshot을 만든다.
4. **Retention policy** — candidate 간 보존 우선순위와 destructive eligibility를 placement mechanics와 분리해 판단한다.
5. **Presentation** — 최종 snapshot 차이에서 짧은 사용자 지시만 만든다.

## Decision 1 — displaced equipment returns to the loot pool

장비 교체 시 기존 장비는 즉시 폐기하지 않는다.

```text
incoming equipment/carrier acquired
→ candidate equipment transition
→ displaced previous equipment/carrier becomes movable loot
→ legal storage/repacking/nesting search
→ preserve when a better complete state exists
→ discard only when retention policy proves sacrifice preferable/necessary
```

이 계약은 ordinary equipment slots, PrimaryWeapon/Holster, Rig, Backpack, body armor + ordinary rig → armored rig atomic transition에 공통 적용한다. Secure Container는 기존 compatibility/lock 규칙 아래 보수적으로 유지한다.

## Decision 2 — containers are both loot and storage

Storage grid를 가진 displaced rig/backpack은 다른 legal storage 안에 들어간 뒤에도 그 내부 grid가 동일 candidate snapshot의 storage surface가 될 수 있다.

예:

```text
새 리그 장착
→ 기존 리그를 가방에 보관
→ 가방의 blocker를 기존 리그 내부로 이동
```

Parent graph는 source filter, parent existence, self/descendant cycle prohibition, locks, reserved cells, canonical sanitizer를 계속 만족해야 한다.

## Decision 3 — preservation before destruction

Governing order:

1. legal empty equipment target
2. source-proven equipment upgrade candidate
3. direct legal storage
4. non-destructive global repacking
5. displaced-equipment preservation / container nesting
6. value-aware bounded eviction + repacking
7. discard only when no better legal retained state exists

장비 업그레이드가 유리해도 벗겨진 기존 장비의 retention value가 자동으로 0이 되지 않는다.

## Decision 4 — bounded multi-victim destructive search

큰 보존 대상 하나를 위해 여러 낮은-retention leaf를 비워야 할 수 있다. Destructive search는 한 victim에 제한하지 않지만 deterministic/bounded하게 유지한다.

- locked item/subtree, protected structural state, populated container는 자동 victim이 아니다.
- victim 후보는 `FarmingGuideLootRetentionPolicy` 순위로 안정적으로 정렬한다.
- 작은 eviction prefix부터 평가하며 기존 non-destructive repacking solver를 재사용한다.
- incoming/retained state가 eviction set보다 명확히 우월할 때만 destructive candidate를 허용한다.
- search breadth/node count는 명시적으로 제한한다.

## Decision 5 — Needed truth derives from current raid state

Historical accepted scan count는 truth가 아니다.

```text
raid acquired count(Item ID)
= current snapshot count(Item ID)
- raid baseline snapshot count(Item ID)
```

따라서 이전에 획득한 Needed item을 나중에 버리면 다음 스캔에서 다시 Needed로 평가된다.

## Decision 6 — unsupported live facts remain conservative

Known `WeightKg`는 policy metadata가 될 수 있지만 현재 PMC의 live carry threshold를 추측하지 않는다. 현재 snapshot이 표현하지 않는 durability, actual plate condition, stack quantity, hydration/energy/HP, live magazine/chamber state 등도 공격적인 자동 판단 근거로 발명하지 않는다.

## Decision 7 — tactical reserve remains an extension boundary

최소 음식/음료, 현재 총기의 탄약, 재장전용 빈 공간 등 tactical reserve는 일반 시장가만으로 희생해서는 안 된다. 이 정보는 geometry에 하드코딩하지 않고 향후 authoritative fact/state가 연결될 때 별도 retention constraint/policy input으로 확장할 수 있게 한다.

## Implementation / regression contract

- Core: snapshot inventory counter와 loot retention policy boundary를 제공한다.
- Desktop: v1.15.5 transition orchestration은 historical planner와 분리한다.
- Incoming/displaced container surface를 candidate state에서 합법적으로 사용할 수 있다.
- 최종 recommendation은 기존 explicit `FarmingGuidePendingInstruction` transaction을 유지한다.
- Compact presentation은 같은 storage 내부 재배치를 숨기고 실제 cross-area move/discard만 표시한다.
- Regression tests/smoke는 ordinary equipment displacement, rig/backpack nesting, cycle safety, bounded multi-eviction, protected containers/locks, Needed reset, existing v1.15.4 superiority/filter/reserved contracts를 고정한다.

## Public verification

v1.15.5 exact product source `62466a957a7e32a623a0ffcfad96bfb16504f823`에서 593 deterministic tests, published EXE smoke, transition/runtime smoke, graceful shutdown, Shutdown Race, package verification, Documentation Consistency와 Release workflow가 성공했다. `refs/tags/v1.15.5`, public release target, `/releases/latest`가 같은 exact source를 가리킨다.

## Consequence

Farming Guide는 개별 item if/else 집합이 아니라 bounded candidate state-transition engine으로 관리된다. Facts, legality, retention policy와 presentation의 경계를 분리했으므로 향후 새로운 authoritative fact나 tactical constraint를 추가해도 placement solver와 사용자 문구를 불필요하게 결합하지 않는다.
