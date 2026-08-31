# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 `AGENTS.md` → `docs/PROJECT_STATE.json` → `docs/ACTIVE_WORK.md` 순으로 복구한 뒤 이 문서를 읽습니다. 대화 기억이 아니라 저장소의 공식 문서, 코드, 테스트, GitHub 상태가 기준입니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품과 운영 상태

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 공개 stable은 **v1.13.3**이다. v1.13.0의 Farming Guide raid-start Loadout / Inventory Editor를 v1.13.1~v1.13.2에서 실사용 기준으로 보완했고, v1.13.3에서 실제 Tarkov inventory interaction과 어긋나던 nested storage / secure container / attachment workbench / weapon preset search 회귀를 수정했다.

v1.13.3 구현·검증·병합·공개 릴리즈와 release evidence 기록이 완료됐다. 기본 운영 모드는 다시 유지보수다. `docs/ACTIVE_WORK.md`가 `NONE`이면 현재 복구할 개발 작업이 없다.

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
version: v1.13.3
exact product release source/tag target:
9a0064d81dca4c2cffcb01c55742d46298d235de
PR: #248 — MERGED
validated PR head: b39f7156f458fd6fd513b5eca551e522d5a12343
PR exact-head CI: 33382678094 — SUCCESS
PR exact-head Shutdown Race CI: 33382678096 — SUCCESS
PR exact-head Documentation Consistency: 33382678065 — SUCCESS
exact-main CI: 33382979766 — SUCCESS
exact-main Shutdown Race CI: 33382979902 — SUCCESS
exact-main Documentation Consistency: 33382979845 — SUCCESS
release workflow: 33383407835 — SUCCESS
release id: 379676479
published UTC: 2026-08-31T10:40:13Z
513 passed / 0 failed / 0 skipped
```

Public release package:

```text
Junhyun-Helper.zip
asset id: 537835859
bytes: 80,620,064
SHA-256:
704afb5e376f9087dd57c1795d8b95397c06a020acd9545fe80c5fc1b546b7b7

SHA256SUMS.txt
asset id: 537835858
bytes: 86
asset SHA-256:
2c74d9c4e4f096c35eb3b4e45deb734af5b9df31306c9961d66c9aa7cd4e5b4d
```

Exact-main artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9754610879
archive bytes: 241,795,611
archive SHA-256:
ae3fb9857920ab61e79c46da01d030fbded4a90eca27ec306e7f5661beb0cc3a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.3`, exact-main source가 모두 `9a0064d81dca4c2cffcb01c55742d46298d235de`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

공식 공개 증거:

