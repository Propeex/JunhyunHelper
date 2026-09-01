# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.1
exact product source/tag target:
821def285e2b4964242b50981f6ba6245e996057
validated PR head: e78ca34c272ac40b8f7c6a4bfcefede59adb9d59
PR CI / Shutdown / Docs: 33476320371 / 33476320367 / 33476320491 — SUCCESS
merge PR: #259
exact-main CI / Shutdown / Docs: 33476586723 / 33476586808 / 33476586819 — SUCCESS
Release workflow: 33476812315 — SUCCESS
release id: 380252024
published UTC: 2026-09-01T06:15:51Z
558 passed / 0 failed / 0 skipped
```

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

Exact-main artifact:

```text
JunhyunHelper-win-x64
artifact id: 9788440065
bytes: 241,908,886
SHA-256: e865fb395dcca353788495bbfb84f860129b39bdc6e89b51780d99db481592b8
```

`/releases/latest`, release target and `refs/tags/v1.15.1` all resolve to `821def285e2b4964242b50981f6ba6245e996057`. Public release is `draft=false`, `prerelease=false`.

## v1.15.1 Farming Guide raid advisor

Farming Guide retains the raid-start Loadout / Inventory Editor and the v1.15.0 raid-session advisor, with the first real-play correction pass applied in v1.15.1.

Current user flow:

- `레이드 시작` snapshots the current working equipment/storage/lock state into an isolated raid session.
- Scanner-confirmed items are evaluated against current Needed quantity, market value, legal footprint, equipment/storage occupancy and locks.
- A new scan silently rejects any previous unaccepted recommendation and recalculates against unchanged current raid state.
- Mini Scanner shows action-only guidance: store, replace, discard, equip or replace-equip. The scanned item name is not repeated.
- Explicit Farming Guide accept hotkey is required before any recommendation mutates raid-session state; successful acceptance shows `반영 완료`.
- Manual equipment/storage/lock changes invalidate stale pending guidance without cancellation-noise text.
- Empty legal equipment targets include PMC equipment, rig/backpack/secure-container carrier slots, recursive attachment slots and armor-plate slots.
- When no empty target is available, lower-priority equipped/attached items may be replacement candidates if legal and unlocked.
- Accepted Store/Replace/Equip/ReplaceEquip actions all contribute to raid-acquired Needed quantity.
- `레이드 종료` discards raid-session mutations and restores the raid-start snapshot without overwriting the saved preset.

## v1.15.1 lock and special-slot contracts

- item/equipment/carrier locks protect the locked target from automatic removal/replacement but do not block direct user editing;
- locking a rig, backpack or secure container does not block automatic storage inside its ordinary inventory grids;
- moving the same locked item preserves its instance lock, while removal/replacement of the target removes that target lock;
- empty-cell locks are independent reserved-space constraints and remain until explicitly unlocked;
- normal F lock toggles update the affected state/visual without rebuilding the whole page, and full rerenders reapply lock visuals;
- Special Slots accept only canonical `specialSlot` items;
- a compatible item occupies exactly one Special Slot regardless of its ordinary inventory footprint;
- rendering, manual placement, sanitizer, summary and raid planning share the same special-slot rule.

## Farming Guide maintained contracts

- current validated Tarkov item dimensions, storage mechanics, filters, equipment/attachment/armor slots and conflicts remain mechanical authority;
- nested storage uses `ParentInstanceId`;
- recursive weapon/helmet/armor child-slot editing remains available;
- occupied one-item slots are never silently overwritten;
- product-owned exact multi-grid visual layout activates only on verified full grid signature; otherwise compact fallback is used;
- changed weapon/helmet imagery uses only an exact source-backed composed/preset match; unsupported combinations keep safe fallback presentation;
- impossible persisted state fails closed;
- raid-session mutations stay isolated from persisted working/preset state;
- pending recommendations remain single-flight and revision-bound even though a newer scan may replace the pending transaction.

Canonical references:

- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
- `docs/RELEASE_NOTES_V1.15.1.md`
- `docs/RELEASE_1.15.1.md`

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
Desktop: 1.15.1
Content write/read: v10 / v3-v10
user.db: v1
Farming Guide state: v2
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.1 requires no new user-state migration beyond the existing readable schema contracts.

## External validation still pending

Automated release validation is complete. Separate real-environment evidence remains `PENDING`:

- further user actual-PC/Tarkov play validation
- Kim Taeyoung actual-PC diagnostic ZIP collection/analysis
