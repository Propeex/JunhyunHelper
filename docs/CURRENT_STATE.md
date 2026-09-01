# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.14.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.14.1
exact product source/tag target:
add12c1b160f54e494d549978073f25e27cc4191
PR #253 final head: 42abdc7945c8f12a26553c6d0386cdadc6e41803
PR CI / Shutdown / Docs: 33456589868 / 33456589884 / 33456589878 — SUCCESS
exact-main CI / Shutdown / Docs: 33456851817 / 33456851818 / 33456851901 — SUCCESS
Release workflow: 33457066723 — SUCCESS
release id: 380147230
published UTC: 2026-09-01T01:01:22Z
529 passed / 0 failed / 0 skipped
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

Exact-main artifact:

```text
JunhyunHelper-win-x64
artifact id: 9781796510
bytes: 241,822,850
SHA-256: c55c6da388c078c9cf011b5db35b2797424daa8d59cdd7a7c9ed232acfd97031
```

`/releases/latest`, release target and `refs/tags/v1.14.1` all resolve to `add12c1b160f54e494d549978073f25e27cc4191`. Public release is `draft=false`, `prerelease=false`.

## v1.14.0 → v1.14.1

v1.14.0 added recursive Farming Guide assembly editing, inline compatible-item selection, assembly-aware presentation, layout identity import and verified multi-grid visual-layout support.

Release-closure review found one implementation gap in v1.14.0: exact layout activation did not compare the expected width/height for every grid index. A dimension-only Tarkov drift could keep stale exact coordinates if the rectangles still did not overlap.

v1.14.1 is the corrective PATCH:

- exact profile stores coordinates plus expected width/height per grid index;
- exact layout requires exact layout identity, grid count and per-index width/height match;
- dimension mismatch fails closed to finite compact presentation;
- current Game Content remains the sole authority for storage legality/filter/item footprint;
- non-overlap remains secondary profile-corruption defense;
- published-runtime A18 smoke and deterministic tests use the same verified signature.

v1.14.0 remains immutable historical evidence; current behavior is v1.14.0 functionality plus the v1.14.1 guard.

Canonical references:

- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
- `docs/RELEASE_1.14.1.md`

## Farming Guide current contract

- raid-start Loadout / Inventory Editor; not live raid inventory mirroring
- current Tarkov item dimensions, storage mechanics, filters, attachment/armor slots and conflicts
- nested storage via `ParentInstanceId`
- recursive weapon/helmet/armor child-slot editing
- same-page compatible-item picker plus search drag/drop using one Core compatibility policy
- one-item slot silent overwrite prohibited
- profile-aware pockets and preset persistence
- product-owned exact multi-grid visual layout only on verified full grid signature; otherwise compact fallback
- impossible persisted state fails closed

Loot value/pickup/discard/replace recommendation and Scanner real-time recommendation remain out of current Farming Guide scope.

## Other maintained contracts

- Scanner: external screen pixels + OCR only; false-positive avoidance; no memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass.
- Quest: exact ProfileVariable has priority; unsupported/structural drift fails closed; Future Needed Items remains conservative.
- Hideout: FIR source semantics preserved.
- Ammo: same-caliber penetration plus proven current direct-purchase state.
- Game Content: candidate validation → active/LKG; suspicious/unknown structures fail closed.
- Map/MiniMap donor pin: `d933792b6042a51cea38dc44b686a096fe30de67`.
- Public stable source/tag/assets are immutable historical identity.

## Schema

```text
Desktop: 1.14.1
Content write/read: v10 / v3-v10
user.db: v1
Farming Guide state: v1
Scanner display settings: v9
Scanner catalog write/read: v4 / v1-v4
```

## External validation still pending

Automated release validation is complete. Separate real-environment evidence remains `PENDING`:

- user's actual PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
