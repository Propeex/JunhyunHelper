# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다. Runtime GPT/AI 의존성은 없다.

현재 public stable은 **v1.15.1**이다.

```text
exact product source/tag target:
821def285e2b4964242b50981f6ba6245e996057
release id: 380252024
published UTC: 2026-09-01T06:15:51Z
558 passed / 0 failed / 0 skipped
```

Validation:

```text
validated PR head: e78ca34c272ac40b8f7c6a4bfcefede59adb9d59
PR CI: 33476320371 — SUCCESS
PR Shutdown Race: 33476320367 — SUCCESS
PR Documentation Consistency: 33476320491 — SUCCESS
merge PR: #259
exact-main CI: 33476586723 — SUCCESS
exact-main Shutdown Race: 33476586808 — SUCCESS
exact-main Documentation Consistency: 33476586819 — SUCCESS
Release workflow: 33476812315 — SUCCESS
```

PR #258 carried the implementation as a Draft. The GitHub connector's draft-to-ready GraphQL mutation failed due to a connector-side schema incompatibility, so the same branch/head was reopened as non-draft PR #259. An earlier full smoke attempt on the same implementation hit a transient Factory-map visibility timeout while the process remained responsive; rerunning the same HEAD succeeded, and PR #259 plus exact-main validation subsequently passed the full smoke pipeline.

Public package:

```text
Junhyun-Helper.zip
asset id: 539091025
bytes: 80,658,918
SHA-256: 80283d9dfc294d195d644ab12ac074b5d4698f4e500475d7435680ccb6e4fc0a

SHA256SUMS.txt
asset id: 539091026
bytes: 86
asset SHA-256: 906bde7d2c5a6e7234b3de1c21ba935c39522af84fe9f6fda352738457fb91d9
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9788440065
bytes: 241,908,886
SHA-256: e865fb395dcca353788495bbfb84f860129b39bdc6e89b51780d99db481592b8
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.1` resolve to `821def285e2b4964242b50981f6ba6245e996057`. Follow-up documentation-only commits are not v1.15.1 product sources and may not replace its public assets.

## 2. Farming Guide raid-session advisor — v1.15.1 current contract

v1.15.0 introduced the user-controlled raid-session recommendation layer. v1.15.1 supersedes the first real-play behaviors documented in `DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md` while preserving the same safety and session architecture.

### Raid-session lifecycle

- `레이드 시작` snapshots the current working equipment/storage/lock state into an isolated raid session.
- Raid-session mutations do not overwrite the saved preset or original working state.
- Manual Farming Guide changes during a raid immediately become the new recommendation input state.
- `레이드 종료` discards raid-session mutations and restores the raid-start snapshot.
- Session revision remains the stale-write guard for pending recommendations.

### Scanner / Mini Scanner bridge

- Scanner owns confirmed Item ID; Farming Guide owns decision/planning.
- Scanner worker callbacks cross a WPF Dispatcher boundary before Farming Guide UI/state interaction.
- At most one pending recommendation exists at a time.
- A new scan silently rejects the previous unaccepted pending recommendation without state mutation, then plans the new item against unchanged current raid state.
- Manual equipment/storage/lock edits invalidate pending advice silently.
- Explicit configured Farming Guide accept hotkey is still required before recommendation effects commit.
- Accepted feedback is `반영 완료`.
- Mini Scanner guidance is action-only and does not repeat the scanned item name.
- Search-result hover + `T` produces a simulated scan through the exact same snapshot/planning path, but its presentation expires after a bounded interval and cannot hide a newer real scan.

Current guidance classes:

- Store: `[보관할 장소]에 보관`
- Replace stored item: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- Equip: `[장착할 장소]에 장착`
- Replace equipped/attached item: `[장착할 장소]의 [기존 아이템]과 교체`

### Equip / replace-equip

Raid planning evaluates legal empty equipment targets as well as storage surfaces.

Supported target classes include:

- PMC equipment slots;
- rig/backpack/secure-container carrier equipment slots;
- recursive weapon/helmet attachment/mod slots;
- replaceable armor-plate slots.

If no legal empty target exists, unlocked lower-priority equipped/attached items may be replacement candidates when current compatibility/conflict rules permit replacement. Accepted Store, Replace, Equip and ReplaceEquip actions all contribute to raid-acquired Needed quantity.

