# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-29 KST

상태: **`v1.10.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.10.0
exact product release source/tag target: a99540c4ae450f9f1995e5378919ae57f41ba930
main CI run: 33201929209 — SUCCESS
release workflow run: 33202187186 — SUCCESS
release id: 378705187
published UTC: 2026-08-28T19:04:46Z
stable asset: Junhyun-Helper.zip
stable asset id: 534229631
stable bytes: 80,543,064
stable SHA-256: 65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639
checksum asset id: 534229630
checksum asset SHA-256: 1c6fc4e5ecf9009d2eef3891f92748dd2d91ebdace2e4fc1f0c9876e4c00a832
439 passed / 0 failed / 0 skipped
```

Main-CI ProductVersion:

```text
1.10.0+a99540c4ae450f9f1995e5378919ae57f41ba930
```

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9698177979
archive bytes: 241,564,056
archive SHA-256: 72f42c6b507105ae5fb1dd20c597996d906a47a50c149a9ad3d197178e52d0c6
```

`/releases/latest`와 `refs/tags/v1.10.0` readback에서 release target/tag ref가 exact product release source와 일치하고 `draft=false`, `prerelease=false`, latest stable임을 확인했다. 공개 ZIP bytes/digest도 exact-main CI package와 동일하다.

공개 증거:

- `docs/RELEASE_1.10.0.md`
- `docs/.release-v1.10.0-status.json`
- `docs/RELEASE_NOTES_V1.10.0.md`
- `docs/DECISION_V1.10.0_MINIMAP_REOPEN_MINISCANNER_FLEA_MINIMUM.md`

## v1.10.0 핵심 변경

### MiniMap

v1.9.1에서 놓친 donor `Hide()` → 동일 loaded Window `Show()` 재사용 경로를 수정했다. Main Map을 A에서 B로 바꾸고 MiniMap을 새로 열거나 다시 표시할 때 첫 visible frame부터 B를 사용한다. 이미 열린 MiniMap의 후속 지도 변경 동기화도 유지한다.

Exact-main published EXE runtime evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

마지막 두 marker는 실제 동일 MiniMap 창에서 A SVG를 렌더한 뒤 hide → Main Map B 선택 → same Window show → 실제 `MapSvg.Source`가 B로 교체된 경우에만 기록된다.

### Mini Scanner 플리마켓 최저가

- Scanner catalog의 `lastLowPrice`를 Item ID 확정 뒤 presentation-only metadata로 사용한다.
- Mini Scanner에 `플리마켓 최저가` 행을 추가했다.
- 다른 정보 행과 동일하게 표시/숨김 및 순서 변경을 지원한다.
- 기존 v6 사용자 순서는 보존하고 새 행만 정확히 한 번 추가한다.
- Scanner display settings schema는 v7.
- Scanner catalog cache는 v1~v4 readable, v4 written.
- 기존 v1~v3 cache는 오프라인 인식용으로 읽을 수 있으나 온라인 가능 시 새 market field를 받도록 stale 처리한다.
- scan-time network I/O와 Scanner identity/recognition 기준은 변경하지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 |
| Items / Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / v1.10.0 reopen + rendered selection sync verified |
| Game Content Update | 구현 완료 / relationship LKG + fail-closed 유지 |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.10.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

## 유지되는 핵심 계약

- Scanner false positive보다 miss 선호.
- OCR/matcher/candidate cap/visual recovery acceptance는 reviewed evidence 없이 완화하지 않는다.
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- Scanner source = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`.
- price/needed/source/relationship metadata는 Item ID proof에 사용하지 않는다.
- Game Content candidate/LKG/completeness/fail-closed를 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- Factory floor/marker 의미는 변경하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion이 아니라 actual published EXE runtime evidence로 검증한다.

## 다음 작업

v1.10.0 릴리즈 배치에 남은 제품 개발 작업은 없다. 기본 운영 모드는 유지보수이며, 새 기능은 사용자가 명시적으로 새 제품 요구사항으로 결정할 때만 시작한다.

이 문서와 이후 documentation-only commit은 v1.10.0 product release source가 아니다. v1.10.0 product source/tag/assets는 `a99540c4ae450f9f1995e5378919ae57f41ba930`에 고정한다.
