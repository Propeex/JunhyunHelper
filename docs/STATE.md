# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.16.3
exact product source/tag target:
89fae2e07b721b1dfd4922642412fcebf01b275d
validated PR head:
1c223a696e896e1af2ec1c35ec727eb3c70aa44d
merge PR: #282
PR CI / Shutdown / Docs:
33618363995 / 33618364028 / 33618363996 — SUCCESS
exact-main CI / Shutdown / Docs:
33618724736 / 33618724737 / 33618725069 — SUCCESS
Release workflow: 33619033186 — SUCCESS
release id: 381157194
published UTC: 2026-09-02T10:21:57Z
623 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 541000063
bytes: 80,735,580
SHA-256: eabc7c162ea583f138fbeb3bd2567145bc28c6f305bde20e049175c56580f657

SHA256SUMS.txt
asset id: 541000067
bytes: 86
asset SHA-256: c25ad9cb116c53143f1aece1a5035313d0a1176acff5b71c6366ea297d69dae5
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9842117423
bytes: 242,138,760
SHA-256: cda8d29a6dfa3499df8ba23522ed7faeb11475e726c6b8ed66566bb29eda55eb
```

Release workflow checked out exact product source `89fae2e07b721b1dfd4922642412fcebf01b275d`, downloaded exact-main artifact `9842117423` with expected digest verification, verified ProductVersion/FIRST_RUN identity, and published only after the actual ZIP hash exactly matched `SHA256SUMS.txt`. `v1.16.3` is not draft or prerelease.

The draft validation PR #281 was closed unmerged only because the connected GitHub ready-for-review GraphQL path referenced the removed `Repository.fullDatabaseId` field. Non-draft PR #282 reused the exact same validated branch/head, passed its own complete validation, and is the authoritative merge PR.

## 2. v1.16.3 Farming Guide decision-safety maintenance

The v1.16 deterministic contract remains:

```text
hard constraints
→ priority / importance
→ applicable situation response
→ legal proposed state
→ explicit user accept
```

No weighted score was introduced.

### Secure-container promotion before ordinary free storage

The historical page path could accept the first ordinary free pocket/rig/backpack placement before evaluating whether a secure-container-eligible high-value item should replace lower-priority removable secure contents.

v1.16.3 adds a non-destructive secure-protection pass before ordinary free storage:

- incoming must be legal for a secure surface according to source-backed storage filters;
- only strictly lower-priority safe leaf contents may be demoted;
- demoted contents must be preserved in other legal free storage rather than discarded;
- locked contents, equal/higher-priority contents and storage-bearing parents are not casually displaced;
- if promotion cannot be proven legal and beneficial, the planner falls through to the ordinary placement/destructive rules.

### Quantity-aware destructive economics

Stored stacks now use actual `Quantity` in destructive value and weight metrics. A 60-round ammunition stack is not evaluated as one round.

Multi-victim eviction no longer checks only sorted prefixes. A bounded deterministic subset search evaluates actual candidate combinations so an irrelevant cheap victim cannot force unnecessary loss or mask a valid lower-loss geometric solution.

The existing economic contract remains: incoming total average-Flea value must be strictly greater than the complete actual sacrificed set.

### Lock semantics and carrier storage

A lock protects an item instance/root from automatic deletion or replacement; it does not convert the carrier's internal storage into unusable space.

- locked rig/backpack/secure-container roots remain equipped/protected;
- their legal internal storage remains available to packing/repacking;
- a locked stored item may move if the same exact `InstanceId` survives the proposed state;
- destructive loss or substitution of a locked instance remains forbidden.

Reserved cells continue to block automatic placement. Carrier replacement continues to fail closed unless locked instances and equivalent reserved connected shape/capacity can be preserved legally.

### Expanded pocket geometry

All applicable v1.16.3 transition/repacking paths use the page's active resolved pocket grids. Profiles with expanded pockets therefore use their real modeled geometry instead of historical standard-pocket assumptions.

## 3. Tactical resource protection

The Farming Guide now retains source-backed tactical facts needed to distinguish raid survival resources without localized-name inference.

Content facts added in schema v12:

- `Energy`
- `Hydration`
- `AmmoCaliber`
- `WeaponCaliber`
- `AllowedAmmoItemIds`

Automatic destructive recommendations preserve:

- the minimum modeled food provider;
- the minimum modeled drink provider;
- loose ammunition compatible with the currently carried PrimaryWeapon1, PrimaryWeapon2 or Holster weapon set.

Compatibility uses source caliber/allowed-ammo relationships rather than text matching.

## 4. FIR priority consistency

Special needed priority applies only to actual Found-in-Raid requirements.

v1.16.3 applies this consistently to existing loot as well as incoming loot by using `CurrentNeededFir` for the special protected boundary. General non-FIR need remains ordinary economic loot and does not gain an unintended absolute priority.

## 5. Final fail-closed recommendation boundary

Before a destructive recommendation is exposed, the proposed state is revalidated against cross-cutting contracts:

- explicit equipment/carrier/item locks;
- protected exact item identity;
- minimum food/drink retention;
- current-weapon compatible loose ammunition retention;
- interpretable complete removed-victim set;
- actual quantity-aware total sacrificed average-Flea value;
- modeled carry-weight limit.

If the planner cannot prove the destructive transition is safe and legal, it fails closed and does not expose the destructive advice.

## 6. MiniMap smoke stabilization

The intermittent Player Marker Size smoke failure was investigated separately from the Farming Guide changes.

Product code was confirmed to update only the player marker. The failure occurred when the test compared a transient standard-marker visual instance during asynchronous donor-marker recreation.

The smoke now verifies the independent Player Marker Size setting and waits with a bounded convergence condition before comparing standard-marker rendering. No user-facing MiniMap marker-size behavior was changed.

## 7. Persistence / Scanner bridge / prior contracts

Existing v1.16.1 persistence normalization and recovery remain active. Scanner remains the owner of confirmed scan facts; Farming Guide owns modeled inventory decisions. Quantity prompt, explicit accept transaction, Mini Scanner lifecycle, nested source-backed storage and Strength-based carry limits remain unchanged except where the v1.16.3 safety rules above explicitly strengthen validation.

The v1.16.2 farmed-value and reserved-overlay fixes remain part of the current product contract.

## 8. v1.16.3 regression coverage

Deterministic/source tests cover the new tactical facts and schema-v12 import/round-trip/refresh path. The first schema-v12 candidate exposed two stale test assertions still expecting v11; those assertions were corrected rather than weakening current-schema validation.

A dedicated published-EXE Farming Guide decision smoke executes synthetic raid states through the actual WPF/page/planner boundary for:

1. secure promotion before free pocket;
2. locked carrier internal storage;
3. expanded pocket geometry;
4. stored stack total value;
5. non-prefix geometric victim selection;
6. final food/drink reserve protection;
7. current-weapon compatible ammo reserve;
8. safe movement of a locked exact item instance;
9. general non-FIR need versus true FIR need semantics.

Exact-main CI passed **623/623 tests**, Release build, Windows x64 self-contained publish, actual published EXE Product UI / Map / Farming Guide decision smoke, graceful shutdown, clean portable-root checks, package creation and checksum verification.

## 9. Schema / canonical references

```text
Desktop: 1.16.3
Content write/read: v12 / v3-v12
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

Canonical release evidence:

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.16.3-status.json`
- `docs/RELEASE_NOTES_V1.16.3.md`
- `docs/CURRENT_STATE.md`

Automated implementation, merge, exact-main and public release validation are complete. Actual Tarkov play validation on the user's own environment remains a separate `PENDING` evidence field and does not make v1.16.3 development or release incomplete.
