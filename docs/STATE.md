# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 public stable은 **v1.16.1**이다.

```text
exact product source/tag target:
7fb148434d22fac823d57d88021f9615081c47cd
validated PR head:
7d7cf002aa4f1d61c891b340ff73c56781655d64
merge PR: #276
PR CI / Shutdown / Docs:
33589038565 / 33589038575 / 33589038576 — SUCCESS
exact-main CI / Shutdown / Docs:
33589274983 / 33589275133 / 33589275021 — SUCCESS
Release workflow: 33589497077 — SUCCESS
release id: 380969416
published UTC: 2026-09-02T04:06:31Z
612 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 540589667
bytes: 80,717,818
SHA-256: 8599645a2d0a38c6b74f4f79cab71120b26e378da254a98605610f1c7493b3c3

SHA256SUMS.txt
asset id: 540589668
bytes: 86
asset SHA-256: c78b0be06dbcf3f5239591d796f3b6a94299445e45157012ee122972cbfcaeee
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9831224038
bytes: 242,086,160
SHA-256: 74435818344f94d6cd9d8fb918582dbdb3b047e789aa0f2f47c398facfbabd2a
```

The public `v1.16.1` release targets `7fb148434d22fac823d57d88021f9615081c47cd`, is `draft=false` and `prerelease=false`. The release workflow checked out that exact source, downloaded exact-main artifact `9831224038` with digest verification, verified the published EXE/FIRST_RUN identity, compared the manifest hash to the actual `Junhyun-Helper.zip` hash, then published and read back the stable release. Documentation-only commits after release are not v1.16.1 product sources.

## 2. v1.16.1 maintenance hardening

### Farming Guide partial-state recovery

The Farming Guide state store already used atomic JSON writes and backup recovery, but syntactically valid JSON with semantically partial/null members could bypass corrupt-file recovery and fail later during normalization.

v1.16.1 normalizes this boundary defensively:

- null profile/preset/snapshot/lock/fixed-equipment collections recover to valid defaults;
- valid equipment, presets and stored items are salvaged where possible;
- structurally unusable entries lacking required identity are discarded;
- attachment and armor-plate subtrees are normalized recursively;
- stack quantity is normalized to the existing minimum-one contract;
- Strength settings are clamped through the existing product policy;
- legacy dogtag persistence remains removed.

The deterministic regression `LoadProfile_SemanticallyPartialJson_IsNormalizedAndRemainsWritable` verifies partial document load, salvage/normalization, save and reload.

### Startup content-schema stale continuation guard

The opportunistic schema refresh previously captured the initiating game mode but asynchronous cache/update boundaries allowed a narrow stale-continuation window if the active profile changed during the operation.

v1.16.1 captures both `ProfileId + GameMode` and rechecks them after asynchronous boundaries and before owning busy state or applying refreshed content/workspaces. A completed operation for an old profile therefore cannot write through a newly active profile.

A maintenance source-contract regression preserves these identity guards.

### Product/UI review result

The maintenance pass inspected:

- MainWindow profile/data update/lifecycle and shutdown paths;
- Farming Guide persistence, nested storage/workbench, quantity and weight state;
- Scanner runtime/coordinator/settings/UI-state persistence;
- Map/MiniMap product settings and window-state persistence;
- atomic storage/content activation and image cache;
- updater/service ownership/disposal;
- rendered WPF runtime smoke for Scanner, Ammo, Farming Guide, Quest, overlays and Map/MiniMap;
- published EXE startup, Product UI, Map, graceful shutdown, Shutdown Race and packaging.

No additional user-visible defect was reproduced with enough evidence to justify speculative layout or behavior changes. A narrower future test opportunity remains explicit minimum-main-window containment, but no current clipping failure was reproduced from source/runtime evidence.

## 3. Farming Guide deterministic rulebook — v1.16.0 behavior retained

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

## 4. Priority / economics

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

## 5. Equipment superiority

Automatic replacement uses simple representative criteria and does not infer hidden instance facts.

- body armor / helmet: armor class;
- headset: hearing distance;
- ordinary rig / backpack / secure container: storage capacity;
- armored rig: armor class first; storage capacity only when armor class is equal;
- weapon / pistol: no automatic superiority replacement.

Durability, remaining uses and actual firearm assembly state are not inferred from item identity.

## 6. Protected state / carrier migration

Protected state consists only of:

1. locked items;
2. reserved cells.

When a storage-bearing equipment item is replaced:

