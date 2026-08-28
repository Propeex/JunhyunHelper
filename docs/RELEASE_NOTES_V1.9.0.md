# 준현 헬퍼 v1.9.0

## 요약

v1.9.0은 Scanner 아이템 정보 활용성을 확장하는 MINOR 릴리즈다. Scanner에 아이템 즐겨찾기와 최근 본 아이템을 추가하고, 검색 상태와 열린 상세 상태를 분리한다. 동시에 v1.8.x 실사용에서 확인된 Map 탈출구 필터와 Ammo 아이콘 순환 UI 회귀를 수정한다.

Scanner의 Item ID 판정, OCR threshold, matcher, candidate cap, visual recovery acceptance와 Game Content LKG/fail-closed 안전 계약은 변경하지 않는다.

## Scanner 즐겨찾기

- 아이템 상세 상단에서 별 버튼으로 즐겨찾기를 등록/해제한다.
- 오른쪽 영역의 상단 약 2/3을 즐겨찾기 목록으로 사용한다.
- 행은 아이콘, 아이템 이름, 즐겨찾기 해제 버튼으로 구성한다.
- 행 본문을 누르면 해당 아이템 상세를 연다.
- 이름이 길면 말줄임표로 처리하며 가로 스크롤은 사용하지 않는다.
- 즐겨찾기에는 canonical Item ID와 순서만 저장한다.
- 이름, 아이콘, 가격, 필요한 개수와 관계 정보는 현재 GameMode 데이터에서 다시 해석한다.

## 최근 본 아이템

- 실제 Scanner 아이템 상세를 열었을 때만 기록한다.
- 최신순이며 같은 Item ID를 다시 열면 중복 생성 없이 맨 위로 이동한다.
- 최대 50개를 유지한다.
- 개별 × 삭제와 `전부 삭제`를 지원한다.
- 최근 기록 삭제는 즐겨찾기에 영향을 주지 않는다.
- 오른쪽 영역의 하단 약 1/3을 사용하고 즐겨찾기와 독립적으로 세로 스크롤한다.
- 재시작 후에도 유지한다.

## Scanner navigation / 검색 상태

- 직접 검색 결과, 제작/교환 관련 아이템, 즐겨찾기, 최근 본 아이템을 포함한 모든 실제 item-open 경로를 하나의 product-owned boundary로 통합한다.
- 해당 경계에서 기본 상세, 관계 정보, 즐겨찾기 상태, recent 기록을 함께 동기화한다.
- 검색어를 지우면 검색 결과와 popup만 닫히며 현재 열려 있는 상세는 유지한다.
- 저장 목록의 이름/아이콘 resolve는 관계 그래프를 매번 생성하지 않는 경량 current-mode lookup을 사용한다.
- PvP/PvE 프로필 전환 시 Scanner가 꺼져 있어도 보이는 즐겨찾기/최근 목록과 열린 상세가 현재 GameMode catalog에 맞게 다시 해석된다.
- GameMode 전환에 따른 자동 상세 재렌더링은 recent 순서를 다시 올리지 않는다.

## 사용자용 Scanner 로그 제거

기존 오른쪽 `로그` 영역은 사용자 UI에서 제거한다. 내부 diagnostic activity/correction pipeline은 유지하며 화면에 노출하지 않는다.

## Map 탈출구 필터 회귀 수정

- 지도 마커 선택 영역에 탈출구 master, PMC, SCAV, Transit 필터를 복원한다.
- 별도 복제 checkbox가 아니라 donor의 실제 기존 controls를 제품 패널로 재배치한다.
- 기존 Checked/Unchecked handler, 설정 persistence, marker rendering, MiniMap refresh 의미를 유지한다.

## Ammo 드롭다운

- 구경 및 즐겨찾기 ComboBox는 동일한 runtime icon template/state를 계속 공유한다.
- 같은 구경의 탄약 아이콘 순환 간격을 700ms로 조정한다.
- filtering과 favorite persistence 의미는 변경하지 않는다.

## 최종 검증 및 공개 증거

최종 version-bump branch gate는 CI run `33165405209`, squash-merge 후 exact-main gate는 CI run `33165706386`에서 각각 성공했다.

Exact-main 제품 릴리즈 소스는 다음으로 고정한다.

```text
e0b0d303141563af564cd71cf00d8c1bfeafe44d
```

Exact-main CI 결과:

```text
432 passed / 0 failed / 0 skipped
Release build: SUCCESS
win-x64 self-contained single-file publish: SUCCESS
ProductVersion: 1.9.0+e0b0d303141563af564cd71cf00d8c1bfeafe44d
published EXE runtime smoke: SUCCESS
release package/checksum verification: SUCCESS
```

실제 published executable evidence:

```text
Ammo:
product-lifecycle=ok
ammo-caliber-runtime-template=ok
favorites-shared-template=ok
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok
shared-cycle-ms=700

Map:
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
minimap-refresh-handler-preserved=ok

Scanner detail:
product-lifecycle=ok
canonical-open-boundary=ok
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok

Scanner Favorites / Recents:
search-clear-detail=ok
favorite-toggle-persistence=ok
recent-open-persistence=ok
right-pane-two-to-one=ok
independent-scroll=ok
user-log-pane-hidden=ok
canonical-item-id=ok
```

동일 실행에서 Product UI, Main Map, Factory, MiniMap, graceful shutdown, clean portable root도 성공했다.

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9683545225
archive bytes: 241,545,444
archive SHA-256:
098c74a99dc6d57c7a01b0e70c860c0d2925e6bbf4835ac2eacabf1f3e5d1bd8
```

공개 release workflow run `33165905504`는 위 exact-main artifact를 다시 다운로드해 digest, ProductVersion, FIRST_RUN 및 package checksum을 검증한 뒤 v1.9.0을 stable/latest로 게시했다.

```text
release id: 378431058
tag: v1.9.0
target: e0b0d303141563af564cd71cf00d8c1bfeafe44d
draft: false
prerelease: false

Junhyun-Helper.zip
asset id: 533681571
bytes: 80,538,029
SHA-256: 9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44

SHA256SUMS.txt
asset id: 533681572
bytes: 86
SHA-256: 2cd7157b4ebeaaa86fa73ee1eccbd1dedac8112089ad04994bd04228fcdcce32
```

공개 ZIP의 byte size와 digest는 exact-main CI package와 정확히 일치하며 `refs/tags/v1.9.0`과 GitHub latest release target도 동일한 product release source를 가리킨다.

v1.9.0은 external Game Content importer/schema/validator 의미를 변경하지 않았으므로 새 network live release-readiness probe는 필요하지 않았다. 마지막 schema-affecting 공개 검증은 run `33151060959`이며 당시 Regular/PvE 모두 fatal 0이었다.

공개 증거는 `docs/RELEASE_1.9.0.md`, `docs/.release-v1.9.0-status.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`에 기록한다. 이들 문서를 공개 뒤 동기화하는 documentation-only commit은 v1.9.0 제품 릴리즈 소스가 아니며, 이미 공개된 tag/release/assets는 immutable historical product artifact로 취급한다.