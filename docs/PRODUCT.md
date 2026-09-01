# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-09-01 KST**  
상태: **v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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

Current public stable은 **v1.15.1**이며 exact product source는 `821def285e2b4964242b50981f6ba6245e996057`이다. 이후 documentation-only main commit은 이 release의 product source를 대체하지 않는다.

## 3. 데이터 authority

### Game Content

Remote Tarkov source를 import/검증해 만든 canonical snapshot이다.

- Quest / Hideout / Item / Ammo 등 게임 기준 데이터
- Farming Guide item dimensions, grids, filters, `specialSlot` classification, equipment/attachment/armor slots, conflicts, preset/layout source data
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

Farming Guide는 Scanner 오른쪽의 first-class section이며 두 역할을 가진다.

1. **Loadout / Inventory Editor** — 레이드 시작 상태와 프리셋을 구성한다.
2. **Live Raid Farming Advisor** — 레이드 중 Scanner가 확인한 아이템을 현재 raid-session 상태와 비교해 보관/교체/버리기/장착 판단을 제안한다.

실제 게임 inventory를 화면 좌표까지 자동 mirror하거나 게임 입력을 대신하지 않는다.

### Equipment / storage

- ordinary storage에서는 current Tarkov item `width × height` 사용
- equipment surfaces for actionable raid-start gear
- Pocket / Rig / Backpack / Secure Container / Special Slots
- drag 중 `R` rotation
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- profile edition/progress 기반 standard/expanded pocket geometry
- contents가 있는 carrier의 destructive replacement fail closed
- pistol/holster는 화면상 Eyewear 아래에 표시
- storage hint는 `R: 회전 · F: 아이템/장비/빈 칸 잠금`

Secure Container는 explicit secure/pouch semantics를 우선하고 generic case/container와 구분한다.

### Special Slots

Special Slots는 일반 1×1 inventory grid가 아니다.

