# ACTIVE WORK

Status: **ACTIVE**

## Goal

Complete the v1.17.3 whole-product optimization, stability-hardening and visual-finishing PATCH from the clean v1.17.2 baseline.

No new user-facing product capability is introduced. Confirmed Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner behavior, Scanner recognition safety, supported schema/read compatibility and user-owned state remain preserved.

## Base / branch

- base main: `12f3e967fa02b80cbea134f92288ac8c6ae67644`
- exact public stable product source: `73f0386a45818408c2a68530b90de7946ecaf1d1`
- current public stable: `v1.17.2`
- working branch: `maintenance/v1.17.3-stability-optimization-2026-09-04`
- target release: `v1.17.3`

## Confirmed scope

- optimize and harden only existing product behavior; no new user-facing feature;
- preserve Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner semantics and pinned donor behavior;
- preserve Scanner recognition thresholds, pacing and matcher safety;
- preserve supported schema/read compatibility and user-owned state;
- improve only evidence-backed correctness, lifetime, concurrency, repeated-work efficiency and visual consistency;
- finish as PATCH v1.17.3 only after full PR, exact-main and public release verification.

## Completed

### Repeated-work / lookup efficiency

- Quest, Hideout and Items reuse content indexes instead of repeatedly scanning canonical content.
- MainWindow rebuilds Quest/Hideout/Items workspaces from one authoritative immutable profile snapshot.
- Scanner catalog snapshot allocation was removed from repeated search paths.
- Scanner canonical item/quest/trader/station lookups and item→Quest/Hideout requirement usages are indexed per content snapshot.
- shared ImageCache performs per-path single-flight download/decode and keeps decoded image references weak.
- Map Quest marker inverse scale reacts to ScaleTransform changes instead of a permanent 120ms DispatcherTimer.

### Correctness / concurrency

- manual update, startup schema refresh, Map-triggered schema refresh, first-run provisioning and recovery updates share one product content-operation gate.
- first-run/recovery paths re-check under the gate before starting duplicate network work.
- mutation failures rebuild authoritative profile-derived presentation.
- Hideout pending debounce is flushed before switching station.
- cancelling a Hideout rollback restores the authoritative row presentation instead of leaving the optimistic preview.

### Shutdown / lifetime safety

- MainWindow lifetime cancellation covers profile I/O, Quest/Hideout/Items mutations, data update/prefetch, Scanner sync, PC diagnostic and related UI progress callbacks.
- ProgramUpdateCoordinator has its own lifetime cancellation and treats application shutdown cancellation as normal.
- shutdown-time recovery cancellation no longer produces misleading diagnostics.
- existing Scanner runtime cancellation/epoch and Map async smoke lifetime boundaries were reviewed and preserved.

### UI / WPF

- shared Button style exposes keyboard focus with the product AccentBrush.
- Quest/Hideout/Items/Ammo/Scanner main layout, minimum-width, scrolling, clipping and virtualization contracts were audited; no speculative visual redesign was introduced.

## Functional candidate validation before release identity staging

Validated branch head:

`815fd5f9da739feb552ccc25bb14843df7d465fd`

Passed:

- CI `33845819785` — SUCCESS;
- Shutdown Race `33845819872` — SUCCESS;
- Documentation Consistency `33845819832` — SUCCESS;
- **503 passed / 0 failed / 0 skipped**;
- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / Map / Scanner smoke;
- graceful shutdown;
- release package/checksum verification.

A prior validation exposed one nullable compile warning in the new weak image cache and three stale source-contract assertions; both were corrected before the successful candidate above.

## Release-identity PR validation

Validated release-identity branch head:

`8adb26a945b650a86df95c6b465944e52edebfc8`

Passed:

- CI `33846264074` — SUCCESS;
- Shutdown Race `33846264056` — SUCCESS;
- Documentation Consistency `33846264066` — SUCCESS;
- **503 passed / 0 failed / 0 skipped**;
- Windows Release build;
- win-x64 self-contained publish;
- actual published EXE Product UI / Map / Scanner smoke;
- graceful shutdown / active-async close;
- stable package/checksum validation and Actions artifact upload.

## Current step

Run the final PR diff/review check on the documentation-checkpoint HEAD, mark PR #294 ready and merge with an exact expected head SHA.

## Remaining

- final PR diff/review-thread check;
- mark PR ready and merge with exact expected head;
- exact-main CI / Shutdown / Documentation Consistency;
- automatic stable `v1.17.3` Release workflow;
- verify public tag/release/package/checksum and exact-main artifact identity;
- write final release evidence to PROJECT_STATE / release-status / README / CURRENT_STATE / STATE;
- close ACTIVE_WORK to `NONE`.
