# ACTIVE WORK

Status: **ACTIVE**

## Goal

Perform a product-purity maintenance pass on the current v1.17.1 codebase.

This work is explicitly **not a performance-optimization pass**. The goal is to remove implementation impurities that no longer belong to the current product and fix defects discovered while doing so.

## Base / branch

- base main: `67c2da75f829f961c4ca1f019b544ef85d6dfad6`
- public stable product source: `4ad1f76ed7c2469e60d0822b229fe03f83c75816`
- public stable: `v1.17.1`
- working branch: `maintenance/v1.17.2-product-purity-2026-09-04`
- target: PATCH release if code changes remain after validation

## Confirmed scope

Audit and, where safe, remove or correct:

- dead/unreachable first-party code;
- removed-feature remnants and stale integration points;
- obsolete version-specific shims that are no longer required by supported compatibility;
- duplicate or superseded implementation paths;
- stale product/runtime flags, fields, settings, assets or tests that no longer have a current owner;
- incorrect stale comments/names/contracts that can mislead maintenance;
- latent correctness defects exposed by the cleanup.

Preserve:

- current product behavior and user-visible contracts;
- supported schema/read compatibility;
- required migration paths for existing user state;
- historical release/decision evidence where it remains official repository history;
- pinned Map donor integration and other explicitly retained product contracts.

Do **not** perform speculative performance optimization, broad redesign or unrelated feature work.

## Completed

- recovered current v1.17.1 official repository state;
- confirmed ACTIVE_WORK was NONE;
- created dedicated maintenance branch;
- classified historical release/decision evidence and active Map donor `Legacy*` bridges as retained, not cleanup targets;
- removed the unreachable one-time v1.6 updater bridge from current packaging/CI;
- removed Scanner's superseded standalone Settings/Advanced event path and runtime event rebinding;
- removed Scanner's old outer search-clear button/runtime concealment path;
- removed unreachable Scanner OCR-substitution editor UI/controller API while retaining the active persisted substitution engine;
- removed unreachable Scanner recognition-debug window while retaining active correction/debug evidence state;
- removed the hidden Items Quest/Hideout usage filter and its runtime concealment shim;
- removed the hidden Ammo summary row and dead summary-string computation;
- removed superseded MainWindow full-refresh Quest/Hideout/Items mutation handlers and runtime handler replacement;
- replaced hidden MainWindow `StatusText` state/event plumbing with a direct XAML Items cleanup indicator backed by current Items workspace state;
- updated affected maintenance contracts to assert the canonical direct paths rather than the retired shims.

## Current step

Run an early Windows PR build/test/runtime gate to detect compile/XAML/reference fallout from the first cleanup batch while continuing the current-document audit.

## Remaining

- full source/test/config/assets audit;
- remove only evidence-backed impurities;
- add/adjust regression tests for defects found;
- update current architecture/reference/state documentation as needed;
- run Windows Release build, deterministic tests, published EXE relevant product smoke and Shutdown Race;
- open PR and complete CI/review;
- merge, exact-main validation and PATCH release if code changes are retained;
- close ACTIVE_WORK with exact evidence.
