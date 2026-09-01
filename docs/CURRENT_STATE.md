# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.5 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.5
exact product source/tag target:
62466a957a7e32a623a0ffcfad96bfb16504f823
validated PR head: 2d9f01da32e3e80860c5a87b2d2e73bc87c31b17
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
JunhyunHelper-win-x64
artifact id: 9805674187
bytes: 242,052,034
SHA-256: 6281d8f2ef0f5ab0d0b6414b6cded95852f9006d23806527c8467badb8bfc088
```

`/releases/latest`, release target and `refs/tags/v1.15.5` all resolve to `62466a957a7e32a623a0ffcfad96bfb16504f823`. The public release is `draft=false`, `prerelease=false`.

## Farming Guide current contract

### Complete equipment

Weapons, helmets, body armor and other equipment remain opaque complete items. Weapon/helmet attachment editing and armor-plate editing are not exposed. Supported top-level Equip/ReplaceEquip remains available.

### Source-backed nested storage

`ParentInstanceId` is the nested-storage address. A stored item may expose interactive internal storage whenever current validated Game Content contains real `StorageGrids` for it.

- no Key tool/case name allowlist;
- source grid dimensions and allowed/excluded category/item filters are authoritative;
- specialized containers may be recursively nested where legal;
- dedicated positive-allow-list storage is preferred over general root storage;
- orphan/duplicate/self/cycle/filter/bounds/overlap state fails closed;
- a physically fitting nested Workbench disables both scroll axes and exposes the full grid without clipped bottom cells.

### Preservation-first state transition — v1.15.5

Recommendation construction preserves the v1.15.4 safety order while extending equipment replacement into a complete-state transition:

1. legal empty equipment target;
2. objectively proven equipment upgrade;
3. direct legal storage;
4. non-destructive global repacking;
5. preserve displaced equipment/carriers through legal storage or nesting;
6. bounded value-aware eviction + repacking only when the retained state is provably preferable;
7. discard only when no better legal state exists.

Displaced equipment is loot, not implicit deletion. A removed rig/backpack may be stored in another legal surface and its own storage grids may participate in the same proposed snapshot. Locks, reserved cells, source filters, dedicated-container preference, nested graph validity and complete-equipment boundaries remain enforced.

Needed truth is derived from `current snapshot count - raid baseline count`, so a Needed item that was acquired and later discarded becomes needed again.

### Compact raid presentation — v1.15.5

Primary instructions use compact action vocabulary: `[장비 위치] 장착`, `방탄복 교체`, `헤드셋 교체`, `[장비 위치] 교체`, `방탄 리그 전환`, `[보관 위치] 보관`, `[보관 위치] [기존 아이템] 버리고 보관`, `버리기`.

Same visible storage-area X/Y/grid/rotation repacking is intentionally silent. Only actual cross-area moves or removals are appended as `+ [아이템] 이동 [위치]` / `+ [아이템] 버리기`; multiple operations are comma-separated. Presentation does not alter planner Action or ProposedSnapshot.

## Schema

```text
Desktop: 1.15.5
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## Canonical references

- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/DECISION_V1.15.4_FARMING_GUIDE_REPACKING_EQUIPMENT_UPGRADES.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_PRESENTATION_VIEWPORT.md`
- `docs/DECISION_V1.15.5_FARMING_GUIDE_STATE_TRANSITION_PLANNER.md`
- `docs/RELEASE_1.15.5.md`
- `docs/RELEASE_NOTES_V1.15.5.md`
- `docs/.release-v1.15.5-status.json`

## External validation still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.15.5 release identity above.
