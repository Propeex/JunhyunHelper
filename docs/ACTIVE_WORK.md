# ACTIVE WORK

Status: **ACTIVE**

## Goal

Perform the v1.17.3 whole-product optimization, stability-hardening and visual-finishing pass on the clean v1.17.2 baseline.

This is a **PATCH maintenance release**. It adds no new product feature and must preserve confirmed user behavior while making the existing product more robust, predictable, efficient and visually consistent.

## Base / branch

- base main: `12f3e967fa02b80cbea134f92288ac8c6ae67644`
- exact public stable product source: `73f0386a45818408c2a68530b90de7946ecaf1d1`
- public stable: `v1.17.2`
- working branch: `maintenance/v1.17.3-stability-optimization-2026-09-04`
- target release: `v1.17.3`

## Confirmed scope

Audit the current product end-to-end for evidence-backed improvements in:

- correctness and fail-closed behavior;
- startup/shutdown and async lifetime safety;
- cancellation, event subscription and resource disposal;
- persistence, atomic writes, transactions and recovery;
- concurrency/serialization and race resistance;
- network/update failure isolation;
- cache/data-structure/repeated-work efficiency where a real hot path or redundant work is visible;
- WPF layout/rendering/lifecycle robustness;
- visual consistency, spacing, typography, clipping, alignment and current design-system coherence;
- diagnostics and regression-test coverage for defects found.

Preserve:

- all confirmed current product behavior and product semantics;
- current Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner contracts;
- pinned Map donor integration unless a concrete defect is proven;
- Scanner recognition thresholds/pacing/matching safety unless reviewed evidence proves a correctness need;
- supported schema/read compatibility and user-owned state;
- public v1.17.2 release identity.

Do not add new user-facing product capabilities or speculative architecture.

## Audit order

1. platform/startup/shutdown/shared lifetime;
2. persistence/update/data-integrity boundaries;
3. profile/Quest/Hideout/Items/Ammo workspaces and mutation paths;
4. Scanner runtime/OCR/catalog/diagnostics;
5. Map/MiniMap first-party bridge and resource ownership;
6. WPF pages/shared controls/overlay/layout/design consistency;
7. packaging/release/diagnostic contracts;
8. full regression and published EXE verification.

## Completed

- recovered the finalized v1.17.2 public-stable repository state;
- classified this work as PATCH maintenance under `docs/VERSIONING.md`;
- created the dedicated v1.17.3 maintenance branch and checkpoint.

## Current step

Run repository-wide evidence gathering and subsystem audits, then implement only changes with concrete correctness, stability, efficiency or UI-quality justification.

## Remaining

- complete broad static/runtime-contract audit;
- implement and test evidence-backed improvements by subsystem;
- update architecture/reference/state docs for material changes;
- stage v1.17.3 release identity;
- run full deterministic tests, Release build, published EXE Product UI/Map/Scanner smoke, Shutdown Race, package and Documentation Consistency gates;
- final PR review, merge, exact-main validation and public v1.17.3 release;
- close ACTIVE_WORK with exact evidence.
