# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.16.4
exact product source/tag target:
5886d8f97abd060d398d4c50d3dd3b720e4ace09
validated PR head:
d55e138c962e87dc8691f82c81d36a516db52941
merge PR: #285
PR CI / Shutdown / Docs:
33623459284 / 33623459290 / 33623459267 — SUCCESS
exact-main CI / Shutdown / Docs:
33623824030 / 33623824052 / 33623824027 — SUCCESS
Release workflow: 33624248788 — SUCCESS
release id: 381192920
published UTC: 2026-09-02T11:22:47Z
623 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 541072599
bytes: 80,738,891
SHA-256: 2ceddbd3cc805bc8de2cdb5eddcef72c2001a6724a43ec7fdd993781af649fb4

SHA256SUMS.txt
asset id: 541072598
bytes: 86
asset SHA-256: 2a07506d6c84048940a35beb7aa637de9e27dd51bea25600a9b62a5a93f6017f
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9844117414
bytes: 242,151,516
SHA-256: f2aea11845611012d26bc135f8d6386200ea5007382d441b652ef6d1b3f86477
```

Release workflow `33624248788` checked out exact product source `5886d8f97abd060d398d4c50d3dd3b720e4ace09`, downloaded exact-main artifact `9844117414` with expected digest verification, verified ProductVersion/FIRST_RUN identity, independently matched the actual release ZIP hash to `SHA256SUMS.txt`, and then published stable `v1.16.4`. The release is not draft or prerelease.

Draft PR #284 was closed unmerged only because the connected GitHub ready-for-review GraphQL path referenced removed `Repository.fullDatabaseId`. Non-draft PR #285 reused the same branch, received complete independent validation, and is the authoritative merge PR.

## 2. v1.16.4 exact item lock contract

The v1.16 deterministic decision flow remains:

```text
hard constraints
→ priority / importance
→ applicable situation response
→ legal proposed state
→ final fail-closed safety
→ explicit user accept
```

No weighted score was introduced.

### Root cause of the v1.16.3 regression

v1.16.3 interpreted an exact stored-item lock as identity preservation: a recommendation could move the item as long as the same `InstanceId` survived. That interpretation was encoded in secure-container promotion, general repacking/carrier migration and the then-current published smoke.

Real play exposed the mismatch: while evaluating a scanned Wires item, Farming Guide instructed moving a locked Grizzly emergency kit. The intended product meaning is stronger—an explicitly locked stored item is position-locked for automatic advice.

### Current authoritative behavior

For every exact locked stored item, automatic advice must preserve:

- exact `InstanceId` and item identity;
- storage kind;
- grid index;
- X/Y coordinates;
- rotation;
- `ParentInstanceId`;
- quantity;
- placement of stored ancestors whose movement would indirectly move it;
- identity of the root Rig / Backpack / SecureContainer that contains it.

Therefore automatic advice cannot discard, replace, relocate, rotate, re-parent or indirectly move the locked instance.

Manual user editing remains authoritative and is not blocked by this recommendation constraint.

### Lock-aware planning and repacking

v1.16.4 applies the position contract throughout the automatic planning stack rather than relying on presentation-only filtering.

- Secure-container promotion is rejected when promotion requires relocating a locked existing item.
- General repacking treats an exact locked item as a hard geometry obstacle.
- A stored ancestor containing a locked descendant is also immovable when moving that ancestor would move the descendant.
- Carrier upgrade/migration is rejected when it cannot preserve the locked descendant's real physical placement and containing root carrier identity.
- The final safety boundary independently checks exact placement again and fails closed if an earlier planner produces an invalid proposal.

A lock on an equipped Rig / Backpack / SecureContainer still protects that carrier root from automatic replacement without disabling its legal internal storage. Independently unlocked contents and legal free cells inside the locked carrier remain usable. Reserved cells retain their separate automatic-placement prohibition.

## 3. Retained v1.16.3 secure-container and destructive safety

v1.16.4 preserves all compatible v1.16.3 decision-safety improvements.

### Secure-container promotion

Secure-container-eligible high-value incoming loot is considered for non-destructive secure promotion before ordinary free storage. Lower-priority secure contents may be demoted only when they can be preserved in other legal storage and no lock/reservation/tactical constraint is violated. v1.16.4 adds exact-position lock preservation to this gate.

### Quantity-aware destructive economics

Stored stack `Quantity` is reflected in destructive value and weight. Multi-victim eviction performs a bounded deterministic subset search rather than considering only sorted prefixes. Incoming total average-Flea value must remain strictly greater than the complete actual sacrificed set.

### Tactical resource protection

Content schema v12 retains source-backed facts required to identify raid survival resources without localized-name inference:

- `Energy`
- `Hydration`
- `AmmoCaliber`
- `WeaponCaliber`
- `AllowedAmmoItemIds`

Automatic destructive advice preserves the minimum modeled food provider, minimum modeled drink provider, and loose ammunition compatible with currently carried PrimaryWeapon1 / PrimaryWeapon2 / Holster weapons.

### FIR priority consistency

Special needed priority applies only to actual Found-in-Raid requirements (`CurrentNeededFir`). General non-FIR need remains ordinary economic loot.

### Expanded pockets / reservations

Transition and repacking paths use the active profile's resolved pocket geometry. Reserved cells continue to block automatic placement. They do not become item locks and their behavior is unchanged by v1.16.4.

## 4. Final fail-closed recommendation boundary

Before recommendation exposure, cross-cutting safety validation covers:

- equipment/carrier locks;
- exact stored-item position locks and indirect ancestor/root-carrier movement;
- reserved/protected state;
- minimum food/drink retention;
- current-weapon compatible loose-ammunition retention;
- complete removed-victim interpretation;
- quantity-aware sacrificed average-Flea value;
- modeled carry-weight constraint.

If legality cannot be proven, the planner retains the current state rather than presenting unsafe advice.

## 5. Scanner / persistence / prior contracts

Scanner remains the owner of confirmed scan facts; Farming Guide owns modeled inventory decisions. Quantity prompt, explicit accept transaction, Mini Scanner lifecycle, nested source-backed storage, state schema v3, persistence normalization/recovery, and Strength-based carry limits remain unchanged except where v1.16.4 strengthens lock safety.

The v1.16.2 farmed-value and reserved-overlay fixes remain active. The v1.16.3 MiniMap Player Marker Size smoke stabilization remains test-only stabilization; no user-facing marker-size behavior changed.

## 6. v1.16.4 regression coverage

The published-EXE Farming Guide decision smoke now executes the user-reported lock class through the actual WPF/page/planner boundary. It verifies at least:

1. secure-container recommendation does not move an explicitly locked existing item when ordinary legal storage exists;
2. general repacking does not move a locked blocker;
3. final safety rejects a proposal that changes locked placement;
4. root carrier replacement does not indirectly move a locked descendant;
5. locked carrier internal legal storage remains usable;
6. prior secure promotion, expanded pockets, stack total value, bounded victim selection, food/drink reserve, current-weapon ammunition reserve and FIR-only priority continue to work.

PR #285 and exact-main both passed Release build, **623/623 deterministic tests**, Windows x64 self-contained publish, actual published EXE Product UI / Map / Farming Guide decision smoke, graceful shutdown, clean portable-root checks, package/checksum verification, dedicated Shutdown Race and Documentation Consistency workflows.

## 7. Schema / canonical references

```text
Desktop: 1.16.4
Content write/read: v12 / v3-v12
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

Canonical release evidence:

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.16.4-status.json`
- `docs/RELEASE_NOTES_V1.16.4.md`
- `docs/CURRENT_STATE.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`

Automated implementation, merge, exact-main and public release validation are complete. Actual Tarkov play validation on the user's own environment remains a separate `PENDING` evidence field and does not make v1.16.4 development or release incomplete.
