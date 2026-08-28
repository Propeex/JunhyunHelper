# RELEASE v1.8.4 — Public Evidence

상태: **PUBLIC STABLE / VERIFIED / IMMUTABLE PRODUCT RELEASE**

기준일: 2026-08-28 KST

## 제품 릴리즈 소스

```text
version: v1.8.4
exact product release source/tag target:
13af4e3a452139dedc32b2db9aa51266e2a01d2a
main CI run: 33153043430 — SUCCESS
release workflow run: 33153234911 — SUCCESS
release id: 378333813
424 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T07:55:12Z
```

Main-CI ProductVersion:

```text
1.8.4+13af4e3a452139dedc32b2db9aa51266e2a01d2a
```

`refs/tags/v1.8.4` readback도 같은 commit을 직접 가리킨다. GitHub `/releases/latest`의 `target_commitish` 역시 동일하다.

## 공개 패키지

Exact-main CI package:

```text
Junhyun-Helper.zip
bytes: 80,528,868
SHA-256:
9e06c16e20a346ad7691dccfee9a2caebcdb6c0cd9a6a35859bcb97d8e03fa42
```

Public GitHub Release asset readback:

```text
Junhyun-Helper.zip
asset id: 533461834
bytes: 80,528,868
digest:
sha256:9e06c16e20a346ad7691dccfee9a2caebcdb6c0cd9a6a35859bcb97d8e03fa42

SHA256SUMS.txt
asset id: 533461832
bytes: 86
digest:
sha256:535514fb48f23e1fe7834ba0cd5be54235f15922d036f5ad071c829ff80b4aad
```

공개 ZIP의 byte size와 digest는 exact-main CI에서 만든 release package와 정확히 일치한다.

## Main-CI Actions artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9678543773
artifact archive bytes: 241,519,179
artifact archive SHA-256:
e966c849652f7408bba868e920ffd45a622bf195309385ea04aad6f1f8758bf0
```

Release workflow는 위 exact-main artifact를 다시 다운로드했고 같은 Actions artifact digest를 확인한 뒤 공개 package checksum/identity를 재검증했다.

## Published executable runtime evidence

Exact-main CI는 self-contained single-file Windows x64 executable을 실제 실행했다.

Ammo animated dropdown:

```text
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok
```

Ammo toolbar:

```text
favorite-selector-left=ok
displayed-columns-visible=ok
displayed-columns-right-edge=ok
```

Scanner item detail:

```text
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok
```

동일 실행에서 Product UI, Main Map, Factory, MiniMap smoke와 graceful shutdown, clean portable root도 모두 성공했다.

## Current live Game Content release-readiness

공개 직전 현재 `json.tarkov.dev` Regular/PvE를 production canonical pipeline으로 각각 검증했다.

```text
live probe run: 33151060959 — SUCCESS

Regular:
items=5312
quests=517
objectives=1457
questItems=305
hideout=26
ammo=200
validationIssues=0
fatal=0

PvE:
items=5312
quests=514
objectives=1434
questItems=293
hideout=26
ammo=200
validationIssues=0
fatal=0
```

각 mode의 `sourceWarnings=1`은 현재 Tarkov Wiki Ballistics coverage warning이며 canonical validation failure가 아니다.

## v1.8.4 제품 변경

- Ammo 즐겨찾기 선택을 의도한 왼쪽 선택 영역에 유지.
- `표시 열` 버튼을 툴바 오른쪽 끝에 유지.
- 기존 구경/즐겨찾기 shared animated icon state/timing 유지.
- Scanner item detail을 기본 정보 → 사용처 → 수급처의 단일 세로 흐름으로 정리.
- 기본 정보는 크기 / 플리 평균가 / 최고 상인 판매가 / 현재 필요한 개수 네 항목.
- craft/barter를 결과 item과 전체 materials가 함께 보이는 recipe card로 표시.
- 좁은 폭에서 material row가 자연스럽게 wrapping.
- 관련 item click은 같은 Scanner item detail로 이동.
- 수급처는 제작 / 교환 / 구매 / 레이드 획득으로 구분하며 empty group은 숨김.

## 변경하지 않은 계약

- Scanner Item ID proof
- OCR threshold / matcher / candidate cap / visual recovery acceptance
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Quest/Hideout needed-source authority
- Game Content candidate/LKG/50% completeness/fail-closed
- v3~v7 legacy snapshot compatibility
- Map/MiniMap donor pin/ownership boundary

## 문서-only 후속 commit 주의

이 파일과 `STATE.md`를 공개 뒤 동기화하는 후속 documentation-only main commit은 **v1.8.4 product release source가 아니다**.

v1.8.4 product source/tag/public asset는 영구적으로 다음 identity를 기준으로 기록한다.

```text
13af4e3a452139dedc32b2db9aa51266e2a01d2a
```

이미 공개된 v1.8.4 tag/release/assets는 immutable historical product release로 취급하며 documentation-only CI가 새 bytes를 만들더라도 교체하지 않는다.
