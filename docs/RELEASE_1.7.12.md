# 준현 헬퍼 v1.7.12 — verified release record

기준일: 2026-08-27
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.12는 새 사용자 기능을 추가하지 않고 장기 유지보수성을 높인 patch-level maintenance release다.

핵심은 개별 WPF Page의 `Loaded` 순서에 분산되어 있던 공통 presentation infrastructure ownership을 명시적인 product-window composition 경계로 이동하고, 실제 published EXE smoke가 드러낸 Ammo의 숨은 lifecycle 결합을 제거한 것이다.

Scanner Item identity recognition의 threshold, candidate cap, matcher, visual recovery acceptance, 200 ms observation target과 Map/MiniMap donor revision은 변경하지 않았다.

## 제품/구조 변경

1. Desktop page infrastructure ownership
   - Quest / Hideout / Items / Ammo image-cache binding을 `MainWindow.OnInitialized`에서 연결한다.
   - Ammo favorite store와 cross-page navigation wiring도 같은 product-window lifetime에서 연결한다.
   - 개별 MainWindow page `Loaded` handler가 다른 page의 준비 상태를 우연히 결정하지 않는다.
2. Ammo presentation lifecycle
   - 초기 dead-code audit에서 `AmmoPage_Loaded` 제거 후 actual published EXE smoke가 Ammo detail collapse/expand 초기화 회귀를 탐지했다.
   - 원인은 제거된 handler 본문의 기능이 아니라 Ammo의 class-level `Loaded` handler가 부모의 Loaded subscription 존재 여부에 간접 의존하던 hidden WPF lifecycle coupling이었다.
   - 부모 handler를 복구하지 않고 Ammo search/detail/grid presentation을 `AmmoPage.OnInitialized` + Loaded dispatcher priority가 직접 소유하도록 수정했다.
3. Regression protection
   - `DesktopStartupWiringContractTests`로 startup/page ownership 경계를 고정했다.
   - source-level contract와 actual published EXE smoke를 함께 사용한다.

공식 결정:

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`

## Audit에서 변경하지 않은 항목

- Quest/Hideout/Items workspace의 반복 `LoadAsync`는 `UserProfileStore`의 immutable in-process snapshot cache 때문에 현재 evidence만으로 DB 병목으로 판정하지 않았다.
- 추가 global mutable cache, 병렬화, one-read/multi-build 재설계는 실제 runtime trace가 병목을 증명할 때까지 보류한다.
- `Legacy` Map/MiniMap bridge는 active compatibility/integration으로 유지한다.
- Factory/Map/MiniMap smoke는 active regression evidence로 유지한다.
- Scanner diagnostic OCR reflection adapter는 알려진 의도적 technical debt로 유지한다.
- original full-refresh mutation handlers + fast rebinding은 lifecycle 관여 증거가 있어 삭제하지 않았다.

## Scanner 안전 계약

유지한 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

또한 false positive보다 miss 선호, stale/cross-frame identity proof 금지, Item ID 확정 전 mapped metadata identity 사용 금지, scan-time network identity work 금지, matcher/visual recovery acceptance 완화 금지를 유지한다.

## PR 검증

PR #197 final head:

```text
23e1784c25954f4900a57cfa4c1c9821d5d6d668
```

Final PR CI:

```text
run: 33042136686
CI number: #2020
result: SUCCESS
397 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

리뷰에서 startup ownership의 공식 문서 누락이 지적되었고 `docs/STATE.md`와 `docs/DEVELOPER_REFERENCE.md`에 반영한 뒤 thread를 resolved 처리했다.

## exact product release source

PR #197 merge/main release source:

```text
d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
```

이 커밋이 v1.7.12 tag의 exact target이며 v1.7.12 ProductVersion metadata에도 이 SHA가 포함된다. 이후 documentation-only main commit은 제품 릴리즈 소스로 해석하지 않는다.

## main CI

```text
run: 33042307773
CI number: #2021
result: SUCCESS
397 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Published ProductVersion:

```text
1.7.12+d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
```

Main-CI release package:

```text
name: Junhyun-Helper.zip
bytes: 80,477,641
SHA-256: 3f0d57f8a5dc92611bc8648a423c43d65917e63e0d73a771b559153803186fa1
```

## Release workflow

```text
run: 33042464642
Release number: #40
result: SUCCESS
```

Release workflow는 exact main CI artifact를 내려받아 ProductVersion, FIRST_RUN identity, package checksum을 다시 검증한 뒤 stable release를 게시했다.

## public release readback

GitHub `/releases/latest` 및 tag ref readback:

```text
release id: 377581895
tag: v1.7.12
name: 준현 헬퍼 v1.7.12
target commitish: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
tag ref object: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
draft: false
prerelease: false
latest stable: true
published at UTC: 2026-08-27T05:26:26Z
```

Public asset:

```text
name: Junhyun-Helper.zip
asset id: 531791229
bytes: 80,477,641
GitHub asset digest: sha256:3f0d57f8a5dc92611bc8648a423c43d65917e63e0d73a771b559153803186fa1
```

이 공개 asset digest는 exact main CI가 생성·검증한 package SHA-256과 일치한다.

Checksum asset:

```text
name: SHA256SUMS.txt
asset id: 531791226
bytes: 86
GitHub asset digest: sha256:97cf0d26c1d6c91c5876ee02f829225a23221e2bc893659d211055aa6af6a99d
```

현재 도구 세션에서는 public binary asset을 독립 anonymous client로 다시 내려받아 byte-level 재검증하지 않았다. 따라서 이 문서는 실제로 수행한 exact main-CI package verification, Release workflow verification, public GitHub metadata/digest/tag-ref readback의 일치만 기록한다.

## 관련 문서

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`
- `docs/RELEASE_NOTES_V1.7.12.md`
- `docs/.release-v1.7.12-status.json`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
