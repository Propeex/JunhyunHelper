# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-09-01 KST**  
상태: **v1.14.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 공개 stable은 **v1.14.0**이다. Farming Guide는 v1.13.0에서 raid-start Loadout / Inventory Editor로 추가됐고, v1.13.1~v1.13.3에서 UI, drag/drop, equipment compatibility, nested storage와 실제 attachment interaction을 실사용 기준으로 보완했다. v1.14.0에서는 재귀 조립 편집, inline compatible-item picker, assembly-aware image presentation, 검증된 multi-grid visual layout을 추가했다.

v1.14.0 구현·검증·병합·공개 릴리즈와 release evidence 기록이 완료됐다. `docs/ACTIVE_WORK.md`가 `NONE`이면 현재 복구할 개발 작업이 없다.

주요 제품 영역:

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo / cross-navigation / profile-aware pickup
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Program Update
- Scanner + Mini Scanner
- Scanner Saved Case / Ground Truth / diagnostics / regression dataset
- Scanner item database / Favorites / Recents
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단

Runtime GPT/AI 의존성은 없다.

## 2. 현재 public stable

```text
version: v1.14.0
exact product release source/tag target:
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
PR: #251 — MERGED
superseded draft PR: #250 — CLOSED UNMERGED
validated PR head:
c5ee50ba60f2bc7db461328608ec591f4320ccca
PR exact-head CI: 33453431628 — SUCCESS
PR exact-head Shutdown Race CI: 33453431625 — SUCCESS
PR exact-head Documentation Consistency: 33453431595 — SUCCESS
exact-main CI: 33453784868 — SUCCESS
exact-main Shutdown Race CI: 33453784901 — SUCCESS
exact-main Documentation Consistency: 33453784893 — SUCCESS
release workflow: 33454002732 — SUCCESS
release id: 380133403
published UTC: 2026-09-01T00:15:44Z
527 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 538692301
bytes: 80,633,458
SHA-256:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b

SHA256SUMS.txt
asset id: 538692300
bytes: 86
asset SHA-256:
06ae3473f7fe87d62b0d05dac0d16640a55e30e8a8fd83e4770f962a8fc5dfe3
```

Exact-main artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9780762947
archive bytes: 241,830,878
archive SHA-256:
1898028e10ef336b2dce35add94d2e1cf83b5c58c27c98649691fe11bdbe8632
```

GitHub `/releases/latest`, release target, `refs/tags/v1.14.0`, exact-main product source가 모두 `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

공식 공개 증거:

