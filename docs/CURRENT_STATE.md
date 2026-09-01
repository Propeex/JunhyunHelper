# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.16.0
exact product source/tag target:
f1c00b0ac9ea0b70f81991d30be9a04128253d48
validated PR head: bbc8dc25ec35ba24a64df00445b4454bbd7f66d8
merge PR: #273
PR CI / Shutdown / Docs:
33537853686 / 33537853397 / 33537853539 — SUCCESS
exact-main CI / Shutdown / Docs:
33538397901 / 33538397873 / 33538397904 — SUCCESS
Release workflow: 33538760085 — SUCCESS
release id: 380701728
published UTC: 2026-09-01T17:37:02Z
610 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539905673
bytes: 80,716,585
SHA-256: db6a769bbe1d0213b7d5e1d59416b230f4c8387554d1d9c9354701c1da56e233

SHA256SUMS.txt
asset id: 539905674
bytes: 86
asset SHA-256: 2d77327a477ac8df8701517890902622323b5b2d8b8c787de0b85ef8a71cd93f
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9812704124
bytes: 242,082,062
SHA-256: 328e2d8a30803443d497f1a85a98b56e672cbdcd36e01d6573a13d580cf7fc49
```

`v1.16.0` is `draft=false`, `prerelease=false`, and its target is the exact product source above.

## Farming Guide current contract — v1.16.0

### Deterministic rulebook

Farming Guide no longer treats loot as a weighted score. The product contract is **제약 확인 → 중요도 비교 → 상황 대처 결정**. Illegal Tarkov states, locks, reserved cells, protected carrier-role migration and final-state weight constraints are hard gates before priority comparison.

### Priority and economics

- Only items whose requirement specifically needs **Found in Raid** receive special needed priority.
- Non-FIR needed items are ordinary economic loot for Farming Guide purposes.
- Economic value is **average Flea Market price**.
- If space requires destructive replacement, compare the incoming item's total Flea value against the total Flea value of the actual sacrificed set; do not use universal item-to-item ₽/slot ranking.
- Quantity-dependent items use their entered quantity for needed count, total value and weight.

### Equipment superiority

- body armor / helmet: armor class
- headset: hearing distance
- ordinary rig / backpack / secure container: storage capacity
- armored rig: armor class first, then storage capacity only when armor class is equal
- weapon / pistol: no automatic superiority replacement

Unknown durability, remaining uses and firearm assembly state are not inferred.

### Protected state and carrier replacement

Protected state consists only of **locked items and reserved cells**. Storage-bearing equipment replacement must preserve locked item instances and recreate equivalent connected reserved-cell shapes/capacity in the replacement carrier. If that protected state cannot legally migrate, replacement is forbidden.

### Stack quantity

Ammo/currency and other authoritative quantity-dependent items request quantity before Mini Scanner Farming Guide recommendation. Stored stack quantity is persisted in Farming Guide state schema v3, displayed on item cards and editable by double-click. New scan cancels stale pending quantity input.

### Weight

The Farming Guide footer exposes current modeled weight and the Strength-based carry limit. Strength level is persisted per profile. Final proposed state must satisfy the configured carry-weight rule; if the current manually reflected state is already above the limit, a recommendation may only preserve or reduce that weight until the state returns under the limit.

### MiniMap hotkey cleanup

Bare NumPad 0–5 no longer trigger direct floor selection. Existing configurable floor-up/floor-down hotkeys remain available. The donor-compatible hook lifecycle is preserved.

### Runtime stability fix found during release validation

The first v1.16 release candidate created a WPF `LayoutUpdated` feedback loop by repeatedly assigning unchanged weight/badge presentation values. This starved Dispatcher `ContextIdle`, preventing Map runtime smoke evidence from executing. Presentation refresh is now idempotent: UI properties change only when the rendered value actually differs. Final PR and exact-main published-EXE smoke both pass.

## Schema

```text
Desktop: 1.16.0
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## Canonical references

- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/RELEASE_NOTES_V1.16.0.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.16.0 release identity above.
