# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다.

현재 public stable은 **v1.15.4**다.

```text
exact product source/tag target:
c27daf2177b643ee16d4a3d5b0997e54a267c2c7
release id: 380429049
published UTC: 2026-09-01T11:12:15Z
585 passed / 0 failed / 0 skipped
```

Validation:

```text
validated PR head: da9e788a8494734149cfa0e65eff3535e14d2bac
merge PR: #268
PR CI: 33500484624 — SUCCESS
PR Shutdown Race: 33500484673 — SUCCESS
PR Documentation Consistency: 33500484510 — SUCCESS
exact-main CI: 33500904378 — SUCCESS
exact-main Shutdown Race: 33500904396 — SUCCESS
exact-main Documentation Consistency: 33500904356 — SUCCESS
Release workflow: 33501233130 — SUCCESS
```

Draft PR #267 used the same final branch/head and passed its gates, but the connected GitHub draft-to-ready GraphQL action failed on an API schema field. It was closed without merge and replaced administratively by non-draft PR #268; no implementation rollback or head substitution occurred.

Public package:

```text
Junhyun-Helper.zip
asset id: 539435772
bytes: 80,695,104
SHA-256: a0a5d6f19beecab7b656250e3d1ae56d3073aae442b7cdc9b19b865a7d8a9e81

SHA256SUMS.txt
asset id: 539435771
bytes: 86
asset SHA-256: 86627e394474b4fb69b27c5db6cc380a2f0a3ebf1876ee6d842159436014ac89
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9797756949
bytes: 242,014,938
SHA-256: 2ab185334c441dfa44f8d1afb774e7c7c6815df07849563ba865210a9b5857bb
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.4` all resolve to `c27daf2177b643ee16d4a3d5b0997e54a267c2c7`. Documentation-only commits after the release are not v1.15.4 product sources.

## 2. Farming Guide complete-equipment boundary

The v1.15.2 complete-equipment decision remains current.

Weapons, helmets, body armor and other equipment are opaque complete items in Farming Guide.

- weapon/helmet attachment editing is not exposed;
- armor-plate editing is not exposed;
- equipment-internal drag/drop and raid Equip/ReplaceEquip targets are not generated;
- persisted legacy attachment/armor state remains readable for compatibility but current runtime normalizes it to root-only equipment state;
- legal top-level equipment targets remain available;
- source attachment/default-preset metadata may still be used as read-only evidence for authoritative complete-item presentation.

v1.15.4 does not reopen this boundary. Its protection comparison uses only the representative top-level source armor class available to the complete-item model and does not claim knowledge of manually changed in-raid plates.

## 3. Source-backed nested storage

`FarmingGuideStoredItemState.ParentInstanceId` is the canonical nested-storage address. A stored item supports nested storage when current validated Game Content exposes one or more real `StorageGrids`.

- container names such as Key tool are not product allowlists;
- each source grid preserves width/height and allowed/excluded category/item filters;
- manual drag/drop, sanitizer and raid planning share storage-filter authority;
- supported containers may remain nested inside Secure Container or another legal storage surface;
- arbitrary legal depth uses the existing parent-instance chain;
- orphan, duplicate, self-parent, cycle, bad-grid, filter, bounds and overlap failures fail closed;
- root Rig/Backpack/Secure Container storage stays on the main Farming Guide surface;
- a positive-allow-list nested grid that accepts an incoming item is evaluated as dedicated storage before general root empty space.

### Nested Workbench viewport — v1.15.4

A source-backed nested storage surface should show the full grid without horizontal scrolling when the complete physical grid fits the effective center-column viewport.

WPF horizontal scrolling is therefore a real-overflow fallback, not an Auto-scrollbar feedback source. This prevents a manufactured horizontal scrollbar from reducing height/width and clipping otherwise fitting Key tool or other specialized-container cells.

## 4. Preservation-first raid planning — v1.15.4

v1.15.3 direct placement treated existing stored placements as effectively fixed. v1.15.4 adds a bounded deterministic repacking domain so sufficient total capacity is not discarded merely because movable items fragment a required footprint.

Recommendation order:

1. legal empty equipment target;
2. objectively proven, structurally safe equipment upgrade;
3. direct legal storage without moving existing items;
4. non-destructive legal repacking of unlocked existing items;
5. need/value-based destructive replacement only after preservation options fail;
6. discard only when no preferable legal plan exists.

