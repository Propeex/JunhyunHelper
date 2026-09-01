# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다. Runtime GPT/AI 의존성은 없다.

현재 public stable은 **v1.15.0**이다.

```text
exact product source/tag target:
b974d56f32d073ce21a5de4171737670f83261f3
release id: 380200480
published UTC: 2026-09-01T03:49:49Z
540 passed / 0 failed / 0 skipped
```

Validation:

```text
validated candidate head: 397c82b8911597128c5878e7974db6a7822888d8
candidate CI: 33466090956 — SUCCESS
candidate Shutdown Race: 33466090958 — SUCCESS
candidate Documentation Consistency: 33466090940 — SUCCESS
merge PR: #256
exact-main CI: 33467376556 — SUCCESS
exact-main Shutdown Race: 33467376508 — SUCCESS
exact-main Documentation Consistency: 33467376529 — SUCCESS
Release workflow: 33467575493 — SUCCESS
```

PR #255 carried the original Draft implementation and completed candidate validation. The GitHub connector's draft-to-ready GraphQL mutation failed because of a connector-side schema incompatibility, so the exact same validated branch/head was reopened as non-draft PR #256 and squash-merged without changing product contents.

Public package:

```text
Junhyun-Helper.zip
asset id: 538909239
bytes: 80,647,419
SHA-256: 95f62c7d795f1954c3fd3437b17d9e15db05f5ab113f95df97055d15061bc76a

SHA256SUMS.txt
asset id: 538909237
bytes: 86
asset SHA-256: 5b8101bf0e086952ee12d4070e678cd1e0b5406e0c32ae91b7bf2562e7ab2ecb
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9785383239
bytes: 241,875,746
SHA-256: 6ba4c5819119a230ee02e4f7c2cb093679527623e3ab9665b8ebc05dee5936ae
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.0` resolve to `b974d56f32d073ce21a5de4171737670f83261f3`. Follow-up documentation-only commits are not v1.15.0 product sources and may not replace its public assets.

## 2. v1.15.0 Farming Guide raid-session advisor

v1.15.0 extends the existing Farming Guide raid-start Loadout / Inventory Editor with a user-controlled raid-session recommendation layer.

### Raid-session lifecycle

- `레이드 시작` snapshots the current working equipment/storage/lock state into an isolated raid session.
- Raid-session mutations do not overwrite the saved preset or original working state.
- Manual Farming Guide changes during a raid immediately become the new recommendation input state.
- `레이드 종료` discards raid-session mutations and restores the raid-start snapshot.
- A monotonically changing session revision invalidates stale pending recommendations after state changes.

### Scanner / Mini Scanner bridge

- recognized Scanner item identity is bridged to Farming Guide through a UI Dispatcher boundary; Scanner worker callbacks do not directly mutate WPF Farming Guide state;
- at most one unaccepted Farming Guide instruction is active;
- Mini Scanner can persistently show the current Farming Guide instruction;
- the configured Farming Guide accept hotkey is required before recommendation effects are committed;
- acceptance produces transient completion feedback;
- manual equipment/storage/lock edits cancel stale pending instructions and clear the Mini Scanner persistent instruction;
- hovering a Farming Guide search result and pressing `T` produces a simulated Scanner input through the same recommendation pipeline.

### Locks / reserved capacity

- hover + `F` toggles lock state for supported item/equipment/storage/cell targets;
- locked items are excluded from automatic sacrifice/replacement;
- locked carriers and their contents are excluded from automatic placement/replacement;
- locked empty cells are reserved capacity unavailable to automatic placement;
- locks persist in Farming Guide state schema v2 and v1 remains readable/migratable;
- locks do not prohibit the user's own direct drag/drop edits.

### Current loot policy

The recommendation engine is deliberately separated from session/UI/scanner plumbing so policy can evolve independently.

Current v1.15.0 policy uses existing JunhyunHelper truth instead of introducing a second item-need database:

1. remaining required quantity from the existing Needed Items plan;
2. current merchant sell / Flea average economic values available to Scanner;
3. occupied cell count and value per cell;
4. total value and deterministic size tie-breaking;
5. legal destination candidates from current validated Farming Guide storage/filter state;
6. replacement search only among non-protected eligible raid-session loot.

The product does not infer extraction probability or tell the user whether to leave the raid. Recommendations remain advisory until explicit acceptance.

## 3. Farming Guide base editor contract

Farming Guide retains all v1.14.x loadout/editor behavior.

### Equipment / storage

- item footprint uses current Tarkov width/height;
- storage legality uses current validated grids, filters, bounds and item dimensions;
- drag supports rotation, bounded snap, bounds/overlap/filter/contiguous-space validation;
- Secure Container classification is distinct from generic case/container classification;
- profile-aware pocket geometry is resolved centrally;
- filled carrier destructive replacement fails closed.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies the owning stored container. Root items have null parent. Load/sanitize accepts the root→parent tree in order and rejects duplicate IDs, orphan/self/cycle relationships, unknown parents, invalid grids, filters, bounds and overlap. Container moves preserve descendants; destructive removal deletes the subtree; a container cannot be moved into itself/its descendant.

### Assembly / workbench

- the same-page workbench exposes actionable storage, attachment/mod and replaceable armor slots;
- recursive navigation supports attachment child slots;
- empty actionable slots can open an inline compatible-item icon picker;
- picker and drag/drop share `FarmingGuideAssemblyPolicy` compatibility/conflict rules;
- occupied one-item slots are never silently overwritten;
- required-slot and conflict validation recurse through the assembly tree;
- impossible persisted assembly state is sanitized fail closed.

### Search / visual presentation

- assembled `ItemPropertiesPreset` / `preset` weapon records are excluded from Farming Guide draggable search while canonical base weapons remain;
- exact preset composed image is used only when current build membership exactly matches authoritative imported preset membership;
- arbitrary builds use deterministic fallback presentation;
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

Farming Guide v10 content preserves item structure needed for storage/assembly, default-preset source and optional storage layout identity.

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

## 9. Program Update / release immutability

- latest public stable GitHub release is update authority;
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

v1.15.0 exact product source passed all applicable automated gates.

## 11. Current schemas / remaining external evidence

```text
Desktop: 1.15.0
Content: write v10 / read v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog: write v4 / read v1-v4
```

Automated release verification is complete. Separate real-environment evidence remains pending:

- user's actual PC/Tarkov play validation;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis.

Current release evidence: `docs/RELEASE_1.15.0.md` and `docs/.release-v1.15.0-status.json`.
