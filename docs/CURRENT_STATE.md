# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.13.1
exact product release source/tag target:
302f83e88cc65b5fae9b86b5cae294b2586c85a0
PR: #243 — MERGED
validated PR head: 314ce0501c0f680aacb13d2b3c61b20487c4eb15
PR exact-head CI: 33364597514 — SUCCESS
PR exact-head Shutdown Race CI: 33364597501 — SUCCESS
PR exact-head Documentation Consistency: 33364597497 — SUCCESS
exact-main CI: 33364865109 — SUCCESS
exact-main Shutdown Race CI: 33364865123 — SUCCESS
exact-main Documentation Consistency: 33364865134 — SUCCESS
release workflow: 33365070880 — SUCCESS
release id: 379553485
published UTC: 2026-08-31T06:39:45Z
494 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537579591
bytes: 80,614,695
SHA-256:
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737

SHA256SUMS.txt
asset id: 537579593
bytes: 86
asset SHA-256:
14c38f75b70a27d3d6d0ec956404e363dd7d134a6111da3a4b11538a97864e8c
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9747973218
archive bytes: 241,778,025
archive SHA-256:
58b38558b33095ddb20ec2e3cdd1ebeea7abb4e9c9c4614ce5d8747927b8e3f6
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.1`, exact-main product source가 모두 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## v1.13.1 핵심 변경 — Farming Guide UI / drag-drop 회귀 수정

v1.13.1은 v1.13.0 Farming Guide의 제품 의미와 데이터 계약을 유지하는 PATCH 릴리즈다.

- 장비 영역을 텍스트 목록형에서 아이콘 중심의 Tarkov 인벤토리 유사 slot board로 재구성했다.
- equipped item, Rig / Backpack / Secure Container, storage grid placement, drag ghost에 실제 item icon을 사용한다.
- `R` 회전 시 비정사각형 icon도 회전된 footprint에 맞게 layout된다.
- WPF mouse capture 중 equipment/carrier target을 놓치던 drag/drop 판정을 geometry-backed probing으로 보강했다.
- geometry fallback은 ScrollViewer / ScrollContentPresenter / clipping ancestor의 visible bounds를 존중한다.
- mouse-up 실제 좌표에서 drop probe를 다시 계산한다.
- valid/invalid 초록·빨강 target border가 pointer 이동/end 뒤 남지 않게 cleanup한다.
- 프리셋 저장 아이콘과 검색창 텍스트 clipping을 수정했다.

## Farming Guide 유지 계약

Farming Guide는 Scanner 오른쪽의 raid-start Loadout / Inventory Editor다.

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- attachment / 교체형 armor plate 설정
- 전체 출발 상태 preset save/load
- melee / PMC dogtag fixed setting 분리
- 총 무게 / 사용·전체 storage cell 표시
- filled carrier destructive replacement fail-closed
- old preset impossible placement current-content sanitization

Loot 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 recommendation, 실제 raid inventory grid 좌표의 지속적인 1:1 동기화는 현재 범위가 아니다.

Canonical product decision:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

Architecture/maintenance contract:

- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호하며 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않는다.
- Scanner는 external screen pixels + OCR만 사용한다. game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass를 사용하지 않는다.
- Quest exact ProfileVariable은 runtime compatibility보다 우선하며 Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존한다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 actual published EXE runtime evidence로 검증한다.
- 공개 stable release bytes/tag/source는 immutable historical identity로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.13.1
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

Farming Guide 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 Game Content와 분리해 저장한다. v1.13.0 → v1.13.1 mandatory user data migration은 없다.

## 검증 상태

Exact product source `302f83e88cc65b5fae9b86b5cae294b2586c85a0`은 다음을 통과했다.

- 494 deterministic tests
- Windows Release build
- Windows x64 self-contained publish
- ProductVersion `1.13.1+302f83e...` identity 확인
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- clean portable root / forbidden dependency audit
- release package + checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## 남은 외부 실사용 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov에서 v1.13.1 최종 실사용 확인
- 김태영 실제 PC에서 diagnostic ZIP 수집/분석

공개 증거:

- `docs/RELEASE_1.13.1.md`
- `docs/.release-v1.13.1-status.json`
- `docs/RELEASE_NOTES_V1.13.1.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

후속 documentation-only commit은 v1.13.1 product release source가 아니다. historical identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정한다.
