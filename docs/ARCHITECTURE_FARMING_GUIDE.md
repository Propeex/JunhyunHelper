# ARCHITECTURE — Farming Guide

기준일: **2026-09-01 KST**  
대상 제품: **v1.15.3+**

이 문서는 `파밍 가이드` subsystem의 책임, authority, persisted loadout editing, live raid session, complete-equipment boundary, source-backed nested storage, storage presentation, Scanner test bridge, Tarkov 변화 대응 및 검증 경계를 정의한다.

관련 authority:

- `docs/PRODUCT.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- current facts: `docs/PROJECT_STATE.json`

`DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md` remains the current equipment boundary: user-facing equipment assembly/modification and recursive equipment-internal raid targets are removed. `DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md` supersedes only the v1.15.2 restriction that nested detail was limited to Backpack/Rig, and restores all authoritative source-backed inventory storage grids without restoring equipment assembly editing.

## 1. 목적 / 비목표

Farming Guide has two product roles:

1. **raid-start Loadout / Inventory Editor**
2. **live raid Farming Advisor** driven by already-confirmed Scanner Item IDs

목적:

- 레이드 출발 시점의 **완제품 장비**와 수납 상태 구성
- current Tarkov item dimensions/grids/filters/special-slot mechanics 적용
- authoritative `StorageGrids`를 가진 stored container의 nested storage를 deterministic state로 유지
- raid 중 current session state와 lock constraints를 기준으로 storage/top-level-equipment pickup action 제안
- compatible positive-allow-list nested storage를 general root storage보다 먼저 활용
- explicit user acceptance 전 recommendation을 commit하지 않음
- search-result hover + `T`를 실제 recommendation path의 bounded simulated input으로 제공
- game structure drift나 impossible persisted state fail closed

비목표:

- weapon/helmet/armor modification editor
- unknown equipment internals inference
- game memory/packet/injection 기반 inventory read
- game input automation
- actual Tarkov inventory screen coordinate mirror
- Scanner recognition authority 변경
- arbitrary equipment build의 fabricated composite rendering
- 컨테이너 이름에 기반한 수납 가능성/우선순위 추측
- 검증되지 않은 visual coordinates 추측

## 2. 계층 책임

```text
JunhyunHelper.Desktop
  ├─ complete-equipment presentation
  ├─ root storage + compact source-backed nested-storage detail
  ├─ drag/drop + geometry probing
  ├─ raid start/end UI
  ├─ F lock / T simulated scan interaction
  ├─ FarmingGuideRaidBridge
  ├─ simulated Scanner snapshot preparation/on-demand catalog bridge
  ├─ recommendation text presentation
  ├─ complete/default-preset image selection presentation
  ├─ storage visual layout rendering
  ├─ preset UI
  └─ published-runtime smoke

JunhyunHelper.Core
  ├─ state models / ParentInstanceId / lock state
  ├─ FarmingGuideRaidSession + revision/pending contract
  ├─ FarmingGuideLootPriorityPolicy
  ├─ FarmingGuideCompleteEquipmentPolicy
  ├─ top-level equipment/storage/special-slot compatibility
  ├─ source-backed nested storage/filter policy
  ├─ dedicated nested storage candidate ordering
  ├─ grid placement / packing rules
  ├─ source assembly policy retained only for compatible source evidence/tests where needed
  ├─ FarmingGuideStorageVisualLayoutResolver
  ├─ search policy
  ├─ pocket geometry
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ Tarkov item import
  ├─ StorageGrids + allowed/excluded category/item filter import
  ├─ specialSlot classification import
  ├─ attachment/armor/default-preset source metadata import
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

## 3. Source content vs Farming Guide runtime

### Source Game Content

Current validated Game Content may contain:

- ordinary item width/height;
- storage grids and allowed/excluded filters;
- canonical `specialSlot` classification;
- top-level equipment compatibility/conflicts;
- attachment/armor source definitions;
- default preset membership and source image metadata;
- optional storage layout identity.

### Complete-equipment projection

`FarmingGuideCompleteEquipmentPolicy` is the product boundary between source evidence and user-facing Farming Guide runtime.

The runtime projection:

- removes attachment slot definitions;
- removes armor-plate slot definitions;
- preserves real source-backed `StorageGrids` and their filters because they represent inventory capacity, not equipment composition;
- retains backpack/rig/secure-container root storage mechanics;
- allows stored items with real storage grids to expose nested storage through the common storage policy;
- selects an authoritative complete/default-preset image when available;
- leaves the root Item ID as the equipment identity.

Source assembly metadata is therefore **read-only evidence**, while source storage grids are active inventory mechanics.

### Persisted legacy assembly

Schema v2 still has fields capable of reading legacy `Attachments` / `ArmorPlates`. That is compatibility, not a current feature.

On load/sanitize against the runtime catalog:

