# ACTIVE WORK

Status: **ACTIVE**

## Goal

Prepare v1.16.3 PATCH maintenance for two verified issues found immediately after v1.16.2:

1. Farming Guide can place a very high-value secure-container-eligible item such as LEDX into a free pocket without first considering whether lower-value removable contents should be moved out of the secure container.
2. The existing published MiniMap smoke can intermittently report that Player Marker Size changed an unrelated marker scale; determine whether this is a real runtime regression or a timing/zoom-sensitive smoke defect and correct the actual boundary without weakening the contract.

## Base

```text
public stable: v1.16.2
exact v1.16.2 product source/tag target: 81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
base main: 9ddcb6a621d2d91f3df0740a3fdfcd340322489d
branch: maintenance/v1.16.3-farming-guide-secure-priority-minimap-smoke-2026-09-02
target version: v1.16.3 PATCH
```

## Confirmed scope

- Preserve the deterministic Farming Guide rulebook; do not introduce weighted scoring.
- For an incoming item that is legal in the secure container, evaluate secure-container promotion before accepting an ordinary free backpack/rig/pocket slot.
- Prefer relocating removable secure-container contents into other legal free storage over discarding them.
- Never sacrifice/move protected locked content or violate reserved-cell contracts.
- Preserve the required food/drink survival reserve; food/drink above the required reserve may be relocated and may only be sacrificed under the existing retention/economic rules when relocation is impossible.
- Destructive promotion must compare the incoming total Flea value against the total sacrificed Flea value, including multi-victim fits.
- If secure promotion is illegal or not beneficial/safe, fall through to the existing ordinary free-storage and destructive-placement rules.
- Investigate the MiniMap marker-scale smoke failure observed on docs-only main validation and fix the real cause only with reproducible evidence.

## Completed

- Recovered v1.16.2 canonical state and exact product source from GitHub.
- Reproduced the LEDX decision from source: direct free-storage placement returns immediately; destructive/repacking consideration starts only after all ordinary free storage fails.
- Confirmed secure container is enumerated before pockets, so the LEDX result was not caused by secure-container ordering or an obvious legality exclusion; it had no direct free secure fit, then a free pocket fit terminated planning.
- Confirmed existing destructive-retention machinery can compare protected victim sets and total sacrificed Flea value once that phase is reached.
- Captured the final docs-only main smoke failure: `Player Marker Size changed unrelated MiniMap marker scale: 11.2515 -> 13.3801` in `RunJunhyunV114MiniMapSmokeAndWriteExtractEvidenceAsync`; build and all 619 deterministic tests passed in that run.

## Current step

Locate the exact current food/drink survival-reserve implementation and secure-container repacking boundaries, then implement the smallest deterministic secure-promotion phase with regression coverage. In parallel, inspect the MiniMap smoke measurement so the unrelated-marker assertion compares a stable invariant rather than transient zoom/layout state if that is the confirmed cause.

## Validation status

- v1.16.2 stable product: fully released and previously validated.
- Current v1.16.3 branch: implementation not yet complete.

## Remaining

- identify and preserve food/drink minimum-reserve logic;
- implement secure-container promotion/repacking before ordinary free storage;
- add LEDX/high-value, relocation, survival-reserve, locked/reserved, illegal-secure, and multi-victim economic regressions;
- diagnose and correct the MiniMap smoke boundary;
- run deterministic tests, Release build, Windows publish and published Product UI/Map/Farming Guide/graceful-shutdown smoke;
- align v1.16.3 release identity and release notes;
- open/validate/merge PR, revalidate exact-main, publish v1.16.3 and verify public assets;
- finalize canonical project documentation and close ACTIVE_WORK to NONE.
