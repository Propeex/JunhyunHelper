# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 public stable은 **v1.16.0**이다.

```text
exact product source/tag target:
f1c00b0ac9ea0b70f81991d30be9a04128253d48
validated PR head:
bbc8dc25ec35ba24a64df00445b4454bbd7f66d8
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
name: JunhyunHelper-win-x64
artifact id: 9812704124
bytes: 242,082,062
SHA-256: 328e2d8a30803443d497f1a85a98b56e672cbdcd36e01d6573a13d580cf7fc49
```

The public `v1.16.0` release targets `f1c00b0ac9ea0b70f81991d30be9a04128253d48`, is `draft=false` and `prerelease=false`. Documentation-only commits after release are not v1.16.0 product sources.

## 2. Farming Guide deterministic rulebook — v1.16.0

The raid planner follows a deterministic manual/rulebook rather than a weighted score:

```text
hard constraints
→ importance / priority
→ applicable situation response
→ one legal proposed state
→ explicit user accept
```

No speculative future-value score or weighted optimization is part of the product contract.

### Hard constraints

- current validated Tarkov placement/filter/conflict/nesting rules;
- locked items;
- reserved cells;
- protected-state migration when replacing storage-bearing equipment;
- final-state carry-weight rule;
- explicit transaction/revision boundary.

Illegal candidates fail closed before priority comparison.

## 3. Priority / economics

### FIR needed

Only an item whose requirement specifically needs **Found in Raid** receives special needed priority. Non-FIR needed items remain ordinary economic loot because they can be acquired through money.

### Economic value

Farming Guide economic value uses **average Flea Market price**. Trader price is not mixed into the Farming Guide importance rule.

When space requires destructive replacement, the incoming item's total value is compared with the total value of the actual sacrificed item set. Universal item-to-item ₽/slot ordering is not the decisive rule.

### Quantity-dependent items

Quantity-dependent items use the user-entered quantity for:

- total economic value;
- current-needed quantity accounting;
- total modeled weight.

## 4. Equipment superiority

Automatic replacement uses simple representative criteria and does not infer hidden instance facts.

- body armor / helmet: armor class;
- headset: hearing distance;
- ordinary rig / backpack / secure container: storage capacity;
- armored rig: armor class first; storage capacity only when armor class is equal;
- weapon / pistol: no automatic superiority replacement.

Durability, remaining uses and actual firearm assembly state are not inferred from item identity.

## 5. Protected state / carrier migration

Protected state consists only of:

1. locked items;
2. reserved cells.

When a storage-bearing equipment item is replaced:

- locked item instances must survive;
- legal relocation is allowed;
- reserved empty space is migrated by connected shape/capacity rather than fixed coordinates;
- the replacement is forbidden if equivalent protected state cannot be represented legally.

Nested storage continues to use current source-backed `StorageGrids`, filters and `ParentInstanceId` graph rules. Cycles, overlap, invalid parents and illegal filter placements fail closed.

## 6. Stack quantity state / UI

Farming Guide state schema is **v3**.

- Mini Scanner requests quantity before Farming Guide recommendation for authoritative quantity-dependent item types.
- Enter commits the quantity into the same recommendation path.
- a new scan cancels stale pending quantity input;
- stored stack quantity persists in state/presets;
- stack count is displayed on Farming Guide item cards;
- double-click opens quantity editing;
- quantity changes participate in value, weight and needed counting.

## 7. Strength / weight rule

Strength level is stored per Farming Guide profile. The footer displays current modeled weight and the Strength-derived carry limit.

The current v1.16 product rule uses the Tarkov mechanics recorded in the release decision/docs: base maximum carry weight 77 kg, +0.6% per Strength level, Elite approximately 100 kg, with Elite weapon-slot weight exclusion according to the implemented policy.

Final proposed state must be within the calculated limit. If the user's reflected current state is already above the limit, recommendations may only preserve or reduce weight until the modeled state returns within limit.

## 8. MiniMap hotkey cleanup

Bare NumPad 0–5 no longer map to direct floor selection. Existing configurable floor-up/floor-down hotkeys remain available.

The donor-compatible global keyboard hook lifecycle remains intact; only the direct NumPad floor-index mapping is disabled.

## 9. Runtime stability correction discovered during v1.16 validation

The first v1.16 release candidate introduced a WPF `LayoutUpdated` feedback loop:

- weight button content was reassigned on every layout cycle;
- quantity badge text could likewise be reassigned even when unchanged;
- those writes generated further layout work;
- Dispatcher `ContextIdle` callbacks were starved;
- Map extract runtime smoke evidence never started even while the process remained responsive.

The presentation refresh is now idempotent: rendered values are assigned only when they actually differ. Final PR and exact-main published-EXE smoke both pass, which also verifies the Map/Factory/MiniMap runtime evidence path is no longer starved.

## 10. Complete-equipment / storage boundary retained

Weapons, helmets, body armor and other equipment remain opaque complete items. Weapon/helmet attachment editing and armor-plate editing are not exposed in Farming Guide.

Source-backed nested storage remains authoritative:

- real `StorageGrids` determine whether a stored item exposes internal storage;
- source dimensions and allowed/excluded filters are authoritative;
- dedicated positive-allow-list nested storage is preferred before general root storage;
- legal recursive nesting is supported;
- displaced storage-bearing equipment may remain a storage surface inside the same proposed snapshot when legally retained;
- nested Workbench viewport fit keeps the v1.15.5 clipping fix.

## 11. Transaction / state truth contracts

- Scanner owns confirmed item identity and scanned price/needed facts; Farming Guide owns final store/equip/replace/discard recommendation.
- user-reflected modeled state is treated as current truth;
- recommendations are revision-bound;
- explicit user accept is the only commit boundary;
- a new scan rejects an unaccepted pending recommendation without mutating the committed state;
- manual Farming Guide edits invalidate stale pending recommendations.

## 12. Schema / compatibility

```text
Desktop: 1.16.0
Public stable: 1.16.0
Content write: v11
Content readable: v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## 13. Verification evidence

The immutable product source `f1c00b0ac9ea0b70f81991d30be9a04128253d48` passed:

- Windows Release/XAML desktop build;
- 610 deterministic tests with zero failure/skip;
- self-contained win-x64 publish;
- actual published EXE Product UI / Map / Farming Guide runtime smoke;
- Map extract and MiniMap lifecycle evidence;
- stack quantity / persistence / rulebook regressions;
- Strength/weight policy regressions;
- protected carrier-role migration regressions;
- graceful shutdown and active-async Shutdown Race;
- release package/checksum verification;
- exact-main Documentation Consistency;
- automated Release workflow;
- public tag/release/asset digest readback.

## 14. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/ACTIVE_WORK.md`
- `docs/CURRENT_STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/RELEASE_NOTES_V1.16.0.md`
- `docs/.release-v1.16.0-status.json`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 15. External evidence still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING` and does not change the verified public release identity.
