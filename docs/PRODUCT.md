# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-09-01 KST**  
상태: **v1.15.2 RELEASE CANDIDATE / v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

정확한 release SHA/asset/CI와 schema 사실값은 `docs/PROJECT_STATE.json`, 공개 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`를 사용한다.

## 1. 제품 정의

준현 헬퍼는 Escape from Tarkov 플레이에 필요한 진행, 아이템, 탄약, 지도, 화면 인식, raid-start loadout과 raid 중 파밍 판단을 하나의 Windows x64 데스크톱 프로그램에서 제공하는 개인용 헬퍼다.

핵심 목표:

- 플레이 중 필요한 정보를 빠르게 확인한다.
- 사용자가 직접 확인한 진행 상태를 정확히 저장한다.
- current Tarkov data를 검증 가능한 범위에서 안전하게 반영한다.
- 알 수 없는 상태를 낙관적으로 추측하지 않고 fail closed한다.
- 게임 프로세스 내부를 읽거나 변조하지 않는 외부 보조 프로그램을 유지한다.
- 사용자 데이터와 외부 Game Content의 lifecycle을 분리한다.
- 실사용 회귀를 재현 가능한 evidence/test로 축적한다.

제품이 아닌 것:

- Tarkov bot / input automation
- anti-cheat bypass 도구
- game memory/packet inspector
- runtime GPT/AI가 필수인 서비스
- backend/account service

## 2. 플랫폼 / 배포

- Windows x64
- .NET 10 / WPF
- self-contained single-file executable
- portable ZIP / installer 없음
- 일반 사용에 관리자 권한 불필요
- mutable user state는 `%LocalAppData%/JunhyunHelper`에 저장
- Program Update는 latest public stable GitHub release를 기준으로 사용자 동의 후 수행
- public stable source/tag/assets는 immutable historical identity

Current public stable은 **v1.15.1**이며 exact product source는 `821def285e2b4964242b50981f6ba6245e996057`이다. v1.15.2는 검증/배포가 완료될 때까지 이 공개 authority를 대체하지 않는다.

## 3. 데이터 authority

### Game Content

Remote Tarkov source를 import/검증해 만든 canonical snapshot이다.

- Quest / Hideout / Item / Ammo 등 게임 기준 데이터
- Farming Guide item dimensions, grids, filters, `specialSlot` classification, equipment compatibility, conflicts, preset/layout source data
- source attachment/armor/default-preset metadata는 content evidence로 보존할 수 있지만 v1.15.2 Farming Guide는 equipment-internal user state를 만들지 않는다.
- candidate가 validation/completeness/integrity를 통과해야 active가 됨
- Last Known Good 보존

### User Progress / user-owned state

- profile / GameMode / level / faction / edition / prestige
- trader / Quest / Hideout 진행
- exact observed ProfileVariables
- FIR/non-FIR inventory와 consumption ledger
- Scanner settings/favorites/recents/reviewed evidence
- Map/MiniMap settings
- Farming Guide working state/presets/fixed equipment/automation locks

Game Content Update나 Program Update가 user-owned state를 덮어쓰지 않는다.

## 4. Quest / Hideout / Needed Items

- exact ProfileVariable 값은 compatibility inference보다 우선한다.
- unsupported/unknown prerequisite와 structural drift는 fail closed한다.
- audited staged task-pool compatibility는 증명된 범위에서만 사용한다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- flexible candidate requirement는 실제 hand-in을 추측하지 않는다.
- Hideout FIR requirement는 source `foundInRaid` 의미를 보존한다.
- deterministic mandatory consumption은 ledger를 사용해 중복 소비와 rollback을 관리한다.

Current needed quantity/source authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

다른 subsystem이 별도 계산으로 새로운 truth를 만들지 않는다.

## 5. Items / Ammo

Items는 canonical content, profile, inventory, Needed Items를 결합한 탐색/조회 surface다.

Ammo는 read-only 비교와 profile-aware pickup 판단을 제공한다.

- same-caliber penetration 비교
- 현재 profile에서 증명된 direct-purchase state 사용
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않음
- authoritative Ammo Pack `containsItems` 관계 우선

## 6. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

Donor 전체는 제품 사양 권위가 아니다. JunhyunHelper first-party bridge/customization이 product lifecycle과 presentation 의미를 소유한다.

## 7. Scanner / Mini Scanner

Scanner는 **Tarkov 화면 픽셀을 current catalog Item ID에 연결**하는 외부 입력 subsystem이다.

대표 흐름:

```text
screen capture
→ detail/header structural validation
→ item-name ROI
→ serialized OCR
→ bounded normalization
→ current-catalog conservative matching
→ optional strict visual corroboration
→ Item ID or fail closed
```

Safety:

- external screen pixels + OCR만 사용
- game memory read / injection / hook / kernel-driver access / input automation / network manipulation / anti-cheat bypass 금지

Recognition:

- false positive보다 miss 선호
- current official catalog가 identity authority
- ambiguity면 Item ID를 내지 않음
- Item ID 확정 전에 price/needed/source/previous-frame metadata를 identity evidence로 사용하지 않음
- reviewed actual Tarkov evidence 없이 OCR/matcher/recovery acceptance를 완화하지 않음
- Ground Truth는 explicit user-reviewed save만 authoritative

Mini Scanner는 Farming Guide의 **지속 지시 presentation surface**이기도 하다. Item ID/가격/필요 개수 truth는 Scanner/Items workspace의 기존 authority를 재사용하고, Farming Guide는 해당 사실을 받아 의사결정만 소유한다.

## 8. Farming Guide

Farming Guide는 두 역할을 가진다.

1. **Loadout / Inventory Editor** — 레이드 시작 시점의 완제품 장비와 수납 상태/프리셋을 구성한다.
2. **Live Raid Farming Advisor** — Scanner가 확인한 아이템을 현재 raid-session 상태와 비교해 보관/교체/버리기/최상위 장비 칸 장착을 제안한다.

실제 게임 inventory를 자동 mirror하거나 게임 입력을 대신하지 않는다. 특히 레이드 중 주운 총기·헬멧·방탄복의 알 수 없는 내부 부품 상태를 추측하지 않는다.

### Complete equipment contract — v1.15.2+

장비는 **opaque complete item 하나**로 처리한다.

- 총기 attachment/mod 편집 UI 없음
- 헬멧 attachment 편집 UI 없음
- body armor / armored rig armor-plate 편집 UI 없음
- recursive assembly navigation / compatible-part picker 없음
- equipment-internal drag/drop target 없음
- raid advisor가 equipment-internal Equip / ReplaceEquip을 만들지 않음
- 저장된 legacy `Attachments` / `ArmorPlates`는 schema read compatibility를 위해 읽을 수 있지만 current Farming Guide runtime에서는 root Item ID만 남기고 폐기한다.

Source Game Content의 assembly/default-preset metadata는 **완제품 source image 선택 같은 read-only evidence**에만 사용할 수 있다. 사용자가 관리해야 할 실제 raid equipment state로 승격하지 않는다.

### Top-level equipment

다음 top-level target은 계속 완제품 단위로 장착/교체할 수 있다.

- Headset
- Helmet
- Face Cover
- Armband
- Body Armor
- Eyewear
- Primary Weapon 1 / 2
- Holster / Pistol
- Rig
- Backpack
- Secure Container
- fixed Melee / Dogtag setup

레이드 지시는 `[장비 칸]에 장착`, `[장비 칸]의 [기존 장비]와 교체`까지 허용한다. `총구/조준경/방탄판에 장착` 같은 내부 지시는 허용하지 않는다.

### Storage mechanics

- ordinary storage에서는 current Tarkov item `width × height` 사용
- Pocket / Rig / Backpack / Secure Container / Special Slots
- drag 중 `R` rotation
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- profile edition/progress 기반 standard/expanded pocket geometry
- contents가 있는 root carrier의 destructive replacement fail closed
- pistol/holster는 화면상 Eyewear 아래에 표시
- storage hint는 `R: 회전 · F: 아이템/장비/빈 칸 잠금`

Secure Container는 explicit secure/pouch semantics를 우선하고 generic case/container와 구분한다.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId`가 nested placement를 표현한다.

