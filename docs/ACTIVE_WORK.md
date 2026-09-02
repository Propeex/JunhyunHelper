# ACTIVE WORK

Status: **ACTIVE**

## Goal

Perform a repository-backed **v1.16.1 product quality / maintenance pass** over 준현 헬퍼, covering internal correctness, functional behavior, user-visible WPF/UI robustness, persistence/error handling, tests, packaging, CI, and release-readiness without changing confirmed product behavior without evidence.

## Base / working state

```text
base main: 14c8c6c2d7edc3ca248490af843b6fb5749ec41a
public stable: v1.16.0
working branch: maintenance/v1.16.1-product-quality-pass-2026-09-02
current PR: #276
superseded PR: #275 (closed unmerged; Draft→Ready connector mutation incompatible with current GitHub GraphQL schema)
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
- Versioned candidate `33588318252` found one release metadata omission: `docs/RELEASE_NOTES_V1.16.1.md` was missing. Build and 611 other tests passed; Shutdown Race `33588318203` and Documentation Consistency `33588318166` passed.
- Added `docs/RELEASE_NOTES_V1.16.1.md`.
- Corrected head `f037687346195eb02fc06088ca46d46e3d4d3bae` passed CI `33588730993`, Shutdown Race `33588731048`, Documentation Consistency `33588730995`: 612 tests, Windows publish, published EXE Product UI + Map + graceful shutdown, package/checksum verification, artifact upload all succeeded.
- PR #275 could not be transitioned from Draft by the connected GitHub tool because its GraphQL response selection references a removed GitHub field. REST correctly refused to merge a Draft PR. #275 was closed unmerged; the validated branch was not changed.
- Opened normal PR #276 from the same branch/base so standard merge flow can continue without product-code workaround.

## Current step

Validate the current PR #276 head after this checkpoint-only commit, then merge only if CI, Shutdown Race, and Documentation Consistency are green.

## Remaining

- Confirm PR #276 final head: 612 deterministic tests, Windows publish, Product UI/Map/graceful-shutdown smoke, package/checksum, Shutdown Race, Documentation Consistency.
- Merge PR #276 to main and verify exact-main CI / Shutdown Race / Documentation Consistency.
- Verify the automatic v1.16.1 release workflow, tag, source target, `Junhyun-Helper.zip`, checksum asset, sizes, and hashes.
- Update `PROJECT_STATE`, `CURRENT_STATE`, `STATE`, README/release notes as required using real release metadata.
- Close `ACTIVE_WORK` to `NONE` only after public release and documentation are exact and verified.
