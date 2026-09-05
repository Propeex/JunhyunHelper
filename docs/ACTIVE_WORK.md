# ACTIVE WORK

Status: **ACTIVE**

## Goal

Change the Mini Scanner `필요 아이템 개수` presentation so found-in-raid and non-FIR requirements remain visibly separated instead of being shown only as one summed total.

## Base / branch

- base main: `11f3119b73146236545a99551696efba6df9934a`
- current public stable: `v1.17.3`
- exact stable product source: `8ec677b1552f9deed55f98931c1df317e9bc4a4b`
- working branch: `maintenance/v1.17.4-mini-scanner-needed-count-2026-09-05`
- target release: `v1.17.4`

## Confirmed scope

The Mini Scanner row label remains `필요 아이템 개수`.

Its value must always display:

`<FIR 필요량>(인레이드) + <non-FIR 필요량>개`

Examples:

- FIR 3 / non-FIR 4 → `3(인레이드) + 4개`
- FIR 0 / non-FIR 4 → `0(인레이드) + 4개`
- FIR 4 / non-FIR 0 → `4(인레이드) + 0개`

Neither zero side is omitted.

This is a presentation change only. Do not alter requirement planning, FIR semantics, Scanner recognition, catalog data, persistence, or Mini Scanner layout beyond what is needed to render the new value.

## Completed

- recovered v1.17.3 public-stable repository state;
- captured and confirmed the Mini Scanner display contract;
- created the v1.17.4 maintenance branch and Draft PR #295;
- confirmed existing planner authority already exposes `RemainingTotal` and `RemainingFir`;
- kept requirement calculation unchanged and changed only Mini Scanner presentation;
- Mini Scanner now renders `FIR(인레이드) + unrestricted개` and preserves zero-valued sides;
- actual Mini Scanner product smoke covers 3+4, 0+4 and 4+0 cases;
- added deterministic source contract for the display boundary;
- updated PRODUCT and DEVELOPER_REFERENCE traceability.

## Current step

Validate the functional candidate through CI / Shutdown Race / Documentation Consistency and published EXE smoke before staging the v1.17.4 release identity.

## Remaining

- implement formatting at the presentation boundary;
- add/update regression tests;
- stage v1.17.4 identity and release notes;
- run CI / Shutdown Race / Documentation Consistency / published EXE smoke / package verification;
- merge, exact-main verify and publish stable v1.17.4;
- finalize project memory and close ACTIVE_WORK.
