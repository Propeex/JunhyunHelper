# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다.

현재 public stable은 **v1.15.3**다.

```text
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
release id: 380333729
published UTC: 2026-09-01T08:35:55Z
563 passed / 0 failed / 0 skipped
```

Validation:

```text
validated PR head: db82512e6e723f2d85ed0ddf3f3c7c9b0e3a70af
merge PR: #265
PR CI: 33487099126 — SUCCESS
PR Shutdown Race: 33487099119 — SUCCESS
PR Documentation Consistency: 33487099201 — SUCCESS
exact-main CI: 33487466031 — SUCCESS
exact-main Shutdown Race: 33487466005 — SUCCESS
exact-main Documentation Consistency: 33487465946 — SUCCESS
Release workflow: 33487795730 — SUCCESS
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539249489
bytes: 80,659,355
SHA-256: a22a426de32aa20a4c158018d98a6eec96b39d460d367d33d9d970d7e2581d99

SHA256SUMS.txt
asset id: 539249490
bytes: 86
asset SHA-256: 286e27a9db1394d1a4487c5b26598f08998bb03e07e21fa116dc4fca5844fdde
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9792459273
bytes: 241,909,375
SHA-256: c0aba02d6a465734c841b044776dfcf087bab9b29141b23c71ffb5a0a65c6cb2
```

`/releases/latest`, release target and lightweight `refs/tags/v1.15.3` all resolve to `c35204da66eb0af454b50550c830b071a0897835`. Documentation-only commits after the release are not v1.15.3 product sources.

## 2. Farming Guide complete-equipment boundary

v1.15.2's complete-equipment decision remains current in v1.15.3.

Weapons, helmets, body armor and other equipment are opaque complete items in Farming Guide.

- weapon/helmet attachment editing is not exposed;
- armor-plate editing is not exposed;
- equipment-internal drag/drop and raid Equip/ReplaceEquip targets are not generated;
- persisted legacy attachment/armor state remains readable for compatibility but current runtime normalizes it to root-only equipment state;
- legal top-level equipment targets remain available;
- source attachment/default-preset metadata may still be used as read-only evidence for authoritative complete-item presentation.

## 3. Source-backed nested storage — v1.15.3

v1.15.3 supersedes only the v1.15.2 restriction that nested storage detail was limited to stored Backpack/Rig items.

`FarmingGuideStoredItemState.ParentInstanceId` remains the canonical nested-storage address. A stored item supports nested storage when current validated Game Content exposes one or more real `StorageGrids`.

- container names such as Key tool are not product allowlists;
- each source grid preserves width/height and allowed/excluded category/item filters;
- manual drag/drop, sanitizer and raid planning share the same storage-filter policy;
- supported containers may remain nested inside Secure Container or another legal storage surface;
- arbitrary legal depth is handled through the existing parent-instance chain;
- orphan, duplicate, self-parent, cycle, bad grid, filter failure, bounds failure and overlap fail closed;
- root Rig/Backpack/Secure Container storage stays on the main Farming Guide surface;
- compact nested detail remains interactive and sized from the actual rendered grid footprint.

### Dedicated storage priority

A nested grid is a dedicated candidate only when it contains a positive source allow-list (`AllowedItemIds` or `AllowedCategoryIds`) and that same filter accepts the incoming item.

For non-destructive empty placement, compatible dedicated nested storage is evaluated before general root Secure Container, Pockets, Rig and Backpack space. Generic/unrestricted nested storage retains the established general ordering and does not receive this priority.

This lets a permitted key use a key-oriented container before consuming general Secure Container cells without hardcoding the container or key name.

## 4. Lock presentation

Stored-item lock presentation now has one unambiguous visual contract:

- ordinary unlocked stored item: neutral border;
- `F`-locked stored item: accent/yellow border;
- unlocking: neutral border restored;
- reserved empty-cell and equipment/carrier lock behavior remains unchanged.

Locks continue to constrain automatic removal/replacement rather than direct user editing.

## 5. Scanner-driven Farming Guide / simulated scan

Scanner remains the authority for confirmed item identity and Scanner-owned presentation facts. Farming Guide owns the recommendation transaction.

```text
confirmed item snapshot
→ current raid snapshot + locks
→ Store / Replace / Discard / top-level Equip / ReplaceEquip proposal
→ revision-bound pending instruction
→ explicit acceptance
→ revision-checked commit
```

Search-result hover + `T` is a product test input for that same path.

- hovered concrete result takes precedence even if Search TextBox still has keyboard focus;
- without a hovered result, `T` remains normal search input;
- active raid session receives the simulated snapshot through the same recommendation handler used by a normal confirmed scan;
- Scanner capture mode may be disabled;
- if same-mode Scanner catalog data is not loaded in memory after restart, verified local catalog data may be loaded on demand;
- snapshot preparation failure is surfaced rather than silently ignored;
- temporary simulated presentation cannot overwrite a newer real scan.

## 6. Raid-session advisor retained contracts

- raid start snapshots working/preset equipment, storage and locks into an isolated session;
- raid end discards session changes and restores the baseline;
- at most one pending recommendation exists;
- a newer scan rejects an older unaccepted pending without mutating state;
- manual equipment/storage/lock edits invalidate stale pending advice;
- explicit configured acceptance is required before commit;
- successful feedback is `반영 완료`;
- Special Slots continue to use canonical current `specialSlot` classification and one-cell special occupancy;
- accepted Store/Replace/top-level Equip/ReplaceEquip affects only session-local acquired Needed quantity, not authoritative profile inventory.

Current action wording:

- Store: `[보관할 장소]에 보관`
- Replace stored item: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- top-level Equip: `[장착할 장비 칸]에 장착`
- top-level ReplaceEquip: `[장착할 장비 칸]의 [기존 장비]와 교체`

## 7. Verification evidence

The final product source `c35204da66eb0af454b50550c830b071a0897835` passed:

- Release/XAML desktop build;
- 563 deterministic tests with zero failure/skip;
- self-contained win-x64 publish;
- actual published EXE startup and product UI runtime smoke;
- Farming Guide source-backed specialized container filter smoke;
- neutral → locked accent → neutral stored-item border smoke;
- compatible dedicated nested storage priority smoke;
- graceful shutdown;
- active-async Shutdown Race;
- release package/checksum verification;
- exact-main documentation consistency;
- public release/tag/latest/asset readback.

The public ZIP byte count and digest are identical to the exact-main package.

## 8. Schema / compatibility

```text
Desktop: 1.15.3
Public stable: 1.15.3
Content write: v10
Content readable: v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.3 does not bump persistent schemas. Existing presets, top-level equipment, stored placements, recursive parent IDs and lock state remain readable.

## 9. Canonical references

- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/RELEASE_1.15.3.md`
- `docs/RELEASE_NOTES_V1.15.3.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 10. External evidence still pending

Automated release validation is complete. Separate real-environment evidence remains open:

- user actual-PC/Tarkov play validation of v1.15.3 Farming Guide visuals/behavior;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis when that maintenance work resumes.
