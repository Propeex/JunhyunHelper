# ACTIVE WORK

Status: **ACTIVE**

## Current task

Clean v1.17.0 Farming Guide rulebook implementation restarted from stable v1.16.4.

## Base / working state

```text
base main: 379c6ab4ab02431c6bb74b537e899e94f45ee987
public stable: v1.16.4
working branch: feature/v1.17.0-farming-guide-restart-2026-09-03
previous PR #287: CLOSED / ABANDONED / MUST NOT BE RESUMED
new PR: not yet opened at this checkpoint
```

## Confirmed scope

Canonical product decision:

`docs/DECISION_V1.17.0_FARMING_GUIDE_RULEBOOK.md`

Confirmed rules:

1. During an active Farming Guide raid, every newly Scanner-identified incoming item is treated by Farming Guide as FIR.
2. Scanner does not classify FIR from an icon/checkmark/color/text and does not ask for separate FIR confirmation.
3. Farming objective is lexicographic:
   - maximize currently needed FIR Quest/Hideout units, capped by remaining quantity;
   - then maximize complete final retained average-Flea value.
4. The user's configured weight rule is the only user-configurable farming constraint; weight is not an item priority.
5. Item category gives no tactical privilege.
6. Every scan is a complete unlocked-item optimization problem, not a local insertion problem.
7. User-fixed items and cells are constraints; locks do not add value.
8. Existing verified Tarkov placement/container/equipment/stack mechanics remain system legality rules.
9. Internal optimization/performance authority may not be used to invent a new product decision criterion, automatic inference, observation authority, user interaction, cross-feature behavior, or visible failure semantic.

## Restart rule

Do not copy implementation from abandoned PR #287 as authority.

Code from the abandoned branch may only be consulted later as a non-authoritative implementation reference after the stable-main design is independently derived and only if it matches this confirmed rulebook. No Scanner FIR observation code is to be reused.

## Current step

Audit stable `main` Farming Guide code against the confirmed rulebook and identify the minimum coherent implementation changes. Start with the existing live raid decision path, priority policy, raid-session state, weight/quantity flow, locks, containers, and tests. Do not change user-visible product behavior outside the confirmed scope.

## Remaining

- inspect stable-main Farming Guide implementation and tests;
- derive a clean architecture/change set from the confirmed rules;
- open a new Draft PR after the initial authority/checkpoint commit;
- implement active-raid scanned-item FIR semantics without Scanner FIR classification;
- replace the old farming priority with needed-FIR quantity then complete retained Flea value;
- implement complete unlocked-item optimization while preserving verified system mechanics and explicit locks;
- preserve quantity/stack and weight behavior within the confirmed rules;
- add deterministic regression coverage;
- validate Windows Release build, tests, published EXE Product UI/runtime smoke, graceful shutdown and package integrity;
- synchronize authoritative project docs;
- only then merge/release.