# STATE — 현재 프로젝트 상태

> 복구 순서는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md`입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`이 기준입니다.

기준일: **2026-09-03 KST**  
상태: **v1.17.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 공개 제품 상태

```text
public stable: v1.17.0
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
validated PR head:
a01d61cd9957db94a7475734c1e8df66ce71f53d
merge PR: #288
PR CI / Shutdown / Docs:
33746966753 / 33746966804 / 33746966771 — SUCCESS
exact-main CI / Shutdown / Docs:
33748900315 / 33748900348 / 33748900377 — SUCCESS
Release workflow: 33749193376 — SUCCESS
release id: 381959220
published UTC: 2026-09-03T11:21:35Z
649 passed / 0 failed / 0 skipped
```

Public release:

```text
Junhyun-Helper.zip
asset id: 542663027
bytes: 80,766,362
SHA-256: 6ecc3a61d0b492f6b475e18f309e55790776911e5496fc704d12ffd611c629cb

SHA256SUMS.txt
asset id: 542663026
bytes: 86
asset SHA-256: 7a2fb4f7ebcb333eafd8cad6f9acbf532549118e608776786666014a24875bdf
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9890816795
bytes: 242,234,759
SHA-256: d9115f24968804fc5b4e65fa7bbaaf008f4af516e044f3b00e0ee6b4525a15dd
```

Release workflow `33749193376` checked out exact product source `8b0e1f8f46fa3822f4cff05b7be3223d40ad7435`, downloaded exact-main artifact `9890816795`, verified ProductVersion/FIRST_RUN identity, independently matched the release ZIP hash to `SHA256SUMS.txt`, and published stable `v1.17.0`. The release is `draft=false` and `prerelease=false`.

## 2. v1.17.0 authoritative Farming Guide objective

The active raid planner no longer uses the v1.16 tactical/category/equipment-superiority decision stack as product authority.

Every Scanner-confirmed incoming item during an active Farming Guide raid is represented by ephemeral `FarmingGuideItemState.RaidAcquired=true`. Scanner does not classify FIR from an icon/check/color/text and does not add a FIR confirmation interaction. `RaidAcquired` is `[JsonIgnore]` and is not persisted to presets/working state.

Every scan is evaluated as a complete final-state optimization with exactly two lexicographic score dimensions:

1. maximize currently needed FIR Quest/Hideout units, capped by remaining need;
2. maximize complete final retained average-Flea value.

No other item-priority tie-breaker is authoritative. Weight, footprint, item category, survival utility and equipment superiority are not farming score dimensions.

## 3. Global final-state planning

The candidate set includes all movable current roots plus the incoming item across:

- ordinary stored items;
- top-level equipment;
- Rig / Backpack / Secure Container roots;
- nested container roots;
- incoming Scanner item.

Melee and Dogtag remain fixed setup state outside the candidate pool but still contribute to final weight. Compatible dedicated storage is a legal placement option, not a retention priority.

`FarmingGuideGlobalPackingPlanner` rebuilds all unlocked placements from scratch. Current unlocked placement is only a stability ordering preference after the retained set is chosen. Search budget exhaustion is explicit `BudgetExceeded`; it is never treated as proof that a destructive optimum does not exist.

## 4. System legality and fail-closed facts

Final proposals must prove:

- real width/height and rotation;
- grid bounds and collision;
- source-backed storage-grid filters;
- parent/child ownership and no self/indirect container cycle;
- equipment-slot compatibility, including `ItemPropertiesHeadwear` for headwear;
- attachment and armor-plate slot filters;
- item conflicts and bidirectional `ConflictingSlotIds`;
- body armor vs armored-rig conflict;
- helmet/headset blocking;
- exact stack quantity semantics;
- item/cell/root fixed constraints;
- final modeled carry weight.

Complete retained Flea/FIR and weight calculations include modeled attachment and armor-plate descendants. Unknown required root geometry, weight or Flea value is not replaced by 1x1/0 kg/0 ₽ for destructive advice. The existing Scanner catalog/presentation resolver supplies canonical item facts; no new observation source or automatic inference flow was introduced.

## 5. Fixed-state contract

An explicitly fixed item/cell is a hard constraint, not extra value.

- fixed item identity, storage, grid, X/Y, rotation, parent and quantity must remain unchanged;
- a stored ancestor cannot move if that indirectly moves a fixed descendant or fixed nested cell;
- the containing root Rig / Backpack / Secure Container is therefore fixed when necessary;
- a fixed carrier's independent legal free storage remains usable;
- unlocked contents remain ordinary planning candidates unless separately fixed.

Same-storage-area movement/rotation required by a legal global solve is surfaced to the user as `내부 재배치`; required physical changes are not hidden.

## 6. Stack and weight contract

Ammo and Currency keep the existing user quantity-input flow. The entered quantity models one actual observed stack instance. v1.17.0 does not invent maximum-stack facts or automatic split/merge semantics.

Quantity scales FIR units, Flea value and weight. Weight remains only a final-state feasibility constraint under the configured Strength rule; it never breaks item-priority ties.

## 7. Regression and published-runtime coverage

PR #288 and exact-main both passed the full Windows gate. Coverage includes:

- FIR absolute priority and remaining-need cap;
- equal FIR → complete retained Flea optimization;
- no tactical food/drink privilege;
- stack quantity in FIR/value/weight;
- complete attachment/plate value and weight;
- unknown price/weight/geometry fail closed;
- incoming container capacity in the same scan;
- equipment replacement/relocation and consecutive scans;
- same-area repacking instruction;
- fixed nested-item and fixed-cell ancestry propagation;
- dedicated nested storage through the v1.17 global solver;
- owner-cycle rejection;
- assembly slot conflicts and headwear compatibility;
- actual published EXE Product UI / Map / Scanner / Farming Guide smoke;
- graceful shutdown, clean portable root and release package/checksum verification;
- dedicated Shutdown Race CI and Documentation Consistency.

Deterministic result on exact product source: **649 passed / 0 failed / 0 skipped**.

## 8. Schema / canonical references

```text
Desktop: 1.17.0
Content write/read: v12 / v3-v12
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
Map donor revision: d933792b6042a51cea38dc44b686a096fe30de67
```

Canonical evidence:

- `docs/PROJECT_STATE.json`
- `docs/.release-v1.17.0-status.json`
- `docs/RELEASE_NOTES_V1.17.0.md`
- `docs/CURRENT_STATE.md`
- `docs/DECISION_V1.17.0_FARMING_GUIDE_RULEBOOK.md`

Automated implementation, merge, exact-main and public release validation are complete. Actual Tarkov play validation on the user's own environment remains a separate `PENDING` evidence field and does not make v1.17.0 development or release incomplete.