```text
legacy equipment tree
→ root item resolves
→ runtime item exposes no internal equipment slots
→ child assembly state is discarded
→ root-only FarmingGuideItemState
```

No schema bump is required.

## 4. Mechanical authority

Current validated Game Content owns:

- grid count / width / height;
- allowed/excluded category/item filters;
- ordinary item footprint;
- canonical `specialSlot` classification;
- top-level equipment compatibility/conflicts;
- actual placement legality.

Special Slots are a context-specific exception to ordinary footprint: a canonical `specialSlot` item occupies exactly one special slot while retaining its ordinary footprint everywhere else.

Container name/category guesses do not create storage mechanics. A nested storage surface exists because source content provides a real storage grid.

## 5. Storage surface model

### Root surfaces

Main Farming Guide renders:

- Pockets;
- Rig;
- Backpack;
- Secure Container;
- Special Slots.

Root Rig/Backpack/Secure Container storage is already visible and therefore does not open a duplicate workbench/detail surface.

### Nested surfaces

`FarmingGuideStoredItemState.ParentInstanceId` identifies the owning stored item.

A stored item may expose a detail surface when `FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage` confirms that current runtime content contains one or more real `StorageGrids` for it.

This rule is source-backed rather than name-backed. It covers backpacks/rigs and specialized containers such as key/document/money/card/injector storage when the current Tarkov data supplies grids, without maintaining a product allowlist of container names.

The same parent tree and storage-filter policy is used by:

- sanitizer;
- rendering;
- collision/placement;
- manual drag/drop;
- raid placement planner;
- lock ancestry checks.

Each grid preserves:

- source width / height;
- allowed category IDs;
- allowed item IDs;
- excluded category IDs;
- excluded item IDs.

Invalid parent relationships fail closed: orphan, duplicate instance, self-parent, cycle, unknown parent, invalid grid/filter/bounds/overlap.

Carrier movement preserves descendants. Destructive removal removes the subtree. A carrier cannot move inside itself or a descendant. Arbitrary supported nesting depth is represented by the same parent chain, including a specialized container inside Secure Container and another legal storage container inside it.

### Dedicated nested placement

A nested grid is a **dedicated candidate** only when:

1. it has a positive source allow-list (`AllowedItemIds` or `AllowedCategoryIds`); and
2. that same grid filter accepts the incoming item.

For non-destructive empty placement, compatible dedicated nested surfaces are evaluated before general root Secure Container/Pockets/Rig/Backpack space. Unrestricted nested storage keeps the established general ordering and is not promoted merely because it is nested.

This is a placement-order policy, not a name-based item-purpose inference.

### Compact nested detail

The detail surface is not a fixed full-column overlay.

```text
render authoritative storage grids
→ measure actual rendered grid footprint
→ add bounded title/close chrome
→ clamp to available viewport
→ show compact detail over still-visible main storage
```

This sizing behavior is product presentation only; it does not change storage mechanics.

## 6. Visual arrangement authority

Product-owned verified metadata may provide:

- layout identity mapping;
- per-grid visual X/Y coordinates;
- per-grid expected width/height signature.

Exact visual coordinates activate only when layout identity, grid count and every grid index dimension match current Game Content. Otherwise rendering uses finite compact fallback coordinates while keeping current mechanics unchanged.

## 7. Complete-item image authority

Farming Guide does not fabricate equipment composition.

Preferred source order:

1. canonical base item `DefaultPresetItemId` → authoritative preset source image;
2. item source `Image512Url` / `GridImageUrl`;
3. canonical item icon.

The selected image represents the item as a complete product presentation, not mutable user assembly state.

Equipment-slot rendering preserves aspect ratio and clips safely while using smaller internal margins than the old assembly presentation so long weapons fill their slots more naturally.

## 8. Persisted working state vs ephemeral raid state

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

- outside raid: normal working-state persistence and preset-selection invalidation;
- inside raid: replace current session state + increment revision; do not persist working state.

Preset selection/deletion is disabled while raid session is active.

## 9. Pending recommendation transaction

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

New-scan path:

```text
pending A at revision N
→ scan B before accept
→ discard pending A with no state mutation
→ plan B against current revision N
→ pending B
```

Any manual inventory/equipment/lock mutation increments revision and clears pending. Desktop clears persistent Mini Scanner instruction silently.

## 10. Raid planner target boundary

Current planner considers:

- legal ordinary root storage surfaces;
- legal source-backed nested storage surfaces;
- compatible dedicated nested storage before general root empty storage;
- legal top-level PMC equipment slots;
- Rig / Backpack / Secure Container top-level carrier equipment slots.

It does **not** enumerate:

- weapon attachment slots;
- helmet attachment slots;
- armor plate slots;
- any equipment-internal target.

This is both a UI and recommendation-domain contract.

## 11. Instruction presentation

Mini Scanner already owns recognized item name, so Farming Guide action text does not repeat the incoming name.

Current wording:

