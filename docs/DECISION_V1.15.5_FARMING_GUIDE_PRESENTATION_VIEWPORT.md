# DECISION — v1.15.5 Farming Guide concise raid instructions and nested Workbench viewport

Status: **CONFIRMED / IMPLEMENTED / VALIDATION IN PROGRESS**  
Date: **2026-09-01 KST**

## Context

v1.15.4 strengthened the Farming Guide planner so it could preserve inventory by repacking unlocked items and could recommend source-backed equipment upgrades. The underlying decisions were correct, but two presentation problems remained in real use.

First, the Mini Scanner repeated implementation detail that the user did not need to consciously execute. Same-container X/Y/grid/rotation repacking and explanatory equipment metrics made the action text long enough to be difficult to read at a glance during a raid.

Second, a Key tool internal Workbench could still show a vertical scrollbar and clip the bottom of a grid even when the whole nested grid physically fit the center column. The v1.15.4 width correction did not reserve equivalent ScrollViewer template chrome on the vertical axis, and leaving vertical scrolling on `Auto` allowed WPF scrollbar feedback to manufacture a smaller viewport.

## Decision 1 — compact action vocabulary

The planner continues to own the complete proposed state. Presentation is a separate final formatting step and must not alter action type, proposed snapshot, locks, storage legality, equipment comparison or loot priority.

The user-facing primary wording is:

- empty equipment target: `[장비 위치] 장착`
- body armor replacement: `방탄복 교체`
- headset replacement: `헤드셋 교체`
- other top-level equipment/carrier replacement: `[장비 위치] 교체`
- body armor + ordinary rig -> armored rig: `방탄 리그 전환`
- non-destructive store/repack: `[보관 위치] 보관`
- destructive storage replacement: `[보관 위치] [기존 아이템] 버리고 보관`
- no preferable legal plan: `버리기`

Equipment-performance explanation such as armor-class delta, headset tuning text or `내부 N개 재배치` is not repeated in the raid instruction. Those facts remain part of the decision logic; removing them from the sentence does not weaken the planner.

## Decision 2 — only materially distinct extra manipulations are spoken

Repacking inside the same visible storage area is ordinary Tarkov inventory manipulation. Grid index, X/Y position and rotation changes inside that same area are therefore intentionally omitted from the instruction.

An existing item receives an extra instruction only when the proposed plan requires it to:

- cross to a different root storage area or a different nested-container instance: `+ [아이템] 이동 [이동할 위치]`;
- leave the modeled raid inventory: `+ [아이템] 버리기`.

When more than one extra manipulation exists, entries are separated by `, `. Mixed move/discard operations keep the same grammar rather than falling back to a verbose explanatory sentence.

For destructive storage replacement, removed item names are part of the primary sentence. Other cross-area moves remain `+` operations.

## Decision 3 — storage-area identity

For instruction suppression only, a visible storage area is identified as:

- root storage: `FarmingGuideStorageKind` (Pockets/Rig/Backpack/SecureContainer/SpecialSlots);
- nested storage: the owning `ParentInstanceId`.

This definition is deliberately coarser than mechanical placement identity. Mechanics still distinguish real grid index, coordinates and rotation. The formatter merely avoids narrating rearrangement the user can perform naturally inside the same bag, rig, secure container or nested case.

## Decision 4 — Workbench fit owns both scroll axes

Nested Workbench sizing must be based on the real rendered grid footprint plus title/close chrome, border/padding and ScrollViewer template chrome.

When the complete grid fits the effective center-column viewport:

- horizontal scrollbar is disabled;
- vertical scrollbar is disabled;
- the Workbench grows enough to expose the whole grid without clipping.

When content genuinely exceeds the available viewport, scrolling remains a physical fallback. Horizontal and vertical scrollbar need is resolved together because either scrollbar reduces the opposite axis.

## Regression contract

The published-product smoke includes a 4x4 Key-tool-like nested storage fixture and fails unless both scroll axes are disabled and `ScrollableWidth` / `ScrollableHeight` are effectively zero when the grid fits.

The same smoke verifies the compact wording contract for:

- equipment attach;
- body armor/headset/general equipment replacement;
- carrier replacement with same-area repacking suppressed;
- armored-rig transition;
- direct storage;
- same-area repacking storage;
- destructive storage replacement with multiple comma-separated cross-area moves;
- discard.

## Non-changes

v1.15.5 does not change:

- preservation-first raid planning order;
- source-backed equipment superiority rules;
- dedicated-container preference;
- storage filters, dimensions or rotation mechanics;
- lock/reserved-cell constraints;
- complete-equipment boundary;
- explicit acceptance transaction semantics.
