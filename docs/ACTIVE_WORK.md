# ACTIVE WORK

Status: **ACTIVE**

## Goal

Change the Mini Scanner `필요 아이템 개수` presentation so FIR and non-FIR/unrestricted current need remain visibly separated instead of being shown only as one summed total.

## Base / branch

- base main: `11f3119b73146236545a99551696efba6df9934a`
- current public stable: `v1.17.3`
- exact stable product source: `8ec677b1552f9deed55f98931c1df317e9bc4a4b`
- working branch: `maintenance/v1.17.4-mini-scanner-needed-count-2026-09-05`
- target release: `v1.17.4`
- Draft PR: #295

## Confirmed scope

The Mini Scanner row remains the existing `필요 아이템 개수` field.

Its value always displays:

`<FIR 필요량>(인레이드) + <그 외 현재 필요량>개`

Examples:

- FIR 3 / other 4 → `3(인레이드) + 4개`
- FIR 0 / other 4 → `0(인레이드) + 4개`
- FIR 4 / other 0 → `4(인레이드) + 0개`

Neither zero side is omitted.

This is a presentation change only. Requirement planning, FIR semantics, Scanner recognition, catalog data, persistence and Mini Scanner layout/order remain unchanged.

## Completed

- recovered v1.17.3 public stable state;
- confirmed the planner already exposes `RemainingTotal` and `RemainingFir`;
- Mini Scanner presentation now derives the other component as `RemainingTotal - RemainingFir`;
- added actual Mini Scanner smoke for 3+4, 0+4 and 4+0;
- added deterministic source contract;
- updated PRODUCT and DEVELOPER_REFERENCE traceability;
- functional candidate `e2477ffd8df3adbc1b9742c35a500944e0d1595f` passed:
  - CI `33938858432`
  - Shutdown Race `33938858490`
  - Documentation Consistency `33938858443`
  - 504 / 504 deterministic tests
  - Release build / win-x64 publish
  - actual Product UI / Map / Scanner smoke
  - package/checksum validation;
- staged Desktop/project/FIRST_RUN/release-notes identity for v1.17.4.

## Current step

Validate the exact v1.17.4 release-identity PR head through CI / Shutdown Race / Documentation Consistency and published EXE/package gates.

## Remaining

- exact release-identity PR validation;
- final PR diff/review check;
- mark PR ready and merge with exact head;
- exact-main CI / Shutdown / Docs;
- automatic stable v1.17.4 release;
- verify public tag/release/assets/digests;
- finalize PROJECT_STATE / README / CURRENT_STATE / STATE / release-status;
- close ACTIVE_WORK to NONE.