### Lock ownership / reserved capacity

`FarmingGuideLockState` keeps four constraint classes: EquipmentSlots, Carriers, ItemInstanceIds and ReservedCells.

Current semantics:

- item lock protects that item from automated removal/replacement; moving the same instance preserves its lock;
- equipment/carrier lock protects the currently equipped target from automated removal/replacement;
- removing/replacing the locked target removes that target lock;
- empty-cell lock is an independent reserved-space constraint and persists until explicitly unlocked;
- locking a rig, backpack or secure container does **not** lock its internal inventory surface;
- item locking does not globally disable a nested carrier's ordinary storage grids;
- direct user drag/drop remains authoritative and is not blocked by automation locks;
- F lock toggles update the affected state/visual without rebuilding the full page, while full rerenders reapply lock visuals from state.

### Special Slots

Special Slots are not generic 1×1 inventory grids.

- eligibility uses canonical current Game Content `specialSlot` classification;
- ineligible items cannot be placed there;
- an eligible item occupies exactly one special slot regardless of ordinary width/height;
- normal inventory/storage continues to use the item's ordinary footprint;
- manual drag/drop, sanitizer, rendering, collision, capacity summary and raid planner use the same special-slot policy;
- nested ordinary storage inside a special-slot item remains ordinary storage.

### Current loot priority

The recommendation engine remains separated from Scanner identity and placement mechanics.

Comparison policy:

1. remaining current-needed quantity;
2. higher effective value per ordinary occupied slot;
3. higher total effective value;
4. smaller ordinary footprint as deterministic tie-breaker.

`EffectiveValue = max(current merchant sell price, current Flea average price, 0)`.

Placement-context mechanics such as special-slot one-cell occupancy are handled by storage policy; the global loot policy does not redefine Tarkov compatibility.

The product does not infer extraction probability or tell the user whether to leave the raid. Recommendations remain advisory until explicit acceptance.

## 3. Farming Guide base editor contract

Farming Guide retains all v1.14.x loadout/editor behavior plus v1.15.x raid-session behavior.

### Equipment / storage

- ordinary item footprint uses current Tarkov width/height;
- storage legality uses current validated grids, filters, bounds and item dimensions;
- Special Slots use the v1.15.1 one-slot special policy above;
- drag supports rotation, bounded snap, bounds/overlap/filter/contiguous-space validation;
- Secure Container classification is distinct from generic case/container classification;
- profile-aware pocket geometry is resolved centrally;
- filled carrier destructive replacement fails closed;
- pistol/holster presentation is below eyewear;
- storage helper text is `R: 회전 · F: 아이템/장비/빈 칸 잠금`.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies the owning stored carrier. Root items have null parent. Load/sanitize accepts the root→parent tree in order and rejects duplicate IDs, orphan/self/cycle relationships, unknown parents, invalid grids, filters, bounds and overlap. Carrier moves preserve descendants; destructive removal deletes the subtree; a carrier cannot be moved into itself/its descendant.

### Assembly / workbench

- the same-page workbench exposes actionable storage, attachment/mod and replaceable armor slots;
- recursive navigation supports attachment child slots;
- empty actionable slots can open an inline compatible-item icon picker;
- picker, manual drag/drop and raid equip planning share Core compatibility/conflict authority;
- occupied one-item slots are never silently overwritten;
- required-slot and conflict validation recurse through the assembly tree;
- impossible persisted assembly state is sanitized fail closed;
- raw source slot identifiers are translated to user-facing Korean labels where a known semantic label exists.

### Search / visual presentation

- assembled `ItemPropertiesPreset` / `preset` weapon records are excluded from Farming Guide draggable search while canonical base weapons remain;
- changed weapon/helmet composed imagery is used only when canonical content contains an authoritative image whose exact contained-item signature matches the current assembly;
- unsupported arbitrary assemblies retain safe base/part presentation rather than fabricated composites;
- product-owned exact storage coordinates are visual metadata only and require exact layout identity, grid count and per-index width/height signature; otherwise compact fallback is used.