- locked item instances must survive;
- legal relocation is allowed;
- reserved empty space is migrated by connected shape/capacity rather than fixed coordinates;
- the replacement is forbidden if equivalent protected state cannot be represented legally.

Nested storage continues to use current source-backed `StorageGrids`, filters and `ParentInstanceId` graph rules. Cycles, overlap, invalid parents and illegal filter placements fail closed.

## 7. Stack quantity state / UI

Farming Guide state schema is **v3**.

- Mini Scanner requests quantity before Farming Guide recommendation for authoritative quantity-dependent item types.
- Enter commits the quantity into the same recommendation path.
- a new scan cancels stale pending quantity input;
- stored stack quantity persists in state/presets;
- stack count is displayed on Farming Guide item cards;
- double-click opens quantity editing;
- quantity changes participate in value, weight and needed counting.

## 8. Strength / weight rule

Strength level is stored per Farming Guide profile. The footer displays current modeled weight and the Strength-derived carry limit.

The current v1.16 product rule uses the Tarkov mechanics recorded in the release decision/docs: base maximum carry weight 77 kg, +0.6% per Strength level, Elite approximately 100 kg, with Elite weapon-slot weight exclusion according to the implemented policy.

Final proposed state must be within the calculated limit. If the user's reflected current state is already above the limit, recommendations may only preserve or reduce weight until the modeled state returns within limit.

## 9. MiniMap hotkey cleanup

Bare NumPad 0–5 no longer map to direct floor selection. Existing configurable floor-up/floor-down hotkeys remain available.

The donor-compatible global keyboard hook lifecycle remains intact; only the direct NumPad floor-index mapping is disabled.

## 10. Runtime stability correction retained from v1.16.0

The first v1.16 release candidate introduced a WPF `LayoutUpdated` feedback loop:

- weight button content was reassigned on every layout cycle;
- quantity badge text could likewise be reassigned even when unchanged;
- those writes generated further layout work;
- Dispatcher `ContextIdle` callbacks were starved;
- Map extract runtime smoke evidence never started even while the process remained responsive.

The presentation refresh remains idempotent: rendered values are assigned only when they actually differ. v1.16.1 PR and exact-main published-EXE smoke pass, again verifying the Map/Factory/MiniMap runtime evidence path is not starved.

## 11. Complete-equipment / storage boundary retained

Weapons, helmets, body armor and other equipment remain opaque complete items. Weapon/helmet attachment editing and armor-plate editing are not exposed in Farming Guide.

Source-backed nested storage remains authoritative:

- real `StorageGrids` determine whether a stored item exposes internal storage;
- source dimensions and allowed/excluded filters are authoritative;
- dedicated positive-allow-list nested storage is preferred before general root storage;
- legal recursive nesting is supported;
- displaced storage-bearing equipment may remain a storage surface inside the same proposed snapshot when legally retained;
- nested Workbench viewport fit keeps the v1.15.5 clipping fix.

## 12. Transaction / state truth contracts

- Scanner owns confirmed item identity and scanned price/needed facts; Farming Guide owns final store/equip/replace/discard recommendation.
- user-reflected modeled state is treated as current truth;
- recommendations are revision-bound;
- explicit user accept is the only commit boundary;
- a new scan rejects an unaccepted pending recommendation without mutating the committed state;
- manual Farming Guide edits invalidate stale pending recommendations.

## 13. Schema / compatibility

```text
Desktop: 1.16.1
Public stable: 1.16.1
Content write: v11
Content readable: v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## 14. Verification evidence

The immutable product source `7fb148434d22fac823d57d88021f9615081c47cd` passed:

- Windows Release/XAML desktop build;
- 612 deterministic tests with zero failure/skip;
- self-contained win-x64 publish;
- actual published EXE Product UI / Map / Farming Guide runtime smoke;
- Map extract and MiniMap lifecycle evidence;
- Farming Guide partial-state persistence recovery regression;
- stale profile/content schema refresh identity regression;
- stack quantity / persistence / rulebook regressions;
- Strength/weight policy regressions;
- protected carrier-role migration regressions;
- graceful shutdown and active-async Shutdown Race;
- release package/checksum verification;
- exact-main Documentation Consistency;
- automated Release workflow;
- exact-main Actions artifact digest readback;
- public tag/release/asset target, size and digest readback.

## 15. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/ACTIVE_WORK.md`
- `docs/CURRENT_STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/RELEASE_NOTES_V1.16.1.md`
- `docs/.release-v1.16.1-status.json`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 16. External evidence still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING` and does not change the verified public release identity.
