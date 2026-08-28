# RELEASE v1.9.1 — Public Evidence

상태: **PUBLIC STABLE / VERIFIED / IMMUTABLE PRODUCT RELEASE**

기준일: 2026-08-29 KST

## 제품 릴리즈 소스

```text
version: v1.9.1
exact product release source/tag target:
723760910ff250a515ed8db456d3f045656ecacb
main CI run: 33184811972 — SUCCESS
release workflow run: 33185056113 — SUCCESS
release id: 378579142
435 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T15:26:04Z
```

Main-CI ProductVersion:

```text
1.9.1+723760910ff250a515ed8db456d3f045656ecacb
```

`refs/tags/v1.9.1`은 위 exact product release source commit을 직접 가리킨다. GitHub `/releases/latest`의 `target_commitish`도 동일하며 v1.9.1이 latest stable이다.

## 공개 패키지

Exact-main CI package:

```text
Junhyun-Helper.zip
bytes: 80,540,488
SHA-256:
7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
```

Public GitHub Release asset readback:

```text
Junhyun-Helper.zip
asset id: 533982952
bytes: 80,540,488
digest:
sha256:7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54

SHA256SUMS.txt
asset id: 533982951
bytes: 86
digest:
sha256:1a98310d28f954c36f400a69f9b6c546bc22137ebbef95bb52991bfff02de431
```

공개 ZIP의 byte size와 digest는 exact-main CI package와 정확히 일치한다.

## Main-CI Actions artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9691310332
artifact archive bytes: 241,554,536
artifact archive SHA-256:
e4ac36ef6968f10b8a5b03c1f8e73a95e308e96f19b65d40d7144c87bcee51b7
```

Release workflow는 exact-main CI artifact를 받아 checksum/product identity를 다시 검증한 뒤 공개했다.

## Published executable runtime evidence

Exact-main CI는 self-contained single-file Windows x64 executable을 실제 실행했다.

Scanner v1.9.1 detail actions:

```text
favorite-wiki-height=34
favorite-symbol-font=ok
favorite-content-centered=ok
wiki-content-centered=ok
```

Map extract filters:

```text
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
hidden-master-render-gate=ok
approved-three-filter-layout=ok
minimap-refresh-handler-preserved=ok
pmc-filter-render-state=ok
scav-filter-render-state=ok
transit-filter-render-state=ok
```

Main Map / MiniMap selection sync:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
```

Scanner item detail:

```text
product-lifecycle=ok
canonical-open-boundary=ok
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok
```

Scanner Favorites / Recents:

```text
search-clear-detail=ok
favorite-toggle-persistence=ok
recent-open-persistence=ok
right-pane-two-to-one=ok
independent-scroll=ok
user-log-pane-hidden=ok
canonical-item-id=ok
```

동일 실행에서 Ammo runtime, Product UI, Main Map, Factory, MiniMap, graceful shutdown, clean portable root도 모두 성공했다.

## v1.9.1 제품 변경

- Scanner 상세의 즐겨찾기 별 버튼을 Wiki 버튼과 34px로 정렬하고 실제 Render 시점 검증을 고정했다.
- 지도 `탈출구` 그룹은 donor의 실제 PMC / Scav / Transit 체크박스 정확히 세 개만 사용자에게 표시한다.
- donor master extract checkbox는 숨겨진 internal render gate로 유지한다.
- Main Map의 visible map selection을 MiniMap 초기화 전에 shared `MapTrackerService`에 동기화하고 이미 열린 MiniMap에도 즉시 반영한다.
- Scanner OCR/matcher/candidate cap/visual recovery/Ground Truth, Map marker 의미, Factory 층 처리, Game Content LKG/fail-closed 의미는 변경하지 않았다.

## 외부 데이터 검증

v1.9.1은 external Game Content importer/schema/validator 의미를 변경하지 않았으므로 새 live network release-readiness probe를 요구하지 않았다. 마지막 schema-affecting 공개 검증은 run `33151060959`이며 Regular/PvE 모두 fatal 0이었다.

## 문서-only 후속 commit 주의

이 파일과 `STATE.md`, `CURRENT_STATE.md`, `README.md`, `DECISIONS.md` 등을 공개 뒤 동기화하는 documentation-only commit은 **v1.9.1 product release source가 아니다**.

v1.9.1 product source/tag/public assets는 영구적으로 다음 identity를 기준으로 기록한다.

```text
723760910ff250a515ed8db456d3f045656ecacb
```
