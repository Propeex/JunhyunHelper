# ARCHITECTURE — Farming Guide

기준일: **2026-09-01 KST**  
대상 제품: **v1.15.1+**

이 문서는 `파밍 가이드` subsystem의 책임, authority, persisted loadout editing, live raid session, Scanner integration, assembly editing, storage presentation, Tarkov 변화 대응 및 검증 경계를 정의한다.

관련 authority:

- `docs/PRODUCT.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- current facts: `docs/PROJECT_STATE.json`

`DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md` supersedes conflicting v1.15.0 real-play behavior.

## 1. 목적 / 비목표

Farming Guide has two product roles:

1. **raid-start Loadout / Inventory Editor**
2. **live raid Farming Advisor** driven by already-confirmed Scanner Item IDs

목적:

- 레이드 출발 시점 장비와 수납 상태 구성
- current Tarkov item dimensions/grids/filters/special-slot/assembly mechanics 조작
- nested storage와 recursive assembly를 deterministic state로 유지
- raid 중 현재 session state와 lock constraints를 기준으로 storage/equipment/assembly pickup action 제안
- explicit user acceptance 전에는 recommendation을 commit하지 않음
- game structure drift 시 impossible/stale state fail closed

비목표:

- game memory/packet/injection 기반 inventory read
- game input automation
- actual Tarkov inventory screen coordinate mirror
- Scanner recognition authority 변경
- arbitrary build의 Tarkov client 완전 동일 렌더링
- 검증되지 않은 visual coordinates나 composite imagery 추측

## 2. 계층 책임

```text
JunhyunHelper.Desktop
  ├─ equipment/storage presentation
  ├─ drag/drop + geometry probing
  ├─ raid start/end UI
  ├─ F lock / T simulated scan interaction
  ├─ FarmingGuideRaidBridge
  ├─ recommendation text presentation
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
  ├─ equipment/storage/special-slot compatibility
  ├─ grid placement / packing rules
  ├─ FarmingGuideAssemblyPolicy
  ├─ FarmingGuideStorageVisualLayoutResolver
  ├─ search policy
  ├─ pocket geometry
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ Tarkov item import
  ├─ specialSlot classification import
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

### Mechanical authority

Current validated Game Content owns:

- grid count / width / height
- allowed/excluded filters
- ordinary item footprint
- canonical `specialSlot` classification
- equipment slot compatibility
- attachment/armor slots and conflicts
- actual placement legality

Special Slots are a context-specific exception to ordinary footprint: a canonical `specialSlot` item occupies exactly one special slot while retaining its ordinary footprint everywhere else.

### Loot decision facts

Existing authorities are projected into `ScannerItemSnapshot` only after Item ID confirmation:

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

### Assembly image authority

A changed weapon/helmet composed image may be used only when canonical content exposes an authoritative preset/composed image whose exact contained-item signature matches the current assembly. Unsupported arbitrary assemblies retain safe base/part presentation. Desktop must not fabricate a composite image that claims unsupported visual accuracy.

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

Normal acceptance path:

```text
scan at revision N
→ planner builds ProposedSnapshot
→ SetPending(... BaseRevision=N)
→ user sees persistent action instruction
→ accept hotkey
→ TryAccept
   ├─ current Revision == N → commit proposed snapshot, Revision++
   └─ mismatch/no pending    → reject
```

v1.15.1 new-scan path:

```text
pending A at revision N
→ scan B before accept
→ discard pending A with no state mutation
→ plan B against current revision N
→ pending B
```

A new scan therefore replaces an unaccepted instruction without weakening the single-pending transaction model.

Any manual inventory/equipment/lock mutation increments revision and clears pending. Desktop clears the persistent Mini Scanner instruction silently; manual invalidation does not emit cancellation-noise text.

## 6. Instruction presentation

Mini Scanner already owns the recognized item name, so Farming Guide action text is intentionally name-free for the incoming item.

Current wording:

- Store: `[보관할 장소]에 보관`
- Replace: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- Equip: `[장착할 장소]에 장착`
- ReplaceEquip: `[장착할 장소]의 [기존 아이템]과 교체`
- accepted feedback: `반영 완료`

Persistent recommendation and transient acceptance feedback have separate lifetimes.

## 7. Lock model

`FarmingGuideLockState` contains four independent constraint classes:

- `EquipmentSlots`
- `Carriers`
- `ItemInstanceIds`
- `ReservedCells`

Current semantics:

