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
- reproduced the missing-value root cause: the summary still hard-coded `ValueSummaryText.Text = "—"`;
- reproduced the reserved-cell visual root cause: reservation overlays used Z-index 50 while placed item cards use the default Z-index 0;
- added `FarmingGuideRaidValuePolicy` using recursive net-acquired inventory deltas and average Flea Market prices;
- wired active-raid value presentation to raid baseline + live `BuildSnapshot()` state, remembering scanned Flea prices with bridge resolution as fallback;
- moved the reserved-cell marker behind placed item cards without changing reservation/manual-drag semantics;
- added deterministic value-policy tests for baseline exclusion, stack quantity, lost baseline items, discarded loot, nested assembly contents and unknown/non-positive prices;
- added source contract coverage for the two corrected UI contracts;
- added published WPF product smoke that renders the farmed value and directly asserts reservation-marker Z-index remains below the placed item card;
- opened PR #279 for continuous CI while the broader Farming Guide audit continues.

## Current step

Run the implementation validation while completing the focused Farming Guide rulebook/state audit. Correct any deterministic or published-WPF regression before versioning the release candidate.

## Validation status

Initial PR validation on head `5d7adabbdab35053b8e1ddd24fbb14f7bd4263b3`:

- Documentation Consistency `33594717790`: FAILED because the first active checkpoint was missing the mandatory `## Confirmed scope` section.
- Subsequent Documentation Consistency `33594792044`: FAILED because the checkpoint used `## Implemented so far` instead of the mandatory `## Completed` heading.
- These are checkpoint-format failures only; this commit aligns the active checkpoint with the repository's exact required headings.
- CI / Shutdown Race remain under validation on the current branch.

## Remaining

- finish focused Farming Guide logic/state/weight/quantity/persistence audit;
- resolve any implementation/CI findings;
- update v1.16.2 version / first-run / release notes / project-state candidate facts;
- obtain green PR CI, Shutdown Race and Documentation Consistency including published Product UI/Map/Farming Guide/graceful-shutdown smoke and package/checksum validation;
- merge, validate exact-main, publish and verify immutable v1.16.2 assets;
- finalize README/CURRENT_STATE/STATE/PROJECT_STATE/release evidence;
- close `ACTIVE_WORK` to `NONE`.
