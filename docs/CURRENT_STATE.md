# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.13.2
exact product release source/tag target:
207cb948affc091c4ad67f18d7e4e4382b2f8125
PR: #245 — MERGED
validated PR head: ef4522880218b5e5ec8d8c0a8a3211e0f0c51020
PR exact-head CI: 33373322410 — SUCCESS
PR exact-head Shutdown Race CI: 33373322440 — SUCCESS
PR exact-head Documentation Consistency: 33373322395 — SUCCESS
exact-main CI: 33373612303 — SUCCESS
exact-main Shutdown Race CI: 33373612281 — SUCCESS
exact-main Documentation Consistency: 33373612283 — SUCCESS
release workflow: 33373940475 — SUCCESS
release id: 379612102
published UTC: 2026-08-31T08:40:02Z
504 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537701878
bytes: 80,617,300
SHA-256:
659071659531259a61d0996e277bf9643ee9fc4cfa8a0a437b4686994bd38bed

SHA256SUMS.txt
asset id: 537701880
bytes: 86
asset SHA-256:
0ebdc1240c721bf0192b703c77cfd944665f870edb7d79444dfd6181a2a43a19
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9751114832
archive bytes: 241,785,937
archive SHA-256:
c4d146d46856f91f3dd489fe9a5d5eab7906cbcb05fe40dfd3966052872aba84
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.2`, exact-main product source가 모두 `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## v1.13.2 핵심 변경 — Farming Guide 장비·수납·프리셋 보완

v1.13.2는 v1.13.0/v1.13.1 Farming Guide의 제품 의미와 데이터 계약을 유지하는 PATCH 릴리즈다.

- pistol / revolver / handgun 계열은 Holster 전용이며 Primary Weapon 1/2에서 제외한다.
- body armor / rig / backpack / secure container compatibility를 canonical type/category 의미로 보강했다.
- 활성 profile의 edition과 Old Patterns 완료 여부에 따라 pocket geometry를 결정한다.
  - standard: `1×1 / 1×1 / 1×1 / 1×1`
  - expanded: `1×1 / 1×2 / 1×2 / 1×1`
- resolved pocket geometry를 UI, placement, persisted-state sanitization에 공통 사용한다.
- storage presentation 순서는 `Rig → Pockets + Special Slots → Backpack → Secure Container`이며 Pockets는 좌측, Special Slots는 우측이다.
- equipped item 및 search result double-click으로 actual storage grid / attachment / armor structure를 확인할 수 있다.
- preset delete를 추가했으며 삭제해도 current working loadout은 유지한다.
- preset name dialog의 DPI/theme clipping을 수정했다.
- melee / PMC dogtag fixed lifecycle은 유지하고 `고정` 문구만 제거했다.

## Farming Guide 유지 계약

Farming Guide는 Scanner 오른쪽의 raid-start Loadout / Inventory Editor다.

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- attachment / 교체형 armor plate 설정
- 전체 출발 상태 preset save/load
- melee / PMC dogtag fixed setting 분리
- 총 무게 / 사용·전체 storage cell 표시
- filled carrier destructive replacement fail-closed
- old preset impossible placement current-content sanitization

Loot 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 recommendation, 실제 raid inventory grid 좌표의 지속적인 1:1 동기화는 현재 범위가 아니다.

Canonical product decision:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

Architecture/maintenance contract:

- `docs/ARCHITECTURE_FARMING_GUIDE.md`

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
Desktop target version: 1.13.2
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

Farming Guide 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 Game Content와 분리해 저장한다. v1.13.1 → v1.13.2 mandatory user data migration은 없다.

## 검증 상태

Exact product source `207cb948affc091c4ad67f18d7e4e4382b2f8125`은 다음을 통과했다.

- 504 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- release package + checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## 남은 외부 실사용 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov에서 v1.13.2 최종 실사용 확인
- 김태영 실제 PC에서 diagnostic ZIP 수집/분석

공개 증거:

- `docs/RELEASE_1.13.2.md`
- `docs/.release-v1.13.2-status.json`
- `docs/RELEASE_NOTES_V1.13.2.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

후속 documentation-only commit은 v1.13.2 product release source가 아니다. historical identity는 `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 고정한다.
