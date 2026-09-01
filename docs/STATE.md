# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다. Runtime GPT/AI 의존성은 없다.

현재 public stable은 **v1.15.2**다.

```text
exact product source/tag target:
f4974ee6bed5047865581240197f7f0e2787ba7c
release id: 380290463
published UTC: 2026-09-01T07:24:43Z
562 passed / 0 failed / 0 skipped
```

Validation:

```text
validated PR head: 1662cc86f6298fc3a13bbcc591d38ae8c8e0787d
merge PR: #262
PR CI: 33481383672 — SUCCESS
PR Shutdown Race: 33481383604 — SUCCESS
PR Documentation Consistency: 33481383640 — SUCCESS
exact-main CI: 33481524940 — SUCCESS
exact-main Shutdown Race: 33481524896 — SUCCESS
exact-main Documentation Consistency: 33481524999 — SUCCESS
Release workflow: 33481956300 — SUCCESS
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539168506
bytes: 80,654,539
SHA-256: 642fa3845ccb4491c2d0b520000316d79067c3957144814b0b3b77516d14ad34

SHA256SUMS.txt
asset id: 539168503
bytes: 86
asset SHA-256: 077160c0ac6076e07d061a0feb8e386f131327ad82bc4281a619afc4ecd91741
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9790251740
bytes: 241,895,658
SHA-256: 57665346651872dd4f351241dabe77de09349150ebb2d8664f8d5f626a8daf65
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.2` all resolve to `f4974ee6bed5047865581240197f7f0e2787ba7c`. Follow-up documentation-only commits are not v1.15.2 product sources and cannot replace the immutable public assets.

## 2. Farming Guide — v1.15.2 current contract

v1.15.0 introduced the raid-session advisor. v1.15.1 corrected first real-play pending/lock/special-slot behavior. v1.15.2 now supersedes the v1.14-v1.15.1 assumption that Farming Guide should model/edit equipment internals.

### Complete-equipment runtime boundary

Weapons, helmets, body armor and other equipment are **opaque complete items** in Farming Guide.

- no weapon attachment/mod editor;
- no helmet attachment editor;
- no body-armor/armored-rig plate editor;
- no recursive assembly workbench or compatible-part picker;
- no equipment-internal drag/drop surface;
- no equipment-internal Equip/ReplaceEquip raid recommendation;
- persisted legacy `Attachments` / `ArmorPlates` remain readable for schema compatibility but are normalized to root-only item state in current runtime.

Current Game Content may continue to import attachment/armor/default-preset source metadata because that evidence is useful for validation, compatibility and authoritative complete-image selection. It is not current user-editable Farming Guide state.

Implementation boundary:

- `FarmingGuideCompleteEquipmentPolicy` projects source `GameItem` records into the Farming Guide runtime catalog;
- runtime layout removes attachment and armor slots;
- unsupported generic case/container internal grids are not exposed as Farming Guide storage surfaces;
- backpack/rig/secure-container root storage mechanics remain source-backed where product-supported;
- legacy assembly state sanitizes against the projected catalog and therefore collapses to root equipment.

### Top-level equipment

The user still equips/replaces complete items at legal top-level targets:

- Headset;
- Helmet;
- Face Cover;
- Armband;
- Body Armor;
- Eyewear;
- Primary Weapon 1 / 2;
- Holster / Pistol;
- Rig;
- Backpack;
- Secure Container;
- fixed Melee / Dogtag setup remains user-defined raid-start state.

Raid guidance may say `[장비 칸]에 장착` or `[장비 칸]의 [기존 장비]와 교체`. It may not tell the user to install a scope, muzzle, plate or other part inside equipment.

### Complete-item imagery

Farming Guide does not fabricate weapon assemblies.

Image priority is:

1. authoritative canonical default-preset image if the base item points to one;
2. source-backed item `Image512Url` / `GridImageUrl`;
3. canonical item icon fallback.

Equipment cards preserve aspect ratio but use substantially smaller internal safety margins so long weapons and other gear fill the equipment slot more like Tarkov. The old base-receiver + part-tile presentation is no longer the user-facing equipment model.

## 3. Farming Guide storage / nested storage

Ordinary placement uses current Tarkov item width/height, current validated grids/filters, bounded snap, bounds, overlap, contiguous-space and rotation rules.

### Root storage

