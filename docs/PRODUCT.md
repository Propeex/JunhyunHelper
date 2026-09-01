# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-09-01 KST**  
상태: **v1.15.0 RELEASE CANDIDATE / v1.14.1 PUBLIC STABLE / PRODUCT COMPLETE**

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

## 3. 데이터 authority

### Game Content

Remote Tarkov source를 import/검증해 만든 canonical snapshot이다.

- Quest / Hideout / Item / Ammo 등 게임 기준 데이터
- Farming Guide item dimensions, grids, filters, slots, conflicts, preset/layout source data
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

Mini Scanner는 v1.15.0부터 파밍 가이드의 **지속 지시 presentation surface**이기도 하다. Item ID/가격/필요 개수 truth는 Scanner/Items workspace의 기존 authority를 재사용하고, 파밍 가이드는 해당 사실을 받아 의사결정만 소유한다.

## 8. Farming Guide

Farming Guide는 Scanner 오른쪽의 first-class section이며 두 역할을 가진다.

1. **Loadout / Inventory Editor** — 레이드 시작 상태와 프리셋을 구성한다.
2. **Live Raid Farming Advisor** — 레이드 중 Scanner가 확인한 아이템을 현재 raid-session 상태와 비교해 보관/교체/버리기 지시를 제안한다.

실제 게임 inventory를 화면 좌표까지 자동 mirror하거나 게임 입력을 대신하지 않는다.

### Equipment / storage

- current Tarkov item `width × height`
- equipment surfaces for actionable raid-start gear
- Pocket / Rig / Backpack / Secure Container / Special Slot
- drag 중 `R` rotation
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- profile edition/progress 기반 standard/expanded pocket geometry
- contents가 있는 carrier의 destructive replacement fail closed

Secure Container는 explicit secure/pouch semantics를 우선하고 generic case/container와 구분한다.

### Nested storage

`FarmingGuideStoredItemState.ParentInstanceId`가 특정 stored container 내부 placement를 표현한다.

- root는 null parent
- orphan/duplicate/self/cycle/invalid grid/filter/bounds/overlap fail closed
- container 이동 시 descendant parent chain 유지
- destructive removal은 subtree 제거
- 자신/descendant 안으로 이동 금지

### Workbench / recursive assembly

별도 generic item information/configuration OS Window는 editing authority가 아니다. 같은 Farming Guide page의 workbench를 사용한다.

- stored bag/rig/carrier → 실제 내부 grid
- weapon → actual mod/attachment slots
- helmet/body armor → actionable attachment/replaceable armor slots
- installed child attachment의 하위 slots까지 재귀 navigation
- 빈 actionable slot single-click → 같은 page의 compatible-item icon picker
- picker 선택과 search drag/drop은 동일 `FarmingGuideAssemblyPolicy` compatibility/conflict authority 사용
- occupied one-item slot silent overwrite 금지
- required-slot/conflict validation은 assembly tree 전체에 적용
- persisted impossible assembly는 fail closed sanitization

### Weapon search / image presentation

- Farming Guide draggable search에서는 assembled `ItemPropertiesPreset` / `preset` records를 제외하고 canonical base weapon을 사용
- current build가 authoritative imported default preset membership과 정확히 일치할 때만 composed preset image 사용
- arbitrary build는 deterministic assembly-aware fallback 사용

### Storage visual layout authority

Storage **mechanics**와 화면상 **visual arrangement**를 분리한다.

Mechanics authority:

- current validated Game Content grid count / width / height
- filters
- item dimensions
- actual placement legality

Product-owned exact visual metadata는 mechanics를 바꿀 수 없다.

**v1.14.1+ current rule:** exact multi-grid coordinates는 verified profile의 layout identity, grid count와 **각 grid index의 expected width/height가 current live grids와 정확히 일치할 때만** 사용한다. 하나라도 다르면 finite compact visual fallback을 사용한다. Non-overlap은 추가 corruption guard다.

### Raid session lifecycle

- `레이드 시작`은 현재 working/preset snapshot과 lock state를 immutable baseline으로 잡고 별도 raid-session을 연다.
- 레이드 중 수동 drag/drop, 장비/보관 상태 변경, lock 변경은 즉시 새로운 session revision이 된다.
- `레이드 종료`는 session 변경을 폐기하고 raid-start baseline snapshot/locks로 복원한다.
- 레이드 중 변경은 명시적인 preset/working-state 저장으로 취급하지 않는다.
- preset 선택/삭제 같은 raid-start configuration 변경은 raid 중 차단한다.

