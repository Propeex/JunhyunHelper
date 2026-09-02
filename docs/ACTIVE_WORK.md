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
- Validate tests, Release/publish path, CI, shutdown-race coverage, documentation consistency, exact-main state, and public release artifacts.
- Keep changes proportional; no speculative feature work or broad redesign.

## Completed

- Recovered the canonical v1.16.0 stable state and audited high-risk runtime, persistence, lifecycle, UI-smoke, packaging, and release paths.
- Hardened `FarmingGuidePresetStore` against syntactically valid but semantically partial/null JSON while preserving salvageable state and existing schema/legacy contracts.
- Added a deterministic partial-state load/save/reload regression.
- Hardened opportunistic content-schema migration against stale async continuations across profile/game-mode changes.
- Added a maintenance source-contract regression preserving the profile identity guards.
- Reviewed Scanner UI-state/settings, Map/MiniMap settings/window-state, atomic storage, content activation, image cache, updater, service ownership/disposal, and published WPF smoke coverage; no further concrete defect justified speculative behavior/layout changes.
- Initial implementation validation passed end-to-end: Documentation Consistency `33587976453`, CI `33587976468`, Shutdown Race `33587976484` all succeeded, including published EXE Product UI + Map + graceful shutdown and package verification.
- Bumped the PATCH target to `1.16.1`, updated package identity, and kept `publicStable` at v1.16.0 until an actual v1.16.1 release exists.
- Final versioned candidate run `33588318252` failed only in `ReleaseIdentityTests.ProjectFirstRunAndReleaseNotesUseTheSameVersion`: `docs/RELEASE_NOTES_V1.16.1.md` was missing. Build succeeded and the remaining test result was 611 passed / 1 failed / 0 skipped. Shutdown Race `33588318203` and Documentation Consistency `33588318166` succeeded.
- Added `docs/RELEASE_NOTES_V1.16.1.md` with the actual maintenance scope and 612-test release target.

## Current step

Validate the corrected release-ready PR head end-to-end, then mark PR #275 ready and merge only if CI, Shutdown Race, and Documentation Consistency are all green.

## Remaining

- Confirm corrected PR head: 612 deterministic tests, Windows publish, Product UI/Map/graceful-shutdown smoke, package/checksum, Shutdown Race, Documentation Consistency.
- Review final diff and mark PR #275 ready.
- Merge to main and verify exact-main CI / Shutdown Race / Documentation Consistency.
- Verify the automatic v1.16.1 release workflow, tag, source target, `Junhyun-Helper.zip`, checksum asset, sizes, and hashes.
- Update `PROJECT_STATE`, `CURRENT_STATE`, `STATE`, README/release notes as required using real release metadata.
- Close `ACTIVE_WORK` to `NONE` only after public release and documentation are exact and verified.