- `docs/RELEASE_1.14.0.md`
- `docs/.release-v1.14.0-status.json`
- `docs/RELEASE_NOTES_V1.14.0.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only main commit은 v1.14.0 product release source가 아니다. historical product identity는 위 source/tag/assets에 고정한다.

## 3. Farming Guide 제품 의미

Farming Guide는 Scanner 오른쪽의 first-class section이며 제품 목적은 **레이드 시작 상태를 구성하는 Loadout / Inventory Editor**다.

실제 raid inventory를 지속적으로 1:1 mirror하는 시스템이 아니다. 사용자가 출발 장비, carrier, 내부 storage, attachment/armor assembly를 구성하고 preset으로 저장/복원하는 제품 surface다.

현재 비포함:

- loot 가치 판단
- pickup/discard/replace 추천
- Scanner 실시간 recommendation
- 실제 raid inventory 좌표의 지속적인 1:1 동기화
- arbitrary build를 Tarkov client와 동일한 합성 이미지로 생성하는 renderer

현재 authority:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 4. Farming Guide equipment / storage 모델

현재 raid-start equipment surface는 사용자가 실제로 구성 가능한 장비를 중심으로 둔다.

- Headset
- Helmet / headwear
- Face cover / eyewear
- Body armor / armored rig
- Armband
- Primary Weapon 1 / 2
- Holster sidearm
- Melee

v1.14.0부터 current Tarkov에서 사용자가 직접 장착할 수 없는 PMC dogtag equipment surface는 제거한다. legacy persisted dogtag 값은 backward-compatible하게 읽되 current working state에서는 정리한다.

Melee는 레이드마다 반복 입력하는 대상이 아니므로 per-profile preset과 분리된 user-level fixed setting이다.

Top-level storage surface:

- Pockets
- Rig
- Backpack
- Secure Container
- Special Slots

Carrier의 실제 grid count/width/height/filter는 current validated Game Content가 mechanics authority다.

Pocket geometry는 active profile을 기준으로 중앙 정책에서 결정한다.

```text
standard: 1×1 / 1×1 / 1×1 / 1×1
expanded: 1×1 / 1×2 / 1×2 / 1×1
```

Expanded pocket eligibility는 제품이 증명 가능한 edition 특전 또는 Old Patterns 완료 상태에서 해석하며 UI와 persisted-state sanitization이 같은 resolved geometry를 소비한다.

## 5. Nested storage 계약

`FarmingGuideStoredItemState.ParentInstanceId`가 storage surface identity를 보존한다.

- `ParentInstanceId == null`: top-level surface
- non-null: 특정 stored container instance 내부 grid

기존 schema-v1 저장 파일에 이 필드가 없어도 null root placement로 deserialize되어 backward compatible하다.

Load/sanitize 순서:

1. root placement를 current carrier/grid/filter/bounds/overlap 기준으로 검증
2. accepted parent가 증명된 nested placement만 단계적으로 수용
3. orphan, duplicate instance, self-parent, unresolved cycle, invalid grid/filter/bounds/overlap은 fail closed

Nested container 이동:

- container instance identity 유지
- descendants parent chain 유지
- 자신 또는 descendant 안으로 이동 금지
- destructive delete/carrier replacement는 subtree 전체 제거

Storage capacity summary는 accepted stored containers의 nested grids까지 포함한다.

## 6. Recursive assembly authority

v1.14.0부터 `FarmingGuideAssemblyPolicy`가 assembly mechanics의 Core authority다.

책임:

- deep attachment/armor tree traversal
- child slot mutation
- slot filter / allowed plate validation
- assembly-wide item conflict validation
- compatible candidate resolution
- required-slot recursion
- persisted assembly sanitization
- deterministic assembly signature

WPF event handler가 이 compatibility 의미를 별도로 재구현하지 않는다.

Installed attachment가 자체 slot을 가지면 workbench에서 해당 child로 들어가 하위 slot을 계속 편집할 수 있다. 상위 owner로 돌아가는 navigation도 in-page workbench 안에서 수행한다.

## 7. In-page workbench / compatible-item picker

별도 generic `장비 정보/장비 설정` OS Window와 read-only internal preview를 제품 interaction으로 사용하지 않는다.

Double-click은 가운데 in-page workbench를 연다. 오른쪽 item search는 계속 사용할 수 있다.

- stored backpack/rig/storage carrier → 실제 내부 storage grid
- top-level worn rig → main storage가 이미 보이면 actionable armor/mod slots
- weapon → actual attachment/mod slots
- helmet/body armor → actionable attachment / replaceable armor plate slots

One-item attachment/armor slot:

- current filter / allowed plate IDs / conflicts 검증
- occupied slot을 묵시적으로 overwrite하지 않음
- 기존 child를 먼저 제거한 뒤 새 child 장착

Empty slot single-click:

- 같은 Farming Guide page 안에 compatible-item icon picker 표시
- current full assembly state 기준 후보만 노출
- candidate single-click 즉시 장착
- 별도 OS/configuration dialog 없음
- 기존 search drag → slot drop과 동일 Core policy 공유

열린 workbench owner를 equipment/storage surface에서 이동하기 시작하면 workbench를 먼저 닫아 stale callback/write-back을 방지한다.

## 8. Assembly-aware image presentation

Image presentation은 assembly truth를 변경하지 않는다.

- current build membership이 authoritative imported default preset 구성과 정확히 일치할 때만 composed preset image를 사용한다.
- arbitrary/custom build는 base item image와 installed-part 표시를 이용한 deterministic fallback을 사용한다.
- exact preset membership을 증명할 수 없으면 composed preset image를 추측해 사용하지 않는다.

Source metadata:

- item `properties.defaultPreset`
- preset image links
- preset `containsItems`

## 9. Storage visual-layout authority

수납 **가능성**과 grid의 **화면상 상대 위치**는 별개다.

Mechanics authority:

```text
current validated Game Content
→ grid count / width / height / filters
```

Visual arrangement authority:

```text
product-owned verified FarmingGuideStorageVisualLayout metadata
```

Exact relative placement는 visual metadata의 expected grid count/width/height signature가 current live grid signature와 정확히 일치할 때만 적용한다.

Mismatch/unknown/stale case:

- old coordinates를 강제로 사용하지 않는다.
- finite deterministic compact layout으로 fallback한다.
- fallback을 authentic Tarkov layout으로 주장하지 않는다.

Importer는 `GridLayoutName`, `gridLayoutName`, `RigLayoutName`, `rigLayoutName` 계열 identity를 `StorageLayoutName`으로 보존한다.

Provenance/license가 확인되지 않은 외부 layout atlas 전체를 제품 source of truth로 포함하지 않는다. 현재 exact catalog는 검증된 최소 product-owned metadata만 사용한다.

## 10. Farming Guide drag / placement 계약

- item은 actual Tarkov `width × height` footprint 사용
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / current filter / contiguous-space 검증
- valid/invalid transient feedback
- mouse-up은 actual release point 기준 drop probe
- clipping ancestor/ScrollViewer 밖 offscreen target 선택 금지

Carrier contents 보호:

- contents가 있는 carrier를 다른 carrier로 묵시적으로 덮어쓰지 않는다.
- 안전한 이동 의미를 증명할 수 없으면 destructive replacement를 fail closed한다.

## 11. Secure Container compatibility

Current Tarkov source에서 Secure Container와 일반 storage case가 모두 `ItemPropertiesContainer`를 사용할 수 있으므로 전체 type을 secure로 간주하지 않는다.

- explicit secure-container / pouch semantics 우선
- fallback은 generic container/case classification이 없는 경우로 제한
- Epsilon/Gamma/Kappa 등 actual secure container 허용
- Medicine Case 같은 일반 case는 secure equipment slot에 장착 금지

## 12. Weapon preset search contract

Upstream item feed에는 base weapon과 `ItemPropertiesPreset` / `preset` assembled weapon records가 함께 존재할 수 있다.

Canonical Game Content record를 임의 병합/삭제하지 않는다. Farming Guide draggable search에서 assembled preset record만 제외한다.

- actual base weapon 보존
- 실제로 다른 Tarkov variant 보존
- base weapon actual mod slots가 workbench authority

## 13. Preset / persistence

Preset은 전체 raid-start working state를 보존한다.

- equipped item
- carrier
- attachment / armor plate tree
- stored item
- nested parent instance relationship
- grid index / row / column
- rotation

선택 preset 삭제는 saved entry만 제거하고 current working loadout은 유지한다.

사용자 상태 authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema: v1
```

