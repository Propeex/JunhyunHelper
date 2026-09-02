# ACTIVE WORK

Status: **ACTIVE**

## Goal

Prepare v1.16.4 PATCH hotfix for a user-reported Farming Guide regression in v1.16.3: an explicitly locked stored item can be included in an automatic movement/repacking instruction.

Observed real-play evidence: the Farming Guide instructed moving a locked Grizzly emergency kit while evaluating a scanned Wires item.

## Base

```text
public stable: v1.16.3
exact v1.16.3 product source/tag target: 89fae2e07b721b1dfd4922642412fcebf01b275d
base main: eecaf1c772a17ec5c7c000d3c66f02b6b59c6770
branch: fix/v1.16.4-farming-guide-locked-item-position-2026-09-02
target version: v1.16.4 PATCH
PR: #284 (draft)
```

## Confirmed scope

- An explicitly locked stored item is position-locked for automatic Farming Guide decisions.
- Automatic advice must not discard, replace, relocate, rotate, re-parent, or otherwise change the storage placement of that exact locked item instance.
- Moving an ancestor container or replacing the root carrier must not indirectly move a locked descendant.
- The user may still manually edit inventory state; the restriction applies to automatic Farming Guide recommendations.
- Locking a carrier root still does not disable use of its legal internal storage. Unlocked contents inside a locked carrier may be used/repacked according to normal rules.
- Locking a stored container freezes that stored container itself; its independently unlocked contents remain usable unless moving the container would move a locked descendant.
- Reserved-cell behavior remains unchanged.

## Root cause

v1.16.3 deliberately changed exact-item lock semantics to identity preservation only. Both the secure-protection planner and general repacking transition planner allowed the same locked `InstanceId` to move, carrier migration could repack a locked descendant, and final safety checked only that the locked instance still existed. This contradicts the user's intended lock meaning.

## Completed

- Recovered the released v1.16.3 canonical state and exact product source.
- Confirmed the incorrect movement semantics in v1.16.3 planning code and in the published decision smoke that explicitly expected a locked item to move.
- Added a v1.16.4 automatic-planning boundary that rejects secure-promotion/carrier-upgrade plans which alter locked placement.
- Added a lock-aware repacking path where a locked item, or any stored ancestor containing a locked descendant, is a hard geometry obstacle.
- Added final fail-closed validation of exact storage kind/grid/X/Y/rotation/parent/quantity, ancestor placement, and root carrier identity for every locked stored item.
- Routed live raid advice through the v1.16.4 planning, transition and final-safety path.
- Replaced the obsolete published lock-movement expectation with v1.16.4 smoke coverage for the user-observed secure-container case, general repacking, final safety and root-carrier replacement.
- Opened draft PR #284 for integrated validation.

## Current step

Resolve any CI findings from the first integrated v1.16.4 candidate, then align release identity and documentation only after the corrected product logic and published smoke are green.

## Remaining

- obtain a green pre-release-identity product candidate;
- update product/decision documentation where v1.16.3 recorded the incorrect lock semantics;
- bump release identity to v1.16.4 and add release notes;
- run final PR CI / Shutdown Race / Documentation Consistency and published EXE smoke;
- merge, exact-main validate, publish v1.16.4, verify assets/digests;
- finalize canonical docs and close ACTIVE_WORK to NONE.