- `docs/RELEASE_1.13.3.md`
- `docs/.release-v1.13.3-status.json`
- `docs/RELEASE_NOTES_V1.13.3.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only main commit은 v1.13.3 product release source가 아니다. historical product identity는 위 source/tag/assets에 고정한다.

## 3. Farming Guide 제품 의미

파밍 가이드는 Scanner 오른쪽의 first-class section이며, 제품 목적은 **레이드 시작 상태를 구성하는 Loadout / Inventory Editor**다.

이 기능은 실제 인게임 inventory 좌표를 지속적으로 1:1 mirror하는 시스템이 아니다. 사용자가 출발 장비, carrier, 점유 공간과 내부 상태를 구성하고 preset으로 저장/복원하는 제품 surface다.

현재 포함하지 않는 것:

- loot 가치 판단
- 무엇을 주울지 추천
- 무엇을 버릴지 추천
- 장비/아이템 교체 추천
- Scanner 실시간 추천 연동
- 실제 raid inventory 좌표의 지속적인 1:1 동기화

기본 제품 경계는 `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`, v1.13.3 interaction supersession은 `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`가 authority다.

## 4. Farming Guide equipment / storage 모델

Equipment는 현재 제품 설계에 필요한 raid-start 장비 슬롯을 표현한다.

- Headset
- Helmet / headwear
- Face cover / eyewear
- Body armor / armored rig
- Armband
- Primary Weapon 1 / 2
- Holster sidearm
- Melee
- PMC dogtag fixed setting

Melee와 PMC dogtag는 레이드마다 반복 입력하는 대상이 아니므로 per-profile preset과 분리된 fixed setting이다.

Top-level storage surface:

- Pockets
- Rig
- Backpack
- Secure Container
- Special Slots

Carrier의 실제 grid 구성은 current validated Game Content에서 읽는다. item은 실제 Tarkov `width × height` footprint로 렌더링한다.

Pocket geometry는 active profile을 기준으로 중앙 정책에서 결정한다.

```text
standard: 1×1 / 1×1 / 1×1 / 1×1
expanded: 1×1 / 1×2 / 1×2 / 1×1
```

Expanded pocket eligibility는 제품이 증명 가능한 edition 특전 또는 Old Patterns 완료 상태에서 해석하며 UI와 persisted-state sanitization이 같은 resolved geometry를 소비한다.

## 5. v1.13.3 nested storage 계약

`FarmingGuideStoredItemState`의 `ParentInstanceId`가 storage surface identity를 보존한다.

- `ParentInstanceId == null`: top-level Pockets/Rig/Backpack/Secure/Special surface
- non-null: 특정 stored container instance 내부 grid

기존 schema-v1 저장 파일에는 이 필드가 없으므로 deserialize 시 null root placement가 되어 backward compatible하다. 이 변경만으로 user-state schema를 올리지 않는다.

Load/sanitize 순서:

1. root placement를 current carrier/grid/filter/bounds/overlap 기준으로 검증
2. accepted parent가 증명된 nested placement만 단계적으로 수용
3. orphan, duplicate instance, self-parent, unresolved cycle, invalid grid/filter/bounds/overlap은 fail closed

Nested container 이동:

- container instance identity는 유지한다.
- descendants의 parent chain은 유지한다.
- 자신 또는 자신의 descendant 안으로 들어가는 cycle을 허용하지 않는다.
- destructive delete/carrier replacement는 subtree 전체를 함께 제거해 orphan을 만들지 않는다.

Storage capacity summary는 현재 장착된 top-level carrier뿐 아니라 accepted stored containers의 nested grids도 포함한다.

## 6. Farming Guide interaction / workbench 계약

v1.13.3부터 generic `장비 정보/장비 설정` 별도 Window와 read-only internal-grid preview를 제품 interaction으로 사용하지 않는다.

Double-click은 가운데 in-page workbench를 연다. 오른쪽 item search는 계속 사용할 수 있다.

아이템 유형/위치에 따라 실제 필요한 surface만 노출한다.

- stored backpack / stored rig / stored storage carrier → 실제 내부 storage grid
- worn/top-level rig → main inventory에 storage grid가 이미 보이므로 actionable armor/mod slots
- weapon → actual attachment/mod slots
- helmet/body armor → actionable attachment / replaceable armor-plate slots
- backpack/secure container → actual storage grid

Attachment/armor-plate slot:

- one-item drop target
- current filter / allowed plate IDs / item conflicts 검증
- occupied slot을 묵시적으로 overwrite하지 않음
- 기존 child를 먼저 drag-out하고 새 child를 넣음

열린 workbench의 owner item을 장비/수납 surface에서 이동하기 시작하면 workbench를 먼저 닫는다. 이는 old owner callback이 이동 후 stale state에 write-back하는 것을 방지한다.

## 7. Farming Guide drag / placement 계약

새 아이템 추가는 오른쪽 검색 결과에서 시작한다.

- drag preview는 실제 footprint를 사용한다.
- drag 중 `R`로 90도 회전한다.
- grid 주변에는 bounded snap tolerance를 사용한다.
- bounds / overlap / current filter / contiguous-space를 검증한다.
- 유효/불가 상태를 시각적으로 구분한다.
- mouse-up에서 cached target만 신뢰하지 않고 actual release point를 기준으로 drop probe를 결정한다.
- clipping ancestor/ScrollViewer 밖의 offscreen target을 geometry fallback으로 선택하지 않는다.

Carrier contents 보호:

- contents가 있는 carrier를 다른 carrier로 묵시적으로 덮어써 내부 상태를 잃게 하지 않는다.
- 안전한 이동 의미를 증명할 수 없으면 destructive replacement를 fail closed한다.

## 8. Secure Container compatibility

Current Tarkov source에서 Secure Container와 일반 storage case가 모두 `ItemPropertiesContainer`를 사용할 수 있다.

따라서 `ItemPropertiesContainer` 전체를 Secure Container로 간주하지 않는다.

- explicit Secure Container / pouch semantics를 우선한다.
- current secure-container fallback은 generic container/case classification이 없는 경우로 제한한다.
- Epsilon/Gamma/Kappa 같은 secure container는 허용한다.
- Medicine Case 같은 일반 case는 secure equipment slot에 장착되지 않는다.

이 판정은 deterministic regression test로 고정돼 있다.

## 9. Weapon preset search contract

Upstream item feed에는 실제 base weapon 외에 `ItemPropertiesPreset` / `preset` assembled weapon records가 함께 존재할 수 있다.

Canonical Game Content importer의 ID/record를 합치거나 삭제하지 않는다. Farming Guide item-search policy에서만 assembled preset record를 draggable inventory item에서 제외한다.

그 결과:

- 동일 weapon preset recipe가 base weapon처럼 여러 번 검색 노출되지 않는다.
- actual base weapon은 그대로 유지된다.
- Glock 등 base weapon의 current `slots`가 workbench source가 된다.

## 10. Farming Guide preset / persistence

Preset은 전체 raid-start working state를 보존한다.

- equipped item
- carrier
- attachment
- armor plate
- stored item
- nested parent instance relationship
- grid index / row / column
- rotation

저장된 preset을 선택하면 전체 상태를 복원한다. 불러온 상태를 수정하면 원본 preset 선택 상태를 해제한다.

선택 preset 삭제는 saved preset entry만 제거하고 current working loadout은 유지한다.

사용자 상태 authority:

```text
%LocalAppData%/JunhyunHelper/farming-guide.json
schema: v1
```

Game Content와 Farming Guide user state를 분리한다. Program Update / Game Content Update가 Farming Guide user state를 덮어쓰지 않는다.

## 11. Farming Guide Game Content 구조

Current validated item source에서 다음 optional structure를 canonical content에 보존한다.

- item width / height
- storage grids
- grid allowed/blocked filters
- equipment / attachment slots
- armor plate slots
- item conflicts
- headphone-blocking 등 editor compatibility에 필요한 current structure

Content write schema는 v9이며 v3~v9를 읽는다. Source field가 없거나 importer가 구조를 이해하지 못하면 해당 의미를 추측하지 않는다.

## 12. Scanner 유지 계약

- false positive보다 miss를 선호한다.
- OCR/matcher/candidate/recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- recognition proof에 price/needed/source/relationship metadata를 사용하지 않는다.
- scan-time network I/O를 identity proof에 추가하지 않는다.
- recognition은 external screen pixels + OCR만 사용한다.

사용하지 않음:

- game process memory read
- code/DLL injection
- process/game hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

Ground Truth는 explicit user-reviewed truth만 authoritative하다. correction hotkey는 evidence-only Saved Case를 저장하며 Ground Truth를 자동 생성/추측하지 않는다.

Scanner 필요 수량/source는 `ItemsWorkspace.Plan.NeededItems` authority를 사용하고 Scanner presentation이 이를 재계산하지 않는다.

## 13. Quest / Needed Items 계약

- exact ProfileVariable 값은 항상 최우선이다.
- audited staged task-pool compatibility는 current structure가 확인되는 범위에서만 사용한다.
- current trader LL이 audited stage보다 낮으면 잠금 의미를 유지한다.
- current stage는 보수적 reconstruction / fail-closed를 유지한다.
- current trader LL이 audited stage보다 높으면 과거 stage threshold가 충족됐다는 runtime-only effective floor를 사용할 수 있다.
- 이 floor를 hidden server counter의 exact fact로 저장하지 않는다.
- structural drift는 fail closed한다.
- Future Needed Items / cleanup은 current Quest UI compatibility를 낙관적으로 전파하지 않는다.

## 14. Hideout / Ammo 계약

Hideout:

- source `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.

