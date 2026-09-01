# ARCHITECTURE — Farming Guide

기준일: **2026-09-01 KST**  
대상 제품: **v1.14.1+**

이 문서는 `파밍 가이드` subsystem의 책임, authority, state, assembly editing, storage presentation, Tarkov 변화 대응 및 검증 경계를 정의한다.

관련 authority:

- `docs/PRODUCT.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- current facts: `docs/PROJECT_STATE.json`

## 1. 목적 / 비목표

Farming Guide is a **raid-start Loadout / Inventory Editor**.

목적:

- 레이드 출발 시점 장비와 수납 상태 구성
- current Tarkov item dimensions/grids/filters/slots 조작
- nested storage와 recursive assembly를 deterministic state로 유지
- game structure drift 시 impossible/stale state fail closed

비목표:

- actual live raid inventory mirror
- memory/packet/injection 기반 동기화
- loot pickup/discard recommendation
- Scanner real-time recommendation
- arbitrary build의 Tarkov client 완전 동일 렌더링
- 검증되지 않은 visual coordinates를 authentic layout으로 추측

## 2. 계층 책임

```text
JunhyunHelper.Desktop
  ├─ equipment/storage presentation
  ├─ drag/drop + geometry probing
  ├─ recursive in-page workbench
  ├─ inline compatible-item picker
  ├─ assembly-aware image presentation
  ├─ storage visual layout rendering
  ├─ preset UI
  └─ published-runtime smoke

JunhyunHelper.Core
  ├─ state models / ParentInstanceId
  ├─ equipment compatibility
  ├─ grid placement / packing rules
  ├─ FarmingGuideAssemblyPolicy
  ├─ FarmingGuideStorageVisualLayoutResolver
  ├─ search policy
  ├─ pocket geometry
  └─ persisted-state sanitization

JunhyunHelper.Infrastructure
  ├─ Tarkov item import
  ├─ assembly/default-preset source import
  ├─ StorageLayoutName import
  ├─ Content v10 persistence / v3-v10 read
  └─ Farming Guide user-state persistence
```

WPF handler가 compatibility/state truth를 따로 재구현하지 않는다.

## 3. Authority split

### Mechanical storage authority

Current validated Game Content:

- grid count / width / height
- allowed/excluded filters
- item footprint
- attachment/armor slots and conflicts
- actual placement legality

### Visual arrangement authority

Product-owned verified metadata:

- layout identity mapping
- per-grid visual X/Y coordinates
- **per-grid expected Width/Height signature**

Visual metadata is presentation-only and may never create/change storage mechanics.

### User state

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

Stores working loadout, presets, equipment/assembly trees, storage placement, rotation and nested parent instance relationships. Game Content lifecycle is separate.

## 4. Nested storage

`FarmingGuideStoredItemState.ParentInstanceId` identifies a stored carrier parent.

- null = root/top-level surface
- non-null = exact parent stored instance

Sanitization validates root→accepted-parent order and rejects duplicate instance IDs, orphan/self/cycle relationships, missing/invalid parent layouts, invalid grid indexes, filters, bounds and overlap.

Movement/deletion rules:

- carrier instance identity and descendant chain are preserved on movement;
- self/descendant containment is forbidden;
- destructive deletion/removal deletes the subtree;
- contents-filled carrier silent replacement is prohibited.

## 5. Assembly policy

`FarmingGuideAssemblyPolicy` is the Core authority for:

- deep node lookup/mutation
- attachment filters
- armor allowed-item rules
- item/assembly conflicts
- required-slot recursion
- bounded recursive traversal
- deterministic assembly signature
- persisted-tree sanitization

Desktop workbench and inline picker consume this policy; they do not invent relaxed UI-only compatibility.

## 6. Workbench / picker

The workbench is in-page, not a generic OS configuration window.

- storage carrier → actual grids
- weapon → attachment/mod slots
- helmet/body armor → attachment/replaceable armor slots
- installed child can be navigated recursively to its child slots
- empty actionable slot can open compatible-item icon picker
- picker click installs using the same Core validation as drag/drop
- occupied one-item target is never silently overwritten
- owner-item movement closes the workbench before drag to prevent stale callback writes

## 7. Assembly-aware presentation

Authoritative composed preset image is used only when:

- a usable imported default-preset image exists; and
- current assembly membership exactly matches authoritative preset membership.

Arbitrary assemblies use a deterministic base-image + installed-part indication fallback. The fallback is not claimed to be the Tarkov client's exact composition renderer.

## 8. Exact storage visual-layout resolver

### v1.14.1 profile shape

Each verified visual grid contains:

```text
X / Y
ExpectedWidth / ExpectedHeight
```

Exact activation requires:

1. verified profile by layout identity or explicit verified item alias;
2. exact live/profile grid count;
3. positive live dimensions;
4. exact per-index live Width/Height == ExpectedWidth/ExpectedHeight;
5. finite transformed positions/bounds;
6. no resulting rectangle overlap.

Any failure returns `false`; Desktop uses finite compact fallback.

### Why v1.14.1 exists

v1.14.0 source stored coordinates only. Its resolver checked identity/count/positive dimensions/non-overlap but not expected per-index dimensions. Thus a dimension-only source drift could retain stale exact coordinates when non-overlapping. v1.14.1 adds the missing mechanical signature and is the current authority for exact layout activation.

### Verified minimal catalog

The product deliberately keeps a small, provenance-reviewed set of exact profiles/aliases rather than copying an unverified atlas wholesale. Unknown carriers use compact fallback.

## 9. Search / equipment compatibility

- `ItemPropertiesPreset` / `preset` assembled weapon records are excluded only from Farming Guide draggable search; canonical content records remain intact.
- pistol/revolver/handgun semantics target Holster rather than generic primary weapon slots.
- Secure Container uses explicit secure/pouch semantics and narrow fallback; generic case/container is not accepted as secure equipment.
- pocket geometry is resolved centrally from current profile facts and consumed by presentation + state sanitization.

## 10. Content / schema

Game Content v10 preserves the structures needed for Farming Guide assembly/layout:

- dimensions
- storage grids and filters
- attachment/armor slots and conflicts
- default preset reference / preset item membership / composed image URL
- optional `StorageLayoutName` sourced from known upstream layout-name fields

Compatibility:

```text
Content write: v10
Content read: v3-v10
Farming Guide user state: v1
```

Old readable snapshots that lack a newer structure are not enriched by guesswork.

## 11. Runtime validation

Deterministic tests cover placement, nested storage, compatibility, assembly recursion, state persistence, search policy, importer layout identity and visual-layout resolver behavior.

Published EXE smoke verifies actual WPF/runtime behavior including:

- Farming Guide positive geometry/render lifecycle
- nested storage / workbench interaction contracts
- exact multi-grid Canvas rendering
- expected grid positions
- `GridDropTarget.GridIndex` and parent-instance identity
- A18 fixture using the verified v1.14.1 grid dimension signature

v1.14.1 exact source `add12c1b160f54e494d549978073f25e27cc4191` passed 529 deterministic tests plus the actual published EXE smoke and release gates documented in `docs/RELEASE_1.14.1.md`.

## 12. Change discipline

When Tarkov storage or assembly data changes:

1. treat current validated Game Content mechanics as authority;
2. fail closed rather than retain impossible state;
3. exact visual profiles must be revalidated against the complete grid signature;
4. visual metadata must never override mechanics;
5. add deterministic regression for the changed contract;
6. user-visible rendering changes require actual published EXE smoke.
