# RELEASE v1.13.1 — PUBLIC / VERIFIED

Date: **2026-08-31 KST**

## Release identity

```text
version: v1.13.1
status: PUBLIC STABLE / VERIFIED
exact product release source:
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

Tag `refs/tags/v1.13.1`, release `target_commitish`, GitHub `/releases/latest`, exact-main product source가 모두 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

## Exact-main artifact

```text
name: JunhyunHelper-win-x64
artifact id: 9747973218
archive bytes: 241,778,025
archive SHA-256:
58b38558b33095ddb20ec2e3cdd1ebeea7abb4e9c9c4614ce5d8747927b8e3f6
```

Release workflow는 위 exact-main CI artifact를 다운로드해 검증된 package/checksum을 그대로 공개했다. 공개용 제품을 별도로 다시 빌드하지 않았다.

## Public assets

```text
Junhyun-Helper.zip
asset id: 537579591
bytes: 80,614,695
SHA-256 / GitHub digest:
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737

SHA256SUMS.txt
asset id: 537579593
bytes: 86
SHA-256 / GitHub digest:
14c38f75b70a27d3d6d0ec956404e363dd7d134a6111da3a4b11538a97864e8c
```

Exact-main CI와 Release workflow에서 `SHA256SUMS.txt`의 `Junhyun-Helper.zip` 항목과 실제 ZIP SHA-256이 동일한 `d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737`임을 확인했다.

## Product changes

v1.13.1은 v1.13.0 Farming Guide의 실사용 UI/interaction 회귀를 바로잡는 PATCH 릴리즈다. 새 제품 능력을 추가하지 않는다.

- 장비 영역을 텍스트 목록형에서 아이콘 중심의 Tarkov 인벤토리 유사 슬롯 board로 재구성했다.
- 장착 장비, 리그·가방·보안 컨테이너, storage grid 배치 아이템을 실제 item icon으로 표시한다.
- drag ghost도 실제 item icon을 사용하고 `R` 회전 상태를 반영한다.
- WPF mouse capture 중 장비/carrier drop target을 찾지 못하던 경로를 geometry-backed probing으로 보강했다.
- geometry fallback은 ScrollViewer/ScrollContentPresenter 및 clipping ancestor의 실제 visible bounds를 존중한다.
- mouse-up 시 실제 release 좌표에서 drop probe를 다시 계산해 마지막 mouse-move 상태에 의존하지 않는다.
- 유효/무효 target의 초록/빨강 transient border를 pointer 이동/end 시 원복한다.
- 90도 회전한 비정사각형 item image는 layout-aware rotation을 사용해 footprint 안에서 축소·clipping되지 않게 했다.
- 프리셋 저장 아이콘과 검색창 수직 clipping을 수정했다.

## Preserved contracts

다음 v1.13.0 제품 계약은 그대로 유지한다.

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated storage grids / equipment slots / attachment slots / armor slots / conflicts 사용
- attachment / 교체형 armor plate 설정
- 전체 raid-start preset save/load
- melee / PMC dogtag fixed setting 분리
- filled carrier destructive replacement fail-closed
- impossible old preset placement current-content sanitization

Loot 가치 판단, pickup/discard/replace 추천, Scanner 실시간 recommendation, 실제 raid inventory 좌표의 지속적인 1:1 동기화는 v1.13.1에도 포함하지 않는다.

## Regression coverage / release gate

Exact-main은 다음을 통과했다.

- 494 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained single-file publish
- ProductVersion `1.13.1+302f83e...` identity 확인
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- ZIP checksum manifest / actual hash equality
- exact-main Documentation Consistency
- exact-main Actions artifact upload
- automatic verified Release workflow
- public tag / latest release / assets / GitHub digest readback

## Public verification

- release workflow `33365070880`: SUCCESS
- `/releases/latest`: v1.13.1
- tag ref `refs/tags/v1.13.1`: exact product source와 일치
- release target: exact product source와 일치
- public ZIP asset metadata/digest: verified
- checksum asset metadata/digest: verified
- stable release: `draft=false`, `prerelease=false`

후속 documentation-only commit은 v1.13.1 product release source가 아니다. v1.13.1 historical product identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정한다.