Ammo:

- pickup 판단은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

## 15. Game Content Update 계약

```text
remote/current source
→ candidate build/import
→ schema + semantic + relationship validation
→ completeness / LKG guard
→ candidate content.db
→ read-back/integrity
→ atomic active promotion
```

- candidate 완성 전 active overwrite 금지
- failed candidate는 current known-good를 변경하지 않음
- source 의미/structure drift가 불명확하면 fail closed
- user progress / Farming Guide / Scanner reviewed GT를 content update와 함께 초기화하지 않음
- external Live Data Probe는 hermetic PR/main CI와 별도 contract monitor

## 16. Map / MiniMap 계약

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper first-party bridge가 selection/lifecycle/presentation 의미를 소유한다.

- Main Map selection은 fresh/reused MiniMap에 동기화된다.
- player heading은 position과 동일한 map별 affine transform 좌표계를 사용한다.
- PMC / Scav / Transit extract filter와 실제 rendered marker를 검증한다.
- loaded marker data는 있는데 standard layer만 비는 bounded empty-layer race는 direct recovery한다.
- Player Marker Size 변경은 unrelated Map/MiniMap presentation setting을 재초기화하지 않는다.
- Mini Scanner 우클릭 correction context menu는 제거 상태를 유지한다.

## 17. Program Update / release 계약

