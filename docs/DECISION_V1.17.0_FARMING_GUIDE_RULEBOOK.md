# Decision — v1.17.0 Farming Guide Rulebook

Date: **2026-09-03 KST**  
Status: **CONFIRMED**

This document is the product authority for the v1.17.0 Farming Guide work restarted from stable `main`. It intentionally contains only user-confirmed product semantics. Implementation details may optimize performance, structure, testing, and solver strategy, but may not invent new product meaning.

## 1. Product boundary

Farming Guide has two kinds of rules:

- **system rules** — Tarkov/legal inventory mechanics and explicit user constraints;
- **farming rules** — how the program decides what is worth keeping.

System rules must never be promoted into extra farming preferences.

## 2. Active-raid FIR rule

When a Farming Guide raid session is active, an item newly identified by Scanner is treated by Farming Guide as **Found in Raid (FIR)**.

This is a Farming Guide raid-session rule.

Therefore:

- Scanner identifies the item; it does **not** determine whether the item is FIR;
- Farming Guide does **not** inspect a FIR icon, checkmark, color, text, or other screen signal;
- Farming Guide does **not** ask the user for a separate FIR confirmation;
- every newly Scanner-identified incoming item during the active Farming Guide raid enters the modeled raid state as FIR;
- items already present in the raid-start baseline do not become FIR merely because the raid session started.

No alternative FIR observation authority may be introduced without a new explicit product decision.

## 3. Farming rules

The farming objective is intentionally small.

### 3.1 First priority — currently needed FIR quantity

If the scanned/retained FIR item is currently required for Quest or Hideout progression, retaining the required units has absolute first priority.

The priority is **quantity-capped** by the remaining required amount for that exact item.

If 2 more units are required, only 2 retained FIR units receive this first-priority benefit. Surplus copies are ordinary economic loot.

Item category is irrelevant. Food, ammo, medicine, keys, armor, currency, barter items, and every other category follow the same rule.

### 3.2 Second priority — final retained economic value

After maximizing satisfied needed-FIR quantity, maximize the **total economic value of the complete retained final state**.

The existing product authority for this v1.17 work is average Flea-market value. Geometry or value-per-slot may be used internally to search faster, but they are not extra farming priorities.

### 3.3 Weight

The user's configured weight rule is the only user-configurable farming constraint.

Weight is a final-state admissibility constraint. It is not an item priority and lighter items do not automatically outrank heavier items.

## 4. Global unlocked-item decision model

A scan is not a local "where can this one new item fit?" problem.

For each active-raid scan:

1. preserve explicit user-fixed state;
2. consider every other movable modeled item together with the newly scanned item;
3. find the best complete Tarkov-legal final state under the weight constraint;
4. compare complete states by:
   1. satisfied needed-FIR quantity;
   2. total retained economic value;
5. derive the user instruction from the difference between the current modeled state and the chosen final state.

Current unlocked placement is not a farming preference. Layout stability may only break a true objective tie.

## 5. User-fixed state

The user's fixed-state concept is deliberately simple:

- items the user explicitly fixed;
- cells the user explicitly fixed/reserved.

The implementation may represent these constraints through stored-item, equipment, carrier, or cell lock structures as necessary, but those structures do not create additional farming value.

## 6. System rules

The solver must respect existing verified Tarkov mechanics, including as applicable:

- item dimensions, rotation, bounds, and collision;
- storage grids and source-backed container filters;
- nested-container legality;
- equipment/carrier compatibility and conflicts;
- special-slot rules;
- stack quantities;
- explicit user-fixed items/cells;
- modeled quantity, price, and weight facts;
- raid-session revision and explicit acceptance consistency.

These are legality/state rules only.

## 7. Explicit non-rules

Farming Guide must not automatically add priorities such as:

- keep food or drink for survival;
- keep ammunition for the current weapon;
- keep a minimum magazine count;
- keep medicine because it is medicine;
- prefer armor/headsets because of combat performance;
- prefer lighter items as a farming priority;
- prefer smaller items or higher value-per-slot as a farming priority;
- protect a category merely because it seems tactically useful.

If the user personally wants something preserved regardless of the farming objective, the user fixes it.

## 8. Uncertainty and destructive advice

Implementation safety may fail closed when a result cannot be proven from available system facts or within a deterministic solver boundary.

However uncertainty is not a product excuse to invent a new observation, confirmation, or classification flow.

- a proven discard may be shown as `버리기`;
- an unproven destructive result must not be presented as proven;
- adding a new user interaction, new screen-derived fact, new inference rule, or new failure meaning requires explicit product approval.

## 9. Development authority boundary

The developer may autonomously improve:

- performance;
- memory use;
- code structure;
- deterministic solver implementation;
- tests and regression coverage;
- internal diagnostics;
- reliability and maintainability;

provided the confirmed product behavior above does not change.

The developer must ask before introducing any new product-level:

- decision criterion;
- automatic inference;
- information/observation authority;
- cross-feature automatic behavior;
- user input or confirmation flow;
- visible failure/hold/discard semantics.

The abandoned PR #287 is not implementation authority for this restart.