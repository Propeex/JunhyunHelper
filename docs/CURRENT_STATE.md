# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.13.3
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

Public package:

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

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9754610879
archive bytes: 241,795,611
archive SHA-256:
ae3fb9857920ab61e79c46da01d030fbded4a90eca27ec306e7f5661beb0cc3a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.3`, exact-main product source가 모두 `9a0064d81dca4c2cffcb01c55742d46298d235de`에 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## v1.13.3 핵심 변경 — Farming Guide live item interaction

v1.13.3은 v1.13.2 실사용에서 확인된 Farming Guide interaction 문제를 수정한 PATCH 릴리즈다.

- Secure Container는 current Tarkov `ItemPropertiesContainer` 구조를 지원하되 Medicine Case 같은 일반 container/case를 오인하지 않는다.
- stored item state에 nullable `ParentInstanceId`를 사용해 nested bag/rig storage를 표현한다.
- root → nested tree 순서 sanitize로 orphan, duplicate instance, self/cycle, invalid grid/filter/bounds/overlap을 fail closed한다.
- 별도 generic item configuration Window를 제거하고 가운데 in-page workbench를 사용한다.
- stored bag/rig double-click은 실제 내부 storage grid를 직접 조작한다.
- weapon/helmet/body armor는 actionable attachment/mod/replaceable armor plate slot을 one-item drag/drop target으로 표시한다.
- occupied attachment/plate slot은 묵시적으로 overwrite하지 않는다.
- nested container 이동은 descendants를 유지하고 destructive delete/replacement는 subtree를 함께 제거한다.
- Farming Guide 검색에서는 upstream assembled weapon preset만 제외하고 canonical base weapon의 실제 mod slots를 사용한다.
- 열린 workbench owner 이동 시 workbench를 먼저 닫아 stale write-back을 방지한다.

Canonical decision / architecture:

- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## Farming Guide 유지 계약

Farming Guide는 Scanner 오른쪽의 raid-start Loadout / Inventory Editor다.

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- nested storage parent-instance 관계 보존
- attachment / replaceable armor plate direct drag/drop
- 전체 출발 상태 preset save/load
- melee / PMC dogtag fixed setting 분리
- profile-aware standard/expanded pockets
- 총 무게 / 사용·전체 storage cell 표시
- filled carrier destructive replacement fail-closed
- old preset impossible placement current-content/profile sanitization

Loot 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 recommendation, 실제 raid inventory grid 좌표의 지속적인 1:1 동기화는 현재 범위가 아니다.

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호하며 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않는다.
- Scanner는 external screen pixels + OCR만 사용한다. game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass를 사용하지 않는다.
- Quest exact ProfileVariable은 runtime compatibility보다 우선하며 Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존한다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 actual published EXE runtime evidence로 검증한다.
- 공개 stable release bytes/tag/source는 immutable historical identity로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.13.3
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

Farming Guide 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 Game Content와 분리해 저장한다. v1.13.2 → v1.13.3 mandatory user data migration은 없다. 기존 schema-v1 JSON에서 missing `ParentInstanceId`는 null root placement로 호환된다.

## 검증 상태

Exact product source `9a0064d81dca4c2cffcb01c55742d46298d235de`은 다음을 통과했다.

- 513 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- Farming Guide live nested-storage / attachment-slot interaction smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- release package + checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload/digest verification
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## 남은 외부 실사용 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov에서 v1.13.3 최종 실사용 확인
- 김태영 실제 PC에서 diagnostic ZIP 수집/분석

공개 증거:

- `docs/RELEASE_1.13.3.md`
- `docs/.release-v1.13.3-status.json`
- `docs/RELEASE_NOTES_V1.13.3.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`

후속 documentation-only commit은 v1.13.3 product release source가 아니다. historical identity는 `9a0064d81dca4c2cffcb01c55742d46298d235de`에 고정한다.
