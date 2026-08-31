# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 구현·검증 계약은 `docs/STATE.md`, 진행 중 작업 여부는 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**

상태: **`v1.11.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.11.4
exact product release source/tag target:
f9d3497004241ea80193e5a0d242e7219cf04f2a
PR: #236 — MERGED
superseded draft PR: #235 — CLOSED / NOT MERGED
final feature head: 84b56e81171543e289ed417d822c40c9d607d4d3
PR exact-head CI: 33345630940 — SUCCESS
PR exact-head Shutdown Race CI: 33345630896 — SUCCESS
PR exact-head Documentation Consistency: 33345630871 — SUCCESS
exact-main CI: 33345851673 — SUCCESS
exact-main Shutdown Race CI: 33345851704 — SUCCESS
exact-main Documentation Consistency: 33345851658 — SUCCESS
release workflow: 33346020525 — SUCCESS
release id: 379449740
published UTC: 2026-08-31T00:56:10Z
478 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537252429
bytes: 80,564,330
SHA-256:
99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643

SHA256SUMS.txt
asset id: 537252430
bytes: 86
asset SHA-256:
6b81b3816b63b49999e225244214f3d2a3eeabc67fa88da2dd38542c0969f092
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9741999225
archive bytes: 241,626,166
archive SHA-256:
0af92581d315e2e69d7ff319f1c9968e52fa0093d8635db0eec894e954e2a450
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.11.4`, exact-main product source가 모두 `f9d3497004241ea80193e5a0d242e7219cf04f2a`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이며 Release workflow는 exact-main CI artifact를 다운로드해 package manifest/actual ZIP hash를 검증한 뒤 공개했다. 공개 ZIP의 GitHub asset digest도 같은 `99ad5d7ce75bc5211edf79a6e80c93b666489bb4a47f4358b2ece70c183f2643`이다.

공개 증거:

- `docs/RELEASE_1.11.4.md`
- `docs/.release-v1.11.4-status.json`
- `docs/RELEASE_NOTES_V1.11.4.md`

## v1.11.4 핵심 변경

### MiniMap 최초 표시 지도 동기화

- Main Map 실제 selection 변경 시 product tracker/registry를 동기적으로 먼저 갱신한다.
- 같은 input turn에 MiniMap을 처음 만들어도 이전 map을 first visible frame에 표시하지 않는다.
- queued reconciliation도 유지해 이후 donor state를 다시 맞춘다.
- fresh first-create와 reused MiniMap 모두 runtime smoke에서 검증한다.

### MiniMap extract / standard marker lifecycle

- PMC / Scav / Transit extract filter와 실제 rendered marker를 검증한다.
- Transit은 packaged data의 예상 grouped extract 수와 실제 MiniMap Transit visual 수를 비교한다.
- donor async refresh cancellation으로 표시 대상 data는 있는데 standard layer만 비는 경우 another refresh를 시작하지 않고 loaded marker DB에서 layer를 직접 복구한다.
- deliberate all-hidden state나 무한 retry는 만들지 않는다.

### Player Marker Size 격리

- Player Marker Size는 MiniMap player marker `PlayerMarkerScale`만 변경한다.
- Name Size / MiniMap Marker Size / 일반·퀘스트·탈출구 marker presentation을 whole-view refresh로 다시 덮지 않는다.

### Mini Scanner

- 우클릭 `현재 결과 교정` context menu를 제거했다.
- left-drag, topmost, recognition/result display, `교정 데이터 추가` 전역 hotkey evidence 저장 계약은 유지한다.

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
| Map + MiniMap | 구현 완료 / first-create sync + marker lifecycle repair |
| Game Content Update | 구현 완료 / LKG + FIR + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner correction zoom | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.11.4
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.11.3 → v1.11.4 mandatory Game Content migration: none
v1.11.3 → v1.11.4 user.db migration: none
v1.11.3 → v1.11.4 Scanner display settings migration: none
```

## 검증 상태

v1.11.4 exact product source `f9d3497004241ea80193e5a0d242e7219cf04f2a`은 다음을 모두 통과했다.

- 478 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- first MiniMap creation synchronization
- actual Transit marker rendering
- standard marker direct recovery
- Player Marker Size isolation
- Mini Scanner context-menu absence
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback
- public ZIP digest = verified exact-main package hash

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.11.4 최종 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

현재 `docs/ACTIVE_WORK.md`는 `NONE`이다. v1.11.4 릴리즈 배치에 남은 제품 개발 작업은 없다. 새 사용자 요구사항, 실사용 회귀, 또는 Tarkov 변화가 확인되면 현재 stable 기준으로 필요한 범위만 수정한다.

이 문서와 이후 documentation-only commit은 v1.11.4 product release source가 아니다. v1.11.4 product source/tag/assets는 `f9d3497004241ea80193e5a0d242e7219cf04f2a`에 고정한다.