Game Content와 Farming Guide user state를 분리한다. Program Update / Game Content Update가 Farming Guide user state를 덮어쓰지 않는다.

## 14. Game Content structure / schema

v1.14.0 canonical item structure는 필요한 범위에서 다음을 보존한다.

- width / height
- storage grids / allowed-blocked filters
- equipment / attachment slots
- slot IDs / names / required flags / filters
- replaceable armor slots / allowed plate IDs
- item conflicts
- default preset reference
- preset image links / contained item IDs
- optional storage layout identity

Schema:

```text
Content write: v10
Readable Content: v3~v10
Farming Guide user state: v1
```

Old readable snapshot에 새 optional assembly/layout structure가 없으면 해당 의미를 추측해 생성하지 않는다.

## 15. Scanner 유지 계약

- false positive보다 miss 선호
- OCR/matcher/candidate/recovery acceptance는 reviewed actual Tarkov evidence 없이 완화 금지
- recognition proof에 price/needed/source/relationship metadata 사용 금지
- scan-time network I/O를 identity proof에 추가하지 않음
- external screen pixels + OCR만 사용

사용하지 않음:

- game process memory read
- code/DLL injection
- process/game hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

Ground Truth는 explicit user-reviewed truth만 authoritative하다. correction hotkey는 evidence-only Saved Case를 저장하며 Ground Truth를 자동 생성하지 않는다.