The repacking planner may move multiple unlocked items, rotate eligible items and move them across legal root/nested storage surfaces. It must preserve:

- source-backed storage filters;
- dedicated-container preference;
- reserved cells;
- item/equipment/carrier locks;
- locked-ancestor constraints;
- parent/descendant graph and descendant attachment;
- self/descendant cycle prohibition;
- canonical sanitizer validity.

A populated nested container is not an eligible destructive replacement merely because the parent item's standalone value is lower than the incoming item.

## 5. Equipment superiority — v1.15.4

Loot value/need and equipment performance are separate facts. Price is not used to invent equipment superiority.

Conservative automatic upgrade rules:

- same-target protective equipment: incoming representative top-level `properties.class` must be strictly higher;
- Backpack: raw source-backed storage capacity must be strictly larger and all modeled contents must fit legally in the incoming carrier;
- ordinary Rig: raw source-backed storage capacity must be strictly larger and all modeled contents must fit legally;
- armored rig -> armored rig: protection class and capacity must both be non-regressing and at least one must strictly improve;
- Headset: `distanceModifier` must be no worse and `distortion` must be no worse, with at least one strict improvement; trade-offs are not automatically ranked.

### Body armor + ordinary rig -> armored rig

A scanned armored rig may replace ordinary body armor plus an ordinary rig only as one atomic transition when:

- the incoming item is source-classified as armored rig;
- representative protection class is strictly higher than the current body armor;
- body-armor and rig targets are not automation-locked;
- equipment conflicts are legal after removing body armor;
- every current modeled rig item can be packed into the incoming rig's real source-backed grids;
- filters/reservations/locks/nested descendants remain valid;
- the final proposed snapshot passes the canonical sanitizer.

Nothing mutates until explicit recommendation acceptance. If the combined transition cannot be proven legal, it fails closed; the incoming armored rig is not reinterpreted as an ordinary rig replacement while body armor remains equipped. The reverse operation cannot fabricate a missing ordinary rig from one scanned item.

## 6. Lock / Scanner-driven retained contracts

Stored-item visual lock contract:

- ordinary unlocked stored item: neutral border;
- `F`-locked stored item: accent/yellow border;
- unlocking restores neutral border;
- reserved empty-cell and equipment/carrier lock behavior remains unchanged.

Search-result hover + `T` remains a product test input for the real Farming Guide recommendation path.

- hovered result takes precedence even when Search TextBox retains keyboard focus;
- no hovered result means `T` remains normal search input;
- Scanner capture mode need not be enabled;
- if the same-mode catalog is absent from memory after restart, verified local Scanner catalog data may be loaded on demand;
- preparation failures are surfaced instead of silently ignored.

## 7. Game Content schema / migration

```text
Desktop: 1.15.4
Public stable: 1.15.4
Content write: v11
Content readable: v3-v11
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

Game Content v11 persists:

- `FarmingGuideItemLayout.ArmorClass`;
- `FarmingGuideItemLayout.HeadsetDistanceModifier`;
- `FarmingGuideItemLayout.HeadsetDistortion`.

Readable v3-v10 snapshots remain valid offline last-known-good. When Desktop loads an older readable snapshot, it opportunistically attempts a normal transactional Data Update after active content is available. Failure does not delete the readable snapshot or block startup; migration can retry later. Product-smoke mode skips this opportunistic network refresh to keep published EXE validation deterministic/offline.

## 8. Verification evidence

The immutable product source `c27daf2177b643ee16d4a3d5b0997e54a267c2c7` passed:

- Windows Release/XAML desktop build;
- 585 deterministic tests with zero failure/skip;
- self-contained win-x64 publish;
- actual published EXE Product UI / Map / Farming Guide runtime smoke;
- fragmented-capacity non-destructive repacking smoke;
- nested Workbench viewport smoke;
- body armor + populated ordinary rig -> armored rig content-preservation/repacking smoke;
- graceful shutdown;
- active-async Shutdown Race;
- release package/checksum verification;
- exact-main Documentation Consistency;
- automated Release workflow;
- public tag/release/latest/asset metadata readback.

## 9. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`
- `docs/RELEASE_1.15.4.md`
- `docs/RELEASE_NOTES_V1.15.4.md`
- `docs/.release-v1.15.4-status.json`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 10. External evidence still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains open and does not change the verified public release identity.
