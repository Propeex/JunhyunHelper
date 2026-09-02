# ACTIVE WORK

Status: **ACTIVE**

## Goal

Perform a repository-backed **v1.16.1 product quality / maintenance pass** over 준현 헬퍼, covering internal correctness, functional behavior, user-visible WPF/UI robustness, persistence/error handling, tests, packaging, CI, and release-readiness without changing confirmed product behavior without evidence.

## Base / working state

```text
base main: 14c8c6c2d7edc3ca248490af843b6fb5749ec41a
public stable: v1.16.0
working branch: maintenance/v1.16.1-product-quality-pass-2026-09-02
PR: not created yet
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
- Confirmed previous work is closed (`ACTIVE_WORK` was `NONE`).
- Confirmed current main HEAD and created the maintenance branch.

## Current step

Repository-wide quality audit: recover product/architecture/reference contracts, enumerate code/tests/UI/workflows, then prioritize concrete findings by user impact and regression risk.

## Remaining

- Complete subsystem/UI/static audit.
- Implement justified corrections and regression tests.
- Run/inspect deterministic test + publish/Windows CI validation.
- Review resulting diff for unintended product changes.
- Create/validate PR, merge when green, verify exact-main.
- Update project state/docs and close this checkpoint when fully complete.