### Persistence

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
Farming Guide state schema: v2
readable Farming Guide state: v1-v2
```

Game Content and user-owned Farming Guide state have separate lifecycles. Program/Game Content updates do not overwrite user state.

## 4. Game Content

Lifecycle:

```text
remote source
→ parse/import
→ schema/semantic validation
→ canonical candidate
→ completeness/LKG guard
→ candidate DB/read-back/integrity validation
→ atomic active replacement
```

Unknown structural/semantic drift fails closed. Optional enrichment can fail soft only within its own boundary. User progress and reviewed Ground Truth are never rewritten by content activation.

Current Content compatibility:

```text
write: v10
read: v3-v10
```

Farming Guide content preserves current item dimensions, grids/filters, `specialSlot` item classification, attachment/armor slots/conflicts, default-preset membership/image and optional storage layout identity.

## 5. Scanner

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
- reviewed actual Tarkov evidence is required before relaxing OCR/matcher/recovery thresholds;
- scan-time network work is not identity proof;
- Ground Truth is explicit user-reviewed truth only.

Scanner uses external screen pixels + OCR only and does not use game memory read, DLL/code injection, process/game hooks, kernel/driver access, input automation, game network manipulation or anti-cheat bypass.

Needed quantity/source presentation reuses `ItemsWorkspace.Plan.NeededItems` rather than creating a second truth. Scanner display settings schema is v10 and preserves v9 settings through migration.

## 6. Quest / Hideout / Needed Items

- exact ProfileVariable values are authoritative over compatibility inference;
- audited staged task-pool compatibility is bounded and structural drift fails closed;
- Future Needed Items / cleanup do not inherit optimistic current-Quest UI compatibility;
- flexible future requirements remain protected when exact candidate consumption is not known;
- Hideout `foundInRaid` source semantics are preserved; non-FIR inventory cannot satisfy FIR requirements;
- consumption ledger prevents double-consumption and supports rollback for deterministic mandatory materials.

## 7. Ammo

- read-only ammo comparison plus profile-aware pickup judgment;
- same-caliber penetration comparison;
- only currently proven direct purchase is treated as current direct purchase;
- flea/barter/craft/higher-LL/unproven quest unlock is not promoted to current direct purchase;
- authoritative Ammo Pack `containsItems` relationship is preferred.

## 8. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper owns product lifecycle/presentation. Maintained regressions include map-selection synchronization, position/heading transform consistency, PMC/Scav/Transit extract filtering, standard-marker recovery, isolated Player Marker Size changes and removal of the Mini Scanner right-click correction menu.

A single v1.15.1 candidate CI attempt hit a Factory floor-presentation smoke timeout while the process was responsive. The identical HEAD succeeded on rerun, the replacement non-draft PR succeeded, and exact-main succeeded. This is recorded as CI timing evidence rather than a product regression.

## 9. Program Update / release immutability

- latest public stable GitHub release is updater authority;
- update is user-consented;
- ZIP/checksum are verified before replacement;
- user data under `%LocalAppData%/JunhyunHelper` is outside program replacement;
- Release workflow consumes the exact-main CI artifact;
- an already-published version's source/tag/assets are immutable and are not replaced by later documentation-only commits.

## 10. Validation gates

Relevant changes use the required subset of:

- deterministic tests;
- Windows Release build / XAML compile;
- self-contained win-x64 publish;
- actual published EXE Product UI / Farming Guide / Map / Scanner runtime smoke;
- exact storage layout / drop-target smoke;
- normal graceful shutdown;
- active-async Shutdown Race;
- portable root / forbidden dependency audit;
- package/checksum verification;
- PR and exact-main Documentation Consistency;
- public tag/release/assets/latest readback.

v1.15.1 exact product source `821def285e2b4964242b50981f6ba6245e996057` passed all applicable automated gates.

## 11. Current schemas / remaining external evidence

```text
Desktop: 1.15.1
Content: write v10 / read v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog: write v4 / read v1-v4
```

Automated release verification is complete. Separate real-environment evidence remains pending:

- further user actual-PC/Tarkov play validation;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis.

Current release evidence:

- `docs/RELEASE_1.15.1.md`
- `docs/.release-v1.15.1-status.json`
- `docs/RELEASE_NOTES_V1.15.1.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