Scanner needed quantity/source authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

## 16. Quest / Needed Items 계약

- exact ProfileVariable 값 최우선
- audited staged task-pool compatibility는 current structure가 증명되는 범위에서만 사용
- lower trader LL은 잠금 의미 유지
- current stage는 conservative reconstruction/fail-closed
- higher LL은 과거 stage threshold 충족 runtime-only effective floor로만 사용 가능
- hidden server counter의 exact fact로 저장 금지
- structural drift fail closed
- Future Needed Items / cleanup에 current Quest UI compatibility 낙관적 전파 금지

## 17. Hideout / Ammo 계약

Hideout:

- source `attributes.foundInRaid` 의미 보존
- FIR requirement에 non-FIR inventory 충당 금지

Ammo:

- same-caliber penetration + 현재 profile에서 증명된 direct purchase state 기준 pickup 판단
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매로 취급하지 않음
- Ammo Pack은 authoritative `containsItems` 관계 우선

## 18. Game Content lifecycle

```text
remote source
→ parse/import
→ schema/required semantics validation
→ canonical candidate
→ completeness / LKG guard
→ candidate DB
→ read-back/integrity validation
→ atomic active replacement
→ image prefetch
```

- candidate 완성 전 active overwrite 금지
- failed candidate 폐기
- healthy LKG 보존
- suspicious partial payload / unexplained shrink 차단
- source semantics가 불명확하면 fail closed
- user progress / Farming Guide user state / Ground Truth 수정 금지

## 19. Map / MiniMap

Pinned donor:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper first-party bridge가 제품 의미와 lifecycle/presentation ownership을 가진다.

유지 계약:

- Main Map selection → fresh/reused MiniMap synchronization
- player heading은 position과 동일 map affine transform 좌표계 사용
- PMC / Scav / Transit extract filters와 rendered marker 검증
- loaded marker data는 있는데 standard layer만 비는 bounded race 직접 복구
- Player Marker Size 변경은 unrelated presentation을 재초기화하지 않음
- Mini Scanner 우클릭 correction context menu 없음

## 20. Program Update / release immutability

- GitHub latest public stable release 사용
- user consent 없이 자동 교체 금지
- stable ZIP + checksum 검증
- staging/package-root 검증 전 current program files 변경 금지
- exact-main CI artifact가 Release workflow input
- public tag/source/assets는 immutable historical identity

Documentation-only main commit은 동일 assembly version의 다른 ProductVersion metadata bytes를 만들 수 있다. 이미 공개된 v1.14.0 asset을 교체하거나 historical source를 재정의하지 않는다. Release workflow는 이미 공개된 동일 version을 만나면 required immutable assets 존재만 확인하고 성공 종료해야 한다.

## 21. v1.14.0 release verification

Exact product source:

```text
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
```

통과한 gate:

- 527/527 deterministic tests
- Windows Release build / XAML compile
- self-contained win-x64 publish
- ProductVersion `1.14.0+9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`
- FIRST_RUN exact version identity
- actual published EXE Product UI / Farming Guide / Map smoke
- recursive assembly / compatible picker / exact multi-grid Canvas + GridDropTarget identity smoke
- graceful shutdown + clean portable root
- active async close Shutdown Race
- package/checksum equality
- Documentation Consistency
- Actions artifact digest verification
- automatic Release workflow
- public latest/tag/release/assets/digest readback

## 22. 외부 실사용 pending

자동화 검증과 별개로:

- 사용자의 실제 PC/Tarkov v1.14.0 최종 실사용 확인: `PENDING`
- 김태영 실제 PC diagnostic ZIP 수집/분석: `PENDING`

사용자 실사용에서 새 회귀가 보고되면 자동화가 녹색이어도 실제 증상을 우선 evidence로 취급한다.
