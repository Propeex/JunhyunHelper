# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 구현·검증 계약은 `docs/STATE.md`, 진행 중 작업 여부는 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-30 KST**

상태: **`v1.11.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.11.2
exact product release source/tag target:
5822757f6490ec82aab33793752e48de14490628
PR: #232 — MERGED
superseded draft PR: #231 — CLOSED / NOT MERGED
PR exact-head CI: 33307979144 — SUCCESS
exact-main CI: 33308162829 — SUCCESS
exact-main Shutdown Race CI: 33308162797 — SUCCESS
exact-main Documentation Consistency: 33308162850 — SUCCESS
release workflow: 33308291656 — SUCCESS
release id: 379257951
published UTC: 2026-08-30T11:11:52Z
470 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536514791
bytes: 80,554,866
SHA-256:
d013ac2d423d2a83c49e1e6483dcad038a3792a5b865c1400085fd56e25592a9

SHA256SUMS.txt
asset id: 536514792
bytes: 86
asset SHA-256:
4860aceab06843707951dcd50951a62843d40ef7a2ea2a9d8efa7972847aa657
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9731167378
archive bytes: 241,597,223
archive SHA-256:
5eef3f620d46f3ac3c7990ec18fdcf46877741fc2c1647a856b3accb2fa26c8b
```

GitHub `/releases/latest`, release target, `refs/tags/v1.11.2`, exact-main product source가 모두 `5822757f6490ec82aab33793752e48de14490628`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이며 Release workflow는 exact-main CI artifact를 검증한 뒤 공개했다.

공개 증거:

- `docs/RELEASE_1.11.2.md`
- `docs/.release-v1.11.2-status.json`
- `docs/RELEASE_NOTES_V1.11.2.md`

## v1.11.2 핵심 변경

### Scanner 교정 데이터 hotkey

- `교정 데이터 추가` 전역 단축키는 레이드 중 **capture/save 전용**으로 동작한다.
- evidence가 있으면 기존 Saved Case 형식으로 저장하고 Mini Scanner에 `저장 완료`를 잠시 표시한다.
- 저장 성공 후 Saved Cases/교정 데이터 창을 자동으로 열지 않는다.
- Main Window 또는 Scanner 탭으로 focus를 강제로 이동하지 않는다.
- no-evidence 상태, evidence-only Saved Case, no automatic Ground Truth, duplicate explicit save 계약은 유지한다.

### Items / Hideout 검색 clear

- v1.11.1에서 중복 삽입된 always-visible 별도 `×` 버튼을 제거했다.
- Quest/Items/Hideout가 product-owned canonical conditional inline clear behavior를 공유한다.
- 검색어가 비어 있으면 `×`는 숨겨지고 텍스트가 있을 때만 표시된다.
- clear 후 기존 TextChanged 검색/필터 경로를 사용하며 검색창 focus를 복구한다.

### Map / MiniMap player heading

- screenshot player 위치는 기존처럼 맵별 `playerMarkerTransform` affine 변환을 사용한다.
- v1.11.1까지 heading은 같은 좌표계 변환을 일관되게 반영하지 않아 Factory MiniMap 등에서 약 90° 오차가 날 수 있었다.
- v1.11.2는 위치 affine transform의 선형부 `[a,b;c,d]`를 heading vector에도 적용한다.
- Factory/Labs의 회전 의미와 Reserve/Labyrinth 등 회전된 transform을 하나의 일반식으로 처리한다.
- Main Map과 MiniMap이 같은 projected heading을 사용한다.
- 위치 배치는 변경하지 않고 방향 좌표계를 위치와 일치시켰다.

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
| Map + MiniMap | 구현 완료 / heading projection 및 lifecycle repair 포함 |
| Game Content Update | 구현 완료 / LKG + FIR + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.11.2
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

```text
v1.11.1 → v1.11.2 mandatory Game Content migration: none
v1.11.1 → v1.11.2 user.db migration: none
v1.11.1 → v1.11.2 Scanner display settings migration: none
```

## 검증 상태

v1.11.2 exact product source `5822757f6490ec82aab33793752e48de14490628`은 다음을 모두 통과했다.

- 470 deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items / Hideout conditional inline search clear runtime smoke
- Factory/Labs/Reserve/Labyrinth 및 전체 현재 player transform heading regression
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

현재 `docs/ACTIVE_WORK.md`는 `NONE`이다. v1.11.2 릴리즈 배치에 남은 제품 개발 작업은 없다. 새 사용자 요구사항, 실사용 회귀, 또는 Tarkov 변화가 확인되면 현재 stable 기준으로 필요한 범위만 수정한다.

이 문서와 이후 documentation-only commit은 v1.11.2 product release source가 아니다. v1.11.2 product source/tag/assets는 `5822757f6490ec82aab33793752e48de14490628`에 고정한다.
