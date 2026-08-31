# RELEASE v1.13.2 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.13.2
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag `refs/tags/v1.13.2`, release `target_commitish`, GitHub `/releases/latest`, exact-main product source가 모두 `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9751114832
archive bytes: 241,785,937
archive SHA-256:
c4d146d46856f91f3dd489fe9a5d5eab7906cbcb05fe40dfd3966052872aba84
```

Release workflow는 위 exact-main CI artifact를 다운로드해 검증된 package/checksum을 그대로 공개했다. 공개용 제품을 별도로 다시 빌드하지 않았다.

## Public assets

```text
Junhyun-Helper.zip
asset id: 537701878
bytes: 80,617,300
SHA-256 / GitHub digest:
659071659531259a61d0996e277bf9643ee9fc4cfa8a0a437b4686994bd38bed

SHA256SUMS.txt
asset id: 537701880
bytes: 86
SHA-256 / GitHub digest:
0ebdc1240c721bf0192b703c77cfd944665f870edb7d79444dfd6181a2a43a19
```

Exact-main CI와 Release workflow에서 `SHA256SUMS.txt`의 `Junhyun-Helper.zip` 항목과 실제 ZIP SHA-256이 동일한 `659071659531259a61d0996e277bf9643ee9fc4cfa8a0a437b4686994bd38bed`임을 확인했다.

## Product changes

v1.13.2는 Farming Guide의 장비·수납·프리셋·내부 정보 UX를 보완하는 PATCH 릴리즈다.

- pistol / revolver / handgun 계열은 Holster 전용으로 판정하고 Primary Weapon 1/2에서 제외한다.
- body armor / rig / backpack / secure container compatibility를 canonical type/category 의미까지 사용하도록 보강했다.
- active profile edition과 Old Patterns 완료 여부로 pocket geometry를 결정한다.
  - standard: `1×1 / 1×1 / 1×1 / 1×1`
  - expanded: `1×1 / 1×2 / 1×2 / 1×1`
- pocket geometry를 UI, placement, persisted-state sanitization에서 동일하게 사용한다.
- storage order를 `Rig → Pockets + Special Slots → Backpack → Secure Container`로 정리했다.
- 장착 장비 및 search result double-click에서 storage grid / attachment / armor structure를 확인할 수 있다.
- 선택 preset 삭제를 추가하고 current working loadout은 보존한다.
- preset name dialog의 DPI/theme clipping을 수정했다.
- melee / PMC dogtag fixed behavior는 보존하고 화면의 `고정` 문구만 제거했다.

## Preserved contracts

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated storage grids / equipment slots / attachment slots / armor slots / conflicts 사용
- attachment / 교체형 armor plate 설정
- 전체 raid-start preset save/load
- melee / PMC dogtag fixed setting 분리
- filled carrier destructive replacement fail-closed
- impossible old preset placement current-content/profile sanitization

Loot 가치 판단, pickup/discard/replace 추천, Scanner 실시간 recommendation, 실제 raid inventory 좌표의 지속적인 1:1 동기화는 v1.13.2에도 포함하지 않는다.

## Regression coverage / release gate

Exact-main은 다음을 통과했다.

- 504 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained single-file publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- ZIP checksum manifest / actual hash equality
- exact-main Documentation Consistency
- exact-main Actions artifact upload
- automatic verified Release workflow
- public tag / latest release / assets / GitHub digest readback

## Public verification

- release workflow `33373940475`: SUCCESS
- `/releases/latest`: v1.13.2
- tag ref `refs/tags/v1.13.2`: exact product source와 일치
- release target: exact product source와 일치
- public ZIP asset metadata/digest: verified
- checksum asset metadata/digest: verified
- stable release: `draft=false`, `prerelease=false`

후속 documentation-only commit은 v1.13.2 product release source가 아니다. v1.13.2 historical product identity는 `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 고정한다.
