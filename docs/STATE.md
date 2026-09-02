# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.16.2
exact product source/tag target:
81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
validated PR head:
119b47c406058ed422afdb17bace54db0f7e68f5
merge PR: #279
PR CI / Shutdown / Docs:
33601684251 / 33601684206 / 33601684210 — SUCCESS
exact-main CI / Shutdown / Docs:
33602013494 / 33602013351 / 33602013617 — SUCCESS
Release workflow: 33602299729 — SUCCESS
release id: 381041582
published UTC: 2026-09-02T07:11:21Z
619 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 540776589
bytes: 80,718,992
SHA-256: 8396a7810ac95a7118f88f68914038332e9876cdfd7b59247d32c4d44c22c7a7

SHA256SUMS.txt
asset id: 540776588
bytes: 86
asset SHA-256: 0fb2eb4894acc0e37b0f3c72633b1d5d37ef8a134ece1829158414c3652da805
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9835631036
bytes: 242,089,986
SHA-256: efcfb965a2a64cb7f7e3916ae3ed1c96d8eba5c0f77e1cd6090d41f6f9a5564c
```

Release workflow checked out the exact product source, re-downloaded the exact-main artifact with digest verification, verified ProductVersion/FIRST_RUN identity, and published only after the actual ZIP hash matched `SHA256SUMS.txt`. `v1.16.2` is not draft or prerelease.

## 2. v1.16.2 Farming Guide regressions

### Farmed value

Root cause: the Farming Guide summary was not wired to raid accounting and displayed `—` unconditionally.

Current value contract:

```text
Σ(net quantity acquired since raid start and still retained
  × known average Flea Market unit price)
```

Therefore:

- raid-start inventory does not count;
- stack quantity counts at its modeled quantity;
- acquired loot removed later no longer counts;
- losing baseline inventory does not create negative value;
- nested inventory is counted through the existing snapshot counter;
- unknown/non-positive Flea price is not guessed.

The calculation uses the same raid baseline/current snapshot truth already used for Farming Guide state, rather than maintaining a separate historical scan total.

### Reserved empty-cell visibility

Root cause: reserved-cell overlays were rendered above real item cards. Manual placement state was correct, but the item was visually covered.

Current contract:

- reserved cells remain protected from automatic placement;
- direct user editing remains authoritative;
- reservation markers render behind item cards;
- a directly placed item remains visible and interactive;
- item-lock accent-border behavior is unchanged.

Published WPF smoke now asserts the real reservation marker Z-index is below the real item card.

## 3. Deterministic Farming Guide rulebook

The planner remains deterministic:

```text
hard constraints
→ priority / importance
→ applicable situation response
→ legal proposed state
→ explicit user accept
```

No weighted score is used.

Priority/economic rules retained:

- special needed priority only for actual Found-in-Raid requirements;
- non-FIR needed items are ordinary economic loot;
- ordinary economics use average Flea Market value;
- destructive replacement compares incoming total value against the complete sacrificed item set;
- when economic value ties, known weight may prefer the lighter state, followed by ordinary footprint.

Equipment representative rules retained:

- body armor / helmet: armor class;
- headset: hearing distance;
- ordinary rig / backpack / secure container: storage capacity;
- armored rig: armor class first, then storage capacity on equal class;
- weapon / pistol: no automatic superiority replacement.

Market value does not automatically upgrade worn equipment. Hidden instance facts such as durability, remaining uses and live firearm assembly are not inferred.

## 4. Protected state / storage

Protected state consists of locked items and reserved cells.

For storage-bearing equipment replacement:

- locked item instances must survive;
- legal repacking is allowed;
- reserved capacity is recreated by equivalent connected shape/capacity rather than fixed coordinates;
- replacement is forbidden if equivalent protected state cannot be represented legally.

Nested storage remains source-backed:

- real `StorageGrids`, dimensions and allow/exclude filters are authoritative;
- specialized containers use source filters instead of item-name allowlists;
- recursive nesting uses `ParentInstanceId`;
- cycles, overlap, missing parents and illegal filter placements fail closed.

Equipment remains an opaque complete-item model where live attachments/plates cannot be proved from Scanner identity. The program does not invent hidden attachment/plate state or weight.

## 5. Quantity / weight

Farming Guide state schema remains v3.

- quantity-dependent Scanner items request quantity before recommendation;
- stored quantity persists and is displayed on cards;
- double-click edits quantity;
- quantity affects needed count, total value and modeled weight;
- stale pending quantity is canceled by a new scan.

Strength level remains profile-specific. Proposed states over the modeled carry limit are blocked. If current modeled state is already above the limit, recommendations may only maintain or reduce weight until it returns under the limit.

## 6. Persistence / bridge lifecycle

v1.16.1 persistence hardening remains active: partial/null semantic state is normalized, salvageable data is kept, unusable state fails closed, and atomic write/backup behavior remains.

Scanner owns confirmed scan facts; Farming Guide owns inventory decisions. The desktop bridge continues to marshal worker events to WPF, deduplicate scan identity, route quantity input, route explicit accept, and clear stale Mini Scanner state.

## 7. v1.16.2 audit result and validation

The maintenance pass rechecked raid state transitions, FIR/economic priority, victim-set comparison, representative equipment rules, protected-state migration, nested repacking, quantity, weight, persistence, Scanner bridge state and rendered WPF behavior.

No additional reproducible rulebook defect was found that justified behavior changes.

Added regression coverage includes:

- baseline exclusion;
- stack quantity value;
- non-negative lost-baseline behavior;
- acquired-then-discarded removal;
- nested inventory counting;
- unknown/non-positive Flea price;
- value-summary wiring;
- reservation-overlay layering;
- published WPF farmed-value rendering;
- published WPF reservation/item layering.

Exact-main CI passed Release build, **619/619 tests**, Windows x64 self-contained publish, actual published EXE Product UI / Map / Farming Guide smoke, graceful shutdown, portable-root check, package creation and checksum verification.

Release ProductVersion was:

```text
1.16.2+81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
```

## 8. Schema / canonical references

```text
Desktop: 1.16.2
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

Canonical release evidence:

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.16.2-status.json`
- `docs/RELEASE_NOTES_V1.16.2.md`
- `docs/CURRENT_STATE.md`

Automated release validation is complete. Actual Tarkov play validation on the user's own environment remains a separate `PENDING` evidence field and does not make v1.16.2 development or release incomplete.
