# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 구현·검증 계약은 `docs/STATE.md`, 진행 중 작업 여부는 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**

상태: **`v1.12.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.12.0
exact product release source/tag target:
b2fcec460df256c581e87b53c6293dc4d2177b9c
final PR: #238 — MERGED
superseded draft PR: #237 — CLOSED / NOT MERGED
validated feature head: 5216ab410c8a4384aee7d9f1a69fbd30302ad0a8
feature-head CI: 33348681591 — SUCCESS
feature-head Shutdown Race CI: 33348681589 — SUCCESS
feature-head Documentation Consistency: 33348681555 — SUCCESS
exact-main CI: 33348916340 — SUCCESS
exact-main Shutdown Race CI: 33348916440 — SUCCESS
exact-main Documentation Consistency: 33348916365 — SUCCESS
release workflow: 33349066686 — SUCCESS
release id: 379463868
published UTC: 2026-08-31T01:56:23Z
482 passed / 0 failed
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537304923
bytes: 80,572,903
SHA-256:
d8ad140ee39ef533471a229ae01e80bc4ad7baeb5b513490c645bdbd3af137c0

SHA256SUMS.txt
asset id: 537304924
bytes: 86
asset SHA-256:
76a0dfb4e7734001a938798c2f6180f815d79b914e7d2b3933423f1f827673d7
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9742966369
archive bytes: 241,651,154
archive SHA-256:
c6122103fefa1c0b5ffd30787a4a60f6af1e151c3dd4694dca3584c7081145e9
```

GitHub `/releases/latest`, release `target_commitish`, `refs/tags/v1.12.0`, exact-main product source가 모두 `b2fcec460df256c581e87b53c6293dc4d2177b9c`로 일치한다. 공개 release는 `draft=false`, `prerelease=false`이며 공개 ZIP의 GitHub asset digest는 exact-main release workflow가 검증한 package SHA-256과 일치한다.

공개 증거:

- `docs/RELEASE_1.12.0.md`
- `docs/.release-v1.12.0-status.json`
- `docs/RELEASE_NOTES_V1.12.0.md`

## v1.12.0 핵심 변경

### Quest availability

- 사용자 실사용의 `확인 필요 49` 증상을 current EFT 1.1 staged task-pool 구조와 대조했다.
- current audited LL1 task-pool Quest는 48개이며, 과거 stage hidden counter를 정확히 알 수 없다는 이유로 trader LL이 이미 상승한 뒤에도 해당 pool을 unknown으로 유지하던 것이 대량 `확인 필요`의 주원인이었다.
- exact ProfileVariable 값은 항상 최우선이다.
- current stage에서는 기존 보수적 reconstruction / fail-closed를 유지한다.
- current trader LL이 audited pool stage보다 높으면 그 과거 stage의 threshold를 만족하는 runtime-only availability floor를 사용한다.
- 이 값은 server counter exact fact로 저장하지 않는다.
- structural drift는 fail-closed한다.
- Future Needed Items / cleanup은 이 current-UI compatibility를 사용하지 않고 기존 보수적 reachability를 유지한다.

### 은신처 검색창 clear

공통 clear glyph가 TextBox의 Left/Top/Right/Bottom 외부 margin을 모두 반영하도록 수정해 Hideout 검색창만 `×`가 다른 높이에 보이던 문제를 해결했다.

### 김태영 PC 진단

- 메인 헤더 좌측 프로필 이미지 클릭으로 시작한다.
- `김태영 본인이 맞습니까?` 확인 후에만 실행한다.
- Windows/display/DPI, GPU/driver/monitor, HDR/color/luminance, allowlisted capture/overlay app, Scanner 상태, display/Tarkov capture와 휘도 통계를 수집한다.
- Tarkov가 실행 중이면 client screen-copy와 PrintWindow evidence를 함께 남긴다.
- optional probe는 fail-soft이며 실패는 `probe-errors.txt`에 남긴다.
- ZIP은 Desktop에 생성되고 자동 업로드/이메일 전송은 하지 않는다.
- 사용자명·컴퓨터명·IP/MAC·네트워크 목록·credential·전체 환경변수·임의 전체 process list·설치 경로는 수집하지 않는다.
- 화면 PNG에는 실행 당시 실제 화면 내용이 포함될 수 있음을 실행 전에 고지한다.

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호한다.
- OCR/matcher/candidate acceptance는 reviewed actual Tarkov evidence 없이 임의 완화하지 않는다.
- Scanner recognition은 external screen pixels + OCR만 사용한다.
- game process memory read / injection / process hook / kernel / input automation / network manipulation / anti-cheat bypass를 사용하지 않는다.
- correction hotkey는 Ground Truth를 생성·추측하지 않는다.
- Hideout requirement의 `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- Ammo pickup은 same-caliber penetration 및 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime evidence로 검증한다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 / maintenance |
| Items / Ammo | 구현 완료 / profile-aware pickup 포함 |
| Map + MiniMap | 구현 완료 / maintenance |
| Game Content Update | 구현 완료 / LKG + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |
| 김태영 PC 진단 | **IMPLEMENTED / PUBLIC STABLE / REAL-PC SAMPLE PENDING** |

## Schema / compatibility

```text
Desktop target version: 1.12.0
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.11.4 → v1.12.0 mandatory Game Content migration: none  
v1.11.4 → v1.12.0 user.db migration: none  
v1.11.4 → v1.12.0 Scanner display settings migration: none

## 검증 상태

v1.12.0 exact product source `b2fcec460df256c581e87b53c6293dc4d2177b9c`은 Release build, deterministic tests, Windows x64 self-contained publish, actual published EXE Product UI / Map / Scanner smoke, graceful shutdown, active-async Shutdown Race, release package checksum audit, exact-main Documentation Consistency, exact-main artifact upload, verified Release workflow, public latest/tag/release/assets readback을 통과했다.

사용자의 실제 PC/Tarkov 플레이 환경에서 v1.12.0 최종 실사용 확인과 김태영 PC diagnostic ZIP의 실제 수집·분석은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

현재 `docs/ACTIVE_WORK.md`는 `NONE`이다. 새 사용자 요구사항, 실사용 회귀, Tarkov 변화, 또는 김태영 PC에서 생성한 실제 diagnostic evidence가 들어오면 현재 stable 기준으로 필요한 범위만 분석·수정한다.

이 문서와 이후 documentation-only commit은 v1.12.0 product release source가 아니다. v1.12.0 product source/tag/assets는 `b2fcec460df256c581e87b53c6293dc4d2177b9c`에 고정한다.
