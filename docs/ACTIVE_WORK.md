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
PR: not opened yet
```

## Confirmed product contract

- An explicitly locked stored item is position-locked for automatic Farming Guide decisions.
- Automatic advice must not discard, replace, relocate, rotate, re-parent, or otherwise change the storage placement of that exact locked item instance.
- The user may still manually edit inventory state; the restriction applies to automatic Farming Guide recommendations.
- Locking a carrier root still does not disable use of its legal internal storage. Unlocked contents inside a locked carrier may be used/repacked according to normal rules.
- Reserved-cell behavior remains unchanged.

## Root cause

v1.16.3 deliberately changed exact-item lock semantics to identity preservation only. Both the secure-protection planner and general repacking transition planner allowed the same locked `InstanceId` to move, while final safety checked only that the locked instance still existed. This contradicts the user's intended lock meaning.

## Current step

Correct all automatic repacking paths and final safety so explicit locked item placement is immutable, replace the v1.16.3 published smoke that asserts locked-item movement with a regression asserting exact placement preservation, and audit related lock handling for bypass paths.

## Remaining

- implement position immutability in secure promotion and general repacking;
- make final safety compare full locked-item placement state, not only instance survival;
- update published-EXE decision smoke and deterministic/source-contract coverage as appropriate;
- update product/decision documentation where v1.16.3 recorded the incorrect lock semantics;
- bump release identity to v1.16.4 and add release notes;
- run PR CI / Shutdown Race / Documentation Consistency and published EXE smoke;
- merge, exact-main validate, publish v1.16.4, verify assets/digests;
- finalize canonical docs and close ACTIVE_WORK to NONE.
