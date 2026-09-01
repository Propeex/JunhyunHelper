# DECISION — v1.15.4 Farming Guide repacking and equipment upgrades

Status: **CONFIRMED / IMPLEMENTED / VALIDATION IN PROGRESS**  
Date: **2026-09-01 KST**

## Context

v1.15.3 could identify legal source-backed storage surfaces, including specialized nested containers, but the live-raid advisor still treated every existing grid placement as effectively fixed. This produced false destructive outcomes when enough total storage existed but movable items fragmented the required footprint.

The same advisor also lacked a product-level distinction between "valuable loot to keep" and "objectively superior equipment that should be worn now". A superior armor, rig or backpack could therefore be placed in ordinary storage merely because free storage existed.

During v1.15.4 implementation, the product also began consuming additional source-backed equipment facts. Persisting those facts under the previous Game Content v10 schema would make behavior depend on whether a machine happened to have refreshed its cache.

## Decision 1 — preservation-first raid planning

The live-raid recommendation order is:

1. legal empty equipment target;
2. objectively proven, structurally safe equipment upgrade;
3. direct legal storage without moving existing items;
4. non-destructive legal repacking of unlocked existing items;
5. value/need-based destructive replacement only after preservation options fail;
6. discard only when no preferable legal plan exists.

The repacking planner is bounded and deterministic. It may move multiple unlocked items, rotate eligible items and move them across legal root/nested storage surfaces. Source-backed storage filters, dedicated-container preference, reserved cells, item locks and parent/descendant cycle constraints remain authoritative.

A populated nested container is not a valid destructive replacement candidate based only on the parent container's own value.

## Decision 2 — equipment superiority is separate from loot value

Market/trader value and quest need remain loot-priority facts. They are not treated as equipment-performance facts.

Automatic equipment upgrades use only conservative source-backed comparisons:

- protective equipment: incoming representative top-level `properties.class` must be strictly higher than the current compatible equipment;
- backpack: raw source-backed storage capacity must be strictly larger and all current modeled contents must remain legal;
- ordinary rig: raw source-backed storage capacity must be strictly larger and all current modeled contents must remain legal;
- armored rig -> armored rig: protection class and capacity must both be non-regressing, with at least one strict improvement;
- headset: `distanceModifier` must be no worse and `distortion` must be no worse, with at least one strict improvement. Trade-offs are not automatically ranked.

The source API exposes additional headset tuning fields. v1.15.4 deliberately does not invent a total headset ranking from those fields or from price.

## Decision 3 — body armor + ordinary rig -> armored rig is atomic

A scanned armored rig may replace an ordinary body armor plus an ordinary rig only when all of the following are true:

- the incoming item is source-classified as an armored rig;
- the incoming representative armor class is strictly higher than the current body armor's representative class;
- the body-armor slot and rig carrier are not locked;
- the incoming armored rig is conflict-free after removing the body armor;
- every current top-level rig item can be packed legally into the incoming rig's real source-backed grids;
- all grid filters and reserved cells remain valid;
- any locked item/subtree can preserve its required automation constraint;
- nested descendants remain attached to their existing parent instance;
- the final proposed snapshot passes the canonical loadout sanitizer.

The transition is one pending raid transaction. The body armor is removed, the incoming armored rig replaces the ordinary rig, and all preserved rig contents are moved in the same proposed revision. Nothing is committed until the user accepts the recommendation.

If any condition fails, the combined transition is not proposed. In particular, an incoming armored rig must not fall through to an ordinary rig replacement while body armor remains equipped.

The reverse operation — armored rig -> body armor + ordinary rig — is not inferred from one scanned item because the advisor cannot create the missing second piece of equipment.

## Decision 4 — complete-equipment armor limitation

Farming Guide intentionally models top-level equipment as complete opaque items and does not expose or track armor-plate internals.

The Tarkov source exposes a top-level armor `class` together with separate `armorSlots` for plate-capable equipment. v1.15.4 therefore treats the top-level class only as the canonical representative protection fact available to the complete-equipment model. The advisor does not claim to know or invent a user's manually changed in-raid plate configuration.

If future product requirements need exact per-plate protection, that is a separate product decision and cannot be added by silently re-opening the complete-equipment internal assembly model.

## Decision 5 — Game Content schema v11

Game Content write schema advances from **v10 to v11** to persist:

- `FarmingGuideItemLayout.ArmorClass`;
- `FarmingGuideItemLayout.HeadsetDistanceModifier`;
- `FarmingGuideItemLayout.HeadsetDistortion`.

Schemas v3-v10 remain readable as last-known-good offline snapshots. When Desktop loads a readable snapshot older than the current write schema, it opportunistically performs a normal transactional Data Update after active content becomes available. If that refresh cannot be completed, startup keeps using the readable older snapshot rather than failing or deleting it. Migration is retried on a later launch/update.

Product-smoke mode skips this opportunistic network refresh so published EXE tests remain deterministic and offline.

## Decision 6 — nested-storage viewport behavior

A source-backed nested storage workbench should display the complete grid without horizontal scrolling when that grid physically fits the effective center-column viewport.

Horizontal scrolling is a physical fallback only. It remains disabled after constrained measurement when content fits, preventing WPF `ScrollViewer` Auto-scrollbar feedback from manufacturing a horizontal bar that then causes a vertical bar and further width loss.

## Verification contract

v1.15.4 is not complete until the frozen candidate head passes:

- deterministic Core/Infrastructure/Maintenance tests;
- Windows Release build;
- self-contained win-x64 publish;
- published EXE Farming Guide smoke covering fragmented-capacity repacking, nested Workbench viewport behavior and body-armor+populated-rig -> armored-rig preservation;
- graceful shutdown and Shutdown Race;
- release package/checksum verification;
- Documentation Consistency;
- exact-main CI before the public tag/release is created.
