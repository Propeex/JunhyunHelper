# RELEASE v1.10.0 — PUBLIC EVIDENCE

기준일: 2026-08-29 KST

상태: **PUBLIC STABLE / VERIFIED**

## 제품 소스

```text
version: v1.10.0
exact product release source/tag target:
a99540c4ae450f9f1995e5378919ae57f41ba930
```

이 SHA는 PR #217 squash merge 결과이며 v1.10.0 제품 바이너리·태그·공개 자산의 immutable 기준이다. 이후 documentation-only commit은 이 제품 source가 아니다.

## Exact-main CI

```text
CI run: 33201929209 — SUCCESS
job: 98953240853
439 passed / 0 failed / 0 skipped
ProductVersion:
1.10.0+a99540c4ae450f9f1995e5378919ae57f41ba930
published EXE bytes: 84,118,613
published files: 32
```

MiniMap rendered runtime evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

동일 MiniMap Window에서 A SVG render → hide → visible Main Map B → same Window show → actual `MapSvg.Source` B 교체를 검증했다.

같은 exact-main published EXE에서 Ammo, Map extract filters, Main Map/Factory/MiniMap, Scanner detail, Favorites/Recents, v1.9.1 detail actions, graceful shutdown, clean portable root가 성공했다.

## Exact-main package

```text
Junhyun-Helper.zip
bytes: 80,543,064
SHA-256:
65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639
```

Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9698177979
archive bytes: 241,564,056
archive SHA-256:
72f42c6b507105ae5fb1dd20c597996d906a47a50c149a9ad3d197178e52d0c6
```

## Public release

```text
Release workflow: 33202187186 — SUCCESS
job: 98954112113
release id: 378705187
tag: v1.10.0
published UTC: 2026-08-28T19:04:46Z
draft: false
prerelease: false
latest stable: true
```

`target_commitish`와 `refs/tags/v1.10.0` object가 모두 exact product source `a99540c4ae450f9f1995e5378919ae57f41ba930`을 직접 가리키는 것을 readback했다.

Public assets:

```text
Junhyun-Helper.zip
asset id: 534229631
bytes: 80,543,064
SHA-256:
65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639

SHA256SUMS.txt
asset id: 534229630
bytes: 86
SHA-256:
1c6fc4e5ecf9009d2eef3891f92748dd2d91ebdace2e4fc1f0c9876e4c00a832
```

공개 ZIP의 bytes/SHA-256은 exact-main CI package와 정확히 일치한다.

## v1.10.0 제품 변경

- Main Map A→B 후 MiniMap 신규 표시/재표시 첫 visible frame을 B로 동기화.
- donor hidden loaded MiniMap Window 재사용 경로 보강.
- Mini Scanner에 `플리마켓 최저가` 추가.
- 해당 행 표시/숨김, 순서 변경, persistence 지원.
- 기존 v6 Mini Scanner 행 순서를 유지하며 새 field를 한 번만 추가.
- Scanner display settings schema v7.
- Scanner catalog cache v1~v4 readable / v4 written.
- `lastLowPrice`는 Item ID 확정 뒤 presentation-only 데이터이며 recognition identity에는 사용하지 않음.
- scan-time network I/O 추가 없음.

## 비변경 계약

Scanner OCR threshold/matcher/candidate cap/visual recovery acceptance, Scanner canonical Item ID authority, Ground Truth/capture geometry, Game Content v8 LKG/completeness/fail-closed, Favorites/Recents, Ammo, Factory floor, Map marker/filter 의미는 변경하지 않았다.
