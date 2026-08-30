# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 구현·검증 계약은 `docs/STATE.md`, 진행 중 작업 여부는 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**

상태: **`v1.11.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.11.3
exact product release source/tag target:
043abad38f4c3ebc9101463a162614ef67df7536
PR: #234 — MERGED
superseded draft PR: #233 — CLOSED / NOT MERGED
PR exact-head CI: 33319386444 — SUCCESS
PR exact-head Shutdown Race CI: 33319386465 — SUCCESS
PR exact-head Documentation Consistency: 33319386455 — SUCCESS
exact-main CI: 33319592093 — SUCCESS
exact-main Shutdown Race CI: 33319592115 — SUCCESS
exact-main Documentation Consistency: 33319592111 — SUCCESS
release workflow: 33319769016 — SUCCESS
release id: 379321405
published UTC: 2026-08-30T15:29:47Z
474 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536758239
bytes: 80,558,970
SHA-256:
e43892ecafc9920a7e3b7295f94b8a5324865977028b3573437d8ff7de4f327e

SHA256SUMS.txt
asset id: 536758240
bytes: 86
asset SHA-256:
5b3cc0468ad6a11076b547883fbd16d1276c74bc51779251c0c3421a070d63c3
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9734538554
archive bytes: 241,607,396
archive SHA-256:
cf10ab86f31c44dff00414b9f4e47ff9bf5a64df18210084bd2b41c42e3ac2a7
```

GitHub `/releases/latest`, release target, `refs/tags/v1.11.3`, exact-main product source가 모두 `043abad38f4c3ebc9101463a162614ef67df7536`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이며 Release workflow는 exact-main CI artifact를 다운로드해 검증한 뒤 공개했다.

공개 증거:

- `docs/RELEASE_1.11.3.md`
- `docs/.release-v1.11.3-status.json`
- `docs/RELEASE_NOTES_V1.11.3.md`

## v1.11.3 핵심 변경

### Items / Hideout 검색 clear lifecycle

- canonical `ProductSearchClearButtonBehavior`는 실제 Items/Hideout page lifecycle에서 attach된다.
- empty query → `×` hidden, typed query → inline `×` visible, click → clear + focus restore 계약을 유지한다.
- v1.11.2 published smoke가 behavior를 직접 attach해 실사용 회귀를 숨길 수 있던 false-positive 경로를 제거했다.

### Map 지도 마커 패널

- expanded panel은 content-sized popup이 아니라 available-height viewport로 동작한다.
- 정상적인 큰 창에서 하단 탈출구 항목이 잘리지 않는다.
- 실제 content overflow가 있을 때만 inner `ScrollViewer`가 vertical scrollbar를 표시한다.
- actual published EXE smoke가 panel height, viewport body fill, rendered overflow와 computed scrollbar state를 검증한다.

### Scanner 교정 이미지 zoom

- 교정 screenshot/ROI에서 마우스 휠 확대/축소를 지원한다.
- 확대된 이미지는 scroll/pan으로 확인할 수 있다.
- source image/canvas 크기는 원본 pixel coordinate를 유지하므로 Ground Truth 및 직접 지정 좌표의 저장 의미가 바뀌지 않는다.
- 최초 runtime smoke에서 Auto scrollbar로 fit scale이 0.573 → 0.596으로 달라지는 상태 의존성을 검출했고 stable arranged control bounds 기준으로 수정했다.

### Scanner correction evidence timing

사용자 diagnostics/calibration batch에서 저장된 case는 `NOT_RUN`이었지만 runtime log상 실제 OCR/matcher가 수행된 case가 확인됐다. 분석 완료 frame 뒤의 geometry-only capture가 single latest debug frame을 덮어써 correction save에 의미 있는 semantics가 유실되는 timing defect였다.

v1.11.3은 correction snapshot에 한해서 다음 조건에서만 직전 analyzed semantics를 보존한다.

- 동일 non-empty title signature
- 동일 capture mode
- 3초 이내

최신 screenshot/geometry는 그대로 유지하며, 이 carry는 live recognition 결정에 사용하지 않는다. OCR/matcher/candidate acceptance threshold는 완화하지 않았다.

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호한다.
- OCR/matcher/candidate acceptance는 reviewed actual Tarkov evidence 없이 임의 완화하지 않는다.
- Scanner recognition은 external screen pixels + OCR만 사용한다.
- game process memory read / injection / process hook / kernel / input automation / network manipulation / anti-cheat bypass를 사용하지 않는다.
- correction hotkey는 Ground Truth를 생성·추측하지 않는다.
- Hideout requirement의 `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- Ammo pickup은 same-caliber penetration 및 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- barter/craft/flea/higher-LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems`를 우선한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime evidence로 검증한다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 / maintenance |
| Items / Ammo | 구현 완료 / profile-aware pickup 포함 |
| Map + MiniMap | 구현 완료 / lifecycle repair + marker panel + heading projection |
| Game Content Update | 구현 완료 / LKG + FIR + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner correction zoom | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.11.3
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.11.2 → v1.11.3 mandatory Game Content migration: none
v1.11.2 → v1.11.3 user.db migration: none
v1.11.2 → v1.11.3 Scanner display settings migration: none
```

## 검증 상태

v1.11.3 exact product source `043abad38f4c3ebc9101463a162614ef67df7536`은 다음을 모두 통과했다.

- 474 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout lifecycle-attached inline search clear runtime validation
- Map marker panel available-height + real-overflow scrollbar runtime validation
- Scanner correction zoom + stable fit + source-pixel coordinate runtime validation
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.11.3 최종 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

현재 `docs/ACTIVE_WORK.md`는 `NONE`이다. v1.11.3 릴리즈 배치에 남은 제품 개발 작업은 없다. 새 사용자 요구사항, 실사용 회귀, 또는 Tarkov 변화가 확인되면 현재 stable 기준으로 필요한 범위만 수정한다.

이 문서와 이후 documentation-only commit은 v1.11.3 product release source가 아니다. v1.11.3 product source/tag/assets는 `043abad38f4c3ebc9101463a162614ef67df7536`에 고정한다.