- Pockets, Rig, Backpack, Secure Container and Special Slots remain visible in the main Farming Guide storage area.
- root Rig / Backpack / Secure Container do not open a duplicate internal detail window because their storage is already visible on the main page.
- filled carrier destructive replacement fails closed.
- Secure Container compatibility remains distinct from generic case/container classification.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies a stored item's owning stored carrier.

Only stored **Backpack** and **Rig** items expose an internal detail surface.

- backpack-in-backpack remains supported when current source grids/filters allow it;
- rig-in-backpack/other legal storage remains supported;
- nested detail uses the real source-backed grids and remains interactive for drag/drop;
- nested backpack/rig can itself contain another supported nested backpack/rig when mechanically legal;
- generic cases/containers and ordinary equipment do not expose a user-facing internal detail surface;
- orphan, duplicate, self, cycle, unknown-parent, invalid-grid/filter/bounds/overlap state fails closed;
- moving a carrier preserves descendant parent relationships;
- destructive carrier removal deletes its subtree;
- a carrier cannot be moved into itself or one of its descendants.

### Compact nested detail

The detail view measures the rendered storage grid footprint and adds only bounded title/close chrome. Width/height are clamped to the available viewport. The main storage area remains visible behind the compact detail instead of being covered by a fixed full-column workbench.

### Special Slots

Special Slots are not generic 1×1 inventory grids.

- eligibility uses canonical current Game Content `specialSlot` classification;
- ineligible items cannot be placed there;
- an eligible item occupies exactly one Special Slot regardless of ordinary width/height;
- ordinary storage continues to use the item's normal footprint;
- manual drag/drop, sanitizer, rendering, collision, capacity summary and raid planner share the same policy.

## 4. Raid-session advisor

### Lifecycle

```text
working/preset snapshot + persisted locks
→ Raid Start
→ isolated FarmingGuideRaidSession
→ manual or accepted session-local changes
→ Raid End
→ baseline restored; session discarded
```

- raid-start state is an immutable baseline;
- raid-session mutations do not overwrite the saved preset or ordinary working state;
- manual Farming Guide changes inside a raid become the new session input and advance revision;
- preset selection/deletion stays blocked while the raid session is active;
- raid end restores the baseline.

### Scanner / pending transaction

Scanner owns confirmed Item ID. Farming Guide owns the recommendation.

```text
confirmed Scanner item + scanner-owned price/needed facts
→ current raid snapshot + locks
→ Store / Replace / Discard / top-level Equip / ReplaceEquip proposal
→ one revision-bound pending instruction
→ explicit accept hotkey
→ revision-checked commit
```

- at most one pending recommendation exists;
- a new scan silently rejects an older unaccepted pending without state mutation, then plans the new item against unchanged current raid state;
- manual equipment/storage/lock changes invalidate stale pending advice silently;
- explicit configured accept hotkey is required before recommendation effects commit;
- successful acceptance feedback is `반영 완료`;
- incoming item name is not repeated in the action text;
- search-result hover + `T` runs the same snapshot/planning path, but the presentation expires after a bounded interval and cannot hide a newer real scan.

Current guidance:

- Store: `[보관할 장소]에 보관`
- Replace stored item: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- top-level Equip: `[장착할 장비 칸]에 장착`
- top-level ReplaceEquip: `[장착할 장비 칸]의 [기존 장비]와 교체`

Accepted Store / Replace / Equip / ReplaceEquip contribute to session-local acquired Needed quantity. They do not directly modify authoritative profile inventory.

### Loot priority

Current comparison policy remains isolated in `FarmingGuideLootPriorityPolicy`:

1. item with remaining current-needed quantity;
2. higher effective value per ordinary occupied slot;
3. higher total effective value;
4. smaller ordinary footprint as deterministic tie-breaker.

`EffectiveValue = max(current merchant sell price, current Flea average price, 0)`.

Legal empty placement is preferred over destructive replacement. Special-slot one-cell occupancy is a placement mechanic, not a global item-value footprint redefinition.

## 5. Lock ownership / reserved capacity

`FarmingGuideLockState` keeps EquipmentSlots, Carriers, ItemInstanceIds and ReservedCells.

- item lock protects that exact item instance from automated removal/replacement;
- moving the same stored instance preserves its lock;
- equipment/carrier lock protects the currently equipped target from automated removal/replacement;
- removing/replacing a target removes its target lock;
- carrier lock does not block automatic placement into its ordinary internal storage;
- item lock does not globally block a supported nested carrier's ordinary storage;
- empty-cell lock is an independent one-cell reserved-capacity constraint and persists until explicitly unlocked;
- direct user drag/drop is authoritative and not blocked by automation locks;
- F lock toggle updates local state/visual without intentionally rebuilding the full page;
- later full rerenders reapply valid lock highlights from state.

