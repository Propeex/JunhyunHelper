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

- recovered current v1.17.1 official repository state and created the dedicated maintenance branch / draft PR #292;
- classified historical release/decision evidence and active Map donor `Legacy*` bridges as retained, not cleanup targets;
- removed the unreachable one-time v1.6 updater/release bridge from current packaging, CI and updater package parsing/extraction;
- removed current-looking stale docs (`docs/NEXT.md`, retired `docs/FARMING_GUIDE.md`) and converted Scanner/deployment/reference docs to evergreen ownership;
- synchronized Content snapshot documentation with the actual v12 write / v3~v12 readable contract and strengthened Documentation Consistency against canonical code/schema drift;
- removed hidden MainWindow `StatusText` state/event plumbing and replaced it with the direct Items cleanup indicator;
- fixed the cleanup-indicator refresh regression exposed by orphan analysis and added a direct wiring contract;
- removed superseded full-refresh Quest/Hideout/Items mutation handlers and runtime handler rebinding; canonical mutation/content-navigation owners now have current names;
- removed hidden Items Quest/Hideout usage filter and its runtime concealment shim;
- canonicalized Ammo toolbar/search/favorite/detail presentation in XAML, removing hidden legacy popup/summary UI, runtime control creation, layout-repair shims and duplicate activation lifecycle;
- removed duplicate global search-clear lifecycle registration; Quest/Hideout/Items/Ammo/Scanner now attach the shared behavior from their explicit owners;
- canonicalized Profile create/edit UI ownership, removed hidden proxy controls, standalone duplicate MainWindow handlers, runtime button discovery/rebinding and runtime profile-card reparenting;
- removed unreachable Scanner OCR-substitution editor UI/controller API while retaining the active persisted substitution engine;
- removed unreachable Scanner recognition-debug Window while extracting the still-used diagnostic image renderer;
- removed the retired Scanner dedicated hotkey capture Window; Scanner Settings is the only hotkey capture authority;
- removed retired Scanner `필요한 곳` panel, its duplicate ItemsWorkspace source join, and narrowed Scanner usage-card navigation to the existing Quest/Hideout navigation contract;
- removed the hidden old Scanner three-row item summary and its dead update computation;
- canonicalized Scanner detail scrolling and favorite/Wiki action layout in XAML instead of runtime visual-tree repair;
- removed retired Mini Scanner identity/flea-minimum display settings while preserving compatible old JSON reading;
- removed unreachable Mini Scanner preview/position-edit/reset subsystem and its unused OCR dependency while preserving direct drag position persistence;
- audited Items/Hideout/Quest/Profile/Ammo/ScannerPage and Scanner Coordinator/runtime private methods; resolved actual orphan paths and retained XAML/cross-partial entrypoints that were false positives;
- confirmed current Scanner OCR wrapper chain (`DiagnosticScannerLab38OcrEngine → EnvironmentGuarded → Serialized → FontAware`) is active and must be retained;
- kept Scanner recognition thresholds/pacing/matching logic, Quest/Hideout domain rules and Map donor implementation unchanged;
- Documentation Consistency is passing on current cleanup iterations.
- updated the three stale deterministic contracts that still required removed Scanner/search-clear lifecycle structures; they now verify the canonical XAML/direct-owner paths instead of reviving retired code.
- renamed current Scanner and Ammo runtime/smoke partials that still carried obsolete version/`Polish`/`Fixes` ownership names; behavior and published verification contracts remain unchanged.
- staged the maintenance release identity as v1.17.2 per `docs/VERSIONING.md`; public stable remains v1.17.1 until exact-main release publication succeeds.

## Current step

Validate the v1.17.2 release-identity cleanup HEAD through Windows CI / published smoke / package, Shutdown Race and Documentation Consistency; then perform the final PR/diff review before merge and exact-main release validation.

## Remaining

- full source/test/config/assets audit;
- remove only evidence-backed impurities;
- add/adjust regression tests for defects found;
- update current architecture/reference/state documentation as needed;
- run Windows Release build, deterministic tests, published EXE relevant product smoke and Shutdown Race;
- open PR and complete CI/review;
- merge, exact-main validation and PATCH release if code changes are retained;
- close ACTIVE_WORK with exact evidence.
