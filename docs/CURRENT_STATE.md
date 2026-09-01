# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.4
exact product source/tag target:
c27daf2177b643ee16d4a3d5b0997e54a267c2c7
validated PR head: da9e788a8494734149cfa0e65eff3535e14d2bac
merge PR: #268
PR CI / Shutdown / Docs:
33500484624 / 33500484673 / 33500484510 — SUCCESS
exact-main CI / Shutdown / Docs:
33500904378 / 33500904396 / 33500904356 — SUCCESS
Release workflow: 33501233130 — SUCCESS
release id: 380429049
published UTC: 2026-09-01T11:12:15Z
585 passed / 0 failed / 0 skipped
```

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
JunhyunHelper-win-x64
artifact id: 9797756949
bytes: 242,014,938
SHA-256: 2ab185334c441dfa44f8d1afb774e7c7c6815df07849563ba865210a9b5857bb
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.4` all resolve to `c27daf2177b643ee16d4a3d5b0997e54a267c2c7`. The release is `draft=false`, `prerelease=false`. Documentation-only follow-up commits are not v1.15.4 product sources.

## Farming Guide current contract

### Complete equipment

Weapons, helmets, body armor and other equipment remain opaque complete items.

- no weapon/helmet attachment editor;
- no armor-plate editor or per-plate raid-state inference;
- no equipment-internal drag/drop or raid Equip/ReplaceEquip target;
- persisted legacy attachment/armor state remains readable only for compatibility and is normalized to root-only runtime state;
- supported top-level equipment Equip/ReplaceEquip remains available.

### Source-backed nested storage

`ParentInstanceId` is the nested-storage address. A stored item may expose interactive internal storage whenever current validated Game Content contains real `StorageGrids` for it.

- no Key tool/case name allowlist;
- source grid width/height and allowed/excluded category/item filters are authoritative;
- specialized containers inside Secure Container or another legal surface remain recursively addressable;
- positive-allow-list dedicated nested storage that accepts an incoming item is preferred over general root storage;
- orphan/duplicate/self/cycle/filter/bounds/overlap state fails closed;
- nested Workbench avoids horizontal scrolling/cell clipping when its complete surface physically fits the effective viewport.

### Preservation-first raid planning — v1.15.4

Recommendation order is:

1. legal empty equipment target;
2. objectively proven and structurally safe equipment upgrade;
3. direct legal storage without moving existing items;
4. non-destructive legal repacking of unlocked existing items;
5. value/need-based destructive replacement only after preservation options fail;
6. discard only when no preferable legal plan exists.

Repacking is bounded and deterministic. It may move or rotate multiple unlocked items across legal root/nested surfaces while preserving source filters, dedicated-container preference, reserved cells, locks and nested parent/descendant constraints. Populated nested containers are not destructively replaced based only on the parent item's value.

### Equipment upgrades — v1.15.4

Market/trader value is not equipment-performance authority.

- protective equipment: incoming representative source `properties.class` must be strictly higher for a same-target protection upgrade;
- backpack/ordinary rig: source-backed storage capacity must be strictly larger and every modeled current content item must remain legal;
- armored rig -> armored rig: protection class and capacity must both be non-regressing and at least one must strictly improve;
- headset: `distanceModifier` and `distortion` must both be no worse and at least one must strictly improve; trade-offs are not auto-ranked;
- ordinary body armor + ordinary rig -> superior armored rig is one atomic fail-closed pending transaction and must preserve all modeled rig contents legally;
- reverse creation of a missing ordinary rig from one scanned item is never inferred.

### Locks / simulated scan retained

- unlocked stored item: neutral border;
- `F`-locked stored item: accent/yellow border;
- equipment/carrier locks and reserved empty-cell semantics remain unchanged;
- hovered search result + `T` uses the real Farming Guide recommendation path even if Search TextBox retains focus;
- a verified same-mode local Scanner catalog may be loaded on demand when capture is disabled/uninitialized after restart.

## Schema

```text
Desktop: 1.15.4
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v11 persists source-backed Farming Guide `ArmorClass`, `HeadsetDistanceModifier` and `HeadsetDistortion`. Older readable v3-v10 snapshots remain last-known-good offline fallback and may be opportunistically refreshed through the normal transactional Data Update boundary.

## Canonical references

- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`
- `docs/RELEASE_1.15.4.md`
- `docs/RELEASE_NOTES_V1.15.4.md`
- `docs/.release-v1.15.4-status.json`

## External validation still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.15.4 release identity above.
