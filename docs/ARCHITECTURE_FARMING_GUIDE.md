# ARCHITECTURE — Farming Guide

기준일: **2026-09-01 KST**  
대상 제품: **v1.15.0+**

이 문서는 `파밍 가이드` subsystem의 책임, authority, persisted loadout editing, live raid session, Scanner integration, assembly editing, storage presentation, Tarkov 변화 대응 및 검증 경계를 정의한다.

관련 authority:

- `docs/PRODUCT.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- current facts: `docs/PROJECT_STATE.json`

## 1. 목적 / 비목표

Farming Guide has two product roles:

1. **raid-start Loadout / Inventory Editor**
2. **live raid Farming Advisor** driven by already-confirmed Scanner Item IDs

목적:

- 레이드 출발 시점 장비와 수납 상태 구성
- current Tarkov item dimensions/grids/filters/slots 조작
- nested storage와 recursive assembly를 deterministic state로 유지
- raid 중 현재 session inventory와 lock constraints를 기준으로 pickup placement/replacement/discard를 제안
- explicit user acceptance 전에는 automated recommendation을 commit하지 않음
- game structure drift 시 impossible/stale state fail closed

비목표:

- game memory/packet/injection 기반 inventory read
- game input automation
- actual Tarkov inventory screen coordinate mirror
- Scanner recognition authority 변경
- arbitrary build의 Tarkov client 완전 동일 렌더링
- 검증되지 않은 visual coordinates를 authentic layout으로 추측

## 2. 계층 책임

```text
JunhyunHelper.Desktop
  ├─ equipment/storage presentation
  ├─ drag/drop + geometry probing
  ├─ raid start/end UI
  ├─ F lock / T simulated scan interaction
  ├─ FarmingGuideRaidBridge
  ├─ recursive in-page workbench
  ├─ inline compatible-item picker
  ├─ assembly-aware image presentation
  ├─ storage visual layout rendering
  ├─ preset UI
  └─ published-runtime smoke

JunhyunHelper.Core
  ├─ state models / ParentInstanceId / lock state
  ├─ FarmingGuideRaidSession + revision/pending contract
  ├─ FarmingGuideLootPriorityPolicy
  ├─ equipment compatibility
  ├─ grid placement / packing rules
  ├─ FarmingGuideAssemblyPolicy
  ├─ FarmingGuideStorageVisualLayoutResolver
  ├─ search policy
  ├─ pocket geometry
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ Tarkov item import
  ├─ assembly/default-preset source import
  ├─ StorageLayoutName import
  ├─ Content v10 persistence / v3-v10 read
  └─ Farming Guide schema-v2 user-state persistence

Scanner subsystem
  ├─ confirms Item ID from external screen pixels
  ├─ joins existing market/dimension presentation facts
  ├─ joins ItemsWorkspace current-needed truth
  └─ publishes ScannerItemSnapshot after recognition
```

WPF handler가 compatibility/Needed Items/market truth를 새로 재구현하지 않는다. Scanner는 loot decision을 소유하지 않고 Farming Guide는 item recognition을 소유하지 않는다.

## 3. Authority split

### Mechanical storage authority

Current validated Game Content:

- grid count / width / height
- allowed/excluded filters
- item footprint
- attachment/armor slots and conflicts
- actual placement legality

### Loot decision facts

Existing authorities are projected into `ScannerItemSnapshot` after Item ID confirmation:

- Item ID: Scanner current-catalog recognition
- current needed quantity: `ItemsWorkspace.Plan.NeededItems`
- trader/flea market presentation: Scanner catalog/presentation mapping
- item dimensions/slots: current catalog/content facts

Farming Guide consumes these facts and owns only the recommendation policy.

### Visual arrangement authority

Product-owned verified metadata:

- layout identity mapping
- per-grid visual X/Y coordinates
- per-grid expected Width/Height signature

Visual metadata is presentation-only and may never create/change storage mechanics.

### User state

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v2
```

Stores working loadout, presets, equipment/assembly trees, storage placement, rotation, nested parent instance relationships and automation lock state.

Schema v1 is readable. Missing v2 locks migrate as empty lock state.

## 4. Persisted working state vs ephemeral raid state

The persisted working/preset snapshot and the live raid session are deliberately separate lifecycles.

