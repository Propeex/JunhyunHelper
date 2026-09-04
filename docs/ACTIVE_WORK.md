# ACTIVE WORK

Status: **ACTIVE**

## Goal

Remove the Farming Guide product feature completely at the user's explicit request.

## Base / branch

- base: current `main` / public stable v1.17.0
- working branch: `product/remove-farming-guide-2026-09-04`
- PR: not opened yet

## Confirmed scope

Remove Farming Guide-specific:

- product UI/navigation and overlays
- persisted Farming Guide state and presets
- raid-session lifecycle, solver/planner, locks, weight configuration and recommendation logic
- Scanner → Farming Guide bridge, acceptance flow, simulated Farming Guide scan actions and related hotkeys/settings
- Farming Guide-only models, services, policies and persistence
- Farming Guide-only tests/smokes
- current product/documentation references that present Farming Guide as an active feature

Preserve Scanner recognition itself, Items/Quest/Hideout/Ammo/Map and all unrelated product behavior.

Historical release/decision records may remain only where needed as immutable history, but must not be presented as current product authority.

## Completed

- recovered v1.17.0 current state and canonical Farming Guide authority
- user explicitly decided to remove the entire Farming Guide feature
- created working branch

## Current step

Inventory all code, UI, persistence, settings, tests, build/release smoke and documentation dependencies before deletion.

## Remaining

- remove implementation and integration points
- repair compile/runtime dependencies
- remove/adjust tests and smoke coverage
- update current product/state/reference documentation and version facts
- run deterministic tests, Release build/publish and applicable runtime smoke
- open PR, validate CI/review
- merge, exact-main validation and release if all gates pass