Program Update와 Game Content Update는 서로 다른 lifecycle이다.

- GitHub latest public stable release를 사용한다.
- 사용자 동의 없이 program file을 자동 교체하지 않는다.
- ZIP/checksum 검증 완료 전 current installation을 변경하지 않는다.
- Release workflow는 exact-main CI에서 검증된 Actions artifact를 소비한다.
- public tag/source/assets가 exact product source와 일치해야 한다.
- 공개 stable release는 immutable historical identity로 취급한다.
- 후속 docs-only main build가 같은 assembly version으로 다른 commit metadata/bytes를 만들더라도 기존 v1.13.3 asset을 교체하지 않는다.

Stable package:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

## 18. v1.13.3 검증 증거

Exact-main source `9a0064d81dca4c2cffcb01c55742d46298d235de`:

- 513 passed / 0 failed / 0 skipped
- Windows Release build / XAML compile: SUCCESS
- Windows x64 self-contained publish: SUCCESS
- ProductVersion: `1.13.3+9a0064d81dca4c2cffcb01c55742d46298d235de`
- actual published EXE Product UI / Farming Guide / Map smoke: SUCCESS
- Farming Guide live nested-storage / attachment-slot interaction smoke: SUCCESS
- graceful shutdown / clean portable root: SUCCESS
- Shutdown Race: SUCCESS
- Documentation Consistency: SUCCESS
- package/checksum audit: SUCCESS
- exact-main Actions artifact upload/digest: SUCCESS
- Release workflow `33383407835`: SUCCESS
- public latest/tag/release/asset digest readback: VERIFIED

## 19. Schema / compatibility

```text
Desktop version: 1.13.3
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.13.2 → v1.13.3 mandatory user data migration은 없다.

## 20. 현재 남은 외부 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov v1.13.3 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집 및 분석

이 항목들은 공개 release 무결성 실패가 아니라 실제 환경 evidence 수집 항목이다.