- Store: `[보관할 장소]에 보관`
- Replace: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- top-level Equip: `[장착할 장비 칸]에 장착`
- top-level ReplaceEquip: `[장착할 장비 칸]의 [기존 장비]와 교체`
- accepted feedback: `반영 완료`

Persistent recommendation and transient acceptance feedback have separate lifetimes.

## 12. Lock model / presentation

`FarmingGuideLockState` contains:

- `EquipmentSlots`;
- `Carriers`;
- `ItemInstanceIds`;
- `ReservedCells`.

Current semantics:

- item lock protects exact instance from automated removal/replacement;
- moving same locked item preserves instance lock;
- equipment/carrier lock protects current target from automated removal/replacement;
- removing/replacing target expires that target lock;
- empty-cell lock is an independent reserved-space constraint until explicitly unlocked;
- locked rig/backpack/secure container still permits ordinary auto-placement inside its storage;
- locked supported nested carrier's internal storage is not globally disabled;
- direct user drag/drop is not blocked by locks.

Visual contract:

- unlocked stored item uses neutral border;
- explicit `F`-locked stored item uses accent/yellow border;
- unlock restores neutral border;
- reserved empty cell and equipment/carrier lock accents keep their existing meanings.

F lock toggle is a lightweight state/visual update. Later full renders reapply valid lock highlights from current state.

## 13. Scanner / simulated input boundary

Scanner:

- confirms Item ID using external screen pixels/OCR;
- owns recognition confidence/fail-closed behavior;
- publishes `ScannerItemSnapshot` after recognition.

Farming Guide begins only after Item ID confirmation. Price/needed facts are never promoted into identity evidence by this bridge.

Search-result hover + `T` is a **test input**, not a second decision implementation.

```text
hovered concrete search result + T
→ prepare same-mode ScannerItemSnapshot
→ use in-memory verified catalog if available
→ otherwise load verified same-mode local catalog on demand
→ same Farming Guide recommendation handler/path as normal confirmed scan
```

Rules:

- hovered result takes precedence even when Search TextBox owns keyboard focus;
- no hovered result means `T` remains ordinary search input;
- capture mode need not be enabled;
- preparation failure is explicitly surfaced rather than silently ignored;
- simulated presentation is bounded and cannot hide/overwrite a newer real scan.

The test path may prepare input differently, but it does not own a separate loot decision policy.

## 14. Loot decision facts / policy

Existing authorities are projected into `ScannerItemSnapshot` only after Item ID confirmation:

- Item ID: Scanner current-catalog recognition or explicit simulated selected-item input;
- current needed quantity: `ItemsWorkspace.Plan.NeededItems`;
- trader/flea market presentation: Scanner catalog/presentation mapping;
- ordinary dimensions/slots: current catalog/content facts.

Farming Guide owns only recommendation policy.

Current priority:

1. remaining current-needed quantity;
2. effective value per ordinary occupied slot;
3. total effective value;
4. smaller ordinary footprint.

`EffectiveValue = max(trader sell, flea average, 0)`.

Legal empty placement is preferred to destructive replacement. Dedicated nested storage ordering is applied inside legal empty-placement discovery before the general root ordering.

## 15. User state

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v2
readable: v1-v2
```

Stores working loadout, presets, complete top-level equipment, storage placement/rotation, nested parent relationships and automation locks. Legacy assembly fields may be read but are normalized away in current runtime.

v1.15.3 does not require a schema bump: a specialized source-backed container uses the same existing stored-item state + `ParentInstanceId` model.

## 16. Tarkov data drift behavior

When current content changes:

- unknown item IDs fail closed during sanitize;
- invalid storage grids/filters/placement fail closed;
- disappearance of a source storage grid removes that nested capacity rather than preserving guessed behavior;
- new source-backed storage containers can become supported without a container-name code allowlist;
- source filter changes immediately redefine legal contents after validated Game Content activation;
- source default preset/image disappearance falls back to lower image authority;
- equipment-internal source changes do not re-enable modification UI because complete-equipment policy is product-owned.

## 17. Verification contract

Applicable changes are verified through:

- deterministic Core/maintenance tests;
- WPF Release build/XAML compile;
- self-contained win-x64 publish;
- actual published EXE Farming Guide/Product UI runtime smoke;
- source-backed specialized nested grid/filter interaction smoke;
- dedicated nested storage > general root placement smoke;
- neutral ↔ locked accent stored-item border smoke;
- hover + `T` simulated scan / on-demand Scanner snapshot path coverage;
- complete-equipment internal-editor boundary preservation;
- normal graceful shutdown;
- active-async Shutdown Race;
- package/checksum audit;
- Documentation Consistency;
- exact-main and public release identity readback.

v1.15.3 exact product source `c35204da66eb0af454b50550c830b071a0897835` passed the full applicable automated gate with **563 passed / 0 failed / 0 skipped**. Public release/tag/latest and public ZIP digest were read back and matched the exact-main evidence.
