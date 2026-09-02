# ACTIVE WORK

Status: **ACTIVE**

## Goal

Perform a repository-backed **v1.16.1 product quality / maintenance pass** over 준현 헬퍼, covering internal correctness, functional behavior, user-visible WPF/UI robustness, persistence/error handling, tests, packaging, CI, and release-readiness without changing confirmed product behavior without evidence.

## Base / working state

```text
base main: 14c8c6c2d7edc3ca248490af843b6fb5749ec41a
public stable: v1.16.0
working branch: maintenance/v1.16.1-product-quality-pass-2026-09-02
PR: #275 (draft)
```

## Confirmed scope

- Audit canonical product/maintenance contracts against implementation and tests.
- Inspect user-visible WPF/UI for clipping, sizing, scrolling, focus/input, DPI/layout, loading/empty/error states, and lifecycle regressions.
- Inspect core state/data flows, Farming Guide, Scanner, settings/persistence, update/error paths, resource cleanup, and shutdown behavior.
- Identify concrete defects or high-confidence maintenance risks; fix them only where behavior is already defined or the correction is semantics-preserving.
- Add/strengthen deterministic regression coverage for fixes.
- Validate tests, Release/publish path, CI, shutdown-race coverage, documentation consistency, and exact-main state as applicable.
- Keep changes proportional; no speculative feature work or broad redesign.

## Completed

- Recovered current canonical project state and v1.16.0 release identity.
- Confirmed previous work was closed (`ACTIVE_WORK` was `NONE`) and created the maintenance branch.
- Audited product/architecture/maintenance contracts and high-risk runtime/persistence paths across MainWindow, Scanner, Farming Guide, update, storage, image-cache, service ownership, and Map window-state code.
- Hardened `FarmingGuidePresetStore` so syntactically valid but semantically partial/null JSON is normalized instead of throwing. Valid salvageable state is preserved, invalid/null collections/items are repaired or discarded, stack quantity/weight values are normalized, and legacy dogtag persistence remains removed.
- Added a deterministic persistence regression proving partial Farming Guide JSON can be loaded, normalized, saved, and reloaded.
- Hardened opportunistic content-schema migration against stale async continuations: a migration started for one profile/game mode cannot later apply its content to a different active profile.
- Added a deterministic source-contract regression preserving the stale-profile guards.
- Opened draft PR #275.
- Documentation Consistency passed on the current reviewed implementation commit.
- Windows CI desktop build and deterministic core tests passed; publish/runtime smoke/package phases are still running.
- UI review confirmed substantial existing rendered WPF smoke coverage for Scanner, Ammo, Farming Guide, nested storage/workbenches, overlays, Quest sidebar, Map, and published EXE startup/shutdown. One remaining verification gap is explicit minimum-main-window containment for the full Farming Guide/header layout; no concrete clipping defect has been reproduced from repository/runtime evidence yet.

## Current step

Finish Windows published-candidate/runtime/package and shutdown-race validation, continue focused high-risk UI/state review, then review the complete PR diff for unintended product changes.

## Remaining

- Complete current Windows published EXE/UI/Map/graceful-shutdown/package CI and shutdown-race validation.
- Address any CI/runtime finding; otherwise avoid speculative UI/layout changes without reproduced failure evidence.
- Review PR diff and versioning/release contracts.
- Bump PATCH/version/docs as required for v1.16.1 and run final CI on the release-ready branch.
- Mark PR ready, merge when all required checks are green, and verify exact-main CI.
- Publish/verify v1.16.1 release/tag/assets if required by the repository release contract.
- Update project state/docs and close this checkpoint only when the maintenance pass is fully merged, verified, and released.