```text
working/preset snapshot + persisted locks
→ Raid Start
→ FarmingGuideRaidSession
   ├─ immutable baseline snapshot/locks
   ├─ current snapshot/locks
   ├─ monotonically increasing Revision
   └─ optional PendingInstruction(BaseRevision)
→ Raid End
→ baseline restored; session discarded
```

`FarmingGuidePage.MarkChanged` behavior:

- outside raid: normal working-state persistence and preset-selection invalidation
- inside raid: replace current session state + increment revision; do not persist working state

Preset selection/deletion is disabled while raid session is active.

## 5. Pending recommendation transaction

`FarmingGuideRaidSession` is the stale-write guard.

```text
scan at revision N
→ planner builds ProposedSnapshot
→ SetPending(... BaseRevision=N)
→ user sees persistent instruction
→ accept hotkey
→ TryAccept
   ├─ current Revision == N → commit proposed snapshot, Revision++
   └─ mismatch/no pending    → reject
```

Any manual inventory/equipment/lock mutation calls `ReplaceCurrentState` or `ReplaceLocks`, increments revision and clears pending.

Desktop additionally clears the persistent Mini Scanner instruction and shows transient cancellation feedback. This prevents a stale UI instruction from surviving even though Core already rejected its transaction.

## 6. Lock model

`FarmingGuideLockState` contains four independent constraint classes:

- `EquipmentSlots`
- `Carriers`
- `ItemInstanceIds`
- `ReservedCells`

Current planner behavior:

- locked root carrier surface is not enumerated as an automatic destination;
- nested storage under a locked carrier/item ancestry is not enumerated;
- locked stored item/subtree is not a replacement candidate;
- reserved cells are synthesized as occupied 1×1 placements for `FindFirstFit`;
- lock state persists in working state and each preset.

Direct user drag/drop is not blocked by these locks. Locks are automation constraints, not edit permissions.

## 7. Raid planner

### Destination enumeration

Current root preference order:

```text
Secure Container
→ Pockets
→ Rig
→ Backpack
→ Special Slots
→ eligible nested stored containers
```

Only current legal storage filters and unlocked surfaces participate.

### Placement

For each surface:

1. `FarmingGuideCompatibility.FilterAllows`
2. current stored placements + reserved cells
3. `FarmingGuidePlacementEngine.FindFirstFit`
4. try normal orientation and rotated orientation where dimensions differ

A legal empty fit is preferred before destructive replacement.

### Replacement

If no legal empty fit exists:

- enumerate stored items on eligible surfaces;
- exclude locked item/subtree candidates;
- compare incoming vs existing `FarmingGuideLootMetrics`;
- remove selected candidate subtree in the proposed snapshot;
- verify incoming item actually fits after removal;
- return one `Replace` proposal.

If no valid replacement exists, return `Discard` with unchanged snapshot.

## 8. Loot priority policy

`FarmingGuideLootPriorityPolicy` is intentionally independent of placement mechanics.

Current comparison:

1. `CurrentNeeded > 0`
2. higher `EffectiveValue / Slots`
3. higher `EffectiveValue`
4. smaller slot footprint

```text
EffectiveValue = max(TraderSellPrice, FleaAveragePrice, 0)
```

This policy boundary may change later without changing Scanner identity, raid transaction semantics or grid packing.

Accepted raid item counts are tracked session-locally so repeatedly accepted copies reduce the remaining current-needed priority for subsequent scans without mutating authoritative profile inventory.

## 9. Scanner bridge / simulated scan

`FarmingGuideRaidBridge` is the narrow Desktop integration boundary.

Responsibilities:

- observe confirmed `ScannerRuntimeStatus.ShowingItem` identities;
- deduplicate the same continuously displayed Item ID until Scanner leaves item state;
- resolve `ScannerItemSnapshot` from Scanner presentation service;
- publish snapshot to Farming Guide handler;
- expose explicit accept callback to configurable global hotkey;
- route persistent Farming Guide instruction vs transient feedback to Mini Scanner;
- provide `PublishSimulatedScan` for search-hover `T` test path.

`T` uses the same snapshot resolver and scan handler. There is no separate test recommendation implementation.

## 10. Mini Scanner instruction lifetime

Mini Scanner has two distinct presentation lifetimes:

- normal Scanner item fields + optional Farming Guide pending instruction: persistent while relevant
- acceptance/cancellation feedback: transient

