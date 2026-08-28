# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-29 KST

상태: **`v1.9.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.9.1
exact product release source/tag target: 723760910ff250a515ed8db456d3f045656ecacb
main CI run: 33184811972 — SUCCESS
release workflow run: 33185056113 — SUCCESS
release id: 378579142
stable asset: Junhyun-Helper.zip
stable asset id: 533982952
stable bytes: 80,540,488
stable SHA-256: 7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
checksum asset id: 533982951
checksum asset SHA-256: 1a98310d28f954c36f400a69f9b6c546bc22137ebbef95bb52991bfff02de431
435 passed / 0 failed / 0 skipped
```

Main-CI ProductVersion:

```text
1.9.1+723760910ff250a515ed8db456d3f045656ecacb
```

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9691310332
archive bytes: 241,554,536
archive SHA-256: e4ac36ef6968f10b8a5b03c1f8e73a95e308e96f19b65d40d7144c87bcee51b7
```

`/releases/latest`와 `refs/tags/v1.9.1` readback에서 release target/tag ref가 exact product release source와 일치하고 `draft=false`, `prerelease=false`, latest stable임을 확인했다. 공개 ZIP bytes/digest도 exact-main CI package와 동일하다.

공개 증거:

- `docs/RELEASE_1.9.1.md`
- `docs/.release-v1.9.1-status.json`
- `docs/RELEASE_NOTES_V1.9.1.md`
- `docs/DECISION_V1.9.1_FINAL_UI_MINIMAP.md`
- `docs/RELEASE_1.9.0.md` — 이전 Scanner Favorites/Recents 릴리즈

## v1.9.1 runtime 계약

Scanner detail:

```text
favorite-wiki-height=34
favorite-symbol-font=ok
favorite-content-centered=ok
wiki-content-centered=ok
```

Map:

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

MiniMap sync:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
```

같은 published EXE에서 Scanner detail/Favorites/Recents, Ammo, Product UI, Main Map, Factory, MiniMap, graceful shutdown, clean portable root가 성공했다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 |
| Items / Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / v1.9.1 selection sync verified |
| Game Content Update | 구현 완료 / relationship LKG + fail-closed 유지 |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.9.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

## 유지되는 핵심 계약

- Scanner false positive보다 miss 선호.
- OCR/matcher/candidate cap/visual recovery acceptance는 reviewed evidence 없이 완화하지 않는다.
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- Scanner source = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`.
- Game Content candidate/LKG/completeness/fail-closed를 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF lifecycle 변경은 source assertion이 아니라 actual published EXE runtime evidence로 검증한다.

v1.9.1은 external Game Content importer/schema/validator 의미를 변경하지 않았으므로 새 network live probe를 요구하지 않았다. 마지막 schema-affecting run `33151060959`에서 Regular/PvE fatal은 모두 0이었다.

## 다음 작업

현재 v1.9.1 릴리즈 배치에 남은 제품 개발 작업은 없다. 기본 운영 모드는 유지보수이며, 새 기능은 사용자가 명시적으로 새 제품 요구사항으로 결정할 때만 시작한다.

이 문서와 이후 documentation-only commit은 v1.9.1 product release source가 아니다. v1.9.1 product source/tag/assets는 `723760910ff250a515ed8db456d3f045656ecacb`에 고정한다.
