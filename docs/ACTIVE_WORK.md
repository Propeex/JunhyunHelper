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
PR: #285 (ready/non-draft)
replaced draft PR: #284 (closed unmerged because connector ready-for-review mutation is broken)
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
- Corrected the canonical v1.16 Farming Guide decision document so item lock now means automatic position lock; the earlier identity-only interpretation is explicitly historical and superseded.
- Release identity is aligned to v1.16.4: Desktop version, FIRST_RUN and `docs/RELEASE_NOTES_V1.16.4.md`; `PROJECT_STATE.product.desktopVersion` is 1.16.4 while public stable remains v1.16.3 until publication.
- Fully versioned candidate head `3fb8093dd090041d71f2ff790bffe54b3e21bf2e` passed:
  - CI `33622697275` — SUCCESS;
  - 623 passed / 0 failed / 0 skipped;
  - Windows x64 self-contained publish — SUCCESS;
  - actual published EXE Product UI / Map / Farming Guide decision smoke — SUCCESS;
  - graceful shutdown / package-checksum path — SUCCESS;
  - Actions artifact `JunhyunHelper-win-x64` id `9843624373`, bytes `242151605`, digest `sha256:4fd9a019ead0698c9556599438106e4e715e7b371ef04adbba198d1580c3f815`;
  - Shutdown Race `33622697267` — SUCCESS;
  - Documentation Consistency `33622697269` — SUCCESS.
- Draft PR #284 was closed unmerged after the connector's ready-for-review mutation failed on removed GitHub GraphQL field `fullDatabaseId`; replacement non-draft PR #285 uses the same validated branch.

## Current step

Obtain green checks on the final PR #285 head, merge it, then treat the resulting main merge commit as the v1.16.4 exact product source candidate.

## Remaining

- obtain fully green PR #285 checks and record exact final head/run evidence;
- merge PR #285;
- revalidate exact main with CI, Shutdown Race and Documentation Consistency;
- publish v1.16.4 from exact-main and verify public tag, release, ZIP, checksum and immutable source identity;
- update `PROJECT_STATE.json`, README, CURRENT_STATE, STATE, release notes and release-status evidence to the actual public release;
- close ACTIVE_WORK to NONE only after implementation, validation, merge, release and canonical documentation are complete.
