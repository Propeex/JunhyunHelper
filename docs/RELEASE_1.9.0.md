# RELEASE v1.9.0 — Public Evidence

상태: **PUBLIC STABLE / VERIFIED / IMMUTABLE PRODUCT RELEASE**

기준일: 2026-08-28 KST

## 제품 릴리즈 소스

```text
version: v1.9.0
exact product release source/tag target:
e0b0d303141563af564cd71cf00d8c1bfeafe44d
main CI run: 33165706386 — SUCCESS
release workflow run: 33165905504 — SUCCESS
release id: 378431058
432 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T11:08:59Z
```

Main-CI ProductVersion:

```text
1.9.0+e0b0d303141563af564cd71cf00d8c1bfeafe44d
```

`refs/tags/v1.9.0` readback은 위 exact product release source commit을 직접 가리킨다. GitHub `/releases/latest`의 `target_commitish`도 동일하며 v1.9.0이 현재 latest stable이다.

## 공개 패키지

Exact-main CI package:

```text
Junhyun-Helper.zip
bytes: 80,538,029
SHA-256:
9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44
```

Public GitHub Release asset readback:

```text
Junhyun-Helper.zip
asset id: 533681571
bytes: 80,538,029
digest:
sha256:9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44

SHA256SUMS.txt
asset id: 533681572
bytes: 86
digest:
sha256:2cd7157b4ebeaaa86fa73ee1eccbd1dedac8112089ad04994bd04228fcdcce32
```

공개 ZIP의 byte size와 digest는 exact-main CI에서 만든 release package와 정확히 일치한다.

## Main-CI Actions artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9683545225
artifact archive bytes: 241,545,444
artifact archive SHA-256:
098c74a99dc6d57c7a01b0e70c860c0d2925e6bbf4835ac2eacabf1f3e5d1bd8
```

Release workflow는 위 exact-main artifact를 run `33165706386`에서 다시 다운로드했고 artifact digest를 다시 확인한 뒤 release package checksum과 product identity를 검증하여 공개했다.

## Published executable runtime evidence

Exact-main CI는 self-contained single-file Windows x64 executable을 실제 실행했다.

Ammo animated dropdown:

```text
product-lifecycle=ok
ammo-caliber-runtime-template=ok
favorites-shared-template=ok
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok
shared-cycle-ms=700
```

Ammo toolbar:

```text
favorite-selector-left=ok
displayed-columns-visible=ok
displayed-columns-right-edge=ok
```

Map extract filters:

```text
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
minimap-refresh-handler-preserved=ok
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

동일 실행에서 Product UI, Main Map, Factory, MiniMap, graceful shutdown, clean portable root도 모두 성공했다.

## v1.9.0 제품 변경

### Scanner Favorites / Recents

- Scanner 아이템 상세에 즐겨찾기 별 버튼을 추가했다.
- 기존 사용자용 로그 영역을 Favorites 상단 약 2/3 + Recents 하단 약 1/3으로 교체했다.
- 두 목록은 독립적으로 세로 스크롤하고 가로 스크롤을 사용하지 않는다.
- 긴 이름은 말줄임표 처리한다.
- Favorites/Recents persistence는 canonical Item ID와 순서만 소유한다.
- 이름/아이콘/가격/필요 개수/관계 정보는 현재 GameMode 데이터에서 다시 resolve한다.
- Recents는 실제 상세를 열었을 때만 기록하며 newest-first, deduplicate/reopen-top, 최대 50개다.
- 개별 recent 삭제와 전체 삭제는 favorites와 독립적이다.

### Scanner navigation / search-detail separation

- 직접 검색, 관계 아이템, Favorites, Recents를 하나의 `OpenScannerItemDetails` 제품 경계로 통합했다.
- canonical 경계에서 기본 상세, 관계 presentation, favorite 상태, recent 기록을 일관되게 동기화한다.
- 검색어를 지우거나 popup을 닫아도 이미 열린 상세는 유지한다.
- 저장 목록의 이름/아이콘은 full relationship build 없이 경량 current-mode lookup으로 resolve한다.
- PvP/PvE profile 전환 시 Scanner가 꺼져 있어도 visible Favorites/Recents와 열린 상세를 current GameMode catalog에 맞춰 다시 resolve한다.
- GameMode 전환으로 자동 재렌더링할 때는 recent 순서를 변경하지 않는다.

### Map / Ammo regression fixes

- 지도 마커 선택 창에 donor의 실제 탈출구 master/PMC/SCAV/Transit checkbox를 복원했다.
- 기존 donor event handler, persistence, marker rendering, MiniMap refresh 의미를 유지한다.
- Ammo 구경/즐겨찾기 ComboBox는 동일 runtime icon template/state를 계속 공유하며 아이콘 순환 간격을 700 ms로 조정했다.

## 변경하지 않은 계약

- Scanner Item ID identity proof
- OCR threshold / matcher / candidate cap / visual recovery acceptance
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`
- Game Content candidate/LKG/50% completeness/fail-closed
- Content schema v8 및 v3~v8 read compatibility
- Map/MiniMap donor pin/ownership boundary

v1.9.0은 external Game Content importer/schema/validator 의미를 변경하지 않았으므로 새 live network release-readiness probe를 요구하지 않았다. 마지막 schema-affecting 공개 검증은 v1.8.4 release family에서 사용한 run `33151060959`이며 당시 Regular/PvE 모두 fatal 0이었다.

## 문서-only 후속 commit 주의

이 파일과 `STATE.md`, `CURRENT_STATE.md`, `README.md` 등을 공개 뒤 동기화하는 documentation-only commit은 **v1.9.0 product release source가 아니다**.

v1.9.0 product source/tag/public assets는 영구적으로 다음 identity를 기준으로 기록한다.

```text
e0b0d303141563af564cd71cf00d8c1bfeafe44d
```

이미 공개된 v1.9.0 tag/release/assets는 immutable historical product release로 취급하며 이후 documentation-only CI가 같은 assembly version으로 새 bytes를 만들더라도 교체하지 않는다.
