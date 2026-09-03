# Decision — v1.17.0 Farming Guide Global Optimization Rulebook

Date: **2026-09-03 KST**  
Status: **CONFIRMED / IMPLEMENTATION IN PROGRESS**

This decision is the canonical product rulebook for Farming Guide raid loot decisions. It supersedes older Farming Guide rules wherever they conflict with this document, including tactical food/drink/ammunition retention, item-level weight/footprint priority tie-breaks, local-only insertion/displacement semantics, and automatic combat-performance upgrade heuristics.

## 1. Two rule domains

Farming Guide distinguishes **system rules** from **farming rules**.

### 1.1 System rules

System rules are not user farming preferences. They are invariants the program must always enforce so that every modeled state and recommendation is physically/legalistically valid in Tarkov and respects explicit user locks.

They include, without being limited to:

- item and grid dimensions;
- rotation and collision;
- storage-grid and container filters;
- nested-container legality and parent/child integrity;
- special-slot semantics;
- attachment / armor-plate / equipment compatibility;
- stack quantity semantics;
- explicit locked item positions;
- explicit locked cells / reserved cells;
- explicit locked equipment/carrier targets;
- indirect lock preservation (for example, a parent/container may not move if doing so would move an explicitly position-locked descendant);
- deterministic, fail-closed behavior when required item facts or a legal/optimal plan cannot be proven.

These rules exist because an invalid plan is not a valid candidate. They do not grant an item farming priority.

### 1.2 Farming rules

The user-facing farming policy is intentionally minimal:

1. **Retain currently needed Found-in-Raid units for quest/hideout progression first.**
2. **Subject to rule 1 and all system constraints, maximize the final retained economic value.**
3. **Respect the user-configured weight limit. Weight is the only user-configurable farming constraint.**

No other automatic tactical preference is part of the farming rulebook.

## 2. Needed FIR semantics are item- and quantity-based, not category-based

"Needed FIR" means an item unit that is currently required as Found in Raid by the product's quest/hideout progress model.

The item's category is irrelevant.

Examples:

- food that is currently FIR-needed is first-priority loot;
- the same food when no longer FIR-needed is ordinary economic loot;
- ammunition, magazines, medicine, drinks, weapons, armor, keys, barter items, currency, documents and all other categories follow the same rule.

Farming Guide must not infer that food, drink, ammunition, magazines, medicine, or any other combat/survival resource is tactically necessary merely because of its category or the current weapon/loadout.

If the user personally wants to preserve such an item regardless of the Farming Guide objective, the user expresses that intent by locking the item/position/target.

## 3. FIR priority is capped by remaining required quantity

The absolute FIR priority applies only up to the remaining required quantity for each item.

If an item has `N` remaining FIR-needed units, retaining more than `N` units does not create additional first-priority benefit. Surplus units are ordinary economic loot.

Therefore the primary optimization objective is:

> maximize the number of currently needed FIR units satisfied, capped independently at each item's remaining FIR need.

This prevents every duplicate copy of an item from receiving infinite/absolute protection merely because one or more copies are still needed.

## 4. Economic objective is final retained total value

After maximizing satisfied FIR need, Farming Guide maximizes the **total retained economic value of the complete final state**.

The current canonical economic source remains average Flea-market value unless another product decision explicitly changes that authority.

The product rule is **not** "highest price per slot" and is not a per-item greedy order.

Geometry, slot count, rotation, container capacity, dedicated storage, nesting, stack quantities, and Tetris effects matter because they determine which complete legal combinations fit. They are system feasibility/optimization mechanics, not independent farming priorities.

A solver may internally use value density, footprint, or other heuristics to order its search, but those heuristics may not change the final objective.

## 5. Weight is a hard farming constraint, not an item preference

Weight does not make one item intrinsically lower priority than another and must not be used as an item-level tie-break preference.

The configured carry-weight rule defines whether a final candidate state is admissible.

Among admissible states, the Farming Guide still optimizes:

1. satisfied FIR need;
2. total retained economic value.

