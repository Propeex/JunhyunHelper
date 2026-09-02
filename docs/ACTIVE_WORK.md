# ACTIVE WORK

Status: **ACTIVE**

## Goal

Fix two user-reported Farming Guide regressions and perform a focused end-to-end Farming Guide audit, including the deterministic decision logic.

Confirmed user-visible symptoms:

1. farmed/looted value is not shown in Farming Guide;
2. when an item is dragged into a locked/reserved empty slot, the item disappears visually.

The maintenance work must preserve the existing v1.16.0/v1.16.1 Farming Guide contract except where required to correct these regressions.

## Base / branch

```text
public stable: v1.16.1
base main: c0ea1e5e1f65299fe7700f794ee8257544f41f29
exact v1.16.1 product source/tag target: 7fb148434d22fac823d57d88021f9615081c47cd
working branch: maintenance/v1.16.2-farming-guide-value-lock-2026-09-02
target version: v1.16.2 PATCH
PR: not opened yet
```

## Current step

Trace the Farming Guide state/render/update flows for raid value accounting and locked/reserved-slot drag placement, then reproduce both defects in deterministic regression coverage.

## Scope

- identify root cause for missing farmed value display and repair it;
- identify root cause for invisible item after drop into a locked/reserved empty slot and repair it;
- audit Farming Guide state transitions, rendering, persistence, nested storage, quantity/weight, locks/reserved slots, raid planning/instructions and deterministic rulebook interactions for adjacent regressions;
- add focused deterministic regressions for every corrected contract;
- validate Release build/publish, rendered Product UI smoke, Farming Guide smoke, graceful shutdown, Shutdown Race, package/checksum and normal CI before release;
- update canonical project documentation and release v1.16.2 if validation is green.

## Completed

- recovered v1.16.1 canonical state from `AGENTS.md`, `PROJECT_STATE.json` and `ACTIVE_WORK.md`;
- created the maintenance branch from current main;
- enumerated the Farming Guide implementation surface for focused analysis.

## Remaining

- root-cause analysis;
- regression tests;
- implementation;
- full Farming Guide audit and validation;
- version/release identity updates;
- PR / CI / merge / exact-main / release verification;
- documentation finalization and `ACTIVE_WORK: NONE`.
