# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.5 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 public stable은 **v1.15.5**다.

```text
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head:
2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
merge PR: #271
PR CI / Shutdown / Docs:
33516899412 / 33516899393 / 33516899505 — SUCCESS
exact-main CI / Shutdown / Docs:
33520705401 / 33520705533 / 33520705395 — SUCCESS
Release workflow: 33521076146 — SUCCESS
release id: 380587916
published UTC: 2026-09-01T14:42:06Z
593 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539684740
bytes: 80,705,841
SHA-256: 32df6c471cf79349932a83a5d7598fecb8971548e4b38bb7bdab917602898d69

SHA256SUMS.txt
asset id: 539684739
bytes: 86
asset SHA-256: 683a2374431389efdc7d3176816917ef8ef466c2b493aa9bc78dfd6416be4f98
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9805674187
bytes: 242,052,034
SHA-256: 6281d8f2ef0f5ab0d0b6414b6cded95852f9006d23806527c8467badb8bfc088
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.5` all resolve to `62466a957a7e32a623a0ffcfad96bfb16504f823`. Documentation-only commits after the release are not v1.15.5 product sources.

## 2. Farming Guide complete-equipment boundary

The v1.15.2 complete-equipment decision remains current. Weapons, helmets, body armor and other equipment are opaque complete items. Weapon/helmet attachment editing, armor-plate editing and equipment-internal raid targets are not exposed. Legal top-level equipment targets remain available.

## 3. Source-backed nested storage

`FarmingGuideStoredItemState.ParentInstanceId` is the canonical nested-storage address. A stored item supports nested storage when current validated Game Content exposes real `StorageGrids`.

- container names such as Key tool are not product allowlists;
- source grid dimensions and allowed/excluded filters are authoritative;
- manual drag/drop, sanitizer and raid planning share storage-filter authority;
- supported containers may remain nested inside Secure Container or another legal storage surface;
- arbitrary legal depth uses the parent-instance chain;
- orphan, duplicate, self-parent, cycle, bad-grid, filter, bounds and overlap failures fail closed;
- a dedicated positive-allow-list nested grid is evaluated before general root empty space.

### Nested Workbench viewport — v1.15.5

Workbench sizing accounts for rendered grid footprint, header, border/padding, ScrollViewer template chrome and any genuinely required system scrollbar. Horizontal and vertical overflow are solved together. If the complete grid fits the effective center-column viewport, both scroll axes are explicitly disabled and the full grid is visible. Scrolling remains only for genuine physical overflow.

## 4. Preservation-first state-transition planning — v1.15.5

The raid planner now treats the result of a scan as a candidate whole-inventory state rather than assuming displaced equipment disappears.

Governing order:

1. legal empty equipment target;
2. source-proven equipment upgrade;
3. direct legal storage;
4. non-destructive global repacking;
5. preserve displaced equipment/carriers by legal storage or nesting;
6. bounded value-aware eviction + repacking when clearly preferable;
7. discard only when no better legal retained state exists.

### Displaced equipment is loot

When an incoming item replaces an equipped item or carrier, the displaced item becomes a movable loot candidate. The planner attempts to retain it in legal root/nested storage before allowing destruction. This applies to ordinary equipment slots, PrimaryWeapon/Holster, Rig, Backpack and the body-armor + ordinary-rig → armored-rig atomic transition. Secure Container remains conservative under existing compatibility/lock rules.

### Containers remain storage after displacement

A displaced rig/backpack that is nested in another legal surface keeps its own real source-backed grids. Existing items may therefore move into that displaced container within the same ProposedSnapshot, provided filters, parent existence, cycle prevention, locks and reserved-cell constraints remain valid.

### Bounded destructive optimization

Destructive search may evict more than one low-retention leaf when needed for a clearly superior retained state, but remains bounded and deterministic. Locked items/subtrees, populated containers and protected structural state are excluded from automatic victim selection. Candidate eviction prefixes are evaluated by the separate retention policy and the existing non-destructive repacking solver.

## 5. Retention and Needed truth

Geometry/legality does not decide value. `FarmingGuideLootRetentionPolicy` is the policy boundary for retention ranking and destructive eligibility.

Needed acquisition is not a historical accepted-scan counter. Raid-acquired quantity is derived from:

```text
current snapshot count(Item ID) - raid baseline snapshot count(Item ID)
```

The inventory counter recursively covers modeled equipment, carriers, stored items and compatible persisted trees. Therefore an acquired Needed item that is later discarded becomes needed again.

Known item weight may be policy metadata, but no player-specific live carry threshold is invented. Unsupported live facts such as actual durability, plate condition, partial stack count, hydration/energy and live magazine/chamber state continue to fail conservatively.

## 6. Compact raid instruction presentation — v1.15.5

Presentation is separate from planner state transitions. It consumes the current/proposed snapshot difference and does not alter `Action` or `ProposedSnapshot`.

Primary vocabulary:

- `[장비 위치] 장착`
- `방탄복 교체`
- `헤드셋 교체`
- `[장비 위치] 교체`
- `방탄 리그 전환`
- `[보관 위치] 보관`
- `[보관 위치] [기존 아이템] 버리고 보관`
- `버리기`

Same visible storage-area grid/X/Y/rotation rearrangement is silent. Only real cross-area moves/removals are appended as `+ [아이템] 이동 [위치]` or `+ [아이템] 버리기`; multiple operations use comma separation.

## 7. Lock / safety contracts

- ordinary unlocked stored item: neutral border;
- `F`-locked stored item: accent/yellow border;
- reserved empty cells and equipment/carrier locks remain authoritative;
- locked ancestors/subtrees are preserved;
- populated nested containers are not destructively replaced based only on parent standalone value;
- explicit recommendation acceptance remains the transaction boundary;
- source-backed equipment superiority rules and dedicated-container preference remain current.

## 8. Schema / compatibility

```text
Desktop: 1.15.5
Public stable: 1.15.5
Content write: v11
Content readable: v3-v11
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## 9. Verification evidence

The immutable product source `62466a957a7e32a623a0ffcfad96bfb16504f823` passed:

- Windows Release/XAML desktop build;
- 593 deterministic tests with zero failure/skip;
- self-contained win-x64 publish;
- actual published EXE Product UI / Map / Farming Guide runtime smoke;
- 4x4 Key-tool-like nested Workbench dual-axis fit regression smoke;
- compact Farming Guide instruction regression smoke;
- displaced equipment/carrier preservation and nested-repacking transition smoke;
- Needed baseline-vs-current counting tests;
- bounded retention/destructive-policy tests;
- graceful shutdown and active-async Shutdown Race;
- release package/checksum verification;
- exact-main Documentation Consistency;
- automated Release workflow;
- public tag/release/latest/asset digest readback.

## 10. Canonical references

- `docs/PROJECT_STATE.json`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`
- `docs/RELEASE_1.15.5.md`
- `docs/RELEASE_NOTES_V1.15.5.md`
- `docs/.release-v1.15.5-status.json`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 11. External evidence still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains open and does not change the verified public release identity.
