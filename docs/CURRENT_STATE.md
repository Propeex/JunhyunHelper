# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
validated PR head: db82512e6e723f2d85ed0ddf3f3c7c9b0e3a70af
merge PR: #265
PR CI / Shutdown / Docs:
33487099126 / 33487099119 / 33487099201 — SUCCESS
exact-main CI / Shutdown / Docs:
33487466031 / 33487466005 / 33487465946 — SUCCESS
Release workflow: 33487795730 — SUCCESS
release id: 380333729
published UTC: 2026-09-01T08:35:55Z
563 passed / 0 failed / 0 skipped
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
JunhyunHelper-win-x64
artifact id: 9792459273
bytes: 241,909,375
SHA-256: c0aba02d6a465734c841b044776dfcf087bab9b29141b23c71ffb5a0a65c6cb2
```

`/releases/latest`, release target and `refs/tags/v1.15.3` all resolve to `c35204da66eb0af454b50550c830b071a0897835`. Public release is `draft=false`, `prerelease=false`. Documentation-only follow-up commits are not v1.15.3 product sources.

## Farming Guide current contract

### Complete equipment

Weapons, helmets, body armor and other equipment remain opaque complete items.

- no weapon/helmet attachment editor;
- no armor-plate editor;
- no equipment-internal drag/drop or raid Equip/ReplaceEquip target;
- legacy attachment/armor state remains readable only for compatibility and is normalized to root-only runtime state;
- top-level equipment Equip/ReplaceEquip remains supported.

### Source-backed nested storage — v1.15.3

`ParentInstanceId` remains the nested-storage address. A stored item may expose compact interactive internal storage whenever current validated Game Content contains real `StorageGrids` for it.

- no Key tool/case name allowlist;
- current source grid width/height and allowed/excluded category/item filters are authoritative;
- specialized containers inside Secure Container or another legal storage surface remain recursively addressable;
- orphan/duplicate/self/cycle/filter/bounds/overlap state fails closed;
- root Rig/Backpack/Secure Container storage remains directly visible on the main Farming Guide page;
- attachment/armor slots are not storage grids and remain outside this feature.

When a nested grid has a positive allow-list and that filter accepts the incoming scanned item, it is a dedicated storage candidate and is evaluated before general Secure Container/Pockets/Rig/Backpack empty space. Unrestricted nested storage does not receive this priority.

### Locks / border

- unlocked stored item: neutral border;
- `F`-locked stored item: accent/yellow border;
- unlocking restores neutral border;
- equipment/carrier locks and reserved empty-cell semantics remain unchanged;
- locks constrain automation, not direct user editing.

### Search result + T simulated scan

- hovered concrete result + `T` takes precedence over Search TextBox focus;
- no hovered result means `T` remains text input;
- active raid session uses the same Scanner recommendation path as a normal confirmed item;
- Scanner capture mode need not be enabled;
- a verified same-mode local Scanner catalog may be loaded on demand if the in-memory catalog is absent after restart;
- preparation failure is visible instead of a silent no-op.

## Raid advisor

- raid start snapshots current working/preset state and locks into an isolated session;
- new scan rejects an older unaccepted pending without state mutation and replans from current state;
- explicit configured accept hotkey is required before recommendation effects commit;
- manual equipment/storage/lock changes invalidate stale pending advice;
- accepted feedback is `반영 완료`;
- Special Slots use canonical current `specialSlot` classification and one-cell occupancy;
- raid end restores the raid-start baseline and discards session changes.

Canonical references:

- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/RELEASE_1.15.3.md`
- `docs/RELEASE_NOTES_V1.15.3.md`

## Schema

```text
Desktop: 1.15.3
Content write/read: v10 / v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.3 introduces no persistence schema bump.

## External validation still pending

Automated release validation is complete. Separate real-environment evidence remains `PENDING`:

- 사용자 실제 Tarkov 플레이에서 v1.15.3 Farming Guide 시각/동작 검증
- 김태영 actual-PC diagnostic ZIP collection/analysis
