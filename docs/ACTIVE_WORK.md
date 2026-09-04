# ACTIVE WORK

Status: **ACTIVE**

## Goal

Remove the Farming Guide product feature completely at the user's explicit request and release the removal as v1.17.1.

## Base / branch

- base: public stable v1.17.0
- branch: `product/remove-farming-guide-2026-09-04`
- PR: #290 (draft until final validation)

## Confirmed scope

Remove Farming Guide-specific UI, navigation, editor/presets, raid-session advisor, packing/repacking/loot logic, locks/weight/quantity flows, Scanner bridge/hotkey/Mini Scanner integration, persistence/services/domain policies, Game Content extension metadata and dedicated tests.

Preserve Quest, Hideout, Items, Ammo, Map/MiniMap, independent Scanner behavior, update safety and user-owned data.

Legacy `farming-guide.json` is no longer read/written and is not automatically deleted.

## Completed

- recovered v1.17.0 authority and captured the user's removal decision
- created branch and draft PR #290
- removed 101+ Farming Guide implementation/test files and all first-class UI/service/Scanner integration
- removed Farming Guide-only GameItem/importer metadata
- pre-version removal head `7901724fa7007860dc1220a667a10911bdaf4a9a` passed CI/Shutdown/Docs
- pre-version deterministic suite: 485 passed / 0 failed / 0 skipped
- pre-version published EXE Product UI / Map / Scanner smoke and graceful shutdown passed
- set target version to v1.17.1
- added current removal decision/release notes and removed active Farming Guide architecture authority
- updated current product/state/architecture documentation

## Current step

Run final PR #290 CI on the v1.17.1 version/document head and resolve any remaining compile, test, runtime-smoke, review or documentation findings.

## Remaining

- verify final PR CI / Shutdown Race / Documentation Consistency
- inspect final PR diff/review for unintended cross-feature deletion
- mark PR ready and merge
- verify exact-main CI / Shutdown Race / Documentation Consistency
- publish and verify v1.17.1 stable release/assets
- finalize PROJECT_STATE/CURRENT_STATE/STATE/README/ACTIVE_WORK with exact release evidence and close ACTIVE_WORK
