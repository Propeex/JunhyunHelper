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
- created dedicated maintenance branch.

## Current step

Inventory the repository and classify suspected impurities by whether they are truly unused, compatibility-critical, historical-only, or still part of current product behavior.

## Remaining

- full source/test/config/assets audit;
- remove only evidence-backed impurities;
- add/adjust regression tests for defects found;
- update current architecture/reference/state documentation as needed;
- run Windows Release build, deterministic tests, published EXE relevant product smoke and Shutdown Race;
- open PR and complete CI/review;
- merge, exact-main validation and PATCH release if code changes are retained;
- close ACTIVE_WORK with exact evidence.
