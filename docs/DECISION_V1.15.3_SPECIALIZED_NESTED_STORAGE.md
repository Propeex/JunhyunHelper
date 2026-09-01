# Decision — v1.15.3 Farming Guide Specialized Nested Storage

Date: **2026-09-01 KST**  
Status: **PUBLIC VERIFIED / CURRENT**

This decision corrects the nested-storage boundary established by `DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`.

v1.15.2 correctly removed user-maintained weapon/helmet/armor internals, but it was too restrictive when it also reduced the nested-storage detail surface to stored Backpack/Rig items. Tarkov has physical inventory containers whose purpose is to store restricted item families—keys, money, cards/documents, injectors and similar inventory objects. Those are storage mechanics, not equipment assembly state, and Farming Guide must model them when authoritative Game Content provides real storage grids.

## 1. Source-backed storage, not a container-name allowlist

A stored item supports nested storage when current validated Game Content exposes one or more `FarmingGuideData.StorageGrids` for that item.

The product must **not** maintain a hardcoded list such as `Key tool`, `Documents case`, `Wallet`, `Injector case`, etc.

Reason:

- Tarkov can add/remove/change containers without a JunhyunHelper code release;
- multiple specialized containers can serve keys/money/cards/documents/injectors and other categories;
- source grid filters already express what each container can accept;
- name/category guesses would duplicate and eventually contradict Tarkov authority.

## 2. Storage filters remain authoritative

Every nested grid preserves its source-backed:

- width / height;
- allowed category IDs;
- allowed item IDs;
- excluded category IDs;
- excluded item IDs.

Manual drag/drop, persisted-state sanitizer and raid automatic placement must all use the same filter contract.

Examples are descriptive, not hardcoded rules:

- a key-oriented container accepts only the key item families permitted by its current grid filter;
- a money/card/document container accepts only what its current filter permits;
- an injector container accepts only what its current filter permits;
- an unrestricted storage container remains unrestricted except for any explicit exclusions.

## 3. Arbitrary supported nesting depth

`FarmingGuideStoredItemState.ParentInstanceId` remains the canonical address for nested storage.

A supported storage item may itself be located inside another valid storage surface, including a Secure Container. Its own storage grids remain independently addressable.

Examples:

```text
Secure Container
└─ specialized case
   └─ allowed item
```

```text
Backpack
└─ storage container
   └─ another storage container
      └─ allowed item
```

Existing safety remains mandatory:

- parent must exist and be accepted before a child;
- orphan / duplicate / self-parent / cycle state fails closed;
- grid index, bounds, overlap and source filter validation applies at every depth;
- moving/removing a parent preserves or removes its descendant subtree according to existing storage semantics;
- an item cannot be moved into itself or its descendants.

## 4. Dedicated nested storage has raid-placement priority

A source-backed nested grid with a **positive allow-list** (`AllowedItemIds` or `AllowedCategoryIds`) is treated as dedicated storage for an incoming item when that same filter accepts the item.

When the raid advisor is looking for a non-destructive empty placement, a compatible dedicated nested surface is evaluated before general-purpose root storage such as Secure Container, Pockets, Rig or Backpack.

Example:

```text
Secure Container
├─ free ordinary cells
└─ Key tool
   └─ free key cell
```

If a scanned key is permitted by the Key tool's current source filter, the advisor should recommend the Key tool interior instead of consuming the Secure Container's general cells first.

This priority is **not** based on container names or inferred item purpose. It exists only when the authoritative grid contains a positive allow-list that accepts the incoming item. Generic/unrestricted nested bags remain behind the established root-storage ordering so they do not unexpectedly absorb ordinary loot.

## 5. Complete-equipment boundary is unchanged

This correction does **not** restore equipment assembly editing.

The following remain hidden/non-editable:

- weapon attachment/mod slots;
- helmet attachment slots;
- body-armor / armored-rig armor plate state;
- recursive equipment assembly picker/editor;
- equipment-internal raid Equip / ReplaceEquip recommendations.

A source `StorageGrid` is inventory capacity. Attachment/armor slots are equipment composition. These are separate domains.

## 6. Nested storage UI

A stored item with real storage grids may open the compact nested-storage detail surface.

- grid dimensions and exact/fallback visual layout use existing storage rendering policy;
- the detail surface remains interactive for drag/drop;
- the host remains bounded and sized to the rendered grid footprint rather than becoming a full-column equipment workbench;
- root Rig / Backpack / Secure Container storage continues to render directly on the main Farming Guide page.

## 7. Lock border presentation

Stored-item cards use the ordinary neutral border by default.

The accent/yellow border is reserved for explicit Farming Guide lock/fix state:

- `F`-locked stored item: yellow/accent border;
- unlocked stored item: normal border;
- locked/reserved empty cell: existing accent reservation overlay;
- equipment/carrier locks retain their existing locked accent presentation.

This makes lock state visually distinguishable from ordinary possession.

## 8. Simulated Scanner input

Search-result hover + `T` is a Farming Guide test command.

- hovering a concrete search result and pressing `T` must take precedence over the search TextBox retaining keyboard focus;
- when no search result is hovered, `T` remains ordinary text input in the search box;
- an active raid session receives the simulated item through the same recommendation path as a real Scanner-confirmed item;
- the test path must not require Scanner capture mode to be enabled;
- if the Scanner catalog is not initialized in memory after restart, the test path may load the verified same-mode local catalog on demand;
- failure to prepare a test snapshot must produce an explicit test failure status rather than silently doing nothing.

## Verification contract

Before v1.15.3 is closed:

- deterministic test that arbitrary source-backed container grids survive complete-equipment runtime projection;
- deterministic test that a specialized nested container inside Secure Container accepts an allowed item and rejects an item denied by its source filter;
- published-product smoke proving a compatible dedicated nested surface wins over otherwise-free general root storage for the same incoming item;
- stored-item default border is neutral and lock application/restoration is accent ↔ neutral;
- hover + `T` path is reachable while the search TextBox has focus and uses an on-demand Scanner snapshot resolver;
- v1.15.2 equipment-internal editing/recommendation boundary remains closed;
- full deterministic suite and Windows Release/XAML build;
- self-contained published EXE product smoke and graceful shutdown;
- Shutdown Race, package verification, PR/exact-main CI and Documentation Consistency;
- public v1.15.3 release/tag/assets/checksum verification.

All items in this verification contract were satisfied by the v1.15.3 public release evidence recorded in `docs/RELEASE_1.15.3.md` and `docs/.release-v1.15.3-status.json`.