사용자가 **상세 내부 화면으로 열 수 있는 stored item은 Backpack 또는 Rig뿐**이다.

- 가방 안 가방 허용
- 가방/허용 storage 안 리그 허용
- 실제 source-backed storage grids와 filters를 사용
- 내부 grid에도 normal drag/drop 가능
- nested backpack/rig 안에 다시 허용된 nested backpack/rig를 둘 수 있음
- root Rig / Backpack / Secure Container의 storage는 별도 상세창이 아니라 메인 Farming Guide storage surface에 표시
- generic case/container나 일반 장비의 내부 detail surface는 현재 제품에서 제공하지 않음
- orphan/duplicate/self/cycle/invalid grid/filter/bounds/overlap state fail closed
- carrier 이동 시 descendant parent chain 유지
- destructive removal은 subtree 제거
- 자신/descendant 안으로 이동 금지

Nested storage detail은 전체 center column을 가리는 고정 overlay가 아니다. 렌더링된 grid footprint + 제목/닫기 chrome에 맞춰 compact size를 계산하고 viewport를 넘지 않도록 제한한다. 메인 storage surface는 뒤에서 계속 보인다.

### Special Slots

- 호환성 authority는 current Game Content의 canonical `specialSlot` classification이다.
- `specialSlot`이 아닌 일반 아이템은 Special Slot에 들어갈 수 없다.
- 호환 아이템은 일반 인벤토리 footprint와 무관하게 Special Slot 정확히 1칸을 사용한다.
- 같은 아이템이 ordinary storage에 있을 때는 원래 width × height를 사용한다.
- sanitizer, manual drag/drop, rendering, collision, summary, raid advisor가 동일 policy를 사용한다.

