# Decision — v1.15.2 Farming Guide Complete-Equipment Model

Date: **2026-09-01 KST**  
Status: **CONFIRMED / IMPLEMENTATION IN PROGRESS**

This decision supersedes the Farming Guide equipment-internal editing and raid-advisor behavior introduced in v1.14.0 and extended in v1.15.1. The user confirmed that the program cannot reliably know the internal attachment/armor-plate state of equipment acquired during a raid, so equipment must be treated as an opaque complete item rather than a user-maintained assembly tree.

## 1. Equipment is one complete item

Weapons, helmets, body armor, armored rigs and other equipment are represented as the complete item occupying its top-level equipment/storage slot.

Farming Guide no longer exposes or edits:

- weapon attachments or recursive mod slots;
- helmet attachments;
- armor-plate slots;
- assembly-compatible-item picker;
- user-maintained internal equipment composition.

Persisted legacy `Attachments` / `ArmorPlates` fields remain readable for schema compatibility, but the current product sanitizes equipment/item state to the root Item ID and does not preserve those internal edits as current user state.

## 2. Only nested bag/rig storage may be opened

The internal-detail interaction is no longer an equipment-modification workbench.

- top-level equipment slots do not open an internal editor;
- top-level rig/backpack storage is already visible in the main Farming Guide storage layout and does not need a separate detail editor;
- a stored backpack or rig inside another storage surface may be opened to expose its real storage grids;
- nested storage remains fully interactive for drag/drop and may contain further supported backpack/rig storage according to the existing `ParentInstanceId` model;
- equipment attachment/armor state is never shown inside this detail view.

## 3. Nested storage detail uses physical grid-sized presentation

Opening a nested bag/rig must not cover the whole Farming Guide storage column. The detail host sizes itself around the rendered grid footprint plus only the title/close chrome required to interact with it, bounded by the available viewport.

## 4. Complete-item imagery

Farming Guide presentation uses a source-backed complete-item image whenever authoritative content provides one.

For weapons whose canonical base record represents a receiver/action but has an authoritative default preset relationship, the default preset's source image is the preferred Farming Guide image. No arbitrary part composition is rendered.

Fallback order is source-authoritative only:

1. authoritative default-preset image for the base item, when present;
2. the item's own source-backed Farming Guide image metadata when present;
3. canonical item icon.

## 5. Equipment-slot image scale

Equipment-slot imagery, especially weapons, should visually fill the available equipment slot substantially like Tarkov while preserving aspect ratio. The image may use a small safety inset but must not retain the previous large internal margins that made weapons look tiny.

## 6. Raid advisor scope

The advisor may still recommend top-level equipment actions:

- `[장비 칸]에 장착`;
- `[장비 칸]의 [기존 장비]와 교체`.

This includes weapon slots, pistol, helmet, body armor, rig, backpack, secure container and other supported top-level equipment targets.

The advisor must never generate an Equip / ReplaceEquip target inside an equipment assembly, attachment slot or armor-plate slot.

## 7. Compatibility / safety

- nested storage mechanics continue to use current validated Game Content grids and filters;
- Special Slot semantics from v1.15.1 remain unchanged;
- locks continue to constrain automatic replacement/removal while direct user edits remain authoritative;
- user data schema remains backward-readable; no fabricated Tarkov equipment composition is introduced;
- Scanner still supplies confirmed Item ID only; Farming Guide does not infer unknown internal equipment state.

## Verification contract

Before v1.15.2 is closed:

- deterministic sanitizer test proving legacy attachment/armor state is collapsed to root-only state;
- source/runtime contract proving equipment targets do not open assembly editing and raid planning has no internal assembly targets;
- nested stored backpack/rig detail remains interactive;
- nested detail host is compact relative to actual grid size;
- complete weapon image resolves from authoritative default-preset source when available;
- equipment slot weapon image uses reduced safety margin / enlarged visual fill;
- full deterministic suite, Release build, published Windows product smoke, Shutdown Race and package verification;
- PR and exact-main Documentation Consistency;
- public v1.15.2 tag/release/assets/checksum verification.
