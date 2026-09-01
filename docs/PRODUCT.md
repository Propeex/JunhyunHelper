# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 준현 헬퍼의 **무엇을 만들고 왜 만드는지**를 정의하는 canonical 제품 요구사항이다. 사용자가 현재 대화에서 새로 확정한 제품 의도가 기존 구현보다 우선한다. 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: **2026-09-01 KST**  
상태: **v1.14.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

정확한 release SHA/asset/CI와 schema 사실값은 `docs/PROJECT_STATE.json`, 공개 상태는 `docs/CURRENT_STATE.md` / `docs/STATE.md`를 사용한다.

## 1. 제품 정의

준현 헬퍼는 Escape from Tarkov 플레이에 필요한 진행, 아이템, 탄약, 지도, 화면 인식, raid-start loadout 정보를 하나의 Windows x64 데스크톱 프로그램에서 제공하는 개인용 헬퍼다.

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
- Farming Guide working state/presets/fixed equipment

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

## 8. Farming Guide

Farming Guide는 Scanner 오른쪽의 first-class section이며 제품 의미는 **레이드 시작 상태를 구성하는 Loadout / Inventory Editor**다. 실제 raid inventory를 지속적으로 1:1 mirror하지 않는다.

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

**v1.14.1 current rule:** exact multi-grid coordinates는 verified profile의 layout identity, grid count와 **각 grid index의 expected width/height가 current live grids와 정확히 일치할 때만** 사용한다. 하나라도 다르면 finite compact visual fallback을 사용한다. Non-overlap은 추가 corruption guard다.

v1.14.0 public source에는 expected dimension comparison이 완전 구현되지 않았으며 이 activation guard는 v1.14.1에서 교정됐다. v1.14.0의 recursive assembly/inline picker functionality는 현재 제품에 유지된다.

### Persistence / non-goals

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema v1
```

현재 Farming Guide 비포함:

- loot 가치 판단
- pickup/discard/replace recommendation
- Scanner 실시간 recommendation
- live raid inventory coordinate mirror

## 9. Diagnostics

진단은 명시적 opt-in이다. 김태영 PC 지원 경로는 로컬 diagnostic ZIP 생성 후 사용자가 직접 전달하는 구조이며 자동 upload/attachment/send를 하지 않는다. 실제 원인 판정은 해당 PC evidence가 있어야 한다.

## 10. UI / interaction

- 제품 전체와 일관된 WPF interaction
- shared overlay는 presentation lifetime만 소유하고 domain truth를 재구현하지 않음
- source/XAML만 보고 user-visible 변경 완료 선언 금지
- 기존 verified behavior를 무관한 새 기능 때문에 변경하지 않음

## 11. Schema / compatibility

```text
Desktop: 1.14.1
Content write: v10
Content readable: v3-v10
user.db: v1
Farming Guide state: v1
Scanner display settings: v9
Scanner catalog write/read: v4 / v1-v4
```

v1.14.1은 mandatory user-data migration을 요구하지 않는다.

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

현재 제품은 product-complete maintenance mode다. 우선순위는 실사용 오류, Tarkov 변화 대응, 안정성/신뢰성, 성능, regression coverage, bounded technical debt cleanup 순이다. 새 기능이나 UX 변경은 사용자의 명시적인 제품 요구사항이 있을 때만 설계한다.
