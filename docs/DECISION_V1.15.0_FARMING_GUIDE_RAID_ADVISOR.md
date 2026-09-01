# DECISION — v1.15.0 Farming Guide Live Raid Advisor

Date: **2026-09-01 KST**  
Status: **CONFIRMED / IMPLEMENTED / RELEASE-CANDIDATE VERIFICATION**

## Context

Farming Guide는 v1.13~v1.14에서 raid-start Loadout / Inventory Editor로 구축됐다. 사용자는 레이드 중 Scanner로 실제 화면의 아이템을 확인할 때, 현재 파밍 가이드 상태와 필요한 아이템/가격을 함께 고려해 무엇을 보관하고 무엇을 버릴지 즉시 판단받고 싶어 한다.

이 기능은 게임 프로세스 내부 inventory를 읽거나 사용자의 게임 입력을 자동화해서는 안 된다. 또한 레이드 중 잠깐 발생한 상태 변화가 raid-start 프리셋을 오염시키면 안 된다.

## Decision

### 1. Raid state is an isolated session

`레이드 시작` 시 현재 Farming Guide snapshot과 lock state를 baseline으로 별도 `FarmingGuideRaidSession`을 연다.

- raid 중 변경은 session revision에만 반영한다.
- working state/preset은 raid 중 자동 저장하지 않는다.
- `레이드 종료`는 raid-session 변경을 폐기하고 baseline snapshot/locks를 복원한다.
- 사용자의 수동 inventory/equipment/lock 변경은 즉시 새 session revision이 된다.

### 2. Scanner owns facts; Farming Guide owns decisions

Scanner는 확인된 Item ID와 기존 authority에서 얻은 presentation facts만 제공한다.

```text
confirmed Item ID
+ current needed quantity
+ trader sell price
+ flea average price
+ item slots
```

Farming Guide가 별도 OCR, market truth, Needed Items 계산을 만들지 않는다.

`ItemsWorkspace.Plan.NeededItems`와 Scanner catalog/presentation pipeline의 기존 truth를 재사용한다.

### 3. Every automated recommendation is a pending transaction

Scanner item event는 즉시 Farming Guide 상태를 수정하지 않는다.

```text
scan
→ recommendation against revision N
→ one pending instruction
→ Mini Scanner displays instruction + accept hotkey
→ explicit user acceptance
→ commit only if revision is still N
```

- pending은 동시에 하나만 유지한다.
- user acceptance가 없으면 상태 변화가 없다.
- manual state/lock change가 발생하면 pending은 stale이며 즉시 폐기한다.
- stale persistent Mini Scanner instruction도 함께 제거한다.

이 계약은 스캔과 UI 입력이 비동기적으로 교차하더라도 오래된 의사결정이 최신 inventory에 적용되는 것을 막는다.

### 4. Locks constrain automation, not the user

Hover + `F` lock은 자동 판단이 건드릴 수 없는 영역을 표현한다.

- stored item lock → replacement 대상 제외
- locked item/subtree → 해당 내부 storage까지 자동 destructive decision에서 보호
- carrier lock → Rig/Backpack/Secure Container 등 해당 storage와 내부를 자동 placement 후보에서 제외
- empty cell lock → 1-cell reserved obstacle
- equipment-slot lock → 향후 equipment recommendation에도 사용하는 동일 automation constraint model

사용자의 직접 drag/drop 편집은 항상 허용되고, 그 변경은 이전 pending을 무효화한다.

### 5. Current loot priority is a replaceable Core policy

Placement mechanics와 loot 가치 정책을 분리한다.

현재 v1.15.0 `FarmingGuideLootPriorityPolicy`:

1. current needed quantity가 남아 있는 item 우선
2. 필요 여부가 같으면 높은 effective value per slot 우선
3. 같으면 높은 total effective value 우선
4. 마지막 동률이면 작은 footprint 우선

Effective value는 Scanner가 제공한 trader/flea 값 중 높은 값을 사용한다.

빈 공간에 합법적으로 들어가면 먼저 배치하고, 공간이 없을 때만 unlocked stored item replacement를 검토한다.

### 6. Simulated scan must use the same decision path

Farming Guide search result hover + `T`는 별도 테스트 알고리즘을 사용하지 않는다.

Scanner snapshot resolver → raid bridge → `HandleScannedItem` → recommendation/pending 경로를 그대로 사용한다. 따라서 실제 scan과 simulated scan 사이의 판단 로직 drift를 피한다.

### 7. Mini Scanner instruction has persistent lifetime

기존 transient runtime status와 Farming Guide pending instruction을 구분한다.

- pending instruction: accept/cancel까지 지속
- `수락 완료`, stale-cancel feedback: 짧은 transient status
- raid end/data change: persistent instruction clear

## Persistence / compatibility

`farming-guide.json`은 schema v2로 올린다.

- v2: profile working lock state + preset lock state 저장
- v1: 읽기 지원, lock은 empty state로 additive migration

Scanner display settings는 schema v10으로 올린다.

- Farming Guide display field/order
- Farming Guide accept hotkey
- 기존 v9 설정을 additive migration

## Non-goals

- game memory / packet / injection inventory reading
- 자동 마우스/키보드 입력
- Tarkov inventory screen coordinate mirror
- acceptance 없는 자동 pickup/replace
- Scanner가 Farming Guide를 위해 새로운 market/Needed Items truth를 계산하는 것

## Verification contract

- deterministic raid-session revision/acceptance tests
- loot-priority tests
- source contract tests for lock/pending/Mini Scanner cleanup
- full existing deterministic suite
- Windows Release/XAML build
- self-contained published EXE startup/Product UI/Map/graceful shutdown smoke
- Shutdown Race CI
- exact-main CI and release artifact verification

## Consequences

- raid behavior is reversible by construction.
- recommendation engine can evolve without rewriting grid placement or Scanner identity logic.
- user-owned locks are persistent but do not prevent direct user edits.
- Scanner remains an external-screen recognition subsystem rather than becoming a live inventory authority.