### Lock contract

사용자는 hover + `F`로 자동 의사결정이 건드리면 안 되는 범위를 lock/unlock한다.

- stored item lock: 해당 item을 자동 replacement 대상으로 사용하지 않는다.
- locked item의 subtree/내부 storage도 자동 destructive decision에서 보호한다.
- Rig / Backpack / Secure Container 등 carrier lock: 해당 storage와 그 내부를 자동 배치 후보에서 제외한다.
- empty grid cell lock: 자동 배치가 사용할 수 없는 1-cell reservation으로 취급한다.
- equipment-slot lock은 장비 자동 판단 확장 시 동일한 automation constraint authority로 사용한다.
- 사용자의 직접 편집은 lock보다 우선하며, 직접 변경이 일어나면 이전 pending instruction은 stale이다.

### Scanner-driven instruction / explicit acceptance

```text
confirmed Scanner Item ID + scanner-owned price/needed facts
→ current raid-session snapshot + locks
→ placement / replacement / discard proposal
→ one pending instruction
→ Mini Scanner persistent instruction + accept hotkey hint
→ explicit accept
→ revision-checked commit
```

- pending instruction은 동시에 하나만 유지한다.
- acceptance 전에는 Farming Guide inventory를 바꾸지 않는다.
- pending이 생성된 session revision과 현재 revision이 다르면 적용하지 않는다.
- 사용자가 수동으로 inventory/equipment/lock을 바꾸면 pending을 즉시 취소하고 Mini Scanner의 지속 지시도 제거한다.
- acceptance 후에는 짧은 `수락 완료` feedback만 표시하고 다음 scan을 기다린다.
- 같은 Item ID도 Scanner가 비-item 상태를 거친 뒤 다시 확인하면 새 scan event로 처리할 수 있다.
- 검색 결과 item hover + `T`는 실제 Scanner와 동일한 snapshot/decision 경로를 사용하는 개발·사용자 검증용 simulated scan이다.

### Current loot priority boundary

현재 v1.15.0 정책은 placement mechanics와 분리된 `FarmingGuideLootPriorityPolicy`가 소유한다.

1. 현재 필요한 수량이 남아 있는 item을 필요하지 않은 item보다 우선한다.
2. 필요 여부가 같으면 trader/flea 중 높은 유효 가치의 **칸당 가치**를 우선한다.
3. 칸당 가치가 같으면 총 유효 가치를 우선한다.
4. 마지막 동률이면 더 작은 footprint를 우선한다.

빈 공간에 합법적으로 들어가는 경우에는 우선 배치하고, 공간이 부족할 때만 위 priority를 이용해 unlocked stored item replacement를 검토한다.

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

## 9. Diagnostics

진단은 명시적 opt-in이다. 김태영 PC 지원 경로는 로컬 diagnostic ZIP 생성 후 사용자가 직접 전달하는 구조이며 자동 upload/attachment/send를 하지 않는다. 실제 원인 판정은 해당 PC evidence가 있어야 한다.

## 10. UI / interaction

- 제품 전체와 일관된 WPF interaction
- shared overlay는 presentation lifetime만 소유하고 domain truth를 재구현하지 않음
- source/XAML만 보고 user-visible 변경 완료 선언 금지
- 기존 verified behavior를 무관한 새 기능 때문에 변경하지 않음

## 11. Schema / compatibility

```text
Desktop candidate: 1.15.0
Public stable: 1.14.1
Content write: v10
Content readable: v3-v10
user.db: v1
Farming Guide state: v2 (reads v1-v2)
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

v1.15.0의 Farming Guide v1→v2 및 Scanner settings v9→v10 migration은 additive이며 기존 user-owned loadout/preset/Scanner 설정을 보존한다.

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

현재 public 제품은 product-complete maintenance mode이며 v1.15.0은 사용자가 명시적으로 확정한 Farming Guide 기능 확장이다. 이후 기본 우선순위는 실사용 오류, Tarkov 변화 대응, 안정성/신뢰성, 성능, regression coverage, bounded technical debt cleanup 순이다. 추가 새 기능이나 UX 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다.
