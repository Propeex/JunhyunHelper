# ACTIVE WORK

Status: **ACTIVE**

## Goal

Prepare v1.16.3 PATCH maintenance as a proactive Farming Guide decision-safety audit before further user real-play validation.

The original trigger remains two verified issues found immediately after v1.16.2:

1. Farming Guide can place a very high-value secure-container-eligible item such as LEDX into a free pocket without first considering whether lower-value removable contents should be moved out of the secure container.
2. The existing published MiniMap smoke can intermittently report that Player Marker Size changed an unrelated marker scale; determine whether this is a real runtime regression or a timing/zoom-sensitive smoke defect and correct the actual boundary without weakening the contract.

The maintenance pass was intentionally expanded to inspect all destructive/repacking paths for latent real-play misjudgments instead of waiting for the user to encounter them.

## Base

```text
public stable: v1.16.2
exact v1.16.2 product source/tag target: 81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
base main: 9ddcb6a621d2d91f3df0740a3fdfcd340322489d
branch: maintenance/v1.16.3-farming-guide-secure-priority-minimap-smoke-2026-09-02
target version: v1.16.3 PATCH
PR: #281
```

## Confirmed scope

- Preserve the deterministic Farming Guide rulebook; do not introduce weighted scoring.
- For an incoming item that is legal in the secure container, evaluate secure-container promotion before accepting an ordinary free backpack/rig/pocket slot.
- Prefer relocating removable secure-container contents into other legal free storage over discarding them.
- Preserve exact locked item instances and locked equipment/carrier roots. A locked rig/backpack/secure container must still expose its legal internal storage; carrier lock is not a blanket storage lock.
- Preserve reserved-cell contracts.
- Preserve the modeled minimum food/drink survival reserve.
- Preserve loose ammunition compatible with the currently carried weapon set; use source-backed weapon/ammo compatibility facts rather than localized-name inference.
- Destructive decisions must use actual stored stack quantity and compare incoming total Flea value against the complete actual sacrificed set.
- Replace the historical prefix-only multi-victim search with a bounded deterministic subset search so an irrelevant cheap victim cannot force unnecessary loss or mask a valid geometric solution.
- Use the active profile's real pocket geometry, including expanded pockets, in all transition/repacking paths.
- Add a final fail-closed cross-contract validation boundary for automatic advice.
- Apply special FIR priority to existing loot only when that loot is actually still needed Found in Raid; ordinary non-FIR need remains economic loot.
- If secure promotion is illegal or not beneficial/safe, fall through to the existing ordinary free-storage and destructive-placement rules.
- Keep the actual MiniMap Player Marker Size product behavior unchanged and stabilize only the proven asynchronous smoke boundary.

## Completed

- Recovered v1.16.2 canonical state and exact product source from GitHub.
- Reproduced the LEDX decision from source: direct free-storage placement returned before secure-container repacking could be considered.
- Added a v1.16.3 non-destructive secure-protection pass before ordinary free storage. It restricts the incoming item to source-legal secure surfaces and only demotes strictly lower-priority safe leaf contents when needed.
- Confirmed Tarkov source data exposes food/drink `energy` / `hydration`, weapon `caliber` / `allowedAmmo`, and ammo `caliber` facts. Added these source-backed facts to Farming Guide content metadata and bumped current content snapshot schema to v12 while retaining v3-v11 readability.
- Added source-backed tactical resource classification for food, drink and currently compatible ammunition.
- Added a final fail-closed raid recommendation safety boundary that re-checks explicit locks, minimum food/drink retention, compatible loose ammunition retention and actual quantity-aware sacrificed Flea value before a destructive recommendation can be accepted.
- Corrected carrier-lock semantics so locking an equipped carrier protects the carrier root from replacement without disabling its internal storage.
- Corrected exact-item lock semantics so the same locked instance may move during non-destructive repacking while remaining protected from deletion/replacement.
- Corrected destructive candidate metrics so stored ammo/currency stacks use their actual quantity rather than one-unit value/weight.
- Corrected the transition/repacking path to use the active profile's resolved pocket grids, including expanded pockets.
- Replaced the historical prefix-only multi-victim eviction path with a bounded deterministic best-first subset search.
- Audited carrier-role migration/equipment replacement preservation. Existing v1.16 migration already preserves locked instance identity, root reserved-cell shape/capacity, direct contents and nested child identity and fails closed when the replacement carrier cannot preserve them.
- Corrected the existing-loot FIR priority boundary so v1.16.3 destructive/final-safety decisions use `CurrentNeededFir`, not general `CurrentNeeded`.
- Captured the MiniMap smoke failure and confirmed Player Marker Size product code updates only the player marker. The smoke assertion could observe a transient donor marker recreation; validation now confirms the independent setting and waits for standard-marker rendering to converge.
- Added deterministic tests for tactical resource classification, source import and content-schema-v12 round-trip/refresh behavior.
- Fixed two stale schema-v11 test assertions found by the first v12 CI candidate. The corrected suite is 623 tests.
- Added published-EXE Farming Guide decision smoke for nine synthetic raid cases: secure promotion before free pocket, locked-carrier storage, expanded pockets, stack total value, non-prefix geometric victim choice, final food/drink reserve, current-weapon ammo reserve, locked-instance movement and FIR-only need semantics.
- Pre-release-identity product logic head `1e20dd97338ad56048071766a85539c29fe8f4ba` passed:
  - CI `33616912770` — SUCCESS;
  - 623 passed / 0 failed / 0 skipped;
  - Windows x64 self-contained publish — SUCCESS;
  - actual published EXE Product UI / full Map/Factory/MiniMap / Farming Guide decision smoke — SUCCESS;
  - graceful shutdown and clean portable root — SUCCESS;
  - release package/checksum verification — SUCCESS;
  - Shutdown Race `33616912788` — SUCCESS;
  - Documentation Consistency `33616912777` — SUCCESS.
- Began release identity alignment: Desktop version is now 1.16.3, FIRST_RUN_KO describes v1.16.3, `docs/RELEASE_NOTES_V1.16.3.md` exists, and canonical content schema is recorded as v12. Public stable remains v1.16.2 until actual release.

## Current step

Validate the fully versioned v1.16.3 PR candidate, including Release build, all deterministic tests, Windows self-contained publish, actual published EXE Product UI/Map/Farming Guide decision smoke, graceful shutdown, package/checksum, Shutdown Race and Documentation Consistency. Correct any release-identity/document mismatch rather than bypassing validation.

## Validation status

- v1.16.2 stable product: fully released and previously validated.
- v1.16.3 pre-identity product logic + integrated runtime decision smoke: fully green at `1e20dd97338ad56048071766a85539c29fe8f4ba`.
- v1.16.3 versioned release candidate: validation pending on the latest PR #281 head.
- User real-PC/Tarkov validation remains separately PENDING and is not a release blocker unless it produces a concrete regression report.

## Remaining

- obtain a fully green versioned PR #281 candidate and record its exact head/run evidence;
- mark PR ready and merge after required checks are green;
- revalidate the exact main product source with CI, Shutdown Race and Documentation Consistency;
- publish v1.16.3 from exact-main and verify public tag, release, ZIP, checksum and immutable source identity;
- update `PROJECT_STATE.json`, README, CURRENT_STATE, STATE, release notes and release-status evidence to the actual public release;
- close ACTIVE_WORK to NONE only after implementation, validation, merge, release and canonical documentation are complete.