### Complete-item imagery

Farming Guide는 임의 조립 이미지를 만들지 않는다.

이미지 우선순위:

1. canonical base item에 authoritative `DefaultPresetItemId`가 있고 해당 preset에 source image가 있으면 그 완제품 image
2. item 자체의 source-backed `Image512Url` / `GridImageUrl`
3. canonical item icon

총기/장비 card는 기존의 큰 내부 여백을 줄여 aspect ratio를 유지하면서 equipment slot을 더 크게 채운다.

### Storage visual layout authority

Storage mechanics와 화면상 visual arrangement를 분리한다. Product-owned exact visual metadata는 mechanics를 바꿀 수 없다.

Exact multi-grid coordinates는 verified profile의 layout identity, grid count와 각 grid index의 expected width/height가 current grids와 정확히 일치할 때만 사용한다. 하나라도 다르면 finite compact visual fallback을 사용한다.

### Raid session lifecycle

- `레이드 시작`은 현재 working/preset snapshot과 lock state를 immutable baseline으로 잡고 별도 raid-session을 연다.
- 레이드 중 수동 drag/drop, 장비/보관 상태 변경, lock 변경은 즉시 새로운 session revision이 된다.
- `레이드 종료`는 session 변경을 폐기하고 raid-start baseline snapshot/locks로 복원한다.
- 레이드 중 변경은 preset/working-state 영구 저장으로 취급하지 않는다.

### Lock contract

Locks는 automation constraint이며 direct edit permission이 아니다.

- stored item lock은 해당 item instance의 자동 removal/replacement를 막는다.
- equipment/carrier lock은 현재 장착된 target 자체의 자동 removal/replacement를 막는다.
- locked target이 제거/교체되면 해당 target lock도 사라진다.
- locked Rig / Backpack / Secure Container 내부 ordinary storage는 여전히 자동 수납 후보가 될 수 있다.
- empty-cell lock은 독립적인 1-cell reservation이며 사용자가 unlock할 때까지 유지한다.
- direct user edit는 lock보다 우선하고, direct state change는 pending advice를 stale 처리한다.

### Scanner-driven instruction / explicit acceptance

```text
confirmed Scanner Item ID + scanner-owned price/needed facts
→ current raid-session complete-equipment/storage snapshot + locks
→ Store / Replace / Discard / top-level Equip / ReplaceEquip proposal
→ one revision-bound pending instruction
→ Mini Scanner action text
→ explicit accept hotkey
→ revision-checked commit
```

