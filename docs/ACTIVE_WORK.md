# ACTIVE WORK

Status: **ACTIVE**

## Goal

Prepare v1.16.3 PATCH maintenance as a proactive Farming Guide decision-safety audit before further user real-play validation.

The original trigger remains two verified issues found immediately after v1.16.2:

1. Farming Guide can place a very high-value secure-container-eligible item such as LEDX into a free pocket without first considering whether lower-value removable contents should be moved out of the secure container.
2. The existing published MiniMap smoke can intermittently report that Player Marker Size changed an unrelated marker scale; determine whether this is a real runtime regression or a timing/zoom-sensitive smoke defect and correct the actual boundary without weakening the contract.

The maintenance pass is intentionally expanded to inspect all destructive/repacking paths for latent real-play misjudgments instead of waiting for the user to encounter them.

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
- Preserve exact locked item instances and locked equipment/carrier roots. A locked rig/backpack/secure container must still expose its legal internal storage; carrier lock is not a blanket storage lock.
- Preserve reserved-cell contracts.
- Preserve the modeled minimum food/drink survival reserve.
- Preserve loose ammunition compatible with the currently carried weapon set; use source-backed weapon/ammo compatibility facts rather than localized-name inference.
- Destructive decisions must use actual stored stack quantity and compare incoming total Flea value against the complete actual sacrificed set.
- Replace the historical prefix-only multi-victim search with a bounded deterministic subset search so an irrelevant cheap victim cannot force unnecessary loss or mask a valid geometric solution.
- Use the active profile's real pocket geometry, including expanded pockets, in all transition/repacking paths.
- Add a final fail-closed cross-contract validation boundary for automatic advice.
- If secure promotion is illegal or not beneficial/safe, fall through to the existing ordinary free-storage and destructive-placement rules.
- Investigate the MiniMap marker-scale smoke failure and fix the real cause only with reproducible evidence.

## Completed

- Recovered v1.16.2 canonical state and exact product source from GitHub.
- Reproduced the LEDX decision from source: direct free-storage placement returned before secure-container repacking could be considered.
- Added a v1.16.3 non-destructive secure-protection pass before ordinary free storage. It restricts the incoming item to source-legal secure surfaces and only demotes strictly lower-priority unlocked leaf contents when needed.
- Confirmed Tarkov source data exposes food/drink `energy` / `hydration`, weapon `caliber` / `allowedAmmo`, and ammo `caliber` facts. Added these source-backed facts to Farming Guide content metadata and bumped current content snapshot schema to v12 while retaining older readable snapshots.
- Added source-backed tactical resource classification for food, drink and currently compatible ammunition.
- Added a final fail-closed raid recommendation safety boundary that re-checks explicit locks, minimum food/drink retention, compatible loose ammunition retention and actual quantity-aware sacrificed Flea value before a destructive recommendation can be accepted.
- Confirmed a latent carrier-lock regression in the historical v1.15.5 transition path: locking an equipped carrier can incorrectly make its internal storage unavailable to repacking.
- Confirmed a latent quantity regression: historical destructive candidate ranking can evaluate a stored ammo/currency stack through one-unit metrics instead of its actual stored quantity.
- Confirmed a latent profile-geometry regression: the historical transition path hard-codes standard pockets instead of the page's resolved expanded-pocket geometry.
- Confirmed a latent search-completeness regression: bounded multi-victim eviction tests only prefixes of a globally sorted victim list rather than deterministic subsets, which can discard an unnecessary cheap item or miss a valid lower-loss geometric solution.
- Captured the MiniMap smoke failure and confirmed Player Marker Size product code updates only the player marker. The smoke assertion could observe a transient donor marker recreation; the branch smoke now validates the independent setting immediately and waits for standard-marker rendering to converge instead of comparing a transient visual instance.
- Added deterministic tests for tactical resource classification and source import of the new tactical facts.

## Current step

Implement the v1.16.3 corrective destructive/repacking layer so every victim candidate is quantity-aware, tactical-reserve-aware, uses the active pocket geometry, does not treat a carrier lock as an internal-storage lock, and searches bounded victim subsets rather than prefixes. Then add end-to-end Farming Guide regression smoke scenarios for the newly identified real-play cases.

## Validation status

- v1.16.2 stable product: fully released and previously validated.
- Current v1.16.3 branch: implementation in progress; not release-ready.

## Remaining

- implement quantity-aware/tactical-safe bounded subset eviction and corrected transition storage surfaces;
- correct carrier-lock internal-storage semantics and expanded-pocket geometry in all applicable transition paths;
- audit carrier-role migration/equipment replacement preservation paths for the same contracts;
- add LEDX/high-value secure promotion, locked-carrier storage, expanded-pocket, stack-value, survival-reserve, current-weapon-ammo, reserved-cell, illegal-secure and multi-victim subset regressions;
- run deterministic tests and correct any compile/regression failures;
- run Release build, Windows x64 self-contained publish and actual published Product UI/Map/Farming Guide/graceful-shutdown smoke;
- run Shutdown Race and Documentation Consistency validation;
- align v1.16.3 version/release identity, release notes and canonical Farming Guide architecture documentation;
- open/validate/merge PR, revalidate exact-main, publish v1.16.3 and verify public tag/release/assets;
- finalize canonical project documentation and close ACTIVE_WORK to NONE.
