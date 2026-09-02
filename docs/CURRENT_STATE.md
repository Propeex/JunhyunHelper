# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.16.2
exact product source/tag target:
81ce1dc93fefd633502e62cb5fdde54c2f61ce8c
validated PR head: 119b47c406058ed422afdb17bace54db0f7e68f5
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

Public package:

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

`v1.16.2` is `draft=false`, `prerelease=false`, and targets the exact product source above. Release workflow `33602299729` checked out that exact commit, re-downloaded the exact-main artifact with expected digest verification, verified published EXE/FIRST_RUN identity, and confirmed the manifest SHA-256 exactly matched the actual release ZIP before publication.

## v1.16.2 Farming Guide fixes

### Farmed value

The Farming Guide value summary was previously disconnected from raid state and hard-coded to `—`.

It now displays the value of **loot net-acquired since raid start and still present in the current modeled snapshot**, using average Flea Market price.

- raid-start inventory is excluded;
- stack quantity is counted;
- acquired-then-discarded loot is removed from the total;
- losing baseline items never makes the value negative;
- nested inventory is counted through the snapshot inventory counter;
- unknown/non-positive Flea prices are not guessed.

### Reserved-cell visibility

Reserved cells remain hard constraints for automatic placement, but direct user placement remains authoritative. The reservation overlay now renders behind item cards, so a manually placed item is visible instead of being covered by the reserved-cell marker.

### Farming Guide audit result

The v1.16.2 maintenance pass rechecked:

- raid baseline/current state and acceptance transaction;
- FIR priority and average-Flea economics;
- destructive victim-set comparison;
- equipment/carrier representative superiority rules;
- locked items and reserved-cell migration;
- nested storage, specialized filters and repacking;
- quantity, weight and Strength boundary;
- persistence normalization/recovery;
- Scanner/Mini Scanner bridge lifecycle;
- rendered WPF / published EXE Farming Guide runtime behavior.

No additional reproducible rulebook defect was found that justified changing the established v1.16 contract.

## Farming Guide current contract

Farming Guide remains deterministic rather than weighted-score based:

```text
hard constraints
→ importance / priority
→ applicable situation response
→ one legal proposed state
→ explicit user accept
```

Key rules:

- special needed priority only for actual Found-in-Raid requirements;
- ordinary economics use average Flea Market value;
- destructive replacement compares incoming total value with the complete sacrificed set;
- armor/helmet use armor class, headset hearing distance, ordinary carrier storage capacity, armored rig armor class then storage capacity;
- weapon/pistol has no automatic superiority replacement;
- locked item instances and equivalent reserved capacity must survive carrier replacement or replacement is forbidden;
- quantity-bearing ammo/currency uses explicit quantity for needed count, value and weight;
- final proposed state must satisfy the Strength-based modeled carry limit.

## Schema

```text
Desktop: 1.16.2
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## Canonical references

- `docs/.release-v1.16.2-status.json`
- `docs/RELEASE_NOTES_V1.16.2.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.16.2 release identity above.