- item lock protects the exact item instance from automated removal/replacement;
- moving the same locked item preserves the instance lock;
- equipment/carrier lock protects the currently equipped target from automated removal/replacement;
- removing/replacing the target expires that target lock;
- empty-cell lock is an independent reserved-space constraint and remains until explicitly unlocked;
- locking a rig/backpack/secure container does not block ordinary auto-placement inside its internal grids;
- locking an item does not globally disable a nested carrier's internal storage surface;
- reserved cells are synthesized as occupied 1×1 placements for ordinary `FindFirstFit` calculations;
- direct user drag/drop is not blocked by locks.

### Lock rendering

F lock toggle is a lightweight state/visual update. It does not intentionally rebuild the whole page. Any later full render must reapply lock visuals from current lock state. Moving/rotating items, accepted recommendations and other rerenders must not erase a still-valid lock highlight.

## 8. Special-slot policy

Special Slots are not generic 1×1 inventory grids.

```text
Eligibility: canonical item type == specialSlot
Footprint inside Special Slots: exactly 1 slot
Footprint elsewhere: current ordinary item width × height
```

The same Core policy is consumed by:

- persisted-state sanitizer
- manual drag/drop legality
- rendering/card footprint
- collision/occupancy
- capacity summary
- raid advisor placement

Nested ordinary storage remains ordinary even when its parent item itself occupies a special slot.

## 9. Raid planner

The planner evaluates storage targets, equipment targets and destructive replacement only when required.

### Root storage destination order

Current root preference order remains deterministic:

```text
Secure Container
→ Pockets
→ Rig
→ Backpack
→ Special Slots
→ eligible nested stored containers
```

A carrier lock does not remove the carrier's internal storage surface from this list. The carrier itself may still be protected from equip replacement.

### Ordinary placement

For each ordinary surface:

1. Core filter compatibility
2. current stored placements + reserved cells
3. `FarmingGuidePlacementEngine.FindFirstFit`
4. normal and rotated orientation where dimensions differ

A legal empty fit is preferred before destructive storage replacement.

### Special-slot placement

For Special Slots:

1. item must satisfy canonical special-slot policy;
2. occupied/free state is evaluated per special slot;
3. compatible item uses one slot regardless of ordinary footprint.

### Equipment / assembly placement

Legal empty targets can include:

- PMC equipment slots
- rig/backpack/secure-container carrier equipment slots
- recursive weapon/helmet attachment slots
- replaceable armor-plate slots

Compatibility and conflicts come from shared Core policy. WPF does not invent a second equip truth.

### Replacement

If no acceptable empty placement exists, the planner may consider unlocked lower-priority current targets.

Storage replacement:

- enumerate legal stored-item candidates;
- exclude locked candidates and illegal destructive carrier cases;
- compare incoming vs existing loot metrics;
- remove candidate subtree in proposed state;
- verify incoming item actually fits after removal.

Equipment/assembly replacement:

- target must be legal for the incoming item;
- existing target must not be locked;
- shared assembly/equipment compatibility and conflict checks must pass;
- incoming item must outrank the existing item under current loot policy.

If no valid action exists, return Discard with unchanged proposed snapshot.

## 10. Loot priority policy

`FarmingGuideLootPriorityPolicy` remains intentionally independent of Scanner identity and placement mechanics.

Current comparison:

1. `CurrentNeeded > 0`
2. higher `EffectiveValue / Slots`
3. higher `EffectiveValue`
4. smaller ordinary slot footprint

```text
EffectiveValue = max(TraderSellPrice, FleaAveragePrice, 0)
```

Accepted Store/Replace/Equip/ReplaceEquip actions are tracked session-locally so repeatedly accepted copies reduce remaining current-needed priority for subsequent scans without mutating authoritative profile inventory.

Special-slot one-cell occupancy is a placement-context mechanic, not a global rewrite of the item's ordinary loot footprint.

## 11. Scanner bridge / simulated scan

`FarmingGuideRaidBridge` is the narrow Desktop integration boundary.

Responsibilities:

- observe confirmed `ScannerRuntimeStatus.ShowingItem` identities;
- deduplicate continuously displayed identity where appropriate;
- resolve `ScannerItemSnapshot` from Scanner presentation service;
- publish snapshot to Farming Guide handler through UI Dispatcher boundary;
- expose explicit accept callback to configurable global hotkey;
- route persistent Farming Guide instruction vs transient feedback to Mini Scanner;
- provide `PublishSimulatedScan` for search-hover `T` test path.