If the manually reflected current state already exceeds the configured limit, the existing fail-safe principle remains valid: automatic advice must not make the modeled overweight condition worse, and may move toward or remain at a safer/equal state until it returns within the configured limit.

## 6. Global unlocked-item optimization model

A scan is not modeled as a local insertion problem.

Conceptually, for every confirmed scan:

1. preserve explicit locked items/positions/targets/cells as fixed system state;
2. take every other movable item from the current modeled raid state and place it into one candidate pool;
3. add the newly scanned item/quantity to that pool;
4. solve the complete legal final state from that pool under Tarkov placement rules and the weight constraint;
5. choose the lexicographically best state by:
   - maximum satisfied needed-FIR units;
   - maximum retained economic value;
6. derive the user instruction from the difference between current state and that optimal final state.

Current unlocked placement is not a farming preference. Keeping an item where it already is may be used only as a deterministic no-value-change tie/stability choice after the true farming objective is equal.

## 7. Locks are system state, not farming priority

Locks do not mean "this item is more valuable". They remove choices from the optimizer.

- a locked stored item remains at its exact modeled position for automatic advice;
- a locked empty cell remains unavailable;
- a locked equipment/carrier target is not automatically replaced;
- legal internal storage of a locked carrier remains usable unless an explicit cell/item lock prevents it;
- a parent/container/root that would indirectly move a locked descendant cannot be moved by automatic advice;
- manual editing remains authoritative and may directly change/remove locks under the existing lock lifecycle.

The optimizer solves only the remaining degrees of freedom.

## 8. No automatic combat/survival heuristics

The following are explicitly **not** Farming Guide priorities or retention rules:

- keeping at least one food item;
- keeping at least one drink item;
- preserving loose ammunition compatible with currently equipped weapons;
- keeping a minimum magazine count;
- preserving medicine because it is medicine;
- preferring armor solely because of armor class/durability;
- preferring headsets solely because of headset performance;
- any other inferred combat/survival reserve not explicitly represented by an existing user lock or a future separately confirmed product rule.

An exact item may still be first-priority if it is currently FIR-needed. Otherwise it competes by retained economic value.

## 9. Equipment and special storage are legal placement surfaces, not extra priorities

Equipment slots, carrier slots, attachments, armor-plate slots, secure storage, dedicated containers and nested storage may provide legal placement opportunities according to Tarkov rules.

Using a legal equipment/storage surface can improve the final feasible state because it changes capacity, but merely being an equipment upgrade does not create a third farming-priority tier.

If the user wants currently worn/held gear to be invariant for tactical reasons, the user locks it.

## 10. Optimality and bounded-runtime safety

The product must not claim a destructive/global rearrangement is optimal merely because a bounded heuristic found one feasible plan.

Implementation may use branch-and-bound, pruning, memoization, search ordering, or bounded fallback strategies for raid-time responsiveness. However:

- a destructive recommendation is emitted only when its rulebook safety is proven;
- if exact optimality cannot be established within the implementation's deterministic safety budget, the system fails closed rather than silently substituting a known-suboptimal destructive plan;
- deterministic tie-breaking may prefer fewer moves / current placement stability only after FIR satisfaction and total retained value are equal.

## 11. Formal objective

For every legal candidate final state `S`:

```text
Primary(S)   = sum over item ids min(retained FIR-qualified units in S, remaining FIR need)
Secondary(S) = total retained economic value in S
```

Choose the admissible state maximizing the tuple:

```text
(Primary(S), Secondary(S))
```

subject to:

```text
Tarkov-valid placement / compatibility / nesting
explicit lock invariants
user-configured weight constraint
```

Any further implementation ordering is non-product deterministic tie-breaking only.

## 12. Supersession notes

Where older documents/code say otherwise, this decision supersedes:

- local-first/direct-fit-first as a product preference;
- one-victim or bounded-victim economic replacement as the conceptual decision model;
- automatic tactical reserves for food/drink/current-weapon ammunition;
- weight and footprint as item-priority tie-breaks;
- armor/headset/combat-performance superiority as an independent automatic farming priority.

Older code paths may temporarily remain during migration, but they are implementation debt and are not authoritative product behavior after this decision.
