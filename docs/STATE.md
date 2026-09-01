# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 저장소 문서, 실제 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.14.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 / 공개 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 주요 제품 영역은 Profile/User Progress, Quest, Hideout, Needed Items/Inventory, Items, Ammo, Game Content update, Map/MiniMap, Scanner/Mini Scanner, Farming Guide, diagnostics, Program Update다. Runtime GPT/AI 의존성은 없다.

현재 public stable은 **v1.14.1**이다.

```text
exact product source/tag target:
add12c1b160f54e494d549978073f25e27cc4191
release id: 380147230
published UTC: 2026-09-01T01:01:22Z
529 passed / 0 failed / 0 skipped
```

Validation:

```text
PR #253 final head: 42abdc7945c8f12a26553c6d0386cdadc6e41803
PR CI: 33456589868 — SUCCESS
PR Shutdown Race: 33456589884 — SUCCESS
PR Documentation Consistency: 33456589878 — SUCCESS
exact-main CI: 33456851817 — SUCCESS
exact-main Shutdown Race: 33456851818 — SUCCESS
exact-main Documentation Consistency: 33456851901 — SUCCESS
Release workflow: 33457066723 — SUCCESS
```

Public package:

```text
Junhyun-Helper.zip
asset id: 538731592
bytes: 80,630,913
SHA-256: b1216d9c661be909aee8c4a3f4eeb199b03eae46ba1f91799172bf8fd0074921

SHA256SUMS.txt
asset id: 538731593
bytes: 86
asset SHA-256: a3817550bf8d8ed0813606ddc4ae511d3f989b473cedea8c1e137e9209b7944a
```

Exact-main Actions artifact:

```text
id: 9781796510
bytes: 241,822,850
SHA-256: c55c6da388c078c9cf011b5db35b2797424daa8d59cdd7a7c9ed232acfd97031
```

`/releases/latest`, release target and lightweight `refs/tags/v1.14.1` resolve to the exact product source. Follow-up documentation-only commits are not product sources and may not replace public v1.14.1 assets.

## 2. v1.14.0 / v1.14.1 Farming Guide evolution

v1.14.0 introduced:

- removal of the non-actionable PMC dogtag equipment-board surface while preserving legacy state readability;
- recursive `FarmingGuideAssemblyPolicy` authority for deep attachment/armor child trees;
- same-page compatible-item picker for empty attachment/armor slots;
- shared Core compatibility between picker and search drag/drop;
- authoritative default-preset composed image use only when membership exactly matches;
- deterministic arbitrary-assembly presentation fallback;
- `StorageLayoutName` import and product-owned exact multi-grid coordinates for a deliberately small verified catalog;
- Content snapshot write schema v10 / readable v3-v10.

During v1.14.0 release-closure review, the exact visual-layout activation contract was audited against the product source. The public resolver checked identity, count, positive dimensions and non-overlap, but did not store/compare each grid index's expected width/height. Therefore v1.14.0 must not be described as having a complete dimension-signature guard.

v1.14.1 corrects that gap:

- each exact profile grid stores `X`, `Y`, `ExpectedWidth`, `ExpectedHeight`;
- live grid count must equal profile count;
- each live grid's width/height must exactly equal the expected values for the same index;
- width/height drift fails closed even when the stale rectangles would not overlap;
- finite/positive/non-overlap validation remains;
- mismatch falls back to compact visual arrangement only; it does not alter current Game Content mechanics;
- deterministic and actual published-runtime A18 verification use the verified profile signature.

v1.14.0 public bytes/source/assets remain immutable historical evidence. v1.14.1 supersedes only the incomplete exact-layout activation guard.

## 3. Farming Guide system contract

Farming Guide is a **raid-start Loadout / Inventory Editor**, not a live inventory mirror.

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

- separate generic item-information/configuration windows are not the editing authority;
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
- product-owned exact storage coordinates are visual metadata only and require the complete verified grid signature; otherwise compact fallback is used.

### Persistence

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
Farming Guide state schema: v1
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

Needed quantity/source presentation reuses `ItemsWorkspace.Plan.NeededItems` rather than creating a second truth.

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

v1.14.1 exact product source passed all of these applicable gates.

## 11. Current schemas / remaining external evidence

```text
Desktop: 1.14.1
Content: write v10 / read v3-v10
user.db: v1
Farming Guide state: v1
Scanner display settings: v9
Scanner catalog: write v4 / read v1-v4
```

Automated release verification is complete. Separate real-environment evidence remains pending:

- user's actual PC/Tarkov play validation;
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis.

Current release evidence: `docs/RELEASE_1.14.1.md` and `docs/.release-v1.14.1-status.json`.