Raid end/data change resets Scanner identity and clears Farming Guide persistent instruction.

Scanner display settings schema v10 owns the Farming Guide field visibility/order and Farming Guide accept hotkey. Existing schema v9 settings migrate additively.

## 11. Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies a stored carrier parent.

- null = root/top-level surface
- non-null = exact parent stored instance

Sanitization validates root→accepted-parent order and rejects duplicate instance IDs, orphan/self/cycle relationships, missing/invalid parent layouts, invalid grid indexes, filters, bounds and overlap.

Movement/deletion rules:

- carrier instance identity and descendant chain are preserved on movement;
- self/descendant containment is forbidden;
- destructive deletion/removal deletes the subtree;
- contents-filled carrier silent replacement is prohibited.

## 12. Assembly policy / workbench

`FarmingGuideAssemblyPolicy` is the Core authority for deep node lookup/mutation, attachment filters, armor allowed-item rules, conflicts, required-slot recursion, bounded traversal, deterministic signature and persisted-tree sanitization.

The workbench is in-page:

- storage carrier → actual grids
- weapon → attachment/mod slots
- helmet/body armor → attachment/replaceable armor slots
- installed child can be navigated recursively
- empty actionable slot can open compatible-item picker
- picker and drag/drop share Core validation
- occupied one-item target is never silently overwritten

## 13. Assembly-aware presentation

Authoritative composed preset image is used only when a usable imported default-preset image exists and current assembly membership exactly matches authoritative preset membership.

Arbitrary assemblies use deterministic fallback presentation and are not claimed to match Tarkov rendering exactly.

## 14. Exact storage visual-layout resolver

Each verified visual grid contains:

```text
X / Y
ExpectedWidth / ExpectedHeight
```

Exact activation requires:

1. verified profile by layout identity or explicit verified item alias;
2. exact live/profile grid count;
3. positive live dimensions;
4. exact per-index live Width/Height signature;
5. finite transformed positions/bounds;
6. no resulting rectangle overlap.

Any failure returns `false`; Desktop uses compact fallback. Visual coordinates never modify legal mechanics.

## 15. Search / equipment compatibility

- assembled weapon preset records are excluded only from Farming Guide draggable search;
- pistol/revolver/handgun semantics target Holster;
- Secure Container uses explicit secure/pouch semantics and narrow fallback;
- pocket geometry is centrally resolved from current profile facts.

Equipment-slot lock is already part of the shared lock model, but v1.15.0 Scanner raid planner currently recommends storage/replacement/discard rather than automatic equipment changes. Future equipment recommendation must consume this same lock authority instead of inventing another model.

## 16. Content / schema

Game Content v10 preserves dimensions, storage grids/filters, attachment/armor slots/conflicts, default-preset membership/image and optional `StorageLayoutName`.

Compatibility:

```text
Content write: v10
Content read: v3-v10
Farming Guide user state: write v2 / read v1-v2
Scanner display settings: v10
```

Old readable snapshots are not enriched by guesswork.

## 17. Runtime validation

Deterministic tests cover existing placement/nested-storage/assembly/layout contracts plus v1.15.0:

- raid baseline/lock normalization
- revision-bound explicit acceptance
- manual/lock mutation pending invalidation
- current-needed priority
- value-per-slot/total/footprint tie breakers
- source-level carrier/item/cell lock enforcement
- stale persistent Mini Scanner instruction cleanup

CI additionally builds/publishes the actual Windows x64 product and runs startup + Product UI + Map + graceful shutdown smoke. Shutdown Race CI separately exercises active-async termination behavior.

Exact public release evidence belongs in `docs/RELEASE_1.15.0.md` only after main/release verification completes.

## 18. Change discipline

When changing Farming Guide:

1. identify whether the change is persisted editor, ephemeral raid state, decision policy, Scanner adapter or presentation;
2. keep current validated Game Content mechanics authoritative;
3. keep Scanner identity and Needed Items truth outside Farming Guide;
4. never commit automated recommendation without explicit acceptance;
5. preserve revision/stale guard when adding asynchronous inputs;
6. route new automation constraints through `FarmingGuideLockState` where semantically applicable;
7. fail closed rather than retain impossible state;
8. add deterministic regression for the changed contract;
9. user-visible WPF changes require actual published EXE smoke.
