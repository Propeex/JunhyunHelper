# ACTIVE WORK

Status: **ACTIVE**

## Goal

Fix two user-reported Farming Guide regressions and perform a focused end-to-end Farming Guide audit, including the deterministic decision logic.

## Confirmed scope

1. Farming Guide must show the value of loot actually farmed during the active raid.
2. An item manually dragged into a reserved/locked empty cell must remain visible; reservation continues to protect the cell from automatic placement rather than prohibiting direct user placement.
3. Farmed value follows the existing economic contract: **net currently retained acquired quantity relative to the raid-start baseline × average Flea Market price**. Baseline items do not count, lost baseline items cannot make the value negative, and loot discarded later is removed from the displayed total.
4. Existing v1.16.0/v1.16.1 deterministic Farming Guide rulebook behavior remains unchanged unless a concrete adjacent regression is reproduced during the audit.
5. The work includes focused audit/validation of raid state transitions, destructive retention, locks/reserved cells, nested storage, quantity/weight, persistence, UI refresh, published WPF smoke and release/package paths.

## Base / branch

```text
public stable: v1.16.1
base main: c0ea1e5e1f65299fe7700f794ee8257544f41f29
exact v1.16.1 product source/tag target: 7fb148434d22fac823d57d88021f9615081c47cd
working branch: maintenance/v1.16.2-farming-guide-value-lock-2026-09-02
target version: v1.16.2 PATCH
PR: #279
```

## Current step

Run the first implementation validation while completing the focused Farming Guide rulebook/state audit. Correct any deterministic or published-WPF regression before versioning the release candidate.

## Root causes

- **Missing farmed value:** `RefreshSummary()` still hard-coded `ValueSummaryText.Text = "—"`; the summary had never been connected to raid baseline/current-state accounting.
- **Invisible item in reserved cell:** reservation overlays were rendered at Z-index 50 while placed item cards use the default Z-index 0, so the reservation marker visually covered a successfully placed item.

## Implemented so far

- added `FarmingGuideRaidValuePolicy` using recursive `FarmingGuideSnapshotInventoryCounter.AcquiredSinceAll` deltas and average Flea Market prices;
- wired active-raid value presentation to the raid baseline and live `BuildSnapshot()` state, with scanned Flea prices retained for stable refreshes and bridge resolution as fallback;
- moved the reserved-cell marker behind placed items while keeping its automatic-placement reservation semantics unchanged;
- added deterministic tests for baseline exclusion, stack quantity, lost baseline items, discarded loot, nested assembly contents and unknown/non-positive price handling;
- added source contract coverage for the two corrected UI contracts;
- added published WPF product smoke that checks the rendered farmed value and directly asserts the reservation marker Z-index stays below the placed item card;
- opened PR #279 for continuous CI while the broader Farming Guide audit continues.

## Validation status

Initial PR validation on head `5d7adabbdab35053b8e1ddd24fbb14f7bd4263b3`:

- Documentation Consistency `33594717790`: FAILED only because this ACTIVE checkpoint lacked the mandatory `## Confirmed scope` section; this checkpoint corrects that formatting failure.
- CI `33594717808`: in progress at checkpoint time.
- Shutdown Race `33594717792`: in progress at checkpoint time.

## Remaining

- finish focused Farming Guide logic/state/weight/quantity/persistence audit;
- resolve any first-pass CI findings;
- update v1.16.2 version / first-run / release notes / project-state candidate facts;
- obtain green PR CI, Shutdown Race and Documentation Consistency including published Product UI/Map/Farming Guide/graceful-shutdown smoke and package/checksum validation;
- merge, validate exact-main, publish and verify immutable v1.16.2 assets;
- finalize README/CURRENT_STATE/STATE/PROJECT_STATE/release evidence;
- close `ACTIVE_WORK` to `NONE`.