`T` uses the same snapshot resolver and scan handler. There is no separate test recommendation implementation.

### Simulated-scan presentation lifetime

A T test scan has a bounded presentation lifetime. Its expiration callback must verify it still owns the presentation before hiding it. A later real Scanner presentation invalidates the stale test timer so test cleanup cannot hide current real information.

## 12. Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies a stored carrier parent.

- null = root/top-level surface
- non-null = exact parent stored instance

Sanitization validates root→accepted-parent order and rejects duplicate instance IDs, orphan/self/cycle relationships, missing/invalid parent layouts, invalid grid indexes, filters, bounds and overlap.

Movement/deletion rules:

- carrier instance identity and descendant chain are preserved on movement;
- self/descendant containment is forbidden;
- destructive deletion/removal deletes the subtree;
- contents-filled carrier silent replacement is prohibited unless explicitly supported by a safe contract.

Locking the carrier target does not by itself make its interior storage unusable.

## 13. Assembly policy / workbench

`FarmingGuideAssemblyPolicy` is the Core authority for deep node lookup/mutation, attachment filters, armor allowed-item rules, conflicts, required-slot recursion, bounded traversal, deterministic signature and persisted-tree sanitization.

The workbench is in-page:

- storage carrier → actual grids
- weapon → attachment/mod slots
- helmet/body armor → attachment/replaceable armor slots
- installed child can be navigated recursively
- empty actionable slot can open compatible-item picker
- picker, drag/drop and raid equip planner share Core validation
- occupied one-item target is never silently overwritten

Source/internal slot IDs such as `mod_*` remain canonical identifiers internally. Desktop may map them to understandable Korean user-facing labels without changing identity.

## 14. Assembly-aware presentation

An authoritative composed preset image is used only when a usable imported image exists and current assembly membership exactly matches the source-backed contained-item signature.

Arbitrary assemblies use deterministic safe fallback presentation and are not claimed to match Tarkov rendering exactly.

## 15. Exact storage visual-layout resolver

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

## 16. Search / equipment compatibility

- assembled weapon preset records are excluded only from Farming Guide draggable search;
- pistol/revolver/handgun semantics target Holster;
- Holster is presented below Eyewear in the current product layout;
- Secure Container uses explicit secure/pouch semantics and narrow fallback;
- pocket geometry is centrally resolved from current profile facts;
- equipment/attachment/armor compatibility is shared across picker, drag/drop, sanitizer and raid planner.

## 17. Content / schema

Game Content v10 preserves dimensions, storage grids/filters, special-slot item classification, attachment/armor slots/conflicts, default-preset membership/image and optional `StorageLayoutName`.

Compatibility:

```text
Content write: v10
Content read: v3-v10
Farming Guide user state: write v2 / read v1-v2
Scanner display settings: v10
```

Old readable snapshots are not enriched by guesswork.

## 18. Runtime validation

Deterministic tests cover existing placement/nested-storage/assembly/layout contracts plus raid-advisor contracts including:

- raid baseline/lock normalization
- revision-bound explicit acceptance
- new-scan pending replacement without state mutation
- manual/lock mutation pending invalidation
- current-needed priority
- value-per-slot/total/footprint tie breakers
- special-slot eligibility and one-slot occupancy
- carrier-lock interior-storage semantics
- target-lock expiration vs persistent reserved cells
- equipment/equip/replace-equip pending actions
- Korean assembly slot-label presentation policy
- stale persistent Mini Scanner instruction cleanup

CI additionally builds/publishes the actual Windows x64 product and runs startup + Product UI + Farming Guide + Scanner + Map + graceful shutdown smoke. Shutdown Race CI separately exercises active-async termination behavior.

v1.15.1 public evidence is in `docs/RELEASE_1.15.1.md` and `docs/.release-v1.15.1-status.json`.

## 19. Change discipline

When changing Farming Guide:

1. identify whether the change is persisted editor, ephemeral raid state, decision policy, Scanner adapter or presentation;
2. keep current validated Game Content mechanics authoritative;
3. keep Scanner identity and Needed Items truth outside Farming Guide;
4. never commit automated recommendation without explicit acceptance;
5. preserve revision/stale guard when adding asynchronous inputs;
6. route automation constraints through `FarmingGuideLockState` where semantically applicable;
7. distinguish target protection from storage-surface availability;
8. fail closed rather than retain impossible state;
9. add deterministic regression for the changed contract;
10. user-visible WPF changes require actual published EXE smoke.
