# ACTIVE WORK

Status: **ACTIVE**

## Goal

Fix two user-reported Farming Guide regressions and perform a focused end-to-end Farming Guide audit, including the deterministic decision logic.

## Base

```text
public stable: v1.16.1
base main: c0ea1e5e1f65299fe7700f794ee8257544f41f29
exact v1.16.1 product source/tag target: 7fb148434d22fac823d57d88021f9615081c47cd
branch: maintenance/v1.16.2-farming-guide-value-lock-2026-09-02
target version: v1.16.2 PATCH
PR: #279
```

## Confirmed scope

1. Farming Guide must show the value of loot actually farmed during the active raid.
2. An item manually dragged into a reserved/locked empty cell must remain visible; reservation continues to protect the cell from automatic placement rather than prohibiting direct user placement.
3. Farmed value follows the existing economic contract: **net currently retained acquired quantity relative to the raid-start baseline × average Flea Market price**. Baseline items do not count, lost baseline items cannot make the value negative, and loot discarded later is removed from the displayed total.
4. Existing v1.16.0/v1.16.1 deterministic Farming Guide rulebook behavior remains unchanged unless a concrete adjacent regression is reproduced during the audit.
5. The work includes focused audit/validation of raid state transitions, destructive retention, locks/reserved cells, nested storage, quantity/weight, persistence, UI refresh, published WPF smoke and release/package paths.

## Completed

- recovered v1.16.1 canonical state from official project memory and created the v1.16.2 maintenance branch;
- reproduced the missing-value root cause: the summary hard-coded `ValueSummaryText.Text = "—"` instead of evaluating raid state;
- reproduced the reserved-cell visual root cause: reservation overlays used Z-index 50 while placed item cards use the default Z-index 0;
- added `FarmingGuideRaidValuePolicy` using recursive net-acquired inventory deltas and average Flea Market prices;
- wired active-raid value presentation to raid baseline + live `BuildSnapshot()` state, remembering scanned Flea prices with bridge resolution as fallback;
- moved the reserved-cell marker behind placed item cards without changing reservation/manual-drag semantics;
- added deterministic value-policy tests for baseline exclusion, stack quantity, lost baseline items, discarded loot, nested inventory and unknown/non-positive prices;
- added source contract coverage for the two corrected UI contracts;
- added published WPF product smoke that renders the farmed value and directly asserts reservation-marker Z-index remains below the placed item card;
- completed focused review of rulebook priority, destructive victim-set comparison, equipment/carrier representative superiority, protected locks/reservations, nested repacking, quantity, weight, persistence and Scanner bridge state; no additional reproducible rulebook defect requiring behavior change was found;
- verified the pre-version implementation candidate on head `f3639cc9295c0d3f1eb3070061bd4251ac30a515`: CI `33594845425` SUCCESS including build, deterministic tests, Windows publish, published Product UI/Map/Farming Guide/graceful-shutdown smoke, package/checksum and artifact upload; Shutdown Race `33594845418` SUCCESS; Documentation Consistency `33594845407` SUCCESS;
- raised desktop version to 1.16.2 while preserving the project-file diff as version-only;
- diagnosed the first versioned candidate failure on head `9156382c0007abaa89ee16ee279aec6394ee4d59` as release-identity only: build succeeded, but `FIRST_RUN_KO.txt` still identified v1.16.1 and project-memory desktopVersion still identified v1.16.1;
- aligned `FIRST_RUN_KO.txt`, `docs/PROJECT_STATE.json` candidate desktopVersion and deterministic test count with v1.16.2;
- added `docs/RELEASE_NOTES_V1.16.2.md` with the two root-cause fixes and Farming Guide audit contract.

## Current step

Validate the fully version-aligned v1.16.2 PR candidate. If all PR gates are green, merge PR #279, revalidate exact-main, then publish and verify the public v1.16.2 release before closing project memory.

## Validation status

Historical checkpoint-format failures:

- Documentation Consistency `33594717790`: FAILED because `## Confirmed scope` was missing.
- Documentation Consistency `33594792044`: FAILED because `## Completed` was not using the mandatory heading.
- Both were documentation checkpoint-format failures only and were corrected.

Green pre-version implementation validation:

- CI `33594845425`: SUCCESS.
- Shutdown Race `33594845418`: SUCCESS.
- Documentation Consistency `33594845407`: SUCCESS.
- Deterministic tests at this stage: 619 total after the new Farming Guide regressions were added.
- Windows publish + published EXE Product UI/Map/Farming Guide/graceful shutdown + package/checksum: SUCCESS.

Expected version-identity gate on head `9156382c0007abaa89ee16ee279aec6394ee4d59`:

- build: SUCCESS;
- test suite: 618 passed / 1 failed; the only failure was `ReleaseIdentityTests.ProjectFirstRunAndReleaseNotesUseTheSameVersion` because FIRST_RUN still said v1.16.1;
- Documentation Consistency also failed because candidate `PROJECT_STATE.product.desktopVersion` had not yet been raised to 1.16.2;
- those identity inputs are now aligned on the current candidate.

## Remaining

- obtain green PR CI, Shutdown Race and Documentation Consistency on the fully version-aligned candidate;
- inspect the final PR diff and merge PR #279;
- validate exact-main CI, Shutdown Race and Documentation Consistency including published Product UI/Map/Farming Guide/graceful-shutdown smoke and package/checksum validation;
- run the automatic release flow for v1.16.2 and verify exact tag target, release metadata, ZIP/checksum assets and SHA-256 digests;
- finalize README/CURRENT_STATE/STATE/PROJECT_STATE/release notes/release evidence with immutable public facts;
- close `ACTIVE_WORK` to `NONE`.