## 6. Persistence

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
Farming Guide state schema: v2
readable: v1-v2
```

v1.15.2 does not require a schema bump. Legacy equipment-internal state can be read but is discarded during current complete-equipment normalization. Preset names, top-level equipment, storage placement, nested backpack/rig relationships and locks remain preserved when valid.

## 7. Game Content

Lifecycle:

```text
remote source
→ parse/import
→ schema/semantic validation
→ canonical candidate
→ completeness/LKG guard
→ DB/read-back/integrity validation
→ atomic active replacement
```

Unknown structural or semantic drift fails closed. User progress and reviewed Ground Truth are not rewritten by content activation.

Current Content compatibility:

```text
write: v10
read: v3-v10
```

Farming Guide content may preserve item dimensions, storage grids/filters, `specialSlot`, equipment compatibility/conflicts, attachment/armor source metadata, default-preset membership/images and storage layout identity. Only the product-supported subset becomes editable runtime state.

## 8. Scanner

Recognition contract:

```text
screen pixels
→ structural validation
→ OCR
→ conservative current-catalog match
→ optional strict visual corroboration
→ Item ID or fail closed
```

- false positive is worse than miss;
- current catalog is identity authority;
- price/needed/source/relationships are not identity proof;
- reviewed actual Tarkov evidence is required before relaxing recognition acceptance;
- Ground Truth is explicit user-reviewed truth only;
- Scanner uses external screen pixels + OCR only and does not use game memory read, injection, game/process hooks, kernel/driver access, input automation, network manipulation or anti-cheat bypass;
- Needed quantity/source presentation reuses `ItemsWorkspace.Plan.NeededItems` authority.

## 9. Quest / Hideout / Needed Items

- exact ProfileVariable values are authoritative over compatibility inference;
- audited staged task-pool compatibility is bounded and structural drift fails closed;
- Future Needed Items / cleanup remain conservative;
- flexible future requirements are protected when exact candidate consumption is unknown;
- Hideout `foundInRaid` semantics are preserved;
- deterministic mandatory-material ledger prevents double consumption and supports rollback.

## 10. Items / Ammo

Items combines canonical content, profile, inventory and Needed Items for browsing/presentation.

Ammo remains read-only comparison plus profile-aware pickup judgment:

- same-caliber penetration comparison;
- only proven current direct purchase is treated as direct purchase;
- flea/barter/craft/higher LL/unproven quest unlock is not promoted to direct purchase;
- authoritative Ammo Pack `containsItems` relation is preferred.

## 11. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper owns integration, settings and lifecycle. Maintained regression coverage includes map-selection synchronization, position/heading transform consistency, PMC/Scav/Transit extract filtering, standard-marker recovery and isolated Player Marker Size behavior.

## 12. Program Update / release immutability

- latest public stable GitHub release is updater authority;
- update is user-consented;
- ZIP/checksum are verified before replacement;
- mutable user data under `%LocalAppData%/JunhyunHelper` is outside program replacement;
- Release workflow consumes the exact-main CI artifact;
- a published version's source/tag/assets are immutable and later documentation-only commits cannot replace them.

## 13. Validation gates

v1.15.2 exact product source `f4974ee6bed5047865581240197f7f0e2787ba7c` passed all applicable automated gates:

- deterministic tests: 562 / 0 / 0;
- Windows Release build / XAML compile;
- self-contained win-x64 publish;
- actual published EXE Product UI / Farming Guide / Map / Scanner smoke;
- normal graceful shutdown;
- active-async Shutdown Race;
- portable root / forbidden dependency audit;
- release package/checksum verification;
- PR + exact-main Documentation Consistency;
- public tag/release/assets/latest readback.

## 14. Current schemas / remaining external evidence

```text
Desktop: 1.15.2
Content: write v10 / read v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog: write v4 / read v1-v4
```

Automated release verification is complete. Separate real-environment evidence remains pending:

- 사용자 actual-PC/Tarkov play validation for v1.15.2 Farming Guide presentation/behavior;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis when that diagnostic work resumes.

Current release / decision evidence:

- `docs/RELEASE_NOTES_V1.15.2.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
