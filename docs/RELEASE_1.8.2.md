# 준현 헬퍼 v1.8.2 공개 릴리즈 증거

상태: **PUBLIC STABLE / VERIFIED**

## 릴리즈 목적

v1.8.2는 v1.8.1 공개 이후 확인된 두 가지 실사용 회귀를 수정하는 유지보수 PATCH다.

1. v1.7.15에서 추가한 Ammo 구경/즐겨찾기 드롭다운 아이콘 UI가 published executable에서 WPF 타입 초기화 순서에 따라 실제 적용되지 않을 수 있던 문제를 제거했다.
2. 현재 json.tarkov.dev가 제공하는 Bitcoin Farm passive production 및 canonical-identical trader direct-purchase 중복 offer를 현재 제품의 canonical relationship 계약에 맞게 정규화했다.

기존 Game Content candidate/LKG fail-closed 계약, 관계 무결성/completeness guard, Scanner recognition acceptance, Map/MiniMap donor 경계는 변경하지 않았다.

## Exact product source

```text
version: v1.8.2
Desktop version: 1.8.2
exact main/product source:
a0a8390c7c863400a97d174e864c405c2e38f47f

tag ref target:
a0a8390c7c863400a97d174e864c405c2e38f47f
```

공개 release target과 `refs/tags/v1.8.2`가 모두 위 exact source를 가리키는 것을 readback으로 확인했다.

## Live source verification

현재 Regular/PvE json.tarkov.dev source는 기능 코드가 확정된 다음 source에서 검증했다.

```text
functional source:
718a095cadebce1c50ff04caecfcccfa95e7bdb8
live probe workflow run: 33136464802
result: SUCCESS
Regular fatal validation issues: 0
PvE fatal validation issues: 0
```

양 모드에서 확인된 현재 upstream shape:

```text
craft count: 214
empty-required craft count: 1

Bitcoin passive production identity:
craft:   5d5c205bd582a50d042a3c0e
station: 5d494a445b56502f18c98a10
product: 59faff1d86f7746c51718c9c

item count: 5312
canonical-identical direct-purchase duplicate keys: 4 per mode
```

위 live probe 뒤 제품 동작 코드에는 변경이 없었고, 이후 커밋은 v1.8.2 버전/패키징/릴리즈 문서 정리였다.

## Exact-main CI

```text
CI run id: 33138083383
CI run number: 2103
result: SUCCESS
head branch: main
head SHA: a0a8390c7c863400a97d174e864c405c2e38f47f
```

검증 결과:

```text
Release build: SUCCESS
421 passed / 0 failed / 0 skipped
win-x64 self-contained single-file publish: SUCCESS
ProductVersion: 1.8.2+a0a8390c7c863400a97d174e864c405c2e38f47f
FIRST_RUN identity: SUCCESS
actual rendered Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
Ammo caliber/favorites rendered icon + shared timer-cycle smoke: SUCCESS
graceful shutdown: SUCCESS
clean portable root: SUCCESS
release package/checksum verification: SUCCESS
```

Main-CI package:

```text
Junhyun-Helper.zip
bytes: 80,520,794
SHA-256:
be83ec72d1678b2496e01ce4378708642e0bf0cc00cebeb407fa38756ecf1f0a
```

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9672853577
archive bytes: 241,494,074
archive SHA-256:
9586f8a277b54bf64446c8348ff4958ed33f4ab94f16fc8c33fe3baf41c4b6fc
```

## Release workflow

```text
Release workflow run id: 33138226890
Release workflow run number: 53
result: SUCCESS
source main SHA: a0a8390c7c863400a97d174e864c405c2e38f47f
```

Release workflow는 exact-main CI artifact를 내려받아 ProductVersion/FIRST_RUN/checksum을 다시 검증한 뒤 stable GitHub release를 게시했다.

## Public release readback

```text
release id: 378240417
tag: v1.8.2
release target: a0a8390c7c863400a97d174e864c405c2e38f47f
draft: false
prerelease: false
latest stable: true
published UTC: 2026-08-28T03:13:04Z
```

Public ZIP:

```text
asset: Junhyun-Helper.zip
asset id: 533189452
bytes: 80,520,794
SHA-256:
be83ec72d1678b2496e01ce4378708642e0bf0cc00cebeb407fa38756ecf1f0a
```

Public checksum manifest:

```text
asset: SHA256SUMS.txt
asset id: 533189451
bytes: 86
asset SHA-256:
73ae27b7d11be8db2fd1119c78e1326ebdad7cd5a5c3982436620f463fa8e45e
```

GitHub release asset metadata의 ZIP digest가 exact-main CI package SHA-256과 일치한다.

## v1.8.2 수정 계약

### Ammo runtime UI

- `AmmoPage`의 runtime class-handler 등록을 명시적인 type initialization 경계에서 결정적으로 수행한다.
- 구경 ComboBox와 즐겨찾기 ComboBox가 동일한 runtime icon template/state를 사용한다.
- legacy favorites menu는 runtime polish 뒤 비표시/비활성 상태를 유지한다.
- published executable smoke가 실제 rendered `Image` / `Image.Source` / geometry 및 shared icon timer-cycle을 검증한다.
- 즐겨찾기 저장 및 구경 filtering 의미는 변경하지 않는다.

### Live Game Content relationship normalization

- audited Bitcoin Farm passive production identity만 일반 craft relationship import에서 제외한다.
- 그 외 empty-required craft는 계속 fail closed한다.
- canonical model 기준으로 완전히 동일한 trader direct-purchase record만 deduplicate한다.
- 가격/화폐/trader/LL/quest unlock/buy limit 등 의미 필드가 다른 offer는 별도 관계로 유지한다.

### 유지되는 안전 계약

- Game Content candidate/LKG 분리
- relationship reference/price/count/limit integrity validation
- relation/material-edge 50% completeness floor
- fresh v8+ critical relationship collection empty fail-closed
- persisted candidate read-back 및 activation/active recovery revalidation
- v3~v7 legacy relationship-null compatibility
- Scanner OCR threshold/candidate cap/matcher/visual recovery acceptance
- Map/MiniMap pinned donor revision 및 ownership boundary

## 불변성

이 문서와 이후 documentation-only commit은 **v1.8.2 product release source가 아니다**.

v1.8.2 제품 source/tag/public assets는 다음 exact source 기준으로 고정한다.

```text
a0a8390c7c863400a97d174e864c405c2e38f47f
```

동일 `1.8.2` 버전에서 이후 문서 commit 때문에 ProductVersion metadata나 package bytes가 달라져도 이미 공개된 v1.8.2 assets를 교체하지 않는다.
