# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.0
exact product source/tag target:
b974d56f32d073ce21a5de4171737670f83261f3
validated candidate head: 397c82b8911597128c5878e7974db6a7822888d8
candidate PR CI / Shutdown / Docs: 33466090956 / 33466090958 / 33466090940 — SUCCESS
merge PR: #256
exact-main CI / Shutdown / Docs: 33467376556 / 33467376508 / 33467376529 — SUCCESS
Release workflow: 33467575493 — SUCCESS
release id: 380200480
published UTC: 2026-09-01T03:49:49Z
540 passed / 0 failed / 0 skipped
```

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

Exact-main artifact:

```text
JunhyunHelper-win-x64
artifact id: 9785383239
bytes: 241,875,746
SHA-256: 6ba4c5819119a230ee02e4f7c2cb093679527623e3ab9665b8ebc05dee5936ae
```

`/releases/latest`, release target and `refs/tags/v1.15.0` all resolve to `b974d56f32d073ce21a5de4171737670f83261f3`. Public release is `draft=false`, `prerelease=false`.

## v1.15.0 Farming Guide raid advisor

Farming Guide now extends the existing raid-start Loadout / Inventory Editor with an explicit **raid-session advisor**. It still does not read Tarkov's internal inventory state or automate game input.

Current user flow:

- `레이드 시작` snapshots the current Farming Guide working state into an isolated raid session.
- Scanner-recognized items are evaluated against the current raid-session state.
- Mini Scanner shows keep/place/replace/discard guidance.
- The user must press the configured Farming Guide accept hotkey before the recommendation mutates the raid-session model.
- Manual equipment/storage/lock changes invalidate stale pending guidance immediately.
- `레이드 종료` discards raid-session mutations and restores the raid-start snapshot without overwriting the original preset.
- Hover + `F` protects an item/equipment/storage/cell from automatic placement/replacement; locked empty cells act as reserved capacity.
- Hovering a Farming Guide search result and pressing `T` sends a simulated scan through the same recommendation path as Scanner input.

Current recommendation policy uses existing JunhyunHelper truth rather than duplicate trackers:

- remaining needed quantity from the existing Needed Items plan;
- current merchant sell / Flea average economic data available to Scanner;
- item footprint and legal storage filters;
- current raid-session occupancy and lock state;
- deterministic placement/replacement selection.

Protected carriers/items/cells are never sacrificed by the automatic recommendation path. Recommendations remain advisory until explicit user acceptance.

Canonical references:

- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
- `docs/RELEASE_NOTES_V1.15.0.md`
- `docs/RELEASE_1.15.0.md`

## Farming Guide maintained contracts

- current Tarkov item dimensions, storage mechanics, filters, attachment/armor slots and conflicts remain authoritative;
- nested storage uses `ParentInstanceId`;
- recursive weapon/helmet/armor child-slot editing remains available;
- one-item slot silent overwrite is prohibited;
- product-owned exact multi-grid visual layout activates only on verified full grid signature; otherwise compact fallback is used;
- impossible persisted state fails closed;
- raid-session mutations are isolated from the saved preset/working state;
- pending Scanner recommendations are single-flight, revision-bound and explicitly accepted;
- locked carriers/items/cells are excluded from automatic recommendation placement/replacement.

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
Desktop: 1.15.0
Content write/read: v10 / v3-v10
user.db: v1
Farming Guide state: v2
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## External validation still pending

Automated release validation is complete. Separate real-environment evidence remains `PENDING`:

- user's actual PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
