# 준현 헬퍼 v1.8.1 공개 릴리즈 증거

상태: **PUBLIC STABLE / VERIFIED**

## 릴리즈 목적

v1.8.1은 v1.8.0 Scanner 아이템 정보 DB에서 발견된 Game Content 관계 데이터 LKG completeness 공백을 닫는 유지보수 PATCH다.

공개 v1.8.0 source/tag/assets는 교체하지 않았으며 immutable historical release로 유지한다.

## Exact product source

```text
version: v1.8.1
Desktop version: 1.8.1
exact main/product source:
dade2ef4dadbf58659b75c80d421bd3738003ff8

tag ref target:
dade2ef4dadbf58659b75c80d421bd3738003ff8
```

공개 release target과 `refs/tags/v1.8.1`이 모두 위 exact source를 가리키는 것을 readback으로 확인했다.

## Exact-main CI

```text
CI run id: 33132600931
CI run number: 2083
result: SUCCESS
head branch: main
head SHA: dade2ef4dadbf58659b75c80d421bd3738003ff8
```

검증 결과:

```text
Release build: SUCCESS
418 passed / 0 failed / 0 skipped
win-x64 self-contained single-file publish: SUCCESS
ProductVersion: 1.8.1+dade2ef4dadbf58659b75c80d421bd3738003ff8
FIRST_RUN identity: SUCCESS
actual Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
clean portable root: SUCCESS
release package/checksum verification: SUCCESS
```

Main-CI package:

```text
Junhyun-Helper.zip
bytes: 80,520,704
SHA-256:
b30cbb045cc089c90108e2d3394510ef6778019ea0a50f6ae16d14de7aaafe9a
```

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9670880711
archive bytes: 241,492,261
archive SHA-256:
8fe5db31d3a728bb74049308319db6f2f7fba72a39c64eb0bd5d98c4433c93da
```

## Release workflow

```text
Release workflow run id: 33132798167
Release workflow run number: 51
result: SUCCESS
source main SHA: dade2ef4dadbf58659b75c80d421bd3738003ff8
```

Release workflow는 exact-main CI artifact를 내려받아 ProductVersion/FIRST_RUN/checksum을 다시 검증한 뒤 stable GitHub release를 게시했다.

## Public release readback

```text
release id: 378212009
tag: v1.8.1
release target: dade2ef4dadbf58659b75c80d421bd3738003ff8
draft: false
prerelease: false
latest stable: true
published UTC: 2026-08-28T01:25:35Z
```

Public ZIP:

```text
asset: Junhyun-Helper.zip
asset id: 533094287
bytes: 80,520,704
SHA-256:
b30cbb045cc089c90108e2d3394510ef6778019ea0a50f6ae16d14de7aaafe9a
```

Public checksum manifest:

```text
asset: SHA256SUMS.txt
asset id: 533094286
bytes: 86
asset SHA-256:
ff45756d4f90b5852a5e85b7ec648a98e4a33000cccaeb6ec13658e22892c6d6
```

GitHub release asset metadata의 ZIP digest가 exact-main CI package SHA-256과 일치한다.

## v1.8.1 수정 계약

- healthy v8+ baseline의 trader purchase / barter / craft / flea 관계에 50% retained-floor 적용
- barter/craft required-item edge도 별도 retained-floor 적용
- present-but-empty v8+ 관계 graph의 critical relation collection은 fail closed
- v3~v7 legacy `ItemRelationshipData == null` 의미 유지
- persisted candidate read-back에서 base + item relationship integrity + completeness 재검증
- activation 및 active recovery boundary에서도 item relationship integrity 검증
- Scanner OCR/recognition acceptance policy 변경 없음
- Map/MiniMap donor revision 변경 없음

## 불변성

이 문서와 이후 documentation-only commit은 **v1.8.1 product release source가 아니다**.

v1.8.1 제품 source/tag/public assets는 다음 exact source 기준으로 고정한다.

```text
dade2ef4dadbf58659b75c80d421bd3738003ff8
```

동일 `1.8.1` 버전에서 이후 문서 commit 때문에 ProductVersion metadata나 package bytes가 달라져도 이미 공개된 v1.8.1 assets를 교체하지 않는다.