- 호환성 authority는 current Game Content의 canonical `specialSlot` classification이다.
- `specialSlot`이 아닌 일반 아이템은 Special Slot에 들어갈 수 없다.
- 호환 아이템은 일반 인벤토리 footprint와 무관하게 Special Slot **1칸**을 사용한다.
- 같은 아이템이 일반 storage에 있을 때는 원래 width × height를 사용한다.
- sanitizer, manual drag/drop, rendering, collision, summary, raid advisor가 동일 policy를 사용한다.
- Special Slot에 들어간 carrier의 내부 ordinary storage까지 special footprint가 되는 것은 아니다.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId`가 특정 stored carrier 내부 placement를 표현한다.

- root는 null parent
- orphan/duplicate/self/cycle/invalid grid/filter/bounds/overlap fail closed
- carrier 이동 시 descendant parent chain 유지
- destructive removal은 subtree 제거
- 자신/descendant 안으로 이동 금지

### Workbench / recursive assembly

별도 generic item information/configuration OS Window는 editing authority가 아니다. 같은 Farming Guide page의 workbench를 사용한다.

- stored bag/rig/carrier → 실제 내부 grid
- weapon → actual mod/attachment slots
- helmet/body armor → actionable attachment/replaceable armor slots
- installed child attachment의 하위 slots까지 재귀 navigation
- 빈 actionable slot single-click → 같은 page의 compatible-item icon picker
- picker 선택, search drag/drop, raid equip planning은 동일 Core compatibility/conflict authority 사용
- occupied one-item slot silent overwrite 금지
- required-slot/conflict validation은 assembly tree 전체에 적용
- persisted impossible assembly는 fail closed sanitization
- source/internal `mod_*` 등의 identifier는 내부 identity로 유지하되 알려진 의미는 사용자에게 한국어 label로 표시

### Weapon search / image presentation

- Farming Guide draggable search에서는 assembled `ItemPropertiesPreset` / `preset` records를 제외하고 canonical base weapon을 사용
- current assembly contained-item signature와 authoritative source-backed preset/composed image signature가 정확히 일치할 때만 해당 composed image 사용
- 정확한 source match가 없으면 base item + installed-part 기반의 안전한 fallback을 유지
- 임의 합성으로 Tarkov 실제 외형과 일치한다고 주장하지 않음

### Storage visual layout authority

Storage **mechanics**와 화면상 **visual arrangement**를 분리한다.

Mechanics authority:

- current validated Game Content grid count / width / height
- filters
- item dimensions
- special-slot classification
- actual placement legality

Product-owned exact visual metadata는 mechanics를 바꿀 수 없다.

Exact multi-grid coordinates는 verified profile의 layout identity, grid count와 **각 grid index의 expected width/height가 current live grids와 정확히 일치할 때만** 사용한다. 하나라도 다르면 finite compact visual fallback을 사용한다. Non-overlap은 추가 corruption guard다.

### Raid session lifecycle

- `레이드 시작`은 현재 working/preset snapshot과 lock state를 immutable baseline으로 잡고 별도 raid-session을 연다.
- 레이드 중 수동 drag/drop, 장비/보관 상태 변경, lock 변경은 즉시 새로운 session revision이 된다.
- `레이드 종료`는 session 변경을 폐기하고 raid-start baseline snapshot/locks로 복원한다.
- 레이드 중 변경은 명시적인 preset/working-state 저장으로 취급하지 않는다.
- preset 선택/삭제 같은 raid-start configuration 변경은 raid 중 차단한다.

### Lock contract

사용자는 hover + `F`로 자동 의사결정이 건드리면 안 되는 target/capacity를 lock/unlock한다. Locks는 automation constraint이며 direct edit permission이 아니다.

- stored item lock: 해당 item instance를 자동 removal/replacement 대상으로 사용하지 않는다.
- 같은 locked item을 이동하면 instance lock은 유지한다.
- equipment/carrier lock: 현재 장착된 target 자체를 자동 removal/replacement하지 않는다.
- locked target이 직접 조작 또는 accepted recommendation으로 제거/교체되면 그 target lock도 사라진다.
- Rig / Backpack / Secure Container carrier lock은 **carrier 자체**를 보호하지만 그 내부 ordinary storage를 자동 배치 후보에서 제외하지 않는다.
- item lock도 nested carrier 내부 storage 전체를 자동으로 봉쇄하지 않는다.
- empty grid cell lock: 특정 target과 무관한 독립 1-cell reservation이며 사용자가 unlock할 때까지 유지한다. 대표 목적은 재장전 시 탄창을 넣을 빈 공간 확보다.
- equipment-slot lock은 같은 automation constraint authority를 사용한다.
- 사용자의 직접 편집은 lock보다 우선하며, 직접 변경이 일어나면 이전 pending instruction은 stale이다.
- F lock toggle은 전체 page rebuild를 의도하지 않으며, full rerender가 발생해도 유효한 lock highlight를 다시 적용한다.

### Scanner-driven instruction / explicit acceptance

```text
confirmed Scanner Item ID + scanner-owned price/needed facts
→ current raid-session snapshot + locks
→ Store / Replace / Discard / Equip / ReplaceEquip proposal
→ one revision-bound pending instruction
→ Mini Scanner persistent action text
→ explicit accept hotkey
→ revision-checked commit
```

- pending instruction은 동시에 하나만 유지한다.
- acceptance 전에는 Farming Guide raid-session state를 바꾸지 않는다.
- pending이 생성된 session revision과 현재 revision이 다르면 적용하지 않는다.
- **새 Scanner item이 확인되면 이전 미수락 pending은 state mutation 없이 폐기하고 새 item을 현재 unchanged raid state에서 다시 계산한다.** 따라서 이전 지시를 먼저 수락하도록 요구하지 않는다.
- 사용자가 수동으로 inventory/equipment/lock을 바꾸면 pending을 즉시 무효화하고 Mini Scanner의 지속 지시를 조용히 제거한다. 별도 취소 문구는 표시하지 않는다.
- acceptance 성공 후 feedback은 `반영 완료`다.
- incoming scanned item name은 Mini Scanner의 다른 field에서 이미 표시되므로 Farming Guide action text에서 반복하지 않는다.
- 같은 Item ID도 Scanner가 비-item 상태를 거친 뒤 다시 확인하면 새 scan event로 처리할 수 있다.
- 검색 결과 item hover + `T`는 실제 Scanner와 동일한 snapshot/decision 경로를 사용하는 simulated scan이다.
- T test presentation은 bounded lifetime 후 사라지며, 늦게 도착한 test cleanup이 더 새로운 real Scanner presentation을 숨길 수 없다.

Current action wording:

- Store: `[보관할 장소]에 보관`
- Replace stored: `[보관할 장소]의 [기존 아이템]과 교체`
- Discard: `버리기`
- Equip: `[장착할 장소]에 장착`
- Replace equipped/attached: `[장착할 장소]의 [기존 아이템]과 교체`
- accepted feedback: `반영 완료`

### Equip / ReplaceEquip targets

Raid advisor는 ordinary storage뿐 아니라 합법적인 장착 위치를 함께 평가한다.

- PMC equipment slots
- Rig / Backpack / Secure Container carrier equipment slots
- recursive weapon/helmet attachment slots
- replaceable armor-plate slots

빈 target이 합법적이면 Equip을 제안할 수 있다. 빈 target이 없을 때는 current compatibility/conflict/lock rules를 만족하고 incoming item이 기존 item보다 현재 loot priority상 우수한 경우 ReplaceEquip을 제안할 수 있다.

Accepted Store / Replace / Equip / ReplaceEquip은 모두 session-local acquired count에 반영되어 이후 Needed priority를 계산할 때 남은 수량을 줄인다. 이는 authoritative profile inventory를 직접 변경하지 않는다.

### Current loot priority boundary

현재 정책은 placement mechanics와 분리된 `FarmingGuideLootPriorityPolicy`가 소유한다.

1. 현재 필요한 수량이 남아 있는 item을 필요하지 않은 item보다 우선한다.
2. 필요 여부가 같으면 trader/flea 중 높은 유효 가치의 **ordinary 칸당 가치**를 우선한다.
3. 칸당 가치가 같으면 총 유효 가치를 우선한다.
4. 마지막 동률이면 더 작은 ordinary footprint를 우선한다.

`EffectiveValue = max(TraderSellPrice, FleaAveragePrice, 0)`.

합법적인 빈 placement를 destructive replacement보다 우선한다. Special Slot의 1칸 점유는 placement context의 mechanic이며 item의 일반 footprint/value-per-slot truth를 전역으로 바꾸지 않는다.

### Persistence / non-goals

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v2
```

schema v2는 working/preset lock state를 저장하며 기존 v1 파일을 읽어 empty-lock 상태로 migration한다.

현재 Farming Guide 비포함:

- game memory 기반 live inventory read
- 게임 입력 자동화/자동 loot
- 화면상의 실제 inventory 좌표를 지속적으로 1:1 추적하는 mirror
- user acceptance 없이 자동 상태 변경
- extraction probability 기반 탈출 지시

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
Desktop: 1.15.1
Public stable: 1.15.1
Content write: v10
Content readable: v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.1은 v1.15.0과 동일한 Farming Guide state v2 / Scanner settings v10 / Game Content v10 schema를 유지한다. 별도의 사용자 데이터 migration은 필요하지 않으며 기존 additive migration과 readable compatibility를 보존한다.

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

현재 public 제품은 product-complete maintenance mode다. v1.15.0은 사용자가 명시적으로 확정한 Farming Guide MINOR 기능 확장이었고, v1.15.1은 그 첫 실사용 PATCH correction이다.

기본 우선순위는 실사용 오류, Tarkov 변화 대응, 안정성/신뢰성, 성능, regression coverage, bounded technical debt cleanup 순이다. 추가 새 기능이나 UX 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다.
