# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.15.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.15.2
exact product source/tag target:
f4974ee6bed5047865581240197f7f0e2787ba7c
validated PR head: 1662cc86f6298fc3a13bbcc591d38ae8c8e0787d
merge PR: #262
PR CI / Shutdown / Docs:
33481383672 / 33481383604 / 33481383640 — SUCCESS
exact-main CI / Shutdown / Docs:
33481524940 / 33481524896 / 33481524999 — SUCCESS
Release workflow: 33481956300 — SUCCESS
release id: 380290463
published UTC: 2026-09-01T07:24:43Z
562 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539168506
bytes: 80,654,539
SHA-256: 642fa3845ccb4491c2d0b520000316d79067c3957144814b0b3b77516d14ad34

SHA256SUMS.txt
asset id: 539168503
bytes: 86
asset SHA-256: 077160c0ac6076e07d061a0feb8e386f131327ad82bc4281a619afc4ecd91741
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9790251740
bytes: 241,895,658
SHA-256: 57665346651872dd4f351241dabe77de09349150ebb2d8664f8d5f626a8daf65
```

`/releases/latest`, release target and `refs/tags/v1.15.2` all resolve to `f4974ee6bed5047865581240197f7f0e2787ba7c`. Public release is `draft=false`, `prerelease=false`. Later documentation-only commits are not v1.15.2 product sources and may not replace these assets.

## v1.15.2 Farming Guide complete-equipment contract

Farming Guide는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 유지하되, 장비 자체는 이제 **완제품 단위**로만 다룬다.

- 총기·헬멧·방탄복·기타 장비 내부 attachment/mod/armor-plate 편집 UI는 없다.
- legacy `Attachments` / `ArmorPlates` state는 readable compatibility를 위해 읽을 수 있지만 current runtime에서는 root Item ID만 남긴다.
- raid advisor는 총구·조준경·방탄판 같은 장비 내부 target을 만들지 않는다.
- Primary Weapon, Holster/Pistol, Helmet, Body Armor, Rig, Backpack, Secure Container 등 최상위 장비 칸 Equip/ReplaceEquip은 유지한다.
- 장비 이미지는 canonical default preset/source에 authoritative complete image가 있으면 이를 우선하고 임의 조립 이미지는 만들지 않는다.
- 무기/장비 이미지는 aspect ratio를 유지하면서 장비 칸을 더 크게 채우도록 표시한다.

## Nested storage

상세 내부 수납 화면을 열 수 있는 stored item은 **Backpack 또는 Rig**다.

- 가방 안 가방 / 가방 안 리그 등 `ParentInstanceId` 기반 nested storage 유지
- 실제 current Game Content의 storage grid/filter를 사용
- 내부에서도 정상 drag/drop 가능
- root Rig / Backpack / Secure Container 수납칸은 메인 Farming Guide surface에 표시하므로 별도 상세창을 열지 않음
- generic case/container나 일반 장비 내부를 Farming Guide 상세 surface로 노출하지 않음
- nested detail은 실제 렌더된 grid footprint를 기준으로 compact 크기를 계산하며 불필요하게 화면 전체를 가리지 않음

## Raid advisor / locks / Special Slots

- `레이드 시작`은 현재 working equipment/storage/locks를 독립적인 raid session baseline으로 snapshot한다.
- 새 Scanner item은 이전 미수락 pending을 state mutation 없이 폐기하고 current state에서 새 지시를 계산한다.
- user accept hotkey 전에는 recommendation state를 commit하지 않는다. 성공 피드백은 `반영 완료`다.
- manual equipment/storage/lock 변경은 stale pending을 조용히 무효화한다.
- item/equipment/carrier lock은 대상 자체의 자동 제거/교체를 막지만 direct user edit는 허용한다.
- locked carrier의 ordinary 내부 storage는 여전히 자동 수납 후보가 될 수 있다.
- empty-cell lock은 독립적인 1-cell reservation으로 유지한다.
- Special Slots는 canonical `specialSlot` item만 허용하며 compatible item은 ordinary footprint와 관계없이 정확히 1칸을 사용한다.
- `레이드 종료`는 raid-session 변경을 폐기하고 시작 baseline으로 복원한다.

Current action wording:

- Store: `[보관할 장소]에 보관`
- Replace stored: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- top-level Equip: `[장착할 장비 칸]에 장착`
- top-level ReplaceEquip: `[장착할 장비 칸]의 [기존 장비]와 교체`
- accepted: `반영 완료`

Canonical references:

- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`
- `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
- `docs/RELEASE_NOTES_V1.15.2.md`

## Other maintained contracts

- Scanner: external screen pixels + OCR only; false-positive avoidance; no game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass.
- Quest: exact ProfileVariable priority; unsupported/structural drift fails closed; Future Needed Items remains conservative.
- Hideout: FIR source semantics preserved.
- Ammo: same-caliber penetration plus proven current direct-purchase state.
- Game Content: candidate validation → active/LKG; suspicious/unknown structures fail closed.
- Map/MiniMap donor pin: `d933792b6042a51cea38dc44b686a096fe30de67`.
- Public stable source/tag/assets are immutable historical identity.

## Schema

```text
Desktop: 1.15.2
Content write/read: v10 / v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.2는 schema bump가 없다. 기존 v2 Farming Guide file의 equipment-internal assembly fields만 complete-equipment runtime에서 root-only state로 정규화한다.

## External validation still pending

Automated release validation is complete. Separate real-environment evidence remains `PENDING`:

- 사용자 실제 Tarkov 플레이에서 v1.15.2 Farming Guide 시각/동작 검증
- 김태영 actual-PC diagnostic ZIP collection/analysis