- 새 scan은 이전 미수락 pending을 state mutation 없이 폐기하고 unchanged current raid state에서 다시 계산한다.
- 수동 inventory/equipment/lock 변경은 pending을 조용히 무효화한다.
- acceptance 성공 feedback은 `반영 완료`다.
- scanned item name은 action text에서 반복하지 않는다.
- T simulated scan은 같은 decision path를 사용하되 bounded lifetime 후 사라진다.

Current action wording:

- Store: `[보관할 장소]에 보관`
- Replace stored: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- top-level Equip: `[장착할 장비 칸]에 장착`
- top-level ReplaceEquip: `[장착할 장비 칸]의 [기존 장비]와 교체`
- accepted feedback: `반영 완료`

Accepted Store / Replace / top-level Equip / ReplaceEquip은 session-local acquired count에 반영되어 이후 Needed priority에서 남은 수량을 줄이지만 authoritative profile inventory를 직접 바꾸지 않는다.

### Loot priority boundary

1. 현재 필요한 수량이 남은 item 우선
2. 같은 필요 여부에서는 `max(trader sell, flea average, 0) / ordinary slots` 우선
3. 같은 칸당 가치에서는 total effective value 우선
4. 마지막 동률이면 작은 ordinary footprint 우선

합법적인 빈 placement를 destructive replacement보다 우선한다.

### Persistence / non-goals

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v2
```

schema v2는 그대로 유지한다. v1.15.2는 schema bump 대신 legacy equipment assembly fields를 current runtime에서 root-only state로 정규화한다.

현재 Farming Guide 비포함:

- game memory 기반 live inventory read
- 게임 입력 자동화/자동 loot
- 화면상의 실제 inventory 좌표를 지속적으로 1:1 추적하는 mirror
- unknown equipment internals inference
- weapon/helmet/armor modification editor
- user acceptance 없이 자동 상태 변경
- extraction probability 기반 탈출 지시

Canonical v1.15.2 correction: `docs/DECISION_V1.15.2_COMPLETE_EQUIPMENT_MODEL.md`.

## 9. Diagnostics

진단은 명시적 opt-in이다. 김태영 PC 지원 경로는 로컬 diagnostic ZIP 생성 후 사용자가 직접 전달하는 구조이며 자동 upload/attachment/send를 하지 않는다. 실제 원인 판정은 해당 PC evidence가 있어야 한다.

## 10. UI / interaction

- 제품 전체와 일관된 WPF interaction
- shared overlay는 presentation lifetime만 소유하고 domain truth를 재구현하지 않음
- source/XAML만 보고 user-visible 변경 완료 선언 금지
- 기존 verified behavior를 무관한 새 기능 때문에 변경하지 않음
- 실사용에서 관찰된 표시/반응 문제는 자동화 테스트가 통과하더라도 실제 회귀 evidence로 취급

## 11. Schema / compatibility

```text
Desktop candidate: 1.15.2
Public stable: 1.15.1
Content write: v10
Content readable: v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.2는 schema를 올리지 않는다. 기존 Farming Guide v2 파일의 equipment-internal assembly data는 읽을 수 있지만 complete-equipment runtime에서는 root-only equipment state로 정리한다. 사용자 프리셋 이름, top-level equipment, storage placement, nested backpack/rig placement와 locks는 보존한다.

## 12. Release quality gate

변경 성격에 따라 다음을 검증한다.

- deterministic tests
- Windows Release build / XAML compile
- self-contained win-x64 publish
- actual published EXE startup / relevant Product UI runtime smoke
- graceful shutdown / active-async Shutdown Race
- portable package/root audit
- ZIP/checksum equality
- CI / Documentation Consistency
- exact-main identity
- public tag/release/assets/latest readback

실사용 보고 증상은 자동화 테스트보다 높은 우선순위의 회귀 evidence다.

## 13. 유지보수 방향

현재 public 제품은 product-complete maintenance mode다. v1.15.0은 Farming Guide MINOR 기능 확장이었고 v1.15.1/v1.15.2는 실제 사용 흐름에 맞게 이를 보수하는 PATCH correction이다.

기본 우선순위는 실사용 오류, Tarkov 변화 대응, 안정성/신뢰성, 성능, regression coverage, bounded technical debt cleanup 순이다. 추가 새 기능이나 UX 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다.
